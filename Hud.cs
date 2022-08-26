using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ENGINE.GAMEPLAY;
using ENGINE.GAMEPLAY.MOTIVATION;
using TMPro;
using System.Text;
using System;
using UnityEngine.SceneManagement;

public class HUDStateContext {
    public List<string> queue = new List<string>();
    public StringBuilder sz = new StringBuilder();
    public float counter;
    public const float duration = 5;
    public const string newline = "\n";
    public const string mark = "<mark=#00000090 padding=\"10,2,2,10\">";
    public const string markEnd = "</mark>";
    public override string ToString() {
        sz.Clear();
        sz.Append(mark);
        for(int i = 0; i < queue.Count; i++) {
            if(i > 0)
                sz.Append(newline);
            sz.Append(queue[i]);
        }
        sz.Append(markEnd);
        return sz.ToString();
    }
}
public class Hud : MonoBehaviour
{
    public bool HideQuest = false;
    public bool HideSatisfaction = false;
    public bool HideTask = false;
    public bool HideMenu = false;
    public bool HideCurrency = false;
    public bool HideAlert = false;
    public Vector2 Margin = new Vector2(10,10);
    public Vector2 TopLeftSize = new Vector2(200, 50);
    public Vector2 TopCenterSize = new Vector2(300, 50);
    public Vector2 TopRightSize = new Vector2(200, 50); 
    public float LeftWidth = 240;     
    public float RightWidth = 200;
    public Vector2 BottomSize = new Vector2(500, 60);
    public Vector2 AlertSize = new Vector2(200, 80);
    public Vector2 AskSize = new Vector2(350, 300);
    public Vector2 TaskSize = new Vector2(500, 400);
    public Vector2 TimerSize = new Vector2(200, 100);
    public float ItemAcquisitionImageSize = 150;

    private TextMeshProUGUI NameText, LevelText, CurrencyText, StateText, VillageNameText, VillageLevelText, ItemAcquisitionText;    
    private Slider LevelProgress, VillageLevelProgress;
    public QuestElement[] QuestElements = new QuestElement[3];

    public string PrefabSatisfaction = "SatisfactionInfo";
    public string PrefabTask = "TaskPanel";

    private string mPrefixLevel = "Lv.";
    private List<GameObject> mSatisfactionList = new List<GameObject>();
    private List<Transform> mTaskObjectList = new List<Transform>();
    private Stack<GameObject> mTaskObjectPool = new Stack<GameObject>();

    private int mTaskAllocCount = 0;

    private ScrollRect ScrollViewSatisfaction, ScrollViewTask;
    private GameObject ContentSatisfaction, ContentTask, TaskPool;
    private GameObject TopLeft, TopCenter, TopRight, Left, Right, Bottom, Ask, Task, InventoryPanel, ItemAcquisitionPanel, ItemAcquisitionImage;
    private Animator ItemAcquisitionAnimator;
    private Button Btn_1, BtnOpenGallery, BtnOpenInventory, BtnAuto;
    private bool mIsAuto = false;
    private bool mItemAcquisitionEnable = false;
    private float mItemAcquisitionTimer = 0;
    public Color ColorBtnOn, ColorBtnOff;
    private GamePlayController mGamePlayController;
    private HUDStateContext mHUDStateContext = new HUDStateContext();
    private TextMeshProUGUI mTimer;

    public UI_Inventory Inven;
    private string[] InvenTapKeys = { "Item", "Resource", "Installation" };
    public string InventoryTitleFormat = "{0}<br>{1:#,###}";
    public string InventoryDescFormat = "{0}<br><br><size=70%>{1}</size>";

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
        ItemAcquisitionPanel = this.transform.Find("Panel_Item_Acquisition").gameObject;
       
        //Satisfaction
        ContentSatisfaction = GameObject.Find("HUD_Content_Satisfaction");
        ScrollViewSatisfaction = GameObject.Find("HUD_ScrollView_Satisfaction").GetComponent<ScrollRect>();
        //Task
        ContentTask = GameObject.Find("HUD_Content_Task");
        ScrollViewTask = GameObject.Find("HUD_ScrollView_Task").GetComponent<ScrollRect>();
        TaskPool = GameObject.Find("HUD_Task_Pool");
        TaskPool.SetActive(false);

        NameText            = GameObject.Find("HUD_Name").GetComponent<TextMeshProUGUI>();
        LevelText           = GameObject.Find("HUD_Level").GetComponent<TextMeshProUGUI>();
        StateText           = GameObject.Find("HUD_State").GetComponent<TextMeshProUGUI>();   
        LevelProgress       = GameObject.Find("HUD_LevelProgress").GetComponent<Slider>();   
        CurrencyText        = GameObject.Find("HUD_Currency").GetComponent<TextMeshProUGUI>();  

        VillageNameText     = GameObject.Find("HUD_VillageName").GetComponent<TextMeshProUGUI>();
        VillageLevelText    = GameObject.Find("HUD_VillageLevel").GetComponent<TextMeshProUGUI>();
        VillageLevelProgress= GameObject.Find("HUD_VillageLevelProgress").GetComponent<Slider>();  
        //auto
        BtnAuto             = GameObject.Find("HUD_Auto").GetComponent<Button>();
        //Bottom Buttons
        Btn_1               = GameObject.Find("HUD_BTN_1").GetComponent<Button>();
        //Gallery
        BtnOpenGallery      =   GameObject.Find("HUD_Gallery_Open").GetComponent<Button>();
        //Item Acquisition 
        ItemAcquisitionImage = GameObject.Find("HUD_Item_Acquisition_Image");
        ItemAcquisitionAnimator = ItemAcquisitionImage.GetComponent<Animator>();
        ItemAcquisitionText  = GameObject.Find("HUD_Item_Acquisition_Text").GetComponent<TextMeshProUGUI>();
        //Timer
        mTimer  = GameObject.Find("HUD_Timer").GetComponent<TextMeshProUGUI>();

        ItemAcquisitionPanel.SetActive(false);

        //Auto
        SetAutoBtnColor();
        BtnAuto.onClick.AddListener(SetAuto);
        //Buttons ------------------------------------------------------------------------
        Btn_1.onClick.AddListener(OnBtn1);
        //Gallery
        BtnOpenGallery.onClick.AddListener(OpenGallery);
        //Inventory
        InventoryPanel = transform.Find("Panel_Inventory").gameObject;
        BtnOpenInventory = GameObject.Find("HUD_Inventory_Open").GetComponent<Button>();
        BtnOpenInventory.onClick.AddListener(OpenInventory);
        
        Init();
    }
    private void Start() {
        Dictionary<string, string> tapInfo = new Dictionary<string, string>() {
            {   InvenTapKeys[0],    L10nHandler.Instance.Get(L10nCode.UI_INVEN_TAP_ITEM) },
            {   InvenTapKeys[1],    L10nHandler.Instance.Get(L10nCode.UI_INVEN_TAP_RESOURCE)},
            {   InvenTapKeys[2],    L10nHandler.Instance.Get(L10nCode.UI_INVEN_TAP_INSTALLATION)}
        };
        Inven.Init(tapInfo, InvenGetTitle, InvenGetDesc, InvenSubmit, InvenClose);
        Inven.OnClose();
        InventoryPanel.SetActive(false);
        //village
        var info = ActorHandler.Instance.GetVillageInfo(mGamePlayController.Village);
        SetVillageName(info.name);
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
        if(HideMenu) {
            Bottom.SetActive(false);
        }

        /*
        Left
        Left-Middle
        1334 : 240 = width : x
        = (240 / 1334) * w
        */
        RectTransform leftRT = Left.GetComponent<RectTransform>();
        leftRT.anchoredPosition = new Vector2(safe.x + Margin.x, safe.y);
        leftRT.sizeDelta = new Vector2(Scale.GetScaledWidth(LeftWidth), safe.height - (Margin.y * 2));
        if(HideQuest)
            Left.SetActive(false);

        /*
        Right
        Left-Middle
        */
        RectTransform RightRT = Right.GetComponent<RectTransform>();
        float ActualRightWidth = Scale.GetScaledWidth(RightWidth);
        RightRT.anchoredPosition = new Vector2(safe.x + safe.width - Margin.x - ActualRightWidth, safe.y);
        RightRT.sizeDelta = new Vector2(ActualRightWidth, safe.height - (Margin.y * 2) );
        if(HideSatisfaction)
            Right.SetActive(false);

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
        if(HideCurrency)
            TopRight.SetActive(false);

        //HUD Alert
        GameObject HUD_Alert = GameObject.Find("HUD_Alert");
        HUD_Alert.GetComponent<RectTransform>().sizeDelta = Scale.GetScaledSize(AlertSize);
        if(HideAlert)
            HUD_Alert.SetActive(false);

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

        //Item Acquisition
        Vector2 itemAcquisitionSize = Scale.GetScaledSize(new Vector2(ItemAcquisitionImageSize, ItemAcquisitionImageSize));
        ItemAcquisitionImage.GetComponent<RectTransform>().sizeDelta = itemAcquisitionSize;
        ItemAcquisitionText.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(itemAcquisitionSize.x * 1.2f, itemAcquisitionSize.y * 0.5f);
        ItemAcquisitionText.gameObject.transform.position += new Vector3(0, itemAcquisitionSize.y * -0.7f, 0); 

        //Timer
        RectTransform timerPanel  = this.transform.Find("Panel_Timer").gameObject.GetComponent<RectTransform>();;
        timerPanel.anchoredPosition = new Vector2(safe.x, safe.y);
        timerPanel.sizeDelta = Scale.GetScaledSize(TimerSize);
    }

    // Update is called once per frame
    private void Update() {
        if(mHUDStateContext.queue.Count > 0) {
            mHUDStateContext.counter += Time.deltaTime;
            if(mHUDStateContext.counter > HUDStateContext.duration) {
                mHUDStateContext.queue.RemoveAt(0);
                mHUDStateContext.counter = 0;
                StateText.text = mHUDStateContext.ToString();   
            }
        }

        //Item Acquisition
        if(mItemAcquisitionEnable) {
            mItemAcquisitionTimer += Time.deltaTime;
            if(mItemAcquisitionTimer > 1.5f) {
                mItemAcquisitionEnable = false;
                ItemAcquisitionPanel.SetActive(false);
            }
        }
        //timer
        mTimer.text = GetTimerString();
    }
    private string GetTimerString() {
        long count = CounterHandler.Instance.GetCount();
        DateTime dt = new DateTime(1000,01,01);
        dt = dt.AddMinutes(count);
        return dt.ToLongTimeString();
    }
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
        mHUDStateContext.queue.Add(sz);
        StateText.text = mHUDStateContext.ToString();        
    }
    public void SetLevelProgress(float v) {
        LevelProgress.value = v;
    }
    public void SetVillageLevelProgress(float v) {
        VillageLevelProgress.value = v;
    }
    // Satisfaction ---------------------------------------------------------------------------------------------
    public void SetSatisfaction(Dictionary<string, ENGINE.GAMEPLAY.MOTIVATION.Satisfaction> satisfaction) {
        var list = SatisfactionDefine.Instance.Get(SATISFACTION_TYPE.SATISFACTION);
        for(int i = 0; i < list.Count; i++) {
           mSatisfactionList[i].GetComponent<SatisfactionElement>().SetSatisfaction(satisfaction[list[i].satisfactionId]);
        }
        ScrollViewSatisfaction.verticalNormalizedPosition = 1;
        //currency
        var listCurrency = SatisfactionDefine.Instance.Get(SATISFACTION_TYPE.CURRENCY);
        CurrencyText.text = string.Empty;
        for(int i = 0; i < listCurrency.Count; i++) {
            CurrencyText.text = string.Format("{0:F} <sup>+</sup>", 
                                                satisfaction[listCurrency[i].satisfactionId].Value
                                            ); 
        }
    }
    public void InitSatisfaction(Dictionary<string, ENGINE.GAMEPLAY.MOTIVATION.Satisfaction> satisfaction) {
        float height = Screen.safeArea.height * 0.5f;     
        //4개씩 보이게끔
        height = height / satisfaction.Count;
        float width = Scale.GetScaledWidth(RightWidth);
        width -= 10; //scroll bar width
        RectTransform contentRect = ContentSatisfaction.GetComponent<RectTransform>();
        contentRect.sizeDelta = new Vector2(width, height * satisfaction.Count);

        GameObject prefab = Resources.Load<GameObject>(PrefabSatisfaction);
        foreach(var p in satisfaction) {
            if(SatisfactionDefine.Instance.Get(p.Key).type != SATISFACTION_TYPE.SATISFACTION) continue;

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
        float width = ScrollViewTask.gameObject.GetComponent<RectTransform>().sizeDelta.x;
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
    //Buttons ------------------------------------------------------------------------------------------
    void OnBtn1() {
        
    }
    void OpenGallery() {
        //VehicleController p = GameObject.Find("Taxi").GetComponent<VehicleController>();
        //p.GetOff();
    }
    // Inventory ----------------------------------------------------------------------------------------
    public void OpenInventory() {
        if(Inven.IsOpen())
            return;

        Inven.OnOpen();
        LoadInventory(InvenTapKeys[0]);
        InventoryPanel.SetActive(true);
    }
    void LoadInventory(string tap) {
        var actor = ActorHandler.Instance.GetActor(mGamePlayController.FollowActorId);
        if(actor == null)
            return;
        Inven.ResetData();
        Actor.ItemContext itemContext = actor.GetItemContext();
        foreach(var item in itemContext.inventory) {
            Inven.AddData(InvenTapKeys[0], item.Key, item.Value);
        }
        foreach(var s in actor.GetSatisfactions()) {
            ConfigSatisfaction_Define r = SatisfactionDefine.Instance.Get(s.Key);
            if(s.Value.Value > 0 && r.type == SATISFACTION_TYPE.RESOURCE) 
                Inven.AddData(InvenTapKeys[1], s.Key, s.Value.Value);
        }
        
        Inven.OnTap(tap);
    }
    string InvenGetTitle(string tap, string key, float amount) {
        if(tap == InvenTapKeys[0]) 
            return string.Format(InventoryTitleFormat, ItemHandler.Instance.GetItemInfo(key).name, amount);
        else if(tap == InvenTapKeys[1])
            return string.Format(InventoryTitleFormat, SatisfactionDefine.Instance.GetTitle(key), amount);
        else
            return key;
    }
    string InvenGetDesc(string tap, string key, float amount) {
        if(tap == InvenTapKeys[0]) {
            var detail = ItemHandler.Instance.GetItemInfo(key);
            string txt = string.Empty;
            switch(detail.category) {
                case ITEM_CATEGORY.SATISFACTION_ONLY:
                txt = L10nHandler.Instance.Get(L10nCode.UI_INVEN_USE);
                break;
                default:
                txt = L10nHandler.Instance.Get(L10nCode.UI_INVEN_ABANDON);
                break;
            }
            Inven.SetSubmitBtnText(txt);
            return string.Format(InventoryDescFormat, detail.name, detail.desc);
        } else if(tap == InvenTapKeys[1]) {
            Inven.SetSubmitBtnText(string.Empty);
            Inven.SetDisableSubmit();
            var detail = SatisfactionDefine.Instance.Get(key);
            return string.Format(InventoryDescFormat, detail.title, detail.desc);
        } else {
            return key;
        }
    }
    void InvenSubmit(string tap, string key, float amount) {
        if(tap == InvenTapKeys[0]) {
            var detail = ItemHandler.Instance.GetItemInfo(key);
            var actor = ActorHandler.Instance.GetActor(mGamePlayController.FollowActorId);
            switch(detail.category) {
                case ITEM_CATEGORY.SATISFACTION_ONLY:
                    actor.UseItemFromInventory(key);
                    break;
                    default:
                    actor.DecreaseQuantityInInventory(key, (int)amount);
                    break;
            }
            LoadInventory(tap);
        }
    }
    void InvenClose() {
        InventoryPanel.SetActive(false);
    }
    public void ObtainItem(Actor a) {
        var actor = ActorHandler.Instance.GetActor(mGamePlayController.FollowActorId);
        if(actor == null || actor.mUniqueId != a.mUniqueId) {
            return;
        }
        //화면 출력
        Actor.ItemContext ic = a.GetItemContext();
        string sz = string.Empty;
        for(int i = 0; i < ic.mObtainItemList.Count; i++) {
            if(i > 0)
                sz += "\n";
            sz += string.Format("{0} x{1}", ItemHandler.Instance.GetItemInfo(ic.mObtainItemList[i].itemId).name, ic.mObtainItemList[i].quantity);
        }
        OpenItemAcquisition(string.Empty, sz);
        ic.mObtainItemList.Clear();
    }
    // Item ----------------------------------------------------------------
    private void OpenItemAcquisition(string img, string text) {
        ItemAcquisitionPanel.SetActive(true);
        ItemAcquisitionText.text = text;
        mItemAcquisitionEnable = true;
        mItemAcquisitionTimer = 0;
    }
}
