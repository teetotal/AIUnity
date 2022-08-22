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
    public TextMeshProUGUI Txt, TxtSold;
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

    private Actor mActor;
    public string CurrencyId = "Gold";
    private StockMarketContext mStockMarketContext = new StockMarketContext();
    private Dictionary<string, float> mTempSold = new Dictionary<string, float>();
    private StringBuilder mTempStringBuilder = new StringBuilder();
    private float mFee = 0.15f;
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

        //OnClick
        STOCK_Btn_Order_Submit.onClick.AddListener(OnClickOrderSubmit);
        STOCK_Btn_Order_Cancel.onClick.AddListener(OnClickCloseOrder);
        STOCK_Btn_Receive.onClick.AddListener(OnClickReceive);
        STOCK_Btn_Receive_Detail.onClick.AddListener(OnClickOpenReceiveDetail);
        STOCK_Btn_Receive_Close.onClick.AddListener(OnClickCloseReceive);
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
                GameObject obj = Util.CreateChildObjectFromPrefabUI("Stock_Element", 
                                                                    STOCK_Scroll_Panel);
                obj.GetComponent<StockMarketPriceElement>().Set(this, p.Key);
            }
        }
            
        
        string actorId = GamePlayController.GetFollowActor().mActor.mUniqueId;
        SetDeposit();

        string sz = string.Empty;
        var list = StockMarketHandler.Instance.GetMarketPrices();
        foreach(var p in list) {
            sz += StockMarketHandler.Instance.Print(p.Key);
            sz += '\n';
        }
        Txt.text = sz;

        //sold
        TxtSold.text = GetSoldListText(GetSold(actorId));

        //buy
        TxtSold.text += GetPurchasedListText(GetPurchased(actorId));
    }
    void SetDeposit() {
        string actorId = mActor.mUniqueId;
        float depositSold = GetDeposit(GetSold(actorId), mFee);
        float depositPurchased = GetDeposit(GetPurchased(actorId), 0);
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
    Dictionary<string, float> GetPurchased(string actorId) {
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
        Actor actor = GamePlayController.GetFollowActor().mActor;
        StockActorOrder order = new StockActorOrder();
        order.Set(true, actor, mStockMarketContext.resourceId, mStockMarketContext.quantity, mStockMarketContext.bid);
        StockMarketHandler.Instance.Order(order);
    }
    void Buy() {
        Actor actor = GamePlayController.GetFollowActor().mActor;
        StockActorOrder order = new StockActorOrder();
        order.Set(false, actor, mStockMarketContext.resourceId, mStockMarketContext.quantity, mStockMarketContext.bid);
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
    }
    public void OpenOrder(bool isSell, string resourceId, string name, float marketPrice) {
        TxtOrderName.text = string.Format("{0} <size=70%><i>{1}</i></size><br>{2:F2}", name, isSell ? "매도" : "매수", marketPrice);
        STOCK_Order_Background.SetActive(true);
        mStockMarketContext.isSell = isSell;
        mStockMarketContext.resourceId = resourceId;
    }
    void OnClickReceive() {
        string actorId = mActor.mUniqueId;
        float depositSold = GetDeposit(GetSold(actorId), mFee);
        var purchase = GetPurchased(actorId);
        float depositPurchased = GetDeposit(purchase, 0);
        float total = depositSold + depositPurchased;

        //리소스 수령 기능 만들어야 함.

        if(total > 0) {
            mActor.ApplySatisfaction(CurrencyId, total, 0, null, true);
            StockMarketHandler.Instance.RemoveActorOrder(mActor.mUniqueId);
            mActor.CallCallback(Actor.LOOP_STATE.STOCK_CALCULATE);
        }
        
    }
    void OnClickOpenReceiveDetail() {
        STOCK_Receive.SetActive(true);
    }
    void OnClickCloseReceive() {
        STOCK_Receive.SetActive(false);
    }
}
