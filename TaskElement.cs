using ENGINE.GAMEPLAY.MOTIVATION;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TaskElement : MonoBehaviour
{
    public TextMeshProUGUI Text1, Text2;
    public Button Btn;
    private FnTask mFn;
    private GamePlayController mGamePlayController;
    private Hud mHud;
    private bool mActivation = false;
    private Color colorEnable = new Color(0,0,0,0);
    private Color colorDisable = new Color(0.5f,0.5f,0.5f,0.8f);

    // Start is called before the first frame update
    void Start()
    {
        Btn.onClick.AddListener(OnClick);
    }
    void OnClick() {
        //Debug.Log("Task OnClick. " + mTaskId);
        mHud.ReleaseTask();
        var actor = mGamePlayController.GetFollowActor();
        if(actor != null) {
            Actor.SET_TASK_ERROR err = actor.mActor.Loop_SetTask(mFn.mTaskId);
            switch(err) {
                case Actor.SET_TASK_ERROR.SUCCESS:
                break;
                default:
                Debug.Log(err);
                actor.mActor.Loop_TaskUI();
                break;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(mActivation) {
            var actor = mGamePlayController.GetFollowActor();
            if(actor == null)
                return;
            mFn.SetTaskString();
            if(!TaskHandler.Instance.CheckSatisfaction(actor.mActor, mFn) || !TaskHandler.Instance.CheckRef(mFn) || !TaskHandler.Instance.CheckTarget(mFn, actor.mActor)) {
                Btn.enabled = false;
                Text2.text = "<s>" + mFn.mTaskString + "</s>";
                Btn.GetComponent<Image>().color = colorDisable;
                return;
            }
            Btn.GetComponent<Image>().color = colorEnable;
            Text2.text = mFn.mTaskString;
            Btn.enabled = true;
        }
    }
    public void Init(Hud hud, GamePlayController gamePlayController) {
        mHud = hud;
        mGamePlayController = gamePlayController;
    }
    public void Set(FnTask fn) {
        var actor = mGamePlayController.GetFollowActor();
        if(actor == null)
            return;
        mFn = fn;
        Text1.text = string.Format("{0} - {1}\n{2}", 
                LevelHandler.Instance.Get(actor.mActor.mType, fn.mInfo.level[0]).title, 
                fn.mInfo.level[1],
                fn.mInfo.villageLevel);
        Text2.text = fn.mTaskString;
        mActivation = true;
    }
    public void Release() {
        mActivation = false;
    }
}
