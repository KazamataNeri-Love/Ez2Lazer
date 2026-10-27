// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shapes;
using osu.Game.Configuration;
using osu.Game.EzOsuGame.Localization;
using osu.Game.EzOsuGame.Material;
using osu.Game.EzOsuGame.Screens;
using osu.Game.Localisation.SkinComponents;
using osu.Game.Skinning.Components;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.EzOsuGame.HUD
{
    /// <summary>
    /// <see cref="BoxElement"/> with three extra traits: an alpha-capable colour, an optional
    /// true-passthrough backdrop blur, and an optional outer shadow.
    /// </summary>
    /// <remarks>
    /// Blur follows the same pattern as <see cref="EzBoxElement"/>: the component declares the offscreen
    /// capture requirement to <see cref="IMaterialCaptureRegistrar"/> itself and samples content already
    /// rendered within <c>OsuScreenStack</c> via <see cref="AcrylicBackdropDrawable"/>.
    /// <para>
    /// The shadow uses <see cref="EdgeEffect"/>, which the framework renders through stencil masking
    /// (<c>renderer.PushMaskingInfo</c>) rather than an offscreen buffer, so it can coexist with the
    /// blur on the same drawable. It does require <see cref="CompositeDrawable.Masking"/>, which the
    /// <see cref="BoxElement"/> base already enables.
    /// </para>
    /// </remarks>
    public partial class NeriBoxElement : BoxElement, IMaterialBackdropConsumer
    {
        /// <summary>
        /// Background colour. Uses <see cref="EzSettingsColour"/> so the alpha channel is editable —
        /// lowering alpha lets the blurred backdrop show through.
        /// </summary>
        [SettingSource(typeof(SkinnableComponentStrings), nameof(SkinnableComponentStrings.Colour), nameof(EzHUDStrings.BOX_ELEMENT_COLOUR_DESCRIPTION),
            SettingControlType = typeof(EzSettingsColour))]
        public new BindableColour4 AccentColour { get; } = new BindableColour4(new Color4(1f, 1f, 1f, 0.12f));

        /// <summary>Box width in pixels. Overrides the base's fixed default size.</summary>
        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.BOX_ELEMENT_WIDTH_LABEL), nameof(EzHUDStrings.BOX_ELEMENT_WIDTH_DESCRIPTION))]
        public BindableNumber<float> BoxWidth { get; } = new BindableNumber<float>(400)
        {
            MinValue = 50,
            MaxValue = 1920,
            Precision = 1,
        };

        /// <summary>Box height in pixels. Overrides the base's fixed default size.</summary>
        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.BOX_ELEMENT_HEIGHT_LABEL), nameof(EzHUDStrings.BOX_ELEMENT_HEIGHT_DESCRIPTION))]
        public BindableNumber<float> BoxHeight { get; } = new BindableNumber<float>(80)
        {
            MinValue = 20,
            MaxValue = 1080,
            Precision = 1,
        };

        /// <summary>Whether the backdrop is blurred. While off, <see cref="BlurStrength"/> has no effect.</summary>
        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.BOX_ELEMENT_BLUR_LABEL), nameof(EzHUDStrings.BOX_ELEMENT_BLUR_DESCRIPTION))]
        public BindableBool BlurEnabled { get; } = new BindableBool(true);

        /// <summary>Blur strength. Only takes effect while <see cref="BlurEnabled"/> is on.</summary>
        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.BOX_ELEMENT_BLUR_STRENGTH_LABEL), nameof(EzHUDStrings.BOX_ELEMENT_BLUR_STRENGTH_DESCRIPTION))]
        public BindableNumber<float> BlurStrength { get; } = new BindableNumber<float>(16)
        {
            MinValue = 0,
            MaxValue = 40,
            Precision = 1,
        };

        /// <summary>Whether an outer shadow is drawn. While off, <see cref="ShadowRange"/> has no effect.</summary>
        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.BOX_ELEMENT_SHADOW_LABEL), nameof(EzHUDStrings.BOX_ELEMENT_SHADOW_DESCRIPTION))]
        public BindableBool ShadowEnabled { get; } = new BindableBool();

        /// <summary>Shadow spread. Only takes effect while <see cref="ShadowEnabled"/> is on.</summary>
        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.BOX_ELEMENT_SHADOW_RANGE_LABEL), nameof(EzHUDStrings.BOX_ELEMENT_SHADOW_RANGE_DESCRIPTION))]
        public BindableNumber<float> ShadowRange { get; } = new BindableNumber<float>(10)
        {
            MinValue = 0,
            MaxValue = 60,
            Precision = 1,
        };

        /// <summary>
        /// Whether the shadow is tinted with <see cref="AccentColour"/> instead of the default black.
        /// The accent's alpha carries over, so a very transparent box yields a very faint shadow.
        /// </summary>
        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.BOX_ELEMENT_SHADOW_USE_BOX_COLOUR_LABEL), nameof(EzHUDStrings.BOX_ELEMENT_SHADOW_USE_BOX_COLOUR_DESCRIPTION))]
        public BindableBool ShadowUsesBoxColour { get; } = new BindableBool();

        private static readonly Color4 shadow_colour = new Color4(0f, 0f, 0f, 0.5f);

        public bool WantsMaterialCapture => BlurEnabled.Value && BlurStrength.Value > 0;

        private readonly AcrylicBackdropDrawable acrylicBackdrop;
        private readonly Box tintBox;
        private EzMaterialCaptureController? captureController;

        [Resolved(canBeNull: true)]
        private IMaterialCaptureRegistrar? materialCaptureRegistrar { get; set; }

        [Resolved]
        private IRenderer renderer { get; set; } = null!;

        public NeriBoxElement()
        {
            // The base draws a single opaque box and tints itself through its own colour.
            // Replace that with [blurred backdrop] + [alpha tint] so the tint's alpha reveals the blur,
            // and keep the tint off the backdrop (it stays untinted so blur hue is not shifted).
            InternalChildren = new Drawable[]
            {
                acrylicBackdrop = new AcrylicBackdropDrawable
                {
                    RelativeSizeAxes = Axes.Both,
                    EffectEnabled = false,
                    FrameBufferScale = Vector2.One,
                },
                tintBox = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.White,
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            captureController = new EzMaterialCaptureController(materialCaptureRegistrar, renderer);

            BoxWidth.BindValueChanged(v => Width = v.NewValue, true);
            BoxHeight.BindValueChanged(v => Height = v.NewValue, true);

            AccentColour.BindValueChanged(v =>
            {
                tintBox.Colour = v.NewValue;
                updateShadow();
            }, true);

            BlurEnabled.BindValueChanged(_ => SyncMaterialCaptureState(), true);
            BlurStrength.BindValueChanged(_ => SyncMaterialCaptureState(), true);
            ShadowEnabled.BindValueChanged(_ => updateShadow(), true);
            ShadowRange.BindValueChanged(_ => updateShadow(), true);
            ShadowUsesBoxColour.BindValueChanged(_ => updateShadow());
        }

        public void SyncMaterialCaptureState()
            => captureController?.Sync(WantsMaterialCapture);

        private void updateShadow()
        {
            // Type = None disables the edge effect entirely (zero cost); a radius of 0 is also a no-op
            // in the draw node, so both the toggle and the range gate the shadow.
            EdgeEffect = ShadowEnabled.Value && ShadowRange.Value > 0
                ? new EdgeEffectParameters
                {
                    Type = EdgeEffectType.Shadow,
                    Radius = ShadowRange.Value,
                    Colour = ShadowUsesBoxColour.Value ? AccentColour.Value : shadow_colour,
                }
                : default;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
                captureController?.Dispose();

            base.Dispose(isDisposing);
        }
    }
}
