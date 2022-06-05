using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ENGINE.GAMEPLAY.MOTIVATION;

public class ActorController : MonoBehaviour
{    
    enum STATE {
        IDLE,
        MOVING,
        TASK,
    };
    public float RandomRange = 5;
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
    private FnTask? mCurrTask = null;
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
                if(GetDistance() < RandomRange) {
                    Debug.Log("Arrived " + name);
                    SetAnimation(StopAnimation);
                    mTargetTransform = null;
                    mState = STATE.TASK;
                    mTaskTime = 0;
                    mCurrTask = mGamePlayController.GetTask(name);
                    SetAnimation(mCurrTask.GetAnimation());
                    
                } else {
                    mAgent.destination = GetDestination();
                }            
            }
            break;
            case STATE.TASK: {
                mTaskTime += Time.deltaTime;
                if(mCurrTask.mInfo.time < mTaskTime) {
                    mGamePlayController.DoTask(name);
                    mCurrTask = null;
                    SetAnimation(StopAnimation);
                    mState = STATE.IDLE;
                    Debug.Log(name + " Task Finished");
                }
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
        Debug.Log(name + "SetAnimation " + animation);
        if(mDicAnimation.ContainsKey(animation)) {
            int id = mDicAnimation[animation];
            mAnimator.SetInteger("AnimationId", id);
        }
    }
    public void MoveTo(string targetObject) {
        //Debug.Log("MoveTo " + targetObject);
        GameObject target = GameObject.Find(targetObject);
        if(target != null) {
            mTargetTransform = target.transform;
            mAgent.destination = mTargetTransform.position;
            mState = STATE.MOVING;
            SetAnimation("Walk");
        }
    }
}
