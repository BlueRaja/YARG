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
        public int FretCount { get; }
        public bool IsFiveLaneMode { get; }

        public int GetFret(DrumsAction action);
        public int GetFret(int pad);

        public int GetDisplayLane(int pad);

        public int[] GetDrumLaneColors();
        public int GetPadColorIndex(int pad);
    }
}
