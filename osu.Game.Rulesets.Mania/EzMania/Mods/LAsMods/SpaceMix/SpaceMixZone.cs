// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Rulesets.Mania.EzMania.Mods.LAsMods
{
    /// <summary>
    /// SpaceMix 三块键盘面板区域枚举。
    /// </summary>
    public enum SpaceMixZone
    {
        /// <summary>左手 5 键，列 0~4，白蓝白蓝白交叉排列</summary>
        DP_Left = 0,

        /// <summary>中间 4 红键，列 5~8，同色水平排列</summary>
        Effect = 1,

        /// <summary>右手 5 键，列 9~13，白蓝白蓝白交叉排列</summary>
        DP_Right = 2
    }
}
