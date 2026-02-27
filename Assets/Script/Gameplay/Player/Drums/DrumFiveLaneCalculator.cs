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
using ColorProfileIndex = YARG.Core.Game.ColorProfile.FiveLaneDrumsColors.ColorProfileIndex;

namespace YARG.Gameplay.Player.Drums
{
    public class DrumFiveLaneCalculator : IDrumLaneCalculator
    {
        private YargPlayer Player { get; }
        public int FretCount => 5;

        public bool IsFiveLaneMode => true;
        private bool ShouldSwapSnareAndHiHat => Player.Profile.SwapSnareAndHiHat;
        private bool LeftyFlip => Player.Profile.LeftyFlip;

        public DrumFiveLaneCalculator(YargPlayer player)
        {
            Player = player;
        }

#region GetFret methods
        public int GetFret(DrumsAction action)
        {
            var pad = action switch
            {
                DrumsAction.RedDrum => (int)FiveLaneDrumPad.Red,
                DrumsAction.YellowCymbal => (int)FiveLaneDrumPad.Yellow,
                DrumsAction.BlueDrum => (int)FiveLaneDrumPad.Blue,
                DrumsAction.OrangeCymbal => (int)FiveLaneDrumPad.Orange,
                DrumsAction.GreenDrum => (int)FiveLaneDrumPad.Green,
                _ => -1,
            };
            return GetFret(pad);
        }

        public int GetFret(int pad)
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
#endregion

#region GetLaneIndex methods
        public int GetDisplayLane(int pad)
        {
            if (ShouldSwapSnareAndHiHat)
            {
                if(pad == (int)FiveLaneDrumPad.Red || pad == (int)FiveLaneDrumPad.Yellow)
                {
                    return pad == (int)FiveLaneDrumPad.Red ? (int)FiveLaneDrumPad.Yellow : (int)FiveLaneDrumPad.Red;
                }
            }
            return pad;
        }
#endregion

#region Colors
        public int[] GetDrumLaneColors()
        {
            ColorProfileIndex[] drumOrder = {
                ColorProfileIndex.RedDrum,
                ColorProfileIndex.YellowDrum,
                ColorProfileIndex.BlueDrum,
                ColorProfileIndex.OrangeDrum,
                ColorProfileIndex.GreenDrum
            };
            if(LeftyFlip)
            {
                drumOrder = drumOrder.Reverse().ToArray();
            }
            if(ShouldSwapSnareAndHiHat)
            {
                (drumOrder[0], drumOrder[1]) = (drumOrder[1], drumOrder[0]);
            }
            return new[] { ColorProfileIndex.Kick }.Concat(drumOrder).Cast<int>().ToArray();
        }

        public int GetPadColorIndex(int pad)
        {
            var color = (FiveLaneDrumPad)pad switch
            {
                FiveLaneDrumPad.Kick   => ColorProfileIndex.Kick,
                FiveLaneDrumPad.Red    => ColorProfileIndex.RedDrum,
                FiveLaneDrumPad.Yellow => ColorProfileIndex.YellowDrum,
                FiveLaneDrumPad.Blue   => ColorProfileIndex.BlueDrum,
                FiveLaneDrumPad.Orange => ColorProfileIndex.OrangeDrum,
                FiveLaneDrumPad.Green  => ColorProfileIndex.GreenDrum,
                _ => ColorProfileIndex.RedDrum,
            };
            return LeftyFlip ? (int)UpdateColorForLeftyFlip(color) : (int)color;
        }

        private ColorProfileIndex UpdateColorForLeftyFlip(ColorProfileIndex color)
        {
            // When LeftyMode is enabled, the lanes are switched visually, but not internally.
            // This leads to the colors being wrong, so we need to swap them around.
            return color switch
            {
                ColorProfileIndex.RedDrum => ColorProfileIndex.GreenDrum,
                ColorProfileIndex.YellowDrum => ColorProfileIndex.OrangeDrum,
                ColorProfileIndex.OrangeDrum => ColorProfileIndex.YellowDrum,
                ColorProfileIndex.GreenDrum => ColorProfileIndex.RedDrum,
                _ => color,
            };
        }
#endregion
    }
}
