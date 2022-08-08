using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class VehicleController : MonoBehaviour
{
    private Animator mAnimator;
    private NavMeshAgent mNavMeshAgent;
    private Vector3 mDestination;
    private Transform mTransform;
    private GameObject mActor;
    private bool mIsIdle = true;
    private float mInitDistance;
    private const string ANIMATION_KEY = "Idle";

    // Start is called before the first frame update
    void Start()
    {
        mTransform = this.gameObject.transform;
        mAnimator = this.gameObject.GetComponent<Animator>();
        mNavMeshAgent = this.gameObject.GetComponent<NavMeshAgent>();
        mNavMeshAgent.ResetPath();
    }

    // Update is called once per frame
    void Update()
    {
        if(!mIsIdle) {
            float dist = Vector3.Distance(transform.position, mDestination); 
            if (dist < mInitDistance && mNavMeshAgent.remainingDistance == 0) { //Arrived.
                mIsIdle = true;
                mNavMeshAgent.ResetPath();
                mAnimator.SetBool(ANIMATION_KEY, mIsIdle);
                mActor.GetComponent<ActorController>().OnArriveVehicle();
            } 
        }
    }
    private bool SetInitDistance(Vector3 target) {
        mInitDistance = Vector3.Distance(transform.position, target); 
        if(mInitDistance < 8) {
            return false;
        }
        return true;
    }
    public void SetDestination(Vector3 target) {
        mIsIdle = false;
        mDestination = target;

        mNavMeshAgent.destination = mDestination;
        mAnimator.SetBool(ANIMATION_KEY, mIsIdle);
    }

    public bool GetIn(GameObject actor, Vector3 target) {
        if(!SetInitDistance(target)) 
            return false;
        mActor = actor;
        mActor.GetComponent<NavMeshAgent>().enabled = false;
        mActor.transform.position = mTransform.position;
        mActor.transform.SetParent(mTransform);
        mActor.SetActive(false);        
        SetDestination(target);
        return true;
    }
    public void GetOff() {
        mActor.SetActive(true);
        mActor.GetComponent<NavMeshAgent>().enabled = true;
        mActor.transform.parent = null;
        mActor.transform.position = mTransform.position + new Vector3(1,0,1);
        mActor.GetComponent<ActorController>().OnGetOffVehicle();
        mActor = null; 
    }
}
