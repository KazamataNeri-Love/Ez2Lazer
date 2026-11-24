// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Skinning;
using osuTK;

namespace osu.Android.EzAndroid.SkinEditor
{
    internal sealed partial class AndroidSkinAnchorSelector : FillFlowContainer
    {
        private const float row_height = 36;

        private readonly Drawable component;
        private readonly ISerialisableDrawable serialisable;
        private readonly bool isOrigin;
        private readonly Action<Anchor?> apply;
        private readonly Dictionary<Anchor, RoundedButton> choices = new Dictionary<Anchor, RoundedButton>();

        private OsuColour colours = null!;
        private RoundedButton header = null!;
        private FillFlowContainer options = null!;
        private RoundedButton? closest;
        private RoundedButton? highlighted;
        private (Anchor? Value, bool Enabled)? currentState;

        public AndroidSkinAnchorSelector(Drawable component, ISerialisableDrawable serialisable, bool isOrigin, Action<Anchor?> apply)
        {
            this.component = component;
            this.serialisable = serialisable;
            this.isOrigin = isOrigin;
            this.apply = apply;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Direction = FillDirection.Vertical;
            Spacing = new Vector2(0, 4);
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour osuColour)
        {
            colours = osuColour;

            Add(header = new RoundedButton
            {
                RelativeSizeAxes = Axes.X,
                Height = row_height,
                Action = () =>
                {
                    if (options.IsPresent)
                        options.Hide();
                    else
                        options.Show();
                },
            });

            Add(options = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 4),
            });

            if (!isOrigin)
            {
                options.Add(closest = new RoundedButton
                {
                    RelativeSizeAxes = Axes.X,
                    Height = row_height,
                    Text = SkinEditorStrings.Closest,
                    BackgroundColour = colours.Gray3,
                    Action = () => choose(null),
                });
            }

            options.Add(new GridContainer
            {
                RelativeSizeAxes = Axes.X,
                Height = 3 * row_height,
                ColumnDimensions = [new Dimension(), new Dimension(), new Dimension()],
                RowDimensions =
                [
                    new Dimension(GridSizeMode.Absolute, row_height),
                    new Dimension(GridSizeMode.Absolute, row_height),
                    new Dimension(GridSizeMode.Absolute, row_height),
                ],
                Content = new Drawable[][]
                {
                    [createChoice(Anchor.TopLeft, @"TL"), createChoice(Anchor.TopCentre, @"TC"), createChoice(Anchor.TopRight, @"TR")],
                    [createChoice(Anchor.CentreLeft, @"CL"), createChoice(Anchor.Centre, @"C"), createChoice(Anchor.CentreRight, @"CR")],
                    [createChoice(Anchor.BottomLeft, @"BL"), createChoice(Anchor.BottomCentre, @"BC"), createChoice(Anchor.BottomRight, @"BR")],
                },
            });
        }

        protected override void Update()
        {
            base.Update();

            bool fixedAnchor = serialisable.UsesFixedAnchor;
            (Anchor? Value, bool Enabled) state = (isOrigin ? component.Origin : fixedAnchor ? component.Anchor : null, !isOrigin || fixedAnchor);
            if (currentState == state)
                return;

            if (currentState == null || currentState.Value.Value != state.Value)
            {
                LocalisableString value = state.Value is Anchor anchor ? anchor.ToString() : SkinEditorStrings.Closest;
                header.Text = LocalisableString.Format(@"{0}: {1}", isOrigin ? SkinEditorStrings.Origin : SkinEditorStrings.Anchor, value);

                if (highlighted != null)
                    highlighted.BackgroundColour = colours.Gray3;

                highlighted = state.Value is Anchor choice ? choices.GetValueOrDefault(choice) : closest;

                if (highlighted != null)
                    highlighted.BackgroundColour = colours.Blue4;
            }

            header.Enabled.Value = state.Enabled;

            if (!state.Enabled)
                options.Hide();

            currentState = state;
        }

        private RoundedButton createChoice(Anchor anchor, string label)
        {
            var button = new RoundedButton
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.X,
                Width = 0.94f,
                Height = row_height - 2,
                Text = label,
                TooltipText = anchor.ToString(),
                BackgroundColour = colours.Gray3,
                Action = () => choose(anchor),
            };
            choices.Add(anchor, button);
            return button;
        }

        private void choose(Anchor? anchor)
        {
            options.Hide();

            if (component.Parent != null && (!isOrigin || serialisable.UsesFixedAnchor))
                apply(anchor);
        }
    }
}
