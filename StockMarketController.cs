using ENGINE;
using ENGINE.GAMEPLAY.MOTIVATION;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;
using UnityEngine.SceneManagement;

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
public class StockRemain {
    public float remain;
    public int quantity;
}
public class StockRemainPool : Singleton<StockRemainPool> {
    private ObjectPool<StockRemain> mPool = new ObjectPool<StockRemain>();
    public StockRemainPool() { }
    public ObjectPool<StockRemain> GetPool() {
        return mPool;
    }
}

public class StockMarketController : MonoBehaviour
{
    public GamePlayController GamePlayController;
    public TextMeshProUGUI TxtReceipt;
    public GameObject STOCK_Scroll_Panel, STOCK_Gold_Info;
    public Vector2 STOCK_Gold_Info_Size = new Vector2(300, 100);
    public GameObject STOCK_Market_Price;
    public Vector2 STOCK_Market_Price_Size = new Vector2(600, 400);
    public float STOCK_Market_Price_Element_Height = 100;
    public Button STOCK_Market_Close;
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
    private StockMarketContext mStockMarketContext = new StockMarketContext();
    private Dictionary<string, float> mTempSold = new Dictionary<string, float>();
    private Dictionary<string, StockRemain> mTempBought = new Dictionary<string, StockRemain>();
    private StringBuilder mTempStringBuilder = new StringBuilder();
    private List<GameObject> mOrderObjectList = new List<GameObject>();
    private long mLastUpdate; 
    // Start is called before the first frame update
    void Start()
    {
        RectTransform goldInfo = STOCK_Gold_Info.GetComponent<RectTransform>();
        goldInfo.sizeDelta = Scale.GetScaledSize(STOCK_Gold_Info_Size);
        goldInfo.anchoredPosition = new Vector2(-Screen.safeArea.x, Screen.safeArea.y);
        
        STOCK_Market_Price.GetComponent<RectTransform>().sizeDelta = Scale.GetScaledSize(STOCK_Market_Price_Size);
        STOCK_Market_Price.SetActive(false);

        STOCK_Order.GetComponent<RectTransform>().sizeDelta = Scale.GetScaledSize(STOCK_Order_Size);
        STOCK_Receive.GetComponent<RectTransform>().sizeDelta = Scale.GetScaledSize(STOCK_Receive_Size);
        OnClickCloseOrder();

        STOCK_Order_Status.GetComponent<RectTransform>().sizeDelta = Scale.GetScaledSize(STOCK_Order_Status_Size);
        OnClickCloseOrderStatus();

        //OnClick
        STOCK_Market_Close.onClick.AddListener(OnClose);
        STOCK_Btn_Order_Submit.onClick.AddListener(OnClickOrderSubmit);
        STOCK_Btn_Order_Cancel.onClick.AddListener(OnClickCloseOrder);
        STOCK_Btn_Receive.onClick.AddListener(OnClickReceive);
        STOCK_Btn_Receive_Detail.onClick.AddListener(OnClickOpenReceiveDetail);
        STOCK_Btn_Receive_Close.onClick.AddListener(OnClickCloseReceive);
        STOCK_Btn_Order_Status.onClick.AddListener(OnClickOpenOrderStatus);
        STOCK_Btn_Order_Status_Close.onClick.AddListener(OnClickCloseOrderStatus);
        OnClickCloseReceive();

        //Dropdown
        DropOrderPrice.onValueChanged.AddListener(OnOrderPriceChange);
    }

    // Update is called once per frame
    void Update()
    {
        if(mActor == null) {
            mActor = GamePlayController.FollowActor;
            //Add Scroll
            var marketPrices = StockMarketHandler.Instance.GetMarketPrices();
            int cnt = marketPrices.Count;
            STOCK_Scroll_Panel.GetComponent<RectTransform>().sizeDelta = new Vector2(0, cnt * Scale.GetScaledHeight(STOCK_Market_Price_Element_Height));
            foreach(var p in marketPrices) {
                GameObject obj = Util.CreateChildObjectFromPrefabUI("StockElement", STOCK_Scroll_Panel);
                obj.GetComponent<StockMarketPriceElement>().Set(this, p.Key, mActor);
            }

            GamePlayController.GetFollowActor().transform.position = new Vector3(3, 0.15f, -4);
        }

        long last = StockMarketHandler.Instance.GetLastUpdate();
        if(last != mLastUpdate) {
            if(!STOCK_Market_Price.activeSelf)
                STOCK_Market_Price.SetActive(true);

            SetDeposit();
            mLastUpdate = last;
            /*
            Debug.Log(string.Format("buy: {0} sell {1}", StockMarketHandler.Instance.cntBuy, StockMarketHandler.Instance.cntSell));
            Debug.Log(string.Format("alloc: {0} release {1}, new: {2}, pop: {3}", 
                            StockSellOrderPool.Instance.GetPool().GetCntAlloc(), 
                            StockSellOrderPool.Instance.GetPool().GetCntRelease(),
                            StockSellOrderPool.Instance.GetPool().GetCntNew(),
                            StockSellOrderPool.Instance.GetPool().GetCntPop()
                            ));
            */
        }
    }
    void SetDeposit() {
        string actorId = mActor.mUniqueId;
        float depositSold = GetDeposit(GetSold(actorId), StockMarketHandler.Instance.FEE);
        StockRemain depositPurchased = GetDeposit(GetPurchasedRemains(actorId));
        TxtDeposit.text = string.Format("{0:N} ({1})", depositSold + depositPurchased.remain, depositPurchased.quantity);
    }
    float GetDeposit(Dictionary<string, float> list, float fee) {
        float sum = 0;
        foreach(var p in list) {
            sum += p.Value;
        }
        return sum * (1 - fee);
    }
    StockRemain GetDeposit(Dictionary<string, StockRemain> list) {
        StockRemain remain = new StockRemain();
        foreach(var p in list) {
            remain.remain += p.Value.remain;
            remain.quantity += p.Value.quantity;
        }
        return remain;
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
    Dictionary<string, StockRemain> GetPurchasedRemains(string actorId) {
        var buy = StockMarketHandler.Instance.GetActorPuchased(actorId);
        if(mTempBought.Count > 0) {
            foreach(var p in mTempBought) {
                StockRemainPool.Instance.GetPool().Release(p.Value);
            }
            mTempBought.Clear();
        }
        

        if(buy != null) {
            foreach(var p in buy) {
                if(!mTempBought.ContainsKey(p.resourceId)) {
                    //pooling
                    StockRemain remain = StockRemainPool.Instance.GetPool().Alloc();
                    remain.quantity = 1;
                    remain.remain = p.bid - p.purchasedPrice;
                    mTempBought[p.resourceId] = remain;
                } else {
                    mTempBought[p.resourceId].remain += p.bid - p.purchasedPrice;
                    mTempBought[p.resourceId].quantity++;
                }
            }
        }
        return mTempBought;
    }
    /*
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
    */

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
        mTempStringBuilder.Append((-sum * StockMarketHandler.Instance.FEE).ToString("N"));
        mTempStringBuilder.Append("\n----------\n<size=120%>");
        mTempStringBuilder.Append((sum * (1 - StockMarketHandler.Instance.FEE)).ToString("N"));
        mTempStringBuilder.Append("</size>\n\n");
        return mTempStringBuilder.ToString();
    }
    string GetPurchasedListText(Dictionary<string, StockRemain> list) {
        float sum = 0;
        mTempStringBuilder.Clear();
        mTempStringBuilder.Append("<size=120%>[구매 차액]</size>\n\n");
        foreach(var p in list) {
            sum += p.Value.remain;
            mTempStringBuilder.Append(SatisfactionDefine.Instance.GetTitle(p.Key));
            mTempStringBuilder.Append(string.Format(" ({0})", p.Value.quantity));
            mTempStringBuilder.Append("\t");
            mTempStringBuilder.Append(p.Value.remain.ToString("N"));
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
        StockMarketHandler.Instance.ActorOrder(order);
    }
    void Buy() {
        float amount = mStockMarketContext.quantity * mStockMarketContext.bid;
        //수량 체크는 order UI에서 하고 들어 와야 한다.
        if(mActor.GetSatisfaction(StockMarketHandler.Instance.CURRENCY).Value < amount)
            throw new System.Exception("Invalid Quanity");

        mActor.ApplySatisfaction(StockMarketHandler.Instance.CURRENCY, -(mStockMarketContext.quantity * mStockMarketContext.bid), 0, null, true);
        mActor.CallCallback(Actor.LOOP_STATE.STOCK_CALCULATE);

        StockActorOrder order = new StockActorOrder();
        order.Set(false, mActor, mStockMarketContext.resourceId, mStockMarketContext.quantity, mStockMarketContext.bid);
        StockMarketHandler.Instance.ActorOrder(order);
    }
    void OnClickOrderSubmit() {
        mStockMarketContext.quantity = int.Parse(DropOrderQuantity.options[DropOrderQuantity.value].text);
        mStockMarketContext.bid = float.Parse(DropOrderPrice.options[DropOrderPrice.value].text);

        //Debug.Log(mStockMarketContext.quantity);
        //Debug.Log(mStockMarketContext.bid);

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
        TxtOrderName.text = string.Format("{0} <size=70%><i>{1}</i></size><br>{2:F2}", name, isSell ? "판매" : "구매", marketPrice);
        STOCK_Order_Background.SetActive(true);
        mStockMarketContext.isSell = isSell;
        mStockMarketContext.resourceId = resourceId;

        //가격 
        //-20% -10% 0% +10% +20%
        DropOrderPrice.options.Clear();
        DropOrderPrice.AddOptions(new List<string>() {
            (marketPrice * 0.9f).ToString("F"),
            (marketPrice * 0.95f).ToString("F"),
            marketPrice.ToString("F"),
            (marketPrice * 1.05f).ToString("F"),
            (marketPrice * 1.1f).ToString("F")
        });
        DropOrderPrice.value = 2;
        SetOrderQuantity(marketPrice);
    }
    void OnOrderPriceChange(int index) {
        //quantity 조정
        float price = float.Parse(DropOrderPrice.options[index].text);
        SetOrderQuantity(price);
    }
    void SetOrderQuantity(float price) {
        List<string> quantity = new List<string>();
        if(mStockMarketContext.isSell) {
            //재고
            int myQuantity = (int)mActor.GetSatisfaction(mStockMarketContext.resourceId).Value;
            for(int i = 0; i < 10; i++ ) {
                if(myQuantity >= i + 1) {
                    quantity.Add((i+1).ToString());
                } else {
                    break;
                }
            }
        } else {
            //money
            float money = mActor.GetSatisfaction(StockMarketHandler.Instance.CURRENCY).Value;
            
            for(int i = 0; i < 10; i++ ) {
                if(((i+1) * price) <= money) {
                    quantity.Add((i+1).ToString());
                } else {
                    break;
                }
            }
        }

        DropOrderQuantity.options.Clear();
        DropOrderQuantity.AddOptions(quantity);
        DropOrderQuantity.value = 0;
    }
    void OnClickReceive() {
        string actorId = mActor.mUniqueId;
        float depositSold = GetDeposit(GetSold(actorId), StockMarketHandler.Instance.FEE);
        Dictionary<string, StockRemain> buyList = GetPurchasedRemains(actorId);
        
        float total = depositSold;

        //리소스 수령
        bool isCallback = false;

        foreach(var resource in buyList) {
            mActor.ApplySatisfaction(resource.Key, resource.Value.quantity, 0, null, true);
            total += resource.Value.remain;
            isCallback = true;
        }

        if(total > 0) {
            mActor.ApplySatisfaction(StockMarketHandler.Instance.CURRENCY, total, 0, null, true);
            isCallback = true;
        }

        if(isCallback) {
            StockMarketHandler.Instance.RemoveActorOrder(mActor.mUniqueId);
            mActor.CallCallback(Actor.LOOP_STATE.STOCK_CALCULATE);
        }
        
    }
    void OnClickOpenReceiveDetail() {
        StockMarketHandler.Instance.Pause();
        //sold
        TxtReceipt.text = GetSoldListText(GetSold(mActor.mUniqueId));

        //buy
        TxtReceipt.text += GetPurchasedListText(GetPurchasedRemains(mActor.mUniqueId));
        STOCK_Receive.SetActive(true);
    }
    void OnClickCloseReceive() {
        STOCK_Receive.SetActive(false);
        StockMarketHandler.Instance.Resume();
    }
    public void OnClickOpenOrderStatus() {
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
    void OnClose() {
        StartCoroutine(LoadAsyncScene("Demo"));
    }
    System.Collections.IEnumerator LoadAsyncScene(string scene)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
}
