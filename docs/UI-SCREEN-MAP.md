# 游戏内界面 → 绘制代码映射索引

用途：拿到「改某个界面的外观 / 加按钮 / 加面板」的需求时，先定位界面入口类与构图方法，再进对应绘图文件。
路径均相对仓库根（`Ez2Lazer_Neri_Edge/`）。

## 定位三步法

1. **找入口类**：屏是 `OsuScreen` 子类；Overlay 是 `OverlayContainer` / `ShearedOverlayContainer` / `SettingsPanel` 子类。
2. **找构图点**：入口类的 `[BackgroundDependencyLoader] load()`，或构造函数里的 `InternalChild` / `Children` / `AddRangeInternal`。这里定义了界面的骨架层级，其余都是子组件。
3. **按目录查表**：本文件的表按「界面 → 文件 → 职责」组织；同目录下 `Xxx.cs` + `Xxx.Yyy.cs`（分部）通常是一个组件的不同层。

---

## A. 全局壳层（所有界面共用）

装配点：`osu.Game/OsuGame.cs` 的 `load()` 尾部（约 L1230–L1320），按 `overlayContent` / `topMostOverlayContent` / `screenCaptureContent` / `leftFloatingOverlayContent` 分区挂载。

| 文件 | 职责 |
|---|---|
| `osu.Game/OsuGame.cs` | 全局装配：Toolbar、各 Overlay、EzLayoutLayer、SkinEditorOverlay、EzLayoutEditorOverlay |
| `osu.Game/Screens/OsuScreenStack.cs` / `Screens/OsuScreen.cs` | 屏幕栈与屏基类（进入/退出、Footer 生命周期） |
| `osu.Game/Overlays/Toolbar/` | 顶部工具栏 |
| `osu.Game/Screens/Footer/ScreenFooter.cs` / `ScreenFooterContent.cs` / `ScreenFooterButton.cs` | 屏底 footer 体系（选歌 / 结算 / Mod 选择共用） |
| `osu.Game/EzOsuGame/Layout/EzLayoutLayer.cs` | **Ez 全局布局层**：叠加在屏之上的 Ez HUD / 布局容器，按屏切换（选歌 HUD 等） |
| `osu.Game/EzOsuGame/Layout/EzLayoutEditorOverlay.cs` / `EzLayoutEditor.cs` | Ez 布局编辑器（与皮肤编辑器互斥显示，见 `OsuGame.cs`） |
| `osu.Game/EzOsuGame/Layout/EzLayoutContainer.cs` | Ez 布局容器实现（`DefaultSkinComponentsContainer` 子类） |
| `osu.Game/EzOsuGame/UI/EzUIStyleV1.cs` / `EzMaterialPanelBackground.cs` / `EzMaterialOverlayAlpha.cs` | Ez 视觉基元（材质面板、透明度联动） |
| `osu.Game/Overlays/OverlayColourProvider.cs` | 每个界面构造时传入 `OverlayColourScheme` 决定配色 |

---

## B. 主菜单

入口：`osu.Game/Screens/Menu/MainMenu.cs`（构图在其 `load()`）。

| 文件 | 职责 |
|---|---|
| `Screens/Menu/MainMenu.cs` | 屏入口：按钮系统、SongTicker、提示流、背景调色 |
| `Screens/Menu/ButtonSystem.cs` / `ButtonArea.cs` / `MainMenuButton.cs` / `FlowContainerWithOrigin.cs` | 按钮群布局与单颗按钮 |
| `Screens/Menu/OsuLogo.cs` | 中心 LOGO（**实例由 `OsuGame` 创建并缓存**，MainMenu 只做 Proxy 挂入） |
| `Screens/Menu/LogoVisualisation.cs` / `MenuLogoVisualisation.cs` | LOGO 频谱可视化基类 / 默认实现 |
| `Screens/Menu/SongTicker.cs` / `MenuTipDisplay.cs` / `OnlineMenuBanner.cs` / `SupporterDisplay.cs` | 角标信息条 |
| `Screens/Menu/KiaiMenuFountains.cs` / `IntroScreen.cs` / `IntroSequence.cs` / `IntroCircles.cs` / `IntroTriangles.cs` | 背景与入场动画 |
| `Screens/Menu/HoldToExitGameOverlay.cs` / `ConfirmExitDialog.cs` / `StorageErrorDialog.cs` | 弹窗 |
| `EzOsuGame/Screens/Menu/EzMenuLogoVisualisation.cs` | **Ez**：实际使用的 LOGO 可视化 |
| `EzOsuGame/Screens/Menu/EzMenuLogoBackdrop.cs` / `EzMenuLogoCentreText.cs` / `EzLogoBackdropSampler.cs` | **Ez**：LOGO 背板 / 中心文字 / 取色采样 |

注意：`MenuSideFlashes.cs`、`StarFountain.cs` 当前无引用点（历史组件）。

---

## C. 选歌 Song Select

入口：`osu.Game/Screens/Select/SoloSongSelect.cs` → `Screens/Select/SongSelect.cs`。
构图：`SongSelect.load()`，`mainContent` 内为 `OsuContextMenuContainer → PopoverContainer → GridContainer(mainGridContainer) → 左 wedges + 右 carousel`；`FooterContent` 在构造函数里设为 `ScreenFooterContent.Argon(CreateFooterButtons)`。

| 文件 | 职责 |
|---|---|
| `Screens/Select/SongSelect.cs` | 屏入口 + 整体布局（左楔形信息区 / 右列表区 / Footer / 皮肤层） |
| `Screens/Select/SoloSongSelect.cs` | Solo 实例（进入游玩） |
| `Screens/Select/SongSelect.EzStartup.cs` | **Ez**：启动预热分部，不参与绘制 |
| `Screens/Select/BeatmapTitleWedge.cs` + `.FavouriteButton/.DifficultyDisplay/.DifficultyStatisticsDisplay/.SpreadDisplay/.Statistic*.cs` | 左上标题楔形及其子块 |
| `Screens/Select/BeatmapMetadataWedge.cs` + `.MetadataDisplay/.TagsLine/.UserRatingDisplay/.SuccessRateDisplay/.RatingSpreadDisplay/.FailRetryDisplay.cs` | 谱面元数据楔形 |
| `Screens/Select/BeatmapDetailsArea.cs` / `BeatmapDetailsArea.Header.cs` / `BeatmapDetailsArea.WedgeSelector.cs` | 左下细节区与楔形切换 |
| `Screens/Select/BeatmapLeaderboardWedge.cs` / `BeatmapLeaderboardScore.cs` / `.Tooltip.cs` | 排行榜楔形 |
| `Screens/Select/BeatmapCarousel.cs` | 右侧列表核心（滚动、选择、键盘/滚轮） |
| `Screens/Select/Panel.cs` / `PanelBeatmap.cs` / `PanelBeatmapSet.cs` / `PanelBeatmapStandalone.cs` + 各 `.SpreadDisplay.cs` | 列表卡片 |
| `Screens/Select/PanelGroup.cs` / `PanelGroupRankedStatus.cs` / `PanelGroupStarDifficulty.cs` / `PanelGroupRankDisplay.cs` / `PanelLocalRankDisplay.cs` | 分组头 |
| `Screens/Select/FilterControl.cs` + `.DifficultyRangeSlider/.ScopedBeatmapSetDisplay` / `NoResultsPlaceholder.cs` | 搜索框与空态 |
| `Screens/Select/FooterButtonMods.cs` / `FooterButtonRandom.cs` / `FooterButtonOptions.cs(+.Popover)` | vendor footer 按钮 |
| `Screens/Select/LeftSideInteractionContainer.cs` / `SongSelectTouchInputDetector.cs` / `ModSpeedHotkeyHandler.cs` | 输入交互 |
| `Screens/Select/WedgeBackground.cs` / `EzOsuGame/UI/EzSongSelectWedgeBackground.cs` | 背景绘制（vendor / **Ez**） |
| `Screens/Select/BeatmapDeleteDialog.cs` / `BeatmapClearScoresDialog.cs` / `LocalScoreDeleteDialog.cs` / `UpdateLocalConfirmationDialog.cs` | 弹窗 |
| `EzOsuGame/Overlays/FooterButtonEzExport.cs` / `FooterButtonEzPreView.cs` | **Ez** footer 按钮 |
| `EzOsuGame/Overlays/EzBeatmapPreviewOverlay.cs` + `Overlays/Preview/*`（`StaticFullMapPreviewRenderer.cs`、`StaticScrollPreviewRenderer.cs`、`ManiaPreview*` 等） | **Ez** 谱面预览层与渲染器 |
| `EzOsuGame/Overlays/EzUserModSelectOverlay.cs`（由 `SongSelect.CreateModSelectOverlay()` 实例化） | **Ez** 选歌态 Mod 选择 |
| `EzOsuGame/Screens/Select/EzSoloSongSelect.QuickRotation.cs` | **Ez** 快速轮换分部 |
| `EzOsuGame/Overlays/CollectionZipExporter.cs` / `SelectedBeatmapExporter.cs` / `VisibleBeatmapZipExporter.cs` | 导出逻辑（非绘制） |

皮肤目标层：`skinnableContent = new SkinnableContainer(new GlobalSkinnableContainerLookup(GlobalSkinnableContainers.SongSelect))` —— Ez 皮肤组件在选歌场景的落点。

---

## D. 游玩与 HUD

入口：`osu.Game/Screens/Play/Player.cs`（`HUDOverlay` 字段 + `CreateHUDOverlay`），HUD 骨架：`osu.Game/Screens/Play/HUDOverlay.cs`（**构造函数**里搭 `FailingLayer` / `HUDComponentsContainer` / `rulesetComponents` / `PlayfieldSkinLayer` / `TopLeft·TopRight·BottomRight` 容器）。

### D1 vendor 默认 HUD（`Screens/Play/HUD/`）

| 文件 | 职责 |
|---|---|
| `Screens/Play/HUDOverlay.cs` | HUD 根容器与分区 |
| `Screens/Play/HUD/FailingLayer.cs` | 失败红屏层 |
| `HealthDisplay.cs` / `DefaultHealthDisplay.cs` / `ArgonHealthDisplay.cs`(+`ArgonHealthDisplayParts/`) | 血条 |
| `GameplayScoreCounter.cs` / `DefaultScoreCounter.cs` / `ArgonScoreCounter.cs` / `PerformancePointsCounter.cs` / `MatchScoreDisplay.cs` | 分数 |
| `ComboCounter.cs` / `DefaultComboCounter.cs` / `ArgonComboCounter.cs` / `LongestComboCounter.cs` | 连击 |
| `GameplayAccuracyCounter.cs` / `DefaultAccuracyCounter.cs` / `ArgonAccuracyCounter.cs` / `Default300GRateCounter.cs` / `UnstableRateCounter.cs` / `BPMCounter.cs` | 准确率 / UR / BPM |
| `SongProgress.cs` / `SongProgressBar.cs` / `SongProgressGraph.cs` / `SongProgressInfo.cs` / `ArgonSongProgress*.cs` / `DefaultSongProgress*.cs` | 进度条区 |
| `KeyCounterDisplay.cs` / `DefaultKeyCounterDisplay.cs` / `KeyCounter*.cs` / `InputCountController.cs` / `JudgementCounter/` / `ClicksPerSecond/` / `HitErrorMeters/` | 输入统计 |
| `DrawableGameplayLeaderboard.cs` / `SpectatorList.cs` / `PlayerAvatar.cs` | 游戏内排行榜 / 观众 |
| `ModDisplay.cs` / `ModFlowDisplay.cs` / `SkinnableModDisplay.cs` | Mod 图标区 |
| `ReplayOverlay.cs` / `ReplaySettingsOverlay.cs` / `HoldForMenuButton.cs` | 回放与暂停入口 |

其它游玩层叠：`Screens/Play/PauseOverlay.cs`、`FailOverlay.cs`、`BreakOverlay.cs`、`SkipOverlay.cs`、`LetterboxOverlay.cs`、`ComboEffects.cs`、`ScrollingMessage.cs`。

### D2 Ez HUD（`EzOsuGame/HUD/`）

| 文件 | 职责 |
|---|---|
| `EzHUDScoreCounter.cs` / `EzHUDAccuracyCounter.cs` / `EzHUDHitResultScore.cs` | Ez 分数 / 判定显示 |
| `EzHUDDanDualPanel.cs` / `EzHUDRadarPanel.cs` / `EzHUDScoreCompareBars.cs` / `EzHUDScoreRaceComponent.cs` / `EzHUDScoreRaceLeaderboard.cs` | Dan / 雷达 / 分差 / 竞速 |
| `EzHUDCamera.cs` / `EzHUDDynamicSpeedDisplay.cs` | 摄像机占位 / 动态变速 |
| `EzSongProgressMarkerLayer.cs` / `EzSongProgressMissOverlay.cs` / `EzSongProgressRestOverlay.cs` / `EzArgonSongProgress.cs` | 进度条叠加层 |
| `EzAudioVisualiser.cs` / `EzAudioVisualiserScale.cs` | 可视化 |
| `EzHUDSpritePlus.cs` / `EzSpriteText.cs` / `EzScoreText.cs` / `EzComboText.cs` / `EzBoxElement.cs` / `NeriBoxElement.cs` | Ez 绘制原子 |
| `IEzHudThemeTarget.cs` | 皮肤主题槽契约（编辑器里按此类型扫描） |

**Ez HUD 不在 `HUDOverlay` 里 new**：多数实现 `ISerialisableDrawable`，由皮肤/布局容器注入（`Skinning/Ez2Skin.cs`、`EzStyleProSkin.cs`、`EzOsuGame/Layout/EzLayoutLayer.cs`）。

### D3 Mania 规则集

入口：`osu.Game.Rulesets.Mania/UI/DrawableManiaRuleset.cs`。

| 文件 | 职责 |
|---|---|
| `Rulesets.Mania/UI/DrawableManiaRuleset.cs` / `ManiaPlayfield.cs` / `Stage.cs` / `Column.cs` / `ColumnFlow.cs` / `ManiaPlayfieldAdjustmentContainer.cs` | Playfield 结构与布局 |
| `Rulesets.Mania/UI/Components/DefaultKeyArea.cs` / `DefaultColumnBackground.cs` / `DefaultHitTarget.cs` / `ManiaJudgementLine.cs` / `EzStageBackdrop.cs` | 列背景 / 判定线（`EzStageBackdrop` 为 Ez） |
| `Rulesets.Mania/UI/DrawableManiaJudgement.cs` / `DefaultManiaJudgementPiece.cs` / `DefaultHitExplosion.cs` | 判定与爆炸 |
| `Rulesets.Mania/EzMania/HUD/*.cs` | **Ez** Mania HUD：`EzHUDComboCounter*`、`EzHUDHitTiming*`、`EzKeyCounter*`、`EzHUDO2JamPillFlow`、`EzHUDSongProgress`、`EzJudgementText` 等 |

---

## E. 结算

入口：`osu.Game/Screens/Ranking/ResultsScreen.cs`（`load()` 内 `PopoverContainer → GridContainer → VerticalScrollContent → StatisticsPanel / ScorePanelList / detachedPanelContainer`）；实例 `SoloResultsScreen.cs`。

| 文件 | 职责 |
|---|---|
| `Screens/Ranking/ResultsScreen.cs` | 屏入口与布局 |
| `Screens/Ranking/ScorePanelList.cs` / `ScorePanel.cs` / `ScorePanelTrackingContainer.cs` / `AspectContainer.cs` | 分数卡列表与单卡 |
| `Screens/Ranking/Expanded/*`（`ExpandedPanelTopContent`、`ExpandedPanelMiddleContent`、`TotalScoreCounter`、`Statistics/*`、`Accuracy/*`） | 展开态 |
| `Screens/Ranking/Contracted/*` | 收起态 |
| `Screens/Ranking/Statistics/*`（`StatisticsPanel`、`PerformanceBreakdownChart`、`HitEventTimingDistributionGraph`、`AverageHitError`、`UnstableRate`） | 右侧统计分析 |
| `Screens/Ranking/RetryButton.cs` / `CollectionButton.cs` / `ReplayDownloadButton.cs` / `FavouriteButton.cs` / `UserTagControl*.cs` | footer 与弹窗 |
| `EzOsuGame/Statistics/ResultsEnvironmentControls.cs` | **Ez**：结算 footer 的判定/血量/偏移控件 |
| `EzOsuGame/Screens/Rotation/EzQuickRotationResults.cs` | **Ez**：QuickRotation 结算按钮 |

`Screens/RankingV2/`（`Argon/ArgonTotalScorePanel.cs`、`ArgonScoreStatisticsGrid.cs`、`ArgonScoreRankDisplay.cs`、`GradedCirclesV2.cs`、`ArgonPlayerInfoPanel.cs`、`ArgonBeatmapInfoPanel.cs`、`Legacy/LegacyRankingBackgroundOverlay.cs`）**不是结算屏**，是可序列化皮肤组件集（`ISerialisableDrawable`），当前无 C# 挂载点，靠皮肤布局使用。

---

## F. 皮肤编辑器（两套并存）

### F1 官方 Skin Editor — `osu.Game/Overlays/SkinEditor/`

挂载：`OsuGame.cs` L1313 `loadComponentSingleFile(skinEditor = new SkinEditorOverlay(ScreenContainer), overlayContent.Add, true)`。

| 文件 | 职责 |
|---|---|
| `SkinEditorOverlay.cs` | **外层 Overlay**：`SetTarget(screen)` 绑定目标屏、`updateScreenSizing()` 用 `scalingContainer.SetCustomRect` 把场景缩进编辑器可视区、`PresentGameplay()` 推 `EndlessPlayer`、`ToggleEzSkinEditor()` 进 Ez 编辑器、临时覆盖 `BeatmapSkins` / `HUDVisibilityMode` |
| `SkinEditor.cs` | **编辑器主体**（`load()` 三行 Grid）：① `EditorMenuBar` + 右上 `HeaderText`；② `SkinEditorSceneLibrary`；③ 三栏 `EzEditorComponentsSidebar` / `content` / `EzEditorSettingsSidebar` |
| `SkinEditor.EzMenu.cs` | **Ez**：菜单栏追加「Ez 设置」项（HUD 吸附开关/距离/像素对齐） |
| `SkinEditorSceneLibrary.cs` | 场景条：SongSelect / Gameplay 按钮（`SceneButton`） |
| `SkinBlueprintContainer.cs` + `.HudSnap.cs` / `SkinBlueprint.cs` | 场景内容层：蓝图选择框、拖动/缩放，Ez HUD 吸附逻辑 |
| `SkinSelectionHandler.cs` / `SkinSelectionScaleHandler.cs` / `SkinSelectionRotationHandler.cs` | 选中与变换手势 |
| `SkinComponentToolbox.cs` / `SkinSettingsToolbox.cs` | 左栏「添加组件」列表 / 右栏单组件参数区 |
| `SkinEditorChangeHandler.cs` | 撤销/重做 |
| `NonSkinnableScreenPlaceholder.cs` | 目标屏无皮肤容器时的占位 |
| `ExternalEditOverlay.cs` | 外部编辑（脚本皮肤）状态遮罩 |
| `EzOsuGame/Layout/EzLayoutEditor.cs` | **Ez**：`SkinEditor` 的派生类，指向 `EzLayoutLayer`；顶栏/场景条/侧栏全部复用父类，只覆盖右上标题文本（`LAYOUT_EDITOR_TITLE`/`SUBTITLE`）与保存逻辑 |
| `EzOsuGame/SkinEditor/SkinHudSnap*.cs`（`SkinHudSnapHelper`、`SkinHudSnapRuntime`、`SkinHudSnapSettings`、`SkinHudSnapDragState`、`SkinHudSnapBounds`、`SkinHudSnapDistanceMenuItem`） | **Ez** HUD 吸附算法与菜单项 |
| `EzOsuGame/Screens/EzEditorComponentsSidebar.cs` / `EzEditorSettingsSidebar.cs` / `EzEditorSectionComponentsSidebar.cs` / `EzEditorSectionSettingsSidebar.cs` / `EzColumnTab.cs` / `EzEditorComponentType.cs` | **Ez** 左右侧栏容器与分区 |

要点：编辑对象是目标屏里的 `SkinnableContainer`；`content.Child = new SkinBlueprintContainer(skinComponentsContainer)` 决定「中间那块可编辑画布」。改画布交互 → `SkinBlueprint*`；改侧栏 → `EzEditor*Sidebar`。

### F2 Ez 皮肤编辑器 — `osu.Game/EzOsuGame/Edit/`

入口：设置 → `Overlays/Settings/Sections/SkinSection.cs` L115 `skinEditor?.ToggleEzSkinEditor()` → `Push(new EzSkinEditorScreen())`。

| 文件 | 职责 |
|---|---|
| `Edit/EzSkinEditorScreen.cs` | **屏入口**：`backgroundContainer` + `GridContainer{ menuBar / sceneBar / 三栏(sidebar, sceneContentHost, …) }`，驱动预览、快照、skin.ini / EzSkin.json 会话、内嵌播放器 |
| `Edit/Components/EzSkinEditorMenuBar.cs` | 顶栏菜单（logo/文本、配置菜单） |
| `Edit/Components/EzSkinEditorTopToolbar.cs` | 顶栏工具栏（预览/对比开关等） |
| `Edit/Components/EzSkinEditorSceneBar.cs` | 场景切换条 + 进度/循环显示（`LoopMarker.cs`、`LoopIntervalDisplay.cs`） |
| `Edit/Components/EzSkinEditorSidebar.cs` / `EzSkinEditorSettingsGroup.cs` | 右侧设置侧栏与分组容器 |
| `Edit/Components/EzSkinEditorSkinDropdown.cs` | 皮肤切换下拉（皮肤专用侧栏的入口） |
| `Edit/Components/EzSkinEditorBeatmapMenuButton.cs` / `EzSkinEditorPreviewHost.cs` / `EzSkinEditorBeatmapPreviewHost.cs` | 谱面选择与预览宿主 |
| `Edit/Components/EzSkinEditorAppearanceSceneContent.cs` | 外观场景内容 |
| `Edit/Components/EzSkinEditorEmbeddedPlayer*.cs` | 内嵌游玩预览 |
| `Edit/Components/EzSkinEditorNoteComparisonHost.cs` / `EzSkinEditorNoteComparisonPane.cs` / `EzSkinEditorNoteDrawablePreview.cs` / `Edit/Note/` | Note 场景对比与预览 |
| `Edit/Components/EzSkinEditorPlaybackControls.cs` | 播放控制 |
| `Edit/Scenes/*.cs`（`AppearanceSceneStrategy`、`SizeSceneStrategy`、`ColourSceneStrategy`、`SkinIniSceneStrategy`、`NoteSceneStrategy`，注册表 `EzSkinEditorSceneRegistry.cs`） | **场景策略**：一个场景 = 一套内容构建 + 设置分区 |
| `Edit/Settings/Sections/*.cs`（13 个 Section） | 侧栏各设置分区：底色/列色/Note 编辑/规则集/脚本皮肤/尺寸/skin.ini（通用·Mania·颜色）/皮肤专用/Stage/贴图 |
| `Edit/EzSkinIni*.cs` / `EzSkinJson*.cs` / `EzSkinEditorComparisonSnapshot.cs` / `EzSkinEditorPreviewState.cs` / `SkinEditorProviderRegistry.cs` | 数据与桥接层（非绘制，但侧栏控件直接绑定） |
| `Edit/EzSkinEditorBeatmapPicker.cs` | 随机挑可玩谱面 |

按 Issue #193 的落点参考：
- logo 与文本重叠 → `osu.Game/Screens/Edit/Components/Menus/EditorMenuBar.cs` 的标题区（官方 `SkinEditor` 与谱面 `Editor` 共用；Ez 全屏编辑器的 `EzSkinEditorMenuBar` 继承它）
- HUD 整套显示开关、「编辑 HUD」跳回官方编辑器 → `EzSkinEditorTopToolbar.cs` / `EzSkinEditorMenuBar.cs`（跳转调 `SkinEditorOverlay.Show()`）
- 皮肤专用侧栏 → `EzSkinEditorSkinDropdown.cs` + `Settings/Sections/EzSkinEditorSkinSpecificSettingsSection.cs`
- 分区/分组整理 → `EzSkinEditorSidebarGroupDefinition.cs` + `Settings/Sections/`
- 进度条/状态按钮重做 → `EzSkinEditorSceneBar.cs`、`EzSkinEditorPlaybackControls.cs`

---

## G. 设置

入口：`osu.Game/Overlays/SettingsOverlay.cs`，section 列表**硬编码**在其 `load()`（L82–L117）：`General / Skin / Input / UserInterface / Gameplay / EzGameSection / [RulesetSection] / Audio / Graphics / Online / Maintenance / Debug / EzCustomSection`（首启引导 `Overlays/FirstRunSetup/ScreenBehaviour.cs` 复用同一批）。

| 文件 | 职责 |
|---|---|
| `Overlays/SettingsOverlay.cs` | 设置入口与 section 注册 |
| `Overlays/SettingsPanel.cs` / `SettingsSubPanel.cs` / `Overlays/OverlaySidebar.cs` | 面板骨架（侧栏 + 内容） |
| `Overlays/Settings/SettingsSection.cs` / `SettingsSubsection.cs` / `SettingsSidebar.cs` / `SettingsSubsectionNavColumn.cs` / `SettingsHeader.cs` / `SettingsFooter.cs` | 页面结构 |
| `Overlays/Settings/SettingsItem*.cs` / `SettingsCheckbox.cs` / `SettingsSlider.cs` / `SettingsDropdown.cs` / `SettingsTextBox.cs` / `SettingsColour.cs` / `SettingsNote.cs` / `EzSettingsSectionDivider.cs` | 控件库 |
| `Overlays/Settings/Sections/*.cs` | vendor 各页 |
| `EzOsuGame/Overlays/EzGameSection.cs` / `EzCustomSection.cs` | **Ez** 页（后者的子页：`EzUISettings`、`EzGameplaySettings`、`EzQuickRotationSettings`、`EzStdSettings`、`EzGameModeSettings`、`EzSkillSettings`、`EzExperimentalSettings`、`ServerSettings`、`EzCustomSettings`） |
| `EzOsuGame/Configuration/Ez2ConfigManager.cs` | `Ez2Setting` 枚举与 Bindable 来源（设置项定义处） |

---

## H. Mod 选择

入口：`osu.Game/Overlays/Mods/ModSelectOverlay.cs`（实际实例化 `EzOsuGame/Overlays/EzUserModSelectOverlay.cs`）。

| 文件 | 职责 |
|---|---|
| `Overlays/Mods/ModSelectOverlay.cs` / `UserModSelectOverlay.cs` | Overlay 入口 |
| `Overlays/Mods/ShearedOverlayContainer.cs` | 斜切 Overlay 骨架（`Header` / `MainAreaContent` / `FooterContent`） |
| `Overlays/Mods/ModSelectColumn.cs` / `ModColumn.cs` / `ModSelectPanel.cs` / `ModPanel.cs` / `IncompatibilityDisplayingModPanel.cs` | 列与单颗 Mod |
| `Overlays/Mods/ModCustomisationPanel.cs` / `ModCustomisationSection.cs` / `ModCustomisationHeader.cs` | 参数自定义面板 |
| `Overlays/Mods/ModPreset*.cs` / `AddPresetButton.cs` / `EditPresetPopover.cs` | 预设列 |
| `Overlays/Mods/RankingInformationDisplay.cs` / `BeatmapAttributesDisplay.cs` / `VerticalAttributeDisplay.cs` | 属性/评级预览 |
| `Overlays/Mods/ModFooterInformationDisplay.cs` / `ModSelectFooterContent.cs` / `SelectAllModsButton.cs` / `DeselectAllModsButton.cs` | footer |
| `EzOsuGame/Overlays/EzUserModSelectOverlay.cs` / `EzModSelectFooterContent.cs` / `EzDeselectRestoreModsButton.cs` / `PinnedModColumn.cs` / `PinnedModPanel.cs` | **Ez** 扩展 |
| `EzOsuGame/Overlays/Preview/EzModSettingsDock.cs` / `EzPreviewSelectOverlay.cs` | **Ez** 预览相关 |

---

## I. 谱面编辑器

入口：`osu.Game/Screens/Edit/Editor.cs`（`load()` 搭「顶栏 `EditorMenuBar` + 子屏区 `ScreenContainer` + `BottomBar`」；子屏由 `EditorScreenSwitcherControl` 切换：Setup / Compose / Design / Timing / Verify）。

| 文件 | 职责 |
|---|---|
| `Screens/Edit/Editor.cs` / `EditorScreen.cs` / `EditorScreenWithTimeline.cs` | 入口与子屏基类（timeline + `MainContent` 网格） |
| `Screens/Edit/Components/Menus/EditorMenuBar.cs` / `EditorMenuItem.cs` / `EditorScreenSwitcherControl.cs` | 顶栏 |
| `Screens/Edit/BottomBar.cs` / `Components/BottomBarContainer.cs` / `Components/PlaybackControl.cs` / `TimeInfoContainer.cs` | 底栏 |
| `Screens/Edit/Components/EditorSidebar.cs` / `EditorSidebarSection.cs` | 侧栏骨架 |
| `Screens/Edit/Compose/ComposeScreen.cs` + `Compose/Components/Timeline/*` | Compose 子屏与时间轴 |
| `Screens/Edit/Timing/*` / `Setup/*` / `Design/*` / `Verify/*` | 其余子屏 |
| `EzOsuGame/Edit/EzEditorColumnEditState.cs` | **Ez**：列编辑状态（顶栏 `ColumnEditMode` 开关） |

注意：`EzOsuGame/Screens/EzEditor*Sidebar.cs` 实际服务的是**皮肤编辑器**，不是谱面编辑器。

---

## J. 新界面选型 / 通用骨架

| 基类文件 | 适用 |
|---|---|
| `Overlays/FullscreenOverlay.cs` + `OverlayView.cs` + `Overlays/TabControlOverlayHeader.cs` + `OverlaySidebar.cs` | 在线浏览类全屏页（BeatmapSet / Rankings 那类） |
| `Overlays/Mods/ShearedOverlayContainer.cs` | 斜切风 + footer 的全屏界面 |
| `Overlays/WaveOverlayContainer.cs` | 波纹进出场的旧式全屏 Overlay |
| `Overlays/SettingsPanel.cs` + `SettingsSection/SettingsSubsection` | 设置类页面（注册点见 G） |
| `Screens/OsuScreen.cs` + `Screens/Footer/ScreenFooter.cs` | 游戏屏 |
| `Overlays/OverlayContainer.cs` | 轻量浮层（如 `SkinEditorOverlay`） |
| `EzOsuGame/UI/EzMaterialPanelBackground.cs` | Ez 风格面板底 |

新增界面时，请在本文件追加对应小节（入口类 + 构图方法 + 文件表）。

---

## K. 高频需求 → 落点速查

| 需求 | 改哪里 |
|---|---|
| 选歌左侧信息楔形 | `Screens/Select/BeatmapTitleWedge*.cs` / `BeatmapMetadataWedge*.cs` |
| 选歌卡片外观 | `Screens/Select/Panel*.cs` |
| 选歌 footer 按钮 | `SongSelect.CreateFooterButtons()` + `Screens/Select/FooterButton*.cs` / `EzOsuGame/Overlays/FooterButtonEz*.cs` |
| 选歌背景 | `Screens/Select/WedgeBackground.cs`、`EzOsuGame/UI/EzSongSelectWedgeBackground.cs` |
| 皮肤编辑器顶栏 | `Overlays/SkinEditor/SkinEditor.cs`（Menu container）+ `SkinEditor.EzMenu.cs` |
| 编辑器顶栏 logo/标题区（与菜单起点） | `Screens/Edit/Components/Menus/EditorMenuBar.cs`（`heading_area`） |
| 皮肤编辑器画布交互 | `Overlays/SkinEditor/SkinBlueprint*.cs`、`SkinSelection*Handler.cs` |
| 皮肤编辑器侧栏 | `EzOsuGame/Screens/EzEditor*Sidebar.cs`、`Overlays/SkinEditor/SkinComponentToolbox.cs` |
| Ez 皮肤编辑器任意区域 | `EzOsuGame/Edit/Components/EzSkinEditor*.cs` |
| Ez 皮肤编辑器场景 | `EzOsuGame/Edit/Scenes/*.cs` + `EzSkinEditorSceneRegistry.cs` |
| Ez 皮肤编辑器设置分区 | `EzOsuGame/Edit/Settings/Sections/*.cs` |
| HUD 组件 | `Screens/Play/HUD/*`、`EzOsuGame/HUD/*`、`Rulesets.Mania/EzMania/HUD/*` |
| 结算面板 | `Screens/Ranking/**`、`Screens/RankingV2/**`（皮肤组件） |
