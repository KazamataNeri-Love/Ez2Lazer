// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Animations;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.EzOsuGame.Edit;
using osu.Game.EzOsuGame.Localization;
using osu.Game.EzOsuGame.Mods;
using osu.Game.EzOsuGame.Screens;
using osu.Game.EzOsuGame.WarmUp;
using osu.Game.Localisation.SkinComponents;
using osu.Game.Overlays.Settings;
using osu.Game.Overlays.SkinEditor;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Play;
using osu.Game.Skinning;

namespace osu.Game.EzOsuGame.HUD
{
    /// <summary>
    /// 与 <see cref="EzHUDSpritePlus"/> 同源的自定义贴图 / 序列帧 HUD 组件。
    /// 游玩期在首条红线（<c>TimingPoints[0].Time</c>）之前停在第 0 帧静止，暂停时同步暂停，
    /// 到达红线后沿用原设 BPM / FPS 推进逻辑，并原生兼容 <c>{0}</c> / <c>{00}</c> / <c>{000}</c> 等常见序列帧命名。
    /// </summary>
    public partial class NeriHUDSpritePlus : CompositeDrawable, ISerialisableDrawable, IEzGameplayWarmUp
    {
        private const string modify_root = "Modify";
        private const int max_animation_frames = 240;

        // 帧动画统一按 60fps 基准烘焙，实际速度由外部推进量决定。
        internal const double BASE_FRAME_LENGTH = 1000.0 / 60.0;

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.SPRITE_PLUS_PATH_LABEL), nameof(EzHUDStrings.SPRITE_PLUS_PATH_DESCRIPTION), SettingControlType = typeof(ModifyPathSelectorControl))]
        public Bindable<string> ModifyPath { get; } = new Bindable<string>("Tachie");

        [SettingSource(typeof(SkinnableComponentStrings), nameof(SkinnableComponentStrings.SpriteName), SettingControlType = typeof(ModifySpriteSelectorControl))]
        public Bindable<string> SpriteName { get; } = new Bindable<string>(string.Empty);

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.SPRITE_PLUS_FRAME_TEMPLATE_LABEL), nameof(EzHUDStrings.SPRITE_PLUS_FRAME_TEMPLATE_DESCRIPTION), SettingControlType = typeof(SettingsTextBox))]
        public Bindable<string> FrameTemplate { get; } = new Bindable<string>("{0}");

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.PLAYBACK_FPS_LABEL), nameof(EzHUDStrings.PLAYBACK_FPS_DESCRIPTION))]
        public BindableNumber<float> FPS { get; } = new BindableNumber<float>(60)
        {
            MinValue = 1,
            MaxValue = 240,
            Precision = 1f,
        };

        [SettingSource(typeof(SkinnableComponentStrings), nameof(SkinnableComponentStrings.Colour), SettingControlType = typeof(EzSettingsColour))]
        public BindableColour4 AccentColour { get; } = new BindableColour4(Colour4.White);

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.HITRESULT_BLENDING_LABEL), nameof(EzHUDStrings.HITRESULT_BLENDING_DESCRIPTION))]
        public Bindable<EzHUDSpritePlus.BlendMode> Blend { get; } = new Bindable<EzHUDSpritePlus.BlendMode>(EzHUDSpritePlus.BlendMode.Mixture);

        [SettingSource("Use Beatmap BPM")]
        public Bindable<bool> UseBeatmapBPM { get; } = new Bindable<bool>(false);

        [SettingSource("Beat Division (1/n)")]
        public BindableNumber<int> BeatDivision { get; } = new BindableNumber<int>(4)
        {
            MinValue = 1,
            MaxValue = 32,
            Precision = 1,
        };

        public bool UsesFixedAnchor { get; set; }

        [Resolved]
        private EzResourceStore resource { get; set; } = null!;

        /// <summary>预热登记用的服务；只有进图会话期间登记才生效。</summary>
        [Resolved]
        private EzGameplayWarmUpService warmUpService { get; set; } = null!;

        [Resolved(canBeNull: true)]
        private Bindable<WorkingBeatmap>? beatmap { get; set; }

        [Resolved(canBeNull: true)]
        private GameplayState? gameplayState { get; set; }

        [Resolved(canBeNull: true)]
        private IGameplayClock? gameplayClock { get; set; }

        [Resolved(canBeNull: true)]
        private IFrameStableClock? frameStableClock { get; set; }

        [Resolved(canBeNull: true)]
        private osu.Game.Overlays.SkinEditor.SkinEditor? skinEditor { get; set; }

        [Resolved(canBeNull: true)]
        private SkinEditorOverlay? skinEditorOverlay { get; set; }

        [Resolved(canBeNull: true)]
        private EzSkinEditorOverlay? ezSkinEditorOverlay { get; set; }

        private bool warmUpRegistered;
        private EzBeatmapSpeedTracker speedTracker = null!;

        private Drawable? currentDrawable;
        private TextureAnimation? currentAnimation;

        private double playbackTime;
        private double? lastSongTime;

        public NeriHUDSpritePlus()
        {
            RelativeSizeAxes = Axes.None;
            AutoSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            // 与 EzHUDSpritePlus 保持一致：在 BDL 阶段登记预热，避免错过 PlayerLoader Ready 快照。
            warmUpService.Register(this);
            warmUpRegistered = true;

            AddInternal(speedTracker = new EzBeatmapSpeedTracker());

            scheduleReload();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            ModifyPath.BindValueChanged(_ => scheduleReload());
            SpriteName.BindValueChanged(_ => scheduleReload());
            FrameTemplate.BindValueChanged(_ => scheduleReload());

            AccentColour.BindValueChanged(_ => applyVisualSettings(), true);
            Blend.BindValueChanged(_ => applyVisualSettings(), true);
        }

        protected override void Update()
        {
            base.Update();

            if (currentAnimation == null)
                return;

            double duration = currentAnimation.Duration;

            if (duration <= 0)
                return;

            if (gameplayClock != null && !isSkinEditorActive)
            {
                double songTime = frameStableClock?.CurrentTime ?? gameplayClock.CurrentTime;
                bool isPaused = gameplayClock.IsPaused.Value || !gameplayClock.IsRunning;
                double firstTimingPointTime = ResolveFirstTimingPointTime(resolveActiveBeatmap());

                playbackTime = AdvanceGameplayPlayback(
                    playbackTime,
                    songTime,
                    lastSongTime,
                    Clock.ElapsedFrameTime,
                    firstTimingPointTime,
                    isPaused,
                    UseBeatmapBPM.Value,
                    BeatDivision.Value,
                    speedTracker.BeatLength.Value,
                    FPS.Value);

                lastSongTime = songTime;
            }
            else
            {
                lastSongTime = null;
                playbackTime = Math.Max(0, playbackTime + Clock.ElapsedFrameTime * getRealTimePlaybackSpeed());
            }

            currentAnimation.PlaybackPosition = playbackTime % duration;
        }

        private bool isSkinEditorActive
            => skinEditor != null
               || skinEditorOverlay?.State.Value == Visibility.Visible
               || ezSkinEditorOverlay?.State.Value == Visibility.Visible;

        private IBeatmap? resolveActiveBeatmap()
        {
            if (gameplayState?.Beatmap != null)
                return gameplayState.Beatmap;

            var working = beatmap?.Value;
            return working?.BeatmapLoaded == true ? working.Beatmap : null;
        }

        private double getRealTimePlaybackSpeed()
        {
            if (UseBeatmapBPM.Value)
            {
                double beatLength = speedTracker.BeatLength.Value;

                if (beatLength > 0)
                    return BASE_FRAME_LENGTH * BeatDivision.Value * speedTracker.Rate.Value / beatLength;
            }

            return FPS.Value * BASE_FRAME_LENGTH / 1000.0;
        }

        /// <summary>
        /// 返回谱面首条红线时间；无红线时回退为 0（屏蔽负时间的开场倒数）。
        /// </summary>
        internal static double ResolveFirstTimingPointTime(IBeatmap? beatmap)
        {
            var timingPoints = beatmap?.ControlPointInfo.TimingPoints;
            return timingPoints is { Count: > 0 } ? timingPoints[0].Time : 0;
        }

        /// <summary>
        /// 计算游玩期下一帧的累计动画时间：首条红线前归零静止，暂停时保持不动，红线后按原设 BPM / FPS 推进。
        /// </summary>
        internal static double AdvanceGameplayPlayback(
            double currentPlaybackTime,
            double songTime,
            double? previousSongTime,
            double elapsedRealTime,
            double firstTimingPointTime,
            bool isPaused,
            bool useBeatmapBpm,
            int beatDivision,
            double beatLength,
            float fps)
        {
            if (songTime < firstTimingPointTime)
                return 0;

            if (isPaused)
                return Math.Max(0, currentPlaybackTime);

            double previous = previousSongTime ?? songTime;

            if (useBeatmapBpm && beatLength > 0)
            {
                // gameplayClock 的增量已是歌曲时间（已含 Rate Mod），不可再乘 Rate。
                double effectiveStart = Math.Max(previous, firstTimingPointTime);
                double deltaSongTime = songTime - effectiveStart;
                double deltaPlayback = deltaSongTime * (BASE_FRAME_LENGTH * beatDivision / beatLength);
                return Math.Max(0, currentPlaybackTime + deltaPlayback);
            }

            if (songTime <= previous || elapsedRealTime <= 0)
                return Math.Max(0, currentPlaybackTime);

            double effectiveRealTime = previous < firstTimingPointTime
                ? elapsedRealTime * ((songTime - firstTimingPointTime) / (songTime - previous))
                : elapsedRealTime;

            double fpsDelta = effectiveRealTime * (fps * BASE_FRAME_LENGTH / 1000.0);
            return Math.Max(0, currentPlaybackTime + fpsDelta);
        }

        private void scheduleReload() => Schedule(reloadDrawable);

        private void reloadDrawable()
        {
            string spriteName = SpriteName.Value?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(spriteName))
            {
                clearDrawable();
                return;
            }

            Texture[] frames = getFrames(spriteName);

            // 防止下拉项重建等瞬时状态清空当前正在显示的贴图。
            if (frames.Length == 0)
                return;

            clearDrawable();
            currentDrawable = createDrawable(frames);
            currentAnimation = currentDrawable as TextureAnimation;
            playbackTime = 0;
            lastSongTime = null;

            AddInternal(currentDrawable);
            applyVisualSettings();
        }

        private EzAnimationRequest buildRequest(string spriteName) => new EzAnimationRequest
        {
            Path = buildBaseLookup(spriteName),
            FrameTemplate = string.IsNullOrWhiteSpace(FrameTemplate.Value) ? null : FrameTemplate.Value.Trim(),
            MaxFrames = max_animation_frames,
        };

        private Texture[] getFrames(string spriteName)
        {
            Texture[] frames = resource.GetTextureFrames(buildRequest(spriteName));

            if (frames.Length > 1)
                return frames;

            Texture[] fallbackFrames = loadTemplateFallbackFrames(spriteName);
            return fallbackFrames.Length > 0 ? fallbackFrames : frames;
        }

        private Texture[] loadTemplateFallbackFrames(string spriteName)
        {
            string template = FrameTemplate.Value?.Trim() ?? string.Empty;
            string directory = buildModifyDirectoryKey();
            IReadOnlyList<string> imageKeys = resource.ListImageKeys(directory);

            IReadOnlyList<string> orderedKeys = ResolveFallbackFrameKeys(imageKeys, spriteName, template, max_animation_frames);

            if (orderedKeys.Count == 0)
                return Array.Empty<Texture>();

            var textures = new List<Texture>(orderedKeys.Count);

            foreach (string key in orderedKeys)
            {
                Texture? texture = resource.Get(key, EzTextureUsage.AnimationSafe);

                if (texture == null)
                    break;

                textures.Add(texture);
            }

            return textures.ToArray();
        }

        /// <summary>
        /// 按模板解析目录中的连续序列帧键，补齐底层索引不支持的 <c>{00}</c>、无连接符及 1 起始序列帧。
        /// </summary>
        internal static IReadOnlyList<string> ResolveFallbackFrameKeys(
            IReadOnlyList<string> imageKeys,
            string spriteName,
            string template,
            int maxFrames = max_animation_frames)
        {
            if (imageKeys.Count == 0 || string.IsNullOrEmpty(spriteName) || maxFrames <= 0)
                return Array.Empty<string>();

            if (!TryParseFrameTemplate(template, out string prefixText, out int width))
                return Array.Empty<string>();

            string prefix = ExpandTemplatePrefix(prefixText, template, spriteName);
            var matched = new SortedDictionary<int, string>();

            foreach (string key in imageKeys)
            {
                string fileName = EzResourcePath.FileName(key);
                string stem = EzResourcePath.IsImage(fileName, out _) ? EzResourcePath.Stem(fileName) : fileName;

                if (stem.Length <= prefix.Length || !stem.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                string digits = stem[prefix.Length..];

                if (!MatchesFrameDigits(digits, width))
                    continue;

                if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out int index))
                    continue;

                matched.TryAdd(index, key);
            }

            if (matched.Count == 0)
                return Array.Empty<string>();

            var orderedKeys = new List<string>(Math.Min(matched.Count, maxFrames));
            int? previousIndex = null;

            foreach ((int index, string key) in matched)
            {
                if (previousIndex.HasValue && index != previousIndex.Value + 1)
                    break;

                orderedKeys.Add(key);
                previousIndex = index;

                if (orderedKeys.Count >= maxFrames)
                    break;
            }

            return orderedKeys;
        }

        internal static bool TryParseFrameTemplate(string template, out string prefixText, out int width)
        {
            prefixText = string.Empty;
            width = 0;

            if (string.IsNullOrEmpty(template))
                return false;

            int open = template.LastIndexOf('{');

            if (open < 0)
                return false;

            int close = template.IndexOf('}', open + 1);

            if (close <= open + 1)
                return false;

            string zeros = template[(open + 1)..close];

            if (zeros.Length > 4 || !zeros.All(c => c == '0'))
                return false;

            string tail = template[(close + 1)..];

            if (tail.Length > 0 && !EzResourcePath.IsImage(tail, out _))
                return false;

            prefixText = template[..open];
            width = zeros.Length;
            return true;
        }

        internal static bool MatchesFrameDigits(string digits, int width)
        {
            if (digits.Length == 0 || !digits.All(char.IsAsciiDigit))
                return false;

            if (width == 1)
                return digits.Length == 1 || digits[0] != '0';

            return digits.Length == width || (digits.Length > width && digits[0] != '0');
        }

        internal static string ExpandTemplatePrefix(string prefixText, string template, string spriteName)
        {
            if (template.Contains("{name}", StringComparison.Ordinal) || template.Contains("{result}", StringComparison.Ordinal))
                return prefixText.Replace("{name}", spriteName, StringComparison.Ordinal).Replace("{result}", spriteName, StringComparison.Ordinal);

            return spriteName + prefixText;
        }

        public Task WarmUpAsync(CancellationToken cancellationToken)
        {
            string spriteName = SpriteName.Value?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(spriteName))
                return Task.CompletedTask;

            return EzGameplayWarmUpService.RunBlocking(() => getFrames(spriteName), cancellationToken);
        }

        private void clearDrawable()
        {
            if (currentDrawable != null)
                RemoveInternal(currentDrawable, disposeImmediately: true);

            currentDrawable = null;
            currentAnimation = null;
        }

        private static Drawable createDrawable(Texture[] frames)
        {
            if (frames.Length == 1)
                return new Sprite { Texture = frames[0] };

            var animation = new TextureAnimation
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Loop = true,
                DefaultFrameLength = BASE_FRAME_LENGTH,
                IsPlaying = false,
            };

            animation.AddFrames(frames);
            return animation;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (warmUpRegistered)
                warmUpService.Unregister(this);
        }

        private void applyVisualSettings()
        {
            if (currentDrawable != null)
            {
                currentDrawable.Colour = AccentColour.Value;
                currentDrawable.Blending = getBlendingParameters(Blend.Value);
            }
        }

        private static BlendingParameters getBlendingParameters(EzHUDSpritePlus.BlendMode mode)
        {
            return mode switch
            {
                EzHUDSpritePlus.BlendMode.Inherit => BlendingParameters.Inherit,
                EzHUDSpritePlus.BlendMode.Mixture => BlendingParameters.Mixture,
                EzHUDSpritePlus.BlendMode.Additive => BlendingParameters.Additive,
                EzHUDSpritePlus.BlendMode.None => BlendingParameters.None,
                _ => BlendingParameters.Mixture,
            };
        }

        private string buildModifyDirectoryKey()
        {
            string path = normaliseModifyPath(ModifyPath.Value);
            return string.IsNullOrEmpty(path) ? modify_root : $"{modify_root}/{path}";
        }

        private string buildBaseLookup(string spriteName)
        {
            string path = normaliseModifyPath(ModifyPath.Value);
            return string.IsNullOrEmpty(path) ? $"{modify_root}/{spriteName}" : $"{modify_root}/{path}/{spriteName}";
        }

        private static string normaliseModifyPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            return path.Trim()
                       .Replace('\\', '/')
                       .Trim('/');
        }

        private static void ensureCurrentInItems(Bindable<string> current, List<string> items, bool normaliseAsPath)
        {
            string value = normaliseAsPath
                ? normaliseModifyPath(current.Value)
                : (current.Value?.Trim() ?? string.Empty);

            int existing = items.FindIndex(i => string.Equals(i, value, StringComparison.OrdinalIgnoreCase));

            if (existing >= 0)
            {
                if (current.Value != items[existing])
                    current.Value = items[existing];
                return;
            }

            if (value.Length > 0)
            {
                items.Insert(0, value);
                if (current.Value != value)
                    current.Value = value;
                return;
            }

            if (items.Count == 0)
                items.Add(string.Empty);

            current.Value = items[0];
        }

        public partial class ModifyPathSelectorControl : SettingsDropdown<string>
        {
            [Resolved]
            private Storage storage { get; set; } = null!;

            private NeriHUDSpritePlus source = null!;

            protected override void LoadComplete()
            {
                base.LoadComplete();

                source = (NeriHUDSpritePlus)SettingSourceObject;
                refreshItems();
            }

            private void refreshItems()
            {
                var list = new List<string>();

                try
                {
                    string modifyRoot = storage.GetFullPath("EzResources/Modify");

                    if (Directory.Exists(modifyRoot))
                    {
                        list.AddRange(Directory.GetDirectories(modifyRoot, "*", SearchOption.AllDirectories)
                                               .Select(path => Path.GetRelativePath(modifyRoot, path))
                                               .Select(normaliseModifyPath)
                                               .Where(path => path.Length > 0)
                                               .Distinct(StringComparer.OrdinalIgnoreCase)
                                               .OrderBy(path => path.Count(c => c == '/'))
                                               .ThenBy(path => path, StringComparer.OrdinalIgnoreCase));
                    }
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }

                ensureCurrentInItems(source.ModifyPath, list, normaliseAsPath: true);
                Items = list;
            }
        }

        public partial class ModifySpriteSelectorControl : SettingsDropdown<string>
        {
            [Resolved]
            private Storage storage { get; set; } = null!;

            private NeriHUDSpritePlus source = null!;

            protected override void LoadComplete()
            {
                base.LoadComplete();

                source = (NeriHUDSpritePlus)SettingSourceObject;
                refreshItems();
                source.ModifyPath.BindValueChanged(_ => refreshItems());
                source.FrameTemplate.BindValueChanged(_ => refreshItems());
            }

            private void refreshItems()
            {
                var list = new List<string>();

                try
                {
                    string path = source.ModifyPath.Value?.Trim() ?? string.Empty;
                    string fullDir = storage.GetFullPath(buildModifyDirectory(path));

                    if (Directory.Exists(fullDir))
                    {
                        string[] rawFileNames = Directory.GetFiles(fullDir)
                                                         .Select(file => Path.GetFileNameWithoutExtension(file))
                                                         .Where(name => !string.IsNullOrEmpty(name))
                                                         .ToArray();

                        var rawFileNameSet = new HashSet<string>(rawFileNames, StringComparer.OrdinalIgnoreCase);

                        list.AddRange(rawFileNames.Select(name => resolveDisplayName(name, rawFileNameSet))
                                                  .Distinct(StringComparer.OrdinalIgnoreCase)
                                                  .OrderBy(name => name, StringComparer.OrdinalIgnoreCase));
                    }
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }

                ensureCurrentInItems(source.SpriteName, list, normaliseAsPath: false);
                Items = list;
            }

            private static string buildModifyDirectory(string relativePath)
            {
                string normalised = normaliseModifyPath(relativePath);
                return string.IsNullOrEmpty(normalised)
                    ? "EzResources/Modify"
                    : $"EzResources/Modify/{normalised}";
            }

            private string resolveDisplayName(string fileNameWithoutExtension, HashSet<string> allNames)
            {
                string template = source.FrameTemplate.Value?.Trim() ?? string.Empty;

                if (!TryParseFrameTemplate(template, out string prefixText, out int width))
                    return fileNameWithoutExtension;

                string staticSuffix = prefixText.Replace("{name}", string.Empty, StringComparison.Ordinal)
                                                .Replace("{result}", string.Empty, StringComparison.Ordinal);

                // 仅在模板前缀为纯分隔后缀（如 "{00}"、"-{00}"、"{name}-{00}"）时折叠帧号。
                if (staticSuffix.Contains('/'))
                    return fileNameWithoutExtension;

                int digitsStart = fileNameWithoutExtension.Length;

                while (digitsStart > 0 && char.IsAsciiDigit(fileNameWithoutExtension[digitsStart - 1]))
                    digitsStart--;

                if (digitsStart >= fileNameWithoutExtension.Length)
                    return fileNameWithoutExtension;

                string digits = fileNameWithoutExtension[digitsStart..];

                if (!MatchesFrameDigits(digits, width))
                    return fileNameWithoutExtension;

                if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out int frame) || frame < 0 || frame > 999)
                    return fileNameWithoutExtension;

                string beforeDigits = fileNameWithoutExtension[..digitsStart];

                if (staticSuffix.Length > 0)
                {
                    if (!beforeDigits.EndsWith(staticSuffix, StringComparison.OrdinalIgnoreCase))
                        return fileNameWithoutExtension;

                    beforeDigits = beforeDigits[..^staticSuffix.Length];
                }

                if (string.IsNullOrEmpty(beforeDigits))
                    return fileNameWithoutExtension;

                string format = $"D{width}";
                string previousFrame = frame > 0 ? $"{beforeDigits}{staticSuffix}{(frame - 1).ToString(format, CultureInfo.InvariantCulture)}" : string.Empty;
                string nextFrame = frame < 999 ? $"{beforeDigits}{staticSuffix}{(frame + 1).ToString(format, CultureInfo.InvariantCulture)}" : string.Empty;

                bool hasNeighbourFrame = (previousFrame.Length > 0 && allNames.Contains(previousFrame))
                                         || (nextFrame.Length > 0 && allNames.Contains(nextFrame));

                return hasNeighbourFrame ? beforeDigits : fileNameWithoutExtension;
            }
        }
    }
}
