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
    /// PS SpaceMix Bracket — 在选定面板区域内生成切叉（Bracket/流形交互）。
    /// </summary>
    public class ManiaModPatternShiftSpaceMixBracket : ManiaModPatternShiftSpaceMixPatternBase
    {
        protected override KeyPatternType PatternType => KeyPatternType.Bracket;
        protected override string PatternName => "Bracket";
        protected override string PatternAcronym => "PSMB";
        public override LocalisableString Description => SpaceMixStrings.SPACEMIX_DESC_BRACKET;
        protected override int DefaultLevel => 4;
        protected override int DefaultWindowProcessInterval => 1;
        protected override int DefaultWindowProcessOffset => 0;

        [SettingSource(typeof(SpaceMixStrings), nameof(SpaceMixStrings.SPACEMIX_PATTERN_LEVEL_LABEL), nameof(SpaceMixStrings.SPACEMIX_LEVEL_BRACKET_DESCRIPTION))]
        public new BindableNumber<int> Level => base.Level;

        protected override void ApplyPatternForWindow(List<ManiaHitObject> windowObjects,
                                                      ManiaBeatmap beatmap,
                                                      double windowStart,
                                                      double windowEnd,
                                                      KeyPatternSettings settings,
                                                      Random rng,
                                                      int maxIterationsPerWindow)
        {
            if (windowObjects.Count < 4 || settings.Level < 2)
                return;

            double beatLength = beatmap.ControlPointInfo.TimingPointAt(windowStart).BeatLength;
            if (beatLength <= 0)
                return;

            int totalCols = beatmap.TotalColumns;
            int halfCols = totalCols / 2;
            double step = Math.Max(1, beatLength / 4.0);

            // Bracket: 交替在左右半区添加 note，形成切叉感
            int bracketCount = Math.Clamp(settings.Level, 1, 6);
            bool side = rng.Next(2) == 0;

            for (int i = 0; i < bracketCount; i++)
            {
                double t = windowStart + (i + 1) * step;
                if (t >= windowEnd)
                    break;

                int minCol = side ? 0 : halfCols;
                int maxCol = side ? halfCols - 1 : totalCols - 1;
                side = !side;

                // 在半区内找可用列
                var available = new List<int>();
                for (int col = minCol; col <= maxCol; col++)
                {
                    if (!ManiaKeyPatternHelp.HasNoteAtTime(beatmap, col, t))
                        available.Add(col);
                }

                if (available.Count > 0)
                {
                    int pick = rng.Next(available.Count);
                    beatmap.HitObjects.Add(new Note { Column = available[pick], StartTime = t });
                }
            }
        }
    }
}
