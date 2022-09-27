using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ENGINE;
using ENGINE.GAMEPLAY.BATTLE_CHESS_TACTIC;

public class ChessTactic_SoldierController : MonoBehaviour
{
    public bool IsInit = false;
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
    public void Init(ChessTactic_Controller controller, Soldier soldier) {
        mController = controller;
        mSoldier = soldier;
    }
    public void ActionStart(Rating rating) {
        mStartPosition = transform.position;

        if(rating.targetId == -1) {
            Position pos = mSoldier.GetPosition();
            mEndPosition = mController.GetTilePosition(pos.x, pos.y);
        } else {
            Position pos = mSoldier.GetMap().GetPosition(rating.targetId);
            mEndPosition = mController.GetTilePosition(pos.x, pos.y);
            if(rating.type == BehaviourType.MOVE)
                mEndPosition += new Vector3(Random.Range(-2.5f, 2.5f), 0 , Random.Range(-2.5f, 2.5f));
        }
        
        mCurrentActionType = rating.type;
        mCurrentActionTarget = rating.targetId;
        IsInit = true;

        float distance = Vector3.Distance(mStartPosition, mEndPosition);
        //Debug.Log(distance);

        switch(rating.type) {
            case BehaviourType.MOVE:{
                if(distance < 5)
                    mAnimator.SetInteger(AnimationId, (int)AnimationCode.Walk);
                else
                    mAnimator.SetInteger(AnimationId, (int)AnimationCode.Run);
            }
            break;
            case BehaviourType.ATTACK:
            mAnimator.SetInteger(AnimationId, (int)AnimationCode.Fire);
            break;
            case BehaviourType.KEEP:
            mAnimator.SetInteger(AnimationId, (int)AnimationCode.Idle);
            break;
        }

        //Debug.Log(string.Format("home: {0}, id: {1}, target: {2}", mSoldier.IsHome(), mSoldier.GetID(), mCurrentActionTarget));
    }
    public void ActionUpdate(float process) {
        if(!IsInit)
            return;

        switch(mCurrentActionType) {
            case BehaviourType.ATTACK: 
                transform.LookAt(mEndPosition);
            break;
            case BehaviourType.MOVE: 
                transform.position = Vector3.Lerp(mStartPosition, mEndPosition, process);
                transform.LookAt(mEndPosition);
            break;
            case BehaviourType.KEEP:
            break;
        }
    }
    public void ActionFinish() {
        if(!IsInit)
            return;

        switch(mCurrentActionType) {
            case BehaviourType.ATTACK: 
               
            break;
            case BehaviourType.MOVE: {
                transform.position = mEndPosition;
            }
            break;
        }
    }
    void Start()
    {
        mAnimator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        switch(mCurrentActionType) {
            case BehaviourType.ATTACK: 
                Vector3 rot = transform.rotation.eulerAngles + ADJUST_ROTATION_VECTOR;
                transform.rotation = Quaternion.Euler(rot);
            break;
        }
    }
}
