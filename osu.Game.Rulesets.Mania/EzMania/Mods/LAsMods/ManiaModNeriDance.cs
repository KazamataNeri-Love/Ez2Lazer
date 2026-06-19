// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mania.Objects.Drawables;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Drawables;

namespace osu.Game.Rulesets.Mania.EzMania.Mods.LAsMods
{
    /// <summary>
    /// Mania Dance — Note 在垂直下落的同时，沿 X 轴做正弦波震荡，
    /// 轨迹如同左右摇摆的波浪。
    /// </summary>
    public class ManiaModNeriDance : ModWithVisibilityAdjustment
    {
        public override string Name => "Dance";
        public override string Acronym => "ND";
        public override IconUsage? Icon => null;
        public override ModType Type => ModType.NeriMod;
        public override LocalisableString Description => "Notes dance side to side in a sine wave!";
        public override double ScoreMultiplier => 1;
        public override bool Ranked => false;
        public override bool ValidForMultiplayer => true;
        public override bool ValidForFreestyleAsRequiredMod => false;

        /// <summary>
        /// Mania 默认可视时间跨度（毫秒）。Note 在 hit 前约 1500ms 出现。
        /// </summary>
        private const double default_visible_duration = 1500;

        /// <summary>
        /// 平滑动画的时间步长（毫秒）。越小越平滑，但会创建更多 Transform。
        /// </summary>
        private const double step_duration = 20;

        [SettingSource("Amplitude", "How far notes oscillate sideways (in pixels).", 1)]
        public BindableFloat Amplitude { get; } = new BindableFloat(15)
        {
            MinValue = 0f,
            MaxValue = 50f,
            Precision = 1f,
        };

        [SettingSource("Frequency", "How many oscillations per second.", 2)]
        public BindableFloat Frequency { get; } = new BindableFloat(2)
        {
            MinValue = 0.5f,
            MaxValue = 6f,
            Precision = 0.1f,
        };

        protected override void ApplyIncreasedVisibilityState(DrawableHitObject hitObject, ArmedState state)
            => applyOscillation(hitObject, state);

        protected override void ApplyNormalVisibilityState(DrawableHitObject hitObject, ArmedState state)
            => applyOscillation(hitObject, state);

        private void applyOscillation(DrawableHitObject drawable, ArmedState state)
        {
            // 跳过 HoldNote 容器和 Body — 容器由子物件托管，Body 填充在 Head/Tail 之间不应独立移动
            if (drawable is DrawableHoldNote or DrawableHoldNoteBody)
                return;

            var maniaObject = (ManiaHitObject)drawable.HitObject;

            double visibleStartTime = maniaObject.StartTime - default_visible_duration;
            double visibleDuration = default_visible_duration;

            float amplitude = Amplitude.Value;
            float frequency = Frequency.Value;

            // 振幅为 0 则无需震荡
            if (amplitude <= 0)
                return;

            int numSteps = (int)(visibleDuration / step_duration);

            for (int i = 0; i <= numSteps; i++)
            {
                double t = visibleStartTime + i * step_duration;
                double elapsed = t - visibleStartTime; // 从可见开始的毫秒数
                double phase = 2 * Math.PI * frequency * elapsed / 1000.0;
                float offset = (float)(amplitude * Math.Sin(phase));

                using (drawable.BeginAbsoluteSequence(t))
                    drawable.MoveToX(offset, step_duration);
            }
        }
    }
}
