// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.EzOsuGame.Localization;
using osu.Game.EzOsuGame.LocalProfile;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings;
using osuTK;

namespace osu.Game.EzOsuGame.Identity
{
    public partial class EzLocalIdentitySection : FillFlowContainer
    {
        [Resolved]
        private EzLocalProfileOverlay? localProfileOverlay { get; set; }

        [Resolved]
        private Ez2ConfigManager ezConfig { get; set; } = null!;

        [Resolved(CanBeNull = true)]
        private EzLocalProfileService? localProfileService { get; set; }

        [Resolved(CanBeNull = true)]
        private IDialogOverlay? dialogOverlay { get; set; }

        /// <summary>当前选中的本地账户名。必须持有字段：框架的绑定副本是弱引用。</summary>
        private Bindable<string> localAccountName = null!;

        /// <summary>接管开关。必须持有字段：同上。</summary>
        private Bindable<bool> localAccountNameTakesOver = null!;

        private const float button_gap = 2.5f;

        private FormDropdown<string> localAccountNameDropdown = null!;

        /// <summary>
        /// Called to request a hide of a parent displaying this section.
        /// </summary>
        public Action? RequestHide;

        public EzLocalIdentitySection()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Direction = FillDirection.Vertical;
            Spacing = new Vector2(0f, SettingsSection.ITEM_SPACING);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            localAccountName = ezConfig.GetBindable<string>(Ez2Setting.LocalAccountName);
            localAccountNameTakesOver = ezConfig.GetBindable<bool>(Ez2Setting.LocalAccountNameTakesOverScore);
            AddRange(createLocalIdentityChildren());
        }

        /// <summary>
        /// 刷新本地账户名候选：当前值 + 已有成绩里出现过的用户名，免得用户手输。
        /// </summary>
        private void refreshLocalAccountNameItems()
        {
            // 当前值必须始终留在列表里：否则框架的 ensureItemSelectionIsValid 会把 Current
            // 顶到列表第一项，等于一打开菜单就偷偷改掉用户的本地账户名。
            setLocalAccountNameItems(null);

            if (localProfileService == null)
                return;

            // 全库扫描，放后台线程，避免卡住菜单展开。
            Task.Run(() => localProfileService.ScanUsernameCounts()).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    return;

                var counts = t.GetResultSafely();

                Schedule(() => setLocalAccountNameItems(counts.Select(c => c.Username)));
            });
        }

        /// <summary>
        /// 用当前值加一批候选名重建列表，统一按名称 A–Z 排序；当前值即使为空也保留。
        /// </summary>
        private void setLocalAccountNameItems(IEnumerable<string>? names)
        {
            if (localAccountNameDropdown == null)
                return;

            string current = localAccountName.Value;
            var seen = new HashSet<string>(StringComparer.Ordinal) { current };
            var items = new List<string> { current };

            foreach (string name in names ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                if (seen.Add(name))
                    items.Add(name);
            }

            localAccountNameDropdown.Items = items.OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                                                   .ThenBy(n => n, StringComparer.Ordinal)
                                                   .ToList();
        }

        private void addLocalAccountNameItem(string name)
        {
            if (localAccountNameDropdown == null || string.IsNullOrWhiteSpace(name))
                return;

            var items = localAccountNameDropdown.Items?.ToList() ?? new List<string>();

            if (items.Contains(name, StringComparer.Ordinal))
                return;

            items.Add(name);
            setLocalAccountNameItems(items);
        }

        /// <summary>
        /// 本地身份那一组：本地账户名 + 新增/重命名 + 本地档案 + 接管开关。
        /// </summary>
        private IEnumerable<Drawable> createLocalIdentityChildren()
        {
            localAccountNameDropdown = new FormDropdown<string>
            {
                Caption = EzSettingsStrings.LOCAL_ACCOUNT_NAME,
                HintText = EzSettingsStrings.LOCAL_ACCOUNT_NAME_TOOLTIP,
                Current = localAccountName,
            };

            refreshLocalAccountNameItems();

            yield return localAccountNameDropdown;

            // 两个按钮各占一半，靠内部 Padding 让出中间的缝隙：外框总宽与上下控件（含本地档案按钮）一致。
            yield return new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Horizontal,
                Children = new Drawable[]
                {
                    new SettingsButtonV2
                    {
                        Text = EzSettingsStrings.LOCAL_ACCOUNT_NAME_ADD,
                        TooltipText = EzSettingsStrings.LOCAL_ACCOUNT_NAME_ADD_TOOLTIP,
                        RelativeSizeAxes = Axes.X,
                        Width = 0.5f,
                        Padding = new MarginPadding { Vertical = SettingsPanel.CONTENT_PADDING.Top, Right = button_gap },
                        Action = () => showLocalAccountNameDialog(string.Empty),
                    },
                    new SettingsButtonV2
                    {
                        Text = EzSettingsStrings.LOCAL_ACCOUNT_NAME_RENAME,
                        TooltipText = EzSettingsStrings.LOCAL_ACCOUNT_NAME_RENAME_TOOLTIP,
                        RelativeSizeAxes = Axes.X,
                        Width = 0.5f,
                        Padding = new MarginPadding { Vertical = SettingsPanel.CONTENT_PADDING.Top, Left = button_gap },
                        Action = () => showLocalAccountNameDialog(localAccountName.Value),
                    },
                },
            };

            yield return new RoundedButton
            {
                RelativeSizeAxes = Axes.X,
                Text = EzSettingsProfile.LOCAL_PROFILE_PANEL_BUTTON,
                Action = () =>
                {
                    RequestHide?.Invoke();
                    localProfileOverlay?.Show();
                },
            };

            yield return new SettingsCheckbox
            {
                LabelText = EzSettingsStrings.LOCAL_ACCOUNT_NAME_TAKEOVER,
                TooltipText = EzSettingsStrings.LOCAL_ACCOUNT_NAME_TAKEOVER_TOOLTIP,
                Current = localAccountNameTakesOver,
            };
        }

        private void showLocalAccountNameDialog(string initial)
        {
            dialogOverlay?.Push(new EzLocalAccountNameDialog(initial, name =>
            {
                if (string.IsNullOrWhiteSpace(name))
                    return;

                localAccountName.Value = name;
                addLocalAccountNameItem(name);
            }));
        }
    }
}
