// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Configuration;
using osu.Game.EzOsuGame.Configuration;

namespace osu.Game.EzOsuGame.Presets
{
    /// <summary>
    /// 「设置预设方案」覆盖的设置白名单。分五组：
    /// <list type="bullet">
    ///   <item>皮肤 —— 单独处理（见 <see cref="EzSettingsPresetBridge"/>），不在此列。</item>
    ///   <item>判定与评级线（<c>EzGameModeSettings</c> / <c>EzStdSettings</c>）。</item>
    ///   <item>背景与布局缩放（对应「图像」→「布局」）。</item>
    ///   <item>Mania 滚动速度（规则集设置，存 Realm，按键名读取）。</item>
    ///   <item>本地账户名（只取名字，接管开关不随预设）。</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// 刻意排除的项：
    /// <list type="bullet">
    ///   <item>
    ///     <c>Ez2Setting.ScratchAxisL/R</c> 等转盘输入项：绑定具体硬件设备的 GUID，跨机器无意义。
    ///   </item>
    ///   <item>
    ///     <c>Ez2Setting.EzSkinJsonAutoApplyOnSkinChange</c>：关闭该项时 <c>SkinSection</c> 会调用
    ///     <c>Ez2ConfigManager.Load()</c> 把 ini 值盖回内存，会冲掉预设刚写进去的内存值。
    ///   </item>
    ///   <item>
    ///     <c>Ez2Setting.EnableScriptedSkins</c>：启动期一次性读取，写它不生效。
    ///   </item>
    ///   <item>
    ///     <c>FrameworkSetting</c> 的窗口模式/分辨率/显示器/失焦最小化：属 <c>framework.ini</c>，
    ///     与机器绑定。
    ///   </item>
    ///   <item>
    ///     判定细节与偏移（<c>BmsPoorHitResultEnable</c>、<c>JudgePrecedence</c>、
    ///     <c>ScoreBonus*</c>、<c>OffsetPlus*</c>、<c>ManiaSkipEmptyEdgeColumns</c>）：不在预设口径内。
    ///   </item>
    /// </list>
    /// </remarks>
    public static class EzSettingsPresetCatalog
    {
        /// <summary>
        /// 判定与血量模式、评级线。
        /// </summary>
        public static IReadOnlyList<Ez2Setting> ManiaSettings { get; } = new[]
        {
            Ez2Setting.AccuracyCutoffS,
            Ez2Setting.AccuracyCutoffA,
            Ez2Setting.ManiaJudgeMode,
            Ez2Setting.StdJudgeMode,
            Ez2Setting.ManiaHealthMode,
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

        /// <summary>
        /// Mania 规则集设置中参与预设的键。值是设置枚举名，与 Realm 中存放该项所用的键一致。
        /// </summary>
        /// <remarks>
        /// 用字符串而不是枚举：规则集设置定义在规则集程序集，本程序集不引用它；
        /// 读取走 <c>IRulesetConfigManager.GetRawBindable</c>，与另外两组共用同一条字符串通道。
        /// 只收滚动速度三项：滚动样式与方向属显示样式，不在预设口径内。
        /// </remarks>
        public static IReadOnlyList<string> ManiaScrollSettings { get; } = new[]
        {
            "ScrollSpeed",
            "ScrollBaseSpeed",
            "ScrollTimePerSpeed",
        };

        /// <summary>
        /// 本地账户名。只收名字本身。
        /// </summary>
        /// <remarks>
        /// 刻意不收 <c>Ez2Setting.LocalAccountNameTakesOverScore</c>：接管开关决定成绩是否上传在线
        /// （<c>SubmittingPlayer</c> 在接管生效时跳过提交），属账号策略而不是方案内容。
        /// 名字被写入后是否生效仍由当前接管开关决定。
        /// </remarks>
        public static IReadOnlyList<Ez2Setting> IdentitySettings { get; } = new[]
        {
            Ez2Setting.LocalAccountName,
        };

        public static IEnumerable<Ez2Setting> AllEzSettings => ManiaSettings.Concat(LayoutEzSettings).Concat(IdentitySettings);

        public static IEnumerable<OsuSetting> AllOsuSettings => LayoutOsuSettings;
    }
}
