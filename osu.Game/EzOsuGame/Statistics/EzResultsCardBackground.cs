// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Logging;
using osu.Game.EzOsuGame.Configuration;

namespace osu.Game.EzOsuGame.Statistics
{
    /// <summary>
    /// 结算卡片按显示序位选取的背景图。图片固定从 <c>EzResources/Modify/Results</c> 取，配置变化即时生效。
    /// </summary>
    public partial class EzResultsCardBackground : CompositeDrawable
    {
        private const int image_slots = 6;

        /// <summary>目前只有 Mania 提供卡片映射。</summary>
        public const string SUPPORTED_RULESET = "mania";

        /// <summary><c>EzResources</c> 下的卡片背景图目录。</summary>
        public const string IMAGE_DIRECTORY = "Modify/Results";

        private readonly Bindable<string>[] imageNames = new Bindable<string>[image_slots];
        private readonly Bindable<Colour4>[] imageColours = new Bindable<Colour4>[image_slots];
        private readonly Bindable<int>[] fillModes = new Bindable<int>[image_slots];

        private Bindable<bool> enabled = null!;
        private Bindable<int> revision = null!;

        private EzResourceStore resources = null!;
        private Sprite sprite = null!;

        private string? currentImageName;

        private int displayIndex;

        /// <summary>成绩所属规则的短名；只有 <see cref="SUPPORTED_RULESET"/> 会应用背景。</summary>
        public string? RulesetShortName { get; init; }

        /// <summary>零基显示序位，决定取哪一张配置图。</summary>
        public int DisplayIndex
        {
            get => displayIndex;
            set
            {
                if (displayIndex == value)
                    return;

                displayIndex = value;

                if (IsLoaded)
                    apply();
            }
        }

        public EzResultsCardBackground()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load(Ez2ConfigManager config, EzResourceStore resourceStore)
        {
            resources = resourceStore;

            // GetBindable 返回的已是绑定到配置的副本，可直接订阅；销毁时 UnbindAll 解除。
            enabled = config.GetBindable<bool>(Ez2Setting.ResultsCardBackgroundEnabled);
            revision = config.GetBindable<int>(Ez2Setting.ResultsCardBackgroundRevision);

            for (int i = 0; i < image_slots; i++)
            {
                imageNames[i] = config.GetBindable<string>((Ez2Setting)((int)Ez2Setting.ResultsCardBackgroundImage0 + i));
                imageColours[i] = config.GetBindable<Colour4>((Ez2Setting)((int)Ez2Setting.ResultsCardBackgroundColour0 + i));
                fillModes[i] = config.GetBindable<int>((Ez2Setting)((int)Ez2Setting.ResultsCardBackgroundFillMode0 + i));
            }

            AddInternal(sprite = new Sprite
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Alpha = 0,
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            enabled.BindValueChanged(_ => apply());

            for (int i = 0; i < image_slots; i++)
            {
                imageNames[i].BindValueChanged(_ => apply());
                imageColours[i].BindValueChanged(_ => apply());
                fillModes[i].BindValueChanged(_ => apply());
            }

            revision.BindValueChanged(_ =>
            {
                // 强制重取（配合 EzResourceStore.InvalidateResourceCaches 让同名文件替换生效）。
                currentImageName = null;
                apply();
            });

            apply();
        }

        /// <summary>重新应用当前配置，同图名也会重新取图。</summary>
        public void Reload()
        {
            currentImageName = null;
            apply();
        }

        private void apply()
        {
            if (!enabled.Value || RulesetShortName != SUPPORTED_RULESET || displayIndex < 0 || displayIndex >= image_slots)
            {
                hide();
                return;
            }

            string imageName = imageNames[displayIndex].Value;

            if (string.IsNullOrWhiteSpace(imageName))
            {
                hide();
                return;
            }

            sprite.FillMode = fillModes[displayIndex].Value == 1 ? FillMode.Fit : FillMode.Fill;
            sprite.Colour = imageColours[displayIndex].Value;

            // 同一图名不重复取图（纹理池会命中缓存，但避免不必要的引用变动）。
            if (imageName != currentImageName)
            {
                currentImageName = imageName;
                sprite.Texture = resources.Get($"{IMAGE_DIRECTORY}/{imageName}", EzTextureUsage.Large);

                Logger.Log($"Results card background [{displayIndex}] '{IMAGE_DIRECTORY}/{imageName}' texture={(sprite.Texture != null ? "ok" : "missing")}",
                    LoggingTarget.Runtime, sprite.Texture != null ? LogLevel.Debug : LogLevel.Important);
            }

            sprite.Alpha = sprite.Texture != null ? 1 : 0;
        }

        private void hide()
        {
            currentImageName = null;
            sprite.Texture = null;
            sprite.Alpha = 0;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                enabled?.UnbindAll();
                revision?.UnbindAll();

                foreach (var name in imageNames)
                    name?.UnbindAll();

                foreach (var colour in imageColours)
                    colour?.UnbindAll();

                foreach (var mode in fillModes)
                    mode?.UnbindAll();
            }

            base.Dispose(isDisposing);
        }
    }
}
