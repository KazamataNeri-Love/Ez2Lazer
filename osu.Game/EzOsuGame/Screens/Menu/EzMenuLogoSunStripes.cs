// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.EzOsuGame.Configuration;
using osuTK;

namespace osu.Game.EzOsuGame.Screens.Menu
{
    public partial class EzMenuLogoSunStripes : CompositeDrawable
    {
        private const int max_bands = 10;
        private const float top_end = 0.44f;
        private const float lower_height = 0.56f;
        private const int stripe_count = 6;

        private readonly Box[] bands = new Box[max_bands];
        private Bindable<EzLogoSunStripesStyle>? style;
        private Bindable<EzLogoBackdropStyle>? backdropStyle;
        private Bindable<Colour4>? solidColour;
        private Bindable<Colour4>? topColour;
        private Bindable<Colour4>? bottomColour;
        private EzLogoSunStripesStyle appliedStyle;
        private Colour4 appliedTop;
        private Colour4 appliedBottom;
        private float scrollPhase;

        public EzMenuLogoSunStripes()
        {
            RelativeSizeAxes = Axes.Both;
            InternalChildren = bands.Select(_ => new Box
            {
                RelativeSizeAxes = Axes.Both,
                RelativePositionAxes = Axes.Y,
                Width = 1,
                Alpha = 0,
            }).ToArray();

            for (int i = 0; i < max_bands; i++)
                bands[i] = (Box)InternalChildren[i];
        }

        [BackgroundDependencyLoader(true)]
        private void load(Ez2ConfigManager? ezConfig)
        {
            if (ezConfig == null)
                return;

            style = ezConfig.GetBindable<EzLogoSunStripesStyle>(Ez2Setting.MenuLogoSunStripesStyle);
            backdropStyle = ezConfig.GetBindable<EzLogoBackdropStyle>(Ez2Setting.MenuLogoBackdropStyle);
            solidColour = ezConfig.GetBindable<Colour4>(Ez2Setting.MenuLogoBackdropColour);
            topColour = ezConfig.GetBindable<Colour4>(Ez2Setting.MenuLogoBackdropColourTop);
            bottomColour = ezConfig.GetBindable<Colour4>(Ez2Setting.MenuLogoBackdropColourBottom);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (style == null || backdropStyle == null || solidColour == null || topColour == null || bottomColour == null)
                return;

            style.BindValueChanged(_ => refresh(), true);
            backdropStyle.BindValueChanged(_ => refresh());
            solidColour.BindValueChanged(_ => refresh());
            topColour.BindValueChanged(_ => refresh());
            bottomColour.BindValueChanged(_ => refresh());
        }

        protected override void Update()
        {
            base.Update();

            if (style?.Value != EzLogoSunStripesStyle.Scrolling || !isEffectivelyVisible)
                return;

            scrollPhase = (scrollPhase + (float)(Time.Elapsed / 1000.0) * 0.25f) % 1f;
            refresh();
        }

        /// <summary>
        /// Calculates the visible unit-height intervals for a stripe animation phase.
        /// </summary>
        /// <param name="phase">A repeating phase in the range from zero to one.</param>
        /// <param name="output">Storage for at least ten interval pairs.</param>
        /// <returns>The number of intervals written.</returns>
        public static int ComputeBands(float phase, Span<(float Start, float End)> output)
        {
            if (output.Length < max_bands)
                throw new ArgumentException("Output must hold ten stripe bands.", nameof(output));

            phase -= MathF.Floor(phase);
            int bandCount = 0;
            float cursor = 0;

            for (int i = 0; i < stripe_count; i++)
            {
                float u = (i + phase) / stripe_count;
                float gapStart = top_end + lower_height * MathF.Pow(u, 1.45f);
                float gapWidth = lower_height * 0.12f * MathF.Pow(u, 1.35f);
                float gapEnd = Math.Min(1, gapStart + gapWidth);

                if (gapEnd <= gapStart)
                    continue;

                if (gapStart > cursor)
                    output[bandCount++] = (cursor, gapStart);

                cursor = Math.Max(cursor, gapEnd);
            }

            if (cursor < 1)
                output[bandCount++] = (cursor, 1);

            return bandCount;
        }

        private void refresh()
        {
            if (style == null || backdropStyle == null || solidColour == null || topColour == null || bottomColour == null)
                return;

            if (style.Value == EzLogoSunStripesStyle.Off)
            {
                for (int i = 0; i < bands.Length; i++)
                    bands[i].Alpha = 0;
                appliedStyle = style.Value;
                return;
            }

            Colour4 top = backdropStyle.Value == EzLogoBackdropStyle.Solid ? solidColour.Value : topColour.Value;
            Colour4 bottom = backdropStyle.Value == EzLogoBackdropStyle.Solid ? solidColour.Value.Darken(0.2f) : bottomColour.Value;

            if (style.Value == EzLogoSunStripesStyle.Static
                && appliedStyle == style.Value
                && appliedTop == top
                && appliedBottom == bottom)
                return;

            Span<(float Start, float End)> geometry = stackalloc (float Start, float End)[max_bands];
            int count = ComputeBands(style.Value == EzLogoSunStripesStyle.Scrolling ? scrollPhase : 0, geometry);
            ColourInfo gradient = ColourInfo.GradientVertical(top, bottom);

            for (int i = 0; i < bands.Length; i++)
            {
                if (i >= count || geometry[i].End <= geometry[i].Start)
                {
                    bands[i].Alpha = 0;
                    continue;
                }

                float start = geometry[i].Start;
                float end = geometry[i].End;
                bands[i].Y = start;
                bands[i].Height = end - start;
                bands[i].Colour = ColourInfo.GradientVertical(gradient.Interpolate(new Vector2(0, start)), gradient.Interpolate(new Vector2(0, end)));
                bands[i].Alpha = 1;
            }

            appliedStyle = style.Value;
            appliedTop = top;
            appliedBottom = bottom;
        }

        private bool isEffectivelyVisible
        {
            get
            {
                for (Drawable? drawable = this; drawable != null; drawable = drawable.Parent)
                {
                    if (!drawable.IsPresent || drawable.Alpha <= 0)
                        return false;
                }

                return true;
            }
        }
    }
}
