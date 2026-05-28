// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Utils;
using osu.Game.Rulesets.Mania.Objects;

namespace osu.Game.Rulesets.Mania.EzMania.Mods.LAsMods
{
    /// <summary>
    /// SpaceMix 面板切换策略引擎。
    /// KPS 越高 → Effect 面板权重越低，高密度段锁定面板不切换。
    /// </summary>
    public class SpaceMixPanelStrategy
    {
        public float KpsMax = 30f;
        public float EffectBaseWeight = 0.35f;
        public int MinBeatsPerPanel = 2;
        public float LockThresholdRatio = 0.4f;
        public float UnlockThresholdRatio = 0.7f;

        public SpaceMixZone ActiveZone { get; private set; } = SpaceMixZone.DP_Left;
        private int beatsInCurrentPanel;
        private bool isLocked;

        public SpaceMixZone DecidePanel(
            List<ManiaHitObject> windowObjects,
            double windowDuration,
            double beatLength,
            Random rng)
        {
            float leftKps = CalcKps(windowObjects, 0, 4, windowDuration);
            float effectKps = CalcKps(windowObjects, 5, 8, windowDuration);
            float rightKps = CalcKps(windowObjects, 9, 13, windowDuration);
            float totalKps = leftKps + effectKps + rightKps;

            if (isLocked && totalKps > KpsMax * UnlockThresholdRatio)
                return ActiveZone;

            if (totalKps > KpsMax * LockThresholdRatio)
            {
                isLocked = true;
                beatsInCurrentPanel = 0;
                return ActiveZone;
            }

            isLocked = false;

            if (beatsInCurrentPanel < MinBeatsPerPanel)
            {
                beatsInCurrentPanel++;
                return ActiveZone;
            }

            float kpsRatio = Math.Clamp(totalKps / KpsMax, 0f, 1f);
            float effectDensityBonus = Math.Clamp(effectKps / Math.Max(1f, totalKps), 0f, 0.3f);
            float effectWeight = Math.Clamp(EffectBaseWeight * (1f - kpsRatio) + effectDensityBonus, 0.05f, 0.85f);

            SpaceMixZone newZone;
            if (rng.NextDouble() < effectWeight)
            {
                newZone = SpaceMixZone.Effect;
            }
            else
            {
                float dpTotal = leftKps + rightKps;
                if (dpTotal <= 0)
                    newZone = rng.Next(2) == 0 ? SpaceMixZone.DP_Left : SpaceMixZone.DP_Right;
                else
                    newZone = rng.NextDouble() < leftKps / dpTotal ? SpaceMixZone.DP_Left : SpaceMixZone.DP_Right;
            }

            ActiveZone = newZone;
            beatsInCurrentPanel = 0;
            return ActiveZone;
        }

        public static float CalcKps(List<ManiaHitObject> objects, int startCol, int endCol, double durationMs)
        {
            if (durationMs <= 0) return 0;
            int count = 0;
            foreach (var o in objects)
                if (o.Column >= startCol && o.Column <= endCol) count++;
            return count / (float)(durationMs / 1000.0);
        }

        public void Reset()
        {
            ActiveZone = SpaceMixZone.DP_Left;
            beatsInCurrentPanel = 0;
            isLocked = false;
        }
    }
}
