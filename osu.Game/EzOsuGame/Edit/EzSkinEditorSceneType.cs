// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.EzOsuGame.Edit
{
    /// <summary>
    /// Ez 皮肤编辑器二级场景。尺寸与颜色设置已迁入 Appearance 侧栏五栏。
    /// 一级分组见 <see cref="EzSkinEditorSceneGroup"/>。
    /// </summary>
    public enum EzSkinEditorSceneType
    {
        SongSelectHud,
        BeatmapPreview,
        GameplayHud,
        MusicPlayer,
        Appearance,
        Note,
        SkinIni,
        SkinSpecific,

        /// <summary>结算屏（编辑结算 HUD 与卡片背景）。</summary>
        Results,
    }
}
