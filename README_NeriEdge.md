# Ez2Lazer Neri_Edge

基于 [SK-la/Ez2Lazer](https://github.com/SK-la/Ez2Lazer) 的分支，保留上游 EzHUDLeaderboard 等全部功能，额外整合了 KazamataNeri-Love 的特色定制功能。

---

## ✨ Neri_Edge 特色功能

### 1️⃣ 故事板/背景缩放 (Background Scale)

在图形设置的布局选项中新增三个滑块，可独立调节游戏背景画面（背景图 / 故事板 / 视频）的缩放和位移：

| 设置项 | 范围 | 说明 |
|---|---|---|
| 背景缩放 (Background scale) | 0.50 ~ 1.00 | 以屏幕中心为基准缩放 |
| 水平位置 (Horizontal position) | 0% ~ 100% | 缩放后的水平偏移百分比 |
| 垂直位置 (Vertical position) | 0% ~ 100% | 缩放后的垂直偏移百分比 |

- 缩放以屏幕中心为锚点，图像从中心向外收缩/放大
- 位移量随缩放比例自动适配，缩放越小可移动范围越大
- 调节时有实时预览层，松手后自动淡出

**涉及文件：**
- `osu.Game/Screens/Play/DimmableStoryboard.cs`
- `osu.Game/Overlays/Settings/Sections/Graphics/LayoutSettings.cs`
- `osu.Game/EzOsuGame/Configuration/Ez2ConfigManager.cs`
- `osu.Game/EzOsuGame/Localization/EzSettingsStrings.cs`

---

### 2️⃣ Background 容器层 (独立背景渲染层)

为规则集游玩界面添加了一个独立的背景渲染层 `BackgroundLayer`，位于所有游戏内容之下：

- 不受 `FrameStabilityContainer` 的时间稳定性影响
- 支持皮肤编辑器发现和皮肤组件加载
- 通过 `SkinnableContainer.ExternalTarget` 代理机制，将皮肤组件渲染到独立容器
- 可用于放置舞台背景、动态光效等不参与游戏判定的纯视觉元素

**涉及文件：**
- `osu.Game/Rulesets/UI/DrawableRuleset.cs`
- `osu.Game/Skinning/SkinnableContainer.cs`
- `osu.Game/Skinning/GlobalSkinnableContainers.cs`
- `osu.Game/Skinning/TrianglesSkin.cs`

---

### 3️⃣ NeriMod 特色 Mod 系统

在 Mod 选择界面新增 **NeriMod** 分类列（橙色标识），包含以下定制 Mod：

#### Mania NeriMods

| Mod | 说明 |
|---|---|
| **ManiaModNeriBarrelRoll** | Barrel Roll — 谱面水平翻转滚动 |
| **ManiaModNeriFasterBarrelRoll** | Faster Barrel Roll — 加速版 Barrel Roll |
| **ManiaModNeriDance** | Dance — 谱面舞蹈式左右摆动 |
| **ManiaModNeriNewDance** | New Dance — 新版 Dance 效果 |
| **ManiaModNeriSPRefiner** | SP Refiner — 分数精准度优化辅助 |

#### Osu NeriMods

| Mod | 说明 |
|---|---|
| **OsuModFasterBarrelRoll** | Osu 版 Faster Barrel Roll |
| **OsuModNeriHidden** | Neri 版 Hidden（隐球） |

#### 基础架构
- `ModType.cs` 新增 `NeriMod = 8` 枚举
- `OsuColour.cs` 中 NeriMod 映射为橙色 (`Orange1`)
- `ModSelectOverlay.cs` 新增 NeriMod 列

**涉及文件：**
- `osu.Game.Rulesets.Mania/EzMania/Mods/LAsMods/ManiaModNeri*.cs`
- `osu.Game.Rulesets.Mania/EzMania/Mods/LAsMods/NeriSPRefinerStrings.cs`
- `osu.Game.Rulesets.Osu/Mods/OsuModFasterBarrelRoll.cs`
- `osu.Game.Rulesets.Osu/Mods/OsuModNeriHidden.cs`
- `osu.Game.Rulesets.Mania/ManiaRuleset.cs`
- `osu.Game.Rulesets.Osu/OsuRuleset.cs`
- `osu.Game/Rulesets/Mods/ModType.cs`
- `osu.Game/Graphics/OsuColour.cs`
- `osu.Game/Overlays/Mods/ModSelectOverlay.cs`

---

### 4️⃣ 特色 HUD 组件

| 组件 | 说明 |
|---|---|
| **EzHUDComboCounterPlus** | 增强版 Combo 计数器，支持更多自定义样式 |
| **Default300GRateCounter** | 默认皮肤 300G 率计算器 — 计算 Perfect/Great 比率 |
| **Legacy300GRateCounter** | Legacy 皮肤 300G 率计算器 — 使用 Legacy 字体显示 |

**涉及文件：**
- `osu.Game.Rulesets.Mania/EzMania/HUD/EzHUDComboCounterPlus.cs`
- `osu.Game/Screens/Play/HUD/Default300GRateCounter.cs`
- `osu.Game/Skinning/Legacy300GRateCounter.cs`

---

### 5️⃣ OsuLogo 配色定制

主菜单 Logo 配色从默认粉色调整为蓝色主题：

| 元素 | 原色 | Neri 色 |
|---|---|---|
| 背景圆盘 | `#ff66ab → #cc5289` | `#1A2B6B → #4A7DD0` |
| 三角形斑纹 | `#ff66ab → #b6346f` | `#1A2B6B → #3A6BB8` |

**涉及文件：**
- `osu.Game/Screens/Menu/OsuLogo.cs`

---

### 6️⃣ 上游合并功能 (ppy/osu 移植)

| 功能 | 来源 | 说明 |
|---|---|---|
| **LegacySkinEncoder** | ppy/osu #38090 | 完整的 `skin.ini` 编码器，支持皮肤重命名持久化；`BarLineHeight` 默认值修正为 1.2f；新增 `SpecialStyle`/`UpsideDown`/`SplitStages` 等参数的编解码保留 |
| **Kiai time 修复** | ppy/osu #38107 | 修复 Legacy 谱面编码器在首个控制点错误输出 kiai time 标志的问题 |

---

## 🔧 构建与运行

```bash
# Release 构建
dotnet build osu.Desktop/osu.Desktop.csproj -c Release

# 直接运行
./osu.Desktop/bin/Release/net8.0/Ez2osu!.exe
```

## 📦 资源依赖

部分皮肤和 HUD 组件需要安装 [EzResources](https://la1225-my.sharepoint.com/:f:/g/personal/la_la1225_onmicrosoft_com/EiosAbw_1C9ErYCNRD1PQvkBaYvhflOkt8G9ZKHNYuppLg?e=DWY1kn) 资源包以获取完整贴图。

## 🌐 仓库

- **上游主分支：** [SK-la/Ez2Lazer](https://github.com/SK-la/Ez2Lazer)
- **Neri Edge：** [KazamataNeri-Love/Ez2Lazer/tree/Edge](https://github.com/KazamataNeri-Love/Ez2Lazer/tree/Edge)
- **功能分支：** `Storyboard-Scale` / `Background-Container` / `Neri-Mod`
