using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows.Speech;

public class SpeechManager : MonoBehaviour
{
    public AudioSource audioSource;
    private DictationRecognizer dictationRecognizer;
    private string[] questions;
    private List<string> answers = new List<string>();
    private int currentQuestion = 0;

    public void SetQuestions(string[] questions)
    {
        this.questions = questions;
        ProcessNextQuestion();
    }

    private void ProcessNextQuestion()
    {
        if (currentQuestion < questions.Length)
        {
            Speak(questions[currentQuestion]);
        }
        else
        {
            Debug.Log("All questions are done.");
            // Handle the completion of all questions here, e.g., send answers to the server or store them locally.
        }
    }

    private void Speak(string textToSpeak)
    {
        StartCoroutine(SynthesizeAndPlay(textToSpeak, () =>
        {
            Debug.Log("Speech Completed.");
            StartCoroutine(AskQuestion());
        }));
    }

    IEnumerator SynthesizeAndPlay(string text, System.Action onComplete)
    {
        using (var tts = new TextToSpeech())
        {
            TextToSpeech.SynthesisReady += (s, e) =>
            {
                audioSource.clip = e.GetAudioClip();
                audioSource.Play();
            };

            tts.Synthesize(text);
        }

        while (audioSource.isPlaying)
        {
            yield return null;
        }

        onComplete();
    }

    IEnumerator AskQuestion()
    {
        dictationRecognizer = new DictationRecognizer();

        dictationRecognizer.DictationResult += (text, confidence) =>
        {
            Debug.Log($"User's answer: {text}");
            answers.Add(text);
            dictationRecognizer.Stop();
            dictationRecognizer.Dispose();
            currentQuestion++;
            ProcessNextQuestion();
        };

        dictationRecognizer.DictationError += (error, hresult) =>
        {
            Debug.LogError($"Dictation error: {error}");
            dictationRecognizer.Dispose();
        };

        dictationRecognizer.Start();
        yield return null;
    }

    private void OnDisable()
    {
        if (dictationRecognizer != null)
        {
            dictationRecognizer.DictationResult -= OnDictationResult;
            dictationRecognizer.DictationError -= OnDictationError;
            dictationRecognizer.Dispose();
        }
    }
}
