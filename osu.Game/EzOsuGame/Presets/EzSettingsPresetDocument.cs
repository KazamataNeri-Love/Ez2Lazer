// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace osu.Game.EzOsuGame.Presets
{
    /// <summary>
    /// 一套「设置预设方案」：皮肤 + Ez Mania 设置 + 图像布局。
    /// </summary>
    /// <remarks>
    /// 存为数据目录 <c>settings-presets/&lt;guid&gt;.json</c>，一个预设一个文件：
    /// 手改 / 分享都无需动数据库，也不涉及 Realm schema 迁移（Edge 常年合并上游，迁移代价高）。
    /// <para>
    /// 键一律带前缀（<c>"Ez2.ManiaHitMode"</c> / <c>"Osu.UIScale"</c>），沿用 <c>EzTurboMode</c> 的
    /// <c>$"{typeof(TLookup).Name}.{lookup}"</c> 惯例，保证 <see cref="Configuration.Ez2Setting"/> 与
    /// <c>OsuSetting</c> 的同名项不会串味。
    /// </para>
    /// </remarks>
    public sealed class EzSettingsPresetDocument
    {
        public const int CURRENT_SCHEMA_VERSION = 1;

        [JsonPropertyName("schemaVersion")]
        public int SchemaVersion { get; set; } = CURRENT_SCHEMA_VERSION;

        /// <summary>
        /// 稳定标识，同时用作文件名。重命名预设不影响它。
        /// </summary>
        [JsonPropertyName("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 显示名，可重命名。初始形如「设置预设1」。
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 皮肤 ID（<c>SkinInfo.ID</c> 的 GUID 字符串形式）。
        /// </summary>
        [JsonPropertyName("skinId")]
        public string? SkinId { get; set; }

        /// <summary>
        /// 皮肤名。ID 失效（皮肤被删、或预设来自另一台机器）时按名回退匹配。
        /// </summary>
        [JsonPropertyName("skinName")]
        public string? SkinName { get; set; }

        /// <summary>
        /// 设置项快照，键带前缀（见类型备注），值是 <c>InvariantCulture</c> 下的字符串形式。
        /// </summary>
        [JsonPropertyName("settings")]
        public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();

        private static readonly JsonSerializerOptions serializer_options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public static EzSettingsPresetDocument Parse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new EzSettingsPresetDocument();

            return JsonSerializer.Deserialize<EzSettingsPresetDocument>(json, serializer_options) ?? new EzSettingsPresetDocument();
        }

        public string Serialize() => JsonSerializer.Serialize(this, serializer_options);

        public EzSettingsPresetDocument Clone() => new EzSettingsPresetDocument
        {
            SchemaVersion = SchemaVersion,
            Id = Id,
            Name = Name,
            SkinId = SkinId,
            SkinName = SkinName,
            Settings = new Dictionary<string, string>(Settings),
        };

        // FormDropdown 直接以下拉项文字显示 ToString() 的结果。
        public override string ToString() => Name;
    }
}
