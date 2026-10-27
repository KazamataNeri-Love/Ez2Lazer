// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Collections;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.EzOsuGame.Localization;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays.Music;
using osu.Game.Skinning;
using osuTK;
using Realms;

namespace osu.Game.EzOsuGame.Screens.MusicPlayer
{
    /// <summary>
    /// Hosts a <see cref="PlaylistOverlay"/> as a layout editor component, with an in-screen collection
    /// selector so the filter can be switched without opening the layout editor.
    /// </summary>
    public partial class EzMusicPlayerPlaylistPanel : CompositeDrawable, ISerialisableDrawable, IScopedSkinSettings
    {
        private const string all_collections = @"— 全部 / All —";
        private const float selector_height = 44;

        public bool UsesFixedAnchor { get; set; }

        [SettingSource(typeof(EzMusicStrings), nameof(EzMusicStrings.PLAYER_PLAYLIST_WIDTH), null, 0)]
        public BindableNumber<float> PanelWidth { get; } = new BindableNumber<float>(400)
        {
            MinValue = 160,
            MaxValue = 1200,
            Precision = 1,
        };

        [SettingSource(typeof(EzMusicStrings), nameof(EzMusicStrings.PLAYER_PLAYLIST_HEIGHT), null, 1)]
        public BindableNumber<float> PanelHeight { get; } = new BindableNumber<float>(PlaylistOverlay.PLAYLIST_HEIGHT)
        {
            MinValue = 200,
            MaxValue = 2000,
            Precision = 1,
        };

        [SettingSource(typeof(EzMusicStrings), nameof(EzMusicStrings.PLAYER_PLAYLIST_VISIBLE), null, 2)]
        public Bindable<bool> StartExpanded { get; } = new Bindable<bool>(true);

        [SettingSource(typeof(EzMusicStrings), nameof(EzMusicStrings.PLAYER_COLLECTION_SELECTOR_LABEL), nameof(EzMusicStrings.PLAYER_COLLECTION_SELECTOR_DESCRIPTION), 3)]
        public Bindable<bool> ShowSelector { get; } = new Bindable<bool>(true);

        /// <summary>
        /// Remembered collection selection. Serialised with the layout, but hidden from the editor because
        /// the value is changed through the in-screen selector instead of a text box.
        /// </summary>
        [SettingSource(typeof(EzMusicStrings), nameof(EzMusicStrings.PLAYER_COLLECTION_SELECTED_LABEL), null, 4)]
        public Bindable<string> SelectedCollection { get; } = new Bindable<string>(all_collections);

        [Resolved]
        private RealmAccess realm { get; set; } = null!;

        private PlaylistOverlay playlist = null!;
        private Container selectorContainer = null!;
        private OsuDropdown<string> selector = null!;
        private IDisposable? collectionSubscription;

        public EzMusicPlayerPlaylistPanel()
        {
            Size = new Vector2(400, PlaylistOverlay.PLAYLIST_HEIGHT);
            Masking = true;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            // OsuDropdown auto-sizes vertically, so only the horizontal axis may be relative.
            selector = new OsuDropdown<string>
            {
                Current = SelectedCollection,
                RelativeSizeAxes = Axes.X,
            };

            InternalChildren = new Drawable[]
            {
                playlist = new PlaylistOverlay
                {
                    RelativeSizeAxes = Axes.Both,
                },
                selectorContainer = new Container
                {
                    RelativeSizeAxes = Axes.X,
                    Height = selector_height,
                    Padding = new MarginPadding { Horizontal = 10, Vertical = 2 },
                    Child = selector,
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            PanelWidth.BindValueChanged(w => Size = new Vector2(w.NewValue, Size.Y));
            PanelHeight.BindValueChanged(h => Size = new Vector2(Size.X, h.NewValue));

            // Applies both at load and when toggled from the layout editor.
            StartExpanded.BindValueChanged(v =>
            {
                if (v.NewValue)
                    playlist.Show();
                else
                    playlist.Hide();
            }, true);

            ShowSelector.BindValueChanged(show =>
            {
                selectorContainer.Alpha = show.NewValue ? 1 : 0;
                selectorContainer.Height = show.NewValue ? selector_height : 0;
                playlist.Padding = new MarginPadding { Top = show.NewValue ? selector_height : 0 };
            }, true);

            SelectedCollection.BindValueChanged(c => playlist.CollectionFilter.Value = c.NewValue == all_collections ? null : c.NewValue, true);

            rebuildCollectionItems();
            collectionSubscription = realm.RegisterForNotifications(r => r.All<BeatmapCollection>(), (_, _) => Schedule(rebuildCollectionItems));
        }

        public IBindable<Visibility> State => playlist.State;

        public void Toggle() => playlist.ToggleVisibility();

        private void rebuildCollectionItems()
        {
            var items = new List<string> { all_collections };

            items.AddRange(realm.Run(r => r.All<BeatmapCollection>()
                                            .AsEnumerable()
                                            .Select(c => c.Name)
                                            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                                            .ToList()));

            selector.Items = items;

            if (SelectedCollection.Value != all_collections && !items.Contains(SelectedCollection.Value))
                SelectedCollection.Value = all_collections;
        }

        public void WatchSettingsScope(Action onChange) => ShowSelector.BindValueChanged(_ => onChange());

        public bool IsSettingActive(PropertyInfo property) => property.Name != nameof(SelectedCollection);

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos)
            => playlist.State.Value == Visibility.Visible && base.ReceivePositionalInputAt(screenSpacePos);

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            collectionSubscription?.Dispose();
        }
    }
}
