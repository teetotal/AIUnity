using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ENGINE.GAMEPLAY.MOTIVATION;

public class SatisfactionElement : MonoBehaviour
{
    public Text SatisfactionText; //start가 호출되기 전에 SetSatisfaction가 호출되서 어쩔 수 없이 public으로 했음
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
        SatisfactionText.text = string.Format("{0} {1} / {2}", title, satisfaction.Value, satisfaction.Max);

        if(satisfaction.Value <= satisfaction.Min) SatisfactionText.color = colorYellow;
        else if(satisfaction.Value >= satisfaction.Max) SatisfactionText.color = colorGreen;
        else SatisfactionText.color = colorNormal;
    }
}
