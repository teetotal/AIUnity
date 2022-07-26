using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ENGINE.GAMEPLAY.MOTIVATION;
using TMPro;
public class QuestElement : MonoBehaviour
{
    public TextMeshProUGUI BtnText, ProgressText;
    public Slider Progress;
    public Button Btn;
    private const string _percent = "%";
    private const string _emptyProgressText = "-";
    private string mQuestId = string.Empty;
    private StringBuilder mSzBuilder = new StringBuilder();
    private string[] strArr = {"<size=100%>", "</size><br><size=70%>", "</size>"};
    private Actor mActor;

    private void Awake() {
        Btn.onClick.AddListener(SetComplete);
    }

    private void SetComplete() {
        Debug.Log(mQuestId);
        bool ret = QuestHandler.Instance.Complete(mActor, mQuestId);     
    }

    public void SetQuestInfo(Actor actor, ConfigQuest_Detail info) {
        mQuestId = info.id;
        mActor = actor;
        
        mSzBuilder.Clear();
        mSzBuilder.Append(strArr[0]);
        mSzBuilder.Append(info.title);
        mSzBuilder.Append(strArr[1]);
        mSzBuilder.Append(info.desc);
        mSzBuilder.Append(strArr[2]);

        BtnText.text = mSzBuilder.ToString();
        // 보상 정보 info.rewards
        float complete = QuestHandler.Instance.GetCompleteRate(actor, info.id);
        Progress.value = complete;
        ProgressText.text = ((int)(complete * 100)).ToString() + _percent;

        if(complete < 1.0f) {
            Btn.interactable = false;
        } 
        else {
            Btn.interactable = true;
        }
    }
    public void SetEmpty() {
        BtnText.text = string.Empty;
        Progress.value = 0;
        ProgressText.text = _emptyProgressText;
    }
    
}
