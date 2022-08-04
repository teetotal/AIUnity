using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ENGINE.GAMEPLAY.MOTIVATION;
using TMPro;
public class Hud : MonoBehaviour
{
    public Vector2 Margin = new Vector2(10,10);
    public Vector2 TopLeftSize = new Vector2(200, 100);
    public Vector2 TopCenterSize = new Vector2(240, 50);
    public Vector2 TopRightSize = new Vector2(200, 40); 
    public float LeftWidth = 240;     
    public float RightWidth = 200;
    public Vector2 BottomSize = new Vector2(500, 60);
    public Vector2 AskSize = new Vector2(350, 300);
    public Vector2 TaskSize = new Vector2(500, 400);
    public Vector2 InventorySize = new Vector2(500, 400);

    private TextMeshProUGUI NameText,LevelText, LevelProgressText, StateText;    
    private TextMeshProUGUI VillageNameText, VillageLevelText;
    private Slider LevelProgress, VillageLevelProgress;
    public QuestElement[] QuestElements = new QuestElement[3];

    public string PrefabSatisfaction = "SatisfactionInfo";
    public string PrefabTask = "TaskPanel";

    private string mPrefixLevel = "Lv.";
    private List<GameObject> mSatisfactionList = new List<GameObject>();
    private List<Transform> mTaskObjectList = new List<Transform>();
    private Stack<GameObject> mTaskObjectPool = new Stack<GameObject>();
    private List<Transform> mInventoryObjectList = new List<Transform>();
    private Stack<GameObject> mInventoryObjectPool = new Stack<GameObject>();

    private int mTaskAllocCount = 0;

    private ScrollRect ScrollViewSatisfaction, ScrollViewTask;
    private GameObject ContentSatisfaction, ContentTask, TaskPool, InventoryPool;
    private GameObject TopLeft, TopCenter, TopRight, Left, Right, Bottom, Ask, Task, Inventory;
    public Button BtnCloseInventory, BtnOpenInventory, BtnAuto;
    private bool mIsAuto = false;
    public Color ColorBtnOn, ColorBtnOff;
    private GamePlayController mGamePlayController;

    private void Awake() {
        mGamePlayController = this.gameObject.GetComponent<GamePlayController>();

        TopLeft     = this.transform.Find("Panel_Top_Left").gameObject;
        TopCenter   = this.transform.Find("Panel_Top_Center").gameObject;
        TopRight    = this.transform.Find("Panel_Top_Right").gameObject;
        Left        = this.transform.Find("Panel_Left").gameObject;
        Right       = this.transform.Find("Panel_Right").gameObject;
        Bottom      = this.transform.Find("Panel_Bottom").gameObject;
        Ask         = this.transform.Find("Panel_Ask").gameObject;
        Task        = this.transform.Find("Panel_Task").gameObject;
       
        //Satisfaction
        ContentSatisfaction = GameObject.Find("HUD_Content_Satisfaction");
        ScrollViewSatisfaction = GameObject.Find("HUD_ScrollView_Satisfaction").GetComponent<ScrollRect>();
        //Task
        ContentTask = GameObject.Find("HUD_Content_Task");
        ScrollViewTask = GameObject.Find("HUD_ScrollView_Task").GetComponent<ScrollRect>();
        TaskPool = GameObject.Find("HUD_Task_Pool");
        TaskPool.SetActive(false);

        NameText            = GameObject.Find("HUD_Name").GetComponent<TextMeshProUGUI>();
        LevelText           = GameObject.Find("HUD_Level").GetComponent<TextMeshProUGUI>();;
        LevelProgressText   = GameObject.Find("HUD_LevelProgressText").GetComponent<TextMeshProUGUI>();
        StateText           = GameObject.Find("HUD_State").GetComponent<TextMeshProUGUI>();   
        LevelProgress       = GameObject.Find("HUD_LevelProgress").GetComponent<Slider>();   

        VillageNameText     = GameObject.Find("HUD_VillageName").GetComponent<TextMeshProUGUI>();
        VillageLevelText    = GameObject.Find("HUD_VillageLevel").GetComponent<TextMeshProUGUI>();
        VillageLevelProgress= GameObject.Find("HUD_VillageLevelProgress").GetComponent<Slider>();  
        //Inventory
        Inventory           = GameObject.Find("HUD_Inventory");
        BtnCloseInventory   = GameObject.Find("HUD_Inventory_Close").GetComponent<Button>();
        BtnOpenInventory    = GameObject.Find("HUD_Inventory_Open").GetComponent<Button>();
        InventoryPool       = GameObject.Find("HUD_Inventory_Pool");
        InventoryPool.SetActive(false);

        //Auto
        SetAutoBtnColor();
        BtnAuto.onClick.AddListener(SetAuto);
        BtnCloseInventory.onClick.AddListener(CloseInventory);
        BtnOpenInventory.onClick.AddListener(OpenInventory);
        
        Init();
    }
    public bool IsAuto() {
        return mIsAuto;
    }

    void SetAuto(){
        //Ask.SetActive(!Ask.activeSelf);
        //Task.SetActive(!Task.activeSelf);
        mIsAuto = !mIsAuto;
        SetAutoBtnColor();
        
    }
    private void SetAutoBtnColor() {
        if(mIsAuto) {
            BtnAuto.GetComponent<Image>().color = ColorBtnOn;
        } else {
            BtnAuto.GetComponent<Image>().color = ColorBtnOff;
        }
    }
    // Start is called before the first frame update
    void Init()
    {  
        //변경 이벤트 잡으려면 deviceOrientation을 계속 확인 하는 방법밖에 없다.
        Rect safe = Screen.safeArea;

        //Debug.Log(string.Format("{0}, {1} {2}", Screen.width, Screen.height, safe));
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
        float x = Scale.GetScaledWidth(BottomSize.x);
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
        leftRT.sizeDelta = new Vector2(Scale.GetScaledWidth(LeftWidth), safe.height - (Margin.y * 2));

        /*
        Right
        Left-Middle
        */
        RectTransform RightRT = Right.GetComponent<RectTransform>();
        float ActualRightWidth = Scale.GetScaledWidth(RightWidth);
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
        topLeftRT.sizeDelta = Scale.GetScaledSize(TopLeftSize);
        /*
        Top Center
        Center-Bottom
        */
        RectTransform topCenterRT = TopCenter.GetComponent<RectTransform>();
        topCenterRT.anchoredPosition = new Vector2(0, safe.y + safe.height - Margin.y);
        topCenterRT.sizeDelta = Scale.GetScaledSize(TopCenterSize);

        /*
        Top Right
        Left-Bottom
        = 40 / 750 * h
        = 400 / 1334 * w
        */
        RectTransform topRightRT = TopRight.GetComponent<RectTransform>();
        topRightRT.sizeDelta = Scale.GetScaledSize(TopRightSize);
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
        x = Scale.GetScaledWidth(AskSize.x);
        askRT.sizeDelta = new Vector2(x, ((AskSize.y / AskSize.x) * x));
        Ask.SetActive(false);

        //Task
        RectTransform taskRT = Task.GetComponent<RectTransform>();
        x = Scale.GetScaledWidth(TaskSize.x);
        taskRT.sizeDelta = new Vector2(x, ((TaskSize.y / TaskSize.x) * x));
        Task.SetActive(false);

        //Inventory
        RectTransform inventoryRT = Inventory.GetComponent<RectTransform>();
        x = Scale.GetScaledWidth(InventorySize.x);
        inventoryRT.sizeDelta = new Vector2(x, ((InventorySize.y / InventorySize.x) * x));
        Inventory.GetComponent<GridLayoutGroup>().cellSize = new Vector2(inventoryRT.sizeDelta.x / 5, inventoryRT.sizeDelta.y / 5);
        //close button
        BtnCloseInventory.transform.position = new Vector3(inventoryRT.position.x + (inventoryRT.sizeDelta.x / 2), inventoryRT.position.y + (inventoryRT.sizeDelta.y / 2),0);
        CloseInventory();
    }

    // Update is called once per frame
    public void SetName(string name) {
        NameText.text = name;
    }
    public void SetLevel(string level)  {
        LevelText.text = level;
    }

    public void SetVillageName(string name) {
        VillageNameText.text = name;
    }
    public void SetVillageLevel(int level)  {
        VillageLevelText.text = mPrefixLevel + level.ToString();
    }

    public void SetState(string sz) {
        StateText.text = sz;        
    }
    public void SetLevelProgress(float v) {
        LevelProgress.value = v;
        LevelProgressText.text = string.Format("{0}%", (int)(v * 100));
    }
    public void SetVillageLevelProgress(float v) {
        VillageLevelProgress.value = v;
    }
    // Satisfaction ---------------------------------------------------------------------------------------------
    public void SetSatisfaction(Dictionary<string, ENGINE.GAMEPLAY.MOTIVATION.Satisfaction> satisfaction) {
        int i = 0;
        /*
        foreach(var p in satisfaction.OrderBy( i => (i.Value.Value / i.Value.Max))) {
            //p.value 적용            
            mSatisfactionList[i].GetComponent<SatisfactionElement>().SetSatisfaction(p.Value);
            i++;
        }   
        */
        foreach(var p in satisfaction) {
            mSatisfactionList[i].GetComponent<SatisfactionElement>().SetSatisfaction(p.Value);
            i++;
        }
        ScrollViewSatisfaction.verticalNormalizedPosition = 1;
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

        GameObject prefab = Resources.Load<GameObject>(PrefabSatisfaction);
        foreach(var p in satisfaction) {
            GameObject obj = Instantiate(prefab);            

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(contentRect.sizeDelta.x, height);
            obj.transform.SetParent(ContentSatisfaction.transform);

            //저장
            mSatisfactionList.Add(obj);
        }

        SetSatisfaction(satisfaction);
    }
    // Task ---------------------------------------------------------------------------------------------
    public void SetTask(Dictionary<string, FnTask> tasks) {
        float width = Scale.GetScaledHeight(493);
        float height = Scale.GetScaledHeight(100);
       
        RectTransform contentRect = ContentTask.GetComponent<RectTransform>();
        contentRect.sizeDelta = new Vector2(width, height * tasks.Count);

        foreach(var p in tasks) {
            GameObject obj = AllocTask(contentRect.sizeDelta.x, height, p.Value);
        }
        Task.SetActive(true);
    }
    private GameObject AllocTask(float width, float height, FnTask fn) {
        GameObject obj;
        if(mTaskObjectPool.Count > 0) {
            obj= mTaskObjectPool.Pop();
        } else {
            GameObject prefab = Resources.Load<GameObject>(PrefabTask);
            obj = Instantiate(prefab);     
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);
            obj.GetComponent<TaskElement>().Init(this, mGamePlayController);
            mTaskAllocCount++;
        }
        TaskElement te = obj.GetComponent<TaskElement>();
        te.Set(fn);

        mTaskObjectList.Add(obj.transform);
        obj.transform.SetParent(ContentTask.transform);
        return obj;
    }
    public void ReleaseTask() {
        for(int i = 0; i < mTaskObjectList.Count; i++) {
            mTaskObjectList[i].gameObject.GetComponent<TaskElement>().Release();
            mTaskObjectPool.Push(mTaskObjectList[i].gameObject);
            mTaskObjectList[i].SetParent(TaskPool.transform);
        }
        mTaskObjectList.Clear();
        Task.SetActive(false);
    }
    // Quest ---------------------------------------------------------------------------------------------
    public void SetQuest(Actor actor, List<string> quests) {
        for(int i = 0; i < QuestElements.Length; i++) {
            if(i > quests.Count - 1) {
                QuestElements[i].SetEmpty();
                continue;
            }
            
            string questId = quests[i];
            var info = QuestHandler.Instance.GetQuestInfo(actor.mType, questId);
            if(info == null)
                throw new System.Exception("Invalid Quest Id: " + questId + ", Actor Type: " + actor.mType.ToString());
            
            QuestElements[i].SetQuestInfo(actor, info);
        }
        return;
    }
    // Inventory ----------------------------------------------------------------------------------------
    void OpenInventory() {
        if(Inventory.activeSelf)
            return;
        
        //Add Inventory.
        var actor = mGamePlayController.GetFollowActor();
        if(actor == null)
            return;
        Actor.ItemContext itemContext = actor.mActor.GetItemContext();
        
        foreach(var item in itemContext.inventory) {
            AllocInventory(item.Key, item.Value);
        }
        Inventory.SetActive(true);
        BtnCloseInventory.gameObject.SetActive(true);
    }
    void CloseInventory() {
        ReleaseInventory();
        Inventory.SetActive(false);
        BtnCloseInventory.gameObject.SetActive(false);
    }
    private GameObject AllocInventory(string id, int quantity) {
        GameObject obj;
        if(mInventoryObjectPool.Count > 0) {
            obj= mInventoryObjectPool.Pop();
        } else {
            GameObject prefab = Resources.Load<GameObject>("Button");
            obj = Instantiate(prefab);     
        }
        ConfigItem_Detail item = ItemHandler.Instance.GetItemInfo(id);
        obj.GetComponent<Button>().GetComponentInChildren<TextMeshProUGUI>().text = string.Format("{0}\n{1}", item.name, quantity);

        mInventoryObjectList.Add(obj.transform);
        obj.transform.SetParent(Inventory.transform);
        return obj;
    }
    public void ReleaseInventory() {
        for(int i = 0; i < mInventoryObjectList.Count; i++) {
            mInventoryObjectPool.Push(mInventoryObjectList[i].gameObject);
            mInventoryObjectList[i].SetParent(InventoryPool.transform);
        }
        mTaskObjectList.Clear();
    }
}
