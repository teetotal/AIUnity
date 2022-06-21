using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Hud : MonoBehaviour
{
    public GameObject TopLeft, TopRight, Left, Bottom, Ask;
    public Vector2 Margin = new Vector2(5,10);
    public Vector2 TopLeftSize = new Vector2(160, 40);
    public Vector2 TopRightSize = new Vector2(400, 40); 
    public Vector2 LeftSize = new Vector2(200, 300); 
    public Vector2 BottomSize = new Vector2(700, 60);
    public Vector2 AskSize = new Vector2(400, 300);

    public Text TopLeftText, TopRightText;    
    public Slider LevelProgress;

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
    }

    // Update is called once per frame
    public void SetTopLeftText(string sz) {
        TopLeftText.text = sz;
    }

    public void SetTopRightText(string sz) {
        TopRightText.text = sz;
    }
    public void SetLevelProgress(float v) {
        LevelProgress.value = v;
    }

}
