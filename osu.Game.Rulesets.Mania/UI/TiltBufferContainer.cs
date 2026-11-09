// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osuTK;

namespace osu.Game.Rulesets.Mania.UI
{
    /// <summary>
    /// Projects its content into a receding track. The projection is anchored at the bottom edge, so the
    /// judgement line keeps its position and only the area above it is compressed.
    /// </summary>
    public partial class TiltBufferContainer : BufferedContainer
    {
        private const float max_angle = 90;

        /// <summary>
        /// 缓冲内容是预乘 alpha 的（子项混入透明缓冲）。默认的 SrcAlpha 合成会再乘一次 alpha，
        /// 导致叠加光效被压暗并呈现为 alpha 混色；预乘合成才能让「只加亮、不覆盖」的内容正确叠加。
        /// </summary>
        private static readonly BlendingParameters premultiplied = new BlendingParameters
        {
            Source = BlendingType.One,
            Destination = BlendingType.OneMinusSrcAlpha,
            SourceAlpha = BlendingType.One,
            DestinationAlpha = BlendingType.OneMinusSrcAlpha,
            RGBEquation = BlendingEquation.Add,
            AlphaEquation = BlendingEquation.Add,
        };

        public TiltBufferContainer()
            : base(pixelSnapping: true)
        {
            RelativeSizeAxes = Axes.Both;
            DrawOriginal = false;
            EffectBlending = premultiplied;
        }

        /// <summary>
        /// Applies the projection.
        /// </summary>
        /// <param name="angle">Track angle in degrees; 0 disables the projection.</param>
        /// <param name="compensation">0 = full distance compression, 1 = keep the far end spacing.</param>
        public void ApplyTilt(float angle, float compensation)
        {
            float t = Math.Clamp(angle / max_angle, 0f, 1f);

            if (t <= 0)
            {
                VerticalPerspective = 0;
                return;
            }

            // 顶部宽度收窄（形状）与顶部高度压缩（间距）分开：补偿只削弱间距压缩，形状收窄保持。
            VerticalPerspective = t;
            PerspectiveScale = new Vector2(
                1f - t * 0.9f * (1f - Math.Clamp(compensation, 0f, 1f) * 0.7f),
                1f - t * 0.8f);
            PerspectiveVerticalOffset = 0f;
        }
    }
}
