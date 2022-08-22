using System.Collections.Generic;
using ENGINE.GAMEPLAY.MOTIVATION;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StockMarketPriceElement : MonoBehaviour
{
    public TextMeshProUGUI TxtName, TxtPriceInfo;
    public Button BtnBuy, BtnSell;
    public string ColorPositive = "#B03D3D";
    public string ColorNegative = "#0181D5";
    public string ColorNormal = "white";
    private StockMarketController mStockMarketController;
    private string mResourceId = string.Empty;
    private string mResourceName = string.Empty;
    private float marketPrice = 0;
    private Actor mActor;
    private long mLastUpdate;
    // Start is called before the first frame update
    void Start()
    {
        BtnBuy.onClick.AddListener(OnClickBuy);
        BtnSell.onClick.AddListener(OnClickSell);
    }

    // Update is called once per frame
    void Update() {
        if(mActor != null) {
            long last = StockMarketHandler.Instance.GetLastUpdate();
            if(last == mLastUpdate) 
                return;
            
            mLastUpdate = last;

            float myMoney = mActor.GetSatisfaction(StockMarketHandler.Instance.CURRENCY).Value;
            var quantity = mActor.GetSatisfaction(mResourceId);
            TxtName.text = string.Format("{0}<br><size=70%>{1}</size>", mResourceName, quantity == null ? string.Empty : quantity.Value);

            var list = StockMarketHandler.Instance.GetMarketPrices(mResourceId);
            marketPrice = list[StockMarketHandler.Instance.CAPACITY-1];
            TxtPriceInfo.text = string.Format("{0:F2}<br><size=80%><color={1}>{2:F2} | {3:F2}%</color></size>", 
                                                marketPrice,
                                                GetColor(list),
                                                marketPrice - list[StockMarketHandler.Instance.CAPACITY-2],
                                                -(100 - (marketPrice / list[StockMarketHandler.Instance.CAPACITY-2] * 100)) 
                                            );
            
            BtnBuy.gameObject.SetActive(myMoney >= marketPrice);
            BtnSell.gameObject.SetActive(quantity != null && quantity.Value > 0);
        }
    }
    string GetColor(List<float> list) {
        if(list[StockMarketHandler.Instance.CAPACITY-1] > list[StockMarketHandler.Instance.CAPACITY-2])
            return ColorPositive;
        else if(list[StockMarketHandler.Instance.CAPACITY-1] < list[StockMarketHandler.Instance.CAPACITY-2])
            return ColorNegative;
        else 
            return ColorNormal;
    }
    public void Set(StockMarketController p, string resourceId, Actor actor)
    {
        mStockMarketController = p;
        mResourceId = resourceId;
        mResourceName = SatisfactionDefine.Instance.GetTitle(mResourceId);
        mActor = actor;
    }
    void OnClickBuy() {
        mStockMarketController.OpenOrder(false, mResourceId, mResourceName, marketPrice);

    }
    void OnClickSell() {
        mStockMarketController.OpenOrder(true, mResourceId, mResourceName, marketPrice);
    }

}
