// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.EzOsuGame.Configuration;

namespace osu.Game.EzOsuGame.Presets
{
    /// <summary>
    /// 「设置预设方案」的磁盘存储：游戏数据目录下的 <c>EzSettings/&lt;guid&gt;.json</c>，一个预设一个文件。
    /// </summary>
    /// <remarks>
    /// 文件名用 <see cref="EzSettingsPresetDocument.Id"/> 而非预设名，所以重命名只是改写文件内容，
    /// 不会产生孤儿文件，也不会和其他预设撞名。
    /// </remarks>
    public class EzSettingsPresetStore
    {
        /// <summary>
        /// 预设目录名，位于游戏数据目录（存放 <c>Ez2game.ini</c> / <c>EzSkinSettings.ini</c> 的同级）。
        /// </summary>
        public const string DIRECTORY_NAME = "EzSettings";

        private const string extension = ".json";

        private readonly Storage storage;

        /// <summary>
        /// 全部预设，按名称排序。UI 直接绑定此列表。
        /// </summary>
        public readonly BindableList<EzSettingsPresetDocument> Presets = new BindableList<EzSettingsPresetDocument>();

        public EzSettingsPresetStore(Storage storage)
        {
            this.storage = storage.GetStorageForDirectory(DIRECTORY_NAME);
            load();
        }

        public EzSettingsPresetDocument Create(string name)
        {
            var document = new EzSettingsPresetDocument
            {
                Name = name,
            };

            save(document);
            return document;
        }

        /// <summary>
        /// 写回磁盘。名称变更后同步列表顺序。
        /// </summary>
        public void Save(EzSettingsPresetDocument document)
        {
            if (string.IsNullOrWhiteSpace(document.Name))
                throw new ArgumentException("预设名不能为空。", nameof(document));

            save(document);
            refreshOrder();
        }

        public void Delete(EzSettingsPresetDocument document)
        {
            string path = pathFor(document);

            try
            {
                if (storage.Exists(path))
                    storage.Delete(path);
            }
            catch (Exception e)
            {
                Logger.Log($"[Ez] 设置预设 {document.Name} 删除失败：{e.Message}", Ez2ConfigManager.LOGGER_NAME, LogLevel.Important);
            }

            Presets.Remove(document);
        }

        /// <summary>
        /// 生成不重名的默认名，用 <paramref name="format"/> 套序号（从 1 开始）。
        /// </summary>
        public string NextDefaultName(Func<int, string> format)
        {
            int index = 1;

            while (Presets.Any(p => string.Equals(p.Name, format(index), StringComparison.OrdinalIgnoreCase)))
                index++;

            return format(index);
        }

        private void save(EzSettingsPresetDocument document)
        {
            if (!Presets.Contains(document))
                Presets.Add(document);

            using (var stream = storage.CreateFileSafely(pathFor(document)))
            using (var writer = new StreamWriter(stream))
                writer.Write(document.Serialize());
        }

        private void load()
        {
            foreach (string file in storage.GetFiles(string.Empty, "*" + extension))
            {
                try
                {
                    using (var stream = storage.GetStream(file))
                    {
                        if (stream == null)
                            continue;

                        using (var reader = new StreamReader(stream))
                        {
                            var document = EzSettingsPresetDocument.Parse(reader.ReadToEnd());

                            if (string.IsNullOrWhiteSpace(document.Name))
                                continue;

                            Presets.Add(document);
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Log($"[Ez] 设置预设 {file} 读取失败，已跳过：{e.Message}", Ez2ConfigManager.LOGGER_NAME, LogLevel.Important);
                }
            }

            refreshOrder();
        }

        private void refreshOrder()
        {
            var ordered = Presets.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList();

            for (int i = 0; i < ordered.Count; i++)
            {
                int current = Presets.IndexOf(ordered[i]);

                if (current != i)
                    Presets.Move(current, i);
            }
        }

        private string pathFor(EzSettingsPresetDocument document) => $"{document.Id}{extension}";
    }
}
