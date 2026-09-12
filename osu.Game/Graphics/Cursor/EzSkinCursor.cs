// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Graphics.Cursor
{
    /// <summary>
    /// Renders the current skin's cursor art (the same art shown by the osu! ruleset gameplay and preview cursors),
    /// so that it can be displayed as the global menu cursor.
    /// </summary>
    /// <remarks>
    /// This mirrors <c>osu.Game.Rulesets.Osu.Skinning.Legacy.LegacyCursor</c>, which osu.Game cannot reference directly.
    /// Sizing replicates <c>osu.Game.Rulesets.Osu.Skinning.NonPlayfieldSprite</c>'s use of
    /// <see cref="LegacySkin.STABLE_MAGIC_SCALE_FACTOR"/> so that the cursor is presented at the same size as in gameplay
    /// (the default menu cursor is laid out at approximately the same size, so enabling this does not change cursor size).
    /// </remarks>
    public partial class EzSkinCursor : SkinReloadableDrawable
    {
        private const double revolution_duration = 10000;

        private const float pressed_scale = 1.3f;
        private const float released_scale = 1f;

        private readonly Container spinContainer;

        private Sprite? cursorSprite;
        private Sprite? middleSprite;

        private bool expand = true;

        /// <summary>
        /// Whether the current skin provided a usable <c>cursor</c> texture.
        /// Consumers should fall back to the default cursor when this is <c>false</c>.
        /// </summary>
        public bool HasSkinCursor => cursorSprite != null;

        public EzSkinCursor()
        {
            InternalChild = spinContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
            };
        }

        protected override void SkinChanged(ISkinSource skin)
        {
            spinContainer.Clear();
            cursorSprite = null;
            middleSprite = null;

            // the cursor art and its middle piece must come from the same skin source (mirroring LegacyCursor).
            // if individual lookups were allowed to fall back across sources, a skin which intentionally has no
            // cursormiddle would incorrectly display another skin's middle.
            ISkin? provider = skin.FindProvider(s => s.GetTexture(@"cursor") != null);

            Texture? cursorTexture = provider?.GetTexture(@"cursor");

            if (provider == null || cursorTexture == null)
            {
                Size = Vector2.Zero;
                return;
            }

            // these are the same lookups the osu! ruleset uses for its cursor (see OsuSkinConfiguration).
            // reading them via a local enum is safe as LegacySkin looks config up by enum name.
            bool centre = provider.GetConfig<EzLegacySkinCursorSetting, bool>(EzLegacySkinCursorSetting.CursorCentre)?.Value ?? true;
            bool rotate = provider.GetConfig<EzLegacySkinCursorSetting, bool>(EzLegacySkinCursorSetting.CursorRotate)?.Value ?? true;

            expand = provider.GetConfig<EzLegacySkinCursorSetting, bool>(EzLegacySkinCursorSetting.CursorExpand)?.Value ?? true;

            Anchor spriteAnchor = centre ? Anchor.Centre : Anchor.TopLeft;

            spinContainer.Add(cursorSprite = createSprite(cursorTexture, spriteAnchor));

            Texture? middleTexture = provider.GetTexture(@"cursormiddle");

            if (middleTexture != null)
                spinContainer.Add(middleSprite = createSprite(middleTexture, spriteAnchor));

            Size = Vector2.ComponentMax(cursorSprite.Size, middleSprite?.Size ?? Vector2.Zero);

            // the art is anchored relative to the pointer: centred by default, or top-left anchored for skins which
            // request it (mirroring LegacyCursor's CursorCentre handling). this drawable is itself centred on the pointer.
            spinContainer.Position = centre ? Vector2.Zero : Size / 2;

            if (rotate)
                cursorSprite.Spin(revolution_duration, RotationDirection.Clockwise);
        }

        private static Sprite createSprite(Texture texture, Anchor anchor) => new Sprite
        {
            Texture = texture,
            // apply the stable scale factor manually rather than mutating the (shared, cached) texture's ScaleAdjust.
            Size = texture.DisplaySize / LegacySkin.STABLE_MAGIC_SCALE_FACTOR,
            Anchor = anchor,
            Origin = anchor,
        };

        /// <summary>
        /// Expands the cursor while a mouse button is held, mirroring the gameplay cursor.
        /// </summary>
        public void Expand()
        {
            if (!expand || cursorSprite == null)
                return;

            cursorSprite.ScaleTo(released_scale).ScaleTo(pressed_scale, 100, Easing.Out);
        }

        /// <summary>
        /// Contracts the cursor once all mouse buttons are released, mirroring the gameplay cursor.
        /// </summary>
        public void Contract() => cursorSprite?.ScaleTo(released_scale, 100, Easing.Out);
    }
}
