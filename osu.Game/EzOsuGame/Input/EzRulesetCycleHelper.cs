// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Rulesets;

namespace osu.Game.EzOsuGame.Input
{
    /// <summary>
    /// [Ez] 规则集循环切换的纯逻辑部分（便于单测）。
    /// </summary>
    /// <remarks>
    /// 与工具栏的 Ctrl+数字直选（<c>ToolbarRulesetSelector</c>）共用同一份
    /// <see cref="RulesetStore.AvailableRulesets"/> 顺序与同一个 <c>Ruleset</c> bindable，
    /// 因此切换音效、音乐 duck 等行为自动一致。
    /// </remarks>
    public static class EzRulesetCycleHelper
    {
        /// <summary>
        /// 求 <paramref name="available"/> 中 <paramref name="current"/> 的下一个规则集（到末尾后回到开头）。
        /// 当前不在列表里（含 null / 尚未选定）时返回第一个；列表为空时返回 null。
        /// </summary>
        public static RulesetInfo? NextRuleset(IReadOnlyList<RulesetInfo> available, RulesetInfo? current)
        {
            if (available.Count == 0)
                return null;

            int index = -1;

            for (int i = 0; i < available.Count; i++)
            {
                if (available[i].ShortName == current?.ShortName)
                {
                    index = i;
                    break;
                }
            }

            return available[(index + 1) % available.Count];
        }
    }
}
