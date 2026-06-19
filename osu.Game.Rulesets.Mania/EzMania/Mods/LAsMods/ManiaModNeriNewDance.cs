// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mania.Objects.Drawables;
using osu.Game.Rulesets.Mania.UI;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.Mania.EzMania.Mods.LAsMods
{
    public class ManiaModNeriNewDance : Mod, IUpdatableByPlayfield, IApplicableToDrawableRuleset<ManiaHitObject>
    {
        public override string Name => "New Dance";
        public override string Acronym => "ND2";
        public override IconUsage? Icon => null;
        public override ModType Type => ModType.NeriMod;
        public override LocalisableString Description => "Notes dance side to side based on position!";
        public override double ScoreMultiplier => 1;
        public override bool Ranked => false;
        public override bool ValidForMultiplayer => true;
        public override bool ValidForFreestyleAsRequiredMod => false;

        private const float hit_target_y = 110;

        [SettingSource("Amplitude", "Max horizontal offset (pixels).", 1)]
        public BindableFloat Amplitude { get; } = new BindableFloat(15)
        {
            MinValue = 0f,
            MaxValue = 60f,
            Precision = 1f,
        };

        [SettingSource("Frequency", "Number of sine waves across visible height (free mode).", 2)]
        public BindableFloat Frequency { get; } = new BindableFloat(2)
        {
            MinValue = 0.5f,
            MaxValue = 8f,
            Precision = 0.1f,
        };

        [SettingSource("Sync to BPM", "Sync oscillation speed to beatmap BPM.", 3)]
        public BindableBool SyncToBPM { get; } = new BindableBool(false);

        [SettingSource("Beats per Cycle", "How many beats per full oscillation.", 4)]
        public BindableFloat BeatsPerCycle { get; } = new BindableFloat(2)
        {
            MinValue = 0.5f,
            MaxValue = 8f,
            Precision = 0.5f,
        };

        [SettingSource("Phase Shift", "Phase offset per column (0=in phase, 1=cascade).", 5)]
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
            float amplitude = Amplitude.Value;
            float phaseShift = PhaseShift.Value;

            if (amplitude <= 0 || maniaPlayfield == null)
                return;

            // 计算有效频率：BPM 模式或自由模式
            float frequency;
            if (SyncToBPM.Value && beatmap != null)
            {
                double currentTime = playfield.Time.Current;
                var timingPoint = beatmap.ControlPointInfo.TimingPointAt(currentTime);
                double bpm = 60000.0 / timingPoint.BeatLength;
                // frequency = BPM / (60 * beatsPerCycle)
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

                        // Y_normalized: 0 = 判定线底部, 1 = Note 刚出现顶部
                        float yNorm = Math.Clamp((hit_target_y - drawable.Y) / colHeight, 0f, 1f);

                        float offset = amplitude * MathF.Sin(2 * MathF.PI * frequency * yNorm + colPhase);

                        drawable.X = offset;
                    }
                }
            }
        }
    }
}
