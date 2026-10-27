// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Game.EzOsuGame.HUD;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.EzOsuGame.Screens.MusicPlayer
{
    /// <summary>
    /// Built-in default layout for <see cref="GlobalSkinnableContainers.MusicPlayer"/>, used when no
    /// user layout file exists so the player is usable before any editing takes place.
    /// </summary>
    public static class EzMusicPlayerLayoutDefaults
    {
        public static SkinLayoutInfo Create()
        {
            var layout = new SkinLayoutInfo();

            layout.Update(null, new[]
            {
                new SerialisedDrawableInfo
                {
                    Type = typeof(EzMusicPlayerInfo),
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    Position = new Vector2(-60, 60),
                    Width = 420,
                    UsesFixedAnchor = true,
                },
                new SerialisedDrawableInfo
                {
                    Type = typeof(EzMusicPlayerCover),
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Position = new Vector2(0, -40),
                    Width = 350,
                    Height = 350,
                    UsesFixedAnchor = true,
                },
                new SerialisedDrawableInfo
                {
                    Type = typeof(EzMusicPlayerControls),
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Position = new Vector2(0, 250),
                    UsesFixedAnchor = true,
                },
                new SerialisedDrawableInfo
                {
                    Type = typeof(EzAudioVisualiser),
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    Position = new Vector2(0, -80),
                    UsesFixedAnchor = true,
                },
                new SerialisedDrawableInfo
                {
                    Type = typeof(EzAudioVisualiserScale),
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    Position = new Vector2(0, -24),
                    UsesFixedAnchor = true,
                },
                new SerialisedDrawableInfo
                {
                    Type = typeof(EzMusicPlayerPlaylistPanel),
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Position = new Vector2(-24, 0),
                    UsesFixedAnchor = true,
                },
            });

            return layout;
        }
    }
}
