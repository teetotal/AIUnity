using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ENGINE;
using ENGINE.GAMEPLAY.BATTLE_CHESS_TACTIC;

public class ChessTactic_SoldierController : MonoBehaviour
{
    [SerializeField]
    private Transform bullet;
    private Vector3 bulletStartPoint, bulletInitLocalPosition;

    private bool IsReady = false;
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
        Walk,
        Recovery
    }
    private const string AnimationId = "AnimationId";
    private BattleActorUI mUI;
    private GameObject mUIObject;
    public void Init(ChessTactic_Controller controller, Soldier soldier) {
        mController = controller;
        mSoldier = soldier;
    }
    private void SetAnimation(AnimationCode code) {
        mAnimator.SetInteger(AnimationId, (int)code);
    }
    public void ActionStart(Rating rating) {
        mStartPosition = transform.position;
        mCurrentActionType = rating.type;
        mCurrentActionTarget = rating.targetId;
        IsReady = true;

        switch(rating.type) {
            //Recovery
            case BehaviourType.RECOVERY: {
                SetAnimation(AnimationCode.Recovery);
            }
            break;
            //Move
            case BehaviourType.MOVE: {
                Position pos = mSoldier.GetMap().GetPosition(rating.targetId);
                mEndPosition = mController.GetTilePosition(pos.x, pos.y) + new Vector3(Random.Range(-2.5f, 2.5f), 0 , Random.Range(-2.5f, 2.5f));

                if(Vector3.Distance(mStartPosition, mEndPosition) < 5)
                    SetAnimation(AnimationCode.Walk);
                else
                    SetAnimation(AnimationCode.Run);
            }
            break;
            //Attack
            case BehaviourType.ATTACK: {
                //bullet
                bullet.gameObject.SetActive(true);
                bullet.localPosition = bulletInitLocalPosition;
                bulletStartPoint = bullet.position;

                GameObject target = mController.GetSoldierObject(!rating.isHome, mCurrentActionTarget);
                mEndPosition = target.transform.position;
                SetAnimation(AnimationCode.Fire);
            }
            break;
            //Keep
            case BehaviourType.KEEP: {
                Position pos = mSoldier.GetPosition();
                mEndPosition = mController.GetTilePosition(pos.x, pos.y);
                SetAnimation(AnimationCode.Idle);

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
                //bulet
                // process % 0.1
                bullet.position = Vector3.Lerp(bulletStartPoint, mEndPosition + new Vector3(0, 0.5f, 0), (process % 0.3f) * 3.3f);
            break;
            case BehaviourType.MOVE: 
                transform.position = Vector3.Lerp(mStartPosition, mEndPosition, process);
                transform.LookAt(mEndPosition);
            break;
            default:
            break;
        }
    }
    public void ActionFinish(Soldier.State state) {
        bullet.gameObject.SetActive(false);

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
        // ---------------------------------
        string sz = string.Empty;
        if(state.attack > 0) {
            if(state.isHit)
                sz += "!";
            sz += state.attack.ToString();
        }
        if(state.damage > 0) {
            sz += " -" + state.damage.ToString();
            sz += " =" + mSoldier.GetHP().ToString();
        }
        if(sz.Length > 0)
            mUI.SetMessage(sz);
        // -------------------------------------
        if(state.isDie) {
            SetAnimation(AnimationCode.Death);
            mSoldier.SetDie();
            IsReady = false;
        }
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
        //bullet
        bulletInitLocalPosition = bullet.localPosition;
        bulletStartPoint = bullet.position;
    }
}
