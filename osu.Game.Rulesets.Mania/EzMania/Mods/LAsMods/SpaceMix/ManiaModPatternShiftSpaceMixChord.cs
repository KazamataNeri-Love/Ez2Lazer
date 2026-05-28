// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mania.Beatmaps;
using osu.Game.Rulesets.Mania.Objects;

namespace osu.Game.Rulesets.Mania.EzMania.Mods.LAsMods
{
    /// <summary>
    /// PS SpaceMix Chord — 在选定面板区域内生成多压和弦（Chord/拍）。
    /// </summary>
    public class ManiaModPatternShiftSpaceMixChord : ManiaModPatternShiftSpaceMixPatternBase
    {
        protected override KeyPatternType PatternType => KeyPatternType.Chord;
        protected override string PatternName => "Chord";
        protected override string PatternAcronym => "PSMC";
        public override LocalisableString Description => SpaceMixStrings.SPACEMIX_DESC_CHORD;
        protected override int DefaultLevel => 4;
        protected override int DefaultWindowProcessInterval => 1;
        protected override int DefaultWindowProcessOffset => 0;

        [SettingSource(typeof(SpaceMixStrings), nameof(SpaceMixStrings.SPACEMIX_PATTERN_LEVEL_LABEL), nameof(SpaceMixStrings.SPACEMIX_LEVEL_CHORD_DESCRIPTION))]
        public new BindableNumber<int> Level => base.Level;

        protected override void ApplyPatternForWindow(List<ManiaHitObject> windowObjects,
                                                      ManiaBeatmap beatmap,
                                                      double windowStart,
                                                      double windowEnd,
                                                      KeyPatternSettings settings,
                                                      Random rng,
                                                      int maxIterationsPerWindow)
        {
            if (windowObjects.Count < 2 || settings.Level < 1)
                return;

            double beatLength = beatmap.ControlPointInfo.TimingPointAt(windowStart).BeatLength;
            if (beatLength <= 0)
                return;

            int totalCols = beatmap.TotalColumns;
            int chordSize = Math.Clamp(settings.MinK + 1, 2, totalCols);
            double step = Math.Max(1, beatLength / 2.0);

            for (double t = windowStart + step; t < windowEnd; t += step)
            {
                // 收集该时刻可用的列
                var available = new List<int>();
                for (int col = 0; col < totalCols; col++)
                {
                    if (!ManiaKeyPatternHelp.HasNoteAtTime(beatmap, col, t))
                        available.Add(col);
                }

                int toAdd = Math.Min(chordSize, available.Count);
                for (int i = 0; i < toAdd; i++)
                {
                    int pick = rng.Next(available.Count);
                    beatmap.HitObjects.Add(new Note { Column = available[pick], StartTime = t });
                    available.RemoveAt(pick);
                }
            }
        }
    }
}
