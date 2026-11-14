// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Osu.UI;
using osuTK;

namespace osu.Game.Rulesets.Osu.Mods
{
    /// <summary>
    /// Polytone 的棱锥几何：世界坐标 X/Y 不变，物件只沿 +Z 推进，
    /// 屏幕表现等价于截面比例 <see cref="RatioAt"/> 下的等比缩放与径向收敛。
    /// 物件与区域边界必须共用同一份换算，否则两者会错位。
    /// </summary>
    internal readonly struct PolytoneGeometry
    {
        /// <summary>
        /// 远端截面比例下限。再小物件出生时会挤成一个点，实际不可读。
        /// </summary>
        public const float MINIMUM_SPAWN_RATIO = 0.08f;

        /// <summary>
        /// 消失点（相机正对的 playfield 位置）。X/Y 不变时，所有轨迹都沿它与物件的连线。
        /// </summary>
        public readonly Vector2 VanishingPoint;

        /// <summary>
        /// 截面比例收敛到 0 的深度，由视角决定。
        /// </summary>
        public readonly float ApexDepth;

        /// <summary>
        /// 出生层（由出生深度决定）的截面比例。
        /// </summary>
        public readonly float SpawnRatio;

        public PolytoneGeometry(float viewAngle, float spawnDepth)
        {
            VanishingPoint = OsuPlayfield.BASE_SIZE * 0.5f;
            ApexDepth = OsuPlayfield.BASE_SIZE.X * 0.5f / MathF.Tan(MathHelper.DegreesToRadians(viewAngle));
            SpawnRatio = Math.Clamp(1 - spawnDepth / ApexDepth, MINIMUM_SPAWN_RATIO, 0.99f);
        }

        /// <summary>
        /// 截面比例。<paramref name="progress"/> 0 为出生、1 为到达击打层。
        /// </summary>
        /// <param name="perspectiveBlend">0 = 线性逼近，1 = 针孔投影（后段爆冲），中间为混合。</param>
        public float RatioAt(double progress, float perspectiveBlend)
        {
            float u = (float)Math.Clamp(progress, 0, 1);
            float linear = SpawnRatio + (1 - SpawnRatio) * u;

            if (perspectiveBlend <= 0)
                return linear;

            float gamma = (1 - SpawnRatio) / SpawnRatio;
            float projected = 1 / (1 + gamma * (1 - u));

            return linear + (projected - linear) * perspectiveBlend;
        }

        public Vector2 Project(Vector2 position, float ratio) => VanishingPoint + (position - VanishingPoint) * ratio;
    }
}
