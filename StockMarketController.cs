using ENGINE.GAMEPLAY.MOTIVATION;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StockMarketController : MonoBehaviour
{
    public GamePlayController GamePlayController;
    public TextMeshProUGUI Txt, TxtPurchased, TxtSold;
    public Button Sell;
    public Button Buy;
    // Start is called before the first frame update
    void Start()
    {
        Sell.onClick.AddListener(OnClickSell);
        Buy.onClick.AddListener(OnClickBuy);
    }

    // Update is called once per frame
    void Update()
    {
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
    void OnClickSell() {
        Actor actor = GamePlayController.GetFollowActor().mActor;
        StockActorOrder order = new StockActorOrder();
        order.Set(true, actor, "Resource2", 2, 7);
        StockMarketHandler.Instance.Order(order);
    }
    void OnClickBuy() {
        Actor actor = GamePlayController.GetFollowActor().mActor;
        StockActorOrder order = new StockActorOrder();
        order.Set(false, actor, "Resource2", 2, 8);
        StockMarketHandler.Instance.Order(order);
    }
}
