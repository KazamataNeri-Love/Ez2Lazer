// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Textures;
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
        private const double disjoint_trail_time_separation = 1000 / 60.0;

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

        private ScheduledDelegate? pendingSkinRefresh;

        private double lastTrailTime;
        private Vector2? currentPosition;

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

            lastTrailTime = 0;
            currentPosition = null;
        }

        protected override double FadeDuration => DisjointTrail ? 150 : 500;

        protected override float FadeExponent => 1;

        protected override bool InterpolateMovements => !DisjointTrail;

        protected override float IntervalMultiplier => 1 / Math.Max(cursorSize.Value, 1);

        protected override bool AvoidDrawingNearCursor => !DisjointTrail;

        protected override void Update()
        {
            base.Update();

            if (!DisjointTrail || !currentPosition.HasValue)
                return;

            if (Time.Current - lastTrailTime >= disjoint_trail_time_separation)
            {
                lastTrailTime = Time.Current;
                AddTrail(currentPosition.Value);
            }
        }

        protected override bool OnMouseMove(MouseMoveEvent e)
        {
            if (!DisjointTrail)
                return base.OnMouseMove(e);

            currentPosition = e.ScreenSpaceMousePosition;

            // intentionally block the base call as we're adding the trails ourselves.
            return false;
        }

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
