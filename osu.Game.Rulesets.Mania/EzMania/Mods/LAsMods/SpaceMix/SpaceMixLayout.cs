// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;

namespace osu.Game.Rulesets.Mania.EzMania.Mods.LAsMods
{
    /// <summary>
    /// SpaceMix 面板区域描述符，记录一个面板的列范围、键数等静态属性。
    /// </summary>
    public readonly struct SpaceMixZoneDescriptor
    {
        public SpaceMixZone Zone { get; init; }
        public int StartColumn { get; init; }
        public int EndColumn { get; init; }
        public int KeyCount => EndColumn - StartColumn + 1;
    }

    /// <summary>
    /// SpaceMix 14K 物理布局常量。
    /// </summary>
    public static class SpaceMixLayout
    {
        /// <summary>左手 5 键：列 0~4，白蓝白蓝白交叉</summary>
        public static readonly SpaceMixZoneDescriptor DP_Left = new()
        {
            Zone = SpaceMixZone.DP_Left,
            StartColumn = 0,
            EndColumn = 4
        };

        /// <summary>中间 4 红键：列 5~8，同色水平</summary>
        public static readonly SpaceMixZoneDescriptor Effect = new()
        {
            Zone = SpaceMixZone.Effect,
            StartColumn = 5,
            EndColumn = 8
        };

        /// <summary>右手 5 键：列 9~13，白蓝白蓝白交叉</summary>
        public static readonly SpaceMixZoneDescriptor DP_Right = new()
        {
            Zone = SpaceMixZone.DP_Right,
            StartColumn = 9,
            EndColumn = 13
        };

        /// <summary>全部三块面板</summary>
        public static readonly IReadOnlyList<SpaceMixZoneDescriptor> AllZones = new[]
        {
            DP_Left, Effect, DP_Right
        };

        /// <summary>仅 DP 面板（左右手）</summary>
        public static readonly IReadOnlyList<SpaceMixZoneDescriptor> DPZones = new[]
        {
            DP_Left, DP_Right
        };

        /// <summary>总列数</summary>
        public const int TOTAL_COLUMNS = 14;
    }
}
