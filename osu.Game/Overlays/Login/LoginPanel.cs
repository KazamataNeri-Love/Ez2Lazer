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
using osu.Framework.Graphics.Primitives;
using osu.Framework.Input.Events;
using osu.Game.Configuration;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.EzOsuGame.Identity;
using osu.Game.EzOsuGame.Localization;
using osu.Game.EzOsuGame.LocalProfile;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings;
using osu.Game.Users;
using osuTK;

namespace osu.Game.Overlays.Login
{
    public partial class LoginPanel : Container
    {
        private bool bounding = true;

        private Drawable? form;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        private UserDropdown? dropdown;

        /// <summary>
        /// Called to request a hide of a parent displaying this container.
        /// </summary>
        public Action? RequestHide;

        private readonly IBindable<APIState> apiState = new Bindable<APIState>();
        private readonly Bindable<UserStatus> configUserStatus = new Bindable<UserStatus>();

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

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

        /// <summary>并排按钮之间的视觉缝隙（靠内部 Padding 让出，外框总宽不变）。</summary>
        private const float button_gap = 2.5f;

        private FormDropdown<string> localAccountNameDropdown = null!;

        public override RectangleF BoundingBox => bounding ? base.BoundingBox : RectangleF.Empty;

        public bool Bounding
        {
            get => bounding;
            set
            {
                bounding = value;
                Invalidate(Invalidation.MiscGeometry);
            }
        }

        public LoginPanel()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            config.BindWith(OsuSetting.UserOnlineStatus, configUserStatus);
            configUserStatus.BindValueChanged(e => updateDropdownCurrent(e.NewValue), true);

            apiState.BindTo(api.State);
            apiState.BindValueChanged(onlineStateChanged, true);
        }

        private void onlineStateChanged(ValueChangedEvent<APIState> state) => Schedule(() =>
        {
            form = null;

            switch (state.NewValue)
            {
                case APIState.Offline:
                {
                    // 未登录时本地身份是唯一能用的身份来源：不接上这一组，游玩只会留下 Guest 署名。
                    form = new LoginForm
                    {
                        RequestHide = RequestHide
                    };

                    var offlineChildren = new List<Drawable>
                    {
                        form,
                    };

                    offlineChildren.AddRange(createLocalIdentityChildren());

                    Child = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Padding = new MarginPadding { Horizontal = SettingsPanel.CONTENT_MARGINS },
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0f, SettingsSection.ITEM_SPACING),
                        Children = offlineChildren,
                    };

                    break;
                }

                case APIState.RequiresSecondFactorAuth:
                    Child = form = new SecondFactorAuthForm();
                    break;

                case APIState.Failing:
                case APIState.Connecting:
                    LinkFlowContainer linkFlow;

                    Child = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Padding = new MarginPadding { Horizontal = SettingsPanel.CONTENT_MARGINS },
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0f, SettingsSection.ITEM_SPACING),
                        Children = new Drawable[]
                        {
                            new LoadingSpinner
                            {
                                State = { Value = Visibility.Visible },
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                            },
                            linkFlow = new LinkFlowContainer
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                TextAnchor = Anchor.TopCentre,
                                AutoSizeAxes = Axes.Both,
                            },
                        },
                    };

                    if (!string.IsNullOrEmpty(api.UserFacingOutageMessage.Value))
                    {
                        linkFlow.AddText("Server outage in progress".ToUpperInvariant(), s =>
                        {
                            s.Font = OsuFont.Style.Caption2.With(weight: FontWeight.Bold);
                            s.Colour = Colour4.Orange;
                        });

                        linkFlow.AddParagraph(api.UserFacingOutageMessage.Value, s => s.Font = OsuFont.Style.Caption1);
                    }
                    else if (state.NewValue == APIState.Failing)
                    {
                        linkFlow.AddParagraph(state.NewValue == APIState.Failing ? ToolbarStrings.AttemptingToReconnect : ToolbarStrings.Connecting, s =>
                        {
                            s.Font = OsuFont.Style.Caption2.With(weight: FontWeight.Bold);
                            s.Colour = Colour4.Orange;
                        });
                    }
                    else
                    {
                        linkFlow.AddParagraph(ToolbarStrings.Connecting, s =>
                            s.Font = OsuFont.Style.Caption2.With(weight: FontWeight.Bold));
                    }

                    linkFlow.NewParagraph();
                    linkFlow.AddLink(LoginPanelStrings.SignOut, api.Logout, string.Empty, s => s.Font = OsuFont.Style.Caption2);
                    break;

                case APIState.Online:
                case APIState.LocalOnline:
                {
                    localAccountName = ezConfig.GetBindable<string>(Ez2Setting.LocalAccountName);
                    localAccountNameTakesOver = ezConfig.GetBindable<bool>(Ez2Setting.LocalAccountNameTakesOverScore);

                    dropdown = new UserDropdown { RelativeSizeAxes = Axes.X };

                    // 自上而下：网络账号卡片 → 在线状态 → 本地账户名 → 本地档案 → 接管成绩。
                    var signedInChildren = new List<Drawable>
                    {
                        new OsuSpriteText
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Text = LoginPanelStrings.SignedIn,
                            Font = OsuFont.GetFont(size: 18, weight: FontWeight.Bold),
                        },
                        new UserRankPanel(api.LocalUser.Value)
                        {
                            RelativeSizeAxes = Axes.X,
                            Action = RequestHide
                        },
                        dropdown,
                    };

                    signedInChildren.AddRange(createLocalIdentityChildren());

                    Child = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Padding = new MarginPadding { Horizontal = SettingsPanel.CONTENT_MARGINS },
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0f, SettingsSection.ITEM_SPACING),
                        Children = signedInChildren,
                    };

                    updateDropdownCurrent(configUserStatus.Value);
                    dropdown.Current.BindValueChanged(action =>
                    {
                        switch (action.NewValue)
                        {
                            case UserAction.Online:
                                configUserStatus.Value = UserStatus.Online;
                                dropdown.StatusColour = colours.Green;
                                break;

                            case UserAction.DoNotDisturb:
                                configUserStatus.Value = UserStatus.DoNotDisturb;
                                dropdown.StatusColour = colours.Red;
                                break;

                            case UserAction.AppearOffline:
                                configUserStatus.Value = UserStatus.Offline;
                                dropdown.StatusColour = colours.Gray7;
                                break;

                            case UserAction.SignOut:
                                api.Logout();
                                break;
                        }
                    }, true);

                    break;
                }
            }

            if (form != null)
                ScheduleAfterChildren(() => GetContainingFocusManager()?.ChangeFocus(form));
        });

        /// <summary>
        /// 刷新本地账户名候选：当前值 + 已有成绩里出现过的用户名，免得用户手输。
        /// </summary>
        private void refreshLocalAccountNameItems()
        {
            // 当前值必须始终留在列表里：否则框架的 ensureItemSelectionIsValid 会把 Current
            // 顶到列表第一项，等于一打开菜单就偷偷改掉用户的本地账户名。
            localAccountNameDropdown.Items = new List<string> { localAccountName.Value };

            if (localProfileService == null)
                return;

            // 全库扫描，放后台线程，避免卡住菜单展开。
            Task.Run(() => localProfileService.ScanUsernameCounts()).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    return;

                var counts = t.GetResultSafely();

                Schedule(() =>
                {
                    foreach (var entry in counts)
                        addLocalAccountNameItem(entry.Username);
                });
            });
        }

        private void addLocalAccountNameItem(string name)
        {
            if (localAccountNameDropdown == null || string.IsNullOrWhiteSpace(name))
                return;

            var items = localAccountNameDropdown.Items?.ToList() ?? new List<string>();

            if (items.Contains(name, StringComparer.Ordinal))
                return;

            items.Add(name);
            localAccountNameDropdown.Items = items;
        }

        /// <summary>
        /// 本地身份那一组：本地账户名 + 新增/重命名 + 本地档案 + 接管开关。
        /// </summary>
        /// <remarks>
        /// 登录与未登录两种状态下都要有：未登录时它是唯一的身份来源。
        /// 生成器每次调用都重新构造控件，不要复用同一份枚举结果。
        /// </remarks>
        private IEnumerable<Drawable> createLocalIdentityChildren()
        {
            localAccountName = ezConfig.GetBindable<string>(Ez2Setting.LocalAccountName);
            localAccountNameTakesOver = ezConfig.GetBindable<bool>(Ez2Setting.LocalAccountNameTakesOverScore);

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

        private void updateDropdownCurrent(UserStatus? status)
        {
            if (dropdown == null)
                return;

            switch (status)
            {
                case UserStatus.Online:
                    dropdown.Current.Value = UserAction.Online;
                    break;

                case UserStatus.DoNotDisturb:
                    dropdown.Current.Value = UserAction.DoNotDisturb;
                    break;

                case UserStatus.Offline:
                    dropdown.Current.Value = UserAction.AppearOffline;
                    break;
            }
        }

        public override bool AcceptsFocus => true;

        protected override bool OnClick(ClickEvent e) => true;

        protected override void OnFocus(FocusEvent e)
        {
            if (form != null) GetContainingFocusManager()!.ChangeFocus(form);
            base.OnFocus(e);
        }
    }
}
