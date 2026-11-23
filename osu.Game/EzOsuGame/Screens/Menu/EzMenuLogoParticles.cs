// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.Graphics.Backgrounds;

namespace osu.Game.EzOsuGame.Screens.Menu
{
    public partial class EzMenuLogoParticles : CompositeDrawable
    {
        private static readonly Colour4 default_top_colour = Colour4.FromHex("1A2B6B");
        private static readonly Colour4 default_bottom_colour = Colour4.FromHex("3A6BB8");

        private readonly TrianglesV2 triangles;

        private Bindable<EzLogoParticlesStyle>? style;
        private Bindable<Colour4>? solidColour;
        private Bindable<Colour4>? topColour;
        private Bindable<Colour4>? bottomColour;

        public float Velocity
        {
            get => triangles.Velocity;
            set => triangles.Velocity = value;
        }

        public EzMenuLogoParticles()
        {
            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            InternalChild = triangles = new TrianglesV2
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Thickness = 0.009f,
                ScaleAdjust = 3,
                SpawnRatio = 1.4f,
                Colour = ColourInfo.GradientVertical(default_top_colour, default_bottom_colour),
            };
        }

        [BackgroundDependencyLoader(true)]
        private void load(Ez2ConfigManager? ezConfig)
        {
            if (ezConfig == null)
                return;

            style = ezConfig.GetBindable<EzLogoParticlesStyle>(Ez2Setting.MenuLogoParticlesStyle);
            solidColour = ezConfig.GetBindable<Colour4>(Ez2Setting.MenuLogoParticlesColour);
            topColour = ezConfig.GetBindable<Colour4>(Ez2Setting.MenuLogoParticlesColourTop);
            bottomColour = ezConfig.GetBindable<Colour4>(Ez2Setting.MenuLogoParticlesColourBottom);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (style == null || solidColour == null || topColour == null || bottomColour == null)
                return;

            style.BindValueChanged(_ => applyStyle(), true);
            solidColour.BindValueChanged(_ => applyStyle());
            topColour.BindValueChanged(_ => applyStyle());
            bottomColour.BindValueChanged(_ => applyStyle());
        }

        private void applyStyle()
        {
            if (style == null || solidColour == null || topColour == null || bottomColour == null)
                return;

            switch (style.Value)
            {
                case EzLogoParticlesStyle.Off:
                    Alpha = 0;
                    break;

                case EzLogoParticlesStyle.Solid:
                    triangles.Colour = ColourInfo.GradientVertical(solidColour.Value, solidColour.Value);
                    Alpha = 1;
                    break;

                case EzLogoParticlesStyle.Gradient:
                    triangles.Colour = ColourInfo.GradientVertical(topColour.Value, bottomColour.Value);
                    Alpha = 1;
                    break;
            }
        }
    }
}
