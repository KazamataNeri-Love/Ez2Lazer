// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Configuration.Tracking;

namespace osu.Game.Rulesets.Configuration
{
    public interface IRulesetConfigManager : ITrackableConfigManager, IDisposable
    {
        /// <summary>
        /// 按键取原始 bindable（不返回绑定副本）。键为规则集设置枚举名，与 Realm 中存放该项所用的键同构。
        /// </summary>
        /// <returns>未注册该键时返回 null。</returns>
        IBindable? GetRawBindable(string key);
    }
}
