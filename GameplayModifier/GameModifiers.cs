using Rewired;
using System;
using System.Collections.Generic;
using System.Linq;
using TootTallyCore.Utils.TootTallyGlobals;
using TrombLoader.Data;
using UnityEngine;
using UnityEngine.PostProcessing;
using UnityEngine.UI;
using UnityEngineInternal.Input;
using static Rewired.Platforms.Custom.CustomInputSource;

namespace TootTallyGameModifiers
{
    public static class GameModifiers
    {
        //bools are: submit score allowed and allowed in multiplayer
        public static Metadata HIDDEN = new Metadata("HD", ModifierType.Hidden, "Hidden: Notes will disappear as they\n approach the left", true, true);
        public static Metadata FLASHLIGHT = new Metadata("FL", ModifierType.Flashlight, "Flashlight: Only a small circle around the\n cursor is visible", true, true);
        public static Metadata BRUTAL = new Metadata("BT", ModifierType.Brutal, "Brutal: Game will speed up if you do good and\n slow down when you are bad (Unrated)", false, false);
        public static Metadata INSTA_FAIL = new Metadata("IF", ModifierType.InstaFail, "Insta Fail: Restart the song as soon as you miss", true, false);
        public static Metadata EASY_MODE = new Metadata("EZ", ModifierType.EasyMode, "Easy Mode: Lower the threshold for combo and champ break", true, true);
        public static Metadata STRICT_MODE = new Metadata("ST", ModifierType.StrictMode, "Strict Mode: Note timing becomes significantly more strict (Unrated)", false, true);
        public static Metadata HIDDEN_CURSOR = new Metadata("HC", ModifierType.HiddenCursor, "Hidden Cursor: Make the cursor invisible", true, true);
        public static Metadata NO_BREATHING = new Metadata("NB", ModifierType.NoBreathing, "No Breathing: Disables the breathing mechanic (Unrated)", false, true);
        public static Metadata MIRROR_MODE = new Metadata("MR", ModifierType.MirrorMode, "Mirror Mode: Inverts the Y axis.", true, true);
        public static Metadata SCORE_V2 = new Metadata("V2", ModifierType.ScoreV2, "Score V2: Normalizes the score to 1 million points.", false, true);
        public static Metadata AUTO_PILOT = new Metadata("AP", ModifierType.AutoPilot, "Auto Pilot: Automatically aim at the notes.", false, true);
        public static Metadata RELAX_MODE = new Metadata("RX", ModifierType.RelaxMode, "Relax Mode: Automatically toot when hovering a note.", false, true);
        public static Metadata KEYBOARD_MODE = new Metadata("KM", ModifierType.KeyboardMode, "Keyboard Mode: Play notes with your keyboard, adjust pitch with mouse", false, false);

        #region Hidden
        public class Hidden : GameModifierBase
        {
            public override Metadata Metadata => HIDDEN;

            public Queue<FullNoteComponents> _activeNotesComponents;
            public Color _headOutColor, _headInColor;
            public Color _tailOutColor, _tailInColor;
            public Color _bodyOutStartColor, _bodyOutEndColor;
            public Color _bodyInStartColor, _bodyInEndColor;
            public static float START_FADEOUT_POSX = 3.5f;
            public static float END_FADEOUT_POSX = -1.6f;
            public static int _counter;

            public override void Initialize(GameController __instance)
            {
                _counter = 1;
                _activeNotesComponents = new Queue<FullNoteComponents>();

                //Doing all this to make sure its future proof in case cosmetic plugin changes the outline colors or some shit
                var note = __instance.allnotes.First();
                _headOutColor = note.transform.Find("StartPoint").GetComponent<Image>().color;
                _headInColor = note.transform.Find("StartPoint/StartPointColor").GetComponent<Image>().color;
                _tailOutColor = note.transform.Find("EndPoint").GetComponent<Image>().color;
                _tailInColor = note.transform.Find("EndPoint/EndPointColor").GetComponent<Image>().color;
                _bodyOutStartColor = note.transform.Find("OutlineLine").GetComponent<LineRenderer>().startColor;
                _bodyOutEndColor = note.transform.Find("OutlineLine").GetComponent<LineRenderer>().endColor;
                _bodyInStartColor = note.transform.Find("Line").GetComponent<LineRenderer>().startColor;
                _bodyInEndColor = note.transform.Find("Line").GetComponent<LineRenderer>().endColor;

                foreach (GameObject currentNote in __instance.allnotes.Skip(1).Where(n => n.transform.position.x <= START_FADEOUT_POSX + 9.4f))
                {
                    if (currentNote.transform.position.x <= START_FADEOUT_POSX + 9.4f)
                    {
                        var noteComp = new FullNoteComponents()
                        {
                            startPoint = currentNote.transform.Find("StartPoint").GetComponent<Image>(),
                            startPointColor = currentNote.transform.Find("StartPoint/StartPointColor").GetComponent<Image>(),
                            endPoint = currentNote.transform.Find("EndPoint").GetComponent<Image>(),
                            endPointColor = currentNote.transform.Find("EndPoint/EndPointColor").GetComponent<Image>(),
                            outlineLine = currentNote.transform.Find("OutlineLine").GetComponent<LineRenderer>(),
                            line = currentNote.transform.Find("Line").GetComponent<LineRenderer>(),
                        };
                        _activeNotesComponents.Enqueue(noteComp);
                    }
                }
            }

            public static void SetFadeOutValues(float startFadeOut, float endFadeOut)
            {
                START_FADEOUT_POSX = startFadeOut;
                END_FADEOUT_POSX = endFadeOut;
            }

            public override void Update(GameController __instance)
            {
                //Start pos * 2.7f is roughly the complete right of the screen, which means the notes are added as they enter the screen
                for (int i = _counter; i < __instance.allnotes.Count; i++)
                {
                    var currentNote = __instance.allnotes[i];
                    if (_activeNotesComponents.Any(x => x.startPoint.transform.parent.gameObject == currentNote)) continue;
                    if (currentNote.transform.position.x > START_FADEOUT_POSX + 9.4f) break;

                    //Get all the note's objects that have to fade
                    var noteComp = new FullNoteComponents()
                    {
                        startPoint = currentNote.transform.GetChild(0).GetComponent<Image>(),
                        startPointColor = currentNote.transform.GetChild(0).GetChild(0).GetComponent<Image>(),
                        endPoint = currentNote.transform.GetChild(1).GetComponent<Image>(),
                        endPointColor = currentNote.transform.GetChild(1).GetChild(0).GetComponent<Image>(),
                        outlineLine = currentNote.transform.GetChild(2).GetComponent<LineRenderer>(),
                        line = currentNote.transform.GetChild(3).GetComponent<LineRenderer>(),
                    };

                    //Add note to active list
                    _counter++;
                    _activeNotesComponents.Enqueue(noteComp);
                }

                for (int i = 0; i < _activeNotesComponents.Count; i++)
                {
                    var note = _activeNotesComponents.ElementAt(i);

                    note.alphaStart.a = 1f - Mathf.Clamp((note.startPoint.transform.position.x - END_FADEOUT_POSX) / (START_FADEOUT_POSX - END_FADEOUT_POSX), 0, 1);
                    note.alphaEnd.a = 1f - Mathf.Clamp((note.endPoint.transform.position.x - END_FADEOUT_POSX) / (START_FADEOUT_POSX - END_FADEOUT_POSX), 0, 1);

                    note.outlineLine.startColor = _bodyOutStartColor - note.alphaStart;
                    note.line.startColor = _bodyInStartColor - note.alphaStart;
                    note.startPoint.color = _headOutColor - note.alphaStart;
                    note.startPointColor.color = _headInColor - note.alphaStart;

                    note.outlineLine.endColor = _bodyOutEndColor - note.alphaEnd;
                    note.line.endColor = _bodyInEndColor - note.alphaEnd;
                    note.endPoint.color = _tailOutColor - note.alphaEnd;
                    note.endPointColor.color = _tailInColor - note.alphaEnd;
                }

                while (_activeNotesComponents.Count > 0 && _activeNotesComponents.Peek().endPoint.transform.position.x <= END_FADEOUT_POSX)
                    _activeNotesComponents.Dequeue();
            }

            public class FullNoteComponents
            {
                public Image startPoint, endPoint;
                public Image startPointColor, endPointColor;
                public LineRenderer outlineLine, line;
                public Color alphaStart, alphaEnd;
                public FullNoteComponents()
                {
                    alphaStart = alphaEnd = Color.black;
                }
            }
        }
        #endregion

        #region Flashlight
        public class Flashlight : GameModifierBase
        {
            public override Metadata Metadata => FLASHLIGHT;

            private VignetteModel.Settings _settings;
            private Vector2 _pointerPos;
            private Color _color;
            private bool _isMirror;

            public override void Initialize(GameController __instance)
            {
                __instance.gameplayppp.vignette.enabled = true;
                _pointerPos = new Vector2(.075f, (__instance.pointer.transform.localPosition.y + 215) / 430);
                _color = new Color(.3f, .3f, .3f, 1);
                _settings = new VignetteModel.Settings()
                {
                    center = _pointerPos,
                    color = _color,
                    intensity = .8f,
                    mode = VignetteModel.Mode.Classic,
                    rounded = true,
                    roundness = 1,
                    smoothness = 1,
                };
                _isMirror = GameModifierManager.GetModifiersString().Contains("MR");
            }

            public override void Update(GameController __instance)
            {

                if (__instance.totalscore != 0)
                {
                    //https://www.desmos.com/calculator/xsdzfdpgv5
                    var adjustedNoteLength = __instance.defaultnotelength / (100f / (__instance.tempo * TootTallyGlobalVariables.gameSpeedMultiplier));
                    _settings.intensity = (325f / Mathf.Min(adjustedNoteLength, 325f)) + __instance.breathcounter;
                    if (__instance.notebuttonpressed && !__instance.outofbreath)
                    {
                        _color.r = _color.g = _color.b = 0f;
                        _color.a = 1f;
                    }
                }


                if (_isMirror)
                    _pointerPos.y = (215 - __instance.pointer.transform.localPosition.y) / 430;
                else
                    _pointerPos.y = (__instance.pointer.transform.localPosition.y + 215) / 430;

                _settings.center = _pointerPos;
                _settings.color = _color;
                __instance.gameplayppp.vignette.settings = _settings;
            }
        }
        #endregion

        #region Brutal
        public class Brutal : GameModifierBase
        {
            public override Metadata Metadata => BRUTAL;

            private float _defaultSpeed;
            private float _speed;
            private int _lastCombo;

            public override void Initialize(GameController __instance)
            {
                _defaultSpeed = 1;
                _speed = _defaultSpeed;
                _lastCombo = 0;
            }

            public override void Update(GameController __instance)
            {
                if (__instance.paused || __instance.quitting || __instance.retrying || __instance.level_finished)
                {
                    _speed = _defaultSpeed;
                    Time.timeScale = 1f;
                }
                else if (__instance.musictrack.outputAudioMixerGroup == __instance.audmix_bgmus)
                    __instance.musictrack.outputAudioMixerGroup = __instance.audmix_bgmus_pitchshifted;
            }

            public override void SpecialUpdate(GameController __instance)
            {
                if (_lastCombo != __instance.highestcombocounter || __instance.highestcombocounter == 0)
                {
                    var shouldIncreaseSpeed = _lastCombo < __instance.highestcombocounter && __instance.highestcombocounter != 0;
                    _speed = Mathf.Clamp(_speed + (shouldIncreaseSpeed ? .015f : -.07f), _defaultSpeed, 2f);
                    Time.timeScale = _speed / _defaultSpeed;
                    __instance.musictrack.pitch = _speed;
                    __instance.audmix.SetFloat("pitchShifterMult", 1f / _speed);
                }
                _lastCombo = __instance.highestcombocounter;
            }
        }
        #endregion

        #region InstaFail
        public class InstaFail : GameModifierBase
        {
            public override Metadata Metadata => INSTA_FAIL;

            public override void SpecialUpdate(GameController __instance)
            {
                if (!__instance.paused && !__instance.quitting && !__instance.retrying && !__instance.level_finished && !TootTallyGlobalVariables.isSpectating && !TootTallyGlobalVariables.isReplaying)
                {
                    __instance.notebuttonpressed = false;
                    __instance.musictrack.Pause();
                    __instance.sfxrefs.backfromfreeplay.Play();
                    __instance.quitting = true;
                    __instance.pauseRetryLevel();
                }
            }
        }
        #endregion

        #region EasyMode
        public class EasyMode : GameModifierBase
        {
            public override Metadata Metadata => EASY_MODE;

            public override void SpecialUpdate(GameController __instance)
            {
                __instance.notescoreaverage = Mathf.Clamp(__instance.notescoreaverage * 1.15f, 0, 100);
            }

        }
        #endregion

        #region StrictMode
        public class StrictMode : GameModifierBase
        {
            public override Metadata Metadata => STRICT_MODE;

            public override void Update(GameController __instance)
            {
                float num8 = __instance.noteholderr.anchoredPosition3D.x - __instance.zeroxpos;
                num8 = num8 > 0f ? -1f : Mathf.Abs(num8);

                if (__instance.noteactive && !__instance.freeplay && !__instance.paused)
                {
                    float num9 = (__instance.currentnoteend - num8) / (__instance.currentnoteend - __instance.currentnotestart);
                    num9 = Mathf.Abs(1f - num9);
                    float num10 = __instance.easeInOutVal(num9, 0f, __instance.currentnotepshift, 1f);
                    float num11 = __instance.pointerrect.anchoredPosition.y - (__instance.currentnotestarty + num10);
                    if (__instance.currentnotepshift != 0f)
                    {
                        float t = (Mathf.Clamp(Mathf.Abs(__instance.currentnotepshift), 10f, 150f) - 10f) / 140f;
                        float num12 = Mathf.Lerp(.96f, .7f, t);
                        num11 *= num12;
                    }
                    float num13 = 100f - Mathf.Abs(num11);
                    if (num13 < 0f || !__instance.noteplaying)
                    {
                        __instance.notescoresamples += 4.78f;
                        __instance.notescoretotal = 0;
                        __instance.notescoreaverage = __instance.notescoretotal / __instance.notescoresamples;
                    }
                }
            }
        }
        #endregion

        #region HiddenCursor
        public class HiddenCursor : GameModifierBase
        {
            public override Metadata Metadata => HIDDEN_CURSOR;

            public override void Initialize(GameController __instance)
            {
                __instance.pointer.SetActive(false);
            }
        }
        #endregion

        #region NoBreathing
        public class NoBreathing : GameModifierBase
        {
            public override Metadata Metadata => NO_BREATHING;

            public override void Update(GameController __instance)
            {
                __instance.breathcounter = 0;
            }
        }
        #endregion

        #region MirrorMode
        public class MirrorMode : GameModifierBase
        {
            public override Metadata Metadata => MIRROR_MODE;

            public override void Initialize(GameController __instance)
            {
                __instance.noteholder.transform.parent.localScale = new Vector3(1, -1, 1);
                __instance.lyricsholder.transform.localScale = new Vector3(1, -1, 1);
                for (int i = 0; i < 15; i++)
                    __instance.noteparticles.transform.GetChild(i).localScale = new Vector3(1, -1, 1);
                __instance.gameplay_settings.mouse_controldirection = GlobalVariables.localsettings.mousecontrolmode switch
                {
                    (int)ControlType.RegularX => (int)ControlType.InvertedX,
                    (int)ControlType.InvertedX => (int)ControlType.RegularX,
                    (int)ControlType.RegularY => (int)ControlType.InvertedY,
                    (int)ControlType.InvertedY => (int)ControlType.RegularY,
                    _ => (int)ControlType.InvertedY
                };
            }

        }
        #endregion

        #region ScoreV2
        public class ScoreV2 : GameModifierBase
        {
            public override Metadata Metadata => SCORE_V2;



            public override void SpecialUpdate(GameController __instance)
            {

            }
        }
        #endregion

        #region RelaxMode
        public class RelaxMode : GameModifierBase
        {
            public override Metadata Metadata => RELAX_MODE;

            private GameController _gameController;
            private int _noteIndex;
            private float _latencyOffset, _trackTime, _lastTrackTime, _lastTimeSample,
                _currentNoteStartTime, _currentNoteEndTime,
                _lastNoteEndTime;
            private bool _isTooting, _releasedBetweenNotes, _shouldBreath, _isNoteActive, _isSlider;

            public override void Initialize(GameController __instance)
            {
                _gameController = __instance;
                _latencyOffset = _gameController.latency_offset;
                _isTooting = false;
                _isSlider = false;
                _shouldBreath = false;
                _isNoteActive = false;
                _releasedBetweenNotes = true;
                _noteIndex = -1;
                _lastNoteEndTime = -999;
                _lastTrackTime = _trackTime = 0f;
                _lastTimeSample = 0f;
                if (_gameController.leveldata.Count > 0)
                {
                    _currentNoteStartTime = B2s(_gameController.leveldata[0][0], _gameController.tempo);
                    _currentNoteEndTime = _currentNoteStartTime + B2s(_gameController.leveldata[0][1], _gameController.tempo);
                }
            }

            public override void Update(GameController __instance)
            {
                if (!_gameController.paused && !_gameController.quitting && _gameController.musictrack.isPlaying)
                    UpdateTrackData();

                if (!_shouldBreath && ((_gameController.breathcounter >= .95f && _isNoteActive) || (!_isNoteActive && _gameController.breathcounter >= .5f)))
                    _shouldBreath = true;
                else if (_shouldBreath && ((_gameController.breathcounter <= .65f && _isNoteActive) || (!_isNoteActive && _gameController.breathcounter <= 0f)))
                    _shouldBreath = false;

                _isTooting = ShouldToot();
                if (!_isTooting)
                    _releasedBetweenNotes = true;
            }

            public override void SpecialUpdate(ref bool __value)
            {
                __value = _isTooting;
            }

            private bool ShouldToot() => ((_trackTime >= Mathf.Max(_currentNoteStartTime - _latencyOffset, _lastNoteEndTime) && _releasedBetweenNotes)
                                     || _trackTime <= _lastNoteEndTime
                                     || _isSlider)
                                     && !_shouldBreath
                                     && _trackTime > .01f;
            private void UpdateTrackData()
            {
                var dt = Time.deltaTime;
                _trackTime += dt * TootTallyGlobalVariables.gameSpeedMultiplier;
                if (_lastTimeSample != _gameController.musictrack.timeSamples)
                {
                    _lastTrackTime = _gameController.musictrack.time - _gameController.noteoffset - _gameController.latency_offset;
                    _lastTimeSample = _gameController.musictrack.timeSamples;
                }
                //slight correction
                _trackTime += (_lastTrackTime - _trackTime) / 60f;

                if (_trackTime >= _currentNoteEndTime)
                {
                    _noteIndex++;
                    if (_noteIndex + 1 < _gameController.leveldata.Count)
                    {
                        _lastNoteEndTime = _currentNoteEndTime + (.005f * TootTallyGlobalVariables.gameSpeedMultiplier);
                        _isSlider = Mathf.Abs(_gameController.leveldata[_noteIndex + 1][0] - (_gameController.leveldata[_noteIndex][0] + _gameController.leveldata[_noteIndex][1])) < 0.05f;
                        _currentNoteStartTime = B2s(_gameController.leveldata[_noteIndex + 1][0], _gameController.tempo);
                        _currentNoteEndTime = _currentNoteStartTime + B2s(_gameController.leveldata[_noteIndex + 1][1], _gameController.tempo);
                        _lastNoteEndTime = Mathf.Min(_lastNoteEndTime, _currentNoteStartTime - .01f);
                        _releasedBetweenNotes = !_isTooting;
                    }
                    else
                    {
                        _currentNoteStartTime = float.MaxValue;
                        _isSlider = false;
                    }

                }

                _isNoteActive = _trackTime >= _currentNoteStartTime - dt * 5f && _trackTime < _currentNoteEndTime + dt * 5f;
            }
            public static float B2s(float time, float bpm) => time / bpm * 60f;

        }
        #endregion

        #region AutoPilot
        public class AutoPilot : GameModifierBase
        {
            public override Metadata Metadata => AUTO_PILOT;

            private GameController _gameController;
            private RectTransform _pointerRect;
            private int _noteIndex;
            private float _trackTime, _lastTrackTime, _lastTimeSample,
                _currentNoteStartTime, _currentNoteEndTime,
                _currentNoteStartY, _currentNoteEndY,
                _lastNoteEndY,
                _lastNoteEndTime,
                _earlyTimingAdjustValue, _lateTimingAdjustValue;
            private Vector2 _pointerPosition;
            private BackgroundPuppetController _bgPuppetController;
            private Vector2 _screenDim;

            public override void Initialize(GameController __instance)
            {
                _gameController = __instance;
                _gameController.controllermode = true;
                _pointerRect = _gameController.pointerrect;
                _noteIndex = -1;
                _lastNoteEndY = 0;
                _lastNoteEndTime = -999;
                _lastTrackTime = _trackTime = 0f;
                _lastTimeSample = 0f;
                _pointerPosition = _pointerRect.anchoredPosition;
                if (_gameController.leveldata.Count > 0)
                {
                    _currentNoteStartTime = B2s(_gameController.leveldata[0][0], _gameController.tempo);
                    _currentNoteEndTime = _currentNoteStartTime + B2s(_gameController.leveldata[0][1], _gameController.tempo);
                    _currentNoteStartY = _gameController.leveldata[0][2];
                    _currentNoteEndY = _gameController.leveldata[0][4];
                }
                _earlyTimingAdjustValue = .005f * TootTallyGlobalVariables.gameSpeedMultiplier;
                _lateTimingAdjustValue = .005f * TootTallyGlobalVariables.gameSpeedMultiplier;

                _screenDim = new Vector2(Screen.width, Screen.height);
                if (__instance.bgcontroller != null)
                    _bgPuppetController = __instance.bgcontroller.fullbgobject.GetComponent<BackgroundPuppetController>();
                else
                    _bgPuppetController = null;
            }

            public override void Update(GameController __instance)
            {
                if (_gameController == null || !_gameController.enabled || _gameController.freeplay) return;

                if (!_gameController.paused && !_gameController.quitting && _gameController.musictrack.isPlaying)
                    UpdateTrackData();

                _pointerPosition.y = GetPositionY();
                _pointerRect.anchoredPosition = _pointerPosition;

                if (_gameController != null && _gameController.puppet_humanc != null)
                {
                    _gameController.puppet_humanc.doPuppetControl(-_pointerPosition.y / 225);
                    _bgPuppetController?.DoPuppetControl(-_pointerPosition.y / 225, _gameController.vibratoamt);
                }
            }

            private void UpdateTrackData()
            {
                var dt = Time.deltaTime;
                _trackTime += dt * TootTallyGlobalVariables.gameSpeedMultiplier;
                if (_lastTimeSample != _gameController.musictrack.timeSamples)
                {
                    _lastTrackTime = _gameController.musictrack.time - _gameController.noteoffset - _gameController.latency_offset;
                    _lastTimeSample = _gameController.musictrack.timeSamples;
                }
                //slight correction
                _trackTime += (_lastTrackTime - _trackTime) / 60f;

                if (_trackTime >= _currentNoteEndTime)
                {
                    _noteIndex++;
                    if (_noteIndex + 1 < _gameController.leveldata.Count)
                    {
                        _lastNoteEndTime = _currentNoteEndTime + _lateTimingAdjustValue;
                        _lastNoteEndY = _currentNoteEndY;

                        _currentNoteStartTime = B2s(_gameController.leveldata[_noteIndex + 1][0], _gameController.tempo);
                        _currentNoteEndTime = _currentNoteStartTime + B2s(_gameController.leveldata[_noteIndex + 1][1], _gameController.tempo);
                        _currentNoteStartY = _gameController.leveldata[_noteIndex + 1][2];
                        _currentNoteEndY = _gameController.leveldata[_noteIndex + 1][4];
                        _lastNoteEndTime = Mathf.Min(_lastNoteEndTime, _currentNoteStartTime - .01f);
                    }
                    else
                    {
                        _currentNoteStartTime = float.MaxValue;
                    }

                }
            }

            private float GetPositionY()
            {
                float by;
                if (_trackTime >= _currentNoteStartTime - _earlyTimingAdjustValue && _trackTime <= _currentNoteEndTime + _lateTimingAdjustValue)
                {
                    if (_currentNoteStartY != _currentNoteEndY)
                        by = Mathf.Clamp(1f - ((_currentNoteEndTime - _trackTime - (.005555f * TootTallyGlobalVariables.gameSpeedMultiplier)) / (_currentNoteEndTime - _currentNoteStartTime)), 0, 1);
                    else
                        by = Mathf.Clamp(1f - ((_currentNoteEndTime - _trackTime) / (_currentNoteEndTime - (_currentNoteStartTime - _earlyTimingAdjustValue))), 0, 1);
                    return _currentNoteStartY + _gameController.easeInOutVal(Mathf.Abs(by), 0f, _currentNoteEndY - _currentNoteStartY, 1f);
                }
                var adjustedNoteStart = _currentNoteStartTime - _earlyTimingAdjustValue;
                by = Mathf.Clamp(1f - ((adjustedNoteStart - _trackTime) / (adjustedNoteStart - _lastNoteEndTime)), 0, 1);
                return Mathf.Lerp(_lastNoteEndY, _currentNoteStartY, InOutQuad(by));
            }

            public static float InQuad(float t) => t * t;
            public static float OutQuad(float t) => 1 - InQuad(1 - t);
            public static float InOutQuad(float t)
            {
                if (t < 0.5) return InQuad(t * 2) / 2;
                return 1 - InQuad((1 - t) * 2) / 2;
            }

            public static float B2s(float time, float bpm) => time / bpm * 60f;

        }
        #endregion

        #region KeyboardMode
        public class KeyboardMode : GameModifierBase
        {
            public override Metadata Metadata => KEYBOARD_MODE;

            private readonly Dictionary<KeyCode, float> _keycodeToPositionDict = new Dictionary<KeyCode, float>()
            {
                { KeyCode.A, 0f },
                { KeyCode.S, 13.75f },
                { KeyCode.D, 41.25f },
                { KeyCode.F, 68.75f },
                { KeyCode.G, 96.25f },
                { KeyCode.H, 110f },
                { KeyCode.J, 137.5f },
                { KeyCode.K, 165f },
                { KeyCode.M, -13.75f },
                { KeyCode.N, -41.25f },
                { KeyCode.B, -68.75f },
                { KeyCode.V, -96.25f },
                { KeyCode.C, -110f },
                { KeyCode.X, -137.5f },
                { KeyCode.Z, -165f },
            };
            private Vector3 _pitchOffset;
            private Vector3 _lastPointerPosition;

            public override void Initialize(GameController __instance)
            {
                __instance.controllermode = true;
                __instance.gameplay_settings.mouse_movementmode = 1; //This makes it so pausing / resuming won't break CursorLockMode;
                Cursor.lockState = CursorLockMode.Locked;
                _lastPointerPosition = __instance.pointer.transform.localPosition;
                _pitchOffset = Vector3.zero;
            }

            public override void Update(GameController __instance)
            {
                var key = _keycodeToPositionDict.Keys.FirstOrDefault(Input.GetKeyDown);
                if (key != default)
                {
                    _lastPointerPosition = __instance.pointer.transform.localPosition;
                    var pitchUp = Input.GetKey(KeyCode.LeftShift) ? 13.75f : 0;
                    var pitchDown = Input.GetKey(KeyCode.LeftControl) ? -13.75f : 0;
                    _lastPointerPosition.y = _keycodeToPositionDict[key] + pitchUp + pitchDown;
                    _pitchOffset.y = 0;
                }
                var mouseDeltaY = Input.GetAxisRaw("Mouse Y") * 4f;
                if (mouseDeltaY != 0)
                {
                    _pitchOffset.y += mouseDeltaY;
                    if (Mathf.Abs(_lastPointerPosition.y + _pitchOffset.y) >= 180)
                    {
                        _lastPointerPosition.y = 180 * Mathf.Sign(_lastPointerPosition.y + _pitchOffset.y);
                        _pitchOffset.y = 0;
                    }
                }
                __instance.pointer.transform.localPosition = _lastPointerPosition + _pitchOffset;
            }
        }
        #endregion

        public readonly struct Metadata
        {
            public string Name { get; }
            public ModifierType ModifierType { get; }
            public string Description { get; }
            public bool ScoreSubmitEnabled { get; }
            public bool AllowedInMultiplayer { get; }

            public Metadata(string name, ModifierType modifierType, string description, bool scoreSubmitEnabled, bool allowedInMultiplayer)
            {
                Name = name;
                ModifierType = modifierType;
                Description = description;
                ScoreSubmitEnabled = scoreSubmitEnabled;
                AllowedInMultiplayer = allowedInMultiplayer;
            }
        }

        public enum ModifierType
        {
            Hidden,
            Flashlight,
            Brutal,
            InstaFail,
            EasyMode,
            StrictMode,
            HiddenCursor,
            NoBreathing,
            MirrorMode,
            ScoreV2,
            KeyboardMode,
            AutoPilot,
            RelaxMode,
        }

        public enum ControlType
        {
            RegularX,
            InvertedX,
            RegularY,
            InvertedY,
            NotSet
        }
    }
}
