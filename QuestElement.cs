using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ENGINE.GAMEPLAY.MOTIVATION;
public class QuestElement : MonoBehaviour
{
    public Text Title, Desc, ProgressText; 
    public Slider Progress;
    private const string _percent = "%";
    public void SetQuestInfo(Actor actor, ConfigQuest_Detail info) {
        Title.text = info.title;
        Desc.text = info.desc;
        // 보상 정보 info.rewards
        float complete = QuestHandler.Instance.GetCompleteRate(actor, info.id);
        Progress.value = complete;
        ProgressText.text = ((int)(complete * 100)).ToString() + _percent;
    }
    
}
