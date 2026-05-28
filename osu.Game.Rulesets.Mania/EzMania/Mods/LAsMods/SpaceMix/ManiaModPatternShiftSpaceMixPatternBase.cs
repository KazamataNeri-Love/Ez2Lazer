// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
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
    /// PS SpaceMix 子模组抽象基类。在 <see cref="ManiaModPatternShiftPatternBase"/> 的基础上
    /// 集成 SpaceMix 面板切换策略，根据当前窗口的 KPS 分布自动选择主导面板区域，
    /// 然后将选定区域内的 HitObject 传递给子类的 ApplyPattern 方法。
    /// </summary>
    public abstract class ManiaModPatternShiftSpaceMixPatternBase : Mod, IApplicableAfterBeatmapConversion, IHasSeed, IEzApplyOrder
    {
        protected const double TIME_TOLERANCE = 3;
        protected const int TOTAL_COLUMNS = 14;

        protected abstract KeyPatternType PatternType { get; }
        protected abstract string PatternName { get; }
        protected abstract string PatternAcronym { get; }

        protected abstract void ApplyPatternForWindow(List<ManiaHitObject> windowObjects,
                                                      ManiaBeatmap beatmap,
                                                      double windowStart,
                                                      double windowEnd,
                                                      KeyPatternSettings settings,
                                                      Random rng,
                                                      int maxIterationsPerWindow);

        protected virtual EzOscillator.EzWaveform DefaultWaveform => EzOscillator.EzWaveform.Sine;
        protected virtual LocalisableString LevelSettingLabel => SpaceMixStrings.SPACEMIX_PATTERN_LEVEL_LABEL;
        protected virtual int DefaultLevel => 0;
        protected virtual int DefaultOscillationBeats => 1;
        protected virtual int DefaultWindowProcessInterval => 1;
        protected virtual int DefaultWindowProcessOffset => 1;
        protected virtual int DefaultApplyOrder => 60;

        public override string Name => $"PS SpaceMix {PatternName}";
        public override string Acronym => PatternAcronym;
        public override double ScoreMultiplier => 1;
        public override LocalisableString Description => SpaceMixStrings.SPACEMIX_PATTERN_DESCRIPTION;
        public override IconUsage? Icon => FontAwesome.Solid.Magic;
        public override ModType Type => ModType.NeriMod;
        public override bool Ranked => false;
        public override bool ValidForMultiplayer => false;
        public override bool ValidForFreestyleAsRequiredMod => false;

        // ── 基础参数（与 PatternBase 一致）──

        [SettingSource(typeof(SpaceMixStrings), nameof(SpaceMixStrings.SPACEMIX_WAVEFORM_LABEL), nameof(SpaceMixStrings.SPACEMIX_WAVEFORM_DESCRIPTION))]
        public Bindable<EzOscillator.EzWaveform> Waveform { get; } = new Bindable<EzOscillator.EzWaveform>(EzOscillator.EzWaveform.Sine);

        public BindableNumber<int> Level { get; } = new BindableInt(0)
        {
            MinValue = 0,
            MaxValue = 10,
            Precision = 1
        };

        [SettingSource(typeof(SpaceMixStrings), nameof(SpaceMixStrings.SPACEMIX_OSCILLATION_BEATS_LABEL), nameof(SpaceMixStrings.SPACEMIX_OSCILLATION_BEATS_DESCRIPTION))]
        public BindableNumber<int> OscillationBeats { get; } = new BindableInt(2)
        {
            MinValue = 1,
            MaxValue = 8,
            Precision = 1
        };

        [SettingSource(typeof(SpaceMixStrings), nameof(SpaceMixStrings.SPACEMIX_WINDOW_INTERVAL_LABEL), nameof(SpaceMixStrings.SPACEMIX_WINDOW_INTERVAL_DESCRIPTION))]
        public BindableNumber<int> WindowInterval { get; } = new BindableInt(2)
        {
            MinValue = 1,
            MaxValue = 4,
            Precision = 1
        };

        [SettingSource(typeof(SpaceMixStrings), nameof(SpaceMixStrings.SPACEMIX_WINDOW_OFFSET_LABEL), nameof(SpaceMixStrings.SPACEMIX_WINDOW_OFFSET_DESCRIPTION))]
        public BindableNumber<int> WindowStartOffset { get; } = new BindableInt(1)
        {
            MinValue = 1,
            MaxValue = 4,
            Precision = 1
        };

        [SettingSource(typeof(SpaceMixStrings), nameof(SpaceMixStrings.SPACEMIX_SKIP_FINE_LABEL), nameof(SpaceMixStrings.SPACEMIX_SKIP_FINE_DESCRIPTION))]
        public BindableNumber<int> SkipFineThreshold { get; } = new BindableInt(2)
        {
            MinValue = 1,
            MaxValue = 10,
            Precision = 1
        };

        [SettingSource(typeof(SpaceMixStrings), nameof(SpaceMixStrings.SPACEMIX_SKIP_QUARTER_LABEL), nameof(SpaceMixStrings.SPACEMIX_SKIP_QUARTER_DESCRIPTION))]
        public BindableNumber<int> SkipQuarterDivisor { get; } = new BindableInt(2)
        {
            MinValue = 1,
            MaxValue = 10,
            Precision = 1
        };

        // ── SpaceMix 面板策略参数 ──

        [SettingSource(typeof(SpaceMixStrings), nameof(SpaceMixStrings.SPACEMIX_KPS_MAX_LABEL), nameof(SpaceMixStrings.SPACEMIX_KPS_MAX_DESCRIPTION))]
        public BindableNumber<float> KpsMax { get; } = new BindableFloat(30f)
        {
            MinValue = 10f,
            MaxValue = 60f,
            Precision = 1f
        };

        [SettingSource(typeof(SpaceMixStrings), nameof(SpaceMixStrings.SPACEMIX_EFFECT_WEIGHT_LABEL), nameof(SpaceMixStrings.SPACEMIX_EFFECT_WEIGHT_DESCRIPTION))]
        public BindableNumber<float> EffectBaseWeight { get; } = new BindableFloat(0.35f)
        {
            MinValue = 0f,
            MaxValue = 1f,
            Precision = 0.05f
        };

        [SettingSource(typeof(SpaceMixStrings), nameof(SpaceMixStrings.SPACEMIX_MIN_BEATS_LABEL), nameof(SpaceMixStrings.SPACEMIX_MIN_BEATS_DESCRIPTION))]
        public BindableNumber<int> MinBeatsPerPanel { get; } = new BindableInt(2)
        {
            MinValue = 1,
            MaxValue = 8,
            Precision = 1
        };

        // ── 种子与顺序 ──

        [SettingSource(typeof(EzCommonModStrings), nameof(EzCommonModStrings.SEED_LABEL), nameof(EzCommonModStrings.SEED_DESCRIPTION), SettingControlType = typeof(SettingsNumberBox))]
        public Bindable<int?> Seed { get; } = new Bindable<int?>();

        [SettingSource(typeof(EzCommonModStrings), nameof(EzCommonModStrings.APPLY_ORDER_LABEL), nameof(EzCommonModStrings.APPLY_ORDER_DESCRIPTION))]
        public BindableNumber<int> ApplyOrderIndex { get; } = new BindableInt(60)
        {
            MinValue = 0,
            MaxValue = 100
        };

        public int ApplyOrder => ApplyOrderIndex.Value;

        protected virtual int WindowProcessInterval => Math.Clamp(WindowInterval.Value, 1, 4);
        protected virtual int WindowProcessOffset => Math.Clamp(WindowStartOffset.Value - 1, 0, WindowProcessInterval - 1);
        protected virtual int MaxIterationsPerWindow => 1;

        protected ManiaModPatternShiftSpaceMixPatternBase()
        {
            Level.Value = DefaultLevel;
            Waveform.Value = DefaultWaveform;
            OscillationBeats.Value = DefaultOscillationBeats;
            WindowInterval.Value = DefaultWindowProcessInterval;
            WindowStartOffset.Value = DefaultWindowProcessOffset;
            ApplyOrderIndex.Value = DefaultApplyOrder;
        }

        public override IEnumerable<(LocalisableString setting, LocalisableString value)> SettingDescription
        {
            get
            {
                yield return (LevelSettingLabel, $"{Level.Value}");
                yield return (SpaceMixStrings.SPACEMIX_WAVEFORM_LABEL, Waveform.Value.ToString());
                yield return (SpaceMixStrings.SPACEMIX_OSCILLATION_BEATS_LABEL, $"{OscillationBeats.Value}");
                yield return (SpaceMixStrings.SPACEMIX_WINDOW_INTERVAL_LABEL, $"{WindowInterval.Value}");
                yield return (SpaceMixStrings.SPACEMIX_WINDOW_OFFSET_LABEL, $"{WindowStartOffset.Value}");
                yield return (SpaceMixStrings.SPACEMIX_KPS_MAX_LABEL, $"{KpsMax.Value:F0}");
                yield return (SpaceMixStrings.SPACEMIX_EFFECT_WEIGHT_LABEL, $"{EffectBaseWeight.Value:F2}");
                yield return (SpaceMixStrings.SPACEMIX_MIN_BEATS_LABEL, $"{MinBeatsPerPanel.Value}");
                yield return (EzCommonModStrings.SEED_LABEL, Seed.Value?.ToString() ?? "Random");
                yield return (EzCommonModStrings.APPLY_ORDER_LABEL, $"{ApplyOrderIndex.Value}");
            }
        }

        public void ApplyToBeatmap(IBeatmap beatmap)
        {
            if (Level.Value <= 0)
                return;

            Seed.Value ??= RNG.Next();

            var maniaBeatmap = (ManiaBeatmap)beatmap;

            // 确保 14 列
            maniaBeatmap.Stages.Clear();
            maniaBeatmap.Stages.Add(new StageDefinition(TOTAL_COLUMNS));
            maniaBeatmap.Difficulty.CircleSize = TOTAL_COLUMNS;

            var oscillator = new EzOscillator(Seed.Value.Value, ezWaveform: Waveform.Value);

            // 构建面板策略
            var strategy = new SpaceMixPanelStrategy
            {
                KpsMax = KpsMax.Value,
                EffectBaseWeight = EffectBaseWeight.Value,
                MinBeatsPerPanel = MinBeatsPerPanel.Value
            };
            strategy.Reset();

            var rng = new Random(Seed.Value.Value);

            // 使用现有滚动窗口引擎，但在回调中集成面板选择 + 区域过滤
            var psSettings = new KeyPatternSettings
            {
                Level = Level.Value,
                OscillationBeats = OscillationBeats.Value,
                WindowProcessInterval = WindowProcessInterval,
                WindowProcessOffset = WindowProcessOffset,
                MaxIterationsPerWindow = MaxIterationsPerWindow,
                Seed = Seed.Value.Value,
                FineCountThreshold = SkipFineThreshold.Value,
                QuarterLineDivisor = SkipQuarterDivisor.Value
            };

            ManiaKeyPatternHelp.ProcessRollingWindowWithOscillator(
                maniaBeatmap,
                PatternType,
                psSettings,
                oscillator,
                (windowObjects, bm, wStart, wEnd, settings, rngInner, maxIter) =>
                {
                    double beatLength = bm.ControlPointInfo.TimingPointAt(wStart).BeatLength;
                    double windowDuration = wEnd - wStart;
                    var zone = strategy.DecidePanel(windowObjects, windowDuration, beatLength, rngInner);
                    var desc = zone == SpaceMixZone.DP_Left ? SpaceMixLayout.DP_Left
                        : zone == SpaceMixZone.Effect ? SpaceMixLayout.Effect
                        : SpaceMixLayout.DP_Right;

                    // ★ 模版约束：只在选定区域内生成 note
                    int beforeCount = bm.HitObjects.Count;

                    var zoneObjects = windowObjects
                        .Where(o => o.Column >= desc.StartColumn && o.Column <= desc.EndColumn)
                        .ToList();

                    // ★ 若该窗口内选定区域没有任何 note 但等级 > 0 → 注入种子
                    //    解决 DP 模板清空 Effect 后子模组无法在 Effect 区生成 note 的问题
                    if (zoneObjects.Count == 0 && settings.Level > 0)
                    {
                        double seedTime = wStart + (wEnd - wStart) * 0.5;
                        int seedCol = desc.StartColumn + (desc.EndColumn - desc.StartColumn) / 2;
                        bm.HitObjects.Add(new Note { Column = seedCol, StartTime = seedTime });
                        zoneObjects.Add(bm.HitObjects[^1]);
                    }

                    if (zoneObjects.Count > 0)
                    {
                        ApplyPatternForWindow(zoneObjects, bm, wStart, wEnd, settings, rngInner, maxIter);
                    }

                    // ★ 列范围强制约束：把新增 note 的列 clamp 到选定区域内
                    for (int i = beforeCount; i < bm.HitObjects.Count; i++)
                    {
                        var obj = bm.HitObjects[i];
                        if (obj.Column < desc.StartColumn)
                            obj.Column = desc.StartColumn;
                        else if (obj.Column > desc.EndColumn)
                            obj.Column = desc.EndColumn;
                    }
                });

            ManiaNoteCleanupTool.CleanupBeatmap(maniaBeatmap, seed: Seed.Value.Value);
        }
    }
}
