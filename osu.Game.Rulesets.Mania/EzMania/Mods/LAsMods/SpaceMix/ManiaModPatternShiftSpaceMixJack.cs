// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mania.Beatmaps;
using osu.Game.Rulesets.Mania.EzMania.Mods.ModHelp;
using osu.Game.Rulesets.Mania.Objects;

namespace osu.Game.Rulesets.Mania.EzMania.Mods.LAsMods
{
    /// <summary>
    /// PS SpaceMix Jack — 在选定面板区域内生成同键连打（Jack）模式。
    /// </summary>
    public class ManiaModPatternShiftSpaceMixJack : ManiaModPatternShiftSpaceMixPatternBase
    {
        protected override KeyPatternType PatternType => KeyPatternType.Jack;
        protected override string PatternName => "Jack";
        protected override string PatternAcronym => "PSMJ";
        public override LocalisableString Description => SpaceMixStrings.SPACEMIX_DESC_JACK;
        protected override int DefaultLevel => 5;
        protected override int DefaultWindowProcessInterval => 2;
        protected override int DefaultWindowProcessOffset => 0;

        [SettingSource(typeof(SpaceMixStrings), nameof(SpaceMixStrings.SPACEMIX_PATTERN_LEVEL_LABEL), nameof(SpaceMixStrings.SPACEMIX_LEVEL_JACK_DESCRIPTION))]
        public new BindableNumber<int> Level => base.Level;

        protected override void ApplyPatternForWindow(List<ManiaHitObject> windowObjects,
                                                      ManiaBeatmap beatmap,
                                                      double windowStart,
                                                      double windowEnd,
                                                      KeyPatternSettings settings,
                                                      Random rng,
                                                      int maxIterationsPerWindow)
        {
            if (windowObjects.Count < 2 || settings.Level <= 0)
                return;

            double beatLength = beatmap.ControlPointInfo.TimingPointAt(windowStart).BeatLength;
            if (beatLength <= 0)
                return;

            // 选一个锚点 note，在同一列附近添加同键连打
            int jackCount = Math.Clamp(settings.Level / 2, 1, 4);
            double step = Math.Max(1, beatLength / 4.0);

            for (int attempt = 0; attempt < jackCount; attempt++)
            {
                var anchor = windowObjects[rng.Next(windowObjects.Count)];
                double newTime = anchor.StartTime + (rng.NextDouble() - 0.5) * step * 2;
                newTime = Math.Clamp(newTime, windowStart, windowEnd);

                int targetCol = anchor.Column;

                if (!ManiaKeyPatternHelp.HasNoteAtTime(beatmap, targetCol, newTime)
                    && !isHoldOccupying(beatmap, targetCol, newTime))
                {
                    beatmap.HitObjects.Add(new Note { Column = targetCol, StartTime = newTime });
                }
            }
        }

        private static bool isHoldOccupying(ManiaBeatmap beatmap, int col, double time)
        {
            foreach (var obj in beatmap.HitObjects)
                if (obj is HoldNote h && h.Column == col && h.StartTime <= time + 0.5 && h.EndTime >= time - 0.5)
                    return true;
            return false;
        }
    }
}
