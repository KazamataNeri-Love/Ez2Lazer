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
using osu.Game.Rulesets.Mania.Skinning.EzStylePro;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Logging;

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

// ═══════════════════════════════════════════════════════════════════════════
// EzProNeriEnergySystem — SuperTime Energy 机制
// ═══════════════════════════════════════════════════════════════════════════

public class EzProNeriEnergySystem : Component
{
    [Resolved(canBeNull: true)]
    private JudgementCountController? judgementCountController { get; set; }

    public double Energy { get; private set; }
    public double EnergyS { get; private set; }
    public readonly BindableBool IsSuperTime = new BindableBool();
    /// <summary>EnergyS 达到 1000 时置 true，消费后手动重置</summary>
    public readonly BindableBool FeverUpReady = new BindableBool();

    public static EzProNeriEnergySystem? Instance { get; private set; }

    private const double MAX_ENERGY = 1000;

    private static readonly Dictionary<HitResult, int> s_energyPerJudgement = new Dictionary<HitResult, int>
    {
        [HitResult.Perfect] = +5,
        [HitResult.Great]   = +1,
        [HitResult.Meh]     = -5,
        [HitResult.Miss]    = -20,
        [HitResult.Poor]    = -10,
    };

    protected override void LoadComplete()
    {
        base.LoadComplete();
        Instance = this;
        Logger.Log($"[EnergySys] LoadComplete — jcc is {(judgementCountController is null ? "NULL" : "OK")}, counters={judgementCountController?.Counters?.Count() ?? 0}");
        if (judgementCountController is null) return;
        int bound = 0;
        foreach (var counter in judgementCountController.Counters)
        {
            if (counter.Types.Length == 0) continue;
            var key = counter.Types.First();
            if (!s_energyPerJudgement.TryGetValue(key, out int per)) continue;
            counter.ResultCount.BindValueChanged(args =>
            {
                int delta = (args.NewValue - args.OldValue) * per;
                if (delta == 0) return;

                if (IsSuperTime.Value)
                {
                    if (delta > 0)
                    {
                        // 正向判定 → EnergyS 累计
                        EnergyS += delta;
                        if (EnergyS >= MAX_ENERGY)
                        {
                            EnergyS -= MAX_ENERGY;  // 扣除满额，允许再次累计
                            FeverUpReady.Value = true;
                        }
                    }
                    else
                    {
                        // 负向判定 → 结束超神时间，Energy 扣减，EnergyS 清零
                        IsSuperTime.Value = false;
                        Energy = Math.Max(0, Energy + delta);
                        EnergyS = 0;
                    }
                    return;
                }

                // 非超神时间：正常累计 Energy
                double newEnergy = Math.Clamp(Energy + delta, 0, MAX_ENERGY);
                if (newEnergy >= MAX_ENERGY && Energy < MAX_ENERGY)
                {
                    Energy = MAX_ENERGY;
                    IsSuperTime.Value = true;
                    EnergyS = 0;
                }
                else
                {
                    Energy = newEnergy;
                }
            }, true);
            bound++;
        }
        Logger.Log($"[EnergySys] bound {bound} counters, Energy={Energy}");
    }

    protected override void Update()
    {
        base.Update();
        if (!IsSuperTime.Value && Energy >= MAX_ENERGY)
        {
            IsSuperTime.Value = true;
            Energy = MAX_ENERGY;
            EnergyS = 0;
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// EzProNeriEnergyGauge — Energy 积累程度显示（Playfield 层）
// ═══════════════════════════════════════════════════════════════════════════
// 三层叠加：BG（底）→ Light（中）→ FeverEnergy（顶）
// Light 和 FeverEnergy 的显示比例从 1% 到 100%，随 Energy 增长线性展开。
// ═══════════════════════════════════════════════════════════════════════════

public partial class EzProNeriEnergyGauge : CompositeDrawable
{
    [Resolved]
    private ISkinSource skin { get; set; } = null!;

    private EzProNeriEnergySystem? energySystem;
    private Container? feverMask;
    private float maskFullHeight;

    /// <summary>公开给 Playfield 回调，用于绑定 SuperTime 事件</summary>
    public EzProNeriEnergySystem? EnergySystem => energySystem;

    [BackgroundDependencyLoader]
    private void load()
    {
        // 全显式尺寸：三张图严格 43×1000，无需任何 RelativeSizeAxes，杜绝一切布局冲突
        RelativeSizeAxes = Axes.None;

        energySystem = new EzProNeriEnergySystem();
        AddInternal(energySystem);

        var bgTex  = skin.GetTexture("SuperTime/FeverEnergyBG");
        var lTex   = skin.GetTexture("SuperTime/FeverEnergyLight");
        var fevTex = skin.GetTexture("SuperTime/FeverEnergy");

        float w = 0; maskFullHeight = 0;
        if (bgTex != null)  { w = Math.Max(w, bgTex.Width);   maskFullHeight = Math.Max(maskFullHeight, bgTex.Height); }
        if (lTex != null)   { w = Math.Max(w, lTex.Width);    maskFullHeight = Math.Max(maskFullHeight, lTex.Height); }
        if (fevTex != null) { w = Math.Max(w, fevTex.Width);  maskFullHeight = Math.Max(maskFullHeight, fevTex.Height); }

        Size = new Vector2(w, maskFullHeight);

        // 底层：BG（显式 Size = gauge 尺寸，完整填充）
        if (bgTex != null)
            AddInternal(new Sprite { Texture = bgTex, Size = new Vector2(w, maskFullHeight), Anchor = Anchor.BottomCentre, Origin = Anchor.BottomCentre });

        // 中层：Light（始终完整显示，同 BG）
        if (lTex != null)
            AddInternal(new Sprite { Texture = lTex, Size = new Vector2(w, maskFullHeight), Anchor = Anchor.BottomCentre, Origin = Anchor.BottomCentre });

        // 顶层：FeverEnergy（遮罩控制高度，初始 exposed = maskFullHeight * 1%）
        if (fevTex != null)
        {
            feverMask = new Container
            {
                Masking = true,
                Size = new Vector2(w, maskFullHeight),
                Height = maskFullHeight * 0.01f,
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre,
                Child = new Sprite { Texture = fevTex, Size = new Vector2(w, maskFullHeight), Anchor = Anchor.BottomCentre, Origin = Anchor.BottomCentre }
            };
            AddInternal(feverMask);
        }

        Logger.Log($"[EnergyGauge] AllExplicit Size=({Size.X},{Size.Y}) w={w} maskFullH={maskFullHeight} bg={bgTex!=null} l={lTex!=null} fev={fevTex!=null}");
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();
        Logger.Log($"[EnergyGauge] LoadComplete — energySystem={energySystem is not null}");
    }

    protected override void Update()
    {
        base.Update();
        if (energySystem is null || feverMask is null) return;

        float ratio = (float)Math.Max(0.01, energySystem.Energy / 1000.0);
        float targetH = maskFullHeight * ratio;
        feverMask.Height = targetH;
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// EzProNeriFeverUp — SuperTime 激活全屏特效（Playfield 层）
// ═══════════════════════════════════════════════════════════════════════════
// 宽度 = Playfield 宽度；从 Playfield 底部匀速上升一个屏幕高度，耗时 4 秒。
// ═══════════════════════════════════════════════════════════════════════════

public partial class EzProNeriFeverUp : CompositeDrawable
{
    [Resolved]
    private ISkinSource skin { get; set; } = null!;

    private Sprite? sprite;
    private float imageHeight;

    [BackgroundDependencyLoader]
    private void load()
    {
        // 填满 Playfield，确保子 Sprite 的 BottomCentre 锚点 = Playfield 的 BottomCentre
        RelativeSizeAxes = Axes.Both;

        var tex = skin.GetTexture("SuperTime/FeverUp");
        if (tex != null)
        {
            imageHeight = tex.Height;
            sprite = new Sprite
            {
                Texture = tex,
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre,
                Y = imageHeight,   // 初始：图片底边紧贴 Playfield 底边
                Alpha = 0,
                Blending = BlendingParameters.Additive,
            };
            AddInternal(sprite);
            Logger.Log($"[FeverUp] loaded h={imageHeight}");
        }
        else
        {
            Logger.Log("[FeverUp] texture NOT found");
        }
    }

    /// <summary>SuperTime 激活时由 Playfield 回调调用</summary>
    public void Trigger()
    {
        if (sprite is null) return;

        // 清除上一次可能残留的 transform
        sprite.ClearTransforms();

        float pw = DrawWidth;
        float ph = DrawHeight;

        sprite.Width = pw;
        sprite.Y = 0;
        sprite.Alpha = 1;
        sprite.MoveToY(-ph, 400, Easing.None);

        Logger.Log($"[FeverUp] Triggered! w={pw} h={ph} Y: 0 → {-ph}");
    }

    /// <summary>SuperTime 结束时复位，确保下次可以再次触发</summary>
    public void Reset()
    {
        if (sprite is null) return;
        sprite.ClearTransforms();
        sprite.Y = imageHeight;
        sprite.Alpha = 0;
        Logger.Log("[FeverUp] Reset");
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// EzProNeriFeverLight — SuperTime BPM 同步闪烁光效（Playfield 层）
// ═══════════════════════════════════════════════════════════════════════════
// 从谱面开始持续计算 BPM 相位但不可见；进入 SuperTime 时瞬间打开，闪烁已与节拍同步。
// ═══════════════════════════════════════════════════════════════════════════

public partial class EzProNeriFeverLight : CompositeDrawable
{
    [Resolved]
    private ISkinSource skin { get; set; } = null!;

    private Sprite? sprite;
    private bool visible;
    private bool justShown;
    /// <summary>由 Playfield 回调根据谱面设置</summary>
    public float Bpm { get; set; } = 150;

    [BackgroundDependencyLoader]
    private void load()
    {
        AutoSizeAxes = Axes.Both;

        var tex = skin.GetTexture("SuperTime/FeverLight");
        if (tex != null)
        {
            sprite = new Sprite
            {
                Texture = tex,
                Alpha = 0,
                Blending = BlendingParameters.Additive,
            };
            AddInternal(sprite);
            Logger.Log($"[FeverLight] loaded");
        }
        else
        {
            Logger.Log("[FeverLight] texture NOT found");
        }
    }

    public void Show()
    {
        visible = true;
        justShown = true;
        if (sprite != null) sprite.Alpha = 1;
    }

    public void Hide()
    {
        visible = false;
        justShown = false;
        if (sprite != null) sprite.Alpha = 0;
    }

    protected override void Update()
    {
        base.Update();
        if (sprite is null || !visible) return;

        if (justShown)
        {
            // 首个可见帧保持 Alpha=1，下一帧开始闪烁
            justShown = false;
            return;
        }

        double beatMs = 60000.0 / Bpm;
        double phase = (Clock.CurrentTime % beatMs) / beatMs;
        // sin 波形，0..1 平滑闪烁，频率 = BPM（每拍一个周期）
        sprite.Alpha = (float)((Math.Sin(phase * 2 * Math.PI) + 1) / 2);
    }
}
// ═══════════════════════════════════════════════════════════════════════════
// EzProNeriBoxlightAnimator — 全屏 BPM 同步帧动画（Background 层）
// ═══════════════════════════════════════════════════════════════════════════
// 加载 boxlight-0.png … boxlight-N.png 序列。
// 两个 Sprite 交替播放，每周期 = 4 拍，偏移 2 拍，实现无缝循环。
// 高度拉伸至屏幕高度，保持比例。无帧时静默跳过。
// ═══════════════════════════════════════════════════════════════════════════

public partial class EzProNeriBoxlightAnimator : CompositeDrawable
{
    [Resolved]
    private ISkinSource skin { get; set; } = null!;

    private readonly List<Texture> frames = new List<Texture>();
    private Sprite? sprite;
    private bool hasFrames;
    private bool sizeInitialized;
    public float Bpm { get; set; } = 150;

    [BackgroundDependencyLoader]
    private void load()
    {
        for (int i = 0; ; i++)
        {
            var tex = skin.GetTexture($"SuperTime/boxlight-{i:D3}");
            if (tex == null) break;
            frames.Add(tex);
        }

        if (frames.Count == 0)
        {
            Logger.Log("[Boxlight] ⚠ No frames loaded — disabled");
            return;
        }

        hasFrames = true;
        Logger.Log($"[Boxlight] Loaded {frames.Count} frames, first={frames[0].Width}×{frames[0].Height}");

        sprite = new Sprite
        {
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
        };
        AddInternal(sprite);
        sprite.Texture = frames[0];
    }

    protected override void Update()
    {
        base.Update();
        if (!hasFrames || sprite == null) return;

        if (!sizeInitialized)
        {
            sizeInitialized = true;
            float parentW = Parent?.DrawWidth ?? 1024;
            var refTex = frames[0];
            float scale = parentW / refTex.Width * 1.50f;
            sprite.Size = new Vector2(refTex.Width * scale, refTex.Height * scale);
        }

        double beatMs = 60000.0 / Bpm;
        double phase = (Clock.CurrentTime % (beatMs * 4)) / (beatMs * 4);
        sprite.Texture = frames[(int)(phase * frames.Count) % frames.Count];
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// EzProNeriBoxgoldAnimator — SuperTime BPM 同步帧动画（Background 层）
// ═══════════════════════════════════════════════════════════════════════════
// 加载 boxgold-000.png … 序列。仅 SuperTime 激活时播放。
// 每周期 = 2 拍，偏移 1 拍，两个 Sprite 重叠播放。
// ═══════════════════════════════════════════════════════════════════════════

public partial class EzProNeriBoxgoldAnimator : CompositeDrawable
{
    [Resolved]
    private ISkinSource skin { get; set; } = null!;

    private readonly List<Texture> frames = new List<Texture>();
    private readonly Sprite[] altSprites = new Sprite[2];
    private bool hasFrames;
    private bool sizeInitialized;
    public float Bpm { get; set; } = 150;

    [BackgroundDependencyLoader]
    private void load()
    {
        for (int i = 0; ; i++)
        {
            var tex = skin.GetTexture($"SuperTime/boxgold-{i:D3}");
            if (tex == null) break;
            frames.Add(tex);
        }

        if (frames.Count == 0)
        {
            Logger.Log("[Boxgold] ⚠ No frames loaded — disabled");
            return;
        }

        hasFrames = true;
        Logger.Log($"[Boxgold] Loaded {frames.Count} frames");

        for (int i = 0; i < 2; i++)
        {
            altSprites[i] = new Sprite
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Alpha = 0,
                Scale = i == 1 ? new Vector2(-1, 1) : Vector2.One,
            };
            AddInternal(altSprites[i]);
        }

        altSprites[0].Texture = frames[0];
        altSprites[1].Texture = frames[frames.Count / 2];
    }

    protected override void Update()
    {
        base.Update();
        if (!hasFrames) return;

        bool superTime = EzProNeriEnergySystem.Instance?.IsSuperTime.Value ?? false;

        if (!superTime)
        {
            altSprites[0].Alpha = 0;
            altSprites[1].Alpha = 0;
            return;
        }

        if (!sizeInitialized)
        {
            sizeInitialized = true;
            float parentW = Parent?.DrawWidth ?? 1024;
            var refTex = frames[0];
            float scale = parentW / refTex.Width;
            foreach (var s in altSprites)
                s.Size = new Vector2(parentW, refTex.Height * scale);
        }

        double beatMs = 60000.0 / Bpm;
        double totalMs = Clock.CurrentTime;
        double cycleMs = beatMs * 8;      // 8 拍完成一次完整帧序列
        double halfMs  = beatMs * 4;      // 每 4 拍启动一个新动画元件

        double phaseA = (totalMs % cycleMs) / cycleMs;
        double phaseB = ((totalMs + halfMs) % cycleMs) / cycleMs;

        altSprites[0].Texture = frames[(int)(phaseA * frames.Count) % frames.Count];
        altSprites[1].Texture = frames[(int)(phaseB * frames.Count) % frames.Count];

        altSprites[0].Alpha = 1;
        altSprites[1].Alpha = 1;
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
    };

    /// <summary>未知 Key 数时的回退配置</summary>
    private static readonly KeyColumnConfig defaultKeyConfig = new(64f);

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
                        return new EzColumnBackground();
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
                // Background 不依赖 Ruleset，优先处理
                if (containerLookup.Lookup == GlobalSkinnableContainers.Background)
                {
                    return new DefaultSkinComponentsContainer(container =>
                    {
                        var boxlight = container.OfType<EzProNeriBoxlightAnimator>().FirstOrDefault();
                        if (boxlight != null)
                        {
                            boxlight.Anchor = Anchor.Centre;
                            boxlight.Origin = Anchor.Centre;

                            double bpm = 150;
                            try
                            {
                                var timingPoints = beatmap.ControlPointInfo?.TimingPoints;
                                if (timingPoints != null && timingPoints.Any(t => t.BeatLength > 0))
                                {
                                    var firstTiming = timingPoints.First(t => t.BeatLength > 0);
                                    bpm = 60000.0 / firstTiming.BeatLength;
                                }
                            }
                            catch { }

                            boxlight.Bpm = (float)bpm;
                        }

                        var boxgold = container.OfType<EzProNeriBoxgoldAnimator>().FirstOrDefault();
                        if (boxgold != null)
                        {
                            boxgold.Anchor = Anchor.Centre;
                            boxgold.Origin = Anchor.Centre;

                            double bpm = 150;
                            try
                            {
                                var timingPoints = beatmap.ControlPointInfo?.TimingPoints;
                                if (timingPoints != null && timingPoints.Any(t => t.BeatLength > 0))
                                {
                                    var firstTiming = timingPoints.First(t => t.BeatLength > 0);
                                    bpm = 60000.0 / firstTiming.BeatLength;
                                }
                            }
                            catch { }

                            boxgold.Bpm = (float)bpm;
                        }
                    })
                    {
                        new EzProNeriBoxlightAnimator(),
                        new EzProNeriBoxgoldAnimator(),
                    };
                }

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
                                comboTitle.Width = 120;
                                comboTitle.Height = 30;
                                comboTitle.Anchor = Anchor.TopCentre;
                                comboTitle.Origin = Anchor.TopCentre;
                                comboTitle.Position = new Vector2(0, 134);
                                comboTitle.Scale = new Vector2(1.8062992f);
                                //comboTitle.ThemeName.Value = (EzEnumGameThemeName)35;
                            }

                            var comboCounter = container.OfType<EzHUDComboCounter>().FirstOrDefault();
                            if (comboCounter != null)
                            {
                                comboCounter.Anchor = Anchor.Centre;
                                comboCounter.Origin = Anchor.TopCentre;
                                comboCounter.Position = new Vector2(0, -300);
                                comboCounter.Scale = new Vector2(2.552373f);
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

                            var energyGauge = container.OfType<EzProNeriEnergyGauge>().FirstOrDefault();
                            if (energyGauge != null)
                            {
                                energyGauge.Anchor = Anchor.BottomLeft;
                                energyGauge.Origin = Anchor.BottomRight;
                                energyGauge.Position = new Vector2(-8f, -10f);
                            }

                            // FeverUp 特效绑定：EnergyS 满 1000 时触发，SuperTime 结束时复位
                            var feverUp = container.OfType<EzProNeriFeverUp>().FirstOrDefault();
                            if (energyGauge?.EnergySystem != null && feverUp != null)
                            {
                                energyGauge.EnergySystem.FeverUpReady.BindValueChanged(fever =>
                                {
                                    if (fever.NewValue)
                                    {
                                        feverUp.Trigger();
                                        energyGauge.EnergySystem.FeverUpReady.Value = false;
                                    }
                                });

                                energyGauge.EnergySystem.IsSuperTime.BindValueChanged(super =>
                                {
                                    if (!super.NewValue)
                                        feverUp.Reset();
                                });
                            }

                            // FeverLight BPM 闪烁：从谱面获取 BPM，SuperTime 激活时显隐
                            var feverLights = container.ChildrenOfType<EzProNeriFeverLight>().ToArray();
                            if (feverLights.Length >= 2)
                            {
                                // 尝试从谱面获取 BPM
                                double bpm = 150;
                                try
                                {
                                    var timingPoints = beatmap.ControlPointInfo?.TimingPoints;
                                    if (timingPoints != null && timingPoints.Any(t => t.BeatLength > 0))
                                    {
                                        var firstTiming = timingPoints.First(t => t.BeatLength > 0);
                                        bpm = 60000.0 / firstTiming.BeatLength;
                                    }
                                }
                                catch { }

                                // 一号：左侧，Anchor=CentreLeft, Origin=CentreRight
                                feverLights[0].Bpm = (float)bpm;
                                feverLights[0].Anchor = Anchor.CentreLeft;
                                feverLights[0].Origin = Anchor.CentreRight;
                                feverLights[0].Scale = new Vector2(2.2658894f);
                                feverLights[0].Position = new Vector2(43f, -140f);

                                // 二号：右侧，Anchor=CentreRight, Origin=CentreLeft
                                feverLights[1].Bpm = (float)bpm;
                                feverLights[1].Anchor = Anchor.CentreRight;
                                feverLights[1].Origin = Anchor.CentreLeft;
                                feverLights[1].Scale = new Vector2(2.2658894f);
                                feverLights[1].Position = new Vector2(-40f, -140f);

                                if (energyGauge?.EnergySystem != null)
                                {
                                    energyGauge.EnergySystem.IsSuperTime.BindValueChanged(super =>
                                    {
                                        foreach (var fl in feverLights)
                                        {
                                            if (super.NewValue)
                                                fl.Show();
                                            else
                                                fl.Hide();
                                        }
                                    });
                                }
                            }
                        })
                        {
                            new EzHUDComboTitle(),
                            new EzHUDComboCounter(),
                            new EzHUDHitTiming(),
                            new EzHUDHitTiming(),
                            new EzHUDHitResultScore(),
                            new EzProNeriEnergyGauge(),
                            new EzProNeriFeverUp(),
                            new EzProNeriFeverLight(),
                            new EzProNeriFeverLight(),
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
