using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ActorController : MonoBehaviour
{    
    public string StopAnimation = "Stop";
    public string GameController = "GamePlay";
    private GamePlayController mGamePlayController;
    private Transform mTargetTransform;
    private NavMeshAgent mAgent;
    // Start is called before the first frame update
    void Start()
    {
        mAgent = gameObject.GetComponent<NavMeshAgent>();
        if(mAgent == null) {
            Debug.Log("Invalid NavMeshAgent");
        }
        mGamePlayController = GameObject.Find(GameController).GetComponent<GamePlayController>();
    }

    // Update is called once per frame
    void Update()
    {
        if(mTargetTransform != null) {
            if(mAgent.remainingDistance < 0.2) {
                SetAnimation(StopAnimation);
                mTargetTransform = null;
                mGamePlayController.DoTask(name);
            } else {
                mAgent.destination = mTargetTransform.position;
            }            
        }        
    }
    public void SetAnimation(string animation) {
        Debug.Log("SetAnimation " + animation);
    }
    public void MoveTo(string targetObject) {
        Debug.Log("MoveTo " + targetObject);
        GameObject target = GameObject.Find(targetObject);
        if(target != null) {
            mTargetTransform = target.transform;
            mAgent.destination = mTargetTransform.position;
        }
    }
}
