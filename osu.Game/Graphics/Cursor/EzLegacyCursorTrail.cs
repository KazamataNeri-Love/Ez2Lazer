// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Threading;
using osu.Game.Configuration;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Graphics.Cursor
{
    /// <summary>
    /// A port of <c>osu.Game.Rulesets.Osu.Skinning.Legacy.LegacyCursorTrail</c>, used by <see cref="EzSkinCursor"/>
    /// to display the skin cursor trail in menus.
    /// </summary>
    /// <remarks>
    /// Unlike gameplay, this trail is not scaled by a playfield adjustment container, so <see cref="Drawable.Scale"/>
    /// is used to apply <see cref="LegacySkin.STABLE_MAGIC_SCALE_FACTOR"/> (the same factor
    /// <c>NonPlayfieldSprite</c> applies in gameplay) rather than mutating the shared texture's scale adjustment.
    /// </remarks>
    public partial class EzLegacyCursorTrail : EzCursorTrail
    {
        /// <summary>
        /// Whether the trail is drawn as disjoint stamps (skins which do not provide a cursor middle piece).
        /// </summary>
        public bool DisjointTrail { get; private set; }

        /// <summary>
        /// Whether a cursor trail texture could be resolved (either from the skin or the game default).
        /// </summary>
        public bool HasTrail { get; private set; }

        private ISkinSource skinSource = null!;
        private TextureStore textures = null!;
        private Bindable<float> cursorSize = null!;
        private InputManager? inputManager;

        private ScheduledDelegate? pendingSkinRefresh;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config, ISkinSource skinSource, TextureStore textures)
        {
            this.skinSource = skinSource;
            this.textures = textures;

            skinSource.SourceChanged += onSourceChanged;

            cursorSize = config.GetBindable<float>(OsuSetting.MenuCursorSize);
            cursorSize.BindValueChanged(s => CursorScale = new Vector2(s.NewValue), true);

            refreshSkin();
        }

        private void onSourceChanged()
        {
            // schedule required to avoid running after disposal (matching SkinReloadableDrawable).
            pendingSkinRefresh?.Cancel();
            pendingSkinRefresh = Scheduler.Add(refreshSkin);
        }

        private void refreshSkin()
        {
            // mirrors LegacyCursorTrail: the trail texture goes through normal source fallback (so the default
            // trail shows for skins which don't supply one), while disjoint-ness is decided by the source
            // which provides the cursor itself.
            Texture? trailTexture = skinSource.GetTexture(@"cursortrail") ?? textures.Get(@"Cursor/cursortrail");

            HasTrail = trailTexture != null;

            // never assign null: the draw node unconditionally binds the texture.
            if (trailTexture != null)
                Texture = trailTexture;

            bool centre = skinSource.GetConfig<EzLegacySkinCursorSetting, bool>(EzLegacySkinCursorSetting.CursorCentre)?.Value ?? true;

            DisjointTrail = skinSource.FindProvider(s => s.GetTexture(@"cursor") != null)?.GetTexture(@"cursormiddle") == null;

            AllowPartRotation = skinSource.GetConfig<EzLegacySkinCursorSetting, bool>(EzLegacySkinCursorSetting.CursorTrailRotate)?.Value ?? true;

            if (DisjointTrail)
            {
                TrailOrigin = centre ? Anchor.Centre : Anchor.TopLeft;
                Blending = BlendingParameters.Inherit;
            }
            else
            {
                Blending = BlendingParameters.Additive;
            }

            // see class remarks: keep the trail's size consistent with the cursor art (which is also scaled by this factor).
            Scale = Vector2.One / LegacySkin.STABLE_MAGIC_SCALE_FACTOR;

            ResetTrail();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            inputManager = GetContainingInputManager();
        }

        /// <remarks>
        /// [Ez] the trail follows every position it is fed (see <see cref="Update"/>) rather than being reduced
        /// to "relevant" input nodes, as its feed rate is already the display frame rate.
        /// </remarks>
        protected override bool ReduceInputPositions => false;

        protected override void Update()
        {
            base.Update();

            if (inputManager == null || !inputManager.CurrentState.Mouse.IsPositionValid)
                return;

            // [Ez] pieces are generated from the update loop rather than from mouse move events, so the trail's path
            // follows the frame rate rather than input event delivery.
            AddTrail(inputManager.CurrentState.Mouse.Position);
        }

        /// <summary>
        /// [Ez] the trail is fed from <see cref="Update"/> rather than from mouse move events.
        /// </summary>
        protected override bool OnMouseMove(MouseMoveEvent e) => false;

        protected override double FadeDuration => DisjointTrail ? 150 : 500;

        protected override float FadeExponent => 1;

        /// <remarks>
        /// [Ez] unlike gameplay, menu trails are always interpolated: pieces are spaced along the pointer path by
        /// distance instead of being stamped at a fixed rate. gameplay's disjoint mode stamps every 1000/60ms, which
        /// at higher pointer speeds leaves visible gaps between stamps; interpolating removes that speed dependency.
        /// </remarks>
        protected override bool InterpolateMovements => true;

        /// <summary>
        /// [Ez] Multiplier applied to the trail piece density relative to gameplay (4 = four times as many pieces).
        /// </summary>
        /// <remarks>
        /// Gameplay places pieces every <c>trailTextureWidth / 2.5</c> pixels (≈30px for a typical trail texture),
        /// which reads as a chain of discrete stamps with visible gaps between them. Menus space pieces four times
        /// as tightly so the trail reads as a continuous streak, independent of pointer speed and frame rate.
        /// </remarks>
        private const float piece_rate_multiplier = 4;

        protected override float IntervalMultiplier => 1f / (piece_rate_multiplier * Math.Max(cursorSize.Value, 1));

        /// <summary>
        /// [Ez] Upper bound on the distance between trail pieces, in pixels.
        /// </summary>
        /// <remarks>
        /// Pointer speeds vary wildly between frames, so spacing that scales purely with the trail texture can still
        /// leave gaps for skins whose cursor trail art is small or mostly transparent. Capping the spacing in pixels
        /// guarantees the trail reads as a continuous streak regardless of skin.
        /// </remarks>
        private const float maximum_piece_spacing = 6;

        protected override float PieceInterval => Math.Min(base.PieceInterval, maximum_piece_spacing);

        protected override bool AvoidDrawingNearCursor => !DisjointTrail;

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (skinSource != null)
                skinSource.SourceChanged -= onSourceChanged;

            pendingSkinRefresh = null;
        }
    }

    /// <summary>
    /// Mirrors the subset of <c>OsuSkinConfiguration</c> used by the cursor and cursor trail.
    /// <see cref="SkinConfiguration.ConfigDictionary"/> is keyed by enum member name, so these names must stay in sync.
    /// </summary>
    internal enum EzLegacySkinCursorSetting
    {
        CursorCentre,
        CursorExpand,
        CursorRotate,
        CursorTrailRotate,
    }
}
