// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Framework.Testing;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.UI;
using osuTK;

namespace osu.Game.Rulesets.Mania.EzMania.Mods.LAsMods
{
    /// <summary>
    /// Mania Barrel Roll — 整个 Playfield 匀速旋转，与 osu! 的 Barrel Roll 效果一致。
    ///
    /// 关键区别：
    ///   OsuPlayfieldAdjustmentContainer 构造时设 Anchor=Origin=Centre（(0,0) 在中心）
    ///   ManiaPlayfieldAdjustmentContainer 默认 Anchor=Origin=TopLeft（(0,0) 在左上角）
    ///   但 ManiaPAC 内部有 scalingContainer（DrawSizePreservingFillContainer, Centre/Centre）
    ///   → 旋转目标应为 scalingContainer，而非 PAC 本身
    /// </summary>
    public class ManiaModNeriBarrelRoll : ModBarrelRoll<ManiaHitObject>
    {
        public override string Name => "Barrel Roll";
        public override string Acronym => "BR";
        public override LocalisableString Description => "The whole playfield is on a wheel!";
        public override ModType Type => ModType.NeriMod;
        public override bool Ranked => false;
        public override bool ValidForMultiplayer => true;
        public override bool ValidForFreestyleAsRequiredMod => false;
        public override double ScoreMultiplier => 1;

        public override Type[] IncompatibleMods => Array.Empty<Type>();

        private Drawable? rotationTarget;
        private bool setupDone;

        public override void Update(Playfield playfield)
        {
            if (!setupDone)
                performSetup(playfield);

            if (rotationTarget == null)
            {
                // Fallback：找不到 scalingContainer，让基类旋转 PAC（绕左上角）
                base.Update(playfield);
                return;
            }

            // 与基类相同的旋转角度公式
            float angle = (Direction.Value == RotationDirection.Counterclockwise ? -1 : 1)
                          * 360 * (float)(playfield.Time.Current / 60000 * SpinSpeed.Value);

            rotationTarget.Rotation = angle;
        }

        private void performSetup(Playfield playfield)
        {
            // 沿父链向上找到 PlayfieldAdjustmentContainer
            PlayfieldAdjustmentContainer? pac = null;
            var current = playfield.Parent;
            while (current != null)
            {
                if (current is PlayfieldAdjustmentContainer p)
                {
                    pac = p;
                    break;
                }
                current = current.Parent;
            }

            if (pac == null) return;

            // ManiaPAC 构造: InternalChild = scalingContainer (Centre/Centre)
            // 递归搜索 DrawSizePreservingFillContainer（即 scalingContainer）
            foreach (var child in pac.ChildrenOfType<DrawSizePreservingFillContainer>())
            {
                // scalingContainer 的锚点是 Centre/Centre，正好作为旋转中心
                if (child.Anchor == Anchor.Centre && child.Origin == Anchor.Centre)
                {
                    rotationTarget = child;
                    break;
                }
            }

            if (rotationTarget == null)
            {
                // 没找到 scalingContainer → 回退基类逻辑
                return;
            }

            // 基类 ApplyToDrawableRuleset 对 PAC 做了缩放（适用于 osu! 方形 Playfield），
            // 但 Mania 的 Playfield 布局已自洽，不需要额外缩放，撤销之。
            pac.Scale = Vector2.One;

            setupDone = true;
        }
    }
}
