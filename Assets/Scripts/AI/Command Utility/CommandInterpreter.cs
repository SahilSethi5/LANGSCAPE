/*
 * Author:
 * Script Description:
 *      Handles converting voice input into valid JSON object.
 * NOTES:
 *      - consider moving Debug.Log() that outputs the command to DebugSuite
 * TO DO:
 *      - Interpret JSON string to valid JSON
 *      - Expand on the instructions for the prompt
 *      - Enable speech recognition via OVR
 */

using OpenAI;
using System.Collections.Generic;
using System;
using Search;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;
using static UnityEngine.Rendering.DebugUI;
#if !UNITY_STANDALONE_WIN
using Oculus.Interaction;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;


//using UnityEngine.Windows.Speech;
#endif

public class CommandInterpreter : MonoBehaviour
{

    // My Code
    private const string LocalhostUri = "http://131.94.128.132:9080"; // Replace "your_port_here" with your actual port
    private const string Model = "mistral"; // Use your desired model
    private const string ApiKey = ""; // Not needed for local server

    private static readonly HttpClient _gptClient;

    private const string myMessage = "";

    // My code end


    // Display
    [SerializeField] private TMP_Text inputBox;
    [SerializeField] private TMP_Text outputBox;
    [SerializeField] private UnityEngine.UI.Image symbol;
    [SerializeField] private TMP_Dropdown dropdown;

    // OpenAI Settings
    private OpenAIApi openai;
    private OpenAIApi b_llm;
    private string prompt;
    private string b_prompt;

    // Whisper
    private readonly string fileName = "output.wav";
    private readonly int MAX_DURATION = 30;
    private AudioClip clip;
    private bool isRecording;
    private float time = 0;

    //Gesture Detect
#if !UNITY_STANDALONE_WIN
    public GestureTest gesture;
    public GestureTest gesture2;
#endif

    // ChatGPT
    private List<ChatMessage> messages = new List<ChatMessage>();
    private List<ChatMessage> b_messages = new List<ChatMessage>();

    // Search
    private SearchAlgorithms sa = new SearchAlgorithms();
    private bool change;

    // Command Indicators for Only Instructions 
    // :: Block Building => 1 for Only Commands, 2 for Words and Commands ::
    string[] indicator = { "f ", "m ", "r ", "c ", "u ", "v ", "q ", "t " };
    string[] indicator2 = { " f ", " m ", " r ", " c ", " u ", " v ", " q ", " t ", "\nf ", "\nm ", "\nr ", "\nc ", "\nu ", "\nv ", "\nq ", "\nt " };
    // :: Background Building => Switch Keyword, 3 for Only Commands, 4 for Words and Commands ::

#if UNITY_STANDALONE_WIN
    string LLM_keyword = "background"; //for windows version
#endif

    string[] indicator3 = { "d ", "l ", "z ", "o " };
    string[] indicator4 = { " d ", " l ", " z ", " o ", "\nd ", "\nl ", "\nz ", "\no " };

    // Loads prompt from file in Assets/Resources/prompt
    void Awake()
    {
        openai = new OpenAIApi(apiKey: "");
        // 1st LLM
        TextAsset filedata = Resources.Load<TextAsset>("OpenAI/PROMPT");
        if (filedata == null)
            throw new System.Exception("No file found called prompt in 'Assets/Resources/OpenAI/PROMPT");
        prompt = filedata.text;
        Debug.Log(prompt);

        // Background LLM
        b_llm = new OpenAIApi(apiKey: "");
        filedata = Resources.Load<TextAsset>("OpenAI/BACKGROUND");
        if (filedata == null)
            throw new System.Exception("No file found called prompt in 'Assets/Resources/OpenAI/BACKGROUND");
        b_prompt = filedata.text;
        Debug.Log(b_prompt);

    }

    private void Start()
    {
        AIMic.Instance.SpeakFluff("Hi there! I'm Langscape, your AI assistant. Need help building something or changing the background? Just give me a shout!");
        outputBox.text = "Hi there! I'm Langscape, your AI assistant. Need help building something or changing the background? Just give me a shout!";

        dropdown.ClearOptions();
        foreach (var device in Microphone.devices)
        {
            dropdown.options.Add(new TMP_Dropdown.OptionData(device));
        }
        dropdown.onValueChanged.AddListener(ChangeMicrophone);
        inputBox.text = "...";
        var index = PlayerPrefs.GetInt("user-mic-device-index");
        dropdown.SetValueWithoutNotify(index);
        dropdown.RefreshShownValue();

        messages.Add(new ChatMessage() { Role = "system", Content = prompt });
        b_messages.Add(new ChatMessage() { Role = "system", Content = b_prompt });
    }
    private void ChangeMicrophone(int index)
    {
        PlayerPrefs.SetInt("user-mic-device-index", index);
    }
    private void StartRecording()
    {
        isRecording = true;
        symbol.enabled = true;
        inputBox.text = "Listening...";

#if UNITY_STANDALONE_WIN
        var index = PlayerPrefs.GetInt("user-mic-device-index");
        clip = Microphone.Start(dropdown.options[index].text, false, MAX_DURATION, 44100);
#else
        //var index = PlayerPrefs.GetInt("user-mic-device-index");
        clip = Microphone.Start(dropdown.options[0].text, false, MAX_DURATION, 44100);
#endif
    }
    private async void EndRecording()
    {
        isRecording = false;
        symbol.enabled = false;
        time = 0;
        inputBox.text = "Transcribing...";
        Microphone.End(null);
        byte[] data = SaveWav.Save(fileName, clip);
        var req = new CreateAudioTranscriptionsRequest
        {
            FileData = new FileData() { Data = data, Name = "audio.wav" },
            Model = "whisper-1",
            Language = "en"
        };
        var res = await openai.CreateAudioTranscription(req);
        inputBox.text = res.Text;
        if (res.Error != null)
            inputBox.text = "You wont believe it.";
        if (res.Text != "")
        {
            CreateJSON(res.Text);
            // SendMessageToChatGPTLocalAsync(res.Text);
        }

    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_STANDALONE_WIN
        if (Input.GetKeyDown(KeyCode.V))
            StartRecording();
        if (Input.GetKeyUp(KeyCode.V))
            EndRecording();
#else
        if (!isRecording && (gesture.selected || gesture2.selected))
        {
            change = gesture2.selected;
            StartRecording();
        }
        if (isRecording && (!gesture.selected && !gesture2.selected))
        {
            EndRecording();
        }
#endif

        if (isRecording)
        {
            time += Time.deltaTime;
            if (time >= MAX_DURATION)
                EndRecording();
        }
    }
    private async void CreateJSON(string request)
    {
        ChatMessage userRequest = new ChatMessage()
        {
            Role = "user",
            Content = request
        };

#if UNITY_STANDALONE_WIN
                change = sa.SwitchLLM(userRequest.Content, LLM_keyword);
#endif

        outputBox.text = "Loading response...";

        // If User Input has key indicator "background" (case-insensitive), Switch to Second LLM
        if (change)
        {
            b_messages.Add(userRequest);

            // Complete the instruction
            try
            {
                Message allMessage = await SendMessageToChatGPTLocalAsync(b_messages); //userRequest was here before
                                                                                       // {
                                                                                       //     Model = "gpt-4",
                                                                                       //     Messages = b_messages,
                                                                                       //     Temperature = 0f,
                                                                                       //     MaxTokens = 256,
                                                                                       //     PresencePenalty = 0,
                                                                                       //     FrequencyPenalty = 0
                                                                                       // });


                ChatMessage completionResponse = new ChatMessage() { Role = "system", Content = allMessage.content };

                if (completionResponse.Content != null)
                {
                    var aiRespa = completionResponse;

                    string fluff = string.Empty; // Sentence
                    string instruct = string.Empty; // Command

                    // If Message Contains Only the Command don't Modify, otherwise Modify
                    if (sa.hasOnlyCommand(aiRespa.Content, indicator3) == false && aiRespa.Content != "n")
                    {
                        var Updated = sa.fullSplit((string)aiRespa.Content, indicator3, indicator4);

                        // Only The Instructions
                        instruct = Updated.command;
                        // Only the Message
                        fluff = Updated.sentence;

                        // If Instruct Has No Instructions, Change to "n" (For Now)
                        if (string.IsNullOrEmpty(instruct) == false)
                        {
                            aiRespa.Content = instruct;
                        }
                        else
                        {
                            aiRespa.Content = "n";
                        }

                    }

                    aiRespa.Content = aiRespa.Content.Trim();

                    b_messages.Add(aiRespa);

                    Debug.Log("Printing fluff: " + fluff);

                    // Checks if Sentence Declaration is Not Empty
                    if (!string.IsNullOrEmpty(fluff))
                    {
                        // Tells TTSpeaker to Speak fluff
                        AIMic.Instance.SpeakFluff(fluff);
                    }
                    else
                    {
                        // Outputs Ai Response without Commands into Output Box
                        fluff = "AI Responded";
                    }

                    // Outputs Ai Response without Commands into Output Box
                    outputBox.text = fluff;
                    // Outputs Ai Response without Sentence into Debug Log
                    Debug.Log("Command: " + aiRespa.Content);

                    // Implement Background Manager Instance
                    BackgroundManager.Instance.Execute(aiRespa.Content);

                }
                else
                {
                    outputBox.text = "No text was generated from this prompt.";
                }
            }
            catch (System.Exception e)
            {
                outputBox.text = e.Message;
            }

            //outputBox.text = "Background Changes Currently Not Implemented";
        }
        else
        {

            List<ChatMessage> temp_messages = new List<ChatMessage>();
            temp_messages = messages;
            messages.Add(userRequest);

            // Complete the instruction
            try
            {
                Message allMessage = await SendMessageToChatGPTLocalAsync(messages); //userRequest was here before

                ChatMessage completionResponse = new ChatMessage() { Role = "system", Content = allMessage.content };

                Debug.Log("completionResponse: " + completionResponse.Content);

                if (completionResponse.Content != null)
                {
                    var aiResponse = completionResponse;

                    Debug.Log("AI response: " + aiResponse.Content);

                    string fluff = string.Empty; // Sentence
                    string instruct = string.Empty; // Command

                    //new
                    if (sa.hasOnlyCommand(aiResponse.Content, indicator) == false && aiResponse.Content != "n")
                    {
                        var Updated = sa.fullSplit((string)aiResponse.Content, indicator, indicator2);

                        // Only The Instructions
                        instruct = Updated.command;
                        // Only the Message
                        fluff = Updated.sentence;

                        if (string.IsNullOrEmpty(instruct) == false)
                        {
                            aiResponse.Content = instruct;
                        }

                        // Comment out since no prompting done, do prompting and comment this back in
                        else
                        {
                            aiResponse.Content = "n";
                        }
                    }
                    aiResponse.Content = aiResponse.Content.Trim();

                    //new end

                    //messages.Add(aiResponse);

                    // Checks if Sentence Declaration is Not Empty
                    if (!string.IsNullOrEmpty(fluff))
                    {
                        // Tells TTSpeaker to Speak fluff
                        AIMic.Instance.SpeakFluff(fluff);
                    }
                    else
                    {
                        // Outputs Ai Response without Commands into Output Box
                        fluff = "AI Responded"; //change later back to this
                        ///fluff = aiResponse.Content;
                    }

                    // Outputs Ai Response without Commands into Output Box
                    outputBox.text = fluff;

                    // Outputs Ai Response without Sentence into Debug Log
                    Debug.Log("Command: " + aiResponse);

                    messages = temp_messages;




#if TEST_CUBEPLACER
            WorldCommand commands = JSONParser.ParseCommand(message.Content);
            if (commands != null && commands.success && commands.modified != null) {
                Debug.Log("Placing: " + commands.modified.Length);
                GridMesh.Instance.Multiplace(commands.modified);
            }
#else
                    //Debug.Log("CommandInterpreter message: " + message.Content.ToString()); //[[[CONSIDER MOVING THIS TO DEBUG SUITE]]]
                    WorldStateManager.Instance.BuildCommandBatch(aiResponse.Content, userRequest.Content);

#endif
                }
                else
                {
                    outputBox.text = "No text was generated from this prompt.";
                }
            }
            catch (System.Exception e)
            {
                outputBox.text = e.Message; //error
            }

            //Remove the last user message
            //private List<ChatMessage> temp_messages = new List<ChatMessage>();


        }
    }

    //MY code

    public static async Task<Message> SendMessageToChatGPTLocalAsync(List<ChatMessage> message)
    {
        var localhostUri = "http://131.94.128.132:9080"; // Replace PORT with the actual port of your local server
        using (var client = new HttpClient())
        {
            client.BaseAddress = new Uri(localhostUri);

            // for(//amount in messages for each each message){
            //     message[i].content = 
            // }

            string allMessage = "";

            for (int i = 0; i < message.Count; i++)
            {

                allMessage += message[i].Content + " ";

                if (i == message.Count - 1)
                {
                    Debug.Log("Test" + message[i].Content);
                }

            }

            Debug.Log("Test: " + allMessage);

            var data = new MyData
            {
                model = "mistral",
                messages = new[]
                {
                new Message
                {
                    role = "user",
                    content = allMessage,

                }
            },
                stream = false,
                temperature = 0f,
                max_tokens = 20,
                // Add other parameters as needed
            };

            Debug.Log("Data print" + data.messages[0].content);


            var json = JsonConvert.SerializeObject(data);
            Debug.Log(json);

            // var json = JsonSerializer.Serialize(new
            // {
            //     Model = "mistral",
            //     Messages = new[]
            //     {
            //         new
            //         {
            //             Role = "user",
            //             Content = message
            //         }
            //     },
            //     Temperature = 0f,
            //     MaxTokens = 256,
            //     PresencePenalty = 0,
            //     FrequencyPenalty = 0
            //     // Add other parameters as needed
            // });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/chat", content);
            Debug.Log("Test4: " + response);

            Message contentJson = new Message() { role = "system", content = "ERROR" };

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();

                string[] jsonResponseStrings = responseContent.Split('\n', (char)StringSplitOptions.RemoveEmptyEntries);

                foreach (string jsonResponseString in jsonResponseStrings)
                {
                    // Check if the JSON string is not empty before parsing
                    if (!string.IsNullOrWhiteSpace(jsonResponseString))
                    {
                        // Parse each JSON object
                        JObject jsonResponse = JObject.Parse(jsonResponseString);

                        contentJson.content = jsonResponse["message"]["content"].ToString();
                    }
                }

                Debug.Log("Test3:" + contentJson.content);

                // Remove extra output from the response we get
                string command = "";
                int consecutiveNewlines = 0;
                for (int i = 0; i < contentJson.content.Length; i++)
                {
                    if (consecutiveNewlines < 2)
                    {
                        command += contentJson.content[i];
                    }

                    // Check for consecutive newline characters
                    if (contentJson.content[i] == '\n')
                    {
                        consecutiveNewlines++;
                        if (consecutiveNewlines == 2)
                        {
                            break; // Stop printing if two consecutive newline characters are found
                        }
                    }
                    else
                    {
                        consecutiveNewlines = 0; // Reset consecutive newline counter if a non-newline character is encountered
                    }
                }

                // contentJson.content is now command
                contentJson.content = command;

                return contentJson;
            }
            else
            {
                return new Message() { role = "system", content = "ERROR" }; ;
            }
        }
    }

    public class MyData
    {
        public string model;
        public Message[] messages;
        public bool stream;
        public float temperature;

        public int max_tokens;
        // Add other parameters as needed
    }

    public class Message
    {
        public string role;
        public string content;

    }

}
