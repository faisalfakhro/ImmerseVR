using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class TitleScript : MonoBehaviour
{
    public static string prompt;
    static bool done = false;
    static bool changed = false;

    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(transform.gameObject);
        DontDestroyOnLoad(this);
        StartCoroutine(getRequest("https://immerse-prep.herokuapp.com/create-interview/0"));
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.anyKey & done & !changed)
        {
            changed = true;
            SceneManager.LoadScene(1);
        }
    }


    IEnumerator getRequest(string uri)
    {
        UnityWebRequest uwr = UnityWebRequest.Get(uri);
        yield return uwr.SendWebRequest();

        if (uwr.isNetworkError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
        }
        else
        {
            Debug.Log("Received: " + uwr.downloadHandler.text);
            prompt = uwr.downloadHandler.text;
            done = true;
        }
    }
}
