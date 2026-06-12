// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.
//
// 方案三：硬编码列宽 + 特殊列因子，脱离游戏内 Ez2ConfigManager 配置
//   - keyConfigs       按 Key 数独立配置（列宽 / Scratch列 / Pedal列 / 各因子）
//   - hitPositionValue = 100f  （判定线距底部 px）

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.EzOsuGame.HUD;
using osu.Game.Rulesets.Mania.Beatmaps;
using osu.Game.Rulesets.Mania.EzMania.HUD;
using osu.Game.Rulesets.Scoring;
using osu.Game.Screens.Play.HUD.HitErrorMeters;
using osu.Game.Skinning;
using osuTK;
using osuTK.Graphics;
using osu.Game.Rulesets.Mania.Objects.Drawables;
using osu.Game.Rulesets.Mania.Skinning.Default;
using osu.Game.Rulesets.Mania.Skinning.EzStylePro;
using osu.Game.Rulesets.Mania.Skinning.Legacy;
using osu.Game.Rulesets.Mania.UI;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Logging;
using osu.Game.EzOsuGame;

// ═══════════════════════════════════════════════════════════════════════════
// EzProNeriColumnBackground — 加载后覆写 Column.EzNoteSizeBindable
// 所有 Note 绘制逻辑完全沿用 EzPro 原始 EzNote / EzHoldNoteHead / EzHoldNoteTail / EzHoldNoteMiddle
// ═══════════════════════════════════════════════════════════════════════════

public partial class EzProNeriColumnBackground : EzColumnBackground
{
    [Resolved]
    private Column column { get; set; } = null!;

    [Resolved]
    private ISkinSource skin { get; set; } = null!;

    [Resolved]
    private EzLocalTextureFactory factory { get; set; } = null!;

    protected override void LoadComplete()
    {
        base.LoadComplete();
        addHardcodedSeparator();
        overrideColumnType();
        overrideNoteSize();
    }

    /// <summary>硬编码分割线，脱离游戏内 ManiaSkipEmptyEdgeColumns 配置</summary>
    private void addHardcodedSeparator()
    {
        bool hasSep = (column.KeyMode, column.Index) switch
        {
            (12, 0 or 10) => true,
            (14, 0 or 5 or 6 or 11) => true,
            (16, 0 or 5 or 9 or 14) => true,
            (18, 0 or 5 or 7 or 8 or 10 or 15) => true,
            _ => false
        };

        if (!hasSep) return;

        AddInternal(new Box
        {
            Anchor = Anchor.TopRight,
            Origin = Anchor.TopCentre,
            Width = 2,
            Colour = Color4.White.Opacity(0.5f),
            Alpha = 0.25f,
            RelativeSizeAxes = Axes.Y,
        });
    }

    private void overrideColumnType()
    {
        Scheduler.Add(() =>
        {
            var types = ManiaEzProForNeriTransformer.GetColumnTypes(column.KeyMode);
            if (column.Index < types.Length)
                column.EzNoteTypeBindable.Value = types[column.Index];
        });
    }

    private void overrideNoteSize()
    {
        Scheduler.Add(() =>
        {
            float w = skin.GetConfig<ManiaSkinConfigurationLookup, float>(
                new ManiaSkinConfigurationLookup(LegacyManiaSkinConfigurationLookups.ColumnWidth, column.Index)
            )?.Value ?? column.DrawWidth;

            if (w <= 0)
            {
                overrideNoteSize();
                return;
            }

            float h = w * factory.GetRatio();
            column.EzNoteSizeBindable.Value = new Vector2(w, h);
        });
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// ComboMilestoneSwitcher — Combo 达标自动切换纹理
// ═══════════════════════════════════════════════════════════════════════════
// Milestones = { 0, 100, 500, 1000, 1500 }，对应同目录
// combo-0000.png / combo-0100.png / combo-0500.png / combo-1000.png / combo-1500.png
// ═══════════════════════════════════════════════════════════════════════════

public partial class ComboMilestoneSwitcher : CompositeDrawable
{
    [Resolved]
    private ScoreProcessor scoreProcessor { get; set; } = null!;

    [Resolved]
    private ISkinSource skin { get; set; } = null!;

    private static readonly int[] s_milestones = { 0, 100, 500, 1000, 1500 };
    private readonly Container[] milestoneContainers = new Container[s_milestones.Length];

    /// <summary>上下移动幅度（px）</summary>
    private const float animation_amplitude = 15f;

    /// <summary>单向移动时长（ms），来回一个完整周期 = 2 × duration</summary>
    private const double animation_duration = 2000;

    [BackgroundDependencyLoader]
    private void load()
    {
        AutoSizeAxes = Axes.Both;
        for (int i = 0; i < s_milestones.Length; i++)
        {
            var texture = skin.GetTexture($"ComboSprite/combo-{s_milestones[i]:D4}");
            milestoneContainers[i] = new Container
            {
                AutoSizeAxes = Axes.Both,
                Alpha = i == 0 ? 1 : 0,
                Child = texture != null ? new Sprite { Texture = texture } : Empty(),
            };
            AddInternal(milestoneContainers[i]);
        }
    }

    private float animationBaseY;

    protected override void LoadComplete()
    {
        base.LoadComplete();

        animationBaseY = Y;
        StartAnimation();

        scoreProcessor.Combo.BindValueChanged(combo =>
        {
            int idx = 0;
            for (int i = s_milestones.Length - 1; i >= 0; i--)
            {
                if (combo.NewValue >= s_milestones[i]) { idx = i; break; }
            }
            for (int i = 0; i < milestoneContainers.Length; i++)
                milestoneContainers[i].Alpha = i == idx ? 1 : 0;
        }, true);
    }

    /// <summary>
    /// 启动缓动循环动画（正弦波驱动，可靠无漂移）。
    /// </summary>
    private void StartAnimation()
    {
        animationBaseY = Y;
        // 将 ComboSwitcher 加入每帧 Update，手动计算正弦偏移
    }

    protected override void Update()
    {
        base.Update();
        if (animationBaseY == 0 && Y != 0)
            animationBaseY = Y;
        if (animationBaseY != 0)
        {
            double t = Clock.CurrentTime / animation_duration * Math.PI;
            Y = animationBaseY + (float)(Math.Sin(t) * animation_amplitude);
        }
    }
}

public class ManiaEzProForNeriTransformer : SkinTransformer
{
    // ═══════════════════════════════════════════════════════════════════════
    // 硬编码配置常量（脱离游戏内配置）
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 按 Key 数独立配置列宽、Scratch 列、Pedal 列、Hidden 列。
    /// Key = 总列数<br/>
    /// 列宽计算：Default=ColumnWidth, Scratch=×ScratchFactor, Pedal=×PedalFactor, Hidden=×HiddenFactor
    /// </summary>
    private static readonly Dictionary<int, KeyColumnConfig> keyConfigs = new()
    {
        [4]  = new KeyColumnConfig(64f),                                                     // 4K
        [5]  = new KeyColumnConfig(64f),                                                     // 5K
        [6]  = new KeyColumnConfig(64f),                                                     // 6K
        [7]  = new KeyColumnConfig(64f),                                                     // 7K
        [8]  = new KeyColumnConfig(60f),                                                     // 8K
        [9]  = new KeyColumnConfig(53f),                                                     // 9K
        [10] = new KeyColumnConfig(48f),                                                     // 10K
        [12] = new KeyColumnConfig(48f, new[] { 0, 11 }, 1.2f),                              // 10K2S: 列0,11 Scratch
        [14] = new KeyColumnConfig(48f, new[] { 0, 12 }, 1.2f, new[] { 6 }, 1.2f, new[] { 13 }),  // 10K2S1P: 列0,12 Scratch; 列6 Pedal; 列13 Hidden(宽0)
        [16] = new KeyColumnConfig(40f, new[] { 0, 15 }, 1.2f),
        [18] = new KeyColumnConfig(36f, new[] { 0, 16 }, 1.2f, new[] { 8 }, 1.2f, new[] { 17 }),  // 14K2S1P: 列0,16 Scratch; 列8 Pedal; 列17 Hidden(宽0)
    };

    /// <summary>未知 Key 数时的回退配置</summary>
    private static readonly KeyColumnConfig defaultKeyConfig = new(64f);

    /// <summary>
    /// 按 Key 数独立配置各列的颜色类型（A/B/S/E/P），决定使用的 Note 图组。
    /// ColorPrefix 映射：A/E → "white"(白), B → "blue"(蓝), S/P → "green"(绿)
    /// </summary>
    private static readonly Dictionary<int, EzColumnType[]> columnTypeConfigs = new()
    {
        [4]  = new[] { EzColumnType.A, EzColumnType.B, EzColumnType.B, EzColumnType.A },
        [5]  = new[] { EzColumnType.A, EzColumnType.B, EzColumnType.P, EzColumnType.B, EzColumnType.A },
        [6]  = new[] { EzColumnType.A, EzColumnType.B, EzColumnType.A, EzColumnType.A, EzColumnType.B, EzColumnType.A },
        [7]  = new[] { EzColumnType.A, EzColumnType.B, EzColumnType.A, EzColumnType.P, EzColumnType.A, EzColumnType.B, EzColumnType.A },
        [8]  = new[] { EzColumnType.A, EzColumnType.B, EzColumnType.A, EzColumnType.B, EzColumnType.B, EzColumnType.A, EzColumnType.B, EzColumnType.A },
        [9]  = new[] { EzColumnType.A, EzColumnType.B, EzColumnType.A, EzColumnType.B, EzColumnType.P, EzColumnType.B, EzColumnType.A, EzColumnType.B, EzColumnType.A },
        [10] = new[] { EzColumnType.A, EzColumnType.B, EzColumnType.A, EzColumnType.B, EzColumnType.A, EzColumnType.A, EzColumnType.B, EzColumnType.A, EzColumnType.B, EzColumnType.A },
        [12] = new[] { EzColumnType.S, EzColumnType.A, EzColumnType.B, EzColumnType.A, EzColumnType.B, EzColumnType.A, EzColumnType.A, EzColumnType.B, EzColumnType.A, EzColumnType.B, EzColumnType.A, EzColumnType.S },
        [14] = new[] { EzColumnType.S, EzColumnType.A, EzColumnType.B, EzColumnType.A, EzColumnType.B, EzColumnType.A, EzColumnType.P, EzColumnType.A, EzColumnType.B, EzColumnType.A, EzColumnType.B, EzColumnType.A, EzColumnType.S, EzColumnType.B },
        [16] = new[] { EzColumnType.S, EzColumnType.A, EzColumnType.B, EzColumnType.A, EzColumnType.B, EzColumnType.A, EzColumnType.S, EzColumnType.S, EzColumnType.S, EzColumnType.S, EzColumnType.A, EzColumnType.B, EzColumnType.A, EzColumnType.B, EzColumnType.A, EzColumnType.S },
        [18] = new[] { EzColumnType.S, EzColumnType.A, EzColumnType.B, EzColumnType.A, EzColumnType.B, EzColumnType.A, EzColumnType.S, EzColumnType.S, EzColumnType.P, EzColumnType.S, EzColumnType.S, EzColumnType.A, EzColumnType.B, EzColumnType.A, EzColumnType.B, EzColumnType.A, EzColumnType.S, EzColumnType.A },
    };

    /// <summary>获取指定 Key 数的列类型完整映射</summary>
    public static EzColumnType[] GetColumnTypes(int keyMode)
        => columnTypeConfigs.TryGetValue(keyMode, out var types) ? types : Array.Empty<EzColumnType>();

    /// <summary>按 Key 数的列配置结构（四种轨道类型：Default / Scratch / Pedal / Hidden）</summary>
    private readonly struct KeyColumnConfig
    {
        public readonly float ColumnWidth;
        public readonly int[]? ScratchColumnIndices;  // null = 无 Scratch
        public readonly float ScratchFactor;
        public readonly int[]? PedalColumnIndices;    // null = 无 Pedal
        public readonly float PedalFactor;
        public readonly int[]? HiddenColumnIndices;   // null = 无 Hidden
        public readonly float HiddenFactor;

        public KeyColumnConfig(float columnWidth,
            int[]? scratchColumnIndices = null, float scratchFactor = 1f,
            int[]? pedalColumnIndices = null, float pedalFactor = 1f,
            int[]? hiddenColumnIndices = null, float hiddenFactor = 0f)
        {
            ColumnWidth = columnWidth;
            ScratchColumnIndices = scratchColumnIndices;
            ScratchFactor = scratchFactor;
            PedalColumnIndices = pedalColumnIndices;
            PedalFactor = pedalFactor;
            HiddenColumnIndices = hiddenColumnIndices;
            HiddenFactor = hiddenFactor;
        }
    }

    private const float hitPositionValue = 110f;      // 判定线距底部（px）
    private const int stage_padding_bottom = 0;

    private readonly ManiaBeatmap beatmap;

    public ManiaEzProForNeriTransformer(ISkin skin, IBeatmap beatmap)
        : base(skin)
    {
        this.beatmap = (ManiaBeatmap)beatmap;
        // 不再从 Ez2ConfigManager 读取任何配置 —— 全部使用上方硬编码常量
    }

    public override Drawable? GetDrawableComponent(ISkinComponentLookup lookup)
    {
        switch (lookup)
        {
            case ManiaSkinComponentLookup maniaComponent:
                switch (maniaComponent.Component)
                {
                    case ManiaSkinComponents.ColumnBackground:
                        return new EzProNeriColumnBackground();
                    case ManiaSkinComponents.KeyArea:
                        return new EzKeyArea();
                    case ManiaSkinComponents.Note:
                        return new EzNote();
                    case ManiaSkinComponents.HitTarget:
                        return new EzHitTarget();
                    case ManiaSkinComponents.HitExplosion:
                        return new EzHitExplosion();
                    case ManiaSkinComponents.HoldNoteHead:
                        return new EzHoldNoteHead();
                    case ManiaSkinComponents.HoldNoteBody:
                        return new EzHoldNoteMiddle();
                    case ManiaSkinComponents.HoldNoteTail:
                        return new EzHoldNoteTail();
                    case ManiaSkinComponents.StageBackground:
                        return new EzStageBottom();
                    case ManiaSkinComponents.StageForeground:
                        return new EzJudgementLine();
                }
                break;

            case GlobalSkinnableContainerLookup containerLookup:
                if (containerLookup.Ruleset == null)
                    return base.GetDrawableComponent(lookup);

                switch (containerLookup.Lookup)
                {
                    case GlobalSkinnableContainers.MainHUDComponents:
                        return new DefaultSkinComponentsContainer(container =>
                        {
                            var hitTiming = container.ChildrenOfType<EzHUDHitTiming>().ToArray();
                            if (hitTiming.Length >= 2)
                            {
                                hitTiming[0].Anchor = Anchor.Centre;
                                hitTiming[0].Origin = Anchor.Centre;
                                hitTiming[0].X = -500;
                                hitTiming[0].AloneShow.Value = AloneShowMenu.Early;
                                hitTiming[1].Anchor = Anchor.Centre;
                                hitTiming[1].Origin = Anchor.Centre;
                                hitTiming[1].X = 500;
                                hitTiming[1].AloneShow.Value = AloneShowMenu.Late;
                            }

                            var comboTitle = container.ChildrenOfType<EzHUDComboTitle>().FirstOrDefault();

                            if (comboTitle != null)
                            {
                                comboTitle.Anchor = Anchor.TopCentre;
                                comboTitle.Origin = Anchor.Centre;
                                comboTitle.Y = 190;
                            }

                            var combos = container.ChildrenOfType<EzHUDComboCounter>().ToArray();

                            if (combos.Length >= 2)
                            {
                                var combo1 = combos[0];
                                var combo2 = combos[1];

                                combo1.Anchor = Anchor.TopCentre;
                                combo1.Origin = Anchor.TopCentre;
                                combo1.Y = 200;
                                combo1.AccentAlpha.Value = 0.8f;
                                combo1.EffectStartFactor.Value = 1.5f;
                                combo1.EffectEndFactor.Value = 1f;
                                combo1.EffectStartTime.Value = 10;
                                combo1.EffectEndDuration.Value = 500;

                                combo2.Anchor = Anchor.TopCentre;
                                combo2.Origin = Anchor.TopCentre;
                                combo2.Y = 200;
                                combo2.AccentAlpha.Value = 0.4f;
                                combo2.EffectStartFactor.Value = 2.5f;
                                combo2.EffectEndFactor.Value = 1f;
                                combo2.EffectStartTime.Value = 10;
                                combo2.EffectEndDuration.Value = 300;
                            }

                            var keyCounter = container.ChildrenOfType<EzHUDKeyCounterDisplay>().FirstOrDefault();
                            var columnHitErrorMeter = container.OfType<EzHUDHitTimingColumns>().FirstOrDefault();

                            if (keyCounter != null)
                            {
                                keyCounter.Anchor = Anchor.BottomCentre;
                                keyCounter.Origin = Anchor.TopCentre;
                                keyCounter.Position = new Vector2(0, -hitPositionValue - stage_padding_bottom);
                            }

                            if (columnHitErrorMeter != null)
                            {
                                columnHitErrorMeter.Anchor = Anchor.BottomCentre;
                                columnHitErrorMeter.Origin = Anchor.Centre;
                                columnHitErrorMeter.Position = new Vector2(0, -hitPositionValue - stage_padding_bottom);
                            }

                            var hitErrorMeter = container.OfType<BarHitErrorMeter>().FirstOrDefault();

                            if (hitErrorMeter != null)
                            {
                                hitErrorMeter.Anchor = Anchor.Centre;
                                hitErrorMeter.Origin = Anchor.Centre;
                                hitErrorMeter.Rotation = -90f;
                                hitErrorMeter.Position = new Vector2(0, -15);
                                hitErrorMeter.Scale = new Vector2(1.25f, 1.25f);
                                hitErrorMeter.JudgementLineThickness.Value = 2;
                                hitErrorMeter.ShowMovingAverage.Value = true;
                                hitErrorMeter.ColourBarVisibility.Value = false;
                                hitErrorMeter.CentreMarkerStyle.Value = BarHitErrorMeter.CentreMarkerStyles.Circle;
                                hitErrorMeter.LabelStyle.Value = BarHitErrorMeter.LabelStyles.None;
                            }

                            var judgementPiece = container.OfType<EzHUDHitResultScore>().FirstOrDefault();

                            if (judgementPiece != null)
                            {
                                judgementPiece.Anchor = Anchor.Centre;
                                judgementPiece.Origin = Anchor.Centre;
                                judgementPiece.Y = 100;
                            }

                            var o2PillBar = container.OfType<O2PillBar>().FirstOrDefault();
                        })
                        {
                            new EzHUDComboTitle(),
                            new EzHUDComboCounter(),
                            new EzHUDComboCounter(),
                            new EzHUDKeyCounterDisplay(),
                            new EzHUDHitTimingColumns(),
                            new BarHitErrorMeter(),
                            new EzHUDHitResultScore(),
                            new EzHUDHitTiming(),
                            new EzHUDHitTiming(),
                            new EzHUDO2JamPillFlow
                            {
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                            },
                        };

                    case GlobalSkinnableContainers.Playfield:
                        return new DefaultSkinComponentsContainer(container =>
                        {
                            var comboTitle = container.OfType<EzHUDComboTitle>().FirstOrDefault();
                            if (comboTitle != null)
                            {
                                comboTitle.Width = 240;
                                comboTitle.Height = 60;
                                comboTitle.Anchor = Anchor.TopCentre;
                                comboTitle.Origin = Anchor.TopCentre;
                                comboTitle.Position = new Vector2(0, 90);
                                comboTitle.Scale = new Vector2(3.5f);
                                //comboTitle.ThemeName.Value = (EzEnumGameThemeName)35;
                            }

                            var comboCounter = container.OfType<EzHUDComboCounter>().FirstOrDefault();
                            if (comboCounter != null)
                            {
                                comboCounter.Anchor = Anchor.Centre;
                                comboCounter.Origin = Anchor.TopCentre;
                                comboCounter.Position = new Vector2(0, -300);
                                comboCounter.Scale = new Vector2(3.5f);
                                comboCounter.ThemeName.Value = (EzEnumGameThemeName)35;
                                comboCounter.EffectStartFactor.Value = 1.5f;
                                comboCounter.EffectEndFactor.Value = 1f;
                                comboCounter.EffectStartTime.Value = 10;
                                comboCounter.EffectEndDuration.Value = 300;
                                comboCounter.AccentAlpha.Value = 0.7f;
                            }

                            var hitTimings = container.ChildrenOfType<EzHUDHitTiming>().ToArray();
                            if (hitTimings.Length >= 2)
                            {
                                hitTimings[0].Width = 300;
                                hitTimings[0].Height = 80;
                                hitTimings[0].Anchor = Anchor.TopCentre;
                                hitTimings[0].Origin = Anchor.TopCentre;
                                hitTimings[0].Position = new Vector2(-2, -19);
                                hitTimings[0].Scale = new Vector2(2.1422572f);
                                hitTimings[0].AloneShow.Value = AloneShowMenu.None;
                                hitTimings[0].Threshold.Value = 16;
                                hitTimings[0].DisplayDuration.Value = 300;
                                hitTimings[0].SymmetryOffset.Value = 26;
                                hitTimings[0].TextAlpha.Value = 0.65f;
                                hitTimings[0].NumberAlpha.Value = 0;

                                hitTimings[1].Width = 300;
                                hitTimings[1].Height = 80;
                                hitTimings[1].Anchor = Anchor.TopCentre;
                                hitTimings[1].Origin = Anchor.TopCentre;
                                hitTimings[1].Position = new Vector2(0, 24);
                                hitTimings[1].Scale = new Vector2(1.0913806f);
                                hitTimings[1].AloneShow.Value = AloneShowMenu.Late;
                                hitTimings[1].Threshold.Value = 22;
                                hitTimings[1].DisplayDuration.Value = 300;
                                hitTimings[1].SymmetryOffset.Value = 60;
                                hitTimings[1].TextAlpha.Value = 0;
                                hitTimings[1].NumberAlpha.Value = 0.7f;
                            }

                            var hitResultScore = container.OfType<EzHUDHitResultScore>().FirstOrDefault();
                            if (hitResultScore != null)
                            {
                                hitResultScore.Width = 200;
                                hitResultScore.Height = 50;
                                hitResultScore.Anchor = Anchor.Centre;
                                hitResultScore.Origin = Anchor.Centre;
                                hitResultScore.Position = new Vector2(0, 75);
                                hitResultScore.Scale = new Vector2(2.2658894f);
                                hitResultScore.ThemeName.Value = (EzEnumGameThemeName)33;
                                hitResultScore.FullComboEffectEnabled.Value = true;
                            }

                            var switcher = container.OfType<ComboMilestoneSwitcher>().FirstOrDefault();
                            if (switcher != null)
                            {
                                switcher.Anchor = Anchor.TopRight;
                                switcher.Origin = (Anchor)9;
                                switcher.Position = new Vector2(-135f, 20f);
                                switcher.Scale = new Vector2(1.5f);
                            }

                        })
                        {
                            new EzHUDHitTiming(),
                            new EzHUDHitTiming(),
                            new EzHUDComboTitle(),
                            new EzHUDComboCounter(),
                            new EzHUDHitResultScore(),
                            new ComboMilestoneSwitcher(),
                        };

                }

                return null;

            case SkinComponentLookup<HitResult>:
                return Drawable.Empty();
        }

        return base.GetDrawableComponent(lookup);
    }

    #region GetConfig — 手动列宽配置

    private float columnWidth;

    /// <summary>
    /// 解析当前 Key 数的列配置，未注册的 Key 数回退到 defaultKeyConfig。
    /// </summary>
    private static KeyColumnConfig resolveConfig(int totalColumns)
        => keyConfigs.TryGetValue(totalColumns, out var cfg) ? cfg : defaultKeyConfig;

    public override IBindable<TValue>? GetConfig<TLookup, TValue>(TLookup lookup)
    {
        if (lookup is ManiaSkinConfigurationLookup maniaLookup)
        {
            // 在这里切换不同key、不同列的配置。
            // 受支持程度见 LegacyManiaSkinConfigurationLookups. 中的列表
            int columnIndex = maniaLookup.ColumnIndex ?? 0;
            var stage = beatmap.GetStageForColumnIndex(columnIndex);

            // 按 Key 数查找配置：Default=ColumnWidth, Scratch=×ScratchFactor, Pedal=×PedalFactor, Hidden=×HiddenFactor
            var config = resolveConfig(stage.Columns);
            float factor = 1f;
            if (config.ScratchColumnIndices?.Contains(columnIndex) == true)
                factor = config.ScratchFactor;
            else if (config.PedalColumnIndices?.Contains(columnIndex) == true)
                factor = config.PedalFactor;
            else if (config.HiddenColumnIndices?.Contains(columnIndex) == true)
                factor = config.HiddenFactor;
            columnWidth = config.ColumnWidth * factor;

            switch (maniaLookup.Lookup)
            {
                case LegacyManiaSkinConfigurationLookups.ColumnWidth:
                    return SkinUtils.As<TValue>(new Bindable<float>(columnWidth));

                case LegacyManiaSkinConfigurationLookups.HitPosition:
                    return SkinUtils.As<TValue>(new Bindable<float>(hitPositionValue));

                case LegacyManiaSkinConfigurationLookups.BarLineHeight:
                    return SkinUtils.As<TValue>(new Bindable<float>(1));

                case LegacyManiaSkinConfigurationLookups.LeftColumnSpacing:
                case LegacyManiaSkinConfigurationLookups.RightColumnSpacing:
                    return SkinUtils.As<TValue>(new Bindable<float>());

                case LegacyManiaSkinConfigurationLookups.StagePaddingBottom:
                    return SkinUtils.As<TValue>(new Bindable<float>());

                case LegacyManiaSkinConfigurationLookups.StagePaddingTop:
                    return SkinUtils.As<TValue>(new Bindable<float>());
            }
        }

        return base.GetConfig<TLookup, TValue>(lookup);
    }

    #endregion
}
