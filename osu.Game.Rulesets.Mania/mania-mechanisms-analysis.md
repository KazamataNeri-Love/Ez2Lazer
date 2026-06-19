# Mania 模式机制解析 — 整合参考表

## 一、运动系统 (Scrolling / Movement)

### 1.1 基础滚动机制

| 机制 | 说明 | 关键参数/公式 |
|------|------|--------------|
| **滚动方向** | 支持 Up（从下往上，osu!标准）和 Down（从上往下） | `ManiaScrollingDirection` 枚举，绑定 `ScrollingDirection` |
| **TimeRange** | 控制屏幕上可见的时间跨度（ms），决定滚动密度 | `MIN_TIME_RANGE = 290ms`（40速），`MAX_TIME_RANGE = 11485ms`（1速） |
| **滚动速度** | 用户可调 1-40（SpeedStyle）或 200-400（TimeStyle），基础值+每级增量 | `ComputeScrollTime(speed, baseMs, timePerMs)` |
| **滚动风格** | 4种模式 | `EzManiaScrollingStyle` |
|  ▪ ScrollSpeedStyle | 40速通配风格，1-400映射为1.0~40.0倍速 | `TimeRange = MAX_TIME_RANGE / (speed/10)` |
|  ▪ ScrollTimeStyle | 相对默认判定线的ms | `TimeRange = baseSpeed - (speed-200) * timePerSpeed` |
|  ▪ ScrollTimeForRealJudgement | 相对实际判定线的ms，忽略HitPosition影响 | scale = 1.0 |
|  ▪ ScrollTimeStyleFixed | 相对屏幕底部的ms，对齐屏幕底部不变 | scale = lengthToHitPosition / 768 |
| **速度倍率补偿** | Track速度变化时自动缩放TimeRange | `TimeRange *= tempo * frequency` |
| **倍率控制点** | BPM变化自动影响滚动速度（Per-note tempo scaling） | `RelativeScaleBeatLengths = true`；控制点 `Multiplier` 影响局部密度 |

### 1.2 定位算法 (Position Algorithm)

| 算法 | 说明 | 公式 |
|------|------|------|
| **ConstantScrollAlgorithm（默认）** | 线性映射，时间偏移→屏幕位置 | `pos = (time - currentTime) / timeRange * scrollLength` |
| **SequentialScrollAlgorithm** | 考虑BPM控制点，倍率变化处速度自适应 | 控制点处累积位置 + 剩余时间×控制倍率 |
| **OverlappingScrollAlgorithm** | 重叠区域可配置 | （未在mania中直接启用） |

### 1.3 位置公式详解

```
时间偏移 → 归一化 → 缩放 → 方向反转 → 屏幕坐标
offset = time - currentTime
normalized = offset / TimeRange          // -1~+1范围
scrollPos = normalized * scrollLength     // 像素偏移
// Direction=Down: scrollPos取反（上正下负）
// Direction=Up: scrollPos取正（下正上负）
finalPos = axisInverted ? -scrollPos : scrollPos
```

### 1.4 判定线位置 (Hit Position)

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `HitPosition` | 判定线距离屏幕顶部的像素距离 | 默认 `768 - 110 = 658`（经典值），皮肤可覆盖 |
| `HIT_TARGET_POSITION` | Stage中的判定线偏移 | `110` |

判定线通过 `HitPositionPaddedContainer` 实现：以 `Padding` 形式将内容推到判定线位置。

### 1.5 列系统 (Column Layout)

| 机制 | 说明 |
|------|------|
| **Stage** | 列的分组单位，每个Stage包含若干Column |
| **Column** | 单列的 `ScrollingPlayfield`，宽度80px（特殊列70px） |
| **特殊列** | 奇数列数时的中间列（如7K的第4列） |
| **StageDefinition** | 定义列数，`IsSpecialColumn(i)` 判断是否为特殊列 |
| **Dual Stage** | 双Stage布局（如14K = 7K×2），由 `Dual` 标志控制 |
| **列间距** | 皮肤可配置 `LeftColumnSpacing`/`RightColumnSpacing`，默认1px |
| **列宽样式** | `ColumnWidthStyle` - EzSkinOnly/GlobalWidth/GlobalTotalWidth |

### 1.6 列与键位映射

| 机制 | 说明 |
|------|------|
| **ManiaAction** | 键位枚举 `Key1~Key20`，按列分配 |
| **Action绑定** | `Column.Action` 绑定对应的 `ManiaAction` |
| **双Stage键位** | 第一Stage使用 `Key1~KeyN`，第二Stage继续递增 |

---

## 二、Note 对象系统 (Hit Object Types)

### 2.1 Note 类型层次

| 类型 | 类名 | 说明 |
|------|------|------|
| **基础** | `ManiaHitObject` | 抽象基类，持有 `Column` 属性 |
| **点Note** | `Note` | 单个按键，产生 `ManiaJudgement` |
| **长按头** | `HeadNote` | HoldNote的头部，继承自 `Note` |
| **长按尾** | `TailNote` | HoldNote的尾部，判定窗口有1.5倍松弛 |
| **HoldNote** | `HoldNote` | 组合体：包含 Head + Tail + Body |
| **HoldNoteBody** | `HoldNoteBody` | 虚拟对象，跟踪长按保持状态 |
| **小节线** | `BarLine` | 视觉辅助线，产生 `IgnoreJudgement` |
| **惩罚类** | `PunishmentHoldNote` | 特定Mod用的特殊长按 |

### 2.2 Drawable 类型层次

| Drawable | 对应对象 | 关键功能 |
|----------|----------|----------|
| `DrawableNote` | Note | 处理按压/释放，`CheckForResult`实现判定 |
| `DrawableHoldNote` | HoldNote | 管理Head/Tail/Body生命周期，处理按压→Hold→释放流程 |
| `DrawableHoldNoteHead` | HeadNote | 按下头时冻结在判定线位置 |
| `DrawableHoldNoteTail` | TailNote | 释放时判定，允许Lenience |
| `DrawableHoldNoteBody` | HoldNoteBody | 跟踪Hold断开时间 |

### 2.3 HoldNote 内部结构

```
DrawableHoldNote
├── sizingContainer (满尺寸容器，高度随按压进度收缩)
│   └── maskingContainer (遮罩容器)
│       └── maskedContents (遮罩内容：body代理 + tail代理)
│   └── headContainer → DrawableHoldNoteHead
├── bodyContainer → DrawableHoldNoteBody
├── bodyPiece (皮肤Drawable，HoldNote身体)
├── tailContainer → DrawableHoldNoteTail
└── slidingSample (循环滑条音效)
```

**长按视觉行为**：
- 按下后 `sizingContainer.Height` 从1逐渐缩小（← 视觉上Note被"推"向判定线）
- `bodyPiece.Y` = Head高度的一半（使body从头部下方开始）
- `bodyPiece.Height` = 总高度 - Head/2 + Tail/2
- 锁定在判定线位置，直到释放

### 2.4 判定结果类型

| 对象 | Judgement类 | MaxResult | MinResult |
|------|-------------|-----------|-----------|
| Note/HeadNote/TailNote | `ManiaJudgement` | Perfect | Miss |
| HoldNoteBody | `HoldNoteBodyJudgement` | IgnoreHit | ComboBreak |
| HoldNote(整体) | `IgnoreJudgement`（由子对象判定） | - | - |

---

## 三、判定系统 (Judgement System)

### 3.1 判定结果等级

| 判定结果 | hitMode适配名称 | HitResult枚举 |
|----------|----------------|--------------|
| **Perfect** | Kool / 305 / Best | `HitResult.Perfect` |
| **Great** | Cool / 300 | `HitResult.Great` |
| **Good** | Good / 200 | `HitResult.Good` |
| **Ok** | Bad / 100 | `HitResult.Ok`（仅Classic/Lazer） |
| **Meh** | Bad / 50 | `HitResult.Meh` |
| **Miss** | Poor / Miss | `HitResult.Miss` |
| **Poor** | KPoor | `HitResult.Poor`（仅BMS系列模式） |

### 3.2 判定窗口基础 (ManiaHitWindows)

| 参数 | Lazer默认范围(OD0→OD10) | 公式 |
|------|------------------------|------|
| **Perfect** | 22.4ms → 13.9ms | `floor(DifficultyRange(OD, {22.4,19.4,13.9}) * totalMultiplier) + 0.5` |
| **Great** | 64 → 34 | `floor(DifficultyRange(OD, {64,49,34}) * totalMultiplier) + 0.5` |
| **Good** | 97 → 67 | `floor(DifficultyRange(OD, {97,82,67}) * totalMultiplier) + 0.5` |
| **Ok** | 127 → 97 | `floor(DifficultyRange(OD, {127,112,97}) * totalMultiplier) + 0.5` |
| **Meh** | 151 → 121 | `floor(DifficultyRange(OD, {151,136,121}) * totalMultiplier) + 0.5` |
| **Miss** | 188 → 158 | `floor(DifficultyRange(OD, {188,173,158}) * totalMultiplier) + 0.5` |

**乘数因子**：
- `totalMultiplier = speedMultiplier / difficultyMultiplier`
- `speedMultiplier`：Track速度补偿（快放时扩大窗口）
- `difficultyMultiplier`：难度缩放（>1缩小窗口=更难）

### 3.3 Classic（osu!stable经典）判定窗口

| 判定 | 非Convert公式 | Convert公式 |
|------|--------------|-------------|
| **Perfect** | `floor(16 * mul) + 0.5` | 同上 |
| **Great** | `floor((34 + 3*invOD) * mul) + 0.5` | OD>4: `floor(34*mul)+0.5`，否则 `floor(47*mul)+0.5` |
| **Good** | `floor((67 + 3*invOD) * mul) + 0.5` | 同上模式 |
| **Ok** | `floor((97 + 3*invOD) * mul) + 0.5` | `floor(97*mul)+0.5` |
| **Meh** | `floor((121 + 3*invOD) * mul) + 0.5` | `floor(121*mul)+0.5` |
| **Miss** | `floor((158 + 3*invOD) * mul) + 0.5` | `floor(158*mul)+0.5` |

其中 `invOD = clamp(10 - OD, 0, 10)`

### 3.4 判定入口

判定由 `DrawableNote.CheckForResult()` 触发：

```
用户按键 → OnPressed → UpdateResult(true)
  ↓
CheckForResult(userTriggered=true, timeOffset)
  ↓
HitObject.HitWindows.ResultFor(timeOffset) 
  ↓ (或 ManiaHitWindows.ResultFor 中通过 helper 的分支)
返回 HitResult
  ↓
GetCappedResult 限制 → ApplyResult
```

**自动Miss**：当Note越过判定窗口且无输入时，`CheckForResult(userTriggered=false)` 自动 `ApplyMinResult()`

### 3.5 HoldNote 判定流程

```
用户按键 → OnPressed
  ↓ (若在判定窗口内)
beginHoldAt(timeOffset)
  ↓
Head.UpdateResult() → 头判定(Perfect~Miss)
  ↓ (如果头判定为Hit)
保持按压状态 → isHolding = true
  ↓ (当用户释放时)
OnReleased
  ↓
Tail.UpdateResult() → 尾判定(受head结果影响)
  ↓
Body.TriggerResult(Tail.IsHit) → 身体判定(IgnoreHit或ComboBreak)
  ↓
CheckForResult → 若Tail命中则ApplyMaxResult，否则MissForcefully
```

**尾判定Cap规则**（`DrawableHoldNoteTail.GetCappedResult`）：
- 如果头未命中或身体已断开 → 最大结果被限制为 Meh
- 否则使用原始判定结果

**释放Lenience**（`TailNote.RELEASE_WINDOW_LENIENCE = 1.5`）：
- 尾判定的 `timeOffset` 除以 1.5，使释放窗口更宽松

### 3.6 HoldNoteJudgementResult 状态机

| 方法 | 说明 |
|------|------|
| `ReportHoldState(time, holding)` | 记录按压/释放状态变化的时间点 |
| `IsHolding(time)` | 查询某个时刻是否处于按压状态 |
| `DroppedHoldAfter(time)` | 查询某个时间点后是否有过释放 |

使用栈存储状态变化：`Stack<(double time, bool holding)>`

---

## 四、Ez2AC 特色判定系统

### 4.1 判定模式 (EzEnumHitMode)

| 模式 | 判定等级 | 说明 |
|------|---------|------|
| **Lazer** | Perfect/Great/Good/Ok/Meh/Miss | 标准osu!判定，OD决定窗口 |
| **Classic** | 同上 | osu!stable经典判定，窗口对称固定 |
| **EZ2AC** | Kool/Cool/Good/Meh/Miss | EZ2AC街机风格，4级判定+Miss |
| **O2Jam** | Cool/Good/Miss | O2Jam简化，2级有效判定+BPM相关窗口 |
| **IIDX_HD** | Kool/Cool/Good/Bad/Poor/KPoor | IIDX Hard模式，7级判定 |
| **LR2_HD** | 同上 | LR2 Hard模式 |
| **Raja_NM** | 同上 | beatoraja Normal模式 |
| **Malody_E** | Best/Cool/Good/Miss | Malody E难度 |
| **Malody_B** | 同上 | Malody B难度（更宽松） |

### 4.2 EZ2AC 判定窗口

| 判定 | 名称 | 窗口(ms) |
|------|------|---------|
| **Perfect** | Kool | `16.67 * totalMultiplier` |
| **Great** | Cool | `33.33 * totalMultiplier` |
| **Good** | Good | `83.33 * totalMultiplier` |
| **Meh** | (Bad) | `100.0 * totalMultiplier` |
| **Miss** | Miss | `116.67 * totalMultiplier` |

**特点**：EZ2AC判定窗口窄而密，Kool仅16.67ms（约1帧），整体范围远比osu!标准小。

### 4.3 EZ2AC 计分基础分

| 判定 | EZ2AC基础分 | Classic基础分 | O2Jam基础分 | IIDX(EX Score) |
|------|------------|-------------|-------------|----------------|
| **Perfect/Kool** | 300 | 300 | 300 | 300 |
| **Great/Cool** | 150 | 300 | — | 150 |
| **Good** | 41 | 200 | 150 | 0 |
| **Ok** | 0 | 100 | — | 0 |
| **Meh** | 0 | 50 | 0 | 0 |

### 4.4 总分计算（ManiaScoreProcessor）

| hitMode | 总分公式 |
|---------|---------|
| **Lazer** | `150000 * comboProgress + 850000 * acc^2+2acc * accProgress + bonusPortion` |
| **Classic** | 同上（但accuracy用ClassicAcc） |
| **EZ2AC / O2Jam / BMS / Malody** | `1000000 * Accuracy.Value`（仅根据正确率，忽略combo和加分） |

### 4.5 BMS系列判定窗口（HitModeHelper）

**IIDX_HD (row 0)**:
| 判定 | 早按(ms) | 晚按(ms) |
|------|---------|---------|
| Kool(305) | ±16.67 | ±16.67 |
| Cool(300) | ±33.33 | ±33.33 |
| Good(200) | ±116.67 | ±116.67 |
| Bad(050) | ±250 | ±250 |
| Poor(Miss) | ±250 | ±250 |
| KPoor | 前500ms以上→早KPoor | 后150ms→晚KPoor |

**LR2_HD (row 1)**:
| 判定 | 早按(ms) | 晚按(ms) |
|------|---------|---------|
| Kool(305) | ±15 | ±15 |
| Cool(300) | ±30 | ±30 |
| Good(200) | ±60 | ±60 |
| Bad(050) | ±200 | ±200 |
| KPoor | 前1000ms→早KPoor | 后150ms→晚KPoor |

**Raja_NM (row 2)**:
| 判定 | 早按(ms) | 晚按(ms) |
|------|---------|---------|
| Kool(305) | ±15 | ±15 |
| Cool(300) | ±45 | ±45 |
| Good(200) | ±112 | ±112 |
| Bad(050) | ±165 | ±210 |
| KPoor | 前500ms→早KPoor | 后150ms→晚KPoor |

### 4.6 O2Jam 判定窗口

BPM相关窗口（最独特的判定系统）：
- **Cool(Perfect)**: `7500 / BPM * totalMultiplier` ms
- **Good**: `22500 / BPM * totalMultiplier` ms
- **Miss**: `31250 / BPM * totalMultiplier` ms

**特点**：判定窗口随BPM变化——BPM越高判定越严，低BPM则宽松。

---

## 五、Note Lock / 判定优先级系统

### 5.1 OrderedHitPolicy

| 策略 | EzEnumJudgePrecedence | 行为 |
|------|----------------------|------|
| **Earliest（原始osu!）** | `Earliest` | 只有最新的Note可被击中（标准Note Lock） |
| **Duration** | `Duration` | 比较所有候选与按键时间的偏差，选偏差最小的 |
| **Combo** | `Combo` | 使用Combo保持窗口：若当前候选已过前一候选的Combo早界，且新候选在Combo晚界内，则替换为新的 |

### 5.2 Note Lock 决策流程

```
用户按键
  ↓
OrderedHitPolicy.IsHittable()
  ↓ (非Earliest模式)
OrderedHitPolicyHelper.IsHittableWithPrecedence()
  ↓
1. 获取所有活跃候选对象（判定窗口与当前时间重叠且未判定）
2. 若BMS模式：检查是否有已判定为Bad的对象可路由到KPoor
3. 按优先级策略选择最优候选
4. 若选中目标 = 当前按下的Note → 允许判定
5. 否则 → 拒绝判定
```

### 5.3 折叠选择算法（selectFoldCandidate）

```
遍历按时间排序的候选列表：
  if 已判定 → 跳过
  if comboAlgorithm:
    检查(t1已过combo早界 ∧ t2在combo晚界内) → 替换
  else (duration):
    检查 t2比t1离按压时间更近 → 替换
  比较新旧判定的"质量"（判定等级rank）
  选择rank更高的；rank相同时选时间偏差更小的
```

### 5.4 命中时自动Miss前置对象

当 `HandleHit` 被调用时，当前Note之前所有未判定的Note强制Miss（miss force），确保不会遗留未被判定。

**非Earliest模式**：只有当对象不再在可判定时间窗口内时才MissForcefully（`IsUserTriggerJudgeableNow`）

---

## 六、血量系统 (Health System)

### 6.1 血量模式 (EzEnumHealthMode)

| 模式 | 行列索引 | 判定权重(305/300/200/100/50/Miss/Poor) |
|------|---------|--------------------------------------|
| **Lazer** | 0 | 由 `ManiaJudgement.HealthIncreaseFor` 计算，Miss扣血与DrainRate相关 |
| **O2Easy** | 1 | +0.003 / +0 / +0.002 / 0 / -0.01 / -0.05 / 0 |
| **O2Normal** | 2 | +0.002 / +0 / +0.001 / 0 / -0.007 / -0.04 / 0 |
| **O2Hard** | 3 | +0.001 / +0 / +0 / 0 / -0.005 / -0.03 / 0 |
| **Ez2Ac** | 4 | +0.004 / +0.003 / +0.001 / 0 / -0.03 / -0.05 / -0.02 |
| **IIDX_HD** | 5 | +0.0016 / +0.0016 / 0 / 0 / -0.05 / -0.09 / -0.05 |
| **LR2_HD** | 6 | +0.001 / +0.001 / +0.0005 / 0 / -0.06 / -0.10 / -0.02 |
| **Raja_HD** | 7 | +0.0015 / +0.0012 / +0.0003 / 0 / -0.05 / -0.10 / -0.05 |

### 6.2 低血量保护机制

| 模式 | 保护规则 |
|------|---------|
| **IIDX_HD** | 血量≤30%时扣血减半 |
| **LR2_HD** | 血量≤30%时扣血×0.6 |
| **Raja_HD** | 血量≤30%扣血×0.6，30%<血量<50%线性插值(0.6→1.0) |

其他模式：扣血幅度限制在 `[-0.2, 0.2]` 区间。

---

## 七、谱面转换系统

### 7.1 列数自动决定

| 来源 | 列数决定逻辑 |
|------|-------------|
| **Mania原生谱面** | `round(CircleSize)`，至少1列 |
| **osu!标准转谱** | 根据SpecialObject比例和OD：<br> <20%滑条/转盘 → 7K<br> 20~30%或CS≥5 → OD>5?7K:6K<br> >60% → OD>4?5K:4K<br> 否则 → max(4, min(OD+1, 7)) |

### 7.2 转换器类型

| 转换器 | 用途 |
|--------|------|
| `PassThroughPatternGenerator` | Mania→Mania原生转换 |
| `HitCirclePatternGenerator` | osu!→Mania圆圈转换 |
| `SliderPatternGenerator` | osu!→Mania滑条转换 |
| `LegacyPatternGenerator` | 旧版谱面兼容 |

---

## 八、关键术语对照表

| Ez2Lazer术语 | 等效街机术语 | 含义 |
|-------------|------------|------|
| Perfect | Kool / 305 / Best | 最精确判定 |
| Great | Cool / 300 | 较精确判定 |
| Good | Good / 200 | 一般判定 |
| Ok | Bad / 100 | 较差判定（仅Lazer/Classic） |
| Meh | Bad / 50 | 差判定 |
| Miss | Poor / Miss | 遗漏或严重偏移 |
| Poor | KPoor | 极早/极晚（仅BMS模式） |
| HoldNote | Long Note / LN | 长按 |
| HeadNote | LN Head | 长按头部 |
| TailNote | LN Tail | 长按尾部 |
| HoldNoteBody | LN Body | 长按身体（保持状态） |
| HitPosition | Judgement Line | 判定线位置 |
| TimeRange | Scroll Speed / Green Number | 可见时间跨度 |
| Note Lock | Note Lock | 单列同一时间只允许一个判定 |
| JudgePrecedence | 无对应 | 重叠Note优先级策略 |
