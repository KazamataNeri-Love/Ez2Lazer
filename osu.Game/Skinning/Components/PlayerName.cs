// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.EzOsuGame.Identity;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Screens.Play;

namespace osu.Game.Skinning.Components
{
    [UsedImplicitly]
    public partial class PlayerName : FontAdjustableSkinComponent
    {
        private readonly OsuSpriteText text;

        [Resolved]
        private GameplayState? gameplayState { get; set; }

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private Ez2ConfigManager ezConfig { get; set; } = null!;

        /// <summary>
        /// 非游玩场景下的生效身份名（本地账户名接管时即本地名）。必须持有字段：绑定副本是弱引用。
        /// </summary>
        private IBindable<string>? effectiveUsername;

        public PlayerName()
        {
            AutoSizeAxes = Axes.Both;

            InternalChildren = new Drawable[]
            {
                text = new OsuSpriteText
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            if (gameplayState != null)
                // 游玩中直接用成绩的署名：接管时 CreateScore 已写入本地账户名。
                text.Text = gameplayState.Score.ScoreInfo.User.Username;
            else
            {
                effectiveUsername = EzLocalIdentity.CreateUsernameBindable(ezConfig, api);
                effectiveUsername.BindValueChanged(u => text.Text = u.NewValue, true);
            }
        }

        protected override void SetFont(FontUsage font) => text.Font = font.With(size: 40);

        protected override void SetTextColour(Colour4 textColour) => text.Colour = textColour;
    }
}
