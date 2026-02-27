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

namespace YARG.Gameplay.Player.Drums
{
    public interface IDrumLaneCalculator
    {
        /** Number of frets/display lanes to show. Does not include the kick. **/
        public int FretCount { get; }

        /** True for five-lane drums, false for four-lane and four-lane pro drums. **/
        public bool IsFiveLaneMode { get; }

        /** Returns the fret index for a DrumAction. Used to index into FretArray. Same as the display lane, except 0-based. **/
        public int GetFret(DrumsAction action);

        /** Returns the fret index for a given drum pad. Used to index into FretArray. Same as the display lane, except 0-based. **/
        public int GetFret(int pad);

        /**
         * Returns the 1-indexed display lane for a given drum pad.
         * Includes the options to swap various lanes. It does NOT include the lane reversal done by LeftyFlip.
         * That flipping is purely visual, and is done by FretArray.
         **/
        public int GetDisplayLane(int pad);

        /**
         * Returns an array of length FretCount+1 of colors for the drum lanes.
         * The first element is the kick color, and the rest are the 1-indexed display lanes.
         * The int values returned are the ColorProfileIndex values defined in the ColorProfile classes.
         * Note that when LeftyFlip is enabled, this method reverses the colors. This is because the lanes are switched visually,
         * but we still want eg. the red lane to be the first lane, even when the snare is on the right.
         **/
        public int[] GetDrumLaneColors();

        /**
         * Returns the color index for a given drum pad.
         * See GetDrumLaneColors() for various gotchas.
         **/
        public int GetPadColorIndex(int pad);
    }
}
