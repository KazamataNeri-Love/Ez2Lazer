# Ez2Lazer 文档导航

本目录同时包含实现说明、工作流程、设计/研究记录和用户指南。请先看每篇文档开头的状态提示：实现说明应与当前源码相符；计划、研究和测量记录只在标明的范围与日期内有效。

## 架构与实现说明

| 文档 | 用途 / 状态 |
|---|---|
| [UI-SCREEN-MAP.md](./UI-SCREEN-MAP.md) | 界面入口、布局和代码落点索引；新增或迁移界面时维护 |
| [scoring/mania.md](./scoring/mania.md) | Mania 判定与模式实现说明 |
| [scoring/session.md](./scoring/session.md) | 跨模式 Session、Timeline、Offset 和 Race ghost 行为 |
| [scoring/performance.md](./scoring/performance.md) | 性能测量口径、已验证结论与未完成项 |
| [mania/stable-classic-hold-note.md](./mania/stable-classic-hold-note.md) | Stable classic 长按判定专题 |
| [EzSkinSystemNotes.md](./EzSkinSystemNotes.md) | Ez 皮肤与资源加载机制、常见排查入口 |
| [ScriptedSkin-Security.md](./ScriptedSkin-Security.md) | ScriptedSkin 安全边界与执行限制 |
| [skills/Skills-README.md](./skills/Skills-README.md) | 技能分析架构、引擎、存储链与版本语义 |

## 开发流程与维护清单

| 文档 | 用途 / 状态 |
|---|---|
| [EZ-UPSTREAM-MERGE.md](./EZ-UPSTREAM-MERGE.md) | 上游同步流程；其中 2026.819 清查是历史记录，不代表当前审计结果 |
| [EZ_CHANGELOG_RELEASE_FORMAT.md](./EZ_CHANGELOG_RELEASE_FORMAT.md) | GitHub Release 正文与游戏内更新日志格式 |
| [skills/418K-Alignment-PLAN.md](./skills/418K-Alignment-PLAN.md) | 4–18K 引擎对齐实施记录；以现状和开放项为准 |
| [skills/DATA-FOLLOWUPS.md](./skills/DATA-FOLLOWUPS.md) | 技能数据的长期待办、已完成事项与已知限制 |

## 设计、研究与历史测量

| 文档 | 用途 / 状态 |
|---|---|
| [EZ_ANALYSIS_STORAGE_REDESIGN.md](./EZ_ANALYSIS_STORAGE_REDESIGN.md) | 分析存储设计记录；开头列有当前源码基线，历史方案段落不等同于现状 |
| [mania_scoring_formula_research_v1.md](./mania_scoring_formula_research_v1.md) | 计分 V1 研究稿；现行公式已演进，文首提供实现对照 |
| [song-select-draw.md](./song-select-draw.md) | 2026-11-11 至 2026-11-12 的性能调查记录，不作为当前功能清单 |

## 用户与资源指南

| 文档 | 用途 / 状态 |
|---|---|
| [桌宠使用说明.md](./桌宠使用说明.md) | 桌宠启用与配置；**仅纯净侧 `Ez2Lazer` 适用**，Edge 与 Android 已于 2026-11-20 移除桌宠，本文在 Edge 仅作历史参考 |
| [Pets/Live2D测试指南.md](./Pets/Live2D测试指南.md) | Live2D 桌宠测试步骤；同上，Edge/Android 已移除 |
| [Pets/Release资源包规范.md](./Pets/Release资源包规范.md) | 桌宠资源包格式与发布要求；同上，Edge/Android 已移除 |

## 来源与维护口径

此索引按用途和时效分类，不据此推断作者。与指定上游 `KazamataNeri-Love/Ez2Lazer` 的 `master` 对照至 2026-11-20：当前 `docs/` 中的 Markdown 文件均已存在于上游 `0c60dcf`；因此仅凭目录位置或文件存在性，不能把它们标为本地独有或判定由 AI 撰写。需要核对来源时，应查相应提交历史和具体差异。

仓库根目录的 README、贡献指南、许可文件及第三方组件说明仍按仓库原有用途维护，不在本导航中重复收录。
