using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ENGINE.GAMEPLAY.MOTIVATION;

public class ActorController : MonoBehaviour
{
    Animator mAnimator;    
    float mAccCounter = 0;
    Vector3 mMovingFrom;
    Quaternion mRotationFrom, mRotationTo;
    Vector3 mMovingTo;
    float mSpeed;
    BATTLE_ACTOR_ACTION_TYPE mCurrActionType;
    BattleController mBattleController;
    // Start is called before the first frame update
    void Start()
    {
        mBattleController = GameObject.Find("BattleHandler").GetComponent<BattleController>();
        mAnimator = gameObject.GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        mAccCounter += Time.deltaTime;

        switch(mCurrActionType) {
            case BATTLE_ACTOR_ACTION_TYPE.DYING:
            {                
                if(mAccCounter > 5) {
                    this.gameObject.SetActive(false);
                }
            }            
            break;            
            case BATTLE_ACTOR_ACTION_TYPE.MOVING:
            {   
                float distance = Vector3.Distance(gameObject.transform.position, mMovingTo);      
                if(distance < 0.1f) {
                    mBattleController.Occupy(this.name);
                    SetIdle();
                } else {                    
			        gameObject.transform.rotation = Quaternion.Lerp(mRotationFrom, mRotationTo, mAccCounter * 4);
                    gameObject.transform.position = Vector3.Lerp(mMovingFrom, mMovingTo, mAccCounter * mSpeed);
                }
                
            }
            break;
        }        
    }
    void SetActionType(BATTLE_ACTOR_ACTION_TYPE type) {
        mCurrActionType = type;
        mAccCounter = 0;
    }
    public void SetDie() {    
        SetActionType(BATTLE_ACTOR_ACTION_TYPE.DYING);

        mAnimator.SetBool("IsDie", true);
        mAnimator.SetBool("IsIdle", false);
        mAnimator.SetBool("IsWalk", false);
        mAnimator.SetBool("IsAttack", false);        
        mAnimator.SetBool("IsAttacked", false);
        
    }
    public void SetWalk(Vector3 to, float speed) {
        mMovingFrom = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, gameObject.transform.position.z);
        mMovingTo = to;
        mSpeed =speed / Vector3.Distance(mMovingFrom, mMovingTo); //cost를 줄이기 위해 미리 계산
        mRotationFrom = this.transform.rotation;        
        mRotationTo = Quaternion.LookRotation(mMovingTo - this.transform.position);
        SetActionType(BATTLE_ACTOR_ACTION_TYPE.MOVING);

        mAnimator.SetBool("IsIdle", false);
        mAnimator.SetBool("IsWalk", true);
        mAnimator.SetBool("IsAttack", false);
        mAnimator.SetBool("IsDie", false);
        mAnimator.SetBool("IsAttacked", false);
    }
    public void SetAttack() {
        SetActionType(BATTLE_ACTOR_ACTION_TYPE.ATTACKING);

        mAnimator.SetBool("IsIdle", false);
        mAnimator.SetBool("IsWalk", false);
        mAnimator.SetBool("IsAttack", true);
        mAnimator.SetBool("IsDie", false);
        mAnimator.SetBool("IsAttacked", false);
    }
    public void SetAttacked() {
        SetActionType(BATTLE_ACTOR_ACTION_TYPE.ATTACKED);
        mAnimator.SetBool("IsIdle", false);
        mAnimator.SetBool("IsWalk", false);
        mAnimator.SetBool("IsAttack", false);
        mAnimator.SetBool("IsDie", false);
        mAnimator.SetBool("IsAttacked", true);
    }
    public void SetIdle() {
        SetActionType(BATTLE_ACTOR_ACTION_TYPE.READY_ATTACKING);

        mAnimator.SetBool("IsIdle", true);
        mAnimator.SetBool("IsWalk", false);
        mAnimator.SetBool("IsAttack", false);
        mAnimator.SetBool("IsDie", false);
        mAnimator.SetBool("IsAttacked", false);
    }
}
