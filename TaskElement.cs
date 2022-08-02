using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TaskElement : MonoBehaviour
{
    public TextMeshProUGUI Text1, Text2;
    public Button Btn;
    private string mTaskId = string.Empty;
    private GamePlayController mGamePlayController;
    private Hud mHud;
    // Start is called before the first frame update
    void Start()
    {
        Btn.onClick.AddListener(OnClick);
    }
    void OnClick() {
        Debug.Log("Task OnClick. " + mTaskId);
        mHud.ReleaseTask();
        var actor = mGamePlayController.GetFollowActor();
        if(actor != null) {
            if(!actor.mActor.Loop_SetTask(mTaskId)) {
                Debug.Log("Set Task Failure");
                actor.mActor.Loop_TaskUI();
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void Init(Hud hud, GamePlayController gamePlayController) {
        mHud = hud;
        mGamePlayController = gamePlayController;
    }
    public void Set(string taskId, string text1, string text2) {
        Text1.text = text1;
        Text2.text = text2;
        mTaskId = taskId;
    }
}
