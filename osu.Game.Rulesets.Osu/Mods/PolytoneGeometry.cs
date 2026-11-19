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
    /// <remarks>
    /// 相机到击打层的距离取视角锥的顶点深度，物件沿光轴匀速推进，
    /// 因此 <c>1/ratio</c> 随进度线性 —— 与针孔投影逐点一致。
    /// </remarks>
    internal readonly struct PolytoneGeometry
    {
        /// <summary>
        /// 消失点（相机正对的 playfield 位置）。X/Y 不变时，所有轨迹都沿它与物件的连线。
        /// </summary>
        public readonly Vector2 VanishingPoint;

        /// <summary>
        /// 相机到击打层的距离，由视角决定。
        /// </summary>
        public readonly float ApexDepth;

        /// <summary>
        /// 出生层到击打层的距离与 <see cref="ApexDepth"/> 之比，即投影公式的深度系数。
        /// </summary>
        public readonly float DepthScale;

        public PolytoneGeometry(float viewAngle, float spawnDepth)
        {
            VanishingPoint = OsuPlayfield.BASE_SIZE * 0.5f;
            ApexDepth = OsuPlayfield.BASE_SIZE.X * 0.5f / MathF.Tan(MathHelper.DegreesToRadians(viewAngle));
            DepthScale = spawnDepth / ApexDepth;
        }

        /// <summary>
        /// 出生层（由出生深度决定）的截面比例。
        /// </summary>
        public float SpawnRatio => 1f / (1f + DepthScale);

        /// <summary>
        /// 截面比例。<paramref name="progress"/> 0 为出生、1 为到达击打层。
        /// </summary>
        public float RatioAt(double progress)
        {
            float u = (float)Math.Clamp(progress, 0, 1);
            return 1f / (1f + DepthScale * (1f - u));
        }

        /// <summary>
        /// 给定截面比例对应的深度（相机到该截面的距离）。
        /// </summary>
        public float DepthAt(float ratio) => ratio <= 0 ? float.PositiveInfinity : ApexDepth * (1f / ratio - 1f);

        /// <summary>
        /// 给定深度的截面比例。
        /// </summary>
        public float RatioAtDepth(float depth) => ApexDepth / (ApexDepth + depth);

        public Vector2 Project(Vector2 position, float ratio) => VanishingPoint + (position - VanishingPoint) * ratio;
    }
}
