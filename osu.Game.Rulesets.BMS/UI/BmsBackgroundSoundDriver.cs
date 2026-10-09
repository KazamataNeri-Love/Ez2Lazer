// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Logging;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.BMS.Audio;
using osu.Game.Rulesets.BMS.Beatmaps;
using osu.Game.Rulesets.BMS.Configuration;
using osu.Game.Rulesets.Configuration;
using osu.Game.Rulesets.Objects;
using osu.Game.Screens.Play;

namespace osu.Game.Rulesets.BMS.UI
{
    /// <summary>
    /// Drives <see cref="BmsKeysoundManager"/> background sound events from the gameplay clock.
    ///
    /// BMS has no dedicated BGM track: background (channel 01) and auto-play lanes are ordinary
    /// keysound events that no hit object carries, so the regular skin/sample pipeline can never
    /// play them. The BMS-owned launch path (<c>BMSPlayerLoader</c> → <see cref="BmsPlayer"/> /
    /// <see cref="BmsReplayPlayer"/>) dispatches them itself, but any other launch path (e.g. a BMS
    /// chart started from the standard song select, which uses a plain <c>SoloPlayer</c>) has no BMS
    /// player at all — this driver covers those, so BGM no longer depends on how gameplay was entered.
    ///
    /// Only the non-note BGM stream is handled here; hit object keysounds remain handled via the
    /// regular skin/sample pipeline (through <see cref="BMSExternalSampleSkin"/> / <see cref="BMSSkin"/>).
    /// </summary>
    internal partial class BmsBackgroundSoundDriver : Drawable
    {
        private const string bms_log_prefix = "[BMS]";

        private BmsKeysoundManager? keysoundManager;
        private Bindable<double>? keysoundVolume;
        private IGameplayClock? gameplayClock;
        private bool isDisposed;

        /// <summary>
        /// Non-null while the working beatmap's main track is silenced. BMS background music is dispatched from the
        /// chart's own background/auto-play channels, so letting the generic track play as well would double it.
        /// </summary>
        private Track? silencedTrack;

        /// <summary>
        /// Applied as an adjustment rather than written to <see cref="Track.Volume"/> so the user's music volume
        /// setting is preserved and restored untouched.
        /// </summary>
        private readonly BindableDouble silencedTrackVolume = new BindableDouble();

        [Resolved]
        private AudioManager audioManager { get; set; } = null!;

        [Resolved]
        private IBindable<WorkingBeatmap> beatmap { get; set; } = null!;

        [Resolved]
        private IRulesetConfigCache rulesetConfigCache { get; set; } = null!;

        public BmsBackgroundSoundDriver()
        {
            AlwaysPresent = true;
        }

        [BackgroundDependencyLoader(true)]
        private void load(IGameplayClock? clock)
        {
            // Make audio available to other non-Drawable BMS helpers (e.g. external sample skin).
            BmsRuntimeAudioContext.Register(audioManager);

            gameplayClock = clock;

            WorkingBeatmap workingBeatmap = beatmap.Value;

            silenceMainTrack(workingBeatmap);

            // BMS-owned working beatmaps are created and driven by the BMS player / player loader.
            if (workingBeatmap is ManiaConvertedWorkingBeatmap || workingBeatmap is BMSWorkingBeatmap)
                return;

            if (workingBeatmap.Beatmap is not BMSBeatmap bmsBeatmap || bmsBeatmap.BackgroundSoundEvents.Count == 0)
                return;

            if (!BMSExternalPath.TryGetContentRoot(workingBeatmap.BeatmapInfo.BeatmapSet, out string bmsFolder))
                return;

            try
            {
                keysoundManager = new BmsKeysoundManager(audioManager, bmsFolder);

                if (resolveAutoPreloadKeysounds())
                {
                    // Only the BGM files are materialised here: hit object keysounds are resolved by the
                    // skin pipeline, and preloading them again would double their memory footprint.
                    keysoundManager.Prepare(Array.Empty<HitObject>(), bmsBeatmap.BackgroundSoundEvents);
                }
                else
                {
                    keysoundManager.SetBackgroundSoundEvents(bmsBeatmap.BackgroundSoundEvents);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"{bms_log_prefix} BGM driver failed to initialise: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
                keysoundManager = null;
            }

            if (keysoundManager == null)
                return;

            if (rulesetConfigCache.GetConfigFor(new BMSRuleset()) is BMSRulesetConfigManager bmsConfig)
            {
                keysoundVolume = bmsConfig.GetBindable<double>(BMSRulesetSetting.KeysoundVolume);
                keysoundVolume.BindValueChanged(onKeysoundVolumeChanged, true);
            }
        }

        /// <summary>
        /// Silences the generic main track: this driver dispatches every background sound itself, so the track
        /// would be heard on top of it. The track stays the clock source, exactly like the BMS-owned paths, where
        /// <c>BMSWorkingBeatmap.GetBeatmapTrack()</c> returns a silent virtual track.
        /// </summary>
        private void silenceMainTrack(WorkingBeatmap workingBeatmap)
        {
            Track track = workingBeatmap.Track;

            if (track is TrackVirtual)
                return;

            silencedTrackVolume.Value = 0;
            track.AddAdjustment(AdjustableProperty.Volume, silencedTrackVolume);
            silencedTrack = track;
        }

        private void onKeysoundVolumeChanged(ValueChangedEvent<double> volume)
        {
            var manager = keysoundManager;

            if (isDisposed || manager == null || manager.IsDisposed)
                return;

            manager.SetVolume(volume.NewValue);
        }

        private bool resolveAutoPreloadKeysounds()
        {
            try
            {
                if (rulesetConfigCache.GetConfigFor(new BMSRuleset()) is BMSRulesetConfigManager bmsConfig)
                    return bmsConfig.Get<bool>(BMSRulesetSetting.AutoPreloadKeysounds);
            }
            catch (Exception ex)
            {
                Logger.Log($"{bms_log_prefix} Failed to read AutoPreloadKeysounds: {ex.Message}", LoggingTarget.Runtime, LogLevel.Important);
            }

            return true;
        }

        protected override void Update()
        {
            base.Update();

            var manager = keysoundManager;

            if (isDisposed || manager == null || manager.IsDisposed)
                return;

            double currentTime = gameplayClock?.CurrentTime ?? Clock.CurrentTime;

            if (currentTime < 0)
                return;

            manager.Update(currentTime);
        }

        protected override void Dispose(bool isDisposing)
        {
            isDisposed = true;

            if (keysoundVolume != null)
                keysoundVolume.ValueChanged -= onKeysoundVolumeChanged;

            if (silencedTrack != null)
            {
                silencedTrack.RemoveAdjustment(AdjustableProperty.Volume, silencedTrackVolume);
                silencedTrack = null;
            }

            keysoundManager?.Dispose();
            keysoundManager = null;

            base.Dispose(isDisposing);
        }
    }
}
