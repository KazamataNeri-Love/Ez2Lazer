# Ez2Lazer Android

`Neri/Android` 是基于 `master` 的 Android 移动端衍生线。本仓库 README 只记录 Android 相对 `master` 的差异、构建验证与移动端演进约定；共有功能请查 [Ez2Lazer 主仓库](https://github.com/KazamataNeri-Love/Ez2Lazer) 的 `master` 文档。

## 当前相对 master 的差异

### Android 16 KB 页对齐

Android 端以 16 KB 页对齐要求构建原生依赖，避免 Android 16 设备加载共享库失败。当前通过 `osu.Android/osu.Android.csproj` 覆盖 framework 传递版本：

- `McEndu.FreeTypeSharp`：`4.1.5.16`
- `ppy.Veldrid.SPIRV`：`1.0.15.16`
- `ez2lazer.Veldrid`：`2026.1110.3`

依赖来源、重建方法与检查步骤见 [Android 16 KB 页对齐](docs/android-16kb-page-size.md)。Release 构建需检查 `XA0141` 为 0，并用 `zipalign -c -P 16 -v 4` 验证 APK，输出应包含 `Verification successful`。

### Android 自建资源包覆盖

为修复 OpenGL ES 3.20（如 Adreno 650）下 `CursorTrail/Texture` 着色器因缺少 `v_UseVertexMask` / `v_MaskingRect` / `v_MaskParameters` 输出导致的链接失败与启动卡加载页问题，`Ez2Lazer.Dependencies.props` 当前将 `ez2lazer.Game.Resources` 升级为本地自建版本 `2026.1124.0-neri.1`（随 `ez-local` 提供；待上游资源包包含该修复并合入 `master` 后可恢复统一版本）。

### Ruleset 保留

Android 项目当前继续引用 osu! 官方四个 Ruleset：Catch、Mania、Osu、Taiko。Android 衍生线的规划不代表移除或替换其中任何一个。

### 已裁剪项索引

桌宠（Live2D）源码及其依赖已从 `master` 移除，Android 线继承该状态；裁剪范围及同步时的保留规则见工作区 [Android 裁剪清单](../Memory/android/pruning-checklist.md)。摄像头模块的物理移除由 [Issue #38](https://github.com/KazamataNeri-Love/Ez2Lazer/issues/38) 跟踪，完成前不视为已裁剪。

## Android 专属开发约定

Android 专属扩展与触屏策略统一规划在 `osu.Android/EzAndroid/`，遵守先装配隔离、再平台入口控制、最后才物理裁剪的低侵入顺序。目录职责与命名空间约定见 [EzAndroid 目录说明](osu.Android/EzAndroid/README.md)。

- [触屏界面比例、布局与输入逻辑规划（Issue #39）](https://github.com/KazamataNeri-Love/Ez2Lazer/issues/39)
- [Android 专属 Ruleset 规划（Issue #40）](https://github.com/KazamataNeri-Love/Ez2Lazer/issues/40)

以上均为规划项，尚不代表相关功能已实现。Android 分支只接收来自 `master` 的同步；通用修复先进入上游轨道，再随同步进入 Android，不反向合流到 `master` 或 `Edge`。

## 构建与验证

在仓库根目录执行：

```powershell
& 'D:\Environments\dotnet\dotnet.exe' build osu.Android/osu.Android.csproj -c Release -m:1 `
  -p:AndroidSdkDirectory=D:\Environments\android-sdk `
  -p:JavaSdkDirectory=D:\Environments\Java\jdk-21.0.13.0.2+1 --nologo

$apk = Get-ChildItem osu.Android/bin/Release -Recurse -Filter '*-Signed.apk' |
  Sort-Object LastWriteTime -Descending | Select-Object -First 1
& 'D:\Environments\android-sdk\build-tools\36.0.0\zipalign.exe' -c -P 16 -v 4 $apk.FullName
```

同步 `master` 后，按 [Android 裁剪清单](../Memory/android/pruning-checklist.md) 核对裁剪边界、Ruleset 与原生库对齐状态。
