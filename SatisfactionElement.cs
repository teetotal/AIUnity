using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ENGINE.GAMEPLAY.MOTIVATION;
using TMPro;

public class SatisfactionElement : MonoBehaviour
{
    public TextMeshProUGUI SatisfactionName, SatisfactionProgressText;
    public Slider SatisfactionProgress;
    private Color colorRed = new Color32(176, 61, 61, 255);
    private Color colorYellow = new Color32(228, 202, 68, 255);
    private Color colorGreen = new Color32(66, 134, 68, 255);
    private Color colorNormal = new Color32(255, 255, 255, 255);
    // Start is called before the first frame update
    void Start()
    {
    }

    public void SetSatisfaction(Satisfaction satisfaction) {
        string title = SatisfactionDefine.Instance.Get(satisfaction.SatisfactionId).title;
        SatisfactionName.text = title; 
        SatisfactionProgressText.text = string.Format("{1} / {2}", title, satisfaction.Value, satisfaction.Max);
        SatisfactionProgress.value = satisfaction.Value / satisfaction.Max;

        if(satisfaction.Value <= satisfaction.Min) SatisfactionName.color = colorYellow;
        else if(satisfaction.Value >= satisfaction.Max) SatisfactionName.color = colorGreen;
        else SatisfactionName.color = colorNormal;
    }
}
