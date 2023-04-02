using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.CognitiveServices.Speech;

public class SpeechToTextAndTextToSpeech : MonoBehaviour
{
    // Hook up the UI elements in your Unity project
    public Text outputText;
    // public Button startButton;
    public AudioSource audioSource;

    private int count = 0;
    private object threadLocker = new object();
    private bool waitingForReco;
    private bool waitingForSpeak;
    private string message;
    private bool micPermissionGranted = false;
    private float dt = 12;

    private const string SubscriptionKey = "7bef5f5fddc0449daae818ae049f1cdf";
    private const string Region = "eastus";

    private const int SampleRate = 24000;

    private SpeechConfig speechConfig;
    private SpeechSynthesizer synthesizer;

    void Start()
    {
        if (outputText == null)
        {
            UnityEngine.Debug.LogError("outputText property is null! Assign a UI Text element to it.");
        }
        // else if (startButton == null)
        // {
        //     message = "startButton property is null! Assign a UI Button to it.";
        //     UnityEngine.Debug.LogError(message);
        // }
        else
        {
            // Continue with normal initialization, Text and Button objects are present.
#if PLATFORM_ANDROID
            // Request to use the microphone
            message = "Waiting for mic permission";
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
            message = "Click button to start";
#endif
            // startButton.onClick.AddListener(StartSpeechCycle);

            // Creates an instance of a speech config with specified subscription key and service region.
            speechConfig = SpeechConfig.FromSubscription(SubscriptionKey, Region);

            // Set the format to raw (24KHz for better quality).
            speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Raw24Khz16BitMonoPcm);

            // Creates a speech synthesizer.
            synthesizer = new SpeechSynthesizer(speechConfig, null);
        }
    }

    public async void StartSpeechCycle()
    {
        while (true)
        {
            string inputText = await ListenForUserInput();
            Debug.Log("You said: " + inputText);
            string responseText = ProcessUserInput(inputText);
            await Speak(responseText);
        }
    }

    private async System.Threading.Tasks.Task<string> ListenForUserInput()
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

    private string ProcessUserInput(string inputText)
    {
        // Process user input and generate a response
        // Replace this with your own logic to process user input
        Debug.Log(count);
        Debug.Log(GPTCaller.questionArray);
        string responseText = GPTCaller.questionArray[count];
        count++;
        Debug.Log("try to speak: " + responseText);
        return responseText;
    }

    private async System.Threading.Tasks.Task Speak(string text)
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

    void Update()
    {
        dt += Time.deltaTime;
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

        lock (threadLocker)
        {
            // if (startButton != null)
            // {
            //     startButton.interactable = !waitingForReco && !waitingForSpeak && micPermissionGranted;
            // }
            if (outputText != null)
            {
                outputText.text = message;
            }
        }
    }

    void OnGUI()
    {
        Event e = Event.current;
        if(e.isKey & dt > 15.0)
        {
            StartSpeechCycle();
            dt = 0;
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

