// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Layout;
using osu.Game.EzOsuGame.Localization;
using osu.Game.EzOsuGame.LocalProfile;
using osu.Game.EzOsuGame.Skills.Dan;
using osu.Game.EzOsuGame.UserInterface;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osuTK;

namespace osu.Game.EzOsuGame.Skills
{
    /// <summary>
    /// One RC/LN side: label + optional aggregate dan + full radar-axis <see cref="EzDisplaySkillsDan"/> + clear evidence.
    /// </summary>
    public partial class EzDanLabeledStatList : CompositeDrawable
    {
        private const int clears_top_n = 15;

        private readonly EzDanSide side;
        private readonly Colour4 accent;

        private FillFlowContainer headerFlow = null!;
        private OsuSpriteText sideLabel = null!;
        private EzDisplayDan aggregateDan = null!;
        private OsuSpriteText clearsText = null!;
        private FillFlowContainer cellsFlow = null!;
        private FillFlowContainer evidenceFlow = null!;
        private OsuSpriteText emptyEvidence = null!;

        private FillDirection cellsDirection = FillDirection.Horizontal;
        private readonly LayoutValue sizeLayout = new LayoutValue(Invalidation.DrawSize);

        private readonly Dictionary<EzMinaSkillAxis, EzDisplaySkillsDan> axisChips = new Dictionary<EzMinaSkillAxis, EzDisplaySkillsDan>();

        public EzDanLabeledStatList(EzDanSide side, Colour4 accent)
        {
            this.side = side;
            this.accent = accent;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            AddLayout(sizeLayout);
        }

        public void SetLayoutInset(MarginPadding padding) => Padding = padding;

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colours)
        {
            foreach (var axis in EzMinaSkillAxisExtensions.RadarAxes)
            {
                axisChips[axis] = new EzDisplaySkillsDan
                {
                    PreferDanImage = true,
                    ShowValue = true,
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Alpha = 0,
                };
            }

            InternalChild = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 8),
                Children = new Drawable[]
                {
                    headerFlow = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(8, 0),
                        Children = new Drawable[]
                        {
                            sideLabel = new OsuSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Font = OsuFont.GetFont(size: 14, weight: FontWeight.Bold),
                                Colour = accent,
                            },
                            aggregateDan = new EzDisplayDan
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                BadgeSize = 20,
                                PreferImage = true,
                                Alpha = 0,
                            },
                            clearsText = new OsuSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Font = OsuFont.GetFont(size: 11, weight: FontWeight.SemiBold),
                                Colour = colours.Content2,
                                Alpha = 0,
                            },
                        },
                    },
                    cellsFlow = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = cellsDirection,
                        Spacing = new Vector2(10, 8),
                    },
                    emptyEvidence = new OsuSpriteText
                    {
                        RelativeSizeAxes = Axes.X,
                        Font = OsuFont.GetFont(size: 12),
                        Colour = colours.Content2,
                        Text = EzSettingsProfile.LOCAL_PROFILE_DAN_CLEARS_EMPTY,
                        Alpha = 0,
                    },
                    evidenceFlow = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Full,
                        Spacing = new Vector2(8),
                    },
                },
            };

            foreach (var chip in axisChips.Values)
                cellsFlow.Add(chip);

            sideLabel.Text = side == EzDanSide.Ln
                ? EzSettingsProfile.LOCAL_PROFILE_DAN_SIDE_LN
                : EzSettingsProfile.LOCAL_PROFILE_DAN_SIDE_RC;
        }

        public void SetCellsDirection(FillDirection direction)
        {
            cellsDirection = direction;

            if (LoadState >= LoadState.Ready)
                cellsFlow.Direction = direction;
        }

        public void UpdateContent(
            int keyCount,
            EzDanEstimate? estimate,
            IReadOnlyDictionary<string, double> axisValues,
            IReadOnlyList<EzDanClearEvidenceRow> clears,
            Func<string, string> resolveTitle,
            Action<EzDanClearEvidenceRow>? onSelectClear)
        {
            if (LoadState < LoadState.Ready)
            {
                Schedule(() => UpdateContent(keyCount, estimate, axisValues, clears, resolveTitle, onSelectClear));
                return;
            }

            cellsFlow.Direction = cellsDirection;

            sideLabel.Text = side == EzDanSide.Ln
                ? EzSettingsProfile.LOCAL_PROFILE_DAN_SIDE_LN
                : EzSettingsProfile.LOCAL_PROFILE_DAN_SIDE_RC;

            if (estimate != null && !string.IsNullOrEmpty(estimate.Label) && estimate.RawDan >= 0)
            {
                aggregateDan.SetLabel(estimate.Label, keyCount, side);
                aggregateDan.Show();
            }
            else
            {
                aggregateDan.Hide();
            }

            if (estimate is { Clears: > 0 })
            {
                clearsText.Text = $"{EzSettingsProfile.LOCAL_PROFILE_DAN_CELL_CLEARS}: {estimate.Clears.ToString(CultureInfo.InvariantCulture)}";
                clearsText.Show();
            }
            else
            {
                clearsText.Hide();
            }

            var ladder = EzDanLadders.For(keyCount, side);

            foreach (var axis in EzMinaSkillAxisExtensions.RadarAxes)
            {
                var chip = axisChips[axis];
                string skillIdMsd = axis.ToMsdSkillId();
                string skillIdSsr = axis.ToSsrSkillId();

                double value = 0;
                if (axisValues.TryGetValue(skillIdMsd, out double msd) && msd > 0)
                    value = msd;
                else if (axisValues.TryGetValue(skillIdSsr, out double ssr) && ssr > 0)
                    value = ssr;
                else if (axisValues.TryGetValue(axis.ToId(), out double bare) && bare > 0)
                    value = bare;

                if (value <= 0 || !double.IsFinite(value))
                {
                    chip.Clear();
                    continue;
                }

                double rawDan = EzDanLabels.SrToRawDan(value, axis);
                string label = ladder.ParseLabel(rawDan);
                chip.Set(axis, label, value, keyCount, side);
            }

            rebuildEvidence(clears, resolveTitle, onSelectClear);
        }

        private void rebuildEvidence(
            IReadOnlyList<EzDanClearEvidenceRow> clears,
            Func<string, string> resolveTitle,
            Action<EzDanClearEvidenceRow>? onSelectClear)
        {
            evidenceFlow.Clear();

            var top = clears.Take(clears_top_n).ToList();

            if (top.Count == 0)
            {
                emptyEvidence.Show();
                evidenceFlow.Hide();
                return;
            }

            emptyEvidence.Hide();
            evidenceFlow.Show();

            float colWidth = resolveEvidenceColumnWidth();

            foreach (var clear in top)
            {
                evidenceFlow.Add(new EzEvidenceScoreRow(
                    resolveTitle(clear.BeatmapHash),
                    EzEvidenceScoreRow.FormatDanMeta(clear.CreditedDan, clear.Accuracy, clear.Rate, clear.ScoredAt),
                    onSelectClear != null ? () => onSelectClear(clear) : null)
                {
                    Width = colWidth,
                    RelativeSizeAxes = Axes.None,
                    AutoSizeAxes = Axes.Y,
                });
            }
        }

        protected override void Update()
        {
            base.Update();

            if (!sizeLayout.IsValid)
            {
                sizeLayout.Validate();
                reflowEvidenceWidths();
            }
        }

        private void reflowEvidenceWidths()
        {
            float colWidth = resolveEvidenceColumnWidth();

            foreach (var child in evidenceFlow)
                child.Width = colWidth;
        }

        private float resolveEvidenceColumnWidth()
        {
            float width = DrawWidth;
            if (width <= 0)
                width = 400;

            int cols = width switch
            {
                < 280 => 1,
                < 420 => 2,
                < 560 => 3,
                _ => 4,
            };

            const float gap = 8f;
            return Math.Max(80, (width - gap * (cols - 1)) / cols);
        }
    }
}
