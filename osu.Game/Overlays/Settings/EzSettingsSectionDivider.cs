// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Overlays.Settings
{
    /// <summary>
    /// A divider + title row used to visually group settings in the skin editor sidebar.
    /// Bound to a dummy <see cref="Bindable{TValue}"/> so it can be driven by <see cref="SettingSourceAttribute"/>.
    /// </summary>
    public partial class EzSettingsSectionDivider : SettingsItem<bool>
    {
        public override LocalisableString LabelText
        {
            get => dividerControl.Title;
            set => dividerControl.Title = value;
        }

        protected override Drawable CreateControl() => dividerControl;

        private readonly DividerControl dividerControl = new DividerControl();

        public EzSettingsSectionDivider()
        {
        }

        private partial class DividerControl : CompositeDrawable, IHasCurrentValue<bool>
        {
            public Bindable<bool> Current { get; set; } = new Bindable<bool>();

            private readonly SpriteText title;

            public LocalisableString Title
            {
                get => title.Text;
                set => title.Text = value;
            }

            public DividerControl()
            {
                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;

                InternalChild = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 3),
                    Padding = new MarginPadding { Top = 10, Bottom = 4 },
                    Children = new Drawable[]
                    {
                        title = new OsuSpriteText
                        {
                            Font = OsuFont.Default.With(size: 14, weight: FontWeight.Bold),
                            Colour = Color4Extensions.FromHex(@"FF6A9F"),
                        },
                        new Box
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 1.5f,
                            Colour = new Color4(1f, 1f, 1f, 0.15f),
                        }
                    }
                };
            }
        }
    }
}
