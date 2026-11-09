// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Animations;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Mania.UI;
using osu.Game.Rulesets.UI.Scrolling;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Rulesets.Mania.Skinning.Legacy
{
    public partial class LegacyHitExplosion : LegacyManiaColumnElement, IHitExplosion
    {
        public const double FADE_IN_DURATION = 80;

        /// <summary>
        /// 叠加但保留目标 alpha：光效只加亮、不覆盖底色。写入 alpha 会让黑底贴图在缓冲层里变成不透明黑块，
        /// 也会让光效在预乘合成的缓冲里被当成 alpha 内容压暗。
        /// </summary>
        private static readonly BlendingParameters additive_preserve_alpha = new BlendingParameters
        {
            Source = BlendingType.SrcAlpha,
            Destination = BlendingType.One,
            SourceAlpha = BlendingType.Zero,
            DestinationAlpha = BlendingType.One,
            RGBEquation = BlendingEquation.Add,
            AlphaEquation = BlendingEquation.Add,
        };

        private readonly IBindable<ScrollingDirection> direction = new Bindable<ScrollingDirection>();

        private Drawable? explosion;

        public LegacyHitExplosion()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load(ISkinSource skin, IScrollingInfo scrollingInfo)
        {
            string imageName = GetColumnSkinConfig<string>(skin, LegacyManiaSkinConfigurationLookups.ExplosionImage)?.Value
                               ?? "lightingN";

            float explosionScale = GetColumnSkinConfig<float>(skin, LegacyManiaSkinConfigurationLookups.ExplosionScale)?.Value
                                   ?? 1;

            // Create a temporary animation to retrieve the number of frames, in an effort to calculate the intended frame length.
            // This animation is discarded and re-queried with the appropriate frame length afterwards.
            var tmp = skin.GetAnimation(imageName, true, false);
            double frameLength = 0;
            if (tmp is IFramedAnimation tmpAnimation && tmpAnimation.FrameCount > 0)
                frameLength = Math.Max(1000 / 60.0, 170.0 / tmpAnimation.FrameCount);

            explosion = skin.GetAnimation(imageName, true, false, frameLength: frameLength)?.With(d =>
            {
                d.Origin = Anchor.Centre;
                d.Blending = additive_preserve_alpha;
                d.Scale = new Vector2(explosionScale);
            });

            if (explosion != null)
                InternalChild = explosion;

            direction.BindTo(scrollingInfo.Direction);
            direction.BindValueChanged(onDirectionChanged, true);
        }

        private void onDirectionChanged(ValueChangedEvent<ScrollingDirection> direction)
        {
            explosion?.Anchor = direction.NewValue == ScrollingDirection.Up ? Anchor.TopCentre : Anchor.BottomCentre;
        }

        public void Animate(JudgementResult result)
        {
            (explosion as IFramedAnimation)?.GotoFrame(0);

            explosion?.FadeInFromZero(FADE_IN_DURATION)
                     .Then().FadeOut(120);
        }
    }
}
