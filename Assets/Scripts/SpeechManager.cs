using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows.Speech;


// Credits: https://elbruno.com/2016/10/26/hololens-using-voice-commands-to-display-a-menu/
public class SpeechManager : MonoBehaviour {
    private KeywordRecognizer keywordRecognizer;
    private readonly Dictionary<string, Action> keywords = new Dictionary<string, Action>();

    public GameObject mainMenu;

	void Start ()
    {
        keywords.Add("Menu", PlaceInFront);

        keywordRecognizer = new KeywordRecognizer(keywords.Keys.ToArray());
        keywordRecognizer.OnPhraseRecognized += KeywordRecognizer_OnPhraseRecognized;
        keywordRecognizer.Start();
	}

    private void KeywordRecognizer_OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        Action keywordAction;
        if (keywords.TryGetValue(args.text, out keywordAction))
            keywordAction.Invoke();
    }

    private void PlaceInFront()
    {
        mainMenu.SetActive(true);
        mainMenu.transform.position = Camera.main.transform.position + Camera.main.transform.forward;
    }
}
