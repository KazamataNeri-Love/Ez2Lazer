// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Game.Rulesets.Mania.Objects.Drawables;
using osu.Game.Rulesets.Mania.UI;
using osuTK;

namespace osu.Game.Rulesets.Mania.EzMania.Tilt
{
    /// <summary>
    /// 按深度缩放 note 自身的高度。挂在每列 <see cref="Column.TopLevelContainer"/> 末尾，
    /// 因此更新时机在滚动容器之后，读到的是本帧已经定好的 <c>Y</c>。
    /// </summary>
    /// <remarks>
    /// 位置与间距由 <see cref="EzTiltScrollAlgorithm"/> 决定，这里只补尺寸：
    /// 屏幕高度 ∝ 局部导数 (1/(1+k·u)²)，归一化后即 ((1 + k(1 − v)) / (1 + k))²，v 为归一化位置。
    /// 与间距律同源，所以「note 占局部间距的比例」在任何深度都保持不变。
    /// </remarks>
    internal partial class EzTiltNoteScaler : Drawable
    {
        private readonly Column column;
        private readonly DrawableManiaRuleset ruleset;

        public EzTiltNoteScaler(Column column, DrawableManiaRuleset ruleset)
        {
            this.column = column;
            this.ruleset = ruleset;

            RelativeSizeAxes = Axes.Both;
        }

        protected override void Update()
        {
            base.Update();

            float strength = ruleset.TiltStrength;

            if (strength <= 0f)
                return;

            var container = column.HitObjectContainer;

            if (!container.IsLoaded)
                return;

            float height = container.DrawHeight;

            if (height <= 0f)
                return;

            foreach (var entry in container.AliveEntries)
            {
                var drawable = entry.Value;

                switch (drawable)
                {
                    case DrawableHoldNote hold:
                        scale(hold.Head, hold.Y, height, strength);
                        scale(hold.Tail, hold.Y - hold.DrawHeight, height, strength);
                        break;

                    case DrawableBarLine:
                        break;

                    default:
                        scale(drawable, drawable.Y, height, strength);
                        break;
                }
            }
        }

        private static void scale(Drawable drawable, float localY, float containerHeight, float strength)
        {
            float v = Math.Clamp(Math.Abs(localY) / containerHeight, 0f, 1f);
            float linear = (1f + strength * (1f - v)) / (1f + strength);
            float factor = linear * linear;

            if (MathF.Abs(drawable.Scale.Y - factor) < 0.002f)
                return;

            drawable.Scale = new Vector2(1f, factor);
        }
    }
}
