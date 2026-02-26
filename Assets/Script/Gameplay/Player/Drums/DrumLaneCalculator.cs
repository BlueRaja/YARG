using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YARG.Core;
using YARG.Core.Audio;
using YARG.Core.Chart;
using YARG.Core.Engine.Drums;
using YARG.Core.Engine.Drums.Engines;
using YARG.Core.Game;
using YARG.Core.Input;
using YARG.Core.Replays;
using YARG.Gameplay.HUD;
using YARG.Gameplay.Visuals;
using YARG.Helpers.Extensions;
using YARG.Player;
using YARG.Settings;
using YARG.Themes;
using ColorProfileIndex = YARG.Core.Game.ColorProfile.FourLaneDrumsColors.ColorProfileIndex;

namespace YARG.Gameplay.Player.Drums
{
    public class DrumLaneCalculator
    {
        private YargPlayer Player { get; }
        public bool IsFiveLaneMode => Player.Profile.CurrentInstrument == Instrument.FiveLaneDrums;
        public bool IsSplitMode => Player.Profile.CurrentInstrument is Instrument.ProDrums && Player.Profile.SplitProTomsAndCymbals;
        private bool ShouldSwapSnareAndHiHat => (IsFiveLaneMode || IsSplitMode) && Player.Profile.SwapSnareAndHiHat;
        private bool ShouldSwapCrashAndRide => IsSplitMode && Player.Profile.SwapCrashAndRide;
        private bool LeftyFlip => Player.Profile.LeftyFlip;

        public DrumLaneCalculator(YargPlayer player)
        {
            Player = player;
        }

        public int FretCount => IsFiveLaneMode ? 5 : IsSplitMode ? 7 : 4;

#region GetFret methods
        public int GetFret(DrumsAction action)
        {
            var pad = action switch
            {
                DrumsAction.RedDrum => (int)FourLaneDrumPad.RedDrum,
                DrumsAction.YellowDrum => (int)FourLaneDrumPad.YellowDrum,
                DrumsAction.BlueDrum => (int)FourLaneDrumPad.BlueDrum,
                DrumsAction.GreenDrum => (int)FourLaneDrumPad.GreenDrum,
                DrumsAction.YellowCymbal => (int)FourLaneDrumPad.YellowCymbal,
                DrumsAction.BlueCymbal => (int)FourLaneDrumPad.BlueCymbal,
                DrumsAction.GreenCymbal => (int)FourLaneDrumPad.GreenCymbal,
                _ => -1,
            };
            return GetFret(pad);
        }

        public int GetFret(int pad)
        {
            if (IsFiveLaneMode)
            {
                return GetFiveLaneFret(pad);
            }

            if (IsSplitMode)
            {
                return GetSplitFret(pad);
            }

            return GetFourLaneFret(pad);
        }

        private static int GetFourLaneFret(int pad)
        {
            return (FourLaneDrumPad) pad switch
            {
                FourLaneDrumPad.RedDrum                                    => 0,
                FourLaneDrumPad.YellowDrum or FourLaneDrumPad.YellowCymbal => 1,
                FourLaneDrumPad.BlueDrum or FourLaneDrumPad.BlueCymbal     => 2,
                FourLaneDrumPad.GreenDrum or FourLaneDrumPad.GreenCymbal   => 3,
                _                                                          => -1,
            };
        }

        private static int GetFiveLaneFret(int pad)
        {
            return (FiveLaneDrumPad) pad switch
            {
                FiveLaneDrumPad.Red    => 0,
                FiveLaneDrumPad.Yellow => 1,
                FiveLaneDrumPad.Blue   => 2,
                FiveLaneDrumPad.Orange => 3,
                FiveLaneDrumPad.Green  => 4,
                _                      => -1,
            };
        }

        private static int GetSplitFret(int pad)
        {
            return (FourLaneDrumPad) pad switch
            {
                FourLaneDrumPad.RedDrum      => 0,
                FourLaneDrumPad.YellowCymbal => 1,
                FourLaneDrumPad.YellowDrum   => 2,
                FourLaneDrumPad.BlueCymbal   => 3,
                FourLaneDrumPad.BlueDrum     => 4,
                FourLaneDrumPad.GreenCymbal  => 5,
                FourLaneDrumPad.GreenDrum    => 6,
                _                            => -1,
            };
        }
#endregion

#region GetLaneIndex methods
        /// <summary>
        /// Returns the three display lane indices (1-4) for the three cymbals in 4-lane (non-split) mode.
        /// LanesToShowCymbals only; no swap (swaps apply only in split mode).
        /// </summary>
        private (int leftCymbalLane, int midCymbalLane, int rightCymbalLane) GetDefaultCymbalLanesForNonSplitMode()
        {
            return Player.Profile.LanesToShowCymbals switch
            {
                LanesToShowCymbals.Lanes234 => (2, 3, 4),
                LanesToShowCymbals.Lanes134 => (1, 3, 4),
                LanesToShowCymbals.Lanes124 => (1, 2, 4),
                LanesToShowCymbals.Lanes123 => (1, 2, 3),
                _ => (2, 3, 4),
            };
        }

        private (int leftCymbalLane, int midCymbalLane, int rightCymbalLane) GetDefaultCymbalLanesForSplitMode()
        {
            return Player.Profile.LanesToShowCymbals switch
            {
                LanesToShowCymbals.Lanes234 => (2, 4, 6),
                LanesToShowCymbals.Lanes134 => (1, 4, 6),
                LanesToShowCymbals.Lanes124 => (1, 3, 6),
                LanesToShowCymbals.Lanes123 => (1, 3, 5),
                _ => (2, 4, 6),
            };
        }

        /// <summary>
        /// Returns the three display lane indices (1-7) for the three cymbals (Yellow, Blue, Green)
        /// based on LanesToShowCymbals, then applies SwapSnareAndHiHat and SwapCrashAndRide.
        /// </summary>
        private (int leftCymbalLane, int midCymbalLane, int rightCymbalLane) GetCymbalDisplayLanes()
        {
            var (leftCymbalLane, midCymbalLane, rightCymbalLane) = IsSplitMode
                ? GetDefaultCymbalLanesForSplitMode()
                : GetDefaultCymbalLanesForNonSplitMode();

            if (ShouldSwapSnareAndHiHat)
            {
                leftCymbalLane = leftCymbalLane == 2 ? 1 : 2;
            }

            if (ShouldSwapCrashAndRide)
            {
                (midCymbalLane, rightCymbalLane) = (rightCymbalLane, midCymbalLane);
            }

            return (leftCymbalLane, midCymbalLane, rightCymbalLane);
        }

        private (int redDrum, int yellowDrum, int blueDrum, int greenDrum) GetDefaultDrumLanesForNonSplitMode()
        {
            return (1, 2, 3, 4);
        }

        private (int redDrum, int yellowDrum, int blueDrum, int greenDrum) GetDefaultDrumLanesForSplitMode()
        {
            return Player.Profile.LanesToShowCymbals switch
            {
                LanesToShowCymbals.Lanes234 => (1, 3, 5, 7),
                LanesToShowCymbals.Lanes134 => (2, 3, 5, 7),
                LanesToShowCymbals.Lanes124 => (2, 4, 5, 7),
                LanesToShowCymbals.Lanes123 => (2, 4, 6, 7),
                _ => (1, 3, 5, 7),
            };
        }

        private (int redDrum, int yellowDrum, int blueDrum, int greenDrum) GetDrumDisplayLanes()
        {
            var (redDrum, yellowDrum, blueDrum, greenDrum) = IsSplitMode
                ? GetDefaultDrumLanesForSplitMode()
                : GetDefaultDrumLanesForNonSplitMode();
            if (ShouldSwapSnareAndHiHat)
            {
                redDrum = redDrum == 2 ? 1 : 2;
            }
            return (redDrum, yellowDrum, blueDrum, greenDrum);
        }

        public int GetDisplayLane(int pad)
        {
            var (leftCymbalLane, midCymbalLane, rightCymbalLane) = GetCymbalDisplayLanes();
            var (redDrum, yellowDrum, blueDrum, greenDrum) = GetDrumDisplayLanes();

            int index = (FourLaneDrumPad)pad switch
            {
                FourLaneDrumPad.RedDrum      => redDrum,
                FourLaneDrumPad.YellowDrum   => yellowDrum,
                FourLaneDrumPad.BlueDrum     => blueDrum,
                FourLaneDrumPad.GreenDrum    => greenDrum,
                FourLaneDrumPad.YellowCymbal => leftCymbalLane,
                FourLaneDrumPad.BlueCymbal   => midCymbalLane,
                FourLaneDrumPad.GreenCymbal  => rightCymbalLane,
                _ => -1,
            };

            return index;
        }
#endregion

#region Colors
        public ColorProfileIndex[] GetDrumLaneColors()
        {
            if(IsFiveLaneMode)
            {
                // Small hack: The two ColorProfileIndex enums aren't technically swappable, but they're immediately being passed to the
                // correct ColorProfile so it doesn't really matter
                return Get5LaneDrumLaneColors().Cast<ColorProfileIndex>().ToArray();
            }
            
            var drums = new[] { (int)FourLaneDrumPad.RedDrum, (int)FourLaneDrumPad.YellowDrum, (int)FourLaneDrumPad.BlueDrum, (int)FourLaneDrumPad.GreenDrum };
            var cymbals = new[] { (int)FourLaneDrumPad.YellowCymbal, (int)FourLaneDrumPad.BlueCymbal, (int)FourLaneDrumPad.GreenCymbal };
            var pads = IsSplitMode
                ? drums.Concat(cymbals).ToArray()
                : drums;
            var colorsByLane = pads.ToDictionary(pad => GetDisplayLane(pad), GetPadColorIndex);
            return Enumerable.Range(0, FretCount + 1).Select(i => colorsByLane.GetValueOrDefault(i)).ToArray();
        }

        private ColorProfile.FiveLaneDrumsColors.ColorProfileIndex[] Get5LaneDrumLaneColors()
        {
            ColorProfile.FiveLaneDrumsColors.ColorProfileIndex[] drumOrder = {
                ColorProfile.FiveLaneDrumsColors.ColorProfileIndex.RedDrum,
                ColorProfile.FiveLaneDrumsColors.ColorProfileIndex.YellowDrum,
                ColorProfile.FiveLaneDrumsColors.ColorProfileIndex.BlueDrum,
                ColorProfile.FiveLaneDrumsColors.ColorProfileIndex.OrangeDrum,
                ColorProfile.FiveLaneDrumsColors.ColorProfileIndex.GreenDrum
            };
            if(LeftyFlip)
            {
                drumOrder = drumOrder.Reverse().ToArray();
            }
            if(ShouldSwapSnareAndHiHat)
            {
                (drumOrder[0], drumOrder[1]) = (drumOrder[1], drumOrder[0]);
            }
            return new[] { ColorProfile.FiveLaneDrumsColors.ColorProfileIndex.Kick }.Concat(drumOrder).ToArray();
        }

        public ColorProfileIndex GetPadColorIndex(int pad)
        {
            var (leftCymbalColor, midCymbalColor, rightCymbalColor) = GetCymbalColors();
            var color = (FourLaneDrumPad)pad switch
            {
                FourLaneDrumPad.Kick         => ColorProfileIndex.Kick,
                FourLaneDrumPad.RedDrum      => ColorProfileIndex.RedDrum,
                FourLaneDrumPad.YellowDrum   => ColorProfileIndex.YellowDrum,
                FourLaneDrumPad.BlueDrum     => ColorProfileIndex.BlueDrum,
                FourLaneDrumPad.GreenDrum    => ColorProfileIndex.GreenDrum,
                FourLaneDrumPad.YellowCymbal => leftCymbalColor,
                FourLaneDrumPad.BlueCymbal   => midCymbalColor,
                FourLaneDrumPad.GreenCymbal  => rightCymbalColor,
                _ => ColorProfileIndex.RedDrum,
            };
            return LeftyFlip ? UpdateColorForLeftyFlip(color) : color;
        }

        private (ColorProfileIndex leftCymbalColor, ColorProfileIndex midCymbalColor, ColorProfileIndex rightCymbalColor) GetCymbalColors()
        {
            return Player.Profile.LanesToShowCymbals switch
            {
                LanesToShowCymbals.Lanes234 => (ColorProfileIndex.YellowCymbal, ColorProfileIndex.BlueCymbal, ColorProfileIndex.GreenCymbal),
                LanesToShowCymbals.Lanes134 => (ColorProfileIndex.RedCymbal, ColorProfileIndex.BlueCymbal, ColorProfileIndex.GreenCymbal),
                LanesToShowCymbals.Lanes124 => (ColorProfileIndex.RedCymbal, ColorProfileIndex.YellowCymbal, ColorProfileIndex.GreenCymbal),
                LanesToShowCymbals.Lanes123 => (ColorProfileIndex.RedCymbal, ColorProfileIndex.YellowCymbal, ColorProfileIndex.BlueCymbal),
                _ => (ColorProfileIndex.YellowCymbal, ColorProfileIndex.BlueCymbal, ColorProfileIndex.GreenCymbal),
            };
        }

        private ColorProfileIndex UpdateColorForLeftyFlip(ColorProfileIndex color)
        {
            // When LeftyMode is enabled, the lanes are switched visually, but not internally.
            // This leads to the colors being wrong, so we need to swap them around.
            return color switch
            {
                ColorProfileIndex.RedDrum => ColorProfileIndex.GreenDrum,
                ColorProfileIndex.YellowDrum => ColorProfileIndex.BlueDrum,
                ColorProfileIndex.BlueDrum => ColorProfileIndex.YellowDrum,
                ColorProfileIndex.GreenDrum => ColorProfileIndex.RedDrum,

                // We still associate each cymbal with the drum to its right, so the color associations
                // end up different for cymbals than drums
                ColorProfileIndex.RedCymbal => ColorProfileIndex.GreenCymbal,
                ColorProfileIndex.YellowCymbal => ColorProfileIndex.BlueCymbal,
                ColorProfileIndex.BlueCymbal => ColorProfileIndex.YellowCymbal,
                ColorProfileIndex.GreenCymbal => ColorProfileIndex.RedCymbal, 
                _ => color,
            };
        }
#endregion
    }
}
