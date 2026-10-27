// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Logging;
using osu.Game.Configuration;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.EzOsuGame.Localization;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Overlays.Dialog;
using osu.Game.Overlays.Notifications;
using osu.Game.Overlays.Settings;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.EzOsuGame.Presets
{
    /// <summary>
    /// 「设置预设方案」控件：下拉选择（选中即应用）+ 新建 / 重命名 / 删除 + 全宽「使用当前的设置」。
    /// </summary>
    /// <remarks>
    /// 挂在「常规 → 快捷操作」下，布局对齐设置里的「皮肤」小节（下拉 + 三等宽按钮 + 全宽大按钮）。
    /// 工作流对齐 Mod 个人预设：「重命名」打开编辑弹窗（改名 + 内容预览），
    /// 「使用当前的设置」把当前状态整个写回选中的预设。
    /// <para>
    /// 下拉的 <c>Items</c> 是动态列表，framework 的 <c>Dropdown&lt;T&gt;</c> 会在列表变化时自动把
    /// <c>Current</c> 修正到第一个可用项（<c>Dropdown.cs</c> 的 <c>ensureItemSelectionIsValid</c>），
    /// 该修正同样会触发 <c>ValueChanged</c>，所以刷新列表期间要用 <see cref="suppressApplyDepth"/> 屏蔽应用，
    /// 避免打开设置面板或删除预设时被动套用。
    /// </para>
    /// </remarks>
    internal partial class EzSettingsPresetControl : CompositeDrawable, IHasPopover
    {
        private readonly EzSettingsPresetStore store;

        private FormDropdown<EzSettingsPresetDocument?> presetDropdown = null!;
        private SettingsButtonV2 renameButton = null!;
        private SettingsButtonV2 useCurrentButton = null!;
        private DangerousSettingsButtonV2 deleteButton = null!;
        private OsuSpriteText emptyHint = null!;

        /// <summary>
        /// 大于 0 时忽略下拉的选中变化（刷新列表、程序化选中时使用）。
        /// </summary>
        private int suppressApplyDepth;

        [Resolved]
        private Ez2ConfigManager ezConfig { get; set; } = null!;

        [Resolved]
        private OsuConfigManager osuConfig { get; set; } = null!;

        [Resolved]
        private SkinManager skins { get; set; } = null!;

        [Resolved(CanBeNull = true)]
        private IDialogOverlay? dialogOverlay { get; set; }

        [Resolved(CanBeNull = true)]
        private INotificationOverlay? notifications { get; set; }

        public EzSettingsPresetControl(EzSettingsPresetStore store)
        {
            this.store = store;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            SettingsButtonV2 newButton;

            InternalChild = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, SettingsSection.ITEM_SPACING_V2),
                Children = new Drawable[]
                {
                    new SettingsItemV2(presetDropdown = new FormDropdown<EzSettingsPresetDocument?>
                    {
                        Caption = EzSettingsStrings.SETTINGS_PRESET_CAPTION,
                        HintText = EzSettingsStrings.SETTINGS_PRESET_HINT,
                        MaxHeight = 300,
                    })
                    {
                        // 预设不参与「恢复默认」语义：复位按钮针对的是单个 bindable，与预设无关。
                        ShowRevertToDefaultButton = false,
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Horizontal,
                        Padding = SettingsPanel.CONTENT_PADDING,
                        Children = new Drawable[]
                        {
                            // 照 Settings/Sections/SkinSection.cs 的写法：按钮自己带 1/3 相对宽度。
                            // 外包一层 Container 会让 AutoSize 与相对宽度互相干扰，出现按钮不等宽。
                            // Padding 在这里只用于让按钮背景内缩出缝隙，垂直留白由外层 FillFlow 的 Padding 提供。
                            newButton = new SettingsButtonV2
                            {
                                Text = EzSettingsStrings.SETTINGS_PRESET_NEW,
                                TooltipText = EzSettingsStrings.SETTINGS_PRESET_NEW_TOOLTIP,
                                RelativeSizeAxes = Axes.X,
                                Width = 1 / 3f,
                                Padding = new MarginPadding { Right = 2.5f },
                                Action = createPreset,
                            },
                            renameButton = new SettingsButtonV2
                            {
                                Text = EzSettingsStrings.SETTINGS_PRESET_RENAME,
                                TooltipText = EzSettingsStrings.SETTINGS_PRESET_RENAME_TOOLTIP,
                                RelativeSizeAxes = Axes.X,
                                Width = 1 / 3f,
                                Padding = new MarginPadding { Horizontal = 2.5f },
                                Action = () => this.ShowPopover(),
                            },
                            deleteButton = new DangerousSettingsButtonV2
                            {
                                Text = EzSettingsStrings.SETTINGS_PRESET_DELETE,
                                RelativeSizeAxes = Axes.X,
                                Width = 1 / 3f,
                                Padding = new MarginPadding { Left = 2.5f },
                                Action = deletePreset,
                            },
                        }
                    },
                    useCurrentButton = new SettingsButtonV2
                    {
                        Text = EzSettingsStrings.SETTINGS_PRESET_USE_CURRENT,
                        TooltipText = EzSettingsStrings.SETTINGS_PRESET_USE_CURRENT_TOOLTIP,
                        Action = useCurrentSettings,
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Padding = SettingsPanel.CONTENT_PADDING,
                        Child = emptyHint = new OsuSpriteText
                        {
                            Text = EzSettingsStrings.SETTINGS_PRESET_EMPTY,
                            Font = OsuFont.GetFont(size: 13),
                            Alpha = 0,
                        }
                    },
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            presetDropdown.Current.BindValueChanged(onPresetSelected);
            store.Presets.BindCollectionChanged((_, _) => refreshList(), true);
        }

        /// <summary>
        /// 列表变化时重建下拉项。framework 会顺带把 <c>Current</c> 修正到可用项，期间屏蔽应用。
        /// </summary>
        private void refreshList() => withSuppressedApply(() =>
        {
            presetDropdown.Items = store.Presets.ToArray();
            updateButtons();
        });

        private void onPresetSelected(ValueChangedEvent<EzSettingsPresetDocument?> change)
        {
            updateButtons();

            if (suppressApplyDepth > 0 || change.NewValue == null)
                return;

            applyPreset(change.NewValue);
        }

        private void createPreset() => withSuppressedApply(() =>
        {
            string name = store.NextDefaultName(index => EzSettingsStrings.SETTINGS_PRESET_DEFAULT_NAME.Format(index));

            // 用当前设置直接作为新预设的内容，省掉「新建后再点一次使用当前的设置」。
            var document = EzSettingsPresetBridge.Capture(ezConfig, osuConfig, skins, name);

            store.Save(document);

            presetDropdown.Current.Value = document;
        });

        /// <summary>
        /// 把当前皮肤与三组设置整个写回选中的预设。
        /// </summary>
        private void useCurrentSettings()
        {
            var current = presetDropdown.Current.Value;

            if (current == null)
                return;

            dialogOverlay?.Push(new ConfirmDialog(
                EzSettingsStrings.SETTINGS_PRESET_USE_CURRENT_CONFIRM.Format(current.Name),
                () => overwriteWithCurrent(current)));
        }

        private void overwriteWithCurrent(EzSettingsPresetDocument preset)
        {
            var captured = EzSettingsPresetBridge.Capture(ezConfig, osuConfig, skins, preset.Name, preset.Id);

            // 就地改写原对象，让下拉的 Current 引用保持有效。
            preset.SchemaVersion = captured.SchemaVersion;
            preset.SkinId = captured.SkinId;
            preset.SkinName = captured.SkinName;
            preset.Settings = captured.Settings;

            try
            {
                store.Save(preset);
            }
            catch (Exception e)
            {
                notifications?.Post(new SimpleNotification
                {
                    Text = $"{preset.Name}: {e.Message}",
                });
                return;
            }

            notifications?.Post(new SimpleNotification
            {
                Text = EzSettingsStrings.SETTINGS_PRESET_SAVED.Format(preset.Name),
            });
        }

        private void deletePreset()
        {
            var current = presetDropdown.Current.Value;

            if (current == null)
                return;

            dialogOverlay?.Push(new DeleteSettingsPresetDialog(current, () =>
            {
                store.Delete(current);

                notifications?.Post(new SimpleNotification
                {
                    Text = EzSettingsStrings.SETTINGS_PRESET_DELETED.Format(current.Name),
                });
            }));
        }

        private void applyPreset(EzSettingsPresetDocument document)
        {
            var report = EzSettingsPresetBridge.Apply(document, ezConfig, osuConfig, skins);

            foreach (string failure in report.Failed)
                Logger.Log($"[Ez] 设置预设「{document.Name}」写入失败：{failure}", Ez2ConfigManager.LOGGER_NAME, LogLevel.Important);

            if (report.Skipped.Count > 0)
                Logger.Log($"[Ez] 设置预设「{document.Name}」跳过 {report.Skipped.Count} 项（枚举项已删除或未注册）。", Ez2ConfigManager.LOGGER_NAME, LogLevel.Debug);

            if (report.Failed.Count > 0 || report.Skipped.Count > 0)
            {
                notifications?.Post(new SimpleNotification
                {
                    Text = EzSettingsStrings.SETTINGS_PRESET_APPLIED_PARTIAL.Format(document.Name, report.Applied, report.Failed.Count + report.Skipped.Count),
                });
            }
            else if (report.SkinMissing)
            {
                notifications?.Post(new SimpleNotification
                {
                    Text = EzSettingsStrings.SETTINGS_PRESET_SKIN_MISSING.Format(document.SkinName ?? string.Empty),
                });
            }
            else
            {
                notifications?.Post(new SimpleNotification
                {
                    Text = EzSettingsStrings.SETTINGS_PRESET_APPLIED.Format(document.Name, report.Applied),
                });
            }
        }

        private void updateButtons()
        {
            bool hasSelection = presetDropdown.Current.Value != null;

            renameButton.Enabled.Value = hasSelection;
            useCurrentButton.Enabled.Value = hasSelection;
            deleteButton.Enabled.Value = hasSelection;
            emptyHint.Alpha = store.Presets.Count == 0 ? 1 : 0;
        }

        public Popover GetPopover()
        {
            var current = presetDropdown.Current.Value
                          ?? throw new InvalidOperationException("没有选中任何预设。");

            // 改名只修改对象内容，下拉的 Current 引用不变；必须显式重建列表才会重算显示文本。
            return new EditSettingsPresetPopover(store, current, refreshList);
        }

        private void withSuppressedApply(Action action)
        {
            suppressApplyDepth++;

            try
            {
                action();
            }
            finally
            {
                suppressApplyDepth--;
            }
        }
    }

    /// <summary>
    /// 删除预设的确认框。
    /// </summary>
    internal partial class DeleteSettingsPresetDialog : DeletionDialog
    {
        public DeleteSettingsPresetDialog(EzSettingsPresetDocument preset, Action delete)
        {
            BodyText = EzSettingsStrings.SETTINGS_PRESET_DELETE_CONFIRM.Format(preset.Name);
            DangerousAction = delete;
        }
    }
}
