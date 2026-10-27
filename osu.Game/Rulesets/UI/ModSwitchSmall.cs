// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Overlays;
using osu.Game.Rulesets.Mods;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.UI
{
    public partial class ModSwitchSmall : CompositeDrawable
    {
        public BindableBool Active { get; } = new BindableBool();

        public const float DEFAULT_SIZE = 60;

        private readonly IMod mod;

        private SpriteIcon? modIcon;

        private Color4 activeForegroundColour;
        private Color4 inactiveForegroundColour;

        public ModSwitchSmall(IMod mod)
        {
            this.mod = mod;

            Size = new Vector2(DEFAULT_SIZE);
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours, OverlayColourProvider? colourProvider)
        {
            FillFlowContainer contentFlow;
            ModSwitchTiny tinySwitch;

            InternalChild = contentFlow = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Direction = FillDirection.Vertical,
                Child = tinySwitch = new ModSwitchTiny(mod)
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Scale = new Vector2(0.6f),
                    Active = { BindTarget = Active }
                }
            };

            if (mod.Icon != null)
            {
                contentFlow.Insert(-1, modIcon = new SpriteIcon
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Size = new Vector2(50, 34),
                    // arbitrary adjustment for better vertical alignment
                    Margin = new MarginPadding { Top = -1 },
                    Icon = mod.Icon.Value
                });
                tinySwitch.Scale = new Vector2(0.36f);
            }

            var modTypeColour = colours.ForModType(mod.Type);

            inactiveForegroundColour = colourProvider?.Background5 ?? colours.Gray3;
            activeForegroundColour = modTypeColour;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Active.BindValueChanged(_ => updateState(), true);
            FinishTransforms(true);
        }

        private void updateState()
        {
            modIcon?.FadeColour(Active.Value ? activeForegroundColour : inactiveForegroundColour, 200, Easing.OutQuint);
        }
    }
}
