using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ENGINE.GAMEPLAY.MOTIVATION;


public class ActorControllerApproching {
    public bool enable = false;
    public float timer { get; set; }
    public Vector3 toPosition { get; set; }
    public Vector3 fromPosition { get; set; }
    public Vector3 lookAt { get; set; }
    public Quaternion fromQuaternion { get; set; }
    public float distance { get; set; }
    public float time { get; set; }
    public float speed { get; set; }    
    public void Set(Transform from, Vector3 toPosition, Vector3 lookAt, float speed) {
        this.enable = true;
        this.fromPosition = new Vector3(from.position.x, from.position.y, from.position.z);        
        this.fromQuaternion = from.rotation;
        this.toPosition = toPosition;
        this.lookAt = lookAt;
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
    public void Release() {
        this.enable = false;
    }
    private void SetDistance() {
        distance = Vector3.Distance(fromPosition, toPosition);
    }
    private void SetTime() {
        //시간 = 거리 / 속력
        time = distance / speed;
    }
}

public class AnimationContext {
    public string name = "";
    public int count = 0;
    public int targetCount;
    public ActorController.STATE_ANIMATION_CALLBACK callbackState;
    private ActorController mActorController;
    private DateTime lastTime;
    public void Init(ActorController p) {
        mActorController = p;
    }
    public void Reset() {
        name = "";
        count = 0;
    }
    public bool Set(string animationName, int targetCount, ActorController.STATE_ANIMATION_CALLBACK callbackState) {
        if(name.Length > 0)
            return false;
        this.name = animationName;
        this.targetCount = targetCount;
        this.callbackState = callbackState;
        mActorController.SetAnimation(this.name);
        lastTime = DateTime.Now;
        return true;
    }
    public bool Increase() {
        TimeSpan span = DateTime.Now - lastTime;
        if(span.TotalMilliseconds < 900)
            return false;
        count ++;
        lastTime = DateTime.Now;
        if(count >= targetCount) {
            return true;
        }
        return false;
    }
}
public class ActorController : MonoBehaviour
{    
    enum STATE {
        INVALID,
        IDLE,
        READY_MOVING,
        MOVING,
        APPROCHING,
        APPROCHING_FINISH,
    };
    public enum STATE_ANIMATION_CALLBACK {
        ACK,
        LOOK_AT_CALLER,
        START_TASK,
        TASK,
        FINISH_TASK,
        LEVELUP,
    }
    
    //config
    public string UIPrefab = "ActorUI";    
    public float ApprochRange = 3f;
    public string AnimationId = "AnimationId";
    public string StopAnimation = "Idle";
    public string GameController = "GamePlay";
    //private string[] mAnimationIds = {"Idle", "Walk", "Greeting", "Strong", "Bashful", "Digging", "Levelup", "Dancing", "Drinking"};    
    private float mDefaultWaitTimeMin = 0.3f;

    //unity objects
    private GameObject mUIObject;
    private BattleActorUI mUI;
    private GamePlayController mGamePlayController;

    class Target {
        public bool enable = false;
        public bool isPositionOnly = false;
        public Transform transform;
        public Vector3 position = Vector3.zero;
        public void SetPostion(Vector3 position) {
            this.enable = true;
            this.isPositionOnly = true;
            this.transform = null;
            this.position = position;
        }
        public void SetTransform(Transform transform) {
            this.enable = true;
            this.isPositionOnly = false;
            this.transform = transform;
            this.position = Vector3.zero;
        }
        public void Release() {
            this.enable = false;
        }
    }
    private Target mTarget = new Target();
    private ActorControllerApproching mApprochingContext = new ActorControllerApproching();
    
    private float mTargetPositionRandom = 0;
    private NavMeshAgent mAgent;
    private Animator mAnimator;
    
    //variable
    private Queue<Actor.CALLBACK_TYPE> mCallbackQueue = new Queue<Actor.CALLBACK_TYPE>();    
    private STATE mState = STATE.INVALID;
    private AnimationContext mAnimationContext = new AnimationContext(); 
    private float mTimer = 0;
    private Actor mActor;
    
    void Start()
    {        
        mAnimationContext.Init(this);
        mActor = ActorHandler.Instance.GetActor(name);
        
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
    public void Callback(Actor.CALLBACK_TYPE type, string actorId) {
        //Debug.Log(name + " " + type.ToString());
        switch(type) {            
            case Actor.CALLBACK_TYPE.TAKE_TASK:
            case Actor.CALLBACK_TYPE.ASKED:
            mCallbackQueue.Enqueue(type);
            Stop();
            break;
            case Actor.CALLBACK_TYPE.SET_READY:
            break;
            case Actor.CALLBACK_TYPE.DO_TASK:
            break;
            case Actor.CALLBACK_TYPE.RESERVE:
            break;
            case Actor.CALLBACK_TYPE.RESERVED:
            break;
            case Actor.CALLBACK_TYPE.ASK:
            break;
            case Actor.CALLBACK_TYPE.INTERRUPT:
            break;
            case Actor.CALLBACK_TYPE.INTERRUPTED:
            break;
            case Actor.CALLBACK_TYPE.REFUSAL:
            break;
        }
    }
    private void Dequeue() {
        if(mCallbackQueue.Count == 0)
            return;
        Actor.CALLBACK_TYPE type = mCallbackQueue.Dequeue();
        switch(type) {
            case Actor.CALLBACK_TYPE.SET_READY:
            return;
            case Actor.CALLBACK_TYPE.TAKE_TASK:
            {
                mUI.SetMessage(mActor.GetCurrentTask().mInfo.title);
                Actor.TaskContext_Target p = mActor.GetTaskContext().target; 
                switch(p.type) {
                    case Actor.TASKCONTEXT_TARGET_TYPE.INVALID:
                    throw new Exception("Dequeue error! Invalid target type.");
                    case Actor.TASKCONTEXT_TARGET_TYPE.NON_TARGET:
                    StartTask();
                    break;
                    case Actor.TASKCONTEXT_TARGET_TYPE.POSITION:                        
                        mTarget.SetPostion(new Vector3(p.position.x, p.position.y, p.position.z));
                        mTargetPositionRandom = ApprochRange;
                        mState = STATE.READY_MOVING;
                    break;
                    default:
                    { 
                        GameObject target = GameObject.Find(p.objectName);
                        if(target == null) {        
                            throw new Exception("Invalid GameObject Name " + p.objectName);
                        }
                        mTargetPositionRandom = UnityEngine.Random.Range(ApprochRange, ApprochRange * 2f);                          
                        mTarget.SetTransform(target.transform);             
                        mState = STATE.READY_MOVING;                                                    
                    }
                    break;
                }
            }
            return;
            case Actor.CALLBACK_TYPE.DO_TASK:
            return;
            case Actor.CALLBACK_TYPE.RESERVE:
            return;
            case Actor.CALLBACK_TYPE.RESERVED:
            return;
            case Actor.CALLBACK_TYPE.ASK:
            return;
            case Actor.CALLBACK_TYPE.ASKED: {
            //mState = STATE.LOOK_AT_CALLER;
            string from = mActor.GetTaskContext().interactionFromActor.mUniqueId;
            var fromObj = mGamePlayController.GetActorObject(from);
            if(fromObj != null) {
                transform.LookAt(fromObj.transform);
            }
            Stop();
            }
            return;
            case Actor.CALLBACK_TYPE.INTERRUPT:
            break;
            case Actor.CALLBACK_TYPE.INTERRUPTED:
            return;
        }
    }    
    void CallbackAnimationFinish(STATE_ANIMATION_CALLBACK state) {
        //Debug.Log("CallbackAnimationFinish " + state.ToString());
        switch(state) {
            case STATE_ANIMATION_CALLBACK.START_TASK:
            mAnimationContext.Set(mActor.GetCurrentTask().GetAnimation(), mActor.GetCurrentTask().mInfo.time, STATE_ANIMATION_CALLBACK.TASK);
            var target = mActor.GetTaskContext().target;
            if(target.type == Actor.TASKCONTEXT_TARGET_TYPE.ACTOR) {
                mUI.SetMessage(ScriptHandler.Instance.GetScript(mActor.GetTaskContext().currentTask.mTaskId, mActor, ActorHandler.Instance.GetActor(target.objectName)) );
                //lookat
                var to = mGamePlayController.GetActorObject(target.objectName);
                if(to != null) {
                    transform.LookAt(to.transform);
                }
            } else {
                mUI.SetMessage(ScriptHandler.Instance.GetScript(mActor.GetTaskContext().currentTask.mTaskId, mActor) );
            }
            if(!mActor.DoTaskBefore()) {
                Debug.Log(string.Format("{0} DoTaskBefore Failure", name));                    
            }
            break;
            case STATE_ANIMATION_CALLBACK.TASK:
            mUI.SetMessage(mActor.GetTaskString());
            mAnimationContext.Set(StopAnimation, 1, STATE_ANIMATION_CALLBACK.FINISH_TASK);            
            break;
            case STATE_ANIMATION_CALLBACK.FINISH_TASK:
            Tuple<bool, bool> ret = mActor.DoTask();
            if(!ret.Item1) {
                Debug.Log(name + " DoTask Failure");                    
            } else {
                if(ret.Item2) {              
                    mAnimationContext.Set("Levelup", 1, STATE_ANIMATION_CALLBACK.LEVELUP);      
                    mUI.SetMessage("LEVEL UP! lv." + mActor.mLevel.ToString());
                    break;
                }
            }
            Stop();
            break;
        }
    }
    // Update is called once per frame
    void Update()
    {
        mActor.SetPosition(transform.position.x, transform.position.y, transform.position.z);

        if(mAnimationContext.name.Length > 0) {
            if( mAnimator.GetCurrentAnimatorStateInfo(0).IsName(mAnimationContext.name) && 
                mAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f) 
            {
                //Debug.Log(mAnimationContext.name + " Done " + mAnimationContext.count.ToString() + " / " + mAnimationContext.targetCount.ToString());
                if(mAnimationContext.Increase()) {
                    STATE_ANIMATION_CALLBACK state = mAnimationContext.callbackState;
                    mAnimationContext.Reset();
                    CallbackAnimationFinish(state);
                }
            }
            return;
        } 

        switch(mState) {
            case STATE.INVALID:
            break;
            case STATE.IDLE:            
            mTimer += Time.deltaTime;
            if(mTimer > mDefaultWaitTimeMin) {
                //Do something
                Dequeue();
            }
            break;
            case STATE.READY_MOVING:
            {
                mTimer += Time.deltaTime;
                if(mTimer > mDefaultWaitTimeMin) {
                    mAgent.ResetPath();                    
                    mState = STATE.MOVING;
                    SetAnimation("Walk");
                    mTimer = 0;
                }
            }
            break;
            case STATE.MOVING:
            {
                if(GetDistance() <  mTargetPositionRandom) {
                    mTimer = 0;                    
                    mAgent.isStopped = true;
                    //SetAnimation(StopAnimation);
                    //mState = STATE.START_TASK;   
                    if(mTarget.isPositionOnly) {
                        Position lookAt = mActor.GetCurrentTask().mInfo.target.lookAt;                        
                        mApprochingContext.Set(transform, mTarget.position, new Vector3(lookAt.x, lookAt.y, lookAt.z), mAgent.acceleration);
                        mState = STATE.APPROCHING;
                        mTimer = 0;
                    } else {
                        mTarget.Release();
                        StartTask(); 
                    }                    
                } else {
                    if(!mTarget.enable)
                        throw new Exception("Target must be enable");
                    if(mTarget.isPositionOnly)
                        mAgent.destination = mTarget.position;
                    else
                        mAgent.destination = mTarget.transform.position;
                }            
            }
            break;   
            case STATE.APPROCHING:
                float rate = mApprochingContext.GetTimeRate(Time.deltaTime);
                transform.position = Vector3.Lerp(mApprochingContext.fromPosition, mApprochingContext.toPosition, rate);
                //transform.LookAt(mApprochingContext.toPosition);
                if(rate >= 1) {                    
                    mState = STATE.APPROCHING_FINISH;
                    SetAnimation(StopAnimation); 
                    mTimer = 0;
                }
            break;
            case STATE.APPROCHING_FINISH:
            {
                mTimer += Time.deltaTime;
                if(mTimer > mDefaultWaitTimeMin) {
                    transform.position = mApprochingContext.toPosition;
                    transform.LookAt(mApprochingContext.lookAt);
                    mTarget.Release();
                    mApprochingContext.Release();
                    StartTask(); 
                }
            }                
            break;
            /*
            case STATE.ACK:
            mTimer += Time.deltaTime;
            if(mTimer > mDefaultWaitTime) {                
                mState = STATE.IDLE;
                SetAnimation(StopAnimation);
            }
            break;         
            case STATE.LOOK_AT_CALLER: {
                string from = mActor.GetTaskContext().interactionFromActor.mUniqueId;
                var fromObj = mGamePlayController.GetActorObject(from);
                if(fromObj != null) {
                    transform.LookAt(fromObj.transform);
                }
                Stop();
            }
            break;
            case STATE.START_TASK: {
                mTimer += Time.deltaTime;
                if(mTimer > mDefaultWaitTimeMin) {
                    mTimer = 0;                    
                    SetAnimation(mActor.GetCurrentTask().GetAnimation());
                    mState = STATE.TASK;                    
                    
                    var target = mActor.GetTaskContext().target;
                    if(target.Item1) {
                        mUI.SetMessage(ScriptHandler.Instance.GetScript(mActor.GetTaskContext().currentTask.mTaskId, name, target.Item2) );
                    }
                    mActor.DoTaskBefore();
                }     
            }
            break;
            case STATE.TASK: {
                mTimer += Time.deltaTime;
                if(mActor.GetCurrentTask().mInfo.time < mTimer) {      
                    SetAnimation(StopAnimation);
                    mState = STATE.FINISH_TASK;
                    mTimer = 0;
                }
            }
            break;
            case STATE.FINISH_TASK:
            mTimer += Time.deltaTime;
            if(mTimer > mDefaultWaitTime)  {
                Debug.Log(name + " FINISH_TASK");
                mTimer = 0;
                Tuple<bool, bool> ret = mActor.DoTask();
                if(!ret.Item1) {
                    Debug.Log(name + " DoTask Failure");                    
                } else {
                    if(ret.Item2) {                    
                        mState = STATE.LEVELUP;
                        SetAnimation("Levelup");      
                        mUI.SetMessage("LEVEL UP! lv." + mActor.mLevel.ToString());
                        break;
                    }
                }
                Stop();
            }            
            break;
            case STATE.LEVELUP: {
                mTimer += Time.deltaTime;                
                if(mTimer > mDefaultWaitTime)  {                    
                    mTimer = 0;                    
                    Stop();
                }
            }
            break;     
            */       
        }        
    }
    private void StartTask() {
        if(!mAnimationContext.Set(StopAnimation, 1, STATE_ANIMATION_CALLBACK.START_TASK))
            Debug.Log("Animation Conext Failure");
        mState = STATE.INVALID;   
    }
    private void Stop() {
        SetAnimation(StopAnimation); 
        if(mState != STATE.IDLE) {
            mTimer = 0;
        }
        mState = STATE.IDLE;        
    }
    private float GetDistance() {
        if(!mTarget.enable) {
            throw new Exception("Target must be enable");
        }
        if(mTarget.isPositionOnly)
            return Vector3.Distance(transform.position, mTarget.position);
        else 
            return Vector3.Distance(transform.position, mTarget.transform.position);
    }    
    public void SetAnimation(string animation) {     
        int id = mGamePlayController.GetAnimationId(animation);   
        if((int)GamePlayController.ANIMATION_ID.Invalid < id) {            
            mAnimator.SetInteger(AnimationId, id);
        }
    }    
}
