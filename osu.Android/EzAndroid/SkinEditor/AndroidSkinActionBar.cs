// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Overlays.SkinEditor;

namespace osu.Android.EzAndroid.SkinEditor
{
    internal sealed partial class AndroidSkinActionBar : GridContainer
    {
        private const float row_height = 38;

        private readonly SkinSettingsToolboxContext context;
        private readonly Action reset;

        public AndroidSkinActionBar(SkinSettingsToolboxContext context, Action reset)
        {
            this.context = context;
            this.reset = reset;

            RelativeSizeAxes = Axes.X;
            Height = 2 * row_height;
            ColumnDimensions = [new Dimension(), new Dimension()];
            RowDimensions = [new Dimension(GridSizeMode.Absolute, row_height), new Dimension(GridSizeMode.Absolute, row_height)];
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            var delete = createButton(@"Delete", context.DeleteComponent);
            delete.BackgroundColour = colours.Red4;

            Content = new Drawable[][]
            {
                [createButton(SkinEditorStrings.BringToFront, context.BringToFront), createButton(SkinEditorStrings.SendToBack, context.SendToBack)],
                [createButton(@"Reset transform", reset), delete],
            };
        }

        private static RoundedButton createButton(LocalisableString text, Action? action) => new RoundedButton
        {
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            RelativeSizeAxes = Axes.X,
            Width = 0.96f,
            Height = row_height - 2,
            Text = text,
            Action = action,
            Enabled = { Value = action != null },
        };
    }
}
