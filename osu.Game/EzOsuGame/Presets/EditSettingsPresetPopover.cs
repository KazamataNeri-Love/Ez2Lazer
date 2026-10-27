// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.EzOsuGame.Localization;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Input.Bindings;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.EzOsuGame.Presets
{
    /// <summary>
    /// 设置预设的编辑弹窗。工作流对齐「Mod 功能 → 个人预设 → 右键预设卡 → 编辑」：
    /// 改名字 + 「使用当前的设置」+ 内容预览 + 保存。
    /// </summary>
    /// <remarks>
    /// 编辑期作用在副本上，关闭弹窗不保存即丢弃改动；只有「保存」才写回磁盘。
    /// </remarks>
    internal partial class EditSettingsPresetPopover : OsuPopover
    {
        private const float content_width = 360;

        private readonly EzSettingsPresetDocument preset;
        private readonly EzSettingsPresetStore store;

        /// <summary>
        /// 保存成功后的回调（控件用它重建下拉列表，否则改名不会反映到下拉显示上）。
        /// </summary>
        private readonly Action? onSaved;

        /// <summary>
        /// 编辑副本。名字以外的内容改动都先落在这里。
        /// </summary>
        private EzSettingsPresetDocument working;

        private LabelledTextBox nameTextBox = null!;
        private ShearedButton useCurrentButton = null!;
        private ShearedButton saveButton = null!;
        private FillFlowContainer previewContent = null!;

        [Resolved]
        private Ez2ConfigManager ezConfig { get; set; } = null!;

        [Resolved]
        private OsuConfigManager osuConfig { get; set; } = null!;

        [Resolved]
        private SkinManager skins { get; set; } = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [Resolved(CanBeNull = true)]
        private INotificationOverlay? notifications { get; set; }

        public EditSettingsPresetPopover(EzSettingsPresetStore store, EzSettingsPresetDocument preset, Action? onSaved = null)
        {
            this.store = store;
            this.preset = preset;
            this.onSaved = onSaved;

            working = preset.Clone();
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Child = new FillFlowContainer
            {
                Width = content_width,
                AutoSizeAxes = Axes.Y,
                Spacing = new Vector2(7),
                Direction = FillDirection.Vertical,
                Children = new Drawable[]
                {
                    nameTextBox = new LabelledTextBox
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Label = osu.Game.Localisation.CommonStrings.Name,
                        TabbableContentContainer = this,
                        Current = { Value = preset.Name },
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 140,
                        CornerRadius = 10,
                        Masking = true,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = colourProvider.Background5,
                            },
                            new OsuScrollContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                Padding = new MarginPadding(7),
                                Child = previewContent = new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Padding = new MarginPadding(7),
                                    Spacing = new Vector2(4),
                                    Direction = FillDirection.Vertical,
                                }
                            },
                        }
                    },
                    new FillFlowContainer
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        AutoSizeAxes = Axes.Both,
                        Spacing = new Vector2(7),
                        Direction = FillDirection.Vertical,
                        Children = new Drawable[]
                        {
                            useCurrentButton = new ShearedButton
                            {
                                // 与 EditPresetPopover 同样，锚到 TopRight 才能让 fill flow 正确按 AABB 定尺寸。
                                Anchor = Anchor.TopRight,
                                Origin = Anchor.TopRight,
                                Width = content_width,
                                Text = EzSettingsStrings.SETTINGS_PRESET_USE_CURRENT,
                                TooltipText = EzSettingsStrings.SETTINGS_PRESET_USE_CURRENT_TOOLTIP,
                                DarkerColour = colours.Blue1,
                                LighterColour = colours.Blue0,
                                TextColour = colourProvider.Background6,
                                Action = useCurrentSettings,
                            },
                            saveButton = new ShearedButton
                            {
                                Anchor = Anchor.TopRight,
                                Origin = Anchor.TopRight,
                                Width = content_width,
                                Text = osu.Game.Resources.Localisation.Web.CommonStrings.ButtonsSave,
                                DarkerColour = colours.Orange1,
                                LighterColour = colours.Orange0,
                                TextColour = colourProvider.Background6,
                                Action = save,
                            },
                        }
                    }
                }
            };

            Body.BorderThickness = 3;
            Body.BorderColour = colours.Orange1;

            nameTextBox.Current.BindValueChanged(s =>
            {
                saveButton.Enabled.Value = !string.IsNullOrWhiteSpace(s.NewValue);
            }, true);

            updatePreview();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            ScheduleAfterChildren(() => GetContainingFocusManager()!.ChangeFocus(nameTextBox));
        }

        public override bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
        {
            switch (e.Action)
            {
                case GlobalAction.Select:
                    saveButton.TriggerClick();
                    return true;
            }

            return base.OnPressed(e);
        }

        /// <summary>
        /// 「使用当前的设置」：把当前皮肤与三组设置抓成新内容（保留 <see cref="EzSettingsPresetDocument.Id"/>）。
        /// </summary>
        private void useCurrentSettings()
        {
            working = EzSettingsPresetBridge.Capture(ezConfig, osuConfig, skins, nameTextBox.Current.Value, preset.Id);
            updatePreview();
            flashFilled();
        }

        private void save()
        {
            string name = nameTextBox.Current.Value.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                notifications?.Post(new SimpleNotification
                {
                    Text = EzSettingsStrings.SETTINGS_PRESET_NAME_EMPTY.ToString(),
                });
                return;
            }

            // 就地改写原对象，让 UI 下拉保持同一个引用（引用变了会让下拉的 Current 失配）。
            preset.Name = name;
            preset.SchemaVersion = working.SchemaVersion;
            preset.SkinId = working.SkinId;
            preset.SkinName = working.SkinName;
            preset.Settings = new Dictionary<string, string>(working.Settings);

            try
            {
                store.Save(preset);
            }
            catch (Exception e)
            {
                notifications?.Post(new SimpleNotification
                {
                    Text = $"{name}: {e.Message}",
                });
                return;
            }

            notifications?.Post(new SimpleNotification
            {
                Text = EzSettingsStrings.SETTINGS_PRESET_SAVED.Format(name),
            });

            // 重建下拉列表：名称是对象内容，不重建的话下拉仍显示旧名。
            onSaved?.Invoke();

            this.HidePopover();
        }

        /// <summary>
        /// 「使用当前的设置」按钮的可用性：当前状态与已保存内容一致时无事可做。
        /// </summary>
        private void flashFilled()
        {
            useCurrentButton.Enabled.Value = false;

            Scheduler.AddDelayed(() => useCurrentButton.Enabled.Value = true, 100);
        }

        private void updatePreview()
        {
            var rows = new List<Drawable>
            {
                createPreviewRow(EzSettingsStrings.SETTINGS_PRESET_PREVIEW_HEADER.ToString(), true),
                createPreviewRow($"{EzSettingsStrings.SETTINGS_PRESET_SKIN_LABEL}: {working.SkinName ?? "-"}", false),
            };

            rows.AddRange(working.Settings
                                .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                                .Select(kvp => createPreviewRow($"{kvp.Key} = {kvp.Value}", false)));

            previewContent.ChildrenEnumerable = rows;
        }

        private Drawable createPreviewRow(string text, bool header) => new OsuSpriteText
        {
            Text = text,
            Font = header ? OsuFont.GetFont(size: 14, weight: FontWeight.SemiBold) : OsuFont.GetFont(size: 12),
            Colour = header ? colourProvider.Content1 : colourProvider.Content2,
        };
    }
}
