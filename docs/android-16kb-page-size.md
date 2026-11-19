# Android 16 KB 页对齐

> 面向 Android 16（4 KB / 16 KB 页内核）构建要求。现状：APK 内 29 个 `.so` 全部 ≥ 16 KB，`XA0141` 为 0 条。

## 1. 背景

Android 16 起，系统要求应用内所有共享库按 16 KB 页对齐（ELF `p_align ≥ 16384`），
否则该库在 16 KB 内核设备上无法 `dlopen`，表现为启动即崩。
`dotnet build` 会输出 `warning XA0141` 指明哪个包里的哪个 `.so` 不达标。

## 2. 三个不达标库与来源

| `.so` | 来源包 | 处理 |
| --- | --- | --- |
| `libveldrid-spirv.so` | `ez2lazer.Veldrid 2026.1110.2`（实际进 APK 的那份） | 本地重编 → `2026.1110.3` |
| 同名 | `ppy.Veldrid.SPIRV 1.0.15-g5911265ec3` | 本地重编 → `1.0.15.16` |
| `libfreetype.so` | `McEndu.FreeTypeSharp 4.1.5`（aar 内 `jni/arm64-v8a`） | 本地重编 → `4.1.5.16` |
| `libSkiaSharp.so` | 原经 `Live2DCSharpSDK` 引入 | 已随桌宠移除，不再打包 |

## 3. 本地包

三个包都放在 `D:\Program Files\Games\Ez\_lab\android-16kb\local-feed`，通过用户级 NuGet 源 `ez-local` 提供：

```powershell
dotnet nuget add source 'D:\Program Files\Games\Ez\_lab\android-16kb\local-feed' --name ez-local
```

`osu.Android/osu.Android.csproj` 里以直接 `PackageReference` 覆盖 framework 传递的旧版本。

## 4. 重建方法

工具链：NDK `28.2.13676358`、CMake `3.22.1`（均在 `D:\Environments\android-sdk`）。

### freetype

```powershell
cd 'D:\Program Files\Games\Ez\_lab\android-16kb'
cmake -G Ninja -S freetype-2.13.3 -B build-arm64 `
  -DCMAKE_TOOLCHAIN_FILE=<ndk>/build/cmake/android.toolchain.cmake `
  -DANDROID_ABI=arm64-v8a -DANDROID_PLATFORM=android-24 -DCMAKE_BUILD_TYPE=Release `
  -DBUILD_SHARED_LIBS=ON -DFT_DISABLE_BZIP2=TRUE -DFT_DISABLE_PNG=TRUE `
  -DFT_DISABLE_HARFBUZZ=TRUE -DFT_DISABLE_BROTLI=TRUE `
  -DCMAKE_SHARED_LINKER_FLAGS=-Wl,-z,max-page-size=16384
ninja -C build-arm64
```

替换原 aar 内 `jni/{arm64-v8a,x86_64}/libfreetype.so` 后重打包为 `4.1.5.16`。

### veldrid-spirv

源码 `https://github.com/mellinoe/veldrid-spirv` @ `5911265`，
子模块 SPIRV-Cross + `ext/known_good.json` 指定的 shaderc 依赖：

```powershell
python ext\update_shaderc_sources.py --dir ext\shaderc --file ext\known_good.json
# shaderc 的 CMakeLists 需在 cmake_minimum_required(2.8.12) 后补一行 cmake_policy(SET CMP0057 NEW)
cmake -G Ninja -S . -B build-arm64 -DCMAKE_TOOLCHAIN_FILE=<ndk>/build/cmake/android.toolchain.cmake `
  -DANDROID_ABI=arm64-v8a -DANDROID_PLATFORM=android-24 -DCMAKE_BUILD_TYPE=Release `
  -DCMAKE_SHARED_LINKER_FLAGS=-Wl,-z,max-page-size=16384
ninja -C build-arm64 veldrid-spirv
llvm-strip --strip-unneeded build-arm64/libveldrid-spirv.so   # 171 MB → 10.5 MB
```

替换 `runtimes/android-{arm64,x64}/native/libveldrid-spirv.so` 后分别重打包为
`ppy.Veldrid.SPIRV 1.0.15.16` 与 `ez2lazer.Veldrid 2026.1110.3`。

## 5. 验证

```powershell
# 1) 构建时不再有 XA0141
dotnet build osu.Android\osu.Android.csproj -c Release -m:1 `
  -p:AndroidSdkDirectory=D:\Environments\android-sdk `
  -p:JavaSdkDirectory=D:\Environments\Java\jdk-21.0.13.0.2+1

# 2) 逐库读 ELF 程序头，确认 p_align ≥ 16384
# 3) APK 级对齐
& 'D:\Environments\android-sdk\build-tools\36.0.0\zipalign.exe' -c -P 16 -v 4 <apk>
```

判定线：`XA0141` 0 条 + `zipalign` 输出 `Verification successful`。

## 6. 注意

- 上游一旦升级这三个包，需要重新确认它们的 16 KB 状态，必要时重编本地包并升版本号。
- 本地包只服务本机构建；CI 或换机需要同步 `_lab/android-16kb/local-feed` 或改回官方包版本。
