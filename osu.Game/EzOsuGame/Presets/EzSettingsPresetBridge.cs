// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using osu.Framework.Bindables;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.Skinning;

namespace osu.Game.EzOsuGame.Presets
{
    /// <summary>
    /// 在 <see cref="EzSettingsPresetDocument"/> 与内存中的配置之间搬运设置。
    /// </summary>
    /// <remarks>
    /// 写入刻意走 <see cref="IParseable"/> 字符串通道（与 <c>IniConfigManager.PerformLoad</c> 同一条路径），
    /// 因此不需要为每个设置项维护「枚举 → 值类型」的反射映射表。
    /// <para>
    /// 只作用于内存中的 bindable；落盘由 <c>ConfigManager.QueueBackgroundSave</c> 的 100ms 去抖完成，
    /// 因此一次应用几十项只会写一次 ini。
    /// </para>
    /// </remarks>
    public static class EzSettingsPresetBridge
    {
        public const string EZ_PREFIX = "Ez2.";
        public const string OSU_PREFIX = "Osu.";

        /// <summary>
        /// 捕获当前设置。皮肤以 ID + 名字双写。
        /// </summary>
        public static EzSettingsPresetDocument Capture(Ez2ConfigManager ez, OsuConfigManager osu, SkinManager skins, string name, Guid? id = null)
        {
            var document = new EzSettingsPresetDocument
            {
                Name = name,
            };

            if (id.HasValue)
                document.Id = id.Value;

            foreach (var setting in EzSettingsPresetCatalog.AllEzSettings)
            {
                if (tryCapture(ez.GetRawBindable(setting), out string value))
                    document.Settings[EZ_PREFIX + setting] = value;
            }

            foreach (var setting in EzSettingsPresetCatalog.AllOsuSettings)
            {
                if (tryCapture(osu.GetRawBindable(setting), out string value))
                    document.Settings[OSU_PREFIX + setting] = value;
            }

            var skin = skins.CurrentSkinInfo.Value;
            document.SkinId = skin.ID.ToString();
            document.SkinName = skin.Value.Name;

            return document;
        }

        /// <summary>
        /// 应用预设。逐项独立容错：单项失败不会中断其余项。
        /// </summary>
        public static EzSettingsPresetApplyReport Apply(EzSettingsPresetDocument document, Ez2ConfigManager ez, OsuConfigManager osu, SkinManager skins)
        {
            var report = new EzSettingsPresetApplyReport();

            foreach ((string key, string value) in document.Settings)
            {
                if (key.StartsWith(EZ_PREFIX, StringComparison.Ordinal))
                {
                    if (Enum.TryParse(key.Substring(EZ_PREFIX.Length), out Ez2Setting setting))
                        tryApply(key, ez.GetRawBindable(setting), value, report);
                    else
                        report.Skipped.Add(key);
                }
                else if (key.StartsWith(OSU_PREFIX, StringComparison.Ordinal))
                {
                    if (Enum.TryParse(key.Substring(OSU_PREFIX.Length), out OsuSetting setting))
                        tryApply(key, osu.GetRawBindable(setting), value, report);
                    else
                        report.Skipped.Add(key);
                }
                else
                {
                    report.Skipped.Add(key);
                }
            }

            // 皮肤放最后：切换皮肤会触发 EzSkin.json 自动应用等后续动作，
            // 但两者覆盖的设置项集合不相交（预设只碰 Mania 与布局，EzSkin 只碰皮肤外观），故先后顺序不影响结果。
            applySkin(document, skins, report);

            return report;
        }

        private static void tryApply(string key, IBindable? bindable, string value, EzSettingsPresetApplyReport report)
        {
            if (bindable == null)
            {
                // 枚举项在本版本已删除/改名，或是未被 InitialiseDefaults 注册。
                report.Skipped.Add(key);
                return;
            }

            if (!(bindable is IParseable parseable))
            {
                report.Failed.Add($"{key}: {bindable.GetType().Name} 不支持 IParseable");
                return;
            }

            try
            {
                // Bindable<T>.Parse 支持字符串 → 枚举 / 数字 / 布尔 / Size / Colour4，
                // 与 IniConfigManager 读盘、EzSkinJsonBridge 应用走的是同一条通道。
                parseable.Parse(value, CultureInfo.InvariantCulture);
                report.Applied++;
            }
            catch (Exception e)
            {
                // 典型情形：极速模式（EzTurboMode）把该项 Disabled 了，此时 Value setter 会抛。
                report.Failed.Add($"{key}: {e.Message}");
            }
        }

        private static void applySkin(EzSettingsPresetDocument document, SkinManager skins, EzSettingsPresetApplyReport report)
        {
            if (string.IsNullOrEmpty(document.SkinId) && string.IsNullOrEmpty(document.SkinName))
                return;

            IList<Live<SkinInfo>> available;

            try
            {
                available = skins.GetAllUsableSkins();
            }
            catch (Exception e)
            {
                report.Failed.Add($"skin: {e.Message}");
                return;
            }

            Live<SkinInfo>? match = null;

            // 先按 ID 精确匹配（同名皮肤也能各归各位）。
            if (!string.IsNullOrEmpty(document.SkinId) && Guid.TryParse(document.SkinId, out Guid id))
                match = available.FirstOrDefault(s => s.ID == id);

            // ID 失效（皮肤已删除、或预设来自另一台机器）时按名字回退。
            if (match is null && !string.IsNullOrEmpty(document.SkinName))
                match = available.FirstOrDefault(s => string.Equals(s.Value.Name, document.SkinName, StringComparison.OrdinalIgnoreCase));

            if (match is null)
            {
                report.SkinMissing = true;
                return;
            }

            try
            {
                skins.CurrentSkinInfo.Value = match;
                report.SkinApplied = true;
            }
            catch (Exception e)
            {
                report.Failed.Add($"skin: {e.Message}");
            }
        }

        private static bool tryCapture(IBindable? bindable, out string value)
        {
            value = string.Empty;

            if (bindable == null)
                return false;

            value = getValueString(bindable);
            return true;
        }

        private static string getValueString(IBindable bindable)
        {
            if (bindable is IFormattable formattable)
                return formattable.ToString(null, CultureInfo.InvariantCulture);

            return bindable.ToString() ?? string.Empty;
        }
    }

    /// <summary>
    /// <see cref="EzSettingsPresetBridge.Apply"/> 的结果摘要，供 UI 决定要不要弹提示。
    /// </summary>
    public sealed class EzSettingsPresetApplyReport
    {
        public int Applied;

        public bool SkinApplied;

        /// <summary>
        /// 预设里记录了皮肤，但当前可用皮肤列表中找不到（已删除或来自另一台机器）。
        /// </summary>
        public bool SkinMissing;

        /// <summary>
        /// 被跳过的键：枚举项已删除/改名，或该项未被注册。
        /// </summary>
        public List<string> Skipped { get; } = new List<string>();

        /// <summary>
        /// 写入失败的键及原因。
        /// </summary>
        public List<string> Failed { get; } = new List<string>();

        public bool HasProblems => Skipped.Count > 0 || Failed.Count > 0 || SkinMissing;
    }
}
