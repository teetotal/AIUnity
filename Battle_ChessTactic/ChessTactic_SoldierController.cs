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
    public void Init(ChessTactic_Controller controller, Soldier soldier) {
        mController = controller;
        mSoldier = soldier;
    }
    public void ActionStart(Rating rating) {
        mStartPosition = transform.position;
        Position pos = mSoldier.GetMap().GetPosition(rating.targetId);
        mEndPosition = mController.GetTilePosition(pos.x, pos.y);
        mCurrentActionType = rating.type;
        mCurrentActionTarget = rating.targetId;
        IsInit = true;

        Debug.Log(string.Format("home: {0}, id: {1}, target: {2}", mSoldier.IsHome(), mSoldier.GetID(), mCurrentActionTarget));
    }
    public void ActionUpdate(float process) {
        if(!IsInit)
            return;

        switch(mCurrentActionType) {
            case BehaviourType.MOVE: {
                transform.position = Vector3.Lerp(mStartPosition, mEndPosition, process);
                transform.LookAt(mEndPosition);
            }
            break;
        }
        
    }
    public void ActionFinish() {
        if(!IsInit)
            return;

        switch(mCurrentActionType) {
            case BehaviourType.MOVE: {
                transform.position = mEndPosition;
            }
            break;
        }
    }
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
