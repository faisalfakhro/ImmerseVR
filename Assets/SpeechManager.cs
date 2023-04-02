using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Microsoft.CognitiveServices.Speech;

public class SpeechManager : MonoBehaviour
{
    public AudioSource audioSource;

    private object threadLocker = new object();
    private bool waitingForReco;
    private bool waitingForSpeak;
    private bool micPermissionGranted = false;

    private const string SubscriptionKey = "YourSubscriptionKey";
    private const string Region = "YourServiceRegion";
    private const int SampleRate = 24000;
    private bool hasStarted = false;

    private SpeechConfig speechConfig;
    private SpeechSynthesizer synthesizer;

    // New members for handling questions
    private string[] questions;
    private List<string> answers = new List<string>();
    private int currentQuestion = 0;

    void Start()
    {
        // Continue with normal initialization.
#if PLATFORM_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
#elif PLATFORM_IOS
        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            Application.RequestUserAuthorization(UserAuthorization.Microphone);
        }
#else
        micPermissionGranted = true;
#endif

        // Creates an instance of a speech config with specified subscription key and service region.
        speechConfig = SpeechConfig.FromSubscription(SubscriptionKey, Region);

        // Set the format to raw (24KHz for better quality).
        speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Raw24Khz16BitMonoPcm);

        // Creates a speech synthesizer.
        synthesizer = new SpeechSynthesizer(speechConfig, null);
    }

    public void StartSpeechCycle()
    {
        SetQuestions(new string[] { "Hello! Nice to meet you. I will be conducting your interview today. To get started, please tell me your name and give a little introduction about yourself." });
    }

    private async Task<string> ListenForUserInput()
    {
        string recognizedText = string.Empty;
        using (var recognizer = new SpeechRecognizer(speechConfig))
        {
            lock (threadLocker)
            {
                waitingForReco = true;
            }

            var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);

            lock (threadLocker)
            {
                if (result.Reason == ResultReason.RecognizedSpeech)
                {
                    recognizedText = result.Text;
                }
                else if (result.Reason == ResultReason.NoMatch)
                {
                    recognizedText = "NOMATCH: Speech could not be recognized.";
                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = CancellationDetails.FromResult(result);
                    recognizedText = $"CANCELED: Reason={cancellation.Reason} ErrorDetails={cancellation.ErrorDetails}";
                }

                waitingForReco = false;
            }
        }
        return recognizedText;
    }

    private async Task Speak(string text)
    {
        lock (threadLocker)
        {
            waitingForSpeak = true;
        }

        string newMessage = null;
        var startTime = DateTime.Now;

        using (var result = await synthesizer.StartSpeakingTextAsync(text))
        {
            var audioDataStream = AudioDataStream.FromResult(result);
            var isFirstAudioChunk = true;
            var audioClip = AudioClip.Create(
                "Speech",
                SampleRate * 600,
                1,
                SampleRate,
                true,
                (float[] audioChunk) =>
                {
                    var chunkSize = audioChunk.Length;
                    var audioChunkBytes = new byte[chunkSize * 2];
                    var readBytes = audioDataStream.ReadData(audioChunkBytes);
                    if (isFirstAudioChunk && readBytes > 0)
                    {
                        var endTime = DateTime.Now;
                        var latency = endTime.Subtract(startTime).TotalMilliseconds;
                        newMessage = $"Speech synthesis succeeded!\nLatency: {latency} ms.";
                        isFirstAudioChunk = false;
                    }

                    for (int i = 0; i < chunkSize; ++i)
                    {
                        if (i < readBytes / 2)
                        {
                            audioChunk[i] = (short)(audioChunkBytes[i * 2 + 1] << 8 | audioChunkBytes[i * 2]) / 32768.0F;
                        }
                        else
                        {
                            audioChunk[i] = 0.0f;
                        }
                    }
                });

            audioSource.clip = audioClip;
            audioSource.Play();
        }
    }

    // New methods from the second script
    public async void SetQuestions(string[] questions)
    {
        this.questions = questions;
        await ProcessNextQuestion();
    }

    private async Task ProcessNextQuestion()
    {
        if (currentQuestion < questions.Length)
        {
            await Speak(questions[currentQuestion]);
        }
        else
        {
            Debug.Log("All questions are done.");
            // Handle the completion of all questions here, e.g., send answers to the server or store them locally.
        }
    }

    private async void SpeakAndAskQuestion(string textToSpeak)
    {
        await Speak(textToSpeak);
        await AskQuestion();
    }

    private async Task AskQuestion()
    {
        string userAnswer = await ListenForUserInput();
        Debug.Log($"User's answer: {userAnswer}");
        answers.Add(userAnswer);
        currentQuestion++;
        await ProcessNextQuestion();
    }


    async void Update()
    {
    #if PLATFORM_ANDROID
        if (!micPermissionGranted && Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            micPermissionGranted = true;
        }
    #elif PLATFORM_IOS
        if (!micPermissionGranted && Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            micPermissionGranted = true;
        }
    #endif

        if (micPermissionGranted && !hasStarted)
        {
            hasStarted = true;
            await ProcessNextQuestion();
        }
    }

    void OnDestroy()
    {
        if (synthesizer != null)
        {
            synthesizer.Dispose();
        }
    }
}
