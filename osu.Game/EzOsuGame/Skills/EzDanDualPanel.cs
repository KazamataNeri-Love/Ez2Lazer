// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Layout;
using osu.Game.Beatmaps;
using osu.Game.EzOsuGame.Localization;
using osu.Game.EzOsuGame.LocalProfile;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osu.Game.Rulesets.Mods;
using osuTK;

namespace osu.Game.EzOsuGame.Skills
{
    /// <summary>
    /// RC|LN dual dan panel for EzAnalysis and Local Profile Track (UX-5).
    /// Narrow: vertical stack with horizontal axis chips; wide: side-by-side with vertical chips.
    /// Leaf metrics use <see cref="UserInterface.EzDisplaySkillsDan"/>.
    /// </summary>
    public partial class EzDanDualPanel : CompositeDrawable
    {
        private const float wide_threshold = 560f;

        public static readonly Colour4 RC_ACCENT = Colour4.FromHex("#e0b04c");
        public static readonly Colour4 LN_ACCENT = Colour4.FromHex("#f07474");

        private readonly Bindable<string?> username = new Bindable<string?>();

        private FillFlowContainer dualFlow = null!;
        private EzDanLabeledStatList rcList = null!;
        private EzDanLabeledStatList lnList = null!;
        private OsuSpriteText emptyHint = null!;

        private IReadOnlyList<EzLocalProfileDrillScoreRow>? drillScores;
        private Action<EzLocalProfileDrillScoreRow>? onSelectDrill;
        private bool includeChartAxes;
        private bool preferPlayerSsr;

        private readonly LayoutValue sizeLayout = new LayoutValue(Invalidation.DrawSize);

        [Resolved]
        private EzSkillProvider skillProvider { get; set; } = null!;

        [Resolved(canBeNull: true)]
        private IBindable<WorkingBeatmap>? beatmap { get; set; }

        [Resolved(canBeNull: true)]
        private IBindable<IReadOnlyList<Mod>>? mods { get; set; }

        public BindableInt KeyCount { get; } = new BindableInt();

        public EzDanDualPanel()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            AddLayout(sizeLayout);
        }

        /// <param name="drills">Optional drill rows for evidence titles / click-through.</param>
        /// <param name="selectDrill">Optional handler when a clear evidence row is activated.</param>
        /// <param name="showChartAxes">Fill axis chips from chart MSD when beatmap available (wedge).</param>
        /// <param name="usePlayerSsr">Prefer player SSR for axis values when username set (Track).</param>
        public void Configure(
            IReadOnlyList<EzLocalProfileDrillScoreRow>? drills = null,
            Action<EzLocalProfileDrillScoreRow>? selectDrill = null,
            bool showChartAxes = false,
            bool usePlayerSsr = false)
        {
            drillScores = drills;
            onSelectDrill = selectDrill;
            includeChartAxes = showChartAxes;
            preferPlayerSsr = usePlayerSsr;
            Schedule(refresh);
        }

        public void BindUsername(Bindable<string?> source) => username.BindTo(source);

        public void SetUsername(string? value) => username.Value = value;

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colours)
        {
            InternalChildren = new Drawable[]
            {
                emptyHint = new OsuSpriteText
                {
                    RelativeSizeAxes = Axes.X,
                    Font = OsuFont.GetFont(size: 13),
                    Colour = colours.Content2,
                    Text = EzSettingsProfile.LOCAL_PROFILE_TRACK_NEEDS_PLAYER,
                    Alpha = 0,
                },
                dualFlow = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Spacing = new Vector2(0, 12),
                    Children = new Drawable[]
                    {
                        rcList = new EzDanLabeledStatList(EzDanSide.Rc, RC_ACCENT)
                        {
                            RelativeSizeAxes = Axes.X,
                            Width = 1,
                        },
                        lnList = new EzDanLabeledStatList(EzDanSide.Ln, LN_ACCENT)
                        {
                            RelativeSizeAxes = Axes.X,
                            Width = 1,
                        },
                    },
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            username.BindValueChanged(_ => refresh(), true);
            KeyCount.BindValueChanged(_ => refresh());

            beatmap?.BindValueChanged(_ => refresh());
            mods?.BindValueChanged(_ => refresh());
        }

        protected override void Update()
        {
            base.Update();

            if (!sizeLayout.IsValid)
            {
                sizeLayout.Validate();
                applyLayoutMode(DrawWidth >= wide_threshold);
            }
        }

        private void applyLayoutMode(bool wide)
        {
            if (wide)
            {
                dualFlow.Direction = FillDirection.Horizontal;
                dualFlow.Spacing = Vector2.Zero;
                rcList.RelativeSizeAxes = Axes.X;
                lnList.RelativeSizeAxes = Axes.X;
                rcList.Width = 0.5f;
                lnList.Width = 0.5f;
                rcList.SetLayoutInset(new MarginPadding { Right = 8 });
                lnList.SetLayoutInset(new MarginPadding { Left = 8 });
                rcList.SetCellsDirection(FillDirection.Vertical);
                lnList.SetCellsDirection(FillDirection.Vertical);
            }
            else
            {
                dualFlow.Direction = FillDirection.Vertical;
                dualFlow.Spacing = new Vector2(0, 12);
                rcList.RelativeSizeAxes = Axes.X;
                lnList.RelativeSizeAxes = Axes.X;
                rcList.Width = 1;
                lnList.Width = 1;
                rcList.SetLayoutInset(new MarginPadding());
                lnList.SetLayoutInset(new MarginPadding());
                rcList.SetCellsDirection(FillDirection.Horizontal);
                lnList.SetCellsDirection(FillDirection.Horizontal);
            }
        }

        private void refresh()
        {
            applyLayoutMode(DrawWidth >= wide_threshold);

            int keys = KeyCount.Value;
            string? user = username.Value;

            // Chart-only wedge: allow empty user if we can still show MSD axes.
            bool hasUser = !string.IsNullOrWhiteSpace(user);
            bool canShowChart = includeChartAxes && beatmap?.Value.BeatmapInfo != null && keys > 0;
            bool canShowPlayer = hasUser && keys > 0 && preferPlayerSsr;

            if (keys <= 0 || (!hasUser && !canShowChart))
            {
                emptyHint.Text = EzSettingsProfile.LOCAL_PROFILE_TRACK_NEEDS_PLAYER;
                emptyHint.Show();
                dualFlow.Hide();
                return;
            }

            emptyHint.Hide();
            dualFlow.Show();

            IReadOnlyDictionary<string, double> axisValues = new Dictionary<string, double>();

            if (canShowPlayer)
            {
                axisValues = skillProvider.GetPlayerSsr(user!, keys);
            }
            else if (canShowChart)
            {
                var modsList = mods?.Value ?? Array.Empty<Mod>();
                var cached = skillProvider.GetBeatmapMsd(beatmap!.Value.BeatmapInfo.Hash);

                if (cached.Count > 0)
                {
                    axisValues = cached;
                }
                else
                {
                    skillProvider.TryGetChartDan(beatmap.Value.BeatmapInfo, modsList);
                    axisValues = skillProvider.GetBeatmapMsd(beatmap.Value.BeatmapInfo.Hash);
                }
            }
            else if (hasUser)
            {
                axisValues = skillProvider.GetPlayerSsr(user!, keys);
            }

            updateSide(rcList, EzDanSide.Rc, user, keys, axisValues);
            updateSide(lnList, EzDanSide.Ln, user, keys, axisValues);
        }

        private void updateSide(
            EzDanLabeledStatList list,
            EzDanSide side,
            string? user,
            int keys,
            IReadOnlyDictionary<string, double> axisValues)
        {
            EzDanEstimate? estimate = null;
            IReadOnlyList<EzDanClearEvidenceRow> clears = Array.Empty<EzDanClearEvidenceRow>();

            if (!string.IsNullOrWhiteSpace(user))
            {
                estimate = skillProvider.GetDan(user, keys, side.ToId());
                clears = skillProvider.GetDanClears(user, keys, side.ToId(), EzDanAlgorithm.VERSION);
            }

            // Chart-side aggregate when no player estimate but chart axes shown.
            if (estimate == null && includeChartAxes && beatmap?.Value.BeatmapInfo != null)
            {
                var modsList = mods?.Value ?? Array.Empty<Mod>();
                var chart = skillProvider.TryGetChartDan(beatmap.Value.BeatmapInfo, modsList)
                            ?? skillProvider.TryGetCachedChartDan(beatmap.Value.BeatmapInfo);

                if (chart != null && chart.KeyCount == keys && chart.Side == side && !string.IsNullOrEmpty(chart.Label))
                {
                    estimate = new EzDanEstimate
                    {
                        Label = chart.Label,
                        RawDan = chart.RawDan,
                        Clears = 0,
                        Side = side.ToId(),
                        KeyCount = keys,
                    };
                }
            }

            list.UpdateContent(keys, estimate, axisValues, clears, resolveTitle, onSelectClear);
        }

        private string resolveTitle(string beatmapHash)
        {
            if (string.IsNullOrEmpty(beatmapHash) || drillScores == null)
                return EzSettingsProfile.LOCAL_PROFILE_AXIS_UNKNOWN_MAP.ToString();

            var drill = drillScores
                        .Where(r => string.Equals(r.BeatmapHash, beatmapHash, StringComparison.Ordinal))
                        .OrderByDescending(r => r.PpResolved)
                        .ThenByDescending(r => r.Date)
                        .FirstOrDefault();

            if (drill == null)
                return EzSettingsProfile.LOCAL_PROFILE_AXIS_UNKNOWN_MAP.ToString();

            if (string.IsNullOrEmpty(drill.Artist))
            {
                return string.IsNullOrEmpty(drill.DifficultyName)
                    ? drill.Title
                    : $"{drill.Title} [{drill.DifficultyName}]";
            }

            return string.IsNullOrEmpty(drill.DifficultyName)
                ? $"{drill.Artist} - {drill.Title}"
                : $"{drill.Artist} - {drill.Title} [{drill.DifficultyName}]";
        }

        private void onSelectClear(EzDanClearEvidenceRow clear)
        {
            if (onSelectDrill == null || drillScores == null)
                return;

            var drill = drillScores
                        .Where(r => string.Equals(r.BeatmapHash, clear.BeatmapHash, StringComparison.Ordinal))
                        .OrderByDescending(r => r.PpResolved)
                        .ThenByDescending(r => r.Date)
                        .FirstOrDefault();

            if (drill != null)
                onSelectDrill(drill);
        }
    }
}
