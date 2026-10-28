// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.EzOsuGame.Statistics;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays.Settings;
using osu.Game.Screens.Edit.Components;
using osuTK;

namespace osu.Game.EzOsuGame.Screens
{
    /// <summary>
    /// 结算卡片背景配置页：模式选择、开关、每张卡的图 / 染色 / 填充方式。
    /// 仅在 Ez 布局编辑器中加入右侧边栏。
    /// </summary>
    public partial class EzEditorSectionResultsSidebar : EditorSidebarSection
    {
        private const int card_count = 6;
        private const int mania_mode = 3;
        private const string none_option = "NULL";

        private static readonly string[] card_titles =
        {
            "Main",
            "Performance Breakdown",
            "Space Graph",
            "Timing Distribution",
            "Every Column Timing Graph",
            "Hit Result Count",
        };

        [Resolved]
        private Ez2ConfigManager ezConfig { get; set; } = null!;

        [Resolved]
        private Storage storage { get; set; } = null!;

        [Resolved]
        private EzResourceStore resourceStore { get; set; } = null!;

        private readonly SettingsDropdown<string>[] imageDropdowns = new SettingsDropdown<string>[card_count];
        private readonly List<IBindable> boundCopies = new List<IBindable>();
        private readonly List<EzResultsCardSection> cardSections = new List<EzResultsCardSection>();

        private Bindable<int>? revision;
        private OsuSpriteText unsupportedNote = null!;

        public EzEditorSectionResultsSidebar()
            : base("Result Card Background")
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            revision = register(ezConfig.GetBindable<int>(Ez2Setting.ResultsCardBackgroundRevision));

            var editMode = register(ezConfig.GetBindable<int>(Ez2Setting.ResultsCardBackgroundEditingMode));
            var images = listImages();

            unsupportedNote = new OsuSpriteText
            {
                Text = "当前仅支持 Mania 模式的卡片映射。",
                Font = OsuFont.GetFont(size: 12),
            };

            var general = new EzResultsCardSection("设置 (Result Card Background)");
            general.AddControl(new SettingsCheckbox
            {
                LabelText = "启用卡片背景",
                Current = register(ezConfig.GetBindable<bool>(Ez2Setting.ResultsCardBackgroundEnabled)),
            });
            general.AddControl(new SettingsDropdown<string>
            {
                LabelText = "模式",
                Items = new[] { "Osu", "Taiko", "Catch", "Mania" },
                Current = createModeBindable(editMode),
            });
            general.AddControl(new SettingsButton
            {
                Text = "刷新列表",
                Action = refreshImageList,
            });
            general.AddControl(new SettingsButton
            {
                Text = "重载",
                Action = reload,
            });
            general.AddControl(unsupportedNote);

            var sections = new List<Drawable> { general };

            for (int i = 0; i < card_count; i++)
            {
                var imageDropdown = new SettingsDropdown<string>
                {
                    LabelText = "背景图",
                    Items = imageItems(images, i),
                    Current = createImageBindable(i),
                };

                imageDropdowns[i] = imageDropdown;

                var section = new EzResultsCardSection($"设置 ({card_titles[i]})");

                section.AddControl(imageDropdown);
                section.AddControl(new EzSettingsColour
                {
                    LabelText = "染色",
                    Current = register(ezConfig.GetBindable<Colour4>((Ez2Setting)((int)Ez2Setting.ResultsCardBackgroundColour0 + i))),
                });
                section.AddControl(new SettingsDropdown<string>
                {
                    LabelText = "填充方式",
                    Items = new[] { "Fill", "Uniform" },
                    Current = createFillModeBindable(i),
                });

                cardSections.Add(section);
                sections.Add(section);
            }

            InternalChild = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(10),
                Children = sections,
            };

            updateCardSection(editMode.Value);
            editMode.BindValueChanged(v => updateCardSection(v.NewValue));
        }

        private T register<T>(T bindable)
            where T : IBindable
        {
            boundCopies.Add(bindable);
            return bindable;
        }

        private List<string> listImages()
        {
            var result = new List<string>();

            try
            {
                string? directory = storage.GetFullPath($"EzResources/{EzResultsCardBackground.IMAGE_DIRECTORY}");

                if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                    return result;

                result.AddRange(Directory.GetFiles(directory, "*.png")
                                         .Select(Path.GetFileName)
                                         .OfType<string>()
                                         .OrderBy(name => name, StringComparer.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to list result card background images");
            }

            return result;
        }

        private Bindable<string> createModeBindable(Bindable<int> mode)
        {
            var modeName = new Bindable<string>(modeNameFor(mode.Value));
            modeName.BindValueChanged(v => mode.Value = modeIndexFor(v.NewValue));
            mode.BindValueChanged(v => modeName.Value = modeNameFor(v.NewValue));
            return modeName;
        }

        private static string modeNameFor(int index)
        {
            switch (index)
            {
                case 0: return "Osu";
                case 1: return "Taiko";
                case 2: return "Catch";
                default: return "Mania";
            }
        }

        private static int modeIndexFor(string name)
        {
            switch (name)
            {
                case "Osu": return 0;
                case "Taiko": return 1;
                case "Catch": return 2;
                default: return mania_mode;
            }
        }

        private Bindable<string> createFillModeBindable(int slot)
        {
            var mode = register(ezConfig.GetBindable<int>((Ez2Setting)((int)Ez2Setting.ResultsCardBackgroundFillMode0 + slot)));
            var modeName = new Bindable<string>(mode.Value == 1 ? "Uniform" : "Fill");

            modeName.BindValueChanged(v => mode.Value = v.NewValue == "Uniform" ? 1 : 0);
            mode.BindValueChanged(v => modeName.Value = v.NewValue == 1 ? "Uniform" : "Fill");

            return modeName;
        }

        private List<string> imageItems(List<string> images, int slot)
        {
            var items = new List<string> { none_option };
            items.AddRange(images);

            string current = ezConfig.Get<string>((Ez2Setting)((int)Ez2Setting.ResultsCardBackgroundImage0 + slot));

            if (!string.IsNullOrWhiteSpace(current) && !items.Contains(current))
                items.Add(current);

            return items;
        }

        private Bindable<string> createImageBindable(int slot)
        {
            var image = register(ezConfig.GetBindable<string>((Ez2Setting)((int)Ez2Setting.ResultsCardBackgroundImage0 + slot)));
            var name = new Bindable<string>(string.IsNullOrWhiteSpace(image.Value) ? none_option : image.Value);

            name.BindValueChanged(v => image.Value = v.NewValue == none_option ? string.Empty : v.NewValue);
            image.BindValueChanged(v => name.Value = string.IsNullOrWhiteSpace(v.NewValue) ? none_option : v.NewValue);

            return name;
        }

        private void refreshImageList()
        {
            var images = listImages();

            for (int i = 0; i < card_count; i++)
                imageDropdowns[i].Items = imageItems(images, i);
        }

        private void reload()
        {
            // 同名图片被替换时纹理缓存仍是旧的，先清缓存再自增版本强制重取。
            resourceStore.InvalidateResourceCaches();

            if (revision != null)
                revision.Value = revision.Value + 1;
        }

        private void updateCardSection(int mode)
        {
            bool supported = mode == mania_mode;

            foreach (var section in cardSections)
            {
                section.Alpha = supported ? 1 : 0;

                // Collapse the section entirely so unsupported modes do not leave a tall empty gap.
                if (supported)
                    section.AutoSizeAxes = Axes.Y;
                else
                {
                    section.AutoSizeAxes = Axes.None;
                    section.Height = 0;
                }
            }

            if (unsupportedNote != null)
                unsupportedNote.Alpha = supported ? 0 : 1;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                foreach (var bound in boundCopies)
                    bound.UnbindAll();

                boundCopies.Clear();
            }

            base.Dispose(isDisposing);
        }
    }

    /// <summary>
    /// 带标题的卡片设置分组，排版与 Select 页的组件设置一致。
    /// </summary>
    internal partial class EzResultsCardSection : EditorSidebarSection
    {
        protected override Container<Drawable> Content { get; }

        public EzResultsCardSection(string title)
            : base(title)
        {
            base.Content.Add(Content = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(10),
            });
        }

        public void AddControl(Drawable control) => Content.Add(control);
    }
}
