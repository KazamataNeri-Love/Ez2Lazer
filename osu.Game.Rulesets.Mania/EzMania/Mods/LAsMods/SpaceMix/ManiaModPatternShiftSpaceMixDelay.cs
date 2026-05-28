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
    /// PS SpaceMix Delay — 在选定面板区域内对 note 进行时间微调偏移。
    /// </summary>
    public class ManiaModPatternShiftSpaceMixDelay : ManiaModPatternShiftSpaceMixPatternBase
    {
        protected override KeyPatternType PatternType => KeyPatternType.Delay;
        protected override string PatternName => "Delay";
        protected override string PatternAcronym => "PSMD";
        public override LocalisableString Description => SpaceMixStrings.SPACEMIX_DESC_DELAY;
        protected override int DefaultLevel => 3;
        protected override int DefaultWindowProcessInterval => 1;
        protected override int DefaultWindowProcessOffset => 1;

        [SettingSource(typeof(SpaceMixStrings), nameof(SpaceMixStrings.SPACEMIX_PATTERN_LEVEL_LABEL), nameof(SpaceMixStrings.SPACEMIX_LEVEL_DELAY_DESCRIPTION))]
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

            double offsetAmount = beatLength * ManiaKeyPatternHelp.GetDelayBeatFraction(settings.Level);
            int maxShift = Math.Min(settings.Level, windowObjects.Count);

            var indexes = Enumerable.Range(0, windowObjects.Count).OrderBy(_ => rng.Next()).Take(maxShift).ToList();

            foreach (int idx in indexes)
            {
                var obj = windowObjects[idx];
                double direction = rng.NextDouble() < 0.5 ? -1 : 1;
                double offset = direction * offsetAmount;
                double newTime = Math.Max(0, obj.StartTime + offset);

                if (!ManiaKeyPatternHelp.HasNoteAtTime(beatmap, obj.Column, newTime, obj))
                {
                    if (obj is HoldNote hold)
                    {
                        double duration = hold.EndTime - hold.StartTime;
                        hold.StartTime = Math.Max(0, hold.StartTime + offset);
                        hold.EndTime = Math.Max(hold.StartTime, hold.StartTime + duration);
                    }
                    else
                    {
                        obj.StartTime = newTime;
                    }
                }
            }
        }
    }
}
