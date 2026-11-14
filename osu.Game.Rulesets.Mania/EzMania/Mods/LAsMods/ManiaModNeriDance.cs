// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.EzOsuGame.Localization;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mania.Objects.Drawables;
using osu.Game.Rulesets.Mania.UI;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.Mania.EzMania.Mods.LAsMods
{
    public enum NeriDanceMode
    {
        [LocalisableDescription(typeof(NeriDanceStrings), nameof(NeriDanceStrings.MODE_POSITION))]
        PositionBased,

        [LocalisableDescription(typeof(NeriDanceStrings), nameof(NeriDanceStrings.MODE_TIME))]
        TimeBased,
    }

    /// <summary>
    /// Mania Dance — note 在下落的同时沿 X 轴左右摆动。
    /// 位置模式按 note 在轨道中的高度取相位（可跟随 BPM、按列错相）；
    /// 时间模式按 note 自己的可见时间做正弦摆动。
    /// </summary>
    public class ManiaModNeriDance : ModWithVisibilityAdjustment, IUpdatableByPlayfield, IApplicableToDrawableRuleset<ManiaHitObject>
    {
        public override string Name => "Dance";
        public override string Acronym => "ND";
        public override IconUsage? Icon => null;
        public override ModType Type => ModType.NeriMod;
        public override LocalisableString Description => NeriDanceStrings.DESCRIPTION;
        public override bool Ranked => false;
        public override bool ValidForMultiplayer => true;
        public override bool ValidForFreestyleAsRequiredMod => false;

        /// <summary>Mania 默认可视时间跨度（毫秒），时间模式下 note 约在判定前 1500ms 出现。</summary>
        private const double default_visible_duration = 1500;

        /// <summary>时间模式的平滑动画步长（毫秒）。</summary>
        private const double step_duration = 20;

        private const float hit_target_y = 110;

        [SettingSource(typeof(NeriDanceStrings), nameof(NeriDanceStrings.MODE_LABEL), nameof(NeriDanceStrings.MODE_DESCRIPTION), 0)]
        public Bindable<NeriDanceMode> Mode { get; } = new Bindable<NeriDanceMode>(NeriDanceMode.PositionBased);

        [SettingSource(typeof(NeriDanceStrings), nameof(NeriDanceStrings.AMPLITUDE_LABEL), nameof(NeriDanceStrings.AMPLITUDE_DESCRIPTION), 1)]
        public BindableFloat Amplitude { get; } = new BindableFloat(15)
        {
            MinValue = 0f,
            MaxValue = 60f,
            Precision = 1f,
        };

        [SettingSource(typeof(NeriDanceStrings), nameof(NeriDanceStrings.FREQUENCY_LABEL), nameof(NeriDanceStrings.FREQUENCY_DESCRIPTION), 2)]
        public BindableFloat Frequency { get; } = new BindableFloat(2)
        {
            MinValue = 0.5f,
            MaxValue = 8f,
            Precision = 0.1f,
        };

        [SettingSource(typeof(NeriDanceStrings), nameof(NeriDanceStrings.SYNC_LABEL), nameof(NeriDanceStrings.SYNC_DESCRIPTION), 3)]
        public BindableBool SyncToBPM { get; } = new BindableBool(false);

        [SettingSource(typeof(NeriDanceStrings), nameof(NeriDanceStrings.BEATS_LABEL), nameof(NeriDanceStrings.BEATS_DESCRIPTION), 4)]
        public BindableFloat BeatsPerCycle { get; } = new BindableFloat(2)
        {
            MinValue = 0.5f,
            MaxValue = 8f,
            Precision = 0.5f,
        };

        [SettingSource(typeof(NeriDanceStrings), nameof(NeriDanceStrings.PHASE_LABEL), nameof(NeriDanceStrings.PHASE_DESCRIPTION), 5)]
        public BindableFloat PhaseShift { get; } = new BindableFloat(0.5f)
        {
            MinValue = 0f,
            MaxValue = 2f,
            Precision = 0.1f,
        };

        private ManiaPlayfield? maniaPlayfield;
        private IBeatmap? beatmap;

        public void ApplyToDrawableRuleset(DrawableRuleset<ManiaHitObject> drawableRuleset)
        {
            maniaPlayfield = (ManiaPlayfield)drawableRuleset.Playfield;
            beatmap = drawableRuleset.Beatmap;
        }

        public void Update(Playfield playfield)
        {
            if (Mode.Value != NeriDanceMode.PositionBased)
                return;

            float amplitude = Amplitude.Value;

            if (amplitude <= 0 || maniaPlayfield == null)
                return;

            float phaseShift = PhaseShift.Value;

            float frequency;

            if (SyncToBPM.Value && beatmap != null)
            {
                double currentTime = playfield.Time.Current;
                double bpm = 60000.0 / beatmap.ControlPointInfo.TimingPointAt(currentTime).BeatLength;
                frequency = (float)(bpm / (60.0 * BeatsPerCycle.Value));
            }
            else
            {
                frequency = Frequency.Value;
            }

            foreach (var stage in maniaPlayfield.Stages)
            {
                foreach (var column in stage.Columns)
                {
                    float colHeight = column.DrawHeight;
                    if (colHeight <= 0) continue;

                    float colPhase = column.Index * phaseShift * MathF.PI;

                    foreach (var entry in column.HitObjectContainer.AliveEntries)
                    {
                        var drawable = entry.Value;

                        if (drawable is DrawableHoldNote)
                            continue;

                        // 0 = 判定线，1 = note 刚出现的顶部
                        float yNorm = Math.Clamp((hit_target_y - drawable.Y) / colHeight, 0f, 1f);

                        drawable.X = amplitude * MathF.Sin(2 * MathF.PI * frequency * yNorm + colPhase);
                    }
                }
            }
        }

        protected override void ApplyIncreasedVisibilityState(DrawableHitObject hitObject, ArmedState state)
            => applyTimeBasedOscillation(hitObject);

        protected override void ApplyNormalVisibilityState(DrawableHitObject hitObject, ArmedState state)
            => applyTimeBasedOscillation(hitObject);

        private void applyTimeBasedOscillation(DrawableHitObject drawable)
        {
            if (Mode.Value != NeriDanceMode.TimeBased)
                return;

            // HoldNote 容器由子物件托管，Body 填充在头尾之间，二者都不应独立移动。
            if (drawable is DrawableHoldNote or DrawableHoldNoteBody)
                return;

            float amplitude = Amplitude.Value;

            if (amplitude <= 0)
                return;

            float frequency = Frequency.Value;
            double visibleStartTime = drawable.HitObject.StartTime - default_visible_duration;
            int numSteps = (int)(default_visible_duration / step_duration);

            for (int i = 0; i <= numSteps; i++)
            {
                double t = visibleStartTime + i * step_duration;
                double phase = 2 * Math.PI * frequency * (t - visibleStartTime) / 1000.0;
                float offset = (float)(amplitude * Math.Sin(phase));

                using (drawable.BeginAbsoluteSequence(t))
                    drawable.MoveToX(offset, step_duration);
            }
        }
    }

    public static class NeriDanceStrings
    {
        public static readonly LocalisableString DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "音符边下落边左右摆动。", "Notes dance side to side!");

        public static readonly LocalisableString MODE_LABEL = new EzLocalizationManager.EzLocalisableString(
            "模式", "Mode");

        public static readonly LocalisableString MODE_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "摆动的驱动方式。", "How the oscillation is driven.");

        public static readonly LocalisableString MODE_POSITION = new EzLocalizationManager.EzLocalisableString(
            "按位置", "Position based");

        public static readonly LocalisableString MODE_TIME = new EzLocalizationManager.EzLocalisableString(
            "按时间", "Time based");

        public static readonly LocalisableString AMPLITUDE_LABEL = new EzLocalizationManager.EzLocalisableString(
            "振幅", "Amplitude");

        public static readonly LocalisableString AMPLITUDE_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "最大横向偏移（像素）。", "Max horizontal offset (pixels).");

        public static readonly LocalisableString FREQUENCY_LABEL = new EzLocalizationManager.EzLocalisableString(
            "频率", "Frequency");

        public static readonly LocalisableString FREQUENCY_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "时间模式下为每秒摆动次数，位置模式下为可视高度内的波数。",
            "Oscillations per second (time mode) or across the visible height (position mode).");

        public static readonly LocalisableString SYNC_LABEL = new EzLocalizationManager.EzLocalisableString(
            "跟随 BPM", "Sync to BPM");

        public static readonly LocalisableString SYNC_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "摆动速度跟随谱面 BPM（位置模式）。", "Sync oscillation speed to beatmap BPM (position mode).");

        public static readonly LocalisableString BEATS_LABEL = new EzLocalizationManager.EzLocalisableString(
            "每周期拍数", "Beats per Cycle");

        public static readonly LocalisableString BEATS_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "一次完整摆动占多少拍。", "How many beats per full oscillation.");

        public static readonly LocalisableString PHASE_LABEL = new EzLocalizationManager.EzLocalisableString(
            "相位偏移", "Phase Shift");

        public static readonly LocalisableString PHASE_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "每列的相位差（0 = 同相，1 = 级联）。", "Phase offset per column (0=in phase, 1=cascade).");
    }
}
