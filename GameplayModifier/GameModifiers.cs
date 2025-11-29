using System;
using System.Collections.Generic;
using System.Linq;
using TootTallyCore.Utils.TootTallyGlobals;
using UnityEngine;
using UnityEngine.PostProcessing;
using UnityEngine.UI;

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
        public static Metadata AUTO_TUNE = new Metadata("AT", ModifierType.AutoTune, "Auto Tune: Snaps to standard grid and slides (Unrated)", false, true);
        public static Metadata HIDDEN_CURSOR = new Metadata("HC", ModifierType.HiddenCursor, "Hidden Cursor: Make the cursor invisible", true, true);
        public static Metadata NO_BREATHING = new Metadata("NB", ModifierType.NoBreathing, "No Breathing: Disables the breathing mechanic (Unrated)", false, true);
        public static Metadata MIRROR_MODE = new Metadata("MR", ModifierType.MirrorMode, "Mirror Mode: Inverts the Y axis.", true, true);
        public static Metadata RELAX_MODE = new Metadata("RX", ModifierType.MirrorMode, "Relax Mode: Automatically toot when hovering a note.", false, true);
        public static Metadata AUTO_PILOT = new Metadata("AP", ModifierType.MirrorMode, "Auto Pilot: Automatically aim at the notes.", false, true);

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

        #region AutoTune
        public class AutoTune : GameModifierBase
        {
            public override Metadata Metadata => AUTO_TUNE;

            public override void SpecialUpdate(GameController __instance)
            {

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

        #region RelaxMode
        public class RelaxMode : GameModifierBase
        {
            public override Metadata Metadata => RELAX_MODE;

            public override void Initialize(GameController __instance)
            {

            }
        }
        #endregion

        #region AutoPilot
        public class AutoPilot : GameModifierBase
        {
            public override Metadata Metadata => AUTO_PILOT;

            public override void Initialize(GameController __instance)
            {

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
            AutoTune,
            HiddenCursor,
            NoBreathing,
            MirrorMode,
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
