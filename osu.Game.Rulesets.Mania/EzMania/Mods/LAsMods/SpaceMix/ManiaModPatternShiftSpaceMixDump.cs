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
    /// PS SpaceMix Dump — 在选定面板区域内生成滑梯形（Dump/楼梯）排列。
    /// </summary>
    public class ManiaModPatternShiftSpaceMixDump : ManiaModPatternShiftSpaceMixPatternBase
    {
        protected override KeyPatternType PatternType => KeyPatternType.Dump;
        protected override string PatternName => "Dump";
        protected override string PatternAcronym => "PSMS";
        public override LocalisableString Description => SpaceMixStrings.SPACEMIX_DESC_DUMP;
        protected override int DefaultLevel => 3;
        protected override int DefaultWindowProcessInterval => 1;
        protected override int DefaultWindowProcessOffset => 1;

        [SettingSource(typeof(SpaceMixStrings), nameof(SpaceMixStrings.SPACEMIX_PATTERN_LEVEL_LABEL), nameof(SpaceMixStrings.SPACEMIX_LEVEL_DUMP_DESCRIPTION))]
        public new BindableNumber<int> Level => base.Level;

        protected override void ApplyPatternForWindow(List<ManiaHitObject> windowObjects,
                                                      ManiaBeatmap beatmap,
                                                      double windowStart,
                                                      double windowEnd,
                                                      KeyPatternSettings settings,
                                                      Random rng,
                                                      int maxIterationsPerWindow)
        {
            if (windowObjects.Count < 3 || settings.Level < 2)
                return;

            // 收集单 note（排除同时刻和弦和 Hold）
            var singles = new List<(double time, ManiaHitObject obj)>();
            int i = 0;
            while (i < windowObjects.Count)
            {
                var obj = windowObjects[i];
                double time = obj.StartTime;
                int j = i + 1;
                while (j < windowObjects.Count && Math.Abs(windowObjects[j].StartTime - time) <= TIME_TOLERANCE)
                    j++;
                if (j - i == 1 && obj is Note)
                    singles.Add((time, obj));
                i = j;
            }

            if (singles.Count < 3)
                return;

            int totalCols = beatmap.TotalColumns;
            int direction = rng.Next(2) == 0 ? 1 : -1;

            // 按时间重排前 N 个单 note 为单调列序列
            int count = Math.Min(singles.Count, settings.Level + 2);
            var sorted = singles.OrderBy(s => s.time).Take(count).ToList();

            double startCol = direction > 0 ? 0 : totalCols - 1;
            double endCol = direction > 0 ? totalCols - 1 : 0;

            for (int k = 0; k < sorted.Count; k++)
            {
                var (time, obj) = sorted[k];
                double ratio = sorted.Count > 1 ? (double)k / (sorted.Count - 1) : 0;
                int targetCol = (int)Math.Round(startCol + (endCol - startCol) * ratio);
                targetCol = Math.Clamp(targetCol, 0, totalCols - 1);

                if (targetCol != obj.Column
                    && !ManiaKeyPatternHelp.HasNoteAtTime(beatmap, targetCol, time))
                {
                    obj.Column = targetCol;
                }
            }
        }
    }
}
