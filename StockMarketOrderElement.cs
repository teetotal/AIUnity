using System.Text;
using ENGINE.GAMEPLAY.MOTIVATION;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class StockMarketOrderElement : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI Txt;
    [SerializeField]
    private Button Btn;
    private int mIdx;
    private StockActorOrder mOrder;
    private StockMarketController mStockMarketController;
    void Start()
    {
        Btn.onClick.AddListener(OnClick);
    }
    public void Set(StockActorOrder order, int idx, StockMarketController controller) {
        mIdx = idx;
        mOrder = order;
        mStockMarketController = controller;

        StringBuilder szBuilder = new StringBuilder();
        szBuilder.Append(order.isSell ? "[판매] " : "[구매] ");
        szBuilder.Append(SatisfactionDefine.Instance.GetTitle(order.resourceId));
        szBuilder.Append("\n");
        szBuilder.Append(string.Format("{0} ({1}/{2})", order.bid, order.quantity, order.orderQuantity));
        
        Txt.text = szBuilder.ToString();
    }
    void OnClick() {
        StockMarketHandler.Instance.CancelActorOrder(mOrder, mIdx);
        mStockMarketController.OnClickCloseOrderStatus();
        mStockMarketController.OnClickOpenOrderStatus();
    }
}
