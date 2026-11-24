# EzAndroid

`EzAndroid` 是 Android 平台专属扩展的隔离目录，命名空间根为 `osu.Android.EzAndroid`。平台策略、移动端专属组件和触屏适配应归入此处；通用游戏逻辑仍由共享项目承载。

## 职责边界

- 放置 Android 专属扩展，以及通过组合或策略接入的触屏 Policy。
- 不在此目录复制共享实现；优先让 Android 装配层选择专属实现。
- 专属 Ruleset 使用独立的 `osu.Game.Rulesets.<Name>` 程序集，并仅由 `osu.Android/osu.Android.csproj` 引用。

## 低侵入顺序

按影响从小到大选择实现方式：

1. **装配隔离**：共享逻辑不变，由 Android 项目选择专属实现。
2. **扩展与策略**：通过组合、继承或独立策略承载 Android 行为；需要共享层扩展点时先在主干建立桌面无副作用的 Hook。
3. **入口限制**：无法在装配层隔离时，再隐藏入口或使用平台守卫。
4. **物理裁剪**：仅在前述方式不适用时删除源码，并登记工作区 Android 裁剪清单。

## 已接入模块

- `Input/`：通过 `IMobileNumericControlFactory` 接管表单与设置滑条，提供数字输入、加速步进，并保留各自的复位与提交行为（Issue #53）。

其余触屏重构和 Android 专属 Ruleset 分别跟踪于 [Issue #39](https://github.com/KazamataNeri-Love/Ez2Lazer/issues/39) 与 [Issue #40](https://github.com/KazamataNeri-Love/Ez2Lazer/issues/40)。
