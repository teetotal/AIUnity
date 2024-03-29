using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ENGINE;
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
    public string animationName = string.Empty;
    public int count = 0;
    public int targetCount;
    public ActorController.STATE_ANIMATION_CALLBACK callbackState;
    private ActorController? mActorController = null;
    private DateTime lastTime;    
    public void Init(ActorController p) {
        mActorController = p;
    }
    public void Reset() {
        animationName = string.Empty;
        count = 0;        
    }
    public bool Set(string animationName, int targetCount, ActorController.STATE_ANIMATION_CALLBACK callbackState) {
        if(this.animationName.Length > 0 || mActorController == null)
            return false;
        this.animationName = animationName;
        this.targetCount = targetCount;
        this.callbackState = callbackState;
        mActorController.SetAnimation(this.animationName);
        lastTime = DateTime.Now;
        return true;
    }
    public bool Increase(float clipLength) {
        TimeSpan span = DateTime.Now - lastTime;
        if(span.TotalMilliseconds < clipLength * 1000)
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
    enum MOVING_STATE {        
        IDLE,        
        READY_MOVING,
        MOVING,
        STOP_MOVING,
        APPROCHING,        
        ANIMATION,
        ANIMATION_STOP,
        LOOKAT
    };
    public enum STATE_ANIMATION_CALLBACK {
        INVALID,
        DIALOGUE,
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
    public float ApprochRange = 1f;
    public string AnimationId = "AnimationId";
    public string StopAnimation = "Idle";
    public string DisappointedAnimation = "Disappointed";
    private string GameController = "Hud";
    //private string[] mAnimationIds = {"Idle", "Walk", "Greeting", "Strong", "Bashful", "Digging", "Levelup", "Dancing", "Drinking"};    
    private float mDefaultWaitTimeMin = 0.3f;

    //unity objects
    private GameObject? mUIObject;
    private BattleActorUI? mUI;
    private GamePlayController mGamePlayController = new GamePlayController();
    private NavMeshAgent mAgent;
    private Animator mAnimator;
    
    //variable
    private Hud? mHud = null;
    private bool mIsFollowingActor = false;
    private Target mTarget = new Target();
    private Transform mLookAtTarget;
    private ActorControllerApproching mApprochingContext = new ActorControllerApproching();    
    private float mTargetPositionRandom = 0;
    //private Queue<Actor.CALLBACK_TYPE> mCallbackQueue = new Queue<Actor.CALLBACK_TYPE>();    
    private MOVING_STATE mMovingState = MOVING_STATE.IDLE;
    private AnimationContext mAnimationContext = new AnimationContext(); 
    public Actor mActor;      
    private VehicleController? mVehicleController;
    private bool mLazyInitFlag = false;  
    
    public bool Init(string name, Actor actor) {
        if(name == string.Empty || actor == null)
            return false;
        this.mActor = actor;
        this.name = name;
        
        return true;
    }
    public void LazyInit() {
        var speed = mActor.GetSatisfaction("Speed");
        if(speed == null) {
            //speed가 없으면 그냥 기본 설정 speed를 사용한다.
            return;
        }
        SetSpeed(speed.Min, speed.Value);
        mLazyInitFlag = true;
    }
    void Start()
    {        
        mAnimationContext.Init(this);        
        mAnimator = GetComponent<Animator>();

        mAgent = gameObject.GetComponent<NavMeshAgent>();
        if(mAgent == null) {
            throw new Exception("Invalid NavMeshAgent");
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
            mUI.SetName(mActor.mInfo.nickname);
        }
        //Hud
        SetHudName();
        SetHudLevel();
        SetLevelProgress();
        /*
        SetHudVillageLevel();
        SetHudVillageProgression();
        */
    }
    public void SetSpeed(float speed, float acceleration) {
        mAgent.speed = speed;
        mAgent.acceleration = acceleration;
    }
    // Actor UI -------------------------------------------------------------
    public void SetVisibleActorUI(bool visible) {
        if(mUIObject != null)
            mUIObject.SetActive(visible);
    }

    // HUD ------------------------------------------------------------------
    public void SetFollowingActor(bool isFollowing, Hud hud) {
        mIsFollowingActor = isFollowing;
        mHud = hud;

        if(mIsFollowingActor && mHud != null) {
            //mHud.InitSatisfaction(mActor.GetSatisfactions());
            SetHudQuest();
        }
    }
    private void SetHudQuest() {
        if(mIsFollowingActor && mHud != null)
            mHud.SetQuest(mActor, mActor.GetQuest());
    }
    private void SetHudSatisfaction()
    {
        if(mIsFollowingActor && mHud != null)
            mHud.SetSatisfaction(mActor.GetSatisfactions());
    }
    private void SetHudName() {
        if(mIsFollowingActor && mHud != null)
            mHud.SetName(mActor.mInfo.nickname);
    } 
    /*
    private void SetHudVillageLevel() {
        if(mIsFollowingActor && mHud != null) {
            int level = ActorHandler.Instance.GetVillageLevel(mActor.mInfo.village);
            mHud.SetVillageLevel(level);
        }
    }
    private void SetHudVillageProgression() {
        if(mIsFollowingActor && mHud != null) {
            float v = ActorHandler.Instance.GetVillageProgression(mActor.mInfo.village);
            mHud.SetVillageLevelProgress(v);
        }
    }
    */
    private void SetHudLevel() {
        if(mIsFollowingActor && mHud != null) {
            var p = LevelHandler.Instance.Get(mActor.mType, mActor.level);
            if(p != null)
                mHud.SetLevel(p.title);
            SetLevelProgress();
            SetHudSatisfaction();
        }
    } 
    private void AddHudState(string sz) {
        if(mIsFollowingActor && mHud != null)
            mHud.SetState(sz);
    }
    private void ResetHudState() {
        if(mIsFollowingActor && mHud != null)
            mHud.SetState("...");
    } 
    private void SetLevelProgress() {
        if(mIsFollowingActor && mHud != null) {
            float v = mActor.GetLevelUpProgress();
            mHud.SetLevelProgress(v);
        }
    }    
    // ----------------------------------------------------------------------
    public void Callback(Actor.LOOP_STATE state, Actor actor) {     
        //if(actor.mUniqueId == "PET200-1")
        //    Debug.Log(string.Format("{0} {1}", state, actor.follower));   

        switch(state) {     
            case Actor.LOOP_STATE.INVALID:
            break;
            case Actor.LOOP_STATE.READY:
            /*
            if(mAgent != null && !mActor.mInfo.isFly)
                mAgent.ResetPath();
            */
            break;            
            case Actor.LOOP_STATE.CHANGE_SCENE: 
            mGamePlayController.ChangeScene(mActor, mActor.PopSceneName());
            break;
            case Actor.LOOP_STATE.TASK_UI:
            if(mIsFollowingActor && mHud != null) {
                mHud.SetTask(TaskHandler.Instance.GetTasks(actor, mGamePlayController.Village, false));
            }
            break;
            case Actor.LOOP_STATE.TAKE_TASK:           
            actor.Loop_Move();
            break;
            case Actor.LOOP_STATE.MOVE:
            {
                //이동 처리
                SetMoving();          
            }            
            break;            
            case Actor.LOOP_STATE.ANIMATION:
            {
                //Script
                SetMessage(GetScript(mActor.GetTargetActor(), mActor.GetCurrentTaskId()));
                //Animation.
                if(!SetCurrentTaskAnimation(STATE_ANIMATION_CALLBACK.TASK)) 
                    throw new Exception("SetCurrentTaskAnimation Failure. " + mActor.mUniqueId);                                
            }
            break;
            case Actor.LOOP_STATE.RESERVED:
            actor.Loop_LookAt();
            break;
            case Actor.LOOP_STATE.LOOKAT:            
            {                
                string targetActorId = actor.GetAsker().mUniqueId;
                var target = mGamePlayController.GetActorObject(targetActorId);
                if(target == null)
                    throw new Exception("Invalid target actor. " + targetActorId);
                //lookat
                mLookAtTarget = target.transform;   
                mMovingState = MOVING_STATE.LOOKAT;
            }
            break;
            case Actor.LOOP_STATE.DIALOGUE:
            {                
                var toActor = actor.GetTaskContext().GetTargetActor();
                if(toActor == null)
                    throw new Exception("null actor");
                var to = mGamePlayController.GetActorController(toActor.mUniqueId);
                DialogueHandler.Instance.Handover(this, to, actor.GetCurrentTaskId());
                mGamePlayController.SetInteractionCameraAngle(this);
            }            
            break;            
            case Actor.LOOP_STATE.SET_TASK:            
            actor.Loop_Move();
            break;
            case Actor.LOOP_STATE.DO_TASK:
            {
                //ResetHudState();
                //SetLevelProgress();
                SetHudSatisfaction();
                SetHudQuest();
                actor.Loop_Chain();
            }            
            break;
            case Actor.LOOP_STATE.AUTO_DO_TASK:
            {
                mMovingState = MOVING_STATE.IDLE;
                SetHudSatisfaction();
                //SetLevelProgress();
                SetHudQuest();
                actor.Loop_Chain();
            }            
            break;
            case Actor.LOOP_STATE.GET_IN_VEHICLE:
            {
                string[] targetArr = mActor.GetTaskContext().target.objectName.Split(':');
                string vehicle = targetArr[0];
                string destination = targetArr[1];

                GameObject target = GameObject.Find(vehicle);
                if(target == null) {        
                    throw new Exception("Invalid GameObject Name " +vehicle);
                }

                mVehicleController = target.GetComponent<VehicleController>();
                Vector3 dest = Util.StringToVector3(destination);
                if(mVehicleController.CheckDistance(dest)) {
                    mVehicleController.GetIn(this.gameObject, dest);
                } else {
                    AddHudState(L10nHandler.Instance.Get(L10nCode.GET_IN_VEHICLE_FAILURE));
                    VehicleHandler.Instance.CancelReserve(actor.mUniqueId);
                    actor.Loop_Release();
                }
            }
            break;
            case Actor.LOOP_STATE.GET_OFF_VEHICLE:
            {
                if(mVehicleController == null)
                    throw new Exception("mVehicleController must be assigned");
                mVehicleController.GetOff();
            }
            break;
            case Actor.LOOP_STATE.LEVELUP:
            {
                SetHudLevel();
                
                //levelup 모션 처리    
                mAnimationContext.Reset();  
                SetAnimationContext("Levelup", 1, STATE_ANIMATION_CALLBACK.LEVELUP);      
                SetMessage(L10nHandler.Instance.Get(L10nCode.LEVEL_UP));                
            }            
            break;
            case Actor.LOOP_STATE.REFUSAL:
            {
                SetAnimationContext(DisappointedAnimation, 1, ActorController.STATE_ANIMATION_CALLBACK.DISAPPOINTED);
            }
            break;            
            case Actor.LOOP_STATE.RELEASE:
            {
                SetHudLevel();
                mMovingState = MOVING_STATE.IDLE;
                actor.Loop_Ready();
            }            
            break;
            case Actor.LOOP_STATE.DISCHARGE:
            {                
                SetHudSatisfaction();
            }            
            break; 
            case Actor.LOOP_STATE.COMPLETE_QUEST:
            {
                SetHudQuest();
                SetHudSatisfaction();
            }            
            break;
            case Actor.LOOP_STATE.TAX_COLLECTION:

            break;
            case Actor.LOOP_STATE.ITEM:
            {
                if(mHud != null)
                    mHud.ObtainItem(mActor);
            }
            break;
            case Actor.LOOP_STATE.STOCK_SELL:
                AddHudState(L10nHandler.Instance.Get(L10nCode.STOCK_SELL));
            break;
            case Actor.LOOP_STATE.STOCK_BUY:
                AddHudState(L10nHandler.Instance.Get(L10nCode.STOCK_BUY));
            break;
            case Actor.LOOP_STATE.STOCK_CALCULATE:
                SetHudSatisfaction();
            break;
        }
    }
    private void SetMoving() {     
        Actor.TaskContext_Target p = mActor.GetTaskContext().target; 
        switch(p.type) {
            case Actor.TASKCONTEXT_TARGET_TYPE.INVALID:                
            break;
            case Actor.TASKCONTEXT_TARGET_TYPE.NON_TARGET:
            Arrive();
            break;
            case Actor.TASKCONTEXT_TARGET_TYPE.POSITION:      
            {
                if(p.position == null) {
                    //포지션 할당
                    Transform tf = GameObject.Find(p.objectName).transform;
                    p.position = new Position(tf.position.x, tf.position.y, tf.position.z);
                    p.lookAt = p.position;
                }
                mTarget.SetPostion(new Vector3(p.position.x, p.position.y, p.position.z));
                mTargetPositionRandom = ApprochRange;
                if(GetDistance() < mTargetPositionRandom) {
                    Arrive();
                } else {
                    mMovingState = MOVING_STATE.READY_MOVING;
                }
            }        
            break;
            case Actor.TASKCONTEXT_TARGET_TYPE.FLY: {
                if(p.position == null) {
                    //포지션 할당
                    Transform tf = GameObject.Find(p.objectName).transform;
                    p.position = new Position(tf.position.x, tf.position.y, tf.position.z);
                    p.lookAt = p.position;
                }

                mTarget.SetPostion(new Vector3(p.position.x, p.position.y, p.position.z));
                mTargetPositionRandom = ApprochRange;
                if(!SetApproching())
                    throw new Exception("SetApproching Failure");
                mMovingState = MOVING_STATE.APPROCHING;                  
            }
            break;
            default:
            { 
                GameObject target = GameObject.Find(p.objectName);
                if(target == null) {        
                    throw new Exception("Invalid GameObject Name " + p.objectName);
                }
                mTargetPositionRandom = UnityEngine.Random.Range(ApprochRange, ApprochRange * 2f);                          
                mTarget.SetTransform(target.transform);      
                float distance = GetDistance(); 
                if(distance < mTargetPositionRandom) {
                    Arrive();
                } else {
                    mMovingState = MOVING_STATE.READY_MOVING;
                }
            }
            break;
        }
        AddHudState(mActor.GetCurrentTaskTitle());
    }
    private void Arrive(){
        SetAnimation(StopAnimation);
        mMovingState = MOVING_STATE.IDLE;
        var task = mActor.GetCurrentTask();
        if(task == null) 
            throw new Exception("Null Task");

        switch(task.mInfo.target.interaction.type) {
            case TASK_INTERACTION_TYPE.ASK:
            case TASK_INTERACTION_TYPE.INTERRUPT:
            mActor.Loop_Dialogue();
            return;
            default:
            mActor.Loop_Animation();
            return;
        }            
    }    
    void CallbackAnimationFinish(STATE_ANIMATION_CALLBACK state) {
        switch(state) {
            case STATE_ANIMATION_CALLBACK.INVALID:            
            break;
            case STATE_ANIMATION_CALLBACK.DIALOGUE:
            SetAnimationContext(StopAnimation, 1, STATE_ANIMATION_CALLBACK.INVALID);
            break;            
            case STATE_ANIMATION_CALLBACK.TASK: 
            {                                
                mActor.Loop_DoTask();
            }
            break;            
            case STATE_ANIMATION_CALLBACK.LEVELUP: 
            {
                SetHudLevel();                  
                mActor.Loop_Release();
            }
            break;                      
            case STATE_ANIMATION_CALLBACK.DISAPPOINTED:
            mActor.Loop_Release();
            break;            
        }
    }
    // Update is called once per frame
    void Update()
    {        
        if(!mLazyInitFlag)
            LazyInit();

        mActor.SetPosition(transform.position.x, transform.position.y, transform.position.z);
        mActor.SetRotation(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);

        switch(mMovingState) {
            case MOVING_STATE.IDLE: 
            {
                if(mActor.HasPet()) {
                    var pet = mActor.GetDoingTaskPet();
                    if(pet == null) return;
                    double distance = mActor.GetDistanceToDoingPet();
                    if(distance < 4) {
                        mAgent.ResetPath();     
                        SetAnimation(StopAnimation);
                    } else {
                        GameObject petDoing = mGamePlayController.GetActorObject(pet.mUniqueId);
                        SetAnimation("Walk");
                        mAgent.destination = petDoing.transform.position;
                    }
                }
                //pet이 있고 pet 중 하나라도 Ready상태가 아니면 pet을 따라 다니는거 구현 해야함!.
                else if(mActor.follower) {
                    //master와 거리를 보고 행동
                    double distance = mActor.GetDistanceToMaster();
                    if(distance < 4) {
                        mAgent.ResetPath();   
                        SetAnimation(StopAnimation);
                    } else {
                        GameObject master = mGamePlayController.GetActorObject(mActor.GetMaster().mUniqueId);
                        SetAnimation("Walk");
                        mAgent.destination = master.transform.position;
                    }
                } else {
                    //주변의 actor를 쳐다 본다.
                    string actorId = mActor.LookAround();
                    if(actorId != string.Empty) {            
                        GameObject? obj = mGamePlayController.GetActorObject(actorId);
                        if(obj == null)
                            throw new Exception("Invalid ActorId. " + actorId);
                        transform.LookAt(obj.transform);
                    }
                }
            }
            break;
            case MOVING_STATE.ANIMATION: 
            {
                if(mAnimationContext.animationName.Length > 0 && mAnimator != null) {
                    AnimatorStateInfo aniInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

                    if( aniInfo.IsName(mAnimationContext.animationName) && 
                        aniInfo.normalizedTime >= 1f) 
                    {  
                        //앉는 자세 위치 고정을 위해
                        if(mApprochingContext.enable)        
                            mApprochingContext.Release();

                        if(mAnimationContext.Increase(aniInfo.length)) {
                            SetAnimation(StopAnimation);                            
                            mMovingState = MOVING_STATE.ANIMATION_STOP; 
                        }
                    } else if(mApprochingContext.enable) { //앉는 자세 위치를 정교하게 하기 위해
                        transform.position = mApprochingContext.toPosition;
                        transform.LookAt(mApprochingContext.lookAt);
                    }
                    return;
                } 
            }
            break;
            case MOVING_STATE.ANIMATION_STOP: 
            {
                if( mAnimator != null && 
                    mAnimator.GetCurrentAnimatorStateInfo(0).IsName(StopAnimation) && 
                    mAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f) 
                {  
                    STATE_ANIMATION_CALLBACK state = mAnimationContext.callbackState;
                    mMovingState = MOVING_STATE.IDLE; 
                    mAnimationContext.Reset();
                    CallbackAnimationFinish(state);
                }                
            }
            break;
            case MOVING_STATE.READY_MOVING:
            {
                if( mAnimator.GetCurrentAnimatorStateInfo(0).IsName(StopAnimation) && 
                    mAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f) {
                    mAgent.ResetPath();                    
                    mMovingState = MOVING_STATE.MOVING;
                    SetAnimation("Walk");
                }
            }
            break;
            case MOVING_STATE.MOVING:
            {
                if(GetDistance() <  mTargetPositionRandom) {   
                    mAgent.ResetPath();             
                    mAgent.isStopped = true;
                    
                    if(mTarget.isPositionOnly) {                        
                        if(!SetApproching())
                            throw new Exception("Set Approching Failure");
                        mMovingState = MOVING_STATE.APPROCHING;
                    } else {     
                        transform.LookAt(mTarget.transform); 
                        SetAnimation(StopAnimation);
                        mMovingState = MOVING_STATE.STOP_MOVING;
                    }                    
                } else {
                    mAgent.destination = GetDestination();
                }            
            }
            break;
            case MOVING_STATE.STOP_MOVING:
            {
                if( mAnimator.GetCurrentAnimatorStateInfo(0).IsName(StopAnimation) && 
                    mAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f) {
                                   
                    mTarget.Release();
                    mMovingState = MOVING_STATE.IDLE;
                    Arrive(); 
                }
            }
            break;
            case MOVING_STATE.APPROCHING:
            {
                float rate = mApprochingContext.GetTimeRate(Time.deltaTime);
                transform.position = Vector3.Lerp(mApprochingContext.fromPosition, mApprochingContext.toPosition, rate);
                transform.LookAt(mApprochingContext.toPosition);
                if(rate >= 1) {              
                    SetAnimation(StopAnimation);
                    mMovingState = MOVING_STATE.STOP_MOVING;
                }
            }
            break;
            case MOVING_STATE.LOOKAT:
            {
                transform.LookAt(mLookAtTarget);
            }
            break;
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
    public bool SetAnimationContext(string animation, int time, STATE_ANIMATION_CALLBACK state) {
        mMovingState = MOVING_STATE.ANIMATION;
        return mAnimationContext.Set(animation, time, state);
    }
    public bool SetCurrentTaskAnimation(STATE_ANIMATION_CALLBACK state) {        
        var task = mActor.GetCurrentTask();
        if(task == null)
            return false;
        
        return SetAnimationContext(task.GetAnimation(), task.mInfo.animationRepeatTime, state);
    }           
    private bool SetApproching() {
        var task = mActor.GetCurrentTask();
        if(task == null)
            return false;

        Position lookAt = task.mInfo.target.lookAt;                        
        mApprochingContext.Set(transform, mTarget.position, new Vector3(lookAt.x, lookAt.y, lookAt.z), mAgent.acceleration);
        return true;

    }
    public string GetScript(Actor? targetActor, string taskId, bool isRefusal = false) {        
        if(taskId == string.Empty)
            throw new Exception("mActor or taskId is null");
        
        if(targetActor == null)
            return isRefusal ? ScriptHandler.Instance.GetScriptRefusal(taskId, mActor) : ScriptHandler.Instance.GetScript(taskId, mActor);
        else 
            return isRefusal ? ScriptHandler.Instance.GetScriptRefusal(taskId, mActor, targetActor): ScriptHandler.Instance.GetScript(taskId, mActor, targetActor);
    }
    public void SetMessage(string msg, bool isOverlap = true) {
        if(mUI != null) {
            mUI.SetMessage(msg, isOverlap);
        }
    }    
    
    public void OnArriveVehicle() {
        mActor.Loop_Chain();
    }
    public void OnGetOffVehicle() {
        mVehicleController = null;
        mActor.Loop_Chain();
    }
}
