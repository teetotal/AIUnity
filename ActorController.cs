using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ENGINE.GAMEPLAY.MOTIVATION;
/*
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
*/
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
    private string[] mAnimationIds = {"Idle", "Walk", "Greeting", "Strong", "Bashful", "Digging", "Levelup"};
    //private float mDefaultWaitTime = 2.5f; 
    private float mDefaultWaitTimeMin = 0.3f;

    //unity objects
    private GameObject mUIObject;
    private BattleActorUI mUI;
    private GamePlayController mGamePlayController;
    private Transform mTargetTransform;
    private float mTargetPositionRandom = 0;
    private NavMeshAgent mAgent;
    private Animator mAnimator;
    
    //variable
    private Queue<Actor.CALLBACK_TYPE> mCallbackQueue = new Queue<Actor.CALLBACK_TYPE>();
    private Dictionary<string, int> mDicAnimation = new Dictionary<string, int>();    
    private STATE mState = STATE.INVALID;
    private AnimationContext mAnimationContext = new AnimationContext(); 
    private float mTimer = 0;
    private Actor mActor;
    
    void Start()
    {        
        mAnimationContext.Init(this);
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
    public void Callback(Actor.CALLBACK_TYPE type) {
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
                var p = mActor.GetTaskContext().target;
                if(p == null) return;
                GameObject target = GameObject.Find(p.Item2);
                if(target != null) {          
                    mTargetPositionRandom = UnityEngine.Random.Range(ApprochRange, ApprochRange * 1.5f);  
                    mTargetTransform = target.transform;            
                    mState = STATE.READY_MOVING;
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
            if(target.Item1) {
                mUI.SetMessage(ScriptHandler.Instance.GetScript(mActor.GetTaskContext().currentTask.mTaskId, name, target.Item2) );
            }
            mActor.DoTaskBefore();
            break;
            case STATE_ANIMATION_CALLBACK.TASK:
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
                    mTargetTransform = null;
                    mAgent.isStopped = true;
                    //SetAnimation(StopAnimation);
                    //mState = STATE.START_TASK;   
                    if(!mAnimationContext.Set(StopAnimation, 1, STATE_ANIMATION_CALLBACK.START_TASK))
                        Debug.Log("Animation Conext Failure");
                    mState = STATE.INVALID;   
                } else {
                    mAgent.destination = mTargetTransform.position;
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
    private void Stop() {
        SetAnimation(StopAnimation); 
        if(mState != STATE.IDLE) {
            mTimer = 0;
        }
        mState = STATE.IDLE;        
    }
    private float GetDistance() {
        float distance = Vector3.Distance(transform.position, mTargetTransform.position);
        return distance;        
    }    
    public void SetAnimation(string animation) {
        //Debug.Log(name + "SetAnimation " + animation);
        if(mDicAnimation.ContainsKey(animation)) {
            int id = mDicAnimation[animation];
            mAnimator.SetInteger(AnimationId, id);
        }
    }    
}
