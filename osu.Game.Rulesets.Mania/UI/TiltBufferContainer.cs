// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Rulesets.Mania.EzMania.Tilt;
using osuTK;

namespace osu.Game.Rulesets.Mania.UI
{
    /// <summary>
    /// Projects its content into a receding track. The projection is anchored at the bottom edge, so the
    /// judgement line keeps its position and only the area above it is compressed.
    /// </summary>
    public partial class TiltBufferContainer : BufferedContainer
    {
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
        /// Applies the track projection.
        /// </summary>
        /// <param name="angle">Track angle in degrees; 0 leaves the content unwarped.</param>
        /// <remarks>
        /// 只做横向收窄：纵向间距与速度由 <see cref="EzTiltScrollAlgorithm"/> 在滚动层决定。
        /// 两层各管一个维度，同一维度不会被压两次。
        /// </remarks>
        public void ApplyTilt(float angle)
        {
            VerticalPerspective = 0f;
            PerspectiveScale = new Vector2(1f, EzTiltModel.TopWidthScale(angle));
            PerspectiveVerticalOffset = 0f;
        }
    }
}
