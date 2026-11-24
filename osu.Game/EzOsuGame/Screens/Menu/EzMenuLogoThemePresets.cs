// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Game.EzOsuGame.Configuration;

namespace osu.Game.EzOsuGame.Screens.Menu
{
    public static class EzMenuLogoThemePresets
    {
        public static void ApplySynthwaveSunset(Ez2ConfigManager ezConfig)
        {
            ezConfig.SetValue(Ez2Setting.MenuLogoHideBorder, true);
            ezConfig.SetValue(Ez2Setting.MenuLogoDisableBeatPulse, true);
            ezConfig.SetValue(Ez2Setting.MenuLogoVisualisationStyle, EzLogoVisualisationStyle.Off);
            ezConfig.SetValue(Ez2Setting.MenuLogoBackdropStyle, EzLogoBackdropStyle.Gradient);
            ezConfig.SetValue(Ez2Setting.MenuLogoBackdropColourTop, Colour4.FromHex("FFE066"));
            ezConfig.SetValue(Ez2Setting.MenuLogoBackdropColourBottom, Colour4.FromHex("FF2E88"));
            ezConfig.SetValue(Ez2Setting.MenuLogoParticlesStyle, EzLogoParticlesStyle.Off);
            ezConfig.SetValue(Ez2Setting.MenuLogoSunStripesStyle, EzLogoSunStripesStyle.Scrolling);
            ezConfig.SetValue(Ez2Setting.MenuLogoGlowEnabled, true);
            ezConfig.SetValue(Ez2Setting.MenuLogoGlowColour, Colour4.FromHex("FF2E88"));
            ezConfig.SetValue(Ez2Setting.MenuLogoGlowRadius, 120.0);
        }
    }
}
