// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Screens;
using osu.Game.Graphics.Containers;
using osuTK;

namespace osu.Game.Screens
{
    public partial class OsuScreenStack : ScreenStack
    {
        [Cached]
        private BackgroundScreenStack backgroundScreenStack;

        private readonly ParallaxContainer parallaxContainer;

        /// <summary>
        /// Parallax host for <see cref="BackgroundScreenStack"/> — used when acrylic capture
        /// reparents the screen tree and needs to refresh nested background blur buffers.
        /// </summary>
        public Drawable BackgroundContent => parallaxContainer;

        /// <summary>
        /// Host for content drawn above the background stack but below every screen.
        /// Screens are added as children at push time, so anything added here beforehand stays underneath them.
        /// </summary>
        public Container UnderlayContent { get; }

        protected float ParallaxAmount => parallaxContainer.ParallaxAmount;

        public new MarginPadding Padding
        {
            get => base.Padding;
            set => base.Padding = value;
        }

        public OsuScreenStack()
        {
            InternalChildren = new Drawable[]
            {
                parallaxContainer = new ParallaxContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = backgroundScreenStack = new BackgroundScreenStack { RelativeSizeAxes = Axes.Both },
                },
                UnderlayContent = new UnderlayLayer { RelativeSizeAxes = Axes.Both },
            };

            ScreenPushed += screenPushed;
            ScreenExited += ScreenChanged;
        }

        private partial class UnderlayLayer : Container
        {
            // Purely decorative: never consume input intended for the screens above.
            public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => false;
        }

        public void PushSynchronously(OsuScreen screen)
        {
            LoadComponent(screen);

            Push(screen);
        }

        private void screenPushed(IScreen prev, IScreen next)
        {
            if (LoadState < LoadState.Ready)
            {
                // dependencies must be present to stay in a sane state.
                // this is generally only ever hit by test scenes.
                Schedule(() => screenPushed(prev, next));
                return;
            }

            // create dependencies synchronously to ensure leases are in a sane state.
            ((OsuScreen)next).CreateLeasedDependencies((prev as OsuScreen)?.Dependencies ?? Dependencies);

            ScreenChanged(prev, next);
        }

        protected virtual void ScreenChanged(IScreen prev, IScreen? next)
        {
            setParallax(next);
        }

        private void setParallax(IScreen? next) =>
            parallaxContainer.ParallaxAmount = ParallaxContainer.DEFAULT_PARALLAX_AMOUNT * ((next as IOsuScreen)?.BackgroundParallaxAmount ?? 1.0f);
    }
}
