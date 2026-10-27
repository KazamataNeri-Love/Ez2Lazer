// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.EzOsuGame.Localization;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays.Settings;

namespace osu.Game.EzOsuGame.Overlays
{
    public partial class EzCustomSettings : SettingsSubsection
    {
        protected override LocalisableString Header => EzSettingsStrings.EZ_CUSTOM_SETTINGS_HEADER;

        [BackgroundDependencyLoader]
        private void load(Ez2ConfigManager ezConfig)
        {
            AddRange(new Drawable[]
            {
                new SettingsItemV2(new FormCheckBox
                {
                    Caption = EzSettingsStrings.USE_SKIN_CURSOR,
                    HintText = EzSettingsStrings.USE_SKIN_CURSOR_TOOLTIP,
                    Current = ezConfig.GetBindable<bool>(Ez2Setting.UseSkinCursor),
                })
                {
                    Keywords = new[] { "cursor", "skin", "menu", "pointer", "光标", "皮肤" }
                },

                new SettingsItemV2(new FormCheckBox
                {
                    Caption = EzSettingsStrings.PREVIEW_FROM_START,
                    HintText = EzSettingsStrings.PREVIEW_FROM_START_TOOLTIP,
                    Current = ezConfig.GetBindable<bool>(Ez2Setting.PreviewFromStart),
                })
                {
                    Keywords = new[] { "preview", "audio", "song select", "从头播放", "预览" }
                },
            });
        }
    }
}
