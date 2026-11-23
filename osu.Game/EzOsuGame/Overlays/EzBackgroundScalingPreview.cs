// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.EzOsuGame.Background;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.EzOsuGame.Overlays
{
    /// <summary>
    /// 设置页调整背景缩放与位置时叠加显示的半透明区域预览。
    /// </summary>
    public partial class EzBackgroundScalingPreview : Container
    {
        private const float corner_radius = 10;

        public EzBackgroundScalingPreview(float scale, float posX, float posY)
        {
            RelativeSizeAxes = Axes.Both;
            RelativePositionAxes = Axes.Both;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            Child = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = corner_radius,
                Child = new Box
                {
                    Colour = Color4.White,
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0.5f,
                },
            };

            UpdateTransform(scale, posX, posY);
        }

        /// <summary>
        /// 同步更新预览框的缩放比例与相对位置。
        /// </summary>
        public void UpdateTransform(float scale, float posX, float posY)
        {
            Scale = new Vector2(scale);
            Position = EzScaledBackgroundContainer.ComputeRelativePosition(scale, posX, posY);
        }
    }
}
