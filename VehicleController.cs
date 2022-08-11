using UnityEngine;
using UnityEngine.AI;
using ENGINE.GAMEPLAY.MOTIVATION;
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
            Debug.Log(string.Format("{0} / {1} {2}", dist, mInitDistance, mNavMeshAgent.remainingDistance));
            if (dist < mInitDistance && mNavMeshAgent.remainingDistance == 0) { //Arrived.
                mIsIdle = true;
                mNavMeshAgent.ResetPath();
                mAnimator.SetBool(ANIMATION_KEY, mIsIdle);
                
                if(mActor != null)
                    mActor.GetComponent<ActorController>().OnArriveVehicle();

                VehicleHandler.Instance.SetMoving(this.gameObject.name, false);
            } 
        }
    }
    public bool CheckDistance(Vector3 target) {
        if(Vector3.Distance(gameObject.transform.position, target) < 10) {
            return false;
        }
        return true;
    }
    public void SetDestination(Vector3 target) {
        mIsIdle = false;
        mDestination = target;
        mInitDistance = Vector3.Distance(transform.position, mDestination); 

        mNavMeshAgent.destination = mDestination;
        mAnimator.SetBool(ANIMATION_KEY, mIsIdle);

        VehicleHandler.Instance.SetMoving(this.gameObject.name, true);
    }

    public void GetIn(GameObject actor, Vector3 target) {
        mActor = actor;
        mActor.GetComponent<NavMeshAgent>().enabled = false;
        mActor.transform.position = mTransform.position;
        mActor.transform.SetParent(mTransform);
        mActor.SetActive(false);        
        SetDestination(target);
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
