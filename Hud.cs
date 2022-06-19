using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Hud : MonoBehaviour
{
    public GameObject TopLeft, TopRight, Left, Bottom;
    public Vector2 Margin;
    public Vector2 TopLeftSize = new Vector2(160, 40);
    public Vector2 TopRightSize = new Vector2(400, 40); 
    public Vector2 LeftSize = new Vector2(240, 240); 
    public Vector2 BottomSize = new Vector2(700, 80);
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
        */
        RectTransform bottomRT = Bottom.GetComponent<RectTransform>();
        bottomRT.sizeDelta = new Vector2((BottomSize.x / 1334.0f) * Screen.width, ((BottomSize.y / 750.0f) * Screen.height) + safe.y);
        bottomRT.anchoredPosition = new Vector2(0, safe.y);

        /*
        Left
        Left-Center
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
        
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
