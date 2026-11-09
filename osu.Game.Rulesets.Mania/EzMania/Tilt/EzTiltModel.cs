// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Game.Rulesets.Mania.EzMania.Tilt
{
    /// <summary>
    /// 斜轨几何的两个独立自由度：横向收窄只由角度决定，纵向距离律强度由角度与补偿共同决定。
    /// 3D 轨道上「轨道看起来多窄」和「note 在屏幕上走多快」是两件事，不能共用一个系数。
    /// </summary>
    public static class EzTiltModel
    {
        public const float MAX_ANGLE = 90f;

        private const float max_horizontal_narrowing = 0.9f;

        /// <summary>
        /// 顶部横向收窄系数（1 = 不收窄）。与屏幕 y 线性插值，等价于轨道的 1/z 收窄。
        /// </summary>
        public static float TopWidthScale(float angle)
            => 1f - max_horizontal_narrowing * Math.Clamp(angle / MAX_ANGLE, 0f, 1f);

        /// <summary>
        /// 纵向距离律强度 k：线性距离 u 经 u / (1 + k·u) 映射为屏幕距离。
        /// 补偿为 0 时 k = 1/W − 1，此时纵向间距为横向收窄的平方（真实透视里平面上的竖直方向正是 1/z²）；
        /// 补偿为 1 时 k = 0，屏幕速度回到匀速，即 Malody 的 com_motion 拉满。
        /// </summary>
        public static float DistanceStrength(float angle, float compensation)
        {
            float width = TopWidthScale(angle);

            if (width <= 0.001f)
                return 1000f;

            return (1f / width - 1f) * (1f - Math.Clamp(compensation, 0f, 1f));
        }
    }
}
