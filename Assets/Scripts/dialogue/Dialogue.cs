using System.Collections;
using System.Collections.Generic;
using EasyTextEffects;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Dialogue : MonoBehaviour
{
    public DialogueSettings settings;

    public Image icon;
    public TextMeshProUGUI textArea;
    public Image bg;
    public Image outline;
    public TextEffect effetcs;

    float currentSpeed;

    Animator animator;

    string toWriteText;
    int lastWrittenIndex;
    AudioClip originalSpeakSound;

    Coroutine writeRoutene;

    int currentPage = -1;

    public List<DialogueEvent> currentEvents = new List<DialogueEvent>();

    public event UnityAction OnDialogueComplete;

    /*
     * 0 = didnt start
     * 1 = opening
     * 2 = writing
     * 3 = waiting on input
     * 4 = colsing
     */
    int stage = 0;

    public void startDialogue(DialogueSettings _settings)
    {
        settings = _settings;

        if (settings.showAtTop)
            transform.localPosition += Vector3.up * (Mathf.Abs(transform.localPosition.y) * 2);

        stage = 1;

        if (settings.icon)
            icon.sprite = settings.icon;
        else
        {
            textArea.GetComponent<RectTransform>().sizeDelta = new Vector2(1340, textArea.GetComponent<RectTransform>().sizeDelta.y);
            textArea.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            icon.gameObject.SetActive(false);
        }

        originalSpeakSound = settings.speakSound;

        if (settings.font)
            textArea.font = settings.font;

        textArea.fontSize = settings.fontSize;

        textArea.color = settings.startColor;

        currentSpeed = settings.startSpeed;

        bg.color = settings.BackgroundColor;

        animator = GetComponent<Animator>();
        animator.SetInteger("enterTransition", (int)settings.enterTransition);
        animator.SetBool("entering", true);
        if (settings.hideBG)
        {
            animator.speed = 1000;
            bg.gameObject.SetActive(false);
            outline.gameObject.SetActive(false);
        }
    }

    public void onDialogueEntered()
    {
        progressDialogue();
    }

    void OnDestroy()
    {
        OnDialogueComplete?.Invoke();
    }

    public void progressDialogue()
    {
        if (currentPage != -1 && currentPage != settings.texts.Count)
            settings.texts[currentPage].endEvent.Invoke();

        currentPage++;

        textArea.text = string.Empty;
        textArea.horizontalAlignment = settings.alignment;
        //effetcs.Refresh();

        if (currentPage > settings.texts.Count - 1)
        {
            DialogueManager.Get().StopDialogue(this);
            return;
        }

        string text = settings.texts[currentPage].text;

        //format text and get all events
        currentEvents.Clear();

        string res = string.Empty;
        bool foundKeyPoint = false;
        int startIndex = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (foundKeyPoint && text[i] != '}')
                res += text[i];

            if (text[i] == '{')
            {
                foundKeyPoint = true;
                res = string.Empty;
                startIndex = i;
            }

            if (text[i] == '}')
            {
                foundKeyPoint = false;

                if (res.Length > 1)
                {
                    DialogueEvent e = new DialogueEvent();

                    e.type = DialogueEvent.DialogueEventType.PublicText;

                    int charCheck = 0;

                    if (res[0] == '/')
                    {
                        e.isCancelation = true;
                        charCheck = 1;
                    }
                    float value = -1;
                    if (!e.isCancelation)
                    {
                        try
                        {
                            value = float.Parse(res.Remove(0, charCheck + 1));
                        }
                        catch { }
                    }

                    if (res[charCheck] == 'e')
                    {
                        e.type = DialogueEvent.DialogueEventType.Action;

                        int IVal = (int)value;
                        if (settings.events.Count > IVal && IVal >= 0)
                        {
                            e.e = settings.events[IVal];
                        }
                    }
                    if (res[charCheck] == 'c')
                    {
                        e.type = DialogueEvent.DialogueEventType.Color;

                        int IVal = (int)value;
                        if (settings.colors.Count > IVal && IVal >= 0)
                        {
                            e.color = settings.colors[IVal];
                        }
                    }
                    if (res[charCheck] == 's')
                    {
                        e.type = DialogueEvent.DialogueEventType.Speeed;

                        e.speed = value;
                    }
                    if (res[charCheck] == 'a' && res[charCheck + 1] == 's')
                    {
                        e.type = DialogueEvent.DialogueEventType.AutoSkip;
                    }
                    if (res[charCheck] == 'u' && res[charCheck + 1] == 's')
                    {
                        e.type = DialogueEvent.DialogueEventType.Unskip;
                    }
                    if (res[charCheck] == 'v')
                    {
                        e.type = DialogueEvent.DialogueEventType.voice;

                        int IVal = (int)value;
                        if (settings.voices.Count > IVal && IVal >= 0)
                        {
                            e.voiceClip = settings.voices[IVal];
                        }
                    }
                    if (res[charCheck] == 's' && res[charCheck + 1] == 'h')
                    {
                        e.type = DialogueEvent.DialogueEventType.Shake;
                    }

                    if (e.type == DialogueEvent.DialogueEventType.PublicText)
                    {
                        e.publicTextKey = res;
                    }

                    e.index = startIndex;

                    currentEvents.Add(e);

                    int removeLength = res.Length + 2;

                    text = text.Remove(startIndex, removeLength);

                    i -= removeLength;
                }
            }
        }

        settings.texts[currentPage].startEvent.Invoke();

        stage = 2;
        toWriteText = text;
        writeRoutene = StartCoroutine(textHandeler(toWriteText));
    }

    void OnDialogueForward()
    {
        if (settings.unskippable && stage != 3) return;

        switch (stage)
        {
            case 1:
                animator.SetBool("skipOpening", true);
                break;

            case 2:
                StopCoroutine(writeRoutene);
                stage = 3;

                textArea.text = string.Empty;
                //effetcs.Refresh();

                for (int eventIndex = 0; eventIndex < toWriteText.Length; eventIndex++)
                {
                    string tempDisplayText = textArea.text;

                    //check for events

                    lastWrittenIndex = eventIndex;

                    var events = getDialogueEvents(eventIndex);

                    for (int i = 0; i < events.Count; i++)
                    {
                        DialogueEvent DEvent = events[i];

                        if (DEvent != null)
                        {
                            switch (DEvent.type)
                            {
                                case DialogueEvent.DialogueEventType.Speeed:
                                    if (DEvent.isCancelation)
                                        currentSpeed = settings.startSpeed;
                                    else
                                        currentSpeed = DEvent.speed;

                                    break;
                                case DialogueEvent.DialogueEventType.Action:
                                    if (DEvent.e != null)
                                        DEvent.e.Invoke();
                                    else
                                        Debug.LogError("Dialogue event failed!");

                                    break;
                                case DialogueEvent.DialogueEventType.Color:
                                    if (DEvent.isCancelation)
                                        tempDisplayText += "</color>";
                                    else
                                        tempDisplayText += "<#" + DEvent.color.ToHexString() + ">";
                                    break;
                                case DialogueEvent.DialogueEventType.Shake:
                                    if (DEvent.isCancelation)
                                        tempDisplayText += "</link>";
                                    else
                                        tempDisplayText += $"<link=shake>";

                                    break;
                                case DialogueEvent.DialogueEventType.AutoSkip:
                                    settings.autoSkip = !DEvent.isCancelation;
                                    settings.unskippable = !DEvent.isCancelation;

                                    break;

                                case DialogueEvent.DialogueEventType.Unskip:
                                    settings.unskippable = !DEvent.isCancelation;

                                    break;

                                case DialogueEvent.DialogueEventType.voice:
                                    if (DEvent.isCancelation)
                                        settings.speakSound = originalSpeakSound;
                                    else
                                    {
                                        settings.speakSound = DEvent.voiceClip;
                                    }

                                    break;

                                case DialogueEvent.DialogueEventType.PublicText:
                                    if (DialogueManager.Get().getPublicTextVars().ContainsKey(DEvent.publicTextKey))
                                        tempDisplayText += DialogueManager.Get().getPublicTextVars()[DEvent.publicTextKey];

                                    break;
                            }

                            currentEvents.Remove(DEvent);
                        }
                    }

                    //add text

                    tempDisplayText += toWriteText[eventIndex];

                    textArea.text = tempDisplayText;
                    //effetcs.Refresh();
                }

                if (settings.speakSound)
                    AudioManager.PlayTemporarySource(settings.speakSound, settings.speakSoundVolume, 1, settings.name + " speak sound");

                break;

            case 3:
                if (!settings.autoSkip)
                    progressDialogue();
                break;

            case 4:
                Destroy(gameObject);
                break;

            default:
                break;
        }
    }

    IEnumerator textHandeler(string text)
    {
        int length = text.Length;
        while (length > 0)
        {
            yield return new WaitForSeconds(currentSpeed);

            string tempDisplayText = textArea.text;

            //check for events

            int eventIndex = text.Length - length;

            lastWrittenIndex = eventIndex;

            var events = getDialogueEvents(eventIndex);

            for (int i = 0; i < events.Count; i++)
            {
                DialogueEvent DEvent = events[i];

                if (DEvent != null)
                {
                    switch (DEvent.type)
                    {
                        case DialogueEvent.DialogueEventType.Speeed:
                            if (DEvent.isCancelation)
                                currentSpeed = settings.startSpeed;
                            else
                                currentSpeed = DEvent.speed;

                            break;
                        case DialogueEvent.DialogueEventType.Action:
                            if (DEvent.e != null)
                                DEvent.e.Invoke();
                            else
                                Debug.LogError("Dialogue event failed!");

                            break;
                        case DialogueEvent.DialogueEventType.Color:
                            if (DEvent.isCancelation)
                                tempDisplayText += "</color>";
                            else
                                tempDisplayText += "<#" + DEvent.color.ToHexString() + ">";

                            break;
                        case DialogueEvent.DialogueEventType.Shake:
                            if (DEvent.isCancelation)
                                tempDisplayText += "</link>";
                            else
                                tempDisplayText += $"<link=shake>";

                            break;
                        case DialogueEvent.DialogueEventType.AutoSkip:
                            settings.autoSkip = !DEvent.isCancelation;
                            settings.unskippable = !DEvent.isCancelation;

                            break;

                        case DialogueEvent.DialogueEventType.Unskip:
                            settings.unskippable = !DEvent.isCancelation;

                            break;

                        case DialogueEvent.DialogueEventType.voice:
                            if (DEvent.isCancelation)
                                settings.speakSound = originalSpeakSound;
                            else
                                settings.speakSound = DEvent.voiceClip;

                            break;

                        case DialogueEvent.DialogueEventType.PublicText:
                            if (DialogueManager.Get().getPublicTextVars().ContainsKey(DEvent.publicTextKey))
                                tempDisplayText += DialogueManager.Get().getPublicTextVars()[DEvent.publicTextKey];

                            break;
                    }
                }
            }

            //add text

            tempDisplayText += text[eventIndex];

            if (settings.speakSound)
                AudioManager.PlayTemporarySource(settings.speakSound, settings.speakSoundVolume, 1, settings.name + " speak sound");

            textArea.text = tempDisplayText;
            //effetcs.Refresh();

            length--;
        }

        stage = 3;

        if (settings.autoSkip && settings.autoSkipSpeed != -1)
        {
            yield return new WaitForSeconds(settings.autoSkipSpeed);
            progressDialogue();
        }
    }

    public void ExitDialogue()
    {
        animator.SetInteger("exitTransition", (int)settings.exitTransition);
        animator.SetBool("exiting", true);
        textArea.gameObject.SetActive(false);
        stage = 4;
    }

    DialogueEvent getDialogueEvent(int i)
    {
        DialogueEvent eventt = null;
        for (int e = 0; e < currentEvents.Count; e++)
        {
            if (currentEvents[e].index == i)
                eventt = currentEvents[e];
        }
        return eventt;
    }

    List<DialogueEvent> getDialogueEvents(int i)
    {
        List<DialogueEvent> eventt = new List<DialogueEvent>();
        for (int e = 0; e < currentEvents.Count; e++)
        {
            if (currentEvents[e].index == i)
                eventt.Add(currentEvents[e]);
        }
        return eventt;
    }
    
}

[System.Serializable]
public class DialogueEvent
{
    public int index;
    public enum DialogueEventType
    {
        PublicText,
        Speeed,
        Action,
        Color,
        AutoSkip,
        Unskip,
        voice,
        Shake
    }
    public DialogueEventType type;
    [HideInInspector]
    public string publicTextKey;
    [HideInInspector]
    public UnityEvent e;
    [HideInInspector]
    public float speed;
    [HideInInspector]
    public Color color;
    [HideInInspector]
    public AudioClip voiceClip;

    public bool isCancelation;
}