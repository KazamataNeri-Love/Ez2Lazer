// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.EzOsuGame.Localization;
using osu.Game.EzOsuGame.Material;
using osu.Game.EzOsuGame.Scoring;
using osu.Game.EzOsuGame.Screens;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation.SkinComponents;
using osu.Game.Overlays.Settings;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Screens.Play;
using osu.Game.Skinning;
using osuTK;
using osuTK.Graphics;
using Container = osu.Framework.Graphics.Containers.Container;

namespace osu.Game.EzOsuGame.HUD
{
    public enum EzScoreCompareBarDirection
    {
        [Description("Upward")]
        Upward,

        [Description("Downward")]
        Downward,
    }

    /// <summary>
    /// BMS 风格三柱分数对比：当前局 + 两条按对比条件选出的参考柱。
    /// 服务注册时 ghost 柱绑定 timeline processor；未注册时展示 ScoreInfo 静态终值。
    /// </summary>
    public partial class EzHUDScoreCompareBars : EzHUDScoreRaceComponent, ISerialisableDrawable, IMaterialBackdropConsumer
    {
        private const float backdrop_blur_strength = 16;
        private const int static_ghost_query_limit = 10;

        public bool UsesFixedAnchor { get; set; }

        public bool WantsMaterialCapture => materialBackdrop.WantsMaterialCapture;

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.SCORE_COMPARE_CONDITION1_LABEL), nameof(EzHUDStrings.SCORE_COMPARE_CONDITION1_DESCRIPTION))]
        public Bindable<EzScoreRaceMetric> CompareCondition1Setting { get; } = new Bindable<EzScoreRaceMetric>(EzScoreRaceMetric.Accuracy);

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.SCORE_COMPARE_CONDITION2_LABEL), nameof(EzHUDStrings.SCORE_COMPARE_CONDITION2_DESCRIPTION))]
        public Bindable<EzScoreRaceMetric> CompareCondition2Setting { get; } = new Bindable<EzScoreRaceMetric>(EzScoreRaceMetric.TotalScore);

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.SCORE_RACE_BAR_DIRECTION_LABEL), nameof(EzHUDStrings.SCORE_RACE_BAR_DIRECTION_DESCRIPTION))]
        public Bindable<EzScoreCompareBarDirection> BarDirection { get; } = new Bindable<EzScoreCompareBarDirection>(EzScoreCompareBarDirection.Upward);

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.SCORE_COMPARE_BAR_HEIGHT_LABEL), nameof(EzHUDStrings.SCORE_COMPARE_BAR_HEIGHT_DESCRIPTION))]
        public BindableNumber<float> BarHeight { get; } = new BindableNumber<float>(120)
        {
            MinValue = 40,
            MaxValue = 400,
            Precision = 1,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.SCORE_COMPARE_BAR_WIDTH_LABEL), nameof(EzHUDStrings.SCORE_COMPARE_BAR_WIDTH_DESCRIPTION))]
        public BindableNumber<float> BarWidth { get; } = new BindableNumber<float>(38)
        {
            MinValue = 10,
            MaxValue = 80,
            Precision = 1,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.SCORE_COMPARE_BORDER_THICKNESS_LABEL), nameof(EzHUDStrings.SCORE_COMPARE_BORDER_THICKNESS_DESCRIPTION),
            SettingControlType = typeof(SettingsPercentageSlider<float>))]
        public BindableFloat BorderThicknessPercent { get; } = new BindableFloat(0.04f)
        {
            MinValue = 0,
            MaxValue = 0.3f,
            Precision = 0.01f,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.SCORE_COMPARE_SHEEN_WIDTH_LABEL), nameof(EzHUDStrings.SCORE_COMPARE_SHEEN_WIDTH_DESCRIPTION),
            SettingControlType = typeof(SettingsPercentageSlider<float>))]
        public BindableFloat SheenWidthPercent { get; } = new BindableFloat(0.12f)
        {
            MinValue = 0,
            MaxValue = 0.5f,
            Precision = 0.01f,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.SCORE_COMPARE_SHOW_TITLE_LABEL), nameof(EzHUDStrings.SCORE_COMPARE_SHOW_TITLE_DESCRIPTION))]
        public BindableBool ShowTitle { get; } = new BindableBool(true);

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.SCORE_COMPARE_SHOW_VALUE_LABEL), nameof(EzHUDStrings.SCORE_COMPARE_SHOW_VALUE_DESCRIPTION))]
        public BindableBool ShowValue { get; } = new BindableBool(true);

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.SCORE_COMPARE_NOW_COLOUR_LABEL), nameof(EzHUDStrings.SCORE_COMPARE_NOW_COLOUR_DESCRIPTION),
            SettingControlType = typeof(EzSettingsColour))]
        public BindableColour4 NowBarColour { get; } = new BindableColour4(Colour4.FromHex("99EB47").Opacity(0.85f));

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.SCORE_COMPARE_CONDITION1_COLOUR_LABEL), nameof(EzHUDStrings.SCORE_COMPARE_CONDITION1_COLOUR_DESCRIPTION),
            SettingControlType = typeof(EzSettingsColour))]
        public BindableColour4 CompareCondition1BarColour { get; } = new BindableColour4(Colour4.FromHex("2E576B").Opacity(0.85f));

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.SCORE_COMPARE_CONDITION2_COLOUR_LABEL), nameof(EzHUDStrings.SCORE_COMPARE_CONDITION2_COLOUR_DESCRIPTION),
            SettingControlType = typeof(EzSettingsColour))]
        public BindableColour4 CompareCondition2BarColour { get; } = new BindableColour4(Colour4.FromHex("FFDD55").Opacity(0.85f));

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.SCORE_COMPARE_BACKGROUND_VISIBLE_LABEL), nameof(EzHUDStrings.SCORE_COMPARE_BACKGROUND_VISIBLE_DESCRIPTION))]
        public BindableBool BackgroundVisible { get; } = new BindableBool(true);

        [SettingSource(typeof(SkinnableComponentStrings), nameof(SkinnableComponentStrings.CornerRadius), nameof(SkinnableComponentStrings.CornerRadiusDescription),
            SettingControlType = typeof(SettingsPercentageSlider<float>))]
        public new BindableFloat CornerRadius { get; } = new BindableFloat(0.12f)
        {
            MinValue = 0,
            MaxValue = 0.5f,
            Precision = 0.01f,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.BACKDROP_BLUR_LABEL), nameof(EzHUDStrings.BACKDROP_BLUR_DESCRIPTION))]
        public BindableBool BackdropBlurEnabled { get; } = new BindableBool(false);

        private const float bar_spacing = 4;
        private const float label_area_height = 36;
        private const float container_padding = 6;

        private readonly EzMaterialContainer materialBackdrop;
        private readonly Container backgroundLayer;
        private Container barsContainer = null!;
        private readonly CompareBar[] bars = new CompareBar[3];

        private EzScoreRaceTimelineScoreProcessor? ghostProcessor1;
        private EzScoreRaceTimelineScoreProcessor? ghostProcessor2;
        private EzScoreRaceState? boundState1;
        private EzScoreRaceState? boundState2;
        private ScoreInfo? boundStaticScore1;
        private ScoreInfo? boundStaticScore2;

        private bool refreshScheduled;
        private long lastNowBarScore = long.MinValue;
        private double lastProcessorUpdateTime;
        private bool passiveMode;
        private EzScoreRaceService? interestService;

        private IBindableDictionary<string, EzScoreRaceState>? stateLookup;

        public EzHUDScoreCompareBars()
        {
            Width = 3 * BarWidth.Value + 2 * bar_spacing + container_padding * 2;
            Height = BarHeight.Value + label_area_height + container_padding * 2;
            Masking = true;

            InternalChild = backgroundLayer = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Alpha = 0,
                Children = new Drawable[]
                {
                    materialBackdrop = new EzMaterialContainer(),
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.White.Opacity(0.12f),
                    },
                },
            };
        }

        protected override void LoadComplete()
        {
            barsContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Padding = new MarginPadding(container_padding),
                Alpha = 0,
            };

            for (int i = 0; i < bars.Length; i++)
            {
                bars[i] = new CompareBar();
                barsContainer.Add(bars[i]);
            }

            AddInternal(barsContainer);

            base.LoadComplete();

            BackgroundVisible.BindValueChanged(_ => updateBackgroundVisibility(), true);
            BackdropBlurEnabled.BindValueChanged(_ => SyncMaterialCaptureState(), true);

            CompareCondition1Setting.BindValueChanged(_ => refreshGhostBars(), true);
            CompareCondition2Setting.BindValueChanged(_ => refreshGhostBars(), true);
            BarDirection.BindValueChanged(_ => layoutBars(), true);
            BarHeight.BindValueChanged(_ => layoutBars(), true);
            BarWidth.BindValueChanged(_ => layoutBars(), true);
            BorderThicknessPercent.BindValueChanged(_ => layoutBars(), true);
            SheenWidthPercent.BindValueChanged(_ => layoutBars(), true);

            NowBarColour.BindValueChanged(_ => onBarColourChanged(), true);
            CompareCondition1BarColour.BindValueChanged(_ => onBarColourChanged(), true);
            CompareCondition2BarColour.BindValueChanged(_ => onBarColourChanged(), true);

            ShowTitle.BindValueChanged(_ => updateTextVisibility(), true);
            ShowValue.BindValueChanged(_ => updateTextVisibility(), true);

            layoutBars();
        }

        private void updateTextVisibility()
        {
            foreach (var bar in bars)
                bar.SetTextVisibility(ShowTitle.Value, ShowValue.Value);
        }

        public void SyncMaterialCaptureState()
        {
            materialBackdrop.MaterialVisible = BackgroundVisible.Value && BackdropBlurEnabled.Value;
            materialBackdrop.BlurSigma = new Vector2(backdrop_blur_strength);
            materialBackdrop.SyncMaterialCaptureState();
        }

        protected override void Update()
        {
            base.Update();

            updateCurrentAndTheoreticalBars();
            float effectiveCornerRadius = CornerRadius.Value * Math.Min(DrawWidth, DrawHeight);
            base.CornerRadius = effectiveCornerRadius;
            materialBackdrop.GlassCornerRadius = effectiveCornerRadius;

            if (passiveMode)
                return;

            if (Time.Current - lastProcessorUpdateTime >= 50)
            {
                ghostProcessor1?.UpdateScore();
                ghostProcessor2?.UpdateScore();
                lastProcessorUpdateTime = Time.Current;
            }
        }

        protected override void OnGameplayClockResolved(GameplayClockContainer clock)
        {
            base.OnGameplayClockResolved(clock);

            ghostProcessor1?.ReferenceClock = clock;
            ghostProcessor2?.ReferenceClock = clock;
        }

        private void bindStateLookupWhenAvailable()
        {
            if (IsScoreRaceServiceAvailable)
                activateLive();
            else
                activatePassive();
        }

        private void ensureGhostProcessors()
        {
            if (ghostProcessor1 != null)
                return;

            ghostProcessor1 = new EzScoreRaceTimelineScoreProcessor();
            ghostProcessor2 = new EzScoreRaceTimelineScoreProcessor();

            if (GameplayClockContainer != null)
            {
                ghostProcessor1.ReferenceClock = GameplayClockContainer;
                ghostProcessor2.ReferenceClock = GameplayClockContainer;
            }

            AddInternal(ghostProcessor1);
            AddInternal(ghostProcessor2);

            ghostProcessor1.TotalScore.BindValueChanged(_ => updateGhostCompareBar(1));
            ghostProcessor2.TotalScore.BindValueChanged(_ => updateGhostCompareBar(2));
        }

        private void activateLive()
        {
            passiveMode = false;
            boundStaticScore1 = null;
            boundStaticScore2 = null;

            var service = ScoreRaceService!;

            registerInterest(service);

            ensureGhostProcessors();

            if (stateLookup == null)
            {
                stateLookup = service.States;
                stateLookup.BindCollectionChanged(onStatesChanged, true);
            }

            barsContainer.Alpha = 1;
            updateBackgroundVisibility();
            refreshPickedGhosts();
        }

        private void activatePassive()
        {
            passiveMode = true;

            unregisterInterest();

            stateLookup = null;
            bindProcessorToState(ghostProcessor1, ref boundState1, null);
            bindProcessorToState(ghostProcessor2, ref boundState2, null);

            barsContainer.Alpha = 1;
            updateBackgroundVisibility();
            refreshStaticGhosts();
        }

        private void onStatesChanged(object? sender, NotifyDictionaryChangedEventArgs<string, EzScoreRaceState> e)
        {
            if (!refreshScheduled)
            {
                refreshScheduled = true;
                Scheduler.AddOnce(scheduleRefresh);
            }
        }

        private void scheduleRefresh()
        {
            refreshScheduled = false;
            refreshPickedGhosts();
        }

        protected override void OnSessionReady()
        {
            bindStateLookupWhenAvailable();
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                unregisterInterest();

                ghostProcessor1?.Dispose();
                ghostProcessor2?.Dispose();
            }

            base.Dispose(isDisposing);
        }

        private void registerInterest(EzScoreRaceService service)
        {
            if (interestService != null)
                return;

            service.RegisterInterest();
            interestService = service;
        }

        private void unregisterInterest()
        {
            if (interestService == null)
                return;

            interestService.UnregisterInterest();
            interestService = null;
        }

        private void refreshGhostBars()
        {
            if (passiveMode)
                refreshStaticGhosts();
            else
                refreshPickedGhosts();
        }

        private void updateCurrentAndTheoreticalBars()
        {
            long barScoreScale = getBarScoreScale();
            long nowBarScore = GetLiveDisplayScore();

            if (nowBarScore != lastNowBarScore)
            {
                bars[0].UpdateValues(EzHUDStrings.SCORE_COMPARE_NOW_LABEL, formatScore(nowBarScore), nowBarScore, barScoreScale, getBarColour(0));
                lastNowBarScore = nowBarScore;
            }

            if (CompareCondition1Setting.Value == EzScoreRaceMetric.TheoreticalMaxScore)
                updateTheoreticalCompareBar(1, CompareCondition1Setting.Value, barScoreScale);

            if (CompareCondition2Setting.Value == EzScoreRaceMetric.TheoreticalMaxScore)
                updateTheoreticalCompareBar(2, CompareCondition2Setting.Value, barScoreScale);
        }

        private void updateGhostCompareBar(int conditionIndex)
        {
            var metric = conditionIndex == 1 ? CompareCondition1Setting.Value : CompareCondition2Setting.Value;

            if (metric == EzScoreRaceMetric.TheoreticalMaxScore)
                return;

            var processor = conditionIndex == 1 ? ghostProcessor1 : ghostProcessor2;
            long barScoreScale = getBarScoreScale();

            if (processor == null || !SupportsGhostRace)
            {
                bars[conditionIndex].UpdateValues(metric.GetLocalisableDescription(), string.Empty, 0, barScoreScale, getBarColour(conditionIndex), 0);
                return;
            }

            long barScore = processor.TotalScore.Value;
            string label = barScore > 0 ? formatScore(barScore) : string.Empty;
            bars[conditionIndex].UpdateValues(
                metric.GetLocalisableDescription(),
                label,
                barScore,
                barScoreScale,
                getBarColour(conditionIndex),
                processor.State?.ScoreInfo.TotalScore);
        }

        private void updateStaticCompareBar(int conditionIndex)
        {
            var metric = conditionIndex == 1 ? CompareCondition1Setting.Value : CompareCondition2Setting.Value;

            if (metric == EzScoreRaceMetric.TheoreticalMaxScore)
                return;

            var scoreInfo = conditionIndex == 1 ? boundStaticScore1 : boundStaticScore2;
            long barScoreScale = getBarScoreScale();

            if (scoreInfo == null || !SupportsGhostRace)
            {
                bars[conditionIndex].UpdateValues(metric.GetLocalisableDescription(), string.Empty, 0, barScoreScale, getBarColour(conditionIndex));
                return;
            }

            long barScore = scoreInfo.TotalScore;
            string label = barScore > 0 ? formatScore(barScore) : string.Empty;
            bars[conditionIndex].UpdateValues(
                metric.GetLocalisableDescription(),
                label,
                barScore,
                barScoreScale,
                getBarColour(conditionIndex));
        }

        private void updateTheoreticalCompareBar(int barIndex, EzScoreRaceMetric metric, long barScoreScale)
        {
            long theoreticalScore = getTheoreticalScoreAtTime();
            bars[barIndex].UpdateValues(
                metric.GetLocalisableDescription(),
                formatScore(theoreticalScore),
                theoreticalScore,
                barScoreScale,
                getBarColour(barIndex),
                barScoreScale);
        }

        private void refreshStaticGhosts()
        {
            var scores = QueryStaticGhostScores(EzScoreModFilter.Any, static_ghost_query_limit);

            boundStaticScore1 = pickBestScoreInfo(scores, CompareCondition1Setting.Value);
            boundStaticScore2 = pickBestScoreInfo(scores, CompareCondition2Setting.Value);

            updateStaticCompareBar(1);
            updateStaticCompareBar(2);
        }

        /// <summary>
        /// 根据当前 compare condition，从字典中选择最佳 ghost 并绑定到对应 processor。
        /// </summary>
        private void refreshPickedGhosts()
        {
            if (stateLookup == null || stateLookup.Count == 0)
            {
                bindProcessorToState(ghostProcessor1, ref boundState1, null);
                bindProcessorToState(ghostProcessor2, ref boundState2, null);
                updateGhostCompareBar(1);
                updateGhostCompareBar(2);
                return;
            }

            var states = stateLookup.Values.OrderByDescending(s => s.ScoreInfo.TotalScore).ToList();

            EzScoreRaceState? newState1 = pickBestState(states, CompareCondition1Setting.Value);
            EzScoreRaceState? newState2 = pickBestState(states, CompareCondition2Setting.Value);

            bindProcessorToState(ghostProcessor1, ref boundState1, newState1);
            bindProcessorToState(ghostProcessor2, ref boundState2, newState2);

            updateGhostCompareBar(1);
            updateGhostCompareBar(2);
        }

        private static EzScoreRaceState? pickBestState(List<EzScoreRaceState> states, EzScoreRaceMetric metric)
        {
            if (metric == EzScoreRaceMetric.TheoreticalMaxScore)
                return null;

            if (states.Count == 0)
                return null;

            if (metric == EzScoreRaceMetric.TotalScore)
                return states.OrderByDescending(s => s.ScoreInfo.TotalScore).ThenByDescending(s => s.ScoreInfo.Date).FirstOrDefault();

            return EzScoreRaceMetricOrdering.ApplyMetricOrdering(
                states,
                metric,
                s => s.ScoreInfo.TotalScore,
                s => s.ScoreInfo.Accuracy,
                s => s.ScoreInfo.MaxCombo,
                s => s.ScoreInfo.Statistics.Where(kv => kv.Key.IsMiss()).Sum(kv => kv.Value)
            ).FirstOrDefault();
        }

        private static ScoreInfo? pickBestScoreInfo(List<ScoreInfo> scores, EzScoreRaceMetric metric)
        {
            if (metric == EzScoreRaceMetric.TheoreticalMaxScore)
                return null;

            if (scores.Count == 0)
                return null;

            if (metric == EzScoreRaceMetric.TotalScore)
                return scores.OrderByDescending(s => s.TotalScore).ThenByDescending(s => s.Date).FirstOrDefault();

            return EzScoreRaceMetricOrdering.ApplyMetricOrdering(
                scores,
                metric,
                s => s.TotalScore,
                s => s.Accuracy,
                s => s.MaxCombo,
                EzLocalScoreQueries.GetMissCount
            ).FirstOrDefault();
        }

        private void bindProcessorToState(EzScoreRaceTimelineScoreProcessor? processor, ref EzScoreRaceState? currentBound, EzScoreRaceState? newState)
        {
            if (currentBound == newState)
                return;

            if (currentBound != null)
            {
                processor?.Reset();
                currentBound = null;
            }

            if (newState != null && processor != null)
            {
                processor.BindTo(newState);
                currentBound = newState;
            }
        }

        private void updateBackgroundVisibility()
        {
            backgroundLayer.Alpha = BackgroundVisible.Value ? 1 : 0;
            SyncMaterialCaptureState();
        }

        /// <summary>按柱位取色：三根柱各自独立，不再随对比条件切换。</summary>
        private Color4 getBarColour(int barIndex)
        {
            Colour4 colour = barIndex switch
            {
                0 => NowBarColour.Value,
                1 => CompareCondition1BarColour.Value,
                _ => CompareCondition2BarColour.Value,
            };

            return colour;
        }

        /// <summary>
        /// 只刷新颜色派生，不重新挑选或重绑 ghost 成绩：拖取色器时不应触发 Realm 查询与分数跳变。
        /// </summary>
        private void onBarColourChanged()
        {
            lastNowBarScore = long.MinValue;

            foreach (var bar in bars)
                bar.InvalidateColour();

            updateGhostCompareBar(1);
            updateGhostCompareBar(2);

            if (CompareCondition1Setting.Value == EzScoreRaceMetric.TheoreticalMaxScore)
                updateTheoreticalCompareBar(1, CompareCondition1Setting.Value, getBarScoreScale());

            if (CompareCondition2Setting.Value == EzScoreRaceMetric.TheoreticalMaxScore)
                updateTheoreticalCompareBar(2, CompareCondition2Setting.Value, getBarScoreScale());
        }

        private static string formatScore(long score) => score.ToString("N0");

        private long getTheoreticalScoreAtTime()
        {
            if (ScoreProcessor == null)
                return 0;

            return ScoreProcessor.GetTheoreticalPerfectJudgedDisplayScore();
        }

        private long getBarScoreScale()
        {
            if (ScoreProcessor != null && ScoreProcessor.MaximumTotalScore > 0)
                return ScoreProcessor.MaximumTotalScore;

            return (long)ScoreProcessor.MAX_SCORE;
        }

        private void layoutBars()
        {
            float barThickness = BarWidth.Value;
            float barAxisHeight = BarHeight.Value;
            var direction = BarDirection.Value;
            float borderThickness = resolveBorderThickness(barThickness);
            float sheenWidth = resolveSheenWidth(barThickness, borderThickness);

            Width = bars.Length * barThickness + (bars.Length - 1) * bar_spacing + container_padding * 2;
            Height = barAxisHeight + label_area_height + container_padding * 2;

            for (int i = 0; i < bars.Length; i++)
            {
                var bar = bars[i];
                bar.Configure(direction, barAxisHeight, barThickness, borderThickness, sheenWidth);

                bar.Anchor = Anchor.BottomLeft;
                bar.Origin = Anchor.BottomLeft;
                bar.Width = barThickness;
                bar.Height = barAxisHeight + label_area_height;
                bar.X = i * (barThickness + bar_spacing);
            }
        }

        private float resolveBorderThickness(float barThickness)
        {
            if (BorderThicknessPercent.Value <= 0)
                return 0;

            return Math.Clamp(barThickness * BorderThicknessPercent.Value, 1, Math.Max(1, barThickness * 0.35f));
        }

        private float resolveSheenWidth(float barThickness, float borderThickness)
        {
            if (SheenWidthPercent.Value <= 0)
                return 0;

            return Math.Clamp(barThickness * SheenWidthPercent.Value, 0, Math.Max(0, barThickness - 2 * borderThickness));
        }

        private partial class CompareBar : CompositeDrawable
        {
            private const float min_bar_height = 3;

            private Container frame = null!;
            private Container fillReveal = null!;
            private Container fillBody = null!;
            private Box fillLeft = null!;
            private Box fillRight = null!;
            private Box sheen = null!;
            private Box borderTop = null!;
            private Box borderBottom = null!;
            private Box borderLeft = null!;
            private Box borderRight = null!;
            private OsuSpriteText titleText = null!;
            private OsuSpriteText valueText = null!;
            private Container barTrack = null!;
            private EzScoreCompareBarDirection direction = EzScoreCompareBarDirection.Upward;
            private float maxBarSize;
            private float barThickness;
            private float borderThickness;
            private float sheenWidth;

            private LocalisableString lastTitle;
            private string lastValue = string.Empty;
            private float lastFillHeight = -1;
            private float lastFrameHeight = -1;
            private Color4 lastBarColour;
            private bool colourDirty;

            public CompareBar()
            {
                RelativeSizeAxes = Axes.None;
            }

            public void Configure(EzScoreCompareBarDirection barDirection, float maxSize, float thickness, float border, float sheenStripWidth)
            {
                direction = barDirection;
                maxBarSize = maxSize;
                barThickness = thickness;
                borderThickness = border;
                sheenWidth = sheenStripWidth;

                barTrack.Size = new Vector2(barThickness, maxSize);
                Width = barThickness;

                frame.Width = barThickness;

                // 边框用四条独立 Box 绘制：框架的遮罩边框只叠加在子节点像素上，空框区域没有子节点，
                // 且内层遮罩会替换外层遮罩信息，导致框内完全看不到边框。
                float borderAlpha = borderThickness > 0 ? 1 : 0;
                borderTop.Alpha = borderAlpha;
                borderBottom.Alpha = borderAlpha;
                borderLeft.Alpha = borderAlpha;
                borderRight.Alpha = borderAlpha;

                // 按当前框高重新套一次钳制：布局变化会把高度改回来，不能让它停在未钳制的边框厚度上。
                applyBorderSize(frame.Height);

                sheen.Width = sheenWidth;
                sheen.Alpha = sheenWidth > 0 ? 1 : 0;

                applyBarAnchor();
            }

            private void applyBorderSize(float frameHeight)
            {
                // 边框是 frame 的兄弟子节点，不会被任何遮罩裁剪；框比边框还薄时必须把四边钳进框内，
                // 否则 0 分时会残留一条画到柱区之外的横线，框底也会比其它柱低一个边框厚度。
                float capHeight = Math.Min(borderThickness, Math.Max(0, frameHeight) * 0.5f);
                float capWidth = Math.Min(borderThickness, barThickness * 0.5f);

                borderTop.Height = capHeight;
                borderBottom.Height = capHeight;
                borderLeft.Width = capWidth;
                borderRight.Width = capWidth;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                InternalChild = new FillFlowContainer
                {
                    Direction = FillDirection.Vertical,
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Spacing = new Vector2(0, 2),
                    Children = new Drawable[]
                    {
                        titleText = new OsuSpriteText
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Font = OsuFont.GetFont(size: 11, weight: FontWeight.Bold),
                        },
                        barTrack = new Container
                        {
                            RelativeSizeAxes = Axes.None,
                            Size = new Vector2(barThickness, maxBarSize),
                            Child = frame = new Container
                            {
                                RelativeSizeAxes = Axes.None,
                                Children = new Drawable[]
                                {
                                    fillReveal = new Container
                                    {
                                        Masking = true,
                                        RelativeSizeAxes = Axes.X,
                                        Width = 1,
                                        Child = fillBody = new Container
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Width = 1,
                                            Children = new Drawable[]
                                            {
                                                fillLeft = new Box
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                    Size = new Vector2(0.5f, 1),
                                                },
                                                fillRight = new Box
                                                {
                                                    Anchor = Anchor.TopRight,
                                                    Origin = Anchor.TopRight,
                                                    RelativeSizeAxes = Axes.Both,
                                                    Size = new Vector2(0.5f, 1),
                                                },
                                                sheen = new Box
                                                {
                                                    Anchor = Anchor.TopCentre,
                                                    Origin = Anchor.TopCentre,
                                                    RelativeSizeAxes = Axes.Y,
                                                    Height = 1,
                                                    Width = barThickness,
                                                },
                                            },
                                        },
                                    },
                                    borderTop = new Box
                                    {
                                        Anchor = Anchor.TopLeft,
                                        Origin = Anchor.TopLeft,
                                        RelativeSizeAxes = Axes.X,
                                    },
                                    borderBottom = new Box
                                    {
                                        Anchor = Anchor.BottomLeft,
                                        Origin = Anchor.BottomLeft,
                                        RelativeSizeAxes = Axes.X,
                                    },
                                    borderLeft = new Box
                                    {
                                        Anchor = Anchor.TopLeft,
                                        Origin = Anchor.TopLeft,
                                        RelativeSizeAxes = Axes.Y,
                                        Height = 1,
                                    },
                                    borderRight = new Box
                                    {
                                        Anchor = Anchor.TopRight,
                                        Origin = Anchor.TopRight,
                                        RelativeSizeAxes = Axes.Y,
                                        Height = 1,
                                    },
                                },
                            },
                        },
                        valueText = new OsuSpriteText
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Font = OsuFont.GetFont(size: 11),
                        },
                    },
                };

                applyBarAnchor();
            }

            private void applyBarAnchor()
            {
                Anchor anchor = direction == EzScoreCompareBarDirection.Upward ? Anchor.BottomCentre : Anchor.TopCentre;

                frame.Anchor = frame.Origin = anchor;
                fillReveal.Anchor = fillReveal.Origin = anchor;
                fillBody.Anchor = fillBody.Origin = anchor;
            }

            public void ResetVisualCache()
            {
                lastFillHeight = -1;
                lastFrameHeight = -1;
                colourDirty = true;
            }

            /// <summary>
            /// 基色来自外部设置时，当前值与缓存相同也要重算一次派生色。
            /// </summary>
            public void InvalidateColour() => colourDirty = true;

            /// <summary>
            /// 只隐藏文字、不改变布局：标题与分数仍占位，柱体位置不会随开关跳动。
            /// </summary>
            public void SetTextVisibility(bool showTitle, bool showValue)
            {
                titleText.Alpha = showTitle ? 1 : 0;
                valueText.Alpha = showValue ? 1 : 0;
            }

            /// <param name="targetScore">
            /// 固定框高用的终值分数；null 表示该柱没有可预知的终值，框高跟随填充一起增长。
            /// </param>
            public void UpdateValues(LocalisableString title, string value, long barScore, long maxBarScore, Color4 barColour, long? targetScore = null)
            {
                Alpha = 1;

                if (!lastTitle.Equals(title))
                {
                    titleText.Text = title;
                    lastTitle = title;
                }

                if (lastValue != value)
                {
                    valueText.Text = value;
                    lastValue = value;
                }

                if (colourDirty || lastBarColour != barColour)
                {
                    buildBarColours(barColour, out var leftFill, out var rightFill, out var sheenColour, out var borderColour);

                    fillLeft.Colour = leftFill;
                    fillRight.Colour = rightFill;
                    sheen.Colour = sheenColour;

                    borderTop.Colour = borderBottom.Colour = borderLeft.Colour = borderRight.Colour = borderColour;
                    lastBarColour = barColour;
                    colourDirty = false;
                }

                float fillHeight = getBarHeight(barScore, maxBarScore);

                if (barScore > 0 && fillHeight < min_bar_height)
                    fillHeight = min_bar_height;

                float frameHeight = targetScore is long target ? getBarHeight(target, maxBarScore) : fillHeight;

                if (frameHeight < fillHeight)
                    frameHeight = fillHeight;

                if (Math.Abs(lastFillHeight - fillHeight) < 0.01f && Math.Abs(lastFrameHeight - frameHeight) < 0.01f)
                    return;

                lastFillHeight = fillHeight;
                lastFrameHeight = frameHeight;

                frame.Width = barThickness;
                frame.Height = frameHeight;
                fillReveal.Height = fillHeight;
                fillBody.Height = frameHeight;

                applyBorderSize(frameHeight);
            }

            private float getBarHeight(long score, long maxScore)
            {
                float ratio = maxScore <= 0 ? 0 : (float)score / maxScore;
                return maxBarSize * Math.Clamp(ratio, 0, 1);
            }

            /// <summary>
            /// 由基色派生柱体视觉：左右两半的横向渐变、中央高光条、四边描边色。
            /// 高光与描边按“朝白插值”派生，任意基色下都能保持可见的白化量。
            /// </summary>
            private static void buildBarColours(Color4 baseColour, out ColourInfo leftFill, out ColourInfo rightFill, out Color4 sheenColour, out ColourInfo borderColour)
            {
                Colour4 baseValue = baseColour;

                Colour4 edge = baseValue.Darken(0.22f);
                Colour4 centre = baseValue.Lighten(0.10f);

                leftFill = ColourInfo.GradientHorizontal(edge, centre);
                rightFill = ColourInfo.GradientHorizontal(centre, edge);
                sheenColour = towardsWhite(baseValue, 0.55f);
                borderColour = towardsWhite(baseValue, 0.72f);
            }

            private static Colour4 towardsWhite(Colour4 colour, float amount)
            {
                float clamped = Math.Clamp(amount, 0, 1);

                return new Colour4(
                    colour.R + (1 - colour.R) * clamped,
                    colour.G + (1 - colour.G) * clamped,
                    colour.B + (1 - colour.B) * clamped,
                    colour.A);
            }
        }
    }
}
