using ENGINE.GAMEPLAY.MOTIVATION;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    public TextMeshProUGUI Txt, TxtPurchased, TxtSold;
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
    public Button STOCK_Btn_Receive;
    private Actor mActor;
    private StockMarketContext mStockMarketContext = new StockMarketContext();
    // Start is called before the first frame update
    void Start()
    {
        STOCK_Gold_Info.GetComponent<RectTransform>().sizeDelta = Scale.GetScaledSize(STOCK_Gold_Info_Size);
        STOCK_Market_Price.GetComponent<RectTransform>().sizeDelta = Scale.GetScaledSize(STOCK_Market_Price_Size);
        STOCK_Order.GetComponent<RectTransform>().sizeDelta = Scale.GetScaledSize(STOCK_Order_Size);
        OnClickCloseOrder();

        //OnClick
        STOCK_Btn_Order_Submit.onClick.AddListener(OnClickOrderSubmit);
        STOCK_Btn_Order_Cancel.onClick.AddListener(OnClickCloseOrder);
        STOCK_Btn_Receive.onClick.AddListener(OnClickOpenReceive);
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
        string sz = string.Empty;
        var list = StockMarketHandler.Instance.GetMarketPrices();
        foreach(var p in list) {
            sz += StockMarketHandler.Instance.Print(p.Key);
            sz += '\n';
        }
        Txt.text = sz;

        //sold
        sz = string.Empty;
        var sold = StockMarketHandler.Instance.GetActorSold(actorId);
        if(sold != null) {
            foreach(var p in sold) {
                sz += string.Format("{0}: {1}", p.resourceId, p.sellingPrice);
                sz += '\n';
            }
        } 
        TxtSold.text = sz;

        //buy
        sz = string.Empty;
        var buy = StockMarketHandler.Instance.GetActorPuchased(actorId);
        if(buy != null) {
            foreach(var p in buy) {
                sz += string.Format("{0}: {1}/{2}", p.resourceId, p.purchasedPrice, p.bid);
                sz += '\n';
            }
        }
        
        TxtPurchased.text = sz;
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
    void OnClickOpenReceive() {
        STOCK_Receive.SetActive(true);
    }
    void OnClickCloseReceive() {
        STOCK_Receive.SetActive(false);
    }
}
