using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ENGINE.GAMEPLAY.MOTIVATION;
public class ActorControllerApproching {
    public float timer { get; set; }
    public Transform to { get; set; }
    public Vector3 fromPosition { get; set; }
    //public Quaternion toQuaternion { get; set; }
    public Quaternion fromQuaternion { get; set; }
    public float distance { get; set; }
    public float time { get; set; }
    public float speed { get; set; }    
    public ActorControllerApproching(Transform from, Transform to, float speed) {
        this.fromPosition = new Vector3(from.position.x, from.position.y, from.position.z);        
        this.fromQuaternion = from.rotation;
        this.to = to;
        this.speed = speed;
        this.timer = 0;
        //this.fromQuaternion = fromQ;        
        //this.toQuaternion = Quaternion.LookRotation(to - from);
        SetDistance();
        SetTime();
    }
    public float GetTimeRate(float deltaTime) {
        timer += deltaTime;
        return Mathf.Min(1, timer/time);
    }
    private void SetDistance() {
        distance = Vector3.Distance(fromPosition, to.position);
    }
    private void SetTime() {
        //시간 = 거리 / 속력
        time = distance / speed;
    }
}

public class ActorController : MonoBehaviour
{    
    enum STATE {
        IDLE,
        ACK,
        READY_MOVING,
        MOVING,
        //APPROCHING,
        START_TASK,
        TASK,
        FINISH_TASK,
        LEVELUP,
    };
    public string UIPrefab = "ActorUI";
    private GameObject mUIObject;
    private BattleActorUI mUI;
    public float ApprochRange = 3f;
    public string AnimationId = "AnimationId";
    public string StopAnimation = "Idle";
    public string GameController = "GamePlay";
    private GamePlayController mGamePlayController;
    private Transform mTargetTransform;
    private NavMeshAgent mAgent;
    private string[] mAnimationIds = {"Idle", "Walk", "Greeting", "Strong", "Bashful", "Digging", "Levelup"};
    private Dictionary<string, int> mDicAnimation = new Dictionary<string, int>();
    private Animator mAnimator;
    private STATE mState = STATE.IDLE;
    private float mTimer = 0;
    private FnTask? mCurrTask = null;
    private ActorControllerApproching? mApprochingContext;
    private Actor mActor;
    private float mDefaultWaitTime = 3.2f; 
    private float mDefaultWaitTimeMin = 0.8f;
    void Start()
    {        
        mActor = ActorHandler.Instance.GetActor(name);
        for(int i =0; i < mAnimationIds.Length; i++ ) {
            mDicAnimation.Add(mAnimationIds[i], i);
        }
        mAnimator = GetComponent<Animator>();

        mAgent = gameObject.GetComponent<NavMeshAgent>();
        if(mAgent == null) {
            Debug.Log("Invalid NavMeshAgent");
        }
        mGamePlayController = GameObject.Find(GameController).GetComponent<GamePlayController>();

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
    public void LookAtMe(Transform tr) {
        transform.LookAt(tr);
    }
    public void Ack(FnTask task, string from) {      
        mActor.SetState(Actor.STATE.TASKED);
        mUI.SetMessage(ScriptHandler.Instance.GetScriptAck(task.mTaskId, name, from));
        SetAnimation(task.mInfo.animationAck);
        mState = STATE.ACK;
        mTimer = 0;
    }

    // Update is called once per frame
    void Update()
    {
        mActor.SetPosition(transform.position.x, transform.position.y, transform.position.z);

        switch(mState) {
            case STATE.IDLE:            
            mTimer = 0;
            break;
            case STATE.ACK:
            mTimer += Time.deltaTime;
            if(mTimer > mDefaultWaitTime) {
                //Debug.Log(name + " ACK");
                mActor.SetState(Actor.STATE.READY);
                mState = STATE.IDLE;
                SetAnimation(StopAnimation);
            }
            break;
            case STATE.READY_MOVING:
            {
                mTimer += Time.deltaTime;
                if(mTimer > 1) {
                    //Debug.Log(name + " READY_MOVING");
                    mAgent.ResetPath();                    
                    mState = STATE.MOVING;
                    SetAnimation("Walk");
                    mTimer = 0;
                }
            }
            break;
            case STATE.MOVING:
            {
                if(GetDistance() < ApprochRange) {                    
                    //mState = STATE.APPROCHING;
                    //mApprochingContext = new ActorControllerApproching(transform, GetDestination(), 2);
                    mTargetTransform = null;
                    mAgent.isStopped = true;

                    mState = STATE.START_TASK;   
                    SetAnimation(StopAnimation);  
                } else {
                    mAgent.destination = mTargetTransform.position;
                }            
            }
            break;
            /*
            case STATE.APPROCHING: {
                float rate = mApprochingContext.GetTimeRate(Time.deltaTime);
                transform.position = Vector3.Lerp(mApprochingContext.fromPosition, mApprochingContext.to.position, rate);
                //transform.LookAt(mApprochingContext.to);
                transform.rotation = Quaternion.Lerp(mApprochingContext.fromQuaternion, mApprochingContext.to.rotation, rate);
                if(rate >= 1) {
                    //Debug.Log(name + " APPROCHED");                    
                    mState = STATE.START_TASK;                    
                    SetAnimation(StopAnimation);    
                }
            }
            break;
            */
            case STATE.START_TASK: {
                mTimer += Time.deltaTime;                

                if(mTimer > mDefaultWaitTimeMin) {
                    //Debug.Log(name + " START_TASK");
                    mTimer = 0;
                    mCurrTask = mGamePlayController.GetTask(name);
                    SetAnimation(mCurrTask.GetAnimation());                
                    mState = STATE.TASK;
                    mApprochingContext = null;       
                    //상대가 있는 경우 바라본다.
                    if(mActor.mTaskTarget.Item1) {
                        GameObject? obj = mGamePlayController.GetActorObject(mActor.mTaskTarget.Item2);
                        if(obj != null) {
                            transform.LookAt(obj.transform);
                            //나를 바라보게 한다.
                            obj.GetComponent<ActorController>().LookAtMe(transform);                        
                            //대사
                            mUI.SetMessage(ScriptHandler.Instance.GetScript(mCurrTask.mTaskId, name, mActor.mTaskTarget.Item2) );
                        }
                    }            
                }                
            }
            break;
            case STATE.TASK: {
                mTimer += Time.deltaTime;
                if(mCurrTask.mInfo.time < mTimer) {      
                    //Debug.Log(name + " TASK");                                  
                    SetAnimation(StopAnimation);
                    mState = STATE.FINISH_TASK;
                    mTimer = 0;
                    
                    //ack                    
                    if(mActor.mTaskTarget.Item1) {
                        mGamePlayController.Ack(mActor.mTaskTarget.Item2, name, mCurrTask);
                        /*
                        GameObject? obj = mGamePlayController.GetActorObject(mActor.mTaskTarget.Item2);
                        if(obj != null) {                            
                            obj.GetComponent<ActorController>().Ack(mCurrTask, name);
                        }*/
                    }
                    SetAnimation(StopAnimation); 
                }
            }
            break;
            case STATE.FINISH_TASK:
            mTimer += Time.deltaTime;
            if(mTimer > mDefaultWaitTime)  {
                Debug.Log(name + " FINISH_TASK");
                mTimer = 0;
                mCurrTask = null;

                if(!mGamePlayController.DoTask(name)) {
                    Debug.Log(name + " DoTask Failure");
                }
                if(mActor.checkLevelUp()) {
                    var reward = LevelHandler.Instance.Get(mActor.mType, mActor.mLevel);
                    if(!mActor.LevelUp(reward.next.rewards)) {
                        Debug.Log(name + " LevelUp Failure");
                    }
                    mState = STATE.LEVELUP;
                    SetAnimation("Levelup");      
                    mUI.SetMessage("LEVEL UP! lv." + mActor.mLevel.ToString());              

                } else {
                    mState = STATE.IDLE;
                    mActor.SetState(Actor.STATE.READY);
                    SetAnimation(StopAnimation); 
                }
            }            
            break;
            case STATE.LEVELUP: {
                mTimer += Time.deltaTime;                
                if(mTimer > mDefaultWaitTime)  {                    
                    Debug.Log(name + " LEVEL UP. lv" + mActor.mLevel.ToString());
                    mTimer = 0;                    
                    SetAnimation(StopAnimation); 
                    mState = STATE.IDLE;
                    mActor.SetState(Actor.STATE.READY);
                }
            }
            break;            
        }        
    }
    
    private float GetDistance() {
        float distance = Vector3.Distance(transform.position, mTargetTransform.position);
        return distance;        
    }
    //position값과 보이는 위치의 차이 보정
    private Transform GetDestination() {
        return mTargetTransform;
        /*
        Vector3 position = mTargetTransform.position;    
        if(mActor.mTaskTarget.Item1)    
            return new Vector3(position.x - 2.5f, position.y, position.z - 2.5f);
        else
            return new Vector3(position.x, position.y, position.z);
        */
    }
    public void SetAnimation(string animation) {
        //Debug.Log(name + "SetAnimation " + animation);
        if(mDicAnimation.ContainsKey(animation)) {
            int id = mDicAnimation[animation];
            mAnimator.SetInteger(AnimationId, id);
        }
    }
    public void MoveTo(string targetObject) {        
        if(mState != STATE.IDLE) {
            Debug.Log(name + " STATE ERROR! " + mState.ToString());
        }
        GameObject target = GameObject.Find(targetObject);
        if(target != null) {            
            mTargetTransform = target.transform;            
            mState = STATE.READY_MOVING;
            SetAnimation(StopAnimation);
        }
    }
}
