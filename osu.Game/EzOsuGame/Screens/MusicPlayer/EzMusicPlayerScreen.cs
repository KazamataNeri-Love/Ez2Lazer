// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Screens;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Input.Bindings;
using osu.Game.Screens.Footer;
using osu.Game.Screens.Play;
using osuTK.Graphics;

namespace osu.Game.EzOsuGame.Screens.MusicPlayer
{
    /// <summary>
    /// Music player screen whose contents are entirely provided by the Ez layout container
    /// (<see cref="GlobalSkinnableContainers.MusicPlayer"/>), so every element is editable in the layout editor.
    /// </summary>
    public partial class EzMusicPlayerScreen : ScreenWithBeatmapBackground, IKeyBindingHandler<GlobalAction>
    {
        public override bool HideOverlaysOnEnter => true;

        public override bool? AllowGlobalTrackControl => true;

        public EzMusicPlayerScreen()
        {
            FooterContent.Value = ScreenFooterContent.Legacy(showBackButton: true);
        }

        private readonly Bindable<WorkingBeatmap> beatmap = new Bindable<WorkingBeatmap>();

        protected override void LoadComplete()
        {
            base.LoadComplete();

            beatmap.BindTo(Beatmap);
            beatmap.BindValueChanged(b =>
            {
                if (!this.IsCurrentScreen()) return;

                ApplyToBackground(bg => bg.Beatmap = b.NewValue);
            });
        }

        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);

            ApplyToBackground(b =>
            {
                b.Beatmap = Beatmap.Value;
                b.IgnoreUserSettings.Value = false;
                b.DimWhenUserSettingsIgnored.Value = 0.3f;
                b.FadeColour(OsuColour.Gray(0.4f), 250, Easing.OutQuint);
            });

            this.FadeInFromZero(400, Easing.OutQuint);
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
            this.FadeOut(250, Easing.OutQuint);

            ApplyToBackground(b => b.FadeColour(Color4.White, 250, Easing.OutQuint));

            return base.OnExiting(e);
        }

        public bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
        {
            if (e.Action == GlobalAction.Back)
            {
                this.Exit();
                return true;
            }

            return false;
        }

        public void OnReleased(KeyBindingReleaseEvent<GlobalAction> e)
        {
        }
    }
}
