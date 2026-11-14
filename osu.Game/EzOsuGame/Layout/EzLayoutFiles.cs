// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets;
using osu.Game.Skinning;

namespace osu.Game.EzOsuGame.Layout
{
    /// <summary>
    /// Ez layout JSON file names. A file is not a <see cref="GlobalSkinnableContainers"/> value.
    /// The three names that already exist as skin areas still map back onto that enum so the
    /// container can line up with the screen's skin lookup.
    /// </summary>
    public static class EzLayoutFiles
    {
        public const string SONG_SELECT = "SongSelect.json";

        public const string MAIN_HUD = "MainHUDComponents.json";

        /// <summary>Edge 旧版主 HUD 布局文件名；仅用于读回退，新写入用 <see cref="MAIN_HUD"/>。</summary>
        public const string LEGACY_MAIN_HUD = "Gameplay.json";

        public const string PLAYFIELD = "Playfield.json";

        public const string BACKGROUND = "Background.json";

        public const string MUSIC_PLAYER = "MusicPlayer.json";

        public const string RESULTS = "Results.json";

        public const string RESULTS_BACKGROUND = "ResultsBackground.json";

        public static GlobalSkinnableContainerLookup? SkinLookup(string layoutFile, RulesetInfo? ruleset)
        {
            GlobalSkinnableContainers? area = layoutFile switch
            {
                SONG_SELECT => GlobalSkinnableContainers.SongSelect,
                MAIN_HUD => GlobalSkinnableContainers.MainHUDComponents,
                PLAYFIELD => GlobalSkinnableContainers.Playfield,
                BACKGROUND => GlobalSkinnableContainers.Background,
                MUSIC_PLAYER => GlobalSkinnableContainers.MusicPlayer,
                RESULTS => GlobalSkinnableContainers.Results,
                RESULTS_BACKGROUND => GlobalSkinnableContainers.ResultsBackground,
                _ => null,
            };

            return area is GlobalSkinnableContainers known
                ? new GlobalSkinnableContainerLookup(known, ruleset)
                : null;
        }
    }
}
