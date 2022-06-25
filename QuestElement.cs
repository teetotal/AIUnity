using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ENGINE.GAMEPLAY.MOTIVATION;
public class QuestElement : MonoBehaviour
{
    public Text Title, Desc, ProgressText, Status; 
    public Slider Progress;
    public Button ButtonComplete;
    public string MessageWIP = "진행중";
    public string MessageDone = "완료";
    public string MessageEmpty = string.Empty;
    private const string _percent = "%";
    private const string _emptyProgressText = "-";
    private string mQuestId = string.Empty;
    private Actor mActor;

    private void Awake() {
        ButtonComplete.onClick.AddListener(SetComplete);
    }

    private void SetComplete() {
        bool ret = QuestHandler.Instance.Complete(mActor, mQuestId);     
        //Debug.Log(mQuestId);
    }

    public void SetQuestInfo(Actor actor, ConfigQuest_Detail info) {
        mQuestId = info.id;
        mActor = actor;

        Title.text = info.title;
        Desc.text = info.desc;
        // 보상 정보 info.rewards
        float complete = QuestHandler.Instance.GetCompleteRate(actor, info.id);
        Progress.value = complete;
        ProgressText.text = ((int)(complete * 100)).ToString() + _percent;

        if(complete < 1.0f) {
            ButtonComplete.gameObject.SetActive(false);
            Status.text = MessageWIP;
        } 
        else {
            ButtonComplete.gameObject.SetActive(true);
            Status.text = MessageDone;
        }
    }
    public void SetEmpty() {
        Title.text = string.Empty;
        Desc.text = string.Empty;
        Progress.value = 0;
        ProgressText.text = _emptyProgressText;
        Status.text = MessageEmpty;
    }
    
}
