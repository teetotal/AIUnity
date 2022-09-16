using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IntroScene : MonoBehaviour
{
    [SerializeField]
    private Button btnStart;
    // Start is called before the first frame update
    void Start()
    {
        btnStart.onClick.AddListener(OnStart);        
    }

    void OnStart() {
        LoadingScene.LoadScene("Demo");
    }
}
