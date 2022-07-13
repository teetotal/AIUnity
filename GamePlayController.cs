using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ENGINE.GAMEPLAY;
using ENGINE.GAMEPLAY.MOTIVATION;
#nullable enable
public class GamePlayController : MonoBehaviour
{
    public enum ANIMATION_ID : int {
        Invalid = -5,
        Min,
        Disappointed,
        Levelup,
        Walk,
        Idle = 0, 
        Greeting, 
        Strong, 
        Bashful, 
        Digging, 
        Dancing,
        Drinking,
        Farming,
        Restrain,
        Headbang,
        HandUp,
        Sitting,
        Max
    }
    public float Interval = 2;
    //NPC아닌 actor type
    public int ManagedActorType = 1;
    //NPC아닌 actor의 최소 실행 주기
    public int ManagedInterval = 2;
    public GameObject? MainCamera;
    private float mTimer = 0;
    private Dictionary<string, Actor> mActors = new Dictionary<string, Actor>();
    private Dictionary<string, ActorController> mActorObjects = new Dictionary<string, ActorController>();    
    private ActorController? mFollowActorObject;
    public float VisibleDistance = 10.0f;
    public float VisibleDistanceBack = 3.0f;
    private Dictionary<string, int> mDicAnimation = new Dictionary<string, int>();    
    private TransparentObject? mTransparentObject;

    public string FollowActorId = string.Empty;
    private Hud HudInstance;
    
    // Start is called before the first frame update
    void Start()
    {
        HudInstance = this.GetComponent<Hud>();
        
        if(!Load()) {
            throw new System.Exception("Loading Failure");            
        }
        //Actor생성
        mActors = ActorHandler.Instance.GetActors();
        if(mActors == null) {
            throw new System.Exception("Loading Actor Failure");
        }
        if(!CreateActors()) {
            throw new System.Exception("Creating Actors Failure");
        }
        //Pet
        ActorHandler.Instance.SetPets();

        for(int i = (int)ANIMATION_ID.Min; i < (int)ANIMATION_ID.Max; i++ ) {
            mDicAnimation.Add(((ANIMATION_ID)i).ToString(), i);
        }

        if(MainCamera != null)
            mTransparentObject = MainCamera.GetComponent<TransparentObject>();
    }
   
    private void Update() {
        float deltaTime = Time.deltaTime;        
        DialogueHandler.Instance.Update(deltaTime);

        mTimer += deltaTime;
        if(mTimer > Interval) {
            Next();            
            mTimer = 0;
        }
        //Actor UI visible   
        if(mFollowActorObject == null) 
            return;    
        foreach(var actor in mActorObjects) {
            bool visible = true;
            if(FollowActorId != actor.Key) {
                Vector3 from = mFollowActorObject.gameObject.transform.position;
                Vector3 to = actor.Value.gameObject.transform.position;

                Vector3 diff = to - from;                            
                float distance = diff.magnitude;
                //Quaternion angle = Quaternion.LookRotation( diff.normalized );

                if(distance > VisibleDistance) visible = false;        
                //else if(distance > VisibleDistanceBack && angle.y < 0) visible = false;        
            }

            actor.Value.SetVisibleActorUI(visible);
        }        
    }    
    private void Next() {
        long counter = CounterHandler.Instance.Next();
        DischargeHandler.Instance.Discharge(ManagedActorType);
        ActorHandler.Instance.UpdateSatisfactionSum();

        foreach(var p in mActors) {
            Actor actor = mActors[p.Key];      
            if(actor.IsAutoTakeable()) {
                if(actor.mType == ManagedActorType && actor.GetTaskContext().lastCount > 0 && CounterHandler.Instance.GetCount() - actor.GetTaskContext().lastCount <= ManagedInterval) {                    
                    continue;
                }
                if(actor.Loop_TakeTask() == false) {
                    actor.Loop_Ready();
                }   
            }            
        }
    }
    public void SetInteractionCameraAngle(ActorController actor) {
        if(mTransparentObject == null)
            return;
        if(mTransparentObject.GetFollowingActorId() != actor.name) 
            return;
        mTransparentObject.SetInteractionAngle();
    }   
    public int GetAnimationId(string aniName) {
        if(mDicAnimation.ContainsKey(aniName)) {
            return mDicAnimation[aniName];
        }
        return (int)ANIMATION_ID.Invalid;
    }
    //잘못된 actorId에 대해서는 그냥 exception발생 시켜버림
    public GameObject GetActorObject(string actorId) {
        return mActorObjects[actorId].gameObject;
    }
    public ActorController GetActorController(string actorId) {
        return mActorObjects[actorId];
    }
    private bool CreateActors() {        
        foreach(var p in mActors) {
            string actorName = p.Key;
            Actor actor = p.Value;
            if(actor.position != null) {
                Vector3 position = new Vector3(actor.position.x, actor.position.y, actor.position.z);
                GameObject prefab = Resources.Load<GameObject>(actor.mInfo.prefab);
                if(prefab == null) 
                    continue;
                Quaternion rotation = Quaternion.identity;
                if(actor.mInfo.rotation != null) {
                    rotation = Quaternion.Euler(actor.mInfo.rotation[0], actor.mInfo.rotation[1], actor.mInfo.rotation[2]);
                }
                GameObject obj = Instantiate(prefab, position, rotation);
                
                
                ActorController actorController = obj.GetComponent<ActorController>();
                if(!actorController.Init(actorName, actor))
                    throw new System.Exception("ActorController Init Failure. " + actorName);

                actor.SetCallback(actorController.Callback);
                
                //나중에 캐릭터 생성에 대한 부분 처리할때 옮겨갈 코드
                if(actorName == FollowActorId) {
                    mFollowActorObject = actorController;
                    actorController.SetFollowingActor(true, HudInstance);
                }
                    
                mActorObjects.Add(actorName, actorController);
            }            
        }
        return true;
    }
    private bool Load() {
        var pLoader = new Loader();
        TextAsset szSatisfaction = Resources.Load<TextAsset>("Config/satisfactions");
        TextAsset szTask = Resources.Load<TextAsset>("Config/task");
        TextAsset szActor = Resources.Load<TextAsset>("Config/actors");
        TextAsset szItem = Resources.Load<TextAsset>("Config/item");
        TextAsset szLevel = Resources.Load<TextAsset>("Config/level");
        TextAsset szQuest = Resources.Load<TextAsset>("Config/quest");
        TextAsset szScript = Resources.Load<TextAsset>("Config/script");        
        TextAsset szScenario = Resources.Load<TextAsset>("Config/scenario");        

        if(!pLoader.Load(szSatisfaction.text, szTask.text, szActor.text, szItem.text, szLevel.text, szQuest.text, szScript.text, szScenario.text)) {
            Debug.Log("Failure Loading config");
            return false;
        }
        return true;
    }
}
