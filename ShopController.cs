using ENGINE.GAMEPLAY.MOTIVATION;
using System.Collections.Generic;
using UnityEngine;

public class ShopController : MonoBehaviour
{
    [SerializeField]
    private UI_Inventory ShopUI;
    [SerializeField]
    private Hud hud;
    [SerializeField]
    private GamePlayController gamePlayController;
    // Start is called before the first frame update
    void Start()
    {
        ShopUI.Init(new Dictionary<string, string>() {
            { ITEM_CATEGORY.SATISFACTION_ONLY.ToString(), "일반템"},
            { ITEM_CATEGORY.WEAPON.ToString(), "무기"},
            { ITEM_CATEGORY.VEHICLE.ToString(), "탈것"},
            { ITEM_CATEGORY.FARMING.ToString(), "농사"},
            { ITEM_CATEGORY.COOKING.ToString(), "요리"},
        }, 
        InvenGetTitle,
        InvenGetDesc,
        InvenSubmit
        );

        var items = ItemHandler.Instance.GetAll();
        foreach(var item in items) {
            ShopUI.AddData( item.Value.category.ToString(), item.Key, item.Value.cost);
        }
        ShopUI.OnTap(ITEM_CATEGORY.SATISFACTION_ONLY.ToString());
    }

    private string InvenGetTitle(string tap, string key, float amount) {
        return string.Format("{0}<br>{1:F}", ItemHandler.Instance.GetItemInfo(key).name, amount);
    }
    private string InvenGetDesc(string tap, string key, float amount) {
        ShopUI.SetSubmitBtnText(L10nHandler.Instance.Get(L10nCode.UI_INVEN_BUY));
        ConfigItem_Detail item = ItemHandler.Instance.GetItemInfo(key);
        return string.Format("{0}<br><size=80%>{1}</size>", 
                            item.name,
                            item.desc
                            );
    }
    private void InvenSubmit(string tap, string key, float amount) {
        hud.SetSatisfaction(gamePlayController.FollowActor.GetSatisfactions());
        hud.OpenInventory();
    }

}
