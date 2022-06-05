using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ENGINE.GAMEPLAY.MOTIVATION;
public class ActorControllerApproching {
    public float timer { get; set; }
    public Vector3 to { get; set; }
    public Vector3 from { get; set; }
    public Quaternion toQuaternion { get; set; }
    public Quaternion fromQuaternion { get; set; }
    public float distance { get; set; }
    public float time { get; set; }
    public float speed { get; set; }    
    public ActorControllerApproching(Vector3 from, Vector3 to, float speed, Quaternion fromQ) {
        this.from = from;
        this.to = to;
        this.speed = speed;
        this.timer = 0;
        this.fromQuaternion = fromQ;        
        this.toQuaternion = Quaternion.LookRotation(to - from);
        SetDistance();
        SetTime();
    }
    public float GetTimeRate(float deltaTime) {
        timer += deltaTime;
        return Mathf.Min(1, timer/time);
    }
    private void SetDistance() {
        distance = Vector3.Distance(from, to);
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
        MOVING,
        APPROCHING,
        START_TASK,
        TASK,
        FINISH_TASK
    };
    public float RandomRange = 1;
    public string AnimationId = "AnimationId";
    public string StopAnimation = "Idle";
    public string GameController = "GamePlay";
    private GamePlayController mGamePlayController;
    private Transform mTargetTransform;
    private NavMeshAgent mAgent;
    private string[] mAnimationIds = {"Idle", "Walk", "Greeting", "Strong", "Bashful", "Digging"};
    private Dictionary<string, int> mDicAnimation = new Dictionary<string, int>();
    private Animator mAnimator;
    private STATE mState = STATE.IDLE;
    private float mTaskTime = 0;
    private float mTaskFinishTime = 0;
    private FnTask? mCurrTask = null;
    private ActorControllerApproching? mApprochingContext;
    void Start()
    {
        for(int i =0; i < mAnimationIds.Length; i++ ) {
            mDicAnimation.Add(mAnimationIds[i], i);
        }
        mAnimator = GetComponent<Animator>();

        mAgent = gameObject.GetComponent<NavMeshAgent>();
        if(mAgent == null) {
            Debug.Log("Invalid NavMeshAgent");
        }
        mGamePlayController = GameObject.Find(GameController).GetComponent<GamePlayController>();
    }

    // Update is called once per frame
    void Update()
    {
        switch(mState) {
            case STATE.IDLE:
            break;
            case STATE.MOVING:
            {
                if(GetDistance() < RandomRange * 2) {                    
                    mState = STATE.APPROCHING;
                    mApprochingContext = new ActorControllerApproching(transform.position, GetDestination(), 2, transform.rotation);
                    mTargetTransform = null;
                    mAgent.isStopped = true;
                } else {
                    mAgent.destination = mTargetTransform.position;
                }            
            }
            break;
            case STATE.APPROCHING: {
                float rate = mApprochingContext.GetTimeRate(Time.deltaTime);
                transform.position = Vector3.Lerp(mApprochingContext.from, mApprochingContext.to, rate);
                transform.rotation = Quaternion.Lerp(mApprochingContext.fromQuaternion, mApprochingContext.toQuaternion, rate * 4);
                if(rate >= 1) {
                    //Debug.Log(name + " APPROCHED");
                    mApprochingContext = null;
                    mState = STATE.START_TASK;                    
                    SetAnimation(StopAnimation);                                        
                }
            }
            break;
            case STATE.START_TASK: {
                mTaskTime = 0;
                mCurrTask = mGamePlayController.GetTask(name);
                SetAnimation(mCurrTask.GetAnimation());
                mState = STATE.TASK;
            }
            break;
            case STATE.TASK: {
                mTaskTime += Time.deltaTime;
                if(mCurrTask.mInfo.time < mTaskTime) {                                        
                    SetAnimation(StopAnimation);
                    mState = STATE.FINISH_TASK;
                    mTaskFinishTime = 0;
                    //Debug.Log(name + " Task Finished");
                }
            }
            break;
            case STATE.FINISH_TASK:
            mTaskFinishTime += Time.deltaTime;
            if(mTaskFinishTime > 1.5)  {
                mCurrTask = null;
                mGamePlayController.DoTask(name);
                mState = STATE.IDLE;
            }            
            break;
        }        
    }
    private float GetDistance() {
        float distance = Vector3.Distance(transform.position, mTargetTransform.position);
        return distance;        
    }
    private Vector3 GetDestination() {
        Vector3 position = mTargetTransform.position;        
        return new Vector3(position.x + Random.Range(-RandomRange, RandomRange), position.y, position.z + Random.Range(-RandomRange, RandomRange));
    }
    public void SetAnimation(string animation) {
        //Debug.Log(name + "SetAnimation " + animation);
        if(mDicAnimation.ContainsKey(animation)) {
            int id = mDicAnimation[animation];
            mAnimator.SetInteger(AnimationId, id);
        }
    }
    public void MoveTo(string targetObject) {        
        GameObject target = GameObject.Find(targetObject);
        if(target != null) {            
            mTargetTransform = target.transform;            
            mAgent.ResetPath();
            mAgent.destination = GetDestination();                        
            Debug.Log(name + " MoveTo " + targetObject + mAgent.destination.ToString());
            mState = STATE.MOVING;
            SetAnimation("Walk");
        }
    }
}
