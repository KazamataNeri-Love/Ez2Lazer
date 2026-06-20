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


## 🌐 仓库

- **上游主分支：** [SK-la/Ez2Lazer](https://github.com/SK-la/Ez2Lazer)
- **Neri Edge：** [KazamataNeri-Love/Ez2Lazer/tree/Edge](https://github.com/KazamataNeri-Love/Ez2Lazer/tree/Edge)
- **功能分支：** `Storyboard-Scale` / `Background-Container` / `Neri-Mod`


# Ez2Lazer 介绍

<p align="center">
  <img width="500" alt="osu! logo" src="assets/lazer.png">
</p>

# Ez2Lazer

中文 | English

Ez2Lazer 是基于 osu! lazer 的深度改造分支，聚焦 Mania/BMS 生态、高可定制 HUD、判定系统切换和谱面分析工具链。  
Ez2Lazer is a heavily customized branch based on osu! lazer, focused on Mania/BMS workflows, configurable HUD, switchable judgement systems and analysis tools.

## 下载与运行 / Download and Run

- 最新版本发布页 / Latest releases: [SK-la/Ez2Lazer Releases](https://github.com/SK-la/Ez2Lazer/releases)
- 资源包 / Resource pack: [EzResources (OneDrive)](https://la1225-my.sharepoint.com/:f:/g/personal/la_la1225_onmicrosoft_com/EiosAbw_1C9ErYCNRD1PQvkBaYvhflOkt8G9ZKHNYuppLg?e=DWY1kn)
- 运行时要求 / Runtime: [.NET 8.0 Runtime](https://dotnet.microsoft.com/download)

**自动更新（推荐）** / **Auto-update (recommended)**  
- Windows：下载 Release 中的 `ez2lazer-win-Setup.exe` 安装（需已安装 [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download)）；之后可在游戏内接收增量更新。  
- Windows: use `ez2lazer-win-Setup.exe` from Releases (requires [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download)); in-game updates download deltas afterward.  
- 手动安装请用 `Ez2Lazer_release_*.zip` 解压运行；zip 无法使用增量更新，需改用 Setup 安装一次。  
- For manual installs use `Ez2Lazer_release_*.zip`; zip installs cannot receive delta updates until you switch to Setup once.

> 未安装 EzResources 时，Ez Pro Skin 和 Ez HUD 组件会缺失贴图。  
> Without EzResources, Ez Pro Skin and Ez HUD widgets will miss textures.

## 文档入口 / Documentation

主文档迁移到 Wiki，README 只保留快速入口。  
The full documentation now lives in Wiki; this README stays as a quick index.

- Wiki 首页 / Home: [Ez2Lazer Wiki](https://github.com/SK-la/Ez2Lazer/wiki)
- 中文总览 / CN overview: [功能总览（中文）](https://github.com/SK-la/Ez2Lazer/wiki/%E5%8A%9F%E8%83%BD%E6%80%BB%E8%A7%88-%E4%B8%AD%E6%96%87)
- English overview: [Feature Overview](https://github.com/SK-la/Ez2Lazer/wiki/Feature-Overview-English)
- 发布说明规范 / Release workflow: [发布说明规范](https://github.com/SK-la/Ez2Lazer/wiki/%E5%8F%91%E5%B8%83%E8%AF%B4%E6%98%8E%E8%A7%84%E8%8C%83-%E4%B8%AD%E6%96%87)

### 功能板块（中文）
- [选歌界面](https://github.com/SK-la/Ez2Lazer/wiki/%E9%80%89%E6%AD%8C%E7%95%8C%E9%9D%A2-%E4%B8%AD%E6%96%87)
- [游戏设置](https://github.com/SK-la/Ez2Lazer/wiki/%E6%B8%B8%E6%88%8F%E8%AE%BE%E7%BD%AE-%E4%B8%AD%E6%96%87)
- [Skin 系统](https://github.com/SK-la/Ez2Lazer/wiki/Skin-%E7%B3%BB%E7%BB%9F-%E4%B8%AD%E6%96%87)
- [Mod 系统](https://github.com/SK-la/Ez2Lazer/wiki/Mod-%E7%B3%BB%E7%BB%9F-%E4%B8%AD%E6%96%87)
- [HUD 组件](https://github.com/SK-la/Ez2Lazer/wiki/HUD-%E7%BB%84%E4%BB%B6-%E4%B8%AD%E6%96%87)
- [编辑器](https://github.com/SK-la/Ez2Lazer/wiki/%E7%BC%96%E8%BE%91%E5%99%A8-%E4%B8%AD%E6%96%87)
- [判定与血量](https://github.com/SK-la/Ez2Lazer/wiki/%E5%88%A4%E5%AE%9A%E4%B8%8E%E8%A1%80%E9%87%8F-%E4%B8%AD%E6%96%87)

### Feature Areas (English)
- [Song Select](https://github.com/SK-la/Ez2Lazer/wiki/Song-Select-English)
- [Game Settings](https://github.com/SK-la/Ez2Lazer/wiki/Game-Settings-English)
- [Skin System](https://github.com/SK-la/Ez2Lazer/wiki/Skin-System-English)
- [Mod System](https://github.com/SK-la/Ez2Lazer/wiki/Mod-System-English)
- [HUD Widgets](https://github.com/SK-la/Ez2Lazer/wiki/HUD-Widgets-English)
- [Editor](https://github.com/SK-la/Ez2Lazer/wiki/Editor-English)
- [Judgement and Health](https://github.com/SK-la/Ez2Lazer/wiki/Judgement-and-Health-English)

## 快速安装 / Quick Setup

1. 下载程序并解压到任意目录。  
2. 进入设置，使用 `更改osu!文件夹位置` 指向你的数据路径。  
3. 下载并解压 EzResources 到该路径下（形成 `.../EzResources`）。  

详细步骤请查看 Wiki 安装页：  
- [安装指南（中文）](https://github.com/SK-la/Ez2Lazer/wiki/%E5%AE%89%E8%A3%85%E6%8C%87%E5%8D%97-(%E4%B8%AD%E6%96%87))
- [Installation Guide (English)](https://github.com/SK-la/Ez2Lazer/wiki/Installation-Guide-(English))

## Build Instructions

```bash
git clone https://github.com/SK-la/Ez2Lazer
git clone https://github.com/SK-la/osu-framework
git clone https://github.com/SK-la/osu-resources
```

默认使用 NuGet：`ez2lazer.Framework`、`ez2lazer.Game.Resources`（版本在 [Ez2Lazer.Dependencies.props](Ez2Lazer.Dependencies.props) 中维护）。

本地联调 framework / resources：编辑该文件，按注释切换 `UseEz2LazerNuGetPackages`（`true` = NuGet，`false` = 同级工程引用）。

Default: NuGet. Toggle `UseEz2LazerNuGetPackages` in `Ez2Lazer.Dependencies.props` for local sibling projects.

自编译版本不会显示游戏内更新选项，也不会从 SK-la/Ez2Lazer Releases 拉取更新。  
Self-built copies hide in-game update settings and do not check SK-la/Ez2Lazer Releases for updates.

## Release Notes Automation

Use the helper script to generate a categorized draft from commit range:

```powershell
pwsh ./GenerateReleaseNotes.ps1 -FromRef "2026.5.1" -ToRef "2026.5.6" -Output "../release-2026.5.6.md" -Title "Release 2026.5.6"
```

Then polish the draft and publish it on GitHub Releases.

## Special Thanks
- [osu!](https://github.com/ppy/osu): The original game and framework.
- [YuLiangSSS](https://osu.ppy.sh/users/15889644): Contributed many fun mods.

## Licence

*osu!* code and framework are licensed under the [MIT licence](https://opensource.org/licenses/MIT).  
See [LICENCE](LICENCE) for details.

This does not cover usage of "osu!" or "ppy" branding, which is protected by trademark law.  
Game resources are covered by a separate licence in [ppy/osu-resources](https://github.com/ppy/osu-resources).
