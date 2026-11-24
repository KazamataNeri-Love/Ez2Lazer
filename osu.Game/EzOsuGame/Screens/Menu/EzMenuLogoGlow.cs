// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.Screens.Menu;

namespace osu.Game.EzOsuGame.Screens.Menu
{
    public partial class EzMenuLogoGlow : CircularContainer
    {
        private Bindable<bool>? enabled;
        private Bindable<Colour4>? colour;
        private Bindable<double>? radius;

        public EzMenuLogoGlow()
        {
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;
            RelativeSizeAxes = Axes.Both;
            Scale = OsuLogo.SCALE_ADJUST;
            BypassAutoSizeAxes = Axes.Both;
            Masking = true;
            InternalChild = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = Colour4.Transparent,
                AlwaysPresent = true,
            };
        }

        [BackgroundDependencyLoader(true)]
        private void load(Ez2ConfigManager? ezConfig)
        {
            if (ezConfig == null)
                return;

            enabled = ezConfig.GetBindable<bool>(Ez2Setting.MenuLogoGlowEnabled);
            colour = ezConfig.GetBindable<Colour4>(Ez2Setting.MenuLogoGlowColour);
            radius = ezConfig.GetBindable<double>(Ez2Setting.MenuLogoGlowRadius);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (enabled == null || colour == null || radius == null)
                return;

            enabled.BindValueChanged(_ => apply(), true);
            colour.BindValueChanged(_ => apply());
            radius.BindValueChanged(_ => apply());
        }

        private void apply()
        {
            if (enabled == null || colour == null || radius == null)
                return;

            Alpha = enabled.Value ? 1 : 0;
            EdgeEffect = enabled.Value
                ? new EdgeEffectParameters
                {
                    Type = EdgeEffectType.Glow,
                    Colour = new Colour4(colour.Value.R, colour.Value.G, colour.Value.B, colour.Value.A * 0.75f),
                    Radius = (float)radius.Value,
                    Hollow = true,
                }
                : default;
        }
    }
}
