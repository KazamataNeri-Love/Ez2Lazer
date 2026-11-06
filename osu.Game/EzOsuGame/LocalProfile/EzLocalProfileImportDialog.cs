// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.EzOsuGame.Localization;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays.Dialog;
using osuTK;

namespace osu.Game.EzOsuGame.LocalProfile
{
    public partial class EzLocalProfileImportDialog : PopupDialog
    {
        private const float content_width = 420;
        private const float row_height = 36;
        private const float list_max_height = 180;

        private readonly Dictionary<string, BindableBool> selections = new Dictionary<string, BindableBool>(StringComparer.Ordinal);
        private readonly BindableBool clearRebuild = new BindableBool();

        /// <summary>True while at least one player is checked; drives the delete button's enabled state.</summary>
        private readonly BindableBool hasSelection = new BindableBool();

        private readonly Bindable<LocalisableString> sortOrder =
            new Bindable<LocalisableString>(EzSettingsProfile.LOCAL_PROFILE_IMPORT_SORT_BY_COUNT);

        public EzLocalProfileImportDialog(
            IReadOnlyList<EzLocalProfileUsernameCount> usernameCounts,
            IReadOnlyCollection<string> previouslyIncluded,
            Action<IReadOnlyList<string>, bool> onConfirm,
            Action<IReadOnlyList<string>>? onDeleteRequested = null)
        {
            HeaderText = EzSettingsProfile.LOCAL_PROFILE_IMPORT_HEADER;
            BodyText = EzSettingsProfile.LOCAL_PROFILE_IMPORT_BODY;
            Icon = FontAwesome.Solid.User;

            var previous = new HashSet<string>(previouslyIncluded, StringComparer.Ordinal);
            bool usePrevious = previous.Count > 0 && usernameCounts.Any(c => previous.Contains(c.Username));

            var flow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(4),
            };

            void rebuildRows()
            {
                flow.Clear();

                foreach (var entry in orderedEntries(usernameCounts, sortOrder.Value == EzSettingsProfile.LOCAL_PROFILE_IMPORT_SORT_BY_NAME))
                {
                    if (!selections.TryGetValue(entry.Username, out var bindable))
                    {
                        bindable = new BindableBool(!usePrevious || previous.Contains(entry.Username));
                        bindable.ValueChanged += _ => updateHasSelection();
                        selections[entry.Username] = bindable;
                    }

                    flow.Add(new OsuCheckbox
                    {
                        LabelText = $"{entry.Username}  ({entry.ScoreCount})",
                        Current = { BindTarget = bindable },
                    });
                }
            }

            rebuildRows();
            sortOrder.ValueChanged += _ => rebuildRows();

            updateHasSelection();

            var sortTabs = new OsuTabControl<LocalisableString>
            {
                RelativeSizeAxes = Axes.X,
                Height = 30,
                Items = new LocalisableString[] { EzSettingsProfile.LOCAL_PROFILE_IMPORT_SORT_BY_COUNT, EzSettingsProfile.LOCAL_PROFILE_IMPORT_SORT_BY_NAME },
            };

            sortTabs.Current.BindTo(sortOrder);

            float listHeight = Math.Clamp(
                usernameCounts.Count * row_height + Math.Max(0, usernameCounts.Count - 1) * 4,
                row_height,
                list_max_height);

            MainContent.Child = new FillFlowContainer
            {
                Margin = new MarginPadding { Top = 12 },
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Width = content_width,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(8),
                Children = new Drawable[]
                {
                    sortTabs,
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = listHeight,
                        Child = new OsuScrollContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Child = flow,
                        }
                    },
                    new OsuCheckbox
                    {
                        LabelText = EzSettingsProfile.LOCAL_PROFILE_IMPORT_CLEAR_REBUILD,
                        Current = { BindTarget = clearRebuild },
                    },
                }
            };

            Buttons = new PopupDialogButton[]
            {
                new PopupDialogOkButton
                {
                    Text = EzSettingsProfile.LOCAL_PROFILE_IMPORT_CONFIRM,
                    Action = () =>
                    {
                        var chosen = selections.Where(kv => kv.Value.Value).Select(kv => kv.Key).ToList();
                        onConfirm(chosen, clearRebuild.Value);
                    }
                },
                new PopupDialogDangerousButton
                {
                    Text = EzSettingsProfile.LOCAL_PROFILE_IMPORT_DELETE,
                    Enabled = { BindTarget = hasSelection },
                    Action = () =>
                    {
                        var chosen = selections.Where(kv => kv.Value.Value).Select(kv => kv.Key).ToList();

                        if (chosen.Count > 0)
                            onDeleteRequested?.Invoke(chosen);
                    }
                },
                new PopupDialogCancelButton
                {
                    Text = EzSettingsProfile.LOCAL_PROFILE_IMPORT_CANCEL,
                }
            };
        }

        private void updateHasSelection() => hasSelection.Value = selections.Values.Any(b => b.Value);

        private static IEnumerable<EzLocalProfileUsernameCount> orderedEntries(
            IReadOnlyList<EzLocalProfileUsernameCount> entries,
            bool byUsername)
        {
            if (byUsername)
            {
                return entries.OrderBy(e => e.Username, StringComparer.OrdinalIgnoreCase)
                              .ThenBy(e => e.Username, StringComparer.Ordinal);
            }

            return entries.OrderByDescending(e => e.ScoreCount)
                          .ThenBy(e => e.Username, StringComparer.Ordinal);
        }
    }
}
