using ENGINE.GAMEPLAY.MOTIVATION;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;

public class StockMarketContext {
    public bool isSell;
    public string resourceId = string.Empty;
    public int quantity;
    public float bid;
    public void Reset() {
        resourceId = string.Empty;
        quantity = -1;
        bid = -1;
    }
}
public class StockMarketController : MonoBehaviour
{
    public GamePlayController GamePlayController;
    public TextMeshProUGUI TxtSold;
    public GameObject STOCK_Scroll_Panel, STOCK_Gold_Info;
    public Vector2 STOCK_Gold_Info_Size = new Vector2(300, 100);
    public GameObject STOCK_Market_Price;
    public Vector2 STOCK_Market_Price_Size = new Vector2(600, 400);
    public GameObject STOCK_Order, STOCK_Order_Background;

    public Button STOCK_Btn_Order_Submit, STOCK_Btn_Order_Cancel;
    public Vector2 STOCK_Order_Size = new Vector2(300, 300);
    public TextMeshProUGUI TxtOrderName;
    public TMP_Dropdown DropOrderPrice, DropOrderQuantity;
    public TextMeshProUGUI TxtDeposit;
    public GameObject STOCK_Receive;
    public Button STOCK_Btn_Receive, STOCK_Btn_Receive_Detail, STOCK_Btn_Receive_Close;
    public Vector2 STOCK_Receive_Size = new Vector2(300, 300);
    public GameObject STOCK_Order_Status_BG, STOCK_Order_Status;
    public Vector2 STOCK_Order_Status_Size = new Vector2(400, 300);
    public Button STOCK_Btn_Order_Status, STOCK_Btn_Order_Status_Close;
    public GameObject STOCK_Order_Status_Scroll;
    public float STOCK_Order_Status_Element_Height = 100;

    private Actor mActor;
    public string CurrencyId = "Gold";
    private StockMarketContext mStockMarketContext = new StockMarketContext();
    private Dictionary<string, float> mTempSold = new Dictionary<string, float>();
    private StringBuilder mTempStringBuilder = new StringBuilder();
    private float mFee = 0.15f;
    private List<GameObject> mOrderObjectList = new List<GameObject>();
    private long mLastUpdate; 
    // Start is called before the first frame update
    void Start()
    {
        RectTransform goldInfo = STOCK_Gold_Info.GetComponent<RectTransform>();
        goldInfo.sizeDelta = Scale.GetScaledSize(STOCK_Gold_Info_Size);
        goldInfo.anchoredPosition = new Vector2(-Screen.safeArea.x, Screen.safeArea.y);
        
        STOCK_Market_Price.GetComponent<RectTransform>().sizeDelta = Scale.GetScaledSize(STOCK_Market_Price_Size);
        STOCK_Order.GetComponent<RectTransform>().sizeDelta = Scale.GetScaledSize(STOCK_Order_Size);
        STOCK_Receive.GetComponent<RectTransform>().sizeDelta = Scale.GetScaledSize(STOCK_Receive_Size);
        OnClickCloseOrder();

        STOCK_Order_Status.GetComponent<RectTransform>().sizeDelta = Scale.GetScaledSize(STOCK_Order_Status_Size);
        OnClickCloseOrderStatus();

        //OnClick
        STOCK_Btn_Order_Submit.onClick.AddListener(OnClickOrderSubmit);
        STOCK_Btn_Order_Cancel.onClick.AddListener(OnClickCloseOrder);
        STOCK_Btn_Receive.onClick.AddListener(OnClickReceive);
        STOCK_Btn_Receive_Detail.onClick.AddListener(OnClickOpenReceiveDetail);
        STOCK_Btn_Receive_Close.onClick.AddListener(OnClickCloseReceive);
        STOCK_Btn_Order_Status.onClick.AddListener(OnClickOpenOrderStatus);
        STOCK_Btn_Order_Status_Close.onClick.AddListener(OnClickCloseOrderStatus);
        OnClickCloseReceive();
    }

    // Update is called once per frame
    void Update()
    {
        if(mActor == null) {
            mActor = GamePlayController.GetFollowActor().mActor;
            //Add Scroll
            var marketPrices = StockMarketHandler.Instance.GetMarketPrices();
            int cnt = marketPrices.Count;
            STOCK_Scroll_Panel.GetComponent<RectTransform>().sizeDelta = new Vector2(0, cnt * Scale.GetScaledHeight(100));
            foreach(var p in marketPrices) {
                GameObject obj = Util.CreateChildObjectFromPrefabUI("StockElement", 
                                                                    STOCK_Scroll_Panel);
                obj.GetComponent<StockMarketPriceElement>().Set(this, p.Key, mActor);
            }
        }
            
        long last = StockMarketHandler.Instance.GetLastUpdate();
        if(last != mLastUpdate) {
            SetDeposit();
            mLastUpdate = last;
        }
    }
    void SetDeposit() {
        string actorId = mActor.mUniqueId;
        float depositSold = GetDeposit(GetSold(actorId), mFee);
        float depositPurchased = GetDeposit(GetPurchasedRemains(actorId), 0);
        TxtDeposit.text = string.Format("{0:N}", depositSold + depositPurchased);
    }
    float GetDeposit(Dictionary<string, float> list, float fee) {
        float sum = 0;
        foreach(var p in list) {
            sum += p.Value;
        }
        return sum * (1 - fee);
    }
    Dictionary<string, float> GetSold(string actorId) {
        var sold = StockMarketHandler.Instance.GetActorSold(actorId);
        mTempSold.Clear();
        if(sold != null) {
            foreach(var p in sold) {
                if(!mTempSold.ContainsKey(p.resourceId))
                    mTempSold[p.resourceId] = 0;
                mTempSold[p.resourceId] += p.sellingPrice;
            }
        } 
        return mTempSold;
    }
    //구매 후 잔액
    Dictionary<string, float> GetPurchasedRemains(string actorId) {
        var buy = StockMarketHandler.Instance.GetActorPuchased(actorId);
        mTempSold.Clear();
        if(buy != null) {
            foreach(var p in buy) {
                if(!mTempSold.ContainsKey(p.resourceId))
                    mTempSold[p.resourceId] = 0;
                mTempSold[p.resourceId] += p.bid - p.purchasedPrice;
            }
        }
        return mTempSold;
    }
    //구매 자원
    Dictionary<string, float> GetPurchasedResources(string actorId) {
        var buy = StockMarketHandler.Instance.GetActorPuchased(actorId);
        mTempSold.Clear();
        if(buy != null) {
            foreach(var p in buy) {
                if(!mTempSold.ContainsKey(p.resourceId))
                    mTempSold[p.resourceId] = 0;
                mTempSold[p.resourceId] += 1;
            }
        }
        return mTempSold;
    }

    string GetSoldListText(Dictionary<string, float> list) {
        float sum = 0;
        mTempStringBuilder.Clear();
        mTempStringBuilder.Append("<size=120%>[판매 수익]</size>\n\n");
        foreach(var p in list) {
            sum += p.Value;
            mTempStringBuilder.Append(SatisfactionDefine.Instance.GetTitle(p.Key));
            mTempStringBuilder.Append("\t");
            mTempStringBuilder.Append(p.Value.ToString("N"));
            mTempStringBuilder.Append("\n");
        }
        mTempStringBuilder.Append("수수료\t");
        mTempStringBuilder.Append((-sum * mFee).ToString("N"));
        mTempStringBuilder.Append("\n----------\n<size=120%>");
        mTempStringBuilder.Append((sum * (1 - mFee)).ToString("N"));
        mTempStringBuilder.Append("</size>\n\n");
        return mTempStringBuilder.ToString();
    }
    string GetPurchasedListText(Dictionary<string, float> list) {
        float sum = 0;
        mTempStringBuilder.Clear();
        mTempStringBuilder.Append("<size=120%>[구매 차액]</size>\n\n");
        foreach(var p in list) {
            sum += p.Value;
            mTempStringBuilder.Append(SatisfactionDefine.Instance.GetTitle(p.Key));
            mTempStringBuilder.Append("\t");
            mTempStringBuilder.Append(p.Value.ToString("N"));
            mTempStringBuilder.Append("\n");
        }
        mTempStringBuilder.Append("----------\n<size=120%>");
        mTempStringBuilder.Append(sum .ToString("N"));
        mTempStringBuilder.Append("</size>");
        return mTempStringBuilder.ToString();
    }
    void Sell() {
        //수량 체크는 order UI에서 하고 들어 와야 한다.
        if(mActor.GetSatisfaction(mStockMarketContext.resourceId).Value < mStockMarketContext.quantity)
            throw new System.Exception("Invalid Quanity");

        mActor.ApplySatisfaction(mStockMarketContext.resourceId, -mStockMarketContext.quantity, 0, null, true);
        StockActorOrder order = new StockActorOrder();
        order.Set(true, mActor, mStockMarketContext.resourceId, mStockMarketContext.quantity, mStockMarketContext.bid);
        StockMarketHandler.Instance.Order(order);
    }
    void Buy() {
        float amount = mStockMarketContext.quantity * mStockMarketContext.bid;
        //수량 체크는 order UI에서 하고 들어 와야 한다.
        if(mActor.GetSatisfaction(CurrencyId).Value < amount)
            throw new System.Exception("Invalid Quanity");

        mActor.ApplySatisfaction(CurrencyId, -(mStockMarketContext.quantity * mStockMarketContext.bid), 0, null, true);
        mActor.CallCallback(Actor.LOOP_STATE.STOCK_CALCULATE);

        StockActorOrder order = new StockActorOrder();
        order.Set(false, mActor, mStockMarketContext.resourceId, mStockMarketContext.quantity, mStockMarketContext.bid);
        StockMarketHandler.Instance.Order(order);
    }
    void OnClickOrderSubmit() {
        mStockMarketContext.quantity = int.Parse(DropOrderQuantity.options[DropOrderQuantity.value].text);
        mStockMarketContext.bid = int.Parse(DropOrderPrice.options[DropOrderPrice.value].text);
        if(mStockMarketContext.isSell) {
            Sell();
        } else {
            Buy();
        }
        OnClickCloseOrder();
    }
    void OnClickCloseOrder() {
        mStockMarketContext.Reset();
        STOCK_Order_Background.SetActive(false);
        StockMarketHandler.Instance.Resume();
    }
    public void OpenOrder(bool isSell, string resourceId, string name, float marketPrice) {
        StockMarketHandler.Instance.Pause();
        TxtOrderName.text = string.Format("{0} <size=70%><i>{1}</i></size><br>{2:F2}", name, isSell ? "매도" : "매수", marketPrice);
        STOCK_Order_Background.SetActive(true);
        mStockMarketContext.isSell = isSell;
        mStockMarketContext.resourceId = resourceId;
    }
    void OnClickReceive() {
        string actorId = mActor.mUniqueId;
        float depositSold = GetDeposit(GetSold(actorId), mFee);
        float depositPurchased = GetDeposit(GetPurchasedRemains(actorId), 0);
        
        float total = depositSold + depositPurchased;

        //리소스 수령
        var list = GetPurchasedResources(actorId);
        bool isCallback = false;
        if(list.Count > 0 || total > 0)
            isCallback = true;

        foreach(var resource in list) {
            mActor.ApplySatisfaction(resource.Key, resource.Value, 0, null, true);
        }

        if(total > 0) {
            mActor.ApplySatisfaction(CurrencyId, total, 0, null, true);
        }

        if(isCallback) {
            StockMarketHandler.Instance.RemoveActorOrder(mActor.mUniqueId);
            mActor.CallCallback(Actor.LOOP_STATE.STOCK_CALCULATE);
        }
        
    }
    void OnClickOpenReceiveDetail() {
        StockMarketHandler.Instance.Pause();
        //sold
        TxtSold.text = GetSoldListText(GetSold(mActor.mUniqueId));

        //buy
        TxtSold.text += GetPurchasedListText(GetPurchasedRemains(mActor.mUniqueId));
        STOCK_Receive.SetActive(true);
    }
    void OnClickCloseReceive() {
        STOCK_Receive.SetActive(false);
        StockMarketHandler.Instance.Resume();
    }
    void OnClickOpenOrderStatus() {
        StockMarketHandler.Instance.Pause();
        STOCK_Order_Status_BG.SetActive(true);

        var sells = StockMarketHandler.Instance.GetActorSellOrders(mActor.mUniqueId);
        var buys = StockMarketHandler.Instance.GetActorBuyOrders(mActor.mUniqueId);
        int cnt = 0;
        if(sells != null) {
            foreach(var p in sells) {
                for(int i = 0; i < p.Value.Count; i ++) {
                    GameObject obj = Util.CreateChildObjectFromPrefabUI("StockOrderElement", STOCK_Order_Status_Scroll);
                    obj.GetComponent<StockMarketOrderElement>().Set(p.Value[i], i, this);
                    mOrderObjectList.Add(obj);
                    cnt++;
                }
            }
        }
        
        if(buys != null) {
            for(int i = 0; i < buys.Count; i++) {
                GameObject obj = Util.CreateChildObjectFromPrefabUI("StockOrderElement", STOCK_Order_Status_Scroll);
                obj.GetComponent<StockMarketOrderElement>().Set(buys[i], i, this);
                mOrderObjectList.Add(obj);
                cnt++;
            }
        }
        
        STOCK_Order_Status_Scroll.GetComponent<RectTransform>().sizeDelta = new Vector2(0, cnt * Scale.GetScaledHeight(STOCK_Order_Status_Element_Height));
        
    }
    public void OnClickCloseOrderStatus() {
        STOCK_Order_Status_BG.SetActive(false);
        for(int i = 0; i < mOrderObjectList.Count; i++) {
            mOrderObjectList[i].transform.SetParent(null);
            Destroy(mOrderObjectList[i]);
        }

        mOrderObjectList.Clear();
        StockMarketHandler.Instance.Resume();
    }
}
