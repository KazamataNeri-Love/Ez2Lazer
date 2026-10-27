// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.EzOsuGame.Localization;
using osu.Game.Graphics;
using osu.Game.Overlays.Settings;

namespace osu.Game.EzOsuGame.Overlays
{
    /// <summary>
    /// A section collecting Ez customisations which don't naturally belong to any of the other categories.
    /// Shown at the very bottom of the settings list, below <c>Debug</c>.
    /// </summary>
    public partial class EzCustomSection : SettingsSection
    {
        public override LocalisableString Header => EzSettingsStrings.EZ_CUSTOM_SECTION_HEADER;

        public override Drawable CreateIcon() => new SpriteIcon
        {
            Icon = OsuIcon.EditCircle
        };

        public EzCustomSection()
        {
            Children = new Drawable[]
            {
                new EzCustomSettings(),
            };
        }
    }
}
