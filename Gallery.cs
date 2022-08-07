using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Gallery : MonoBehaviour
{
    public Button CloseButton;
    public string MainSceneName;
    // Start is called before the first frame update
    void Start()
    {
        CloseButton.onClick.AddListener(CloseScene);
    }

    void CloseScene()
    {
        StartCoroutine(LoadAsyncScene(MainSceneName));
    }
    IEnumerator LoadAsyncScene(string scene)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene);

        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            yield return null;
            Debug.Log("Comeback " + asyncLoad.progress.ToString());
        }
    }
}
