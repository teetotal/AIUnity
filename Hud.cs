using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ENGINE.GAMEPLAY.MOTIVATION;
public class Hud : MonoBehaviour
{
    //public ScrollRect ScrollRectQuest;    
    public GameObject ScrollViewSatisfaction;
    public GameObject ContentSatisfaction;
    public GameObject TopLeft, TopCenter, TopRight, Left, Right, Bottom, Ask;
    public Vector2 Margin = new Vector2(5,5);
    public Vector2 TopLeftSize = new Vector2(200, 60);
    public Vector2 TopCenterSize = new Vector2(240, 40);
    public Vector2 TopRightSize = new Vector2(200, 40); 
    public float LeftWidth = 240;     
    public float RightWidth = 200;
    public Vector2 BottomSize = new Vector2(500, 60);
    public Vector2 AskSize = new Vector2(400, 300);

    public Text NameText,LevelText, LevelTextProgress, TopCenterText;    
    public Slider LevelProgress;
    public QuestElement[] QuestElements = new QuestElement[3];

    public string PrefabSatisfaction = "SatisfactionInfo";

    private string mPrefixLevel = "Lv.";
    private Dictionary<string, GameObject> mDicSatisfaction = new Dictionary<string, GameObject>();

    void Start() {
        Init();
    }
    // Start is called before the first frame update
    void Init()
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
        float x = (BottomSize.x / 1334.0f) * safe.width;
        bottomRT.sizeDelta = new Vector2(x, ((BottomSize.y / BottomSize.x) * x));
        bottomRT.anchoredPosition = new Vector2(0, safe.y);

        /*
        Left
        Left-Middle
        1334 : 240 = width : x
        = (240 / 1334) * w
        */
        RectTransform leftRT = Left.GetComponent<RectTransform>();
        leftRT.anchoredPosition = new Vector2(safe.x + Margin.x, safe.y + Margin.y);
        leftRT.sizeDelta = new Vector2((LeftWidth / 1334.0f) * safe.width, safe.height - (Margin.y * 2));

        /*
        Right
        Left-Middle
        */
        RectTransform RightRT = Right.GetComponent<RectTransform>();
        float ActualRightWidth = RightWidth * safe.width / 1334.0f;
        RightRT.anchoredPosition = new Vector2(safe.x + safe.width - Margin.x - ActualRightWidth, safe.y + Margin.y);
        RightRT.sizeDelta = new Vector2(ActualRightWidth, safe.height - (Margin.y * 2));

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
        topLeftRT.sizeDelta = new Vector2((TopLeftSize.x / 1334.0f) * safe.width, (TopLeftSize.y / 750.0f) * safe.height);
        /*
        Top Center
        Center-Bottom
        */
        RectTransform topCenterRT = TopCenter.GetComponent<RectTransform>();
        topCenterRT.anchoredPosition = new Vector2(0, safe.y + safe.height - Margin.y);
        topCenterRT.sizeDelta = new Vector2((TopCenterSize.x / 1334.0f) * safe.width, (TopCenterSize.y / 750.0f) * safe.height);

        /*
        Top Right
        Left-Bottom
        = 40 / 750 * h
        = 400 / 1334 * w
        */
        RectTransform topRightRT = TopRight.GetComponent<RectTransform>();
        topRightRT.sizeDelta = new Vector2((TopRightSize.x / 1334.0f) * safe.width, (TopRightSize.y / 750.0f) * safe.height);
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
        x = (AskSize.x / 1334.0f) * safe.width;
        askRT.sizeDelta = new Vector2(x, ((AskSize.y / AskSize.x) * x));
        Ask.SetActive(false);
    }

    // Update is called once per frame
    public void SetName(string name) {
        NameText.text = name;
    }
    public void SetLevel(int level)  {
        LevelText.text = mPrefixLevel + level.ToString();
    }

    public void SetTopCenterText(string sz) {
        TopCenterText.text = sz;        
    }
    public void SetLevelProgress(float v) {
        LevelProgress.value = v;
        LevelTextProgress.text = string.Format("{0}%", (int)(v * 100));
    }
    // ---------------------------------------------------------------------------------------------
    public void SetSatisfaction(Dictionary<string, ENGINE.GAMEPLAY.MOTIVATION.Satisfaction> satisfaction) {
        foreach(var p in satisfaction) {
            //p.value 적용
            mDicSatisfaction[p.Key].GetComponent<SatisfactionElement>().SetSatisfaction(p.Value);
        }        
    }
    public void InitSatisfaction(Dictionary<string, ENGINE.GAMEPLAY.MOTIVATION.Satisfaction> satisfaction) {
        float height = Screen.safeArea.height / 6;     
        int n = satisfaction.Count;
        //4개씩 보이게끔
        height = height / 4;
        float width = (RightWidth / 1334.0f) * Screen.safeArea.width;
        width -= 10; //scroll bar width
        RectTransform contentRect = ContentSatisfaction.GetComponent<RectTransform>();
        contentRect.sizeDelta = new Vector2(width, height * n);

        Debug.Log(height);
        Debug.Log(contentRect.sizeDelta);

        GameObject prefab = Resources.Load<GameObject>(PrefabSatisfaction);
        foreach(var p in satisfaction) {
            GameObject obj = Instantiate(prefab);            

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(contentRect.sizeDelta.x, height);
            obj.transform.SetParent(ContentSatisfaction.transform);

            //저장
            mDicSatisfaction.Add(p.Key, obj);
        }

        SetSatisfaction(satisfaction);
    }
    public void SetQuest(Actor actor, List<string> quests) {
        for(int i = 0; i < quests.Count; i++) {
            string questId = quests[i];
            var info = QuestHandler.Instance.GetQuestInfo(actor.mType, questId);
            if(info == null)
                throw new System.Exception("Invalid Quest Id: " + questId + ", Actor Type: " + actor.mType.ToString());
            
            QuestElements[i].SetQuestInfo(actor, info);
        }
        return;
    }

}
