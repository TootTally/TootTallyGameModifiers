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
        public static Metadata HIDDEN = new Metadata("HD", ModifierType.Hidden, "Hidden: Notes will disappear as they\n approach the left");
        public static Metadata FLASHLIGHT = new Metadata("FL", ModifierType.Flashlight, "Flashlight: Only a small circle around the\n cursor is visible");
        public static Metadata BRUTAL = new Metadata("BT", ModifierType.Brutal, "Brutal: Game will speed up if you do good and\n slow down when you are bad");
        public static Metadata INSTA_FAIL = new Metadata("IF", ModifierType.InstaFail, "Insta Fail: Restart the song as soon as you miss");

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

            public static int _counter;

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

        public class Flashlight : GameModifierBase
        {
            public override Metadata Metadata => FLASHLIGHT;

            private VignetteModel.Settings _settings;
            private Vector2 _pointerPos;
            private Color _color;

            public Flashlight() : base() { }

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
            }

            public override void Update(GameController __instance)
            {

                if (__instance.totalscore != 0)
                {
                    _settings.intensity = (180f / Mathf.Min(__instance.defaultnotelength, 180f)) + __instance.breathcounter;
                    if (__instance.notebuttonpressed && !__instance.outofbreath)
                    {
                        _color.r = _color.g = _color.b = 0f;
                        _color.a = 1f;
                    }
                }


                _pointerPos.y = (__instance.pointer.transform.localPosition.y + 215) / 430;
                _settings.center = _pointerPos;
                _settings.color = _color;
                __instance.gameplayppp.vignette.settings = _settings;
            }
        }

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

        public readonly struct Metadata
        {
            public string Name { get; }
            public ModifierType ModifierType { get; }
            public string Description { get; }

            public Metadata(string name, ModifierType modifierType, string description)
            {
                Name = name;
                ModifierType = modifierType;
                Description = description;
            }
        }

        public enum ModifierType
        {
            Hidden,
            Flashlight,
            Brutal,
            InstaFail
        }
    }
}
