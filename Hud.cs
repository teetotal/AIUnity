using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Hud : MonoBehaviour
{
    public ScrollRect ScrollRectQuest;
    public GameObject ContentQuest;
    public GameObject TopLeft, TopRight, Left, Bottom, Ask;
    public Vector2 Margin = new Vector2(5,10);
    public Vector2 TopLeftSize = new Vector2(160, 40);
    public Vector2 TopRightSize = new Vector2(400, 40); 
    public Vector2 LeftSize = new Vector2(200, 300); 
    public Vector2 BottomSize = new Vector2(700, 60);
    public Vector2 AskSize = new Vector2(400, 300);

    public Text NameText,LevelText, LevelTextProgress, TopRightText;    
    public Slider LevelProgress;

    private string mPrefixLevel = "Lv.";

    void SetQuest(RectTransform leftRT) {
        int n = 4;

        RectTransform contentRect = ContentQuest.GetComponent<RectTransform>();
        contentRect.sizeDelta = new Vector2(leftRT.sizeDelta.x, (leftRT.sizeDelta.y / 3) * 4);
        GameObject prefab = Resources.Load<GameObject>("QuestPanel");

        
        for(int i = 0; i < n; i ++) {
            GameObject obj = Instantiate(prefab);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(contentRect.sizeDelta.x, leftRT.sizeDelta.y / 3);

            obj.transform.SetParent(ContentQuest.transform);
        }
        ScrollRectQuest.normalizedPosition = new Vector2(0, 1);
    }
    // Start is called before the first frame update
    void Start()
    {  
        //변경 이벤트 잡으려면 deviceOrientation을 계속 확인 하는 방법밖에 없다.
        Rect safe = Screen.safeArea;

        Debug.Log(string.Format("{0}, {1} {2}", Screen.width, Screen.height, safe));
        /*
        Bottom
        Center-Bottom
        750 : 80 = height : y
        = (80 / 750) * height
        = (700.0f / 1334.0f) * w
        가로 길이를 구한다.
        1366 : BottomSize.x = Screen.width : x
        x = (BottomSize.x / 1334) * Screen.width 
        세로 길이를 구한다.
        BottomSize.x : BottomSize.y = x : y
        y = (BottomSize.y / BottomSize.x) * x
        */
        RectTransform bottomRT = Bottom.GetComponent<RectTransform>();
        float x = (BottomSize.x / 1334.0f) * Screen.width;
        bottomRT.sizeDelta = new Vector2(x, ((BottomSize.y / BottomSize.x) * x));
        bottomRT.anchoredPosition = new Vector2(0, safe.y);

        /*
        Left
        Left-Middle
        750 : 240 = height : y
        = (240 / 750) * h
        1334 : 240 = width : x
        = (240 / 1334) * w
        */
        RectTransform leftRT = Left.GetComponent<RectTransform>();
        leftRT.anchoredPosition = new Vector2(safe.x + Margin.x, 0);
        leftRT.sizeDelta = new Vector2((LeftSize.x / 1334.0f) * Screen.width, (LeftSize.y / 750.0f) * Screen.height);

        /*
        Top Left
        Left-Bottom
        750 : 40 = h : y
        = 40 / 750 * h
        1334 : 160 = w : x
        = 160 / 1334 * w 
        */
        RectTransform topLeftRT = TopLeft.GetComponent<RectTransform>();
        topLeftRT.anchoredPosition = new Vector2(safe.x + Margin.x, safe.y + safe.height - Margin.y);
        topLeftRT.sizeDelta = new Vector2((TopLeftSize.x / 1334.0f) * Screen.width, (TopLeftSize.y / 750.0f) * Screen.height);

        /*
        Top Right
        Left-Bottom
        = 40 / 750 * h
        = 400 / 1334 * w
        */
        RectTransform topRightRT = TopRight.GetComponent<RectTransform>();
        topRightRT.sizeDelta = new Vector2((TopRightSize.x / 1334.0f) * Screen.width, (TopRightSize.y / 750.0f) * Screen.height);
        topRightRT.anchoredPosition = new Vector2(safe.x + safe.width - Margin.x - topRightRT.sizeDelta.x, safe.y + safe.height - Margin.y);
        
        /*
        Ask
        Center-Middle
        가로 길이를 구한다.
        1334 : AskSize.x = Screen.width : x
        x = (AskSize.x / 1334) * Screen.width 
        세로 길이를 구한다.
        AskSize.x : AskSize.y = x : y
        y = (AskSize.y / AskSize.x) * x
        */
        RectTransform askRT = Ask.GetComponent<RectTransform>();
        x = (AskSize.x / 1334.0f) * Screen.width;
        askRT.sizeDelta = new Vector2(x, ((AskSize.y / AskSize.x) * x));
        Ask.SetActive(false);

        SetQuest(leftRT);
    }

    // Update is called once per frame
    public void SetName(string name) {
        NameText.text = name;
    }
    public void SetLevel(int level)  {
        LevelText.text = mPrefixLevel + level.ToString();
    }

    public void SetTopRightText(string sz) {
        TopRightText.text = sz;
    }
    public void SetLevelProgress(float v) {
        LevelProgress.value = v;
        LevelTextProgress.text = string.Format("{0}%", (int)(v * 100));
    }

}
