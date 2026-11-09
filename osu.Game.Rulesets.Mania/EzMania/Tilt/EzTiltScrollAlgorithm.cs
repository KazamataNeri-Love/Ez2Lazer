// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.UI.Scrolling.Algorithms;

namespace osu.Game.Rulesets.Mania.EzMania.Tilt
{
    /// <summary>
    /// 在既有滚动算法上叠一层射影距离律：u / (1 + k·u)。
    /// 位置、LN 长度、对象进入时间都走同一条曲线，因此间距与速度自动一致——这是 Malody 的
    /// com_motion 真正改的东西，横向收窄不参与。
    /// </summary>
    /// <remarks>
    /// k = 0 时与原算法完全等价。u 归一化到容器长度，所以 k 与分辨率无关。
    /// </remarks>
    public sealed class EzTiltScrollAlgorithm : IScrollAlgorithm
    {
        private readonly IScrollAlgorithm inner;
        private readonly float strength;

        public EzTiltScrollAlgorithm(IScrollAlgorithm inner, float strength)
        {
            this.inner = inner;
            this.strength = Math.Clamp(strength, 0f, 1000f);
        }

        /// <remarks>
        /// 取两者较早的一个：压缩后对象在远端停留更久，宁可提前出现，也不要临到远端才弹进来。
        /// </remarks>
        public double GetDisplayStartTime(double originTime, float offset, double timeRange, float scrollLength)
            => Math.Min(TimeAt(-offset, originTime, timeRange, scrollLength) - timeRange,
                        inner.GetDisplayStartTime(originTime, offset, timeRange, scrollLength));

        public float GetLength(double startTime, double endTime, double timeRange, float scrollLength)
            => -PositionAt(startTime, endTime, timeRange, scrollLength);

        public float PositionAt(double time, double currentTime, double timeRange, float scrollLength, double? originTime = null)
            => project(inner.PositionAt(time, currentTime, timeRange, scrollLength, originTime), scrollLength);

        public double TimeAt(float position, double currentTime, double timeRange, float scrollLength)
            => inner.TimeAt(unproject(position, scrollLength), currentTime, timeRange, scrollLength);

        public void Reset() => inner.Reset();

        private float project(float position, float scrollLength)
        {
            if (strength <= 0f || scrollLength <= 0f)
                return position;

            float u = position / scrollLength;
            return scrollLength * u * (1f + strength) / Math.Max(1e-3f, 1f + strength * u);
        }

        private float unproject(float position, float scrollLength)
        {
            if (strength <= 0f || scrollLength <= 0f)
                return position;

            float v = position / scrollLength;
            return scrollLength * v / Math.Max(1e-3f, 1f + strength - strength * v);
        }
    }
}
