// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.EzOsuGame.Localization;
using osu.Game.EzOsuGame.LocalProfile;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays.Dialog;
using osuTK;

namespace osu.Game.EzOsuGame.Maintenance
{
    public partial class EzReplayMaintenanceDialog : PopupDialog
    {
        private const float content_width = 420;
        private const float row_height = 36;
        private const float list_max_height = 220;

        private readonly Dictionary<string, BindableBool> selections = new Dictionary<string, BindableBool>(StringComparer.Ordinal);
        private readonly BindableBool hasSelection = new BindableBool();

        public EzReplayMaintenanceDialog(IReadOnlyList<EzLocalProfileUsernameCount> usernameCounts, Action<IReadOnlyList<string>> onDeleteRequested)
        {
            HeaderText = EzReplayMaintenanceStrings.DIALOG_HEADER;
            BodyText = EzReplayMaintenanceStrings.DIALOG_BODY;
            Icon = FontAwesome.Solid.Trash;

            var flow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(4),
            };

            foreach (var entry in usernameCounts)
            {
                var bindable = new BindableBool();
                selections[entry.Username] = bindable;

                bindable.ValueChanged += _ => updateHasSelection();

                flow.Add(new OsuCheckbox
                {
                    LabelText = $"{entry.Username}  ({entry.ScoreCount})",
                    Current = { BindTarget = bindable },
                });
            }

            updateHasSelection();

            float listHeight = Math.Clamp(
                usernameCounts.Count * row_height + Math.Max(0, usernameCounts.Count - 1) * 4,
                row_height,
                list_max_height);

            MainContent.Child = new Container
            {
                Margin = new MarginPadding { Top = 12 },
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Width = content_width,
                Height = listHeight,
                Child = new OsuScrollContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = flow,
                },
            };

            Buttons = new PopupDialogButton[]
            {
                new PopupDialogDangerousButton
                {
                    Text = EzReplayMaintenanceStrings.DELETE,
                    Enabled = { BindTarget = hasSelection },
                    Action = () =>
                    {
                        var chosen = selections.Where(kv => kv.Value.Value).Select(kv => kv.Key).ToList();

                        if (chosen.Count > 0)
                            onDeleteRequested(chosen);
                    }
                },
                new PopupDialogCancelButton
                {
                    Text = EzReplayMaintenanceStrings.CANCEL,
                },
            };
        }

        private void updateHasSelection() => hasSelection.Value = selections.Values.Any(b => b.Value);
    }
}
