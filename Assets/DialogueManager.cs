using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DialogueManager : Singleton<DialogueManager>
{
    public GameObject DialogueCanvas;

    protected override void OnLoaded()
    {
        DialogueCanvas = new GameObject();
        DialogueCanvas.transform.SetParent(transform);
        var canvas = DialogueCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var canvasScaler = DialogueCanvas.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);

        var canvasRaycaster = DialogueCanvas.AddComponent<GraphicRaycaster>();

        dialoguePrefab = Resources.Load<GameObject>("Dialogue");
    }

    [SerializeField] private GameObject dialoguePrefab;

    [SerializeField] List<Dialogue> dialogs = new List<Dialogue>();

    [SerializeField] private Dictionary<string, string> publicTextVars = new Dictionary<string, string>();
    [SerializeField] private bool freezePlayer;

    public Dialogue createDialogue(DialogueSettings settings, Transform otherCanvas = null)
    {
        if (getDialogue(settings.name)) return null;
        otherCanvas ??= DialogueCanvas.transform;
        Dialogue d = Instantiate(dialoguePrefab, otherCanvas).GetComponent<Dialogue>();
        d.startDialogue(settings);
        dialogs.Add(d);

        if (settings.freezePlayer)
            freezePlayer = true;

        return d;
    }

    public Dialogue getDialogue(string name)
    {
        Dialogue toReturn = null;
        for (int i = 0; i < dialogs.Count; i++)
        {
            if (dialogs[i].settings.name == name)
            {
                toReturn = dialogs[i];
                break;
            }
        }

        return toReturn;
    }

    public void StopDialogue(string name)
    {
        StopDialogue(getDialogue(name));
    }

    public void StopDialogue(Dialogue d)
    {
        dialogs.Remove(d);

        bool keepFrozen = false;
        for (int i = 0; i < dialogs.Count; i++)
        {
            if (dialogs[i].settings.freezePlayer == true)
                keepFrozen = true;
        }
        freezePlayer = keepFrozen;

        d.ExitDialogue();
    }

    public bool getFreezePlayer() { return freezePlayer; }

    public Dictionary<string, string> getPublicTextVars() { return publicTextVars; }
    public void addPublicTextVar(string key, string val)
    {
        if (!publicTextVars.ContainsKey(key))
            publicTextVars.Add(key, val);
    }
}

public enum DialogueEnterTransition
{
    None,
    Scale,
    Expand
}

[System.Serializable]
public class DialogueSettings
{
    public string name;
    [Header("Text options")]
    public List<DialogueText> texts;
    public float fontSize = 70;

    [Header("Visual")]
    public Sprite icon;

    public TMP_FontAsset font;
    public Color startColor = Color.white;
    public Color BackgroundColor = Color.black;
    public bool hideBG;
    public HorizontalAlignmentOptions alignment;

    public DialogueEnterTransition enterTransition = DialogueEnterTransition.Expand;
    public DialogueEnterTransition exitTransition = DialogueEnterTransition.Scale;

    [Header("Audio")]
    public AudioClip speakSound;
    public float speakSoundVolume = 1;

    [Header("Settings")]
    public float startSpeed = 0.05f;
    public bool unskippable;
    public bool autoSkip;
    public float autoSkipSpeed = 0.5f;
    public bool freezePlayer = true;

    [Header("Special events")]
    public List<Color> colors;
    public List<UnityEvent> events;
    public List<AudioClip> voices;
}

[System.Serializable]
public class DialogueText
{
    public DialogueText(string text)
    {
        this.text = text;
    }
    [Multiline]
    public string text;
    public UnityEvent startEvent = new();
    public UnityEvent endEvent = new();
}