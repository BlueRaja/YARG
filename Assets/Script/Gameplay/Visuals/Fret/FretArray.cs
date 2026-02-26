using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YARG.Core;
using YARG.Core.Chart;
using YARG.Core.Game;
using YARG.Core.Logging;
using YARG.Themes;
using Color = System.Drawing.Color;
using static YARG.Themes.ThemeManager;

namespace YARG.Gameplay.Visuals
{
    public class FretArray : MonoBehaviour
    {
        private const float WIDTH_NUMERATOR   = 2f;
        private const float WIDTH_DENOMINATOR = 5f;

        public int FretCount;
        public bool UseKickFrets;

        [SerializeField]
        private float _trackWidth = 2f;

        [Space]
        [SerializeField]
        private Transform _leftKickFretPosition;
        [SerializeField]
        private Transform _rightKickFretPosition;

        private readonly List<Fret> _frets = new();
        private readonly List<KickFret> _kickFrets = new();

        private bool[] _activeFrets;
        private bool[] _pulsingFrets;
        private float  _pulseDuration;

        public void Initialize(ThemePreset themePreset, VisualStyle style,
            ColorProfile.IFretColorProvider fretColorProvider, bool leftyFlip)
        {
            var range = Enumerable.Range(1, FretCount);
            // Slight hack: Prepend a useless 0 because the frets are 1-indexed
            var fretIndices = new[] { 0 }.Concat(leftyFlip ? range.Reverse() : range);
            Initialize(themePreset, style, 
                fretIndices.Select(fretColorProvider.GetFretColor).ToArray(),
                fretIndices.Select(fretColorProvider.GetFretInnerColor).ToArray(),
                fretIndices.Select(fretColorProvider.GetParticleColor).ToArray(), 
                fretIndices.Select(i => fretColorProvider.GetParticleColor(0 /* open note */)).ToArray(),
                fretColorProvider.GetFretColor(0),
                leftyFlip);
        }

        /**
         * Initializes the fret array with the given colors.
         * Colors are assumed to have already been flipped for lefty mode.
         **/
        public void Initialize(ThemePreset themePreset, VisualStyle style,
            Color[] fretColors, Color[] fretInnerColors, Color[] fretParticleColors,
            Color[] fretOpenParticleColors, Color kickColor, bool leftyFlip)
        {
            var fretPrefab = ThemeManager.Instance.CreateFretPrefabFromTheme(
                themePreset, style);

            // Spawn in normal frets
            _frets.Clear();
            for (int fretNumber = 0; fretNumber < FretCount; fretNumber++)
            {
                // Spawn
                var fret = Instantiate(fretPrefab, transform);
                fret.SetActive(true);

                // Position
                float x = _trackWidth / FretCount * fretNumber - _trackWidth / 2f + 1f / FretCount;
                fret.transform.localPosition = new Vector3(leftyFlip ? -x : x, 0f, 0f);

                // Scale
                float scale = (_trackWidth / WIDTH_NUMERATOR) / (FretCount / WIDTH_DENOMINATOR);
                fret.transform.localScale = new Vector3(scale, 1f, 1f);

                // Add
                var fretComp = fret.GetComponent<Fret>();
                _frets.Add(fretComp);
            }

            _kickFrets.Clear();
            if (UseKickFrets)
            {
                var kickFretPrefab = ThemeManager.Instance.CreateKickFretPrefabFromTheme(
                    themePreset, style);

                // Spawn in kick frets
                var leftKick = Instantiate(kickFretPrefab, transform);
                leftKick.SetActive(true);
                var rightKick = Instantiate(kickFretPrefab, transform);
                rightKick.SetActive(true);

                // Position kick frets
                leftKick.transform.localPosition = _leftKickFretPosition.localPosition;
                rightKick.transform.localPosition = _rightKickFretPosition.localPosition;
                rightKick.transform.localScale = rightKick.transform.localScale.InvertX();

                // Add kick frets
                _kickFrets.Add(leftKick.GetComponent<KickFret>());
                _kickFrets.Add(rightKick.GetComponent<KickFret>());
            }

            InitializeColor(fretColors, fretInnerColors, fretParticleColors, fretOpenParticleColors, kickColor);

            _activeFrets = new bool[FretCount];
            _pulsingFrets = new bool[FretCount];
            // Start with all frets active, they will be set inactive once TrackPlayer figures itself out
            for (int i = 0; i < FretCount; i++)
            {
                _activeFrets[i] = true;
            }
        }

        private void InitializeColor(Color[] fretColors, Color[] fretInnerColors, Color[] fretParticleColors,
            Color[] fretOpenParticleColors, Color kickColor)
        {
            if(fretColors.Length != FretCount + 1
                || fretInnerColors.Length != FretCount + 1
                || fretParticleColors.Length != FretCount + 1
                || fretOpenParticleColors.Length != FretCount + 1)
            {
                YargLogger.LogFormatError("Received inconsistent fret array. Got {0} colors, {1} inner colors, {2} particle colors, and {3} open particle colors, but expected {4}.", fretColors.Length, fretInnerColors.Length, fretParticleColors.Length, fretOpenParticleColors.Length, FretCount+1);
                return;
            }
            for (int i = 0; i < _frets.Count; i++)
            {
                _frets[i].Initialize(
                    fretColors[i+1],
                    fretInnerColors[i+1],
                    fretParticleColors[i+1],
                    fretOpenParticleColors[i+1]
                );
            }

            foreach (var kick in _kickFrets)
            {
                kick.Initialize(kickColor);
            }
        }

        public void SetPressed(int index, bool pressed)
        {
            _frets[index].SetPressed(pressed);
        }

        public void SetPressedDrum(int index, bool pressed, Fret.AnimType animType)
        {
            _frets[index].SetPressedDrum(pressed, animType);
        }

        public void SetSustained(int index, bool sustained)
        {
            _frets[index].SetSustained(sustained);
        }

        public void PlayHitAnimation(int index)
        {
            _frets[index].PlayHitAnimation();
            _frets[index].PlayHitParticles();
        }

        public void PlayCymbalHitAnimation(int index)
        {
            _frets[index].PlayCymbalHitAnimation();
            _frets[index].PlayHitParticles();
        }

        public void PlayOpenHitAnimation()
        {
            foreach (var fret in _frets)
            {
                fret.PlayHitAnimation();
                fret.PlayOpenHitParticles();
            }
        }

        public void PlayMissAnimation(int index)
        {
            if (0 <= index && index <= _frets.Count)
            {
                _frets[index].PlayMissAnimation();
                _frets[index].PlayMissParticles();
            }
        }

        public void PlayOpenMissAnimation()
        {
            foreach (var fret in _frets)
            {
                fret.PlayOpenMissAnimation();
                fret.PlayOpenMissParticles();
            }
        }

        public void PlayKickFretAnimation()
        {
            foreach (var kick in _kickFrets)
            {
                kick.PlayHitAnimation();
            }
        }

        public void ResetAll()
        {
            foreach (var fret in _frets)
            {
                fret.SetSustained(false);
            }
        }

        public void UpdateAccentColorState(int fretIndex, bool shouldWhiten)
        {
            if (shouldWhiten)
            {
                _frets[fretIndex].WhitenFretColor();
            }
            else
            {
                _frets[fretIndex].RestoreFretColor();
            }
        }

        public void SetFretColorPulse(int fretIndex, bool pulse, float duration)
        {
            _pulseDuration = duration;
            _pulsingFrets[fretIndex] = pulse;
        }

        public void PulseFretColors()
        {
            for (int i = 0; i < _pulsingFrets.Length; i++)
            {
                if (!_pulsingFrets[i] || _activeFrets[i])
                {
                    continue;
                }

                _frets[i].FadeColor(_pulseDuration, true, false);
            }
        }

        public void UpdateFretActiveState(bool[] frets)
        {
            // We should always receive the same number of frets that we actually have, but...
            if (frets.Length != _frets.Count)
            {
                YargLogger.LogFormatDebug("Received inconsistent fret array. Got {0} flags, but we have {1} frets.", frets.Length, _frets.Count);
                return;
            }

            for (int i = 0; i < _frets.Count; i++)
            {
                if (_activeFrets[i] != frets[i])
                {
                    if (frets[i])
                    {
                        _frets[i].ResetColor(true);
                    }
                    else
                    {
                        _frets[i].DimColor(true);
                    }
                }

                _activeFrets[i] = frets[i];
            }
        }
    }
}