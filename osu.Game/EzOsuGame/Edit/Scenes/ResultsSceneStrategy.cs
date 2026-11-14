// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Localisation;
using osu.Game.EzOsuGame.Edit.Components;
using osu.Game.EzOsuGame.Localization;
using osu.Game.EzOsuGame.Screens;

namespace osu.Game.EzOsuGame.Edit.Scenes
{
    /// <summary>
    /// 结算场景：在结算屏打开编辑器时生效，编辑结算 HUD / 背景两个布局层，
    /// 侧栏提供结算卡片背景配置。
    /// </summary>
    public class ResultsSceneStrategy : IEzSkinEditorSceneStrategy
    {
        private EzSkinEditorSettingsGroup[]? sections;

        public EzEditorSceneType SceneType => EzEditorSceneType.Results;

        public EzEditorSceneGroup Group => EzEditorSceneGroup.Gameplay;

        public LocalisableString TabTitle => EzEditorStrings.TAB_RESULTS;

        public bool SupportsHudLayer => true;

        public void ReleaseCachedDrawables() => sections = null;

        public IReadOnlyList<EzSkinEditorSettingsGroup> Sections => sections ??= new EzSkinEditorSettingsGroup[]
        {
            new EzSkinEditorResultsCardBackgroundSection(),
        };
    }

    /// <summary>
    /// 把已有的结算卡片背景配置页包成编辑器侧栏分组。
    /// </summary>
    public partial class EzSkinEditorResultsCardBackgroundSection : EzSkinEditorSettingsGroup
    {
        public EzSkinEditorResultsCardBackgroundSection()
            : base(EzEditorStrings.GROUP_RESULTS_CARD_BACKGROUND)
        {
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Add(new EzEditorSectionResultsSidebar());
        }
    }
}
