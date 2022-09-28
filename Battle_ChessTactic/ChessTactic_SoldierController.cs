using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ENGINE;
using ENGINE.GAMEPLAY.BATTLE_CHESS_TACTIC;

public class ChessTactic_SoldierController : MonoBehaviour
{
    public bool IsReady = false;
    private Soldier mSoldier;
    private ChessTactic_Controller mController;
    private Vector3 mStartPosition, mEndPosition;
    private BehaviourType mCurrentActionType;
    private int mCurrentActionTarget;
    private Animator mAnimator;
    private Vector3 ADJUST_ROTATION_VECTOR = new Vector3(0, 45, 0);
    private enum AnimationCode {
        Idle = 0,
        Run,
        Fire,
        Death,
        Walk
    }
    private const string AnimationId = "AnimationId";
    private BattleActorUI mUI;
    private GameObject mUIObject;
    public void Init(ChessTactic_Controller controller, Soldier soldier) {
        mController = controller;
        mSoldier = soldier;
    }
    public void ActionStart(Rating rating) {
        mStartPosition = transform.position;
        mCurrentActionType = rating.type;
        mCurrentActionTarget = rating.targetId;
        IsReady = true;

        switch(rating.type) {
            //Move
            case BehaviourType.MOVE:{
                Position pos = mSoldier.GetMap().GetPosition(rating.targetId);
                mEndPosition = mController.GetTilePosition(pos.x, pos.y) + new Vector3(Random.Range(-2.5f, 2.5f), 0 , Random.Range(-2.5f, 2.5f));

                if(Vector3.Distance(mStartPosition, mEndPosition) < 5)
                    mAnimator.SetInteger(AnimationId, (int)AnimationCode.Walk);
                else
                    mAnimator.SetInteger(AnimationId, (int)AnimationCode.Run);
            }
            break;
            //Attack
            case BehaviourType.ATTACK: {
                GameObject target = mController.GetSoldierObject(!rating.isHome, mCurrentActionTarget);
                mEndPosition = target.transform.position;
                mAnimator.SetInteger(AnimationId, (int)AnimationCode.Fire);
            }
            break;
            //Keep
            case BehaviourType.KEEP: {
                Position pos = mSoldier.GetPosition();
                mEndPosition = mController.GetTilePosition(pos.x, pos.y);
                mAnimator.SetInteger(AnimationId, (int)AnimationCode.Idle);

            }
            break;
        }

        //Debug.Log(string.Format("home: {0}, id: {1}, target: {2}", mSoldier.IsHome(), mSoldier.GetID(), mCurrentActionTarget));
    }
    public void ActionUpdate(float process) {
        if(!IsReady)
            return;

        switch(mCurrentActionType) {
            case BehaviourType.ATTACK: 
                transform.LookAt(mEndPosition);
                Vector3 rot = transform.rotation.eulerAngles + ADJUST_ROTATION_VECTOR;
                transform.rotation = Quaternion.Euler(rot);
            break;
            case BehaviourType.MOVE: 
                transform.position = Vector3.Lerp(mStartPosition, mEndPosition, process);
                transform.LookAt(mEndPosition);
            break;
            case BehaviourType.KEEP:
            break;
        }
    }
    public void ActionFinish(Soldier.State state) {
        if(!IsReady)
            return;

        switch(mCurrentActionType) {
            case BehaviourType.ATTACK: 
            break;
            case BehaviourType.MOVE: {
                transform.position = mEndPosition;
            }
            break;
        }

        string sz = string.Empty;
        if(state.attack > 0) {
            sz += "+" + state.attack.ToString();
        }
        if(state.damage > 0) {
            sz += " -" + state.damage.ToString();
            sz += " =" + mSoldier.GetHP().ToString();
        }
        if(state.isDie) {
            mAnimator.SetInteger(AnimationId, (int)AnimationCode.Death);
            mSoldier.SetDie();
            IsReady = false;
        }

        if(sz.Length > 0)
            mUI.SetMessage(sz);
        mUI.SetHP(mSoldier.GetHP());

    }
    void Start()
    {
        mAnimator = GetComponent<Animator>();
        //UI 
        var prefab = Resources.Load<GameObject>("ActorUI");
        var canvas = GameObject.Find("Canvas");
        if(prefab != null && canvas != null) {
            mUIObject = Instantiate<GameObject>(prefab, Vector3.zero, Quaternion.identity);
            mUIObject.name = "ActorUI_" + name;
            mUIObject.transform.SetParent(canvas.transform);
            mUI = mUIObject.GetComponent<BattleActorUI>();
            mUI.targetName = name;
            mUI.SetName(mSoldier.GetName());
            mUI.SetHP(mSoldier.GetHP());
        }
    }
}
