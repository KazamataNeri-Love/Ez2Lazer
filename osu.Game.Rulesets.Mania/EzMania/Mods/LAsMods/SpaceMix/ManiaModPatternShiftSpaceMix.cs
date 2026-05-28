// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Framework.Utils;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Configuration;
using osu.Game.EzOsuGame.Localization;
using osu.Game.Overlays.Settings;
using osu.Game.Rulesets.Mania.Beatmaps;
using osu.Game.Rulesets.Mania.EzMania.Mods.ModHelp;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Mania.EzMania.Mods.LAsMods
{
    /// <summary>
    /// PS SpaceMix — 完全复用 <see cref="ManiaModPatternShift"/> 的谱面重构逻辑，
    /// 仅做以下两点修改：
    ///   1. 始终转换至 14K（SpaceMix 专用列数）
    ///   2. 列分配算法采用 SpaceMix 三面板区域感知版本
    /// </summary>
    public class ManiaModPatternShiftSpaceMix : Mod, IApplicableAfterBeatmapConversion, IApplicableToBeatmapConverter, IHasSeed, IEzApplyOrder
    {
        private const double min_column_spacing_ms = 8;
        private const int TOTAL_COLUMNS = 14;
        private const int DP_LEFT_START = 0;
        private const int DP_LEFT_END = 4;
        private const int EFFECT_START = 5;
        private const int EFFECT_END = 8;
        private const int DP_RIGHT_START = 9;
        private const int DP_RIGHT_END = 13;

        public override string Name => "PS SpaceMix";
        public override string Acronym => "PSM";
        public override double ScoreMultiplier => 1;
        public override LocalisableString Description => SpaceMixStrings.SPACEMIX_MAIN_DESCRIPTION;
        public override IconUsage? Icon => FontAwesome.Solid.Magic;
        public override ModType Type => ModType.NeriMod;
        public override bool Ranked => false;
        public override bool ValidForMultiplayer => true;
        public override bool ValidForFreestyleAsRequiredMod => false;

        // ── 参数：与 ManiaModPatternShift 完全一致，但无 KeyCount（始终14K）──

        [SettingSource(typeof(SpaceMixStrings), nameof(SpaceMixStrings.SPACEMIX_DENSITY_LABEL), nameof(SpaceMixStrings.SPACEMIX_DENSITY_DESCRIPTION))]
        public BindableNumber<int> Density { get; } = new BindableInt(7)
        {
            MinValue = 1,
            MaxValue = 10,
            Precision = 1
        };

        [SettingSource(typeof(SpaceMixStrings), nameof(SpaceMixStrings.SPACEMIX_MAX_CHORD_LABEL), nameof(SpaceMixStrings.SPACEMIX_MAX_CHORD_DESCRIPTION))]
        public BindableNumber<int> MaxChord { get; } = new BindableInt(5)
        {
            MinValue = 1,
            MaxValue = TOTAL_COLUMNS,
            Precision = 1
        };

        [SettingSource(typeof(SpaceMixStrings), nameof(SpaceMixStrings.SPACEMIX_ALIGN_LABEL), nameof(SpaceMixStrings.SPACEMIX_ALIGN_DESCRIPTION))]
        public BindableNumber<int> AlignDivisor { get; } = new BindableInt
        {
            MinValue = 0,
            MaxValue = 16,
            Precision = 1
        };

        [SettingSource(typeof(SpaceMixStrings), nameof(SpaceMixStrings.SPACEMIX_DELAY_LABEL), nameof(SpaceMixStrings.SPACEMIX_DELAY_DESCRIPTION))]
        public BindableNumber<int> DelayLevel { get; } = new BindableInt
        {
            MinValue = 0,
            MaxValue = 10,
            Precision = 1
        };

        [SettingSource(typeof(SpaceMixStrings), nameof(SpaceMixStrings.SPACEMIX_REGENERATE_LABEL), nameof(SpaceMixStrings.SPACEMIX_REGENERATE_DESCRIPTION))]
        public BindableBool Regenerate { get; } = new BindableBool();

        [SettingSource(typeof(SpaceMixStrings), nameof(SpaceMixStrings.SPACEMIX_REGENERATE_DIFFICULTY_LABEL), nameof(SpaceMixStrings.SPACEMIX_REGENERATE_DIFFICULTY_DESCRIPTION))]
        public BindableNumber<int> RegenerateDifficulty { get; } = new BindableInt(5)
        {
            MinValue = 2,
            MaxValue = 10,
            Precision = 1
        };

        // ── 面板切换参数 ──

        [SettingSource(typeof(SpaceMixStrings), nameof(SpaceMixStrings.SPACEMIX_WAVEFORM_LABEL), nameof(SpaceMixStrings.SPACEMIX_WAVEFORM_DESCRIPTION))]
        public Bindable<EzOscillator.EzWaveform> Waveform { get; } = new Bindable<EzOscillator.EzWaveform>(EzOscillator.EzWaveform.Sine);

        [SettingSource(typeof(SpaceMixStrings), nameof(SpaceMixStrings.SPACEMIX_PANEL_OSCILLATION_LABEL), nameof(SpaceMixStrings.SPACEMIX_PANEL_OSCILLATION_DESCRIPTION))]
        public BindableNumber<int> PanelOscillationBeats { get; } = new BindableInt(4)
        {
            MinValue = 1,
            MaxValue = 16,
            Precision = 1
        };

        [SettingSource(typeof(EzCommonModStrings), nameof(EzCommonModStrings.SEED_LABEL), nameof(EzCommonModStrings.SEED_DESCRIPTION), SettingControlType = typeof(SettingsNumberBox))]
        public Bindable<int?> Seed { get; } = new Bindable<int?>();

        [SettingSource(typeof(EzCommonModStrings), nameof(EzCommonModStrings.APPLY_ORDER_LABEL), nameof(EzCommonModStrings.APPLY_ORDER_DESCRIPTION))]
        public BindableNumber<int> ApplyOrderIndex { get; } = new BindableInt(60)
        {
            MinValue = 0,
            MaxValue = 100
        };

        public int ApplyOrder => ApplyOrderIndex.Value;

        public override IEnumerable<(LocalisableString setting, LocalisableString value)> SettingDescription
        {
            get
            {
                yield return (SpaceMixStrings.SPACEMIX_DENSITY_LABEL, $"{Density.Value}");
                yield return (SpaceMixStrings.SPACEMIX_MAX_CHORD_LABEL, $"{MaxChord.Value}");
                yield return (SpaceMixStrings.SPACEMIX_ALIGN_LABEL, AlignDivisor.Value == 0 ? "Off" : $"1/{AlignDivisor.Value}");
                yield return (SpaceMixStrings.SPACEMIX_DELAY_LABEL, $"{DelayLevel.Value}");
                yield return (SpaceMixStrings.SPACEMIX_REGENERATE_LABEL, Regenerate.Value ? "On" : "Off");
                yield return (SpaceMixStrings.SPACEMIX_REGENERATE_DIFFICULTY_LABEL, $"{RegenerateDifficulty.Value}");
                yield return (SpaceMixStrings.SPACEMIX_WAVEFORM_LABEL, Waveform.Value.ToString());
                yield return (SpaceMixStrings.SPACEMIX_PANEL_OSCILLATION_LABEL, $"{PanelOscillationBeats.Value} beat(s)");
                yield return (EzCommonModStrings.SEED_LABEL, Seed.Value?.ToString() ?? "Random");
                yield return (EzCommonModStrings.APPLY_ORDER_LABEL, $"{ApplyOrderIndex.Value}");
            }
        }

        // ── 谱面转换器：固定 14K ──

        public void ApplyToBeatmapConverter(IBeatmapConverter converter)
        {
            if (converter is ManiaBeatmapConverter maniaConverter)
                maniaConverter.TargetColumns = TOTAL_COLUMNS;
        }

        // ── ApplyToBeatmap：完全复用 ManiaModPatternShift 的 applyToBeatmapInternal ──

        public void ApplyToBeatmap(IBeatmap beatmap)
        {
            applyToBeatmapInternal((ManiaBeatmap)beatmap);
        }

        private void applyToBeatmapInternal(ManiaBeatmap maniaBeatmap)
        {
            Seed.Value ??= RNG.Next();
            var rng = new Random(Seed.Value.Value);

            int maxChord = Math.Clamp(MaxChord.Value, 1, TOTAL_COLUMNS);
            int difficulty = Math.Clamp(Density.Value, 1, 10);

            if (maniaBeatmap.HitObjects.Count == 0 && !Regenerate.Value)
                return;

            maniaBeatmap.Stages.Clear();
            maniaBeatmap.Stages.Add(new StageDefinition(TOTAL_COLUMNS));
            maniaBeatmap.Difficulty.CircleSize = TOTAL_COLUMNS;

            double snap(double time)
            {
                if (AlignDivisor.Value <= 0)
                    return time;
                return maniaBeatmap.ControlPointInfo.GetClosestSnappedTime(time, AlignDivisor.Value);
            }

            var notes = maniaBeatmap.HitObjects.Select(h =>
            {
                if (h is HoldNote hold)
                {
                    double start = snap(hold.StartTime);
                    double end = snap(hold.EndTime);
                    if (end < start)
                        end = start;
                    return new PatternShiftNote(start, end, hold.GetNodeSamples(0), hold.Column, end > start);
                }

                double time = snap(h.StartTime);
                return new PatternShiftNote(time, time, h.Samples, h.Column, false);
            }).OrderBy(n => n.StartTime).ThenBy(n => n.SourceColumn).ToList();

            if (Regenerate.Value)
                notes = modifyNotesByDifficulty(notes, maniaBeatmap, TOTAL_COLUMNS, RegenerateDifficulty.Value, rng);

            var chords = buildChords(notes);
            applyDelay(chords, maniaBeatmap.ControlPointInfo, DelayLevel.Value, rng);
            reduceAllChords(chords, maxChord, difficulty);

            // ★ SpaceMix 专用列分配（带振荡器驱动）
            var panelOsc = new EzOscillator(Seed.Value.Value, frequency: 1.0, ezWaveform: Waveform.Value);
            assignColumnsSpaceMix(chords, rng, panelOsc, maniaBeatmap, PanelOscillationBeats.Value);

            var newObjects = new List<ManiaHitObject>(notes.Count);
            foreach (var chord in chords)
            {
                foreach (var note in chord.Notes)
                {
                    if (note.IsHold && note.EndTime > note.StartTime)
                    {
                        newObjects.Add(new HoldNote
                        {
                            Column = note.AssignedColumn,
                            StartTime = note.StartTime,
                            Duration = note.EndTime - note.StartTime,
                            NodeSamples = new List<IList<HitSampleInfo>> { note.Samples, Array.Empty<HitSampleInfo>() }
                        });
                    }
                    else
                    {
                        newObjects.Add(new Note
                        {
                            Column = note.AssignedColumn,
                            StartTime = note.StartTime,
                            Samples = note.Samples
                        });
                    }
                }
            }

            maniaBeatmap.HitObjects = newObjects.OrderBy(h => h.StartTime).ThenBy(h => h.Column).ToList();
            ManiaNoteCleanupTool.EnforceHoldReleaseGap(maniaBeatmap);
        }

        // ──── 以下方法完全从 ManiaModPatternShift 复制 ────

        private static List<PatternShiftChord> buildChords(List<PatternShiftNote> notes)
        {
            var chords = new List<PatternShiftChord>();
            PatternShiftChord? current = null;

            foreach (var note in notes)
            {
                if (current == null || note.StartTime != current.Time)
                {
                    current = new PatternShiftChord(note.StartTime);
                    chords.Add(current);
                }
                current.Notes.Add(note);
            }

            return chords;
        }

        private static void reduceAllChords(List<PatternShiftChord> chordList, int maxChord, int difficulty)
        {
            int[] chordTimeLimits = { 200, 100, 50, 25, 12 };
            int[] chordNeighborLimits = { 1, 1, 1, 1, 1 };

            for (int i = 0; i < chordTimeLimits.Length; i++)
                chordTimeLimits[i] = chordTimeLimits[i] * 100 / difficulty / difficulty;

            foreach (var chord in chordList)
                reduceChordSize(chord, maxChord);

            for (int i = 1; i < chordList.Count - 1; i++)
            {
                for (int j = 0; j < chordTimeLimits.Length; j++)
                {
                    double spacing = chordList[i + 1].Time - chordList[i - 1].Time;
                    int neighborSize = chordList[i - 1].Notes.Count + chordList[i + 1].Notes.Count;

                    if (spacing < chordTimeLimits[j] && neighborSize > chordNeighborLimits[j])
                        reduceChordSize(chordList[i], Math.Min(maxChord, 5 - j));
                }
            }
        }

        private static void reduceChordSize(PatternShiftChord chord, int newSize)
        {
            if (chord.Notes.Count <= newSize)
                return;
            chord.Notes = chord.Notes.OrderBy(n => n.SourceColumn).ToList();
            while (chord.Notes.Count > newSize)
                chord.Notes.RemoveAt(0);
        }

        private static void applyDelay(List<PatternShiftChord> chords, ControlPointInfo controlPoints, int delayLevel, Random rng)
        {
            if (delayLevel <= 0)
                return;

            foreach (var chord in chords)
            {
                int noteCount = chord.Notes.Count;
                int maxShift = ManiaKeyPatternHelp.GetDelayMaxShiftCount(delayLevel, noteCount);
                if (maxShift <= 0)
                    continue;

                double beatLength = controlPoints.TimingPointAt(chord.Time).BeatLength;
                double offsetAmount = beatLength * ManiaKeyPatternHelp.GetDelayBeatFraction(delayLevel);

                var indexes = Enumerable.Range(0, noteCount).OrderBy(_ => rng.Next()).Take(maxShift).ToList();

                foreach (int index in indexes)
                {
                    var note = chord.Notes[index];
                    double direction = rng.NextDouble() < 0.5 ? -1 : 1;
                    double offset = direction * offsetAmount;
                    note.StartTime = Math.Max(0, note.StartTime + offset);
                    note.EndTime = Math.Max(note.StartTime, note.EndTime + offset);
                }
            }
        }

        // ──── SpaceMix 和弦级区域列分配（振荡器驱动）────

        /// <summary>
        /// 用振荡器驱动面板切换，减少快速切换感。
        /// 振荡器周期 = PanelOscillationBeats 拍，在每半个周期切换一次面板。
        /// 波形正值 → "中右-左右-左中"；波形负值 → "中左-右左-右中"
        /// </summary>
        private static void assignColumnsSpaceMix(List<PatternShiftChord> chords, Random rng,
            EzOscillator osc, ManiaBeatmap beatmap, int oscillationBeats)
        {
            double[] lastColTime = new double[TOTAL_COLUMNS];
            var placedNotes = new List<PatternShiftNote>();
            for (int i = 0; i < TOTAL_COLUMNS; i++)
                lastColTime[i] = -1000;
            int lastNote = 0;

            double beatLength = beatmap.ControlPointInfo.TimingPointAt(chords[0].Time).BeatLength;
            if (beatLength <= 0) beatLength = 500;

            // 两个轮换模式
            // 模式A (波形≥0): 中右→左右→左中
            // 模式B (波形<0):  中左→右左→右中
            var rotationA = new[] { (EFFECT_START, DP_RIGHT_START), (DP_LEFT_START, DP_RIGHT_START), (DP_LEFT_START, EFFECT_START) };
            var rotationB = new[] { (DP_LEFT_START, EFFECT_START), (DP_RIGHT_START, DP_LEFT_START), (DP_RIGHT_START, EFFECT_START) };

            var zoneBounds = new Dictionary<int, (int, int)>
            {
                [DP_LEFT_START]  = (DP_LEFT_START, DP_LEFT_END),
                [EFFECT_START]   = (EFFECT_START, EFFECT_END),
                [DP_RIGHT_START] = (DP_RIGHT_START, DP_RIGHT_END)
            };

            double firstTime = chords[0].Time;
            int lastStepIndex = -1; // 上一次使用的 rotation step 索引

            foreach (var chord in chords)
            {
                chord.Notes = chord.Notes.OrderBy(n => n.SourceColumn).ToList();
                var usedColumns = new HashSet<int>();
                var assigned = new List<PatternShiftNote>();

                // ★ 基于拍数计算振荡位置，决定使用哪个步
                double beatPos = (chord.Time - firstTime) / beatLength;
                long oscIndex = (long)(beatPos / oscillationBeats);
                osc.Reset(unchecked((int)(oscIndex * 397)));
                double oscVal = osc.NextSigned(); // [-1, 1]

                // 在半周期处切换步（0→1→2→0...）
                double halfPhase = (beatPos % oscillationBeats) / oscillationBeats;
                int stepIndex;
                if (halfPhase < 1.0 / 3.0)
                    stepIndex = 0;
                else if (halfPhase < 2.0 / 3.0)
                    stepIndex = 1;
                else
                    stepIndex = 2;

                // 确保 step 变化时才切换，避免相邻和弦重复计算
                if (stepIndex != lastStepIndex)
                    lastStepIndex = stepIndex;

                // 根据波形选择模式
                var rotation = oscVal >= 0 ? rotationA : rotationB;
                var (cand0, cand1) = rotation[stepIndex];
                int chosen = rng.Next(2) == 0 ? cand0 : cand1;
                var (zoneStart, zoneEnd) = zoneBounds[chosen];

                foreach (var note in chord.Notes)
                {
                    int column = chooseColumnInZone(lastColTime, lastNote, rng, note.StartTime, min_column_spacing_ms, zoneStart, zoneEnd);
                    if (column < 0) continue;
                    if (usedColumns.Contains(column)) continue;
                    if (hasAssignedNoteAtTime(placedNotes, column, note.StartTime, min_column_spacing_ms)) continue;

                    note.AssignedColumn = column;
                    lastNote = column;
                    lastColTime[column] = note.IsHold ? note.EndTime : note.StartTime;
                    usedColumns.Add(column);
                    assigned.Add(note);
                    placedNotes.Add(note);
                }

                chord.Notes = assigned;
            }
        }

        /// <summary>根据原始列判断所属区域</summary>
        private static SpaceMixZoneDescriptor GetZoneForColumn(int column)
        {
            if (column <= 4) return SpaceMixLayout.DP_Left;
            if (column <= 8) return SpaceMixLayout.Effect;
            return SpaceMixLayout.DP_Right;
        }

        /// <summary>在指定区域内选择最优列</summary>
        private static int chooseColumnInZone(double[] lastUsedTime, int lastNote, Random rng,
            double currentTime, double minSpacingMs, int zoneStart, int zoneEnd)
        {
            var candidates = new List<int>();
            double safeTime = currentTime - Math.Max(0, minSpacingMs);

            for (int i = zoneStart; i <= zoneEnd; i++)
            {
                if (lastUsedTime[i] <= safeTime)
                    candidates.Add(i);
            }

            if (candidates.Count == 0)
            {
                for (int i = zoneStart; i <= zoneEnd; i++)
                {
                    if (lastUsedTime[i] <= currentTime)
                        candidates.Add(i);
                }
            }

            if (candidates.Count == 0)
                return -1;

            // 选最久未使用的列
            double minTime = double.MaxValue;
            var minList = new List<int>();

            for (int i = 0; i < candidates.Count; i++)
            {
                int idx = candidates[i];
                if (lastUsedTime[idx] < minTime)
                {
                    minList.Clear();
                    minTime = lastUsedTime[idx];
                    minList.Add(idx);
                }
                else if (lastUsedTime[idx] <= minTime + 24 && lastUsedTime[idx] >= minTime - 24)
                {
                    minList.Add(idx);
                }
            }

            // 左右均衡：若 minList 同时含左右两侧，选与上一个异侧的
            int midPoint = (zoneStart + zoneEnd) / 2;
            int noteLeft = minList.Count(i => i <= midPoint);
            int noteRight = minList.Count(i => i > midPoint);

            if (noteRight > 0 && noteLeft > 0)
            {
                bool lastOnLeft = lastNote <= midPoint;
                minList = minList.Where(i => lastOnLeft ? i > midPoint : i <= midPoint).ToList();
            }

            return minList.Count > 0 ? minList[rng.Next(minList.Count)] : -1;
        }

        private static bool hasAssignedNoteAtTime(List<PatternShiftNote> notes, int column, double time, double tolerance = 0.5)
        {
            for (int i = 0; i < notes.Count; i++)
            {
                var note = notes[i];
                if (note.AssignedColumn != column)
                    continue;
                if (!note.IsHold && Math.Abs(note.StartTime - time) <= tolerance)
                    return true;
            }
            return false;
        }

        // ──── Regenerate 难度调整（从 ManiaModPatternShift 复制）────

        private List<PatternShiftNote> modifyNotesByDifficulty(List<PatternShiftNote> originalNotes, ManiaBeatmap beatmap, int targetColumns, int stars, Random rng)
        {
            var notes = new List<PatternShiftNote>(originalNotes);
            const int center = 5;
            int delta = stars - center;

            int oscSeed = Seed.Value ?? RNG.Next();
            var osc = new EzOscillator(oscSeed);

            if (notes.Count == 0)
            {
                if (delta <= 0) return notes;
                var tp0 = beatmap.ControlPointInfo.TimingPoints.FirstOrDefault();
                double beatLen0 = tp0?.BeatLength ?? 500;
                int initialAdd = Math.Min(8, Math.Max(1, (int)Math.Round(3 * (delta / (double)center))));
                var seedList = new List<PatternShiftNote>();
                for (int i = 0; i < initialAdd; i++)
                {
                    double t = i * beatLen0 * (1.0 + 0.25 * osc.Next());
                    int col = rng.Next(0, Math.Max(1, targetColumns));
                    seedList.Add(new PatternShiftNote(Math.Max(0, t), Math.Max(0, t), Array.Empty<HitSampleInfo>(), col, false));
                }
                return seedList.OrderBy(n => n.StartTime).ThenBy(n => n.SourceColumn).ToList();
            }

            if (delta < 0)
            {
                double removalRatio = Math.Min(0.95, -delta / (double)(center - 1));
                int targetRemove = (int)Math.Round(notes.Count * removalRatio);
                var remaining = new List<PatternShiftNote>();
                int removed = 0;
                for (int i = 0; i < notes.Count; i++)
                {
                    double oscVal = osc.Next();
                    double p = removalRatio * (0.6 + 0.8 * (1.0 - oscVal));
                    if (removed < targetRemove && rng.NextDouble() < p) { removed++; continue; }
                    remaining.Add(notes[i]);
                }
                while (removed < targetRemove && remaining.Count > 0) { remaining.RemoveAt(remaining.Count - 1); removed++; }
                return remaining.OrderBy(n => n.StartTime).ThenBy(n => n.SourceColumn).ToList();
            }

            if (delta > 0)
            {
                double insertRatio = Math.Min(2.0, delta / (double)center);
                int targetAdd = (int)Math.Round(notes.Count * insertRatio * 0.25);
                var result = new List<PatternShiftNote>(notes);
                for (int i = 0; i < targetAdd; i++)
                {
                    int idx = rng.Next(0, notes.Count);
                    var anchor = notes[idx];
                    var tp = beatmap.ControlPointInfo.TimingPointAt(anchor.StartTime);
                    double beatLength = tp.BeatLength;
                    int[] allowedSubdiv = { 2, 4, 8, 16 };
                    int subdiv = allowedSubdiv[rng.Next(allowedSubdiv.Length)];
                    double offset = (rng.NextDouble() - 0.5) * (beatLength / subdiv) * (1.0 + 0.5 * (1.0 - osc.Next()));
                    double newTime = Math.Max(0, anchor.StartTime + offset);
                    int col = rng.Next(0, Math.Max(1, targetColumns));
                    bool exists = result.Any(n => n.SourceColumn == col && Math.Abs(n.StartTime - newTime) <= 48);
                    if (exists)
                    {
                        for (int t = 0; t < 6 && exists; t++) { col = (col + 1) % targetColumns; exists = result.Any(n => n.SourceColumn == col && Math.Abs(n.StartTime - newTime) <= 48); }
                        if (exists) continue;
                    }
                    result.Add(new PatternShiftNote(newTime, newTime, Array.Empty<HitSampleInfo>(), col, false));
                }
                // Gap filling
                int remainingGapAdds = targetAdd;
                for (int i = 0; i < notes.Count - 1 && remainingGapAdds > 0; i++)
                {
                    double left = notes[i].StartTime;
                    double right = notes[i + 1].StartTime;
                    double gap = right - left;
                    var tpLeft = beatmap.ControlPointInfo.TimingPointAt(left);
                    double localBeat = tpLeft.BeatLength;
                    if (gap > Math.Max(300, localBeat * 1.5))
                    {
                        int inserts = Math.Min(2, Math.Max(1, (int)Math.Floor(gap / (localBeat * 2.0))));
                        for (int j = 0; j < inserts && remainingGapAdds > 0; j++)
                        {
                            double t = left + (osc.Next() * 0.75 + 0.125) * gap;
                            int[] subdivs = { 2, 4, 8 };
                            int sub = subdivs[rng.Next(subdivs.Length)];
                            double step = localBeat / sub;
                            double snapped = Math.Round(t / step) * step;
                            int col = rng.Next(0, Math.Max(1, targetColumns));
                            bool ex = result.Any(n => n.SourceColumn == col && Math.Abs(n.StartTime - snapped) <= 48);
                            if (ex)
                            {
                                for (int c = 0; c < Math.Min(6, targetColumns) && ex; c++) { col = (col + 1) % targetColumns; ex = result.Any(n => n.SourceColumn == col && Math.Abs(n.StartTime - snapped) <= 48); }
                                if (ex) continue;
                            }
                            result.Add(new PatternShiftNote(Math.Max(0, snapped), Math.Max(0, snapped), Array.Empty<HitSampleInfo>(), col, false));
                            remainingGapAdds--;
                        }
                    }
                }
                return result.OrderBy(n => n.StartTime).ThenBy(n => n.SourceColumn).ToList();
            }

            return notes;
        }

        // ──── 内部类型（从 ManiaModPatternShift 复制）────

        private class PatternShiftNote
        {
            public IList<HitSampleInfo> Samples { get; }
            public int SourceColumn { get; }
            public bool IsHold { get; }
            public double StartTime { get; set; }
            public double EndTime { get; set; }
            public int AssignedColumn { get; set; }

            public PatternShiftNote(double startTime, double endTime, IList<HitSampleInfo> samples, int sourceColumn, bool isHold)
            {
                StartTime = startTime;
                EndTime = endTime;
                Samples = samples;
                SourceColumn = sourceColumn;
                IsHold = isHold;
            }
        }

        private class PatternShiftChord
        {
            public double Time { get; }
            public List<PatternShiftNote> Notes { get; set; } = new List<PatternShiftNote>();

            public PatternShiftChord(double time)
            {
                Time = time;
            }
        }
    }
}
