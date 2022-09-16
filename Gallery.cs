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
        LoadingScene.LoadScene(MainSceneName);
    }
}
