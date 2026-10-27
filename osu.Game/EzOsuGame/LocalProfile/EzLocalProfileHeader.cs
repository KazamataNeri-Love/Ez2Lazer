// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.EzOsuGame.Identity;
using osu.Game.EzOsuGame.Localization;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Overlays.Profile.Header.Components;
using osu.Game.Users.Drawables;
using osuTK;

namespace osu.Game.EzOsuGame.LocalProfile
{
    public partial class EzLocalProfileHeader : OverlayHeader
    {
        private const float avatar_size = 70;

        public Action? OpenAccount { get; set; }

        private UpdateableAvatar avatar = null!;
        private OsuSpriteText usernameText = null!;
        private OsuTextFlowContainer metaText = null!;
        private Box contentBackground = null!;
        private Box coverBackground = null!;

        protected override OverlayTitle CreateTitle() => new HeaderTitle();

        protected override Drawable CreateBackground() => coverBackground = new Box
        {
            RelativeSizeAxes = Axes.X,
            Height = 60,
        };

        protected override Drawable CreateContent()
        {
            return new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Children = new Drawable[]
                {
                    contentBackground = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Padding = new MarginPadding
                        {
                            Horizontal = WaveOverlayContainer.HORIZONTAL_PADDING,
                            Vertical = 12
                        },
                        Children = new Drawable[]
                        {
                            new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(16, 0),
                                Padding = new MarginPadding { Right = 140 },
                                Children = new Drawable[]
                                {
                                    new Container
                                    {
                                        Size = new Vector2(avatar_size),
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Masking = true,
                                        CornerRadius = 8,
                                        EdgeEffect = new EdgeEffectParameters
                                        {
                                            Type = EdgeEffectType.Shadow,
                                            Radius = 6,
                                            Colour = Colour4.Black.Opacity(0.35f),
                                        },
                                        Child = avatar = new UpdateableAvatar(isInteractive: false)
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                        }
                                    },
                                    new FillFlowContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Direction = FillDirection.Vertical,
                                        Spacing = new Vector2(0, 6),
                                        Children = new Drawable[]
                                        {
                                            new FillFlowContainer
                                            {
                                                AutoSizeAxes = Axes.Both,
                                                Direction = FillDirection.Horizontal,
                                                Spacing = new Vector2(10, 0),
                                                Children = new Drawable[]
                                                {
                                                    usernameText = new OsuSpriteText
                                                    {
                                                        Anchor = Anchor.CentreLeft,
                                                        Origin = Anchor.CentreLeft,
                                                        Font = OsuFont.GetFont(size: 24, weight: FontWeight.Bold),
                                                    },
                                                    new LocalBadge
                                                    {
                                                        Anchor = Anchor.CentreLeft,
                                                        Origin = Anchor.CentreLeft,
                                                    }
                                                }
                                            },
                                            metaText = new OsuTextFlowContainer(t => t.Font = OsuFont.GetFont(size: 12))
                                            {
                                                RelativeSizeAxes = Axes.X,
                                                AutoSizeAxes = Axes.Y,
                                            }
                                        }
                                    },
                                }
                            },
                            new AccountHeaderButton
                            {
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                Action = () => OpenAccount?.Invoke(),
                            }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// 当前生效的身份名（本地账户名接管时即本地名）。必须持有：框架的绑定副本是弱引用。
        /// </summary>
        private IBindable<string> effectiveUsername = null!;

        /// <summary>当前生效身份的用户对象（头像据此查本地头像目录）。必须持有：同上。</summary>
        private IBindable<APIUser> effectiveUser = null!;

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colours, IAPIProvider api, Ez2ConfigManager ezConfig)
        {
            coverBackground.Colour = colours.Background3;
            contentBackground.Colour = colours.Background4;
            metaText.Colour = colours.Content2;

            // 头像按「当前生效身份」取名：DrawableAvatar 会优先查 EzResources/Modify/avatars/{用户名}/，
            // 命中就用本地头像，否则才回退在线头像。
            effectiveUser = EzLocalIdentity.CreateUserBindable(ezConfig, api);
            effectiveUser.BindValueChanged(u => avatar.User = u.NewValue, true);

            effectiveUsername = EzLocalIdentity.CreateUsernameBindable(ezConfig, api);
            effectiveUsername.BindValueChanged(u => UpdateUsername(u.NewValue), true);
        }

        public void UpdateUsername(string username)
        {
            usernameText.Text = string.IsNullOrEmpty(username)
                ? EzSettingsProfile.LOCAL_PROFILE_TITLE
                : username;
        }

        public void UpdateMeta(EzLocalProfileSnapshot snapshot)
        {
            if (!snapshot.HasData || snapshot.LastComputedAt == null)
            {
                metaText.Text = EzSettingsProfile.LOCAL_PROFILE_SHARED_HINT.ToString();
                return;
            }

            string names = snapshot.IncludedUsernames.Count == 0
                ? "-"
                : string.Join(", ", snapshot.IncludedUsernames
                                            .Select(EzLocalProfileConstants.NormaliseUsername)
                                            .Distinct(StringComparer.Ordinal));

            string baseText = $"{EzSettingsProfile.LOCAL_PROFILE_SHARED_HINT} · {snapshot.LastComputedAt.Value.LocalDateTime:g} · {names}";

            if (snapshot.NeedsRecompute)
                metaText.Text = $"{baseText} · {EzSettingsProfile.LOCAL_PROFILE_NEEDS_RECOMPUTE}";
            else
                metaText.Text = baseText;
        }

        private partial class HeaderTitle : OverlayTitle
        {
            public HeaderTitle()
            {
                Title = EzSettingsProfile.LOCAL_PROFILE_TITLE;
                Description = EzSettingsProfile.LOCAL_PROFILE_DESCRIPTION;
                Icon = FontAwesome.Solid.User;
            }
        }

        private partial class LocalBadge : CircularContainer
        {
            [BackgroundDependencyLoader]
            private void load(OverlayColourProvider colours)
            {
                AutoSizeAxes = Axes.Both;
                Masking = true;
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colours.Background6,
                    },
                    new OsuSpriteText
                    {
                        Text = EzSettingsProfile.LOCAL_PROFILE_BADGE,
                        Font = OsuFont.GetFont(size: 11, weight: FontWeight.Bold),
                        Colour = colours.Foreground1,
                        Margin = new MarginPadding { Horizontal = 8, Vertical = 3 },
                    }
                };
            }
        }

        private partial class AccountHeaderButton : ProfileHeaderButton
        {
            public AccountHeaderButton()
            {
                Child = new OsuSpriteText
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Text = EzSettingsProfile.LOCAL_PROFILE_ACCOUNT_BUTTON,
                    Font = OsuFont.GetFont(size: 12, weight: FontWeight.Bold),
                };
            }
        }
    }
}
