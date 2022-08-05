using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ENGINE.GAMEPLAY.MOTIVATION;
using TMPro;

public class ItemElement : MonoBehaviour
{
    public Button mButton;
    public TextMeshProUGUI mText;
    private Actor mActor;
    private string mId = string.Empty;
    private int mAmount;
    private StringBuilder mStringBuilder = new StringBuilder();
    private const string newline = "\n";
    private bool mIsItem;
    private Hud mHUD;

    // Start is called before the first frame update
    void Start()
    {
        mHUD = GameObject.Find("Hud").GetComponent<Hud>();
        mButton.onClick.AddListener(OnClick);
    }
    void OnClick() {
        if(mIsItem)
            mHUD.OpenItemInfo(mActor, mId);
    }
    public void SetItem(Actor actor, string itemId, int quantity) {
        mActor = actor;
        mId = itemId;
        mAmount = quantity;
        mIsItem = true;

        mStringBuilder.Clear();
        mStringBuilder.Append(ItemHandler.Instance.GetItemInfo(mId).name);
        mStringBuilder.Append(newline);
        mStringBuilder.Append(quantity.ToString());

        mText.text = mStringBuilder.ToString();
    }
    public void SetSatisfaction(Actor actor, string satisfactionId, int amount) {
        mActor = actor;
        mId = satisfactionId;
        mAmount = amount;
        mIsItem = false;

        mStringBuilder.Clear();
        mStringBuilder.Append(SatisfactionDefine.Instance.GetTitle(mId));
        mStringBuilder.Append(newline);
        mStringBuilder.Append(mAmount.ToString());

        mText.text = mStringBuilder.ToString();
    }
}
