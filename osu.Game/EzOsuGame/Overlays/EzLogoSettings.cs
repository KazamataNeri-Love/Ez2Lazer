// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.EzOsuGame.Localization;
using osu.Game.EzOsuGame.Screens.Menu;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays.Settings;

namespace osu.Game.EzOsuGame.Overlays
{
    public partial class EzLogoSettings : SettingsSubsection
    {
        private static readonly List<string> menu_logo_items = new List<string>
        {
            @"Menu/logo",
            @"Menu/logo2",
        };

        protected override LocalisableString Header => EzSettingsStrings.LOGO_SETTINGS_HEADER;

        private Bindable<EzLogoBackdropStyle> logoBackdropStyle = null!;
        private Bindable<EzLogoParticlesStyle> logoParticlesStyle = null!;
        private Bindable<bool> logoGlowEnabled = null!;

        [BackgroundDependencyLoader]
        private void load(Ez2ConfigManager ezConfig)
        {
            var menuLogoPath = ezConfig.GetBindable<string>(Ez2Setting.MenuLogoPath);
            logoBackdropStyle = ezConfig.GetBindable<EzLogoBackdropStyle>(Ez2Setting.MenuLogoBackdropStyle);
            logoParticlesStyle = ezConfig.GetBindable<EzLogoParticlesStyle>(Ez2Setting.MenuLogoParticlesStyle);
            logoGlowEnabled = ezConfig.GetBindable<bool>(Ez2Setting.MenuLogoGlowEnabled);

            var logoBackdropColour = new SettingsItemV2(new EzFormColour
            {
                Caption = EzSettingsStrings.LOGO_BACKDROP_COLOUR,
                Current = ezConfig.GetBindable<Colour4>(Ez2Setting.MenuLogoBackdropColour),
            });
            var logoBackdropBlur = new SettingsItemV2(new FormSliderBar<double>
            {
                Caption = EzSettingsStrings.LOGO_BACKDROP_BLUR,
                Current = ezConfig.GetBindable<double>(Ez2Setting.MenuLogoBackdropBlur),
                KeyboardStep = 1,
            });
            var logoBackdropColourTop = new SettingsItemV2(new EzFormColour
            {
                Caption = EzSettingsStrings.LOGO_BACKDROP_COLOUR_TOP,
                Current = ezConfig.GetBindable<Colour4>(Ez2Setting.MenuLogoBackdropColourTop),
            });
            var logoBackdropColourBottom = new SettingsItemV2(new EzFormColour
            {
                Caption = EzSettingsStrings.LOGO_BACKDROP_COLOUR_BOTTOM,
                Current = ezConfig.GetBindable<Colour4>(Ez2Setting.MenuLogoBackdropColourBottom),
            });
            var logoBackdropOpacity = new SettingsItemV2(new FormSliderBar<double>
            {
                Caption = EzSettingsStrings.LOGO_BACKDROP_OPACITY,
                HintText = EzSettingsStrings.LOGO_BACKDROP_OPACITY_TOOLTIP,
                Current = ezConfig.GetBindable<double>(Ez2Setting.MenuLogoBackdropOpacity),
                KeyboardStep = 0.01f,
            });

            var logoParticlesStyleItem = new SettingsItemV2(new FormEnumDropdown<EzLogoParticlesStyle>
            {
                Caption = EzSettingsStrings.LOGO_PARTICLES_STYLE,
                Current = logoParticlesStyle,
            });
            var logoParticlesColour = new SettingsItemV2(new EzFormColour
            {
                Caption = EzSettingsStrings.LOGO_PARTICLES_COLOUR,
                Current = ezConfig.GetBindable<Colour4>(Ez2Setting.MenuLogoParticlesColour),
            });
            var logoParticlesColourTop = new SettingsItemV2(new EzFormColour
            {
                Caption = EzSettingsStrings.LOGO_PARTICLES_COLOUR_TOP,
                Current = ezConfig.GetBindable<Colour4>(Ez2Setting.MenuLogoParticlesColourTop),
            });
            var logoParticlesColourBottom = new SettingsItemV2(new EzFormColour
            {
                Caption = EzSettingsStrings.LOGO_PARTICLES_COLOUR_BOTTOM,
                Current = ezConfig.GetBindable<Colour4>(Ez2Setting.MenuLogoParticlesColourBottom),
            });

            logoParticlesStyle.BindValueChanged(style =>
            {
                logoParticlesColour.CanBeShown.Value = style.NewValue == EzLogoParticlesStyle.Solid;
                logoParticlesColourTop.CanBeShown.Value = style.NewValue == EzLogoParticlesStyle.Gradient;
                logoParticlesColourBottom.CanBeShown.Value = style.NewValue == EzLogoParticlesStyle.Gradient;
            }, true);

            var logoSunStripesStyleItem = new SettingsItemV2(new FormEnumDropdown<EzLogoSunStripesStyle>
            {
                Caption = EzSettingsStrings.LOGO_SUN_STRIPES_STYLE,
                HintText = EzSettingsStrings.LOGO_SUN_STRIPES_TOOLTIP,
                Current = ezConfig.GetBindable<EzLogoSunStripesStyle>(Ez2Setting.MenuLogoSunStripesStyle),
            });
            var logoGlowColour = new SettingsItemV2(new EzFormColour
            {
                Caption = EzSettingsStrings.LOGO_GLOW_COLOUR,
                Current = ezConfig.GetBindable<Colour4>(Ez2Setting.MenuLogoGlowColour),
            });
            var logoGlowRadius = new SettingsItemV2(new FormSliderBar<double>
            {
                Caption = EzSettingsStrings.LOGO_GLOW_RADIUS,
                Current = ezConfig.GetBindable<double>(Ez2Setting.MenuLogoGlowRadius),
                KeyboardStep = 5,
            });

            logoBackdropStyle.BindValueChanged(style =>
            {
                logoBackdropColour.CanBeShown.Value = style.NewValue == EzLogoBackdropStyle.Solid;
                logoBackdropColourTop.CanBeShown.Value = style.NewValue == EzLogoBackdropStyle.Gradient;
                logoBackdropColourBottom.CanBeShown.Value = style.NewValue == EzLogoBackdropStyle.Gradient;
                logoBackdropBlur.CanBeShown.Value = style.NewValue == EzLogoBackdropStyle.Acrylic;
                logoSunStripesStyleItem.CanBeShown.Value = style.NewValue is EzLogoBackdropStyle.Solid or EzLogoBackdropStyle.Gradient;
            }, true);
            logoGlowEnabled.BindValueChanged(enabled =>
            {
                logoGlowColour.CanBeShown.Value = enabled.NewValue;
                logoGlowRadius.CanBeShown.Value = enabled.NewValue;
            }, true);

            ensureItemAvailable(menuLogoPath, menu_logo_items);

            Children = new Drawable[]
            {
                new SettingsButtonV2
                {
                    Text = EzSettingsStrings.LOGO_SYNTHWAVE_PRESET,
                    TooltipText = EzSettingsStrings.LOGO_SYNTHWAVE_PRESET_TOOLTIP,
                    Action = () => EzMenuLogoThemePresets.ApplySynthwaveSunset(ezConfig),
                },
                new SettingsItemV2(new FormCheckBox
                {
                    Caption = EzSettingsStrings.LOGO_HIDE_BORDER,
                    HintText = EzSettingsStrings.LOGO_HIDE_BORDER_TOOLTIP,
                    Current = ezConfig.GetBindable<bool>(Ez2Setting.MenuLogoHideBorder),
                }),
                new SettingsItemV2(new FormCheckBox
                {
                    Caption = EzSettingsStrings.LOGO_DISABLE_BEAT_PULSE,
                    HintText = EzSettingsStrings.LOGO_DISABLE_BEAT_PULSE_TOOLTIP,
                    Current = ezConfig.GetBindable<bool>(Ez2Setting.MenuLogoDisableBeatPulse),
                }),
                new SettingsItemV2(new FormEnumDropdown<EzLogoBackdropStyle>
                {
                    Caption = EzSettingsStrings.LOGO_BACKDROP,
                    HintText = EzSettingsStrings.LOGO_BACKDROP_TOOLTIP,
                    Current = logoBackdropStyle,
                }),
                logoBackdropColour,
                logoBackdropColourTop,
                logoBackdropColourBottom,
                logoBackdropBlur,
                logoBackdropOpacity,
                logoSunStripesStyleItem,
                new SettingsItemV2(new FormCheckBox
                {
                    Caption = EzSettingsStrings.LOGO_GLOW_ENABLED,
                    Current = logoGlowEnabled,
                }),
                logoGlowColour,
                logoGlowRadius,
                logoParticlesStyleItem,
                logoParticlesColour,
                logoParticlesColourTop,
                logoParticlesColourBottom,
                new SettingsItemV2(new FormDropdown<string>
                {
                    Caption = "Menu Logo",
                    Current = menuLogoPath,
                    Items = menu_logo_items,
                }),
                new SettingsItemV2(new FormTextBox
                {
                    Caption = EzSettingsStrings.LOGO_CENTRE_TEXT,
                    HintText = EzSettingsStrings.LOGO_CENTRE_TEXT_TOOLTIP,
                    PlaceholderText = EzSettingsStrings.LOGO_CENTRE_TEXT_PLACEHOLDER,
                    Current = ezConfig.GetBindable<string>(Ez2Setting.MenuLogoText),
                }),
                new SettingsItemV2(new FormEnumDropdown<EzLogoVisualisationStyle>
                {
                    Caption = EzSettingsStrings.LOGO_VISUALISATION,
                    HintText = EzSettingsStrings.LOGO_VISUALISATION_TOOLTIP,
                    Current = ezConfig.GetBindable<EzLogoVisualisationStyle>(Ez2Setting.MenuLogoVisualisationStyle),
                }),
            };
        }

        private static void ensureItemAvailable(Bindable<string> current, List<string> items)
        {
            if (!string.IsNullOrEmpty(current.Value) && !items.Contains(current.Value))
                items.Insert(0, current.Value);

            if (items.Count == 0)
                items.Add(string.Empty);

            if (string.IsNullOrEmpty(current.Value))
                current.Value = items[0];
        }
    }
}
