// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Localisation;
using osu.Game.Database;
using osu.Game.EzOsuGame.Localization;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using osu.Game.Overlays.Settings;
using osu.Game.Overlays.Settings.Sections.Maintenance;
using osu.Game.Scoring;

namespace osu.Game.EzOsuGame.Maintenance
{
    public partial class EzReplayMaintenanceSettings : SettingsSubsection
    {
        protected override LocalisableString Header => EzReplayMaintenanceStrings.SECTION_HEADER;

        [BackgroundDependencyLoader]
        private void load(RealmAccess realm, ScoreManager scores, IDialogOverlay? dialogOverlay, INotificationOverlay? notifications)
        {
            var service = new EzReplayMaintenanceService(realm, scores);

            Add(new SettingsButtonV2
            {
                Text = EzReplayMaintenanceStrings.MANAGE_BUTTON,
                TooltipText = EzReplayMaintenanceStrings.MANAGE_BUTTON_TOOLTIP,
                Keywords = new[] { "replay", "score", "delete", "回放", "成绩", "删除", "玩家", "用户名" },
                Action = () => requestDialog(service, dialogOverlay, notifications),
            });
        }

        private void requestDialog(EzReplayMaintenanceService service, IDialogOverlay? dialogOverlay, INotificationOverlay? notifications)
        {
            if (dialogOverlay == null)
                return;

            Task.Run(() => service.ScanUsernameCounts()).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    notifications?.Post(new SimpleErrorNotification { Text = EzReplayMaintenanceStrings.SCAN_FAILED });
                    return;
                }

                if (t.IsCanceled)
                    return;

                var counts = t.GetResultSafely();

                Schedule(() =>
                {
                    if (counts.Count == 0)
                    {
                        notifications?.Post(new SimpleNotification { Text = EzReplayMaintenanceStrings.NO_SCORES });
                        return;
                    }

                    dialogOverlay.Push(new EzReplayMaintenanceDialog(counts, chosen => dialogOverlay.Push(
                        new MassDeleteConfirmationDialog(
                            () => delete(service, notifications, chosen),
                            LocalisableString.Format(EzReplayMaintenanceStrings.CONFIRM_BODY.ToString(), string.Join(", ", chosen))))));
                });
            });
        }

        private void delete(EzReplayMaintenanceService service, INotificationOverlay? notifications, IReadOnlyCollection<string> usernames)
        {
            notifications?.Post(new SimpleNotification { Text = EzReplayMaintenanceStrings.DELETING });

            service.DeleteUsernamesAsync(usernames).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    notifications?.Post(new SimpleErrorNotification { Text = EzReplayMaintenanceStrings.DELETE_FAILED });
                    return;
                }

                if (t.IsCanceled)
                    return;

                notifications?.Post(new ProgressCompletionNotification
                {
                    Text = LocalisableString.Format(EzReplayMaintenanceStrings.DELETE_DONE.ToString(), t.GetResultSafely()),
                });
            });
        }
    }
}
