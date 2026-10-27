// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Rulesets.Mania.Objects.Drawables;
using osu.Game.Rulesets.Mania.UI;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.UI;
using osu.Game.Rulesets.UI.Scrolling;
using osuTK;

namespace osu.Game.Rulesets.Mania.EzMania.Mods.LAsMods
{
    /// <summary>
    /// 挂在每列 <see cref="Column.TopLevelContainer"/> 末尾的斜轨补偿组件。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 为什么不能写在 <see cref="ManiaModSlantCompensation"/> 的 Update 里：
    /// <see cref="ScrollingHitObjectContainer"/> 每帧都会在自己的 <c>UpdateAfterChildrenLife()</c> 中
    /// 把每个物件的 <c>Y</c> 重设为线性滚动位置，而它属于列的子树、晚于 Playfield 的 Update。
    /// 本组件位于列内最后一个容器，更新时机在滚动容器之后，因此读到的 <c>Y</c> 一定是本帧的线性值。
    /// </para>
    /// <para>
    /// 补偿模型来自 Emiria 的 <c>WidgetKeyTrack3D</c>：把「屏幕上应有的位置」反解成「透视缓冲区内应绘制的位置」。
    /// 映射函数与 framework 的 <c>sh_Perspective.fs</c> 完全一致，因此不依赖任何经验常数。
    /// </para>
    /// </remarks>
    internal partial class SlantNoteCompensator : Drawable
    {
        private readonly Column column;
        private readonly IBindable<float> motionCompensation;
        private readonly IBindable<float> thicknessCompensation;

        private BufferedContainer? perspective;

        public SlantNoteCompensator(Column column, IBindable<float> motionCompensation, IBindable<float> thicknessCompensation)
        {
            this.column = column;
            this.motionCompensation = motionCompensation;
            this.thicknessCompensation = thicknessCompensation;

            RelativeSizeAxes = Axes.Both;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            perspective = findPerspectiveContainer();
        }

        protected override void Update()
        {
            base.Update();

            var buffer = perspective;

            if (buffer == null || !buffer.IsLoaded || buffer.VerticalPerspective <= 0)
                return;

            float bufferHeight = buffer.DrawHeight;

            if (bufferHeight <= 0)
                return;

            // s 与 ManiaPlayfieldAdjustmentContainer 传给 shader 的值保持同一个来源。
            float s = Math.Clamp(buffer.PerspectiveScale.X, 0.05f, 0.999f);

            if (MathF.Abs(s - 1f) < 0.001f)
                return;

            float kMotion = motionCompensation.Value;
            float kThickness = thicknessCompensation.Value;

            if (kMotion <= 0 && kThickness <= 0)
                return;

            var container = column.HitObjectContainer;

            if (!container.IsLoaded)
                return;

            // AliveEntries 未排序，避免 AliveObjects 每帧的 OrderBy 开销。
            foreach (var entry in container.AliveEntries)
                compensate(entry.Value, container, buffer, bufferHeight, s, kMotion, kThickness);
        }

        private static void compensate(DrawableHitObject drawable, ScrollingHitObjectContainer container, BufferedContainer buffer,
                                      float bufferHeight, float s, float kMotion, float kThickness)
        {
            // 物件 Origin（BottomCentre = 判定点）在透视缓冲区中的 y。
            float currentBufferY = drawable.ToSpaceOfOtherDrawable(Vector2.Zero, buffer).Y;
            float p = Math.Clamp(currentBufferY / bufferHeight, 0f, 1f);

            float pTarget = p + (toSourceY(p, s) - p) * kMotion;

            if (MathF.Abs(pTarget - p) > 0.0001f)
            {
                float currentLocalY = drawable.ToSpaceOfOtherDrawable(Vector2.Zero, container).Y;
                float targetLocalY = container.ToLocalSpace(buffer.ToScreenSpace(new Vector2(0, pTarget * bufferHeight))).Y;
                drawable.Y += targetLocalY - currentLocalY;
            }

            if (kThickness <= 0 || drawable is DrawableBarLine)
                return;

            // 屏幕上的视觉高度 ∝ buffer 高度 × f'(源位置)；要恒定则乘以 1 / f'。
            float sourceY = toSourceY(p, s);
            float derivative = (2f * (1f - s) * sourceY + 2f * s) / (1f + s);

            if (derivative <= 0.0001f)
                return;

            float scaleY = 1f + (1f / derivative - 1f) * kThickness;

            if (drawable.HitObject is IHasDuration duration)
            {
                // LN 用 Height 表达长度：底边（起点）已在上面完成映射，这里只重算长度。
                remapHeight(drawable, container, buffer, bufferHeight, s, kMotion, duration.EndTime);
            }
            else
            {
                // tap note：Origin = BottomCentre，向上拉伸，位置不变。
                drawable.Scale = new Vector2(1f, scaleY);
            }
        }

        private static void remapHeight(DrawableHitObject drawable, ScrollingHitObjectContainer container, BufferedContainer buffer,
                                        float bufferHeight, float s, float kMotion, double endTime)
        {
            float endLinear = container.PositionAtTime(endTime, container.Time.Current);
            float endLocalY = container.DrawHeight + endLinear;
            float endBufferY = buffer.ToLocalSpace(container.ToScreenSpace(new Vector2(0, endLocalY))).Y;
            float pEnd = Math.Clamp(endBufferY / bufferHeight, 0f, 1f);
            float pEndTarget = pEnd + (toSourceY(pEnd, s) - pEnd) * kMotion;
            float endTargetLocalY = container.ToLocalSpace(buffer.ToScreenSpace(new Vector2(0, pEndTarget * bufferHeight))).Y;

            float startLocalY = drawable.ToSpaceOfOtherDrawable(Vector2.Zero, container).Y;
            drawable.Height = MathF.Max(0f, startLocalY - endTargetLocalY);
        }

        /// <summary>
        /// shader 的屏幕 → 缓冲区映射（<c>sh_Perspective.fs</c> 中 y 轴的逆解）。
        /// 输入/输出均为缓冲区归一化 y（0 = 顶部，1 = 底部）。
        /// </summary>
        private static float toSourceY(float yScreen, float s)
        {
            if (MathF.Abs(s - 1f) < 0.001f)
                return yScreen;

            return (-s + MathF.Sqrt(s * s + (1f - s) * (1f + s) * yScreen)) / (1f - s);
        }

        private BufferedContainer? findPerspectiveContainer()
        {
            PlayfieldAdjustmentContainer? adjustment = null;

            for (Drawable? d = Parent; d != null; d = d.Parent)
            {
                if (d is PlayfieldAdjustmentContainer pac)
                {
                    adjustment = pac;
                    break;
                }
            }

            return adjustment == null ? null : findBufferedContainer(adjustment);
        }

        private static BufferedContainer? findBufferedContainer(CompositeDrawable root) => root.ChildrenOfType<BufferedContainer>().FirstOrDefault();
    }
}
