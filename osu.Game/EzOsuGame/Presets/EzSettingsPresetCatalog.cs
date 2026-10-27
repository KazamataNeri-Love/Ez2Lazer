// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Configuration;
using osu.Game.EzOsuGame.Configuration;

namespace osu.Game.EzOsuGame.Presets
{
    /// <summary>
    /// 「设置预设方案」覆盖的设置白名单。三组内容与设置侧边栏一一对应：
    /// <list type="bullet">
    ///   <item>皮肤 —— 单独处理（见 <see cref="EzSettingsPresetBridge"/>），不在此列。</item>
    ///   <item>「Ez 游玩」→「Ez Mania 设置」（<c>EzGameModeSettings</c>）。</item>
    ///   <item>「图像」→「布局」（<c>LayoutSettings</c>）。</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// 刻意排除的项：
    /// <list type="bullet">
    ///   <item>
    ///     <c>Ez2Setting.ScratchAxisL/R</c>（以及整套转盘输入项）：绑定的是具体硬件设备的 GUID，
    ///     跨机器/跨设备无意义，属输入配置而非「方案」。
    ///   </item>
    ///   <item>
    ///     <c>Ez2Setting.EzSkinJsonAutoApplyOnSkinChange</c>：关闭该项时 <c>SkinSection</c> 会调用
    ///     <c>Ez2ConfigManager.Load()</c> 把 ini 值盖回内存，会把预设刚写进去的内存值整批冲掉。
    ///   </item>
    ///   <item>
    ///     <c>Ez2Setting.EnableScriptedSkins</c>：启动期一次性读取，且 <c>SkinSection</c> 用的是
    ///     非响应式 bindable，写它不生效。
    ///   </item>
    ///   <item>
    ///     <c>FrameworkSetting</c> 的窗口模式/分辨率/显示器/失焦最小化：属 <c>framework.ini</c>，
    ///     分辨率这类值与机器绑定，不应随预设跨机器复制。
    ///   </item>
    ///   <item>
    ///     Mania 下落速度（<c>ManiaRulesetSetting.Scroll*</c>）：存 Realm 的 ruleset 配置，不在 ini 体系内。
    ///   </item>
    /// </list>
    /// </remarks>
    public static class EzSettingsPresetCatalog
    {
        /// <summary>
        /// 「Ez 游玩」→「Ez Mania 设置」。
        /// </summary>
        public static IReadOnlyList<Ez2Setting> ManiaSettings { get; } = new[]
        {
            // 判定门槛
            Ez2Setting.AccuracyCutoffS,
            Ez2Setting.AccuracyCutoffA,

            // 判定与血量模式
            Ez2Setting.ManiaJudgeMode,
            Ez2Setting.StdJudgeMode,
            Ez2Setting.ManiaHealthMode,
            Ez2Setting.BmsPoorHitResultEnable,
            Ez2Setting.JudgePrecedence,

            // 判定附加分
            Ez2Setting.ScoreBonusTendency,
            Ez2Setting.ScoreBonusCrossMs,

            // 偏移
            Ez2Setting.OffsetPlusMania,
            Ez2Setting.OffsetPlusNonMania,

            // 键型行为
            Ez2Setting.ManiaSkipEmptyEdgeColumns,
        };

        /// <summary>
        /// 「图像」→「布局」中的 Ez 侧项（<c>EzSkinSettings.ini</c>）。
        /// </summary>
        public static IReadOnlyList<Ez2Setting> LayoutEzSettings { get; } = new[]
        {
            Ez2Setting.BackgroundScale,
            Ez2Setting.BackgroundPosX,
            Ez2Setting.BackgroundPosY,
            Ez2Setting.ScalingGameMode,
        };

        /// <summary>
        /// 「图像」→「布局」中的官方项（<c>Ez2game.ini</c>）。
        /// </summary>
        /// <remarks>
        /// 这些项 Ez 侧没有对应实现（界面缩放、屏幕缩放与横纵缩放、背景 dim），
        /// 若不纳入会导致「图像 → 布局」预设不完整，故按约定允许预设写入官方 <see cref="OsuSetting"/>。
        /// </remarks>
        public static IReadOnlyList<OsuSetting> LayoutOsuSettings { get; } = new[]
        {
            OsuSetting.UIScale,
            OsuSetting.Scaling,
            OsuSetting.ScalingSizeX,
            OsuSetting.ScalingSizeY,
            OsuSetting.ScalingPositionX,
            OsuSetting.ScalingPositionY,
            OsuSetting.ScalingBackgroundDim,
        };

        public static IEnumerable<Ez2Setting> AllEzSettings => ManiaSettings.Concat(LayoutEzSettings);

        public static IEnumerable<OsuSetting> AllOsuSettings => LayoutOsuSettings;
    }
}
