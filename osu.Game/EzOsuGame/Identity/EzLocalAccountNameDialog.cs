// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.EzOsuGame.Localization;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays.Dialog;
using osuTK;

namespace osu.Game.EzOsuGame.Identity
{
    /// <summary>
    /// 新增 / 修改本地账户名的对话框。
    /// </summary>
    /// <remarks>
    /// 布局照 <c>BeatmapLeaderboardScore.RenamePlayerDialog</c>（同样是 <see cref="PopupDialog"/> + 文本输入）。
    /// </remarks>
    internal partial class EzLocalAccountNameDialog : PopupDialog
    {
        private readonly FocusedTextBox textBox;

        public EzLocalAccountNameDialog(string initial, Action<string> apply)
        {
            HeaderText = EzSettingsStrings.LOCAL_ACCOUNT_NAME_DIALOG_HEADER;
            BodyText = EzSettingsStrings.LOCAL_ACCOUNT_NAME_DIALOG_BODY;
            Icon = FontAwesome.Solid.User;

            textBox = new FocusedTextBox
            {
                PlaceholderText = EzSettingsStrings.LOCAL_ACCOUNT_NAME_DIALOG_PLACEHOLDER,
                Text = initial,
                RelativeSizeAxes = Axes.X,
                FontSize = OsuFont.DEFAULT_FONT_SIZE,
                SelectAllOnFocus = true,
                TabbableContentContainer = this,
            };

            MainContent.Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(10),
                Padding = new MarginPadding { Horizontal = 15 },
                Children = new Drawable[]
                {
                    textBox,
                }
            };

            Buttons = new PopupDialogButton[]
            {
                new PopupDialogOkButton
                {
                    Text = osu.Game.Resources.Localisation.Web.CommonStrings.ButtonsSave,
                    Action = () => apply(textBox.Text.Trim()),
                },
                new PopupDialogCancelButton(),
            };

            textBox.OnCommit += (_, _) => PerformOkAction();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            ScheduleAfterChildren(() =>
            {
                GetContainingFocusManager()?.ChangeFocus(textBox);
                textBox.SelectAll();
            });
        }
    }
}
