using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class GPTCaller : MonoBehaviour
{
   public Request newReq;
   public static Message response;
   public SpeechManager speechManager;
   public static string[] questionArray;


   public static Message[] conversation;
   public int count;
   private string apiKeyConfig = "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";
   private string ApiUrl = "https://api.openai.com/v1/chat/completions";


   public IEnumerator SendRequest(Request prompt)
   {
       var request = new UnityWebRequest(ApiUrl, "POST");
       var jsonData = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(prompt));
       request.uploadHandler = new UploadHandlerRaw(jsonData);
       request.downloadHandler = new DownloadHandlerBuffer();
       request.SetRequestHeader("Content-Type", "application/json");
       request.SetRequestHeader("Authorization", $"Bearer {apiKeyConfig}");


       yield return request.SendWebRequest();


       if (request.isNetworkError)
       {
           Debug.Log("Error While Sending: " + request.error);
       }
       else
       {
           response = JsonUtility.FromJson<Choices>(request.downloadHandler.text).choices[0].message;
           Debug.Log(response.content);
           Debug.Log("Received: " + JsonUtility.ToJson(response));

           questionArray = response.content.Trim('{', '}').Split("NEXT");
           speechManager.SetQuestions(questionArray);
       }
   }




   // Start is called before the first frame update
   void Start()
   {
       Message[] messages = new Message[] {new Message("system", "You are an AI for a VR Job Interview Simulator. You will take on the role of the Interviewer and Interview the user.Generate 5 helpful interview questions in this format: '[QUESTION1]: question 1 text NEXT  [QUESTION2]: question 2 text NEXT [QUESTION3]: question 3 text NEXT [QUESTION4]: question 4 text NEXT [QUESTION5]: question 5 text NEXT Feedback. At the end of the interview, invite the user to ask any questions they may have about the position or company. After the interview, provide detailed feedback on the interview, including both positive aspects and areas for improvement. Consider the content and delivery of the user's responses. Offer at least 5 specific and actionable tips for improvement, focusing on aspects such as attitude, qualifications, presentation skills, and other relevant attributes. Be friendly and supportive throughout the process, while also being honest abwwwout the user's performance. Assign a final letter grade reflecting the user's interview skills. Be truthful, so they know where they need to improve and can excel in real-world interviews. Your goal is to help the user ace their real-world interview after practicing with you. Don't let them down! Name of the company: Insight Global Interviewee Name: Ben Job Title: Software Engineer Job Description: Must-Haves: - 5+ years of experience designing/developing web applications for products using WPF, C# - 3 - 5 years of SQL database - Proven analytical and problem-solving abilities. - Strong understanding and demonstrated usage of object-oriented design concepts. - Experience with software versioning and release management. Ability to effectively prioritize and execute tasks in a high-pressure environment. Experience working both independently and in a team-oriented, collaborative environment. Excellent communication skills. Day-to-Day: Benjamin Moore is currently seeking a fullstack developer with strong .NET and JavaScript skills to join their Application Development team at their Corporate Headquarters in Montvale, NJ three days a week. The role requires that the individual be a self-starter with demonstrated skills in .NET, JavaScript, SQL Server, and a strong understanding of enterprise application architecture, including Azure cloud development.")};

       newReq = new Request(
           "gpt-3.5-turbo",
           messages,
           0.7
       );


       Debug.Log(JsonUtility.ToJson(newReq));


       StartCoroutine(SendRequest(newReq));

   }


   // Update is called once per frame
   void Update()
   {

   }


   [ContextMenu("TestMyMethod")]
   void TestMyMethod()
   {


       Start();


   }
}


// All the following serializable objects are because Unity JSON handling is stupid
[System.Serializable]
public class Request
{
   public string model;
   public Message[] messages;
   public double temperature;


   public Request(string model, Message[] messages, double temperature)
   {
       this.model = model;
       this.messages = messages;
       this.temperature = temperature;
   }
}


[System.Serializable]
public class Message
{
   public string role;
   public string content;


   public Message(string role, string content)
   {
       this.role = role;
       this.content = content;
   }
}


[System.Serializable]
public class Choices
{
   public string id;
   public Choice[] choices;
}


[System.Serializable]
public class Choice
{
   public int index;
   public Message message;
   public string finish_reason;
}
