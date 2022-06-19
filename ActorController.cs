using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ENGINE.GAMEPLAY;
using ENGINE.GAMEPLAY.MOTIVATION;
#nullable enable
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
    private ActorController? mActorController = null;
    private DateTime lastTime;
    public void Init(ActorController p) {
        mActorController = p;
    }
    public void Reset() {
        name = "";
        count = 0;
    }
    public bool Set(string animationName, int targetCount, ActorController.STATE_ANIMATION_CALLBACK callbackState) {
        if(name.Length > 0 || mActorController == null)
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
public class Target {
    public bool enable = false;
    public bool isPositionOnly = false;
    public Transform? transform { get; set; } = null;
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
public class ActorController : MonoBehaviour
{    
    enum STATE {
        INVALID,
        IDLE,
        READY_MOVING,
        MOVING,
        APPROCHING,        
    };
    public enum STATE_ANIMATION_CALLBACK {
        ACK,
        STOPPING,
        START_TASK,
        TASK,
        FINISH_TASK,
        LEVELUP,
        APPROCHING,
        DISAPPOINTED_START,
        DISAPPOINTED,
        DISAPPOINTED_FINISH,
    }
    
    //config
    public string UIPrefab = "ActorUI";    
    public float ApprochRange = 3f;
    public string AnimationId = "AnimationId";
    public string StopAnimation = "Idle";
    public string DisappointedAnimation = "Disappointed";
    public string GameController = "GamePlay";
    //private string[] mAnimationIds = {"Idle", "Walk", "Greeting", "Strong", "Bashful", "Digging", "Levelup", "Dancing", "Drinking"};    
    private float mDefaultWaitTimeMin = 0.3f;

    //unity objects
    private GameObject? mUIObject;
    private BattleActorUI? mUI;
    private GamePlayController mGamePlayController = new GamePlayController();
    private NavMeshAgent? mAgent;
    private Animator? mAnimator;
    
    //variable
    private Target mTarget = new Target();
    private ActorControllerApproching mApprochingContext = new ActorControllerApproching();    
    private float mTargetPositionRandom = 0;
    private Queue<Actor.CALLBACK_TYPE> mCallbackQueue = new Queue<Actor.CALLBACK_TYPE>();    
    private STATE mState = STATE.IDLE;
    private AnimationContext mAnimationContext = new AnimationContext(); 
    private float mTimer = 0;
    private Actor? mActor = null;    
    private bool mIsActorReleaseAtStopFinish = false; //거절당했을때 처럼 actor 상태를 초기화 할 시점을 StopFinish에 실행하게 하는 flag
    
    void Start()
    {        
        mAnimationContext.Init(this);
        var actor = ActorHandler.Instance.GetActor(name);
        if(actor == null)
            throw new Exception("Invalid Actor. " + name);
        mActor = actor;
        
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
        switch(type) {            
            case Actor.CALLBACK_TYPE.SET_READY:
            case Actor.CALLBACK_TYPE.TAKE_TASK:            
            case Actor.CALLBACK_TYPE.ASKED:            
            case Actor.CALLBACK_TYPE.REFUSAL:
            case Actor.CALLBACK_TYPE.LEVELUP:            
            mCallbackQueue.Enqueue(type);
            break;
            case Actor.CALLBACK_TYPE.DO_TASK:
            break;
            case Actor.CALLBACK_TYPE.RESERVE:
            break;            
            case Actor.CALLBACK_TYPE.RESERVED:
            {   
            }
            break;
            case Actor.CALLBACK_TYPE.ASK:
            break;
            case Actor.CALLBACK_TYPE.INTERRUPT:
            break;
            case Actor.CALLBACK_TYPE.INTERRUPTED:
            break;                        
        }
    }
    private void Dequeue() {
        if(mActor == null)
            throw new Exception("mActor is null");
        if(mCallbackQueue.Count == 0)
            return;
        Actor.CALLBACK_TYPE type = mCallbackQueue.Dequeue();
        //if(name == "꼼이") Debug.Log(string.Format("Dequeue {0}\t{1}", name, type));

        switch(type) {
            case Actor.CALLBACK_TYPE.SET_READY:
            Stop();
            return;
            case Actor.CALLBACK_TYPE.TAKE_TASK:
            {
                //SetMessage(mActor.GetCurrentTaskTitle());
                Actor.TaskContext_Target p = mActor.GetTaskContext().target; 
                switch(p.type) {
                    case Actor.TASKCONTEXT_TARGET_TYPE.INVALID:
                        //throw new Exception("Dequeue error! Invalid target type." + name);
                        //asked actor일 경우 여기로 올수 있다.
                    break;
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
            case Actor.CALLBACK_TYPE.ASKED:             
            return;
            case Actor.CALLBACK_TYPE.INTERRUPT:
            break;
            case Actor.CALLBACK_TYPE.INTERRUPTED:
            return;            
            case Actor.CALLBACK_TYPE.REFUSAL:
            {
                //Debug.Log("refusal " + name);
                LookAtCaller();
                SetMessage(GetScript(GetInteractionFromActor(), mActor.GetTaskContext().ackTaskId, true));
                mActor.GetTaskContext().ReleaseAck();
                //Stop();
            }
            return;        
            case Actor.CALLBACK_TYPE.LEVELUP:
            {
                mAnimationContext.Set("Levelup", 1, STATE_ANIMATION_CALLBACK.LEVELUP);      
                SetMessage("LEVEL UP! lv." + mActor.mLevel.ToString());
            }            
            return;    
        }
    }    
    void CallbackAnimationFinish(STATE_ANIMATION_CALLBACK state) {
        if(mActor == null)
            throw new Exception("mActor is null");

        switch(state) {
            case STATE_ANIMATION_CALLBACK.STOPPING:
            StopFinish();
            break;
            case STATE_ANIMATION_CALLBACK.START_TASK: {                
                STATE_ANIMATION_CALLBACK next = STATE_ANIMATION_CALLBACK.TASK;                
                if(!SetCurrentTaskAnimation(next)) {
                    throw new Exception("SetCurrentTaskAnimation Failure");
                }                
                var target = mActor.GetTaskContext().target;
                if(target.type == Actor.TASKCONTEXT_TARGET_TYPE.ACTOR) {
                    SetMessage(GetScript(ActorHandler.Instance.GetActor(target.objectName), mActor.GetCurrentTaskId()));
                    //lookat
                    var to = mGamePlayController.GetActorObject(target.objectName);
                    if(to != null) {
                        transform.LookAt(to.transform);
                    }
                } else {
                    SetMessage( GetScript(null, mActor.GetCurrentTaskId()) );
                }     
                //interaction type에 따라 카메라 앵글 변경
                SetInteractionCameraAngle();

            }            
            break;
            case STATE_ANIMATION_CALLBACK.TASK: {
                STATE_ANIMATION_CALLBACK next = STATE_ANIMATION_CALLBACK.FINISH_TASK;                
                if(!mActor.DoTaskBefore()) {
                    //거절 당한 상태
                    //거절 모션 처리
                    next = STATE_ANIMATION_CALLBACK.DISAPPOINTED_START;                    
                } 
                //SetMessage(mActor.GetTaskString());
                mAnimationContext.Set(StopAnimation, 1, next);
            }
            break;
            case STATE_ANIMATION_CALLBACK.FINISH_TASK:            
            mActor.DoTask();                  
            Stop();
            break;
            case STATE_ANIMATION_CALLBACK.LEVELUP:
            Stop();
            break;
            case STATE_ANIMATION_CALLBACK.APPROCHING:
            {
                transform.position = mApprochingContext.toPosition;
                transform.LookAt(mApprochingContext.lookAt);
                mTarget.Release();
                mApprochingContext.Release();
                StartTask(); 
            }           
            break;
            case STATE_ANIMATION_CALLBACK.DISAPPOINTED_START:
            mAnimationContext.Set(StopAnimation, 1, STATE_ANIMATION_CALLBACK.DISAPPOINTED);
            break;
            case STATE_ANIMATION_CALLBACK.DISAPPOINTED:
            mAnimationContext.Set(DisappointedAnimation, 1, STATE_ANIMATION_CALLBACK.DISAPPOINTED_FINISH);            
            break;
            case STATE_ANIMATION_CALLBACK.DISAPPOINTED_FINISH:            
            mIsActorReleaseAtStopFinish = true;
            Stop();                        
            break;            
        }
    }
    // Update is called once per frame
    void Update()
    {
        if(mActor == null || mAgent == null)
            throw new Exception("mActor or mAgent is null");

        mActor.SetPosition(transform.position.x, transform.position.y, transform.position.z);

        //주변의 actor를 쳐다 본다.
        if(mActor.GetTaskContext().state == Actor.STATE.RESERVED) {
            string actorId = mActor.LookAround();
            if(actorId != string.Empty) {
                GameObject? obj = mGamePlayController.GetActorObject(actorId);
                if(obj == null)
                    throw new Exception("Invalid ActorId. " + actorId);
                
                float distance = mActor.GetDistance(actorId);
                Vector3 direction =  transform.position - obj.transform.position;
                Quaternion toRotation = Quaternion.FromToRotation(transform.forward, direction);
                toRotation.x = 0;
                toRotation.z = 0;
                float rate = distance == 0 ? 0 : (1.0f/distance);
                rate = rate * 0.1f;
                transform.rotation = Quaternion.Lerp(transform.rotation, toRotation, rate);
            }
        }
        

        if(mAnimationContext.name.Length > 0 && mAnimator != null) {
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
                        if(!SetApproching())
                            throw new Exception("Set Approching Failure");
                        mState = STATE.APPROCHING;
                        mTimer = 0;
                    } else {
                        mTarget.Release();
                        StartTask(); 
                    }                    
                } else {
                    mAgent.destination = GetDestination();
                }            
            }
            break;   
            case STATE.APPROCHING:
                float rate = mApprochingContext.GetTimeRate(Time.deltaTime);
                transform.position = Vector3.Lerp(mApprochingContext.fromPosition, mApprochingContext.toPosition, rate);
                //transform.LookAt(mApprochingContext.toPosition);
                if(rate >= 1) {         
                    mTimer = 0;
                    mAnimationContext.Set(StopAnimation, 1, STATE_ANIMATION_CALLBACK.APPROCHING);
                    mState = STATE.INVALID;
                }
            break;            
        }        
    }
    private void StartTask() {
        if(!mAnimationContext.Set(StopAnimation, 1, STATE_ANIMATION_CALLBACK.START_TASK))
            throw new Exception("Animation Conext Failure");
        mState = STATE.INVALID;   
    }
    private void Stop() {
        mAnimationContext.Set(StopAnimation, 1, STATE_ANIMATION_CALLBACK.STOPPING);        
    }
    private void StopFinish() {
        if(mActor == null)
            throw new Exception("mActor or mAgent is null");

        mApprochingContext.Release();
        mTarget.Release();                
        if(mState != STATE.IDLE) {
            mTimer = 0;
        }
        mState = STATE.IDLE;        

        if(mIsActorReleaseAtStopFinish) {
            mIsActorReleaseAtStopFinish = false;
            mActor.ReleaseTask();
        }
    }
    private float GetDistance() {
        return Vector3.Distance(transform.position, GetDestination());
    }    
    private Vector3 GetDestination() {
        if(!mTarget.enable) {
            throw new Exception("Target must be enable");
        }
        if(mTarget.isPositionOnly)
            return mTarget.position;
        else if(mTarget.transform != null)
            return mTarget.transform.position;
        else 
            throw new Exception("Target Transform must be enable");
    }    
    public void SetAnimation(string animation) {     
        if(mGamePlayController != null && mAnimator != null) {
            int id = mGamePlayController.GetAnimationId(animation);   
            if((int)GamePlayController.ANIMATION_ID.Invalid < id) {            
                mAnimator.SetInteger(AnimationId, id);
            }
        }        
    }   
    private bool SetCurrentTaskAnimation(STATE_ANIMATION_CALLBACK state) {
        if(mActor == null)
            return false;
        var task = mActor.GetCurrentTask();
        if(task == null)
            return false;
        mAnimationContext.Set(task.GetAnimation(), task.mInfo.time, state);  
        return true;
    }
    private Actor? GetInteractionFromActor() {
        if(mActor == null)
            return null;
        var task = mActor.GetTaskContext();
        if(task == null || task.interactionFromActor == null)
            return null;
        return task.interactionFromActor;
    }
    private void LookAtCaller() {
        var fromObj = GetInteractionFromObject();
        if(fromObj != null) {
            transform.LookAt(fromObj.transform);
        }
    }
    private GameObject? GetInteractionFromObject() {
        if(mActor == null)
            return null;
        var task = mActor.GetTaskContext();
        if(task == null || task.interactionFromActor == null)
            return null;
        string from = task.interactionFromActor.mUniqueId;            
        return mGamePlayController.GetActorObject(from);
    }
    private bool SetApproching() {
        if(mActor == null || mAgent == null)
            return false;
        var task = mActor.GetCurrentTask();
        if(task == null)
            return false;

        Position lookAt = task.mInfo.target.lookAt;                        
        mApprochingContext.Set(transform, mTarget.position, new Vector3(lookAt.x, lookAt.y, lookAt.z), mAgent.acceleration);
        return true;

    }
    private string GetScript(Actor? targetActor, string taskId, bool isRefusal = false) {        
        if(mActor == null || taskId == string.Empty)
            throw new Exception("mActor or taskId is null");
        
        if(targetActor == null)
            return isRefusal ? ScriptHandler.Instance.GetScriptRefusal(taskId, mActor) : ScriptHandler.Instance.GetScript(taskId, mActor);
        else 
            return isRefusal ? ScriptHandler.Instance.GetScriptRefusal(taskId, mActor, targetActor): ScriptHandler.Instance.GetScript(taskId, mActor, targetActor);
    }
    public void SetMessage(string msg, bool isOverlap = true) {
        if(mUI != null && mActor != null) {
            mUI.SetMessage(msg, (int)CounterHandler.Instance.GetCount(), isOverlap);
        }
    }
    private void SetInteractionCameraAngle() {
        if(mActor == null)
            throw new Exception("mActor must exist");
        var task = mActor.GetCurrentTask();
        if(task == null)
            throw new Exception("Task must exist");
        switch(task.mInfo.target.interaction.type) {
            case TASK_INTERACTION_TYPE.ASK:
            case TASK_INTERACTION_TYPE.INTERRUPT:
                mGamePlayController.SetInteractionCameraAngle(this);
            break;
        }
    }
}
