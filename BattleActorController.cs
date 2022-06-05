using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ENGINE.GAMEPLAY.MOTIVATION;

public class BattleActorController : MonoBehaviour
{
    public string UIPrefab;
    private GameObject mUIObject;
    private BattleActorUI mUI;
    Animator mAnimator;    
    float mAccCounter = 0;
    Vector3 mMovingFrom;
    Quaternion mRotationFrom, mRotationTo;
    Vector3 mMovingTo;
    float mMovingTime;
    float mSpeed;
    BATTLE_ACTOR_ACTION_TYPE mCurrActionType;
    BattleController mBattleController;
    
    // Start is called before the first frame update
    void Start()
    {
        var p = GameObject.Find("BattleHandler");
        if(p != null)
            mBattleController = p.GetComponent<BattleController>();
            
        mAnimator = gameObject.GetComponent<Animator>();
        //UI 
        var prefab = Resources.Load<GameObject>(UIPrefab);
        var canvas = GameObject.Find("Canvas");
        if(prefab != null && canvas != null) {
            mUIObject = Instantiate<GameObject>(prefab, Vector3.zero, Quaternion.identity);
            mUIObject.name = "ActorUI_" + name;
            mUIObject.transform.SetParent(canvas.transform);
            mUI = mUIObject.GetComponent<BattleActorUI>();
            mUI.targetName = name;
            mUI.SetName(name);
        }

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
                if(mMovingTime <= mAccCounter) {
                    if(mBattleController != null) mBattleController.Occupy(this.name);
                    SetIdle();
                } else {                    
			        gameObject.transform.rotation = Quaternion.Lerp(mRotationFrom, mRotationTo, mAccCounter * 4);
                    gameObject.transform.position = Vector3.Lerp(mMovingFrom, mMovingTo, mAccCounter * mSpeed);
                }
                
            }
            break;
        }        
    }
    void SetHP(float amount) {        
        mUI.SetHP(amount);
    }
    void SetMessage(string message) {        
        mUI.SetMessage(message);
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

        SetHP(0);
        mUIObject.SetActive(false);
        
    }
    public void SetWalk(Vector3 to, float speed) {
        mMovingFrom = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, gameObject.transform.position.z);
        mMovingTo = to;
        mSpeed = speed / Vector3.Distance(mMovingFrom, mMovingTo); //cost를 줄이기 위해 미리 계산
        mMovingTime = Vector3.Distance(mMovingFrom, mMovingTo) / speed; //이동까지 걸릴 시간

        mRotationFrom = this.transform.rotation;        
        mRotationTo = Quaternion.LookRotation(mMovingTo - this.transform.position);
        SetActionType(BATTLE_ACTOR_ACTION_TYPE.MOVING);

        mAnimator.SetBool("IsIdle", false);
        mAnimator.SetBool("IsWalk", true);
        mAnimator.SetBool("IsAttack", false);
        mAnimator.SetBool("IsDie", false);
        mAnimator.SetBool("IsAttacked", false);
    }
    public void SetAttack(float hpRatio, BattleActorAction action) {
        SetActionType(BATTLE_ACTOR_ACTION_TYPE.ATTACKING);

        mAnimator.SetBool("IsIdle", false);
        mAnimator.SetBool("IsWalk", false);
        mAnimator.SetBool("IsAttack", true);
        mAnimator.SetBool("IsDie", false);
        mAnimator.SetBool("IsAttacked", false);
        //SetMessage(string.Format("{0} {1}({2})", action.Counter, action.TargetActorId, action.AttackAmount));
    }
    public void SetAttacked(float hpRatio, BattleActorAction action) {
        SetActionType(BATTLE_ACTOR_ACTION_TYPE.ATTACKED);
        mAnimator.SetBool("IsIdle", false);
        mAnimator.SetBool("IsWalk", false);
        mAnimator.SetBool("IsAttack", false);
        mAnimator.SetBool("IsDie", false);
        mAnimator.SetBool("IsAttacked", true);
        SetHP(hpRatio);
        SetMessage(string.Format("{0} {1}({2})", action.Counter, action.TargetActorId, action.AttackAmount));
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
