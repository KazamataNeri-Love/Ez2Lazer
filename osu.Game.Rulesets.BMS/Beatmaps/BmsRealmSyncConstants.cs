// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Rulesets.BMS.Beatmaps
{
    /// <summary>
    /// Versioning for how BMS charts are registered in Realm <see cref="osu.Game.Models.RealmNamedFileUsage"/> rows.
    /// Bump when <c>Files.Filename</c> rules change so the next sync re-imports external sets.
    /// </summary>
    public static class BmsRealmSyncConstants
    {
        /// <summary>
        /// v1: chart files use paths relative to <see cref="osu.Game.Beatmaps.BeatmapSetInfo.ExternalContentRoot"/> (not bare file names).
        /// v2: the declared stage image is registered alongside the charts, so background-change comparisons can
        /// distinguish charts instead of always reporting "equal" for unregistered files.
        /// v3: <c>Metadata.AudioFile</c> carries the chart's background music, so the standard song select has a
        /// track to preview.
        /// v4: the audio file is registered alongside the stage image, so switching charts switches the preview track
        /// (<see cref="osu.Game.Beatmaps.BeatmapInfo.AudioEquals"/>).
        /// </summary>
        public const int FILE_MAPPING_SCHEMA_VERSION = 4;

        public const string REALM_FILE_MAPPING_META_KEY = "realm_file_mapping_version";
    }
}
