// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Logging;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Configuration;

namespace osu.Game.EzOsuGame.Presets
{
    /// <summary>
    /// 设置预设访问规则集设置的入口。规则集设置的类型定义在规则集程序集，这里只经由
    /// <see cref="IRulesetConfigCache"/> 与 <see cref="IRulesetConfigManager"/> 的通用通道操作。
    /// </summary>
    internal static class EzSettingsPresetRulesets
    {
        public const string MANIA_SHORT_NAME = "mania";

        /// <summary>
        /// 取 Mania 的规则集设置管理器。规则集不可用或缓存尚未加载时返回 null。
        /// </summary>
        public static IRulesetConfigManager? GetManiaConfig(RulesetStore rulesets, IRulesetConfigCache cache)
        {
            try
            {
                var info = rulesets.GetRuleset(MANIA_SHORT_NAME);

                return info == null ? null : cache.GetConfigFor(info.CreateInstance());
            }
            catch (Exception e)
            {
                Logger.Log($"[Ez] 取 Mania 规则集设置失败：{e.Message}", Ez2ConfigManager.LOGGER_NAME, LogLevel.Debug);
                return null;
            }
        }
    }
}
