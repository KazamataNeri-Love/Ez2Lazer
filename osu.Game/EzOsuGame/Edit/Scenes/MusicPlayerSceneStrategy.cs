// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Localisation;
using osu.Game.EzOsuGame.Edit.Components;
using osu.Game.EzOsuGame.Localization;

namespace osu.Game.EzOsuGame.Edit.Scenes
{
    /// <summary>
    /// 听歌界面场景：以真实的 EzMusicPlayerScreen 为宿主，编辑 MusicPlayer 布局容器。
    /// </summary>
    public class MusicPlayerSceneStrategy : IEzSkinEditorSceneStrategy
    {
        private EzSkinEditorSettingsGroup[]? sections;

        public EzEditorSceneType SceneType => EzEditorSceneType.MusicPlayer;

        public EzEditorSceneGroup Group => EzEditorSceneGroup.SongSelect;

        public LocalisableString TabTitle => EzEditorStrings.SCENE_MUSIC_PLAYER;

        public bool SupportsHudLayer => true;

        public bool HoverExpandsSidebar => false;

        public void ReleaseCachedDrawables() => sections = null;

        public IReadOnlyList<EzSkinEditorSettingsGroup> Sections => sections ??=
            new[] { new EzSkinEditorHudWorkspaceSection() }
                .Concat(EzSkinEditorComponentLibrarySections.Create(rulesetScoped: false))
                .ToArray();
    }
}
