using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ENGINE.GAMEPLAY.MOTIVATION;
using TMPro;

public class UI_Inventory : MonoBehaviour
{
    [SerializeField]
    private bool HideTap = false;
    [SerializeField]
    private bool HideClose = false;
    [SerializeField]
    private Vector2 Size = new Vector2(700, 500);
    [SerializeField]
    private Vector2 RatioTap = new Vector2(0.2f, 0.3f);
    [SerializeField]
    private Vector2 RatioMain = new Vector2(0.6f, 0.4f);
    [SerializeField]
    private float RatioClose = 0.08f;
    [SerializeField]
    private float SizeItemHeight = 100;

    private Button TemplateTapBtn, TemplateItemBtn, TemplateCloseBtn;
    private TextMeshProUGUI TxtDesc;
    private Transform Pool;
    private Button Submit;
    
    private RectTransform Parent;
    private RectTransform Tap;
    private RectTransform Main;
    private RectTransform Scroll, Desc, Content;
    private RectTransform Close;

    //data
    private Dictionary<string, List<Config_KV_SF>> mData = new Dictionary<string, List<Config_KV_SF>>();
    private Dictionary<string, string> mTapInfo;

    //objects
    private Dictionary<string, Button> mTapButtons = new Dictionary<string, Button>();
    private Stack<GameObject> mPool = new Stack<GameObject>();
    private List<GameObject> mAlloc = new List<GameObject>(); 

    //context
    private string mCurrentItemId = string.Empty;

    //delegate
    public delegate string FnGetTitle(string tap, string key, float amount);
    private FnGetTitle mFnGetTitle;
    public delegate string FnGetDesc(string tap, string key, float amount);
    private FnGetDesc mFnGetDesc;
    public delegate void FnSubmit(string itemId); 
    private FnSubmit mFnSubmit;
    
    // public --------------------------
    public void Init(Dictionary<string, string> tapInfo, FnGetTitle fnTitle, FnGetDesc fnDesc, FnSubmit fnSubmit) {
        mFnGetTitle = fnTitle;
        mFnGetDesc = fnDesc;
        mFnSubmit = fnSubmit;
        ResetData(tapInfo);
    }
    public void ResetData(Dictionary<string, string> tapInfo) {
        mTapInfo = tapInfo;
        if(mData.Count > 0)
            mData.Clear();
        SetTap();
    }
    public void AddData(string tap, string key, float amount) {
        if(!mData.ContainsKey(tap))
            mData[tap] = new List<Config_KV_SF>();
        
        Config_KV_SF p = new Config_KV_SF();
        p.key = key;
        p.value = amount;
        mData[tap].Add(p);
    }
    public void SetSubmitBtnText(string sz) {
        Submit.GetComponentInChildren<TextMeshProUGUI>().text = sz;
    }
    // private -------------------------
    void Awake() {
        Parent  =   GetComponent<RectTransform>();

        Tap     =   transform.Find("UI_INVEN_Panel_Tap").GetComponent<RectTransform>();
        Main    =   transform.Find("UI_INVEN_Panel_Main").GetComponent<RectTransform>();
        Close   =   transform.Find("UI_INVEN_Panel_Close").GetComponent<RectTransform>();

        Scroll  =   Main.transform.Find("UI_INVEN_Panel_Scroll").GetComponent<RectTransform>();
        Content =   GameObject.Find("UI_INVEN_Content").GetComponent<RectTransform>();
        Desc    =   Main.transform.Find("UI_INVEN_Panel_Desc").GetComponent<RectTransform>();

        TxtDesc =   GameObject.Find("UI_INVEN_Description").GetComponent<TextMeshProUGUI>();
        Submit  =   GameObject.Find("UI_INVEN_Submit").GetComponent<Button>();

        Submit.onClick.AddListener(OnSubmit);

        //template
        Transform template  =  transform.Find("Template").gameObject.transform;
        TemplateTapBtn      =  template.Find("TapBtn").GetComponent<Button>();
        TemplateCloseBtn    =  template.Find("CloseBtn").GetComponent<Button>();
        TemplateItemBtn     =  template.Find("ItemBtn").GetComponent<Button>();

        //Pool
        Pool    = transform.Find("UI_INVEN_Pool").transform;
    }
    void Start() {
        Parent.sizeDelta = Scale.GetScaledSize(Size);
        Main.sizeDelta = Parent.sizeDelta;
        //Close
        if(HideClose)
            Close.gameObject.SetActive(false);
        else {
            float widthClose = Parent.sizeDelta.x * RatioClose;
            Close.sizeDelta = new Vector2(widthClose, widthClose);
            Close.anchoredPosition = new Vector2(widthClose, 0);

            SetCloseButton();
        }
        //Scroll
        Scroll.sizeDelta = new Vector2(Parent.sizeDelta.x * RatioMain.x, Parent.sizeDelta.y);
        //Desc
        Desc.sizeDelta  =   new Vector2(Parent.sizeDelta.x * RatioMain.y, Parent.sizeDelta.y);
        Desc.anchoredPosition = new Vector2(Scroll.sizeDelta.x, 0);
        //Tap
        if(HideTap)
            Tap.gameObject.SetActive(false);
        else
            Tap.sizeDelta = new Vector2(Parent.sizeDelta.x * RatioTap.x, Parent.sizeDelta.y * RatioTap.y);

        //example ---------
        /*
        Dictionary<string, List<Config_KV_SF>> p =new Dictionary<string, List<Config_KV_SF>>();
        p.Add("Tap1", new List<Config_KV_SF>());
        p.Add("Tap2", new List<Config_KV_SF>());
        p.Add("Tap3", new List<Config_KV_SF>());

        for(int i = 0; i < 10; i++) {
            Config_KV_SF item = new Config_KV_SF();
            item.key = "item" + i.ToString();
            item.value = 1;
            p["Tap1"].Add(item);
        }

        Dictionary<string, string> pTap = new Dictionary<string, string>();
        pTap.Add("Tap1", "메뉴1");
        pTap.Add("Tap2", "메뉴2");
        pTap.Add("Tap3", "메뉴3");
        SetData(p, pTap);
        OnTap("Tap1");

        SetSubmitBtnText("판매");
        */
    }

    void SetData(Dictionary<string, List<Config_KV_SF>> data, Dictionary<string, string> tapInfo) {
        mData = data;
        mTapInfo = tapInfo;
        SetTap();
    }

    // close --------------------------
    void SetCloseButton() {
        GameObject closeBtn = Util.CreateChildObjectFromUIObject(TemplateCloseBtn.gameObject, Close.gameObject);
        
        RectTransform rect = closeBtn.GetComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Close.sizeDelta;

        Button btn = closeBtn.GetComponent<Button>();
        btn.onClick.AddListener(OnClose);

    }
    public void OnClose() {
        Parent.gameObject.SetActive(false);
    }
    public void OnOpen() {
        Parent.gameObject.SetActive(true);
    }
    // Tap --------------------------
    void SetTap() {
        mTapButtons.Clear();
        for(int i = 0; i < Tap.transform.childCount; i++) {
            Transform tr = Tap.transform.GetChild(i);
            tr.SetParent(null);
            Destroy(tr.gameObject);
        }
        foreach(var p in mTapInfo) {
            GameObject tapBtn = Util.CreateChildObjectFromUIObject(TemplateTapBtn.gameObject, Tap.gameObject);
            tapBtn.GetComponentInChildren<TextMeshProUGUI>().text = p.Value;
            Button btn = tapBtn.GetComponent<Button>();
            mTapButtons.Add(p.Key, btn);
            btn.onClick.AddListener(() => OnTap(p.Key));
        }
    }
    public void OnTap(string key) {
        mCurrentItemId = string.Empty;
        TxtDesc.text = string.Empty;
        Submit.interactable = false;
        
        //release
        for(int i = 0; i < mAlloc.Count; i++) {
            mAlloc[i].transform.SetParent(Pool);
            mAlloc[i].GetComponent<Button>().onClick.RemoveAllListeners();
            mPool.Push(mAlloc[i]);
        }
        mAlloc.Clear();

        //set interactable
        foreach(var btn in mTapButtons) {
            bool b = btn.Key != key;
            btn.Value.interactable = b;
        }
        if(!mData.ContainsKey(key))
            return;
        //grid size
        List<Config_KV_SF> list = mData[key];
        
        GridLayoutGroup grid = Content.GetComponent<GridLayoutGroup>();
        int cols = grid.constraintCount;
        float itemHeight = Scale.GetScaledHeight(SizeItemHeight);
        grid.cellSize = new Vector2((Scroll.sizeDelta.x - 10)/ cols, itemHeight);

        int row = (list.Count / cols);
        if(list.Count % cols > 0)
            row++;
        Content.sizeDelta = new Vector2(0, itemHeight * row);

        for(int i = 0; i < list.Count; i++) {
            GameObject itemBtn;
            if(mPool.Count > 0) {
                itemBtn = mPool.Pop();
                itemBtn.transform.SetParent(Content);
            } else {
                itemBtn = Util.CreateChildObjectFromUIObject(TemplateItemBtn.gameObject, Content.gameObject);
            }
            itemBtn.GetComponentInChildren<TextMeshProUGUI>().text = mFnGetTitle(key, list[i].key, list[i].value);
            
            //onclick
            Config_KV_SF item = list[i];
            itemBtn.GetComponent<Button>().onClick.AddListener(() => OnItem(key, item.key, item.value));
            mAlloc.Add(itemBtn);
        }
    }
    // OnItem -----------------
    void OnItem(string tap, string itemId, float amount) {
        Submit.interactable = true;
        mCurrentItemId = itemId;
        TxtDesc.text = mFnGetDesc(tap, itemId, amount);
    }
    public void SetDisableSubmit() {
        Submit.interactable = false;
    }
    // OnSubmit ----------------
    void OnSubmit() {
        //Debug.Log(mCurrentItemId);
        if(mFnSubmit != null)
            mFnSubmit(mCurrentItemId);
    }
}
