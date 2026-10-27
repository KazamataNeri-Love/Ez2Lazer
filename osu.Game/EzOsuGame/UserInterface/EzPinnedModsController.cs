// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.Rulesets.Mods;

namespace osu.Game.EzOsuGame.UserInterface
{
    /// <summary>
    /// MOD 选择界面置顶栏的数据源：按显示顺序维护 Mod 类型名，并持久化到 <see cref="Ez2Setting.PinnedMods"/>。
    /// </summary>
    public class EzPinnedModsController
    {
        private readonly Ez2ConfigManager config;
        private readonly List<string> keys = new List<string>();

        /// <summary>
        /// 置顶项的类型名，顺序即显示顺序。
        /// </summary>
        public IReadOnlyList<string> Keys => keys;

        public event Action? Changed;

        public EzPinnedModsController(Ez2ConfigManager config)
        {
            this.config = config;

            foreach (string key in config.Get<string>(Ez2Setting.PinnedMods).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                keys.Add(key);
        }

        public bool IsPinned(Mod mod) => keys.Contains(GetKey(mod));

        public void Pin(Mod mod)
        {
            string key = GetKey(mod);

            if (keys.Contains(key))
                return;

            keys.Add(key);
            persist();
        }

        public void Unpin(Mod mod)
        {
            if (keys.Remove(GetKey(mod)))
                persist();
        }

        /// <summary>
        /// 将置顶项按 <paramref name="offset"/> 移动（负数上移，正数下移）。
        /// </summary>
        public void Move(Mod mod, int offset)
        {
            int index = keys.IndexOf(GetKey(mod));
            int target = index + offset;

            if (index < 0 || target < 0 || target >= keys.Count)
                return;

            (keys[index], keys[target]) = (keys[target], keys[index]);
            persist();
        }

        public static string GetKey(Mod mod) => mod.GetType().Name;

        private void persist()
        {
            config.SetValue(Ez2Setting.PinnedMods, string.Join(',', keys));
            Changed?.Invoke();
        }
    }
}
