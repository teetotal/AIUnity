using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ENGINE.GAMEPLAY;
using ENGINE.GAMEPLAY.MOTIVATION;
#nullable enable
public class GamePlayController : MonoBehaviour
{
    public enum ANIMATION_ID : int {
        Invalid = -6,
        Min,
        Laziness,
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
        DancingGroup,
        Max
    }
    public string Village = "village1";
    public float Interval = 2;
    //NPC아닌 actor type
    public int ManagedActorType = 1;
    private float mTimer = 0;
    private Dictionary<string, Actor> mActors = new Dictionary<string, Actor>();
    private Dictionary<string, ActorController> mActorObjects = new Dictionary<string, ActorController>();    
    private Dictionary<string, VehicleController> mVehicles = new Dictionary<string, VehicleController>();
    private ActorController? mFollowActorObject;
    public float VisibleDistance = 10.0f;
    public float VisibleDistanceBack = 3.0f;
    private Dictionary<string, int> mDicAnimation = new Dictionary<string, int>();    
    public TransparentObject TransparentInstance;

    public string FollowActorId = string.Empty;
    private Hud HudInstance;
    private const string L10N_TAX_COLLECTION = "TAX_COLLECTION";
    private const string L10N_UPDATE_MARKET_PRICE = "UPDATE_MARKET_PRICE";
    
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

        ActorHandler.Instance.PostInit(VillageLevelUpCallback);

        for(int i = (int)ANIMATION_ID.Min; i < (int)ANIMATION_ID.Max; i++ ) {
            mDicAnimation.Add(((ANIMATION_ID)i).ToString(), i);
        }

        //set village to follower
        ActorHandler.Instance.GetActor(FollowActorId).SetVillage(Village);

        //Vehicle
        VehicleHandler.Instance.Init(FnHangAround, Village);
        CreateVehicles();

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
        //counting
        long counter = CounterHandler.Instance.Next();
        //discharge
        DischargeHandler.Instance.Discharge(ManagedActorType);
        //tax collection
        if(ActorHandler.Instance.TaxCollection()) {
            //update
            SetHudVillage();
        }
        //update market price
        if(SatisfactionMarketPrice.Instance.Update()) {
            if(HudInstance != null)
                HudInstance.SetState(L10nHandler.Instance.Get(L10N_UPDATE_MARKET_PRICE));
        }
        //vehicle
        VehicleHandler.Instance.Update();

        foreach(var p in mActors) {
            Actor actor = mActors[p.Key];   
            if(actor.IsAutoTakeable()) {
                double duration = CounterHandler.Instance.GetCount() - actor.GetTaskContext().lastCount;
                var actorController = GetActorController(p.Key);
                if( //actor.mType == ManagedActorType && 
                    actor.GetTaskContext().lastCount > 0 && 
                    duration <= actor.mInfo.laziness //check laziness
                ) {        
                    //두리번 거리는 animation 실행 하게 해야함.    
                    actorController.SetAnimation("Laziness");
                    continue;
                } else {
                    actorController.SetAnimation("Idle");
                }
                
                if(!HudInstance.IsAuto() && FollowActorId == p.Key) {
                    actor.Loop_TaskUI();
                } else if(actor.Loop_TakeTask() == false) {
                    actor.Loop_Ready();
                }   
            }            
        }
    }
    private void VillageLevelUpCallback(string villageId, int level) {
        //Debug.Log(string.Format("VillageLevelUp {0}, {1}", villageId, level));
        SetHudVillage();
    }
    private void SetHudVillage() {
        string actorId = TransparentInstance.GetFollowingActorId();
        Actor actor = ActorHandler.Instance.GetActor(actorId);
        string village = actor.mInfo.village;

        string name = ActorHandler.Instance.GetVillageInfo(village).name;
        int level = ActorHandler.Instance.GetVillageLevel(village);
        float v = ActorHandler.Instance.GetVillageProgression(village);

        HudInstance.SetVillageName(name);
        HudInstance.SetVillageLevel(level);
        HudInstance.SetVillageLevelProgress(v);

        HudInstance.SetState(L10nHandler.Instance.Get(L10N_TAX_COLLECTION));
    }
    public void SetInteractionCameraAngle(ActorController actor) {
        if(TransparentInstance == null)
            return;
        if(TransparentInstance.GetFollowingActorId() != actor.name) 
            return;
        TransparentInstance.SetInteractionAngle();
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
            if(p.Value.mInfo.village != string.Empty && p.Value.mInfo.village != Village)
                continue;

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
    public ActorController? GetFollowActor() {
        return mFollowActorObject;
    }
    //Vehicle -------------------------------------------------------------------------
    private void CreateVehicles() {
        var vehs = VehicleHandler.Instance.GetAll(Village);
        for(int i = 0; i < vehs.Count; i++) {
            GameObject obj = Instantiate(Resources.Load<GameObject>(vehs[i].prefab), 
                        GetPostionFromString(vehs[i].positions[0].position), 
                        GetRotationFromString(vehs[i].positions[0].rotation)
                    );
            obj.name = vehs[i].vehicleId;
            mVehicles.Add(vehs[i].vehicleId, obj.GetComponent<VehicleController>()); 
        }
    } 
    private bool FnHangAround(string vehicleId, string position, string rotation) {
        Vector3 dest = GetPostionFromString(position);
        if(mVehicles[vehicleId].CheckDistance(dest)) {
            mVehicles[vehicleId].SetDestination(dest);
            return true;
        }
        else {
            return false;
        }
    }
    private Vector3 GetPostionFromString(string sz) {
        string[] szPosition = sz.Split(',');
        return new Vector3(float.Parse(szPosition[0]),float.Parse(szPosition[1]), float.Parse(szPosition[2]));
    }
    private Quaternion GetRotationFromString(string sz) {
        string[] szRotation = sz.Split(',');
        return Quaternion.Euler(float.Parse(szRotation[0]), float.Parse(szRotation[1]), float.Parse(szRotation[2]));
    }
    //Scene ---------------------------------------------------------------------------
    public void ChangeScene(Actor actor, string scene) {
        var followActor = GetFollowActor();
        if(followActor != null && followActor.mActor.mUniqueId == actor.mUniqueId) {
            StartCoroutine(LoadAsyncScene(scene));
        }
    }
    IEnumerator LoadAsyncScene(string scene)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene);

        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            yield return null;
            Debug.Log(asyncLoad.progress);
        }
    }
    private bool Load() {
        if(Loader.Instance.mInitialized)
            return true;

        TextAsset szSatisfaction = Resources.Load<TextAsset>("Config/satisfactions");
        TextAsset szTask = Resources.Load<TextAsset>("Config/task");
        TextAsset szActor = Resources.Load<TextAsset>("Config/actors");
        TextAsset szItem = Resources.Load<TextAsset>("Config/item");
        TextAsset szLevel = Resources.Load<TextAsset>("Config/level");
        TextAsset szQuest = Resources.Load<TextAsset>("Config/quest");
        TextAsset szScript = Resources.Load<TextAsset>("Config/script");        
        TextAsset szScenario = Resources.Load<TextAsset>("Config/scenario");   
        TextAsset szVillage = Resources.Load<TextAsset>("Config/village");  
        TextAsset szL10n = Resources.Load<TextAsset>("Config/l10n");   
        TextAsset szVehicle = Resources.Load<TextAsset>("Config/vehicle");          

        if(!Loader.Instance.Load(   szSatisfaction.text, 
                                    szTask.text, 
                                    szActor.text, 
                                    szItem.text, 
                                    szLevel.text, 
                                    szQuest.text, 
                                    szScript.text, 
                                    szScenario.text, 
                                    szVillage.text, 
                                    szL10n.text,
                                    szVehicle.text
                                )) {
            Debug.Log("Failure Loading config");
            return false;
        }
        return true;
    }
}
