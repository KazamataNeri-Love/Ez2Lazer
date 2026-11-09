// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.EzOsuGame.Edit;
using osu.Game.EzOsuGame.Layout;
using osu.Game.Rulesets.Mania.EzMania;
using osu.Game.Rulesets.UI;
using osuTK;

namespace osu.Game.Rulesets.Mania.UI
{
    public partial class ManiaPlayfieldAdjustmentContainer : PlayfieldAdjustmentContainer
    {
        protected override Container<Drawable> Content { get; }

        private readonly DrawSizePreservingFillContainer scalingContainer;
        private readonly Container positionContainer;

        /// <summary>
        /// 皮肤级斜轨。只在 <see cref="Ez3DTiltGate.RequiresBuffer"/> 成立时创建，否则为 null（零开销路径）。
        /// </summary>
        private readonly TiltBufferContainer? tiltContainer;

        private readonly DrawableManiaRuleset drawableManiaRuleset;
        private BindableFloat tiltAngle = null!;

        [Resolved]
        private EzConfigLayoutStore layoutStore { get; set; } = null!;

        private ManiaPlayfieldLayoutConfig playfieldLayout => layoutStore.PlayfieldLayout;

        private Anchor appliedAnchor;
        private Vector2 appliedPosition;

        public ManiaPlayfieldAdjustmentContainer(DrawableManiaRuleset drawableManiaRuleset)
        {
            this.drawableManiaRuleset = drawableManiaRuleset;

            InternalChild = scalingContainer = new DrawSizePreservingFillContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
            };

            Content = new Container
            {
                RelativeSizeAxes = Axes.Both,
            };

            // The stage position is applied here, outside the pseudo-3D buffer, so an offset never clips against it.
            positionContainer = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
            };

            var layout = GlobalConfigStore.Layout?.PlayfieldLayout;

            // 用实际角度而非默认值判断：皮肤编辑器的对比快照会把 Default 改写成当前值，
            // 那时按默认值判断会导致投影容器不被创建，斜轨角度整局失效。
            // 容器只能在构造期创建（运行期换父会崩），因此编辑器场景无条件建，换取拖滑块即时生效；
            // 其余场合只在真的会用到斜轨时才建。
            if (Ez3DTiltGate.RequiresBuffer(EzSkinEditorActiveState.Active, layout?.TiltAngle.Value ?? 0, drawableManiaRuleset.Mods))
            {
                positionContainer.Child = tiltContainer = new TiltBufferContainer
                {
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    Child = Content,
                };
            }
            else
            {
                positionContainer.Child = Content;
            }

            scalingContainer.Child = positionContainer;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            // 斜轨的生效值由 ruleset 统一持有：舞台设置与 Tilted Track 覆盖都写在那里，
            // 横向收窄（这里）与纵向距离律（EzTiltScrollAlgorithm）因此共用同一个角度。
            tiltAngle = drawableManiaRuleset.TiltAngle;

            tiltAngle.BindValueChanged(_ => updatePerspective());
            updatePerspective();
        }

        private void updatePerspective()
        {
            tiltContainer?.ApplyTilt(tiltAngle.Value);
        }

        protected override void Update()
        {
            base.Update();

            float aspectRatio = DrawWidth / DrawHeight;
            bool isPortrait = aspectRatio < 1f;

            if (isPortrait && drawableManiaRuleset.Beatmap.Stages.Count == 1)
            {
                // Scale playfield up by 25% to become playable on mobile devices,
                // and leave a 10% horizontal gap if the playfield is scaled down due to being too wide.
                const float base_scale = 1.25f;
                const float base_width = 768f / base_scale;
                const float side_gap = 0.9f;

                scalingContainer.Strategy = DrawSizePreservationStrategy.Maximum;
                float stageWidth = drawableManiaRuleset.Playfield.Stages[0].DrawWidth;
                scalingContainer.TargetDrawSize = new Vector2(1024, base_width * Math.Max(stageWidth / aspectRatio / (base_width * side_gap), 1f));
            }
            else
            {
                scalingContainer.Strategy = DrawSizePreservationStrategy.Minimum;
                scalingContainer.Scale = new Vector2(1f);
                scalingContainer.Size = new Vector2(1f);
                scalingContainer.TargetDrawSize = new Vector2(1024, 768);
            }

            updateStagePosition();
        }

        private void updateStagePosition()
        {
            float containerWidth = positionContainer.DrawWidth;
            float containerHeight = positionContainer.DrawHeight;

            if (containerWidth <= 0 || containerHeight <= 0)
                return;

            float stageWidth = 0;

            foreach (var stage in drawableManiaRuleset.Playfield.Stages)
                stageWidth += stage.DrawWidth;

            float value = playfieldLayout.StageXFraction;
            int direction = ManiaPlayfieldLayoutHelper.GetStageHorizontalDirection(value);

            playfieldLayout.StageOffsetFractionX = ManiaPlayfieldLayoutHelper.CalculateStageOffsetFraction(value, containerWidth, stageWidth);

            Anchor anchor = direction < 0 ? Anchor.CentreLeft : direction > 0 ? Anchor.CentreRight : Anchor.Centre;
            Vector2 position = ManiaPlayfieldLayoutHelper.CalculateStageOffsetPosition(
                value,
                playfieldLayout.StageYFraction,
                direction,
                containerWidth,
                containerHeight,
                stageWidth);

            if (appliedAnchor == anchor && appliedPosition == position)
                return;

            appliedAnchor = anchor;
            appliedPosition = position;
            positionContainer.Anchor = anchor;
            positionContainer.Position = position;
        }
    }
}
