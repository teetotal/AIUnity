using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ENGINE.GAMEPLAY;
using ENGINE.GAMEPLAY.MOTIVATION;
//#nullable enable
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
        Plant,
        Harvest,
        Tillage,
        Max
    }
    public bool HideActors = false;
    public string Village = "village1";
    public float Interval = 2;
    //NPC아닌 actor type
    public int ManagedActorType = 1;
    private float mTimer = 0;
    private Dictionary<string, Actor> mActors = new Dictionary<string, Actor>();
    private Dictionary<string, ActorController> mActorObjects = new Dictionary<string, ActorController>();    
    private Dictionary<string, VehicleController> mVehicles = new Dictionary<string, VehicleController>();
    //Farming
    private Dictionary<string, GameObject> mFarmingObjects = new Dictionary<string, GameObject>();
    public float VisibleDistance = 10.0f;
    public float VisibleDistanceBack = 3.0f;
    private Dictionary<string, int> mDicAnimation = new Dictionary<string, int>();    
    public TransparentObject TransparentInstance;

    public string FollowActorId = string.Empty;
    public Actor FollowActor;
    private ActorController mFollowActorObject;
    private Hud HudInstance;
    
    // Start is called before the first frame update
    private void Awake() {
        if(!Load()) {
            throw new System.Exception("Loading Failure");            
        }
    }
    void Start()
    {
        HudInstance = this.GetComponent<Hud>();

        ActorHandler.Instance.PostInit(VillageLevelUpCallback);

        for(int i = (int)ANIMATION_ID.Min; i < (int)ANIMATION_ID.Max; i++ ) {
            mDicAnimation.Add(((ANIMATION_ID)i).ToString(), i);
        }
        //Vehicle
        VehicleHandler.Instance.Init(FnHangAround, Village);
        CreateVehicles();

        //Farming
        var farms = FarmingHandler.Instance.GetFarms(Village);
        if(farms != null)
            CreateFarms(farms);
        FarmingHandler.Instance.SetCallback(OnFarmingCallback);

        //set village to follower
        var actor = ActorHandler.Instance.GetActor(FollowActorId);
        if(actor == null)
            throw new Exception("GamePlayController. Invalid FollwActorId");

        FollowActor = actor;
        FollowActor.SetVillage(Village);

        //set hud village
        SetHudVillage();
        //set hud actor 
        HudInstance.SetName(FollowActor.mInfo.nickname);
        HudInstance.SetLevel(LevelHandler.Instance.Get(FollowActor.mType, FollowActor.level).title);
        HudInstance.SetLevelProgress(FollowActor.GetLevelUpProgress());
        HudInstance.InitSatisfaction(FollowActor.GetSatisfactions());

        if(HideActors)
            return;

        //Actor생성
        mActors = ActorHandler.Instance.GetActors(Village);
        if(!mActors.ContainsKey(FollowActor.mUniqueId))
            mActors.Add(FollowActor.mUniqueId, FollowActor); //follow actor 추가
        if(mActors == null) {
            throw new System.Exception("Loading Actor Failure");
        }

        if(!CreateActors()) {
            throw new System.Exception("Creating Actors Failure");
        }
        //Init Dialog
        DialogueHandler.Instance.Init(HudInstance, this); 
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
        ActorHandler.Instance.TaxCollection();
        //vehicle
        VehicleHandler.Instance.Update();
        //farming
        FarmingHandler.Instance.Update(Village);
        //Stock
        StockMarketHandler.Instance.Update();

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
                    if(!HudInstance.HideTask)
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
        string name = ActorHandler.Instance.GetVillageInfo(Village).name;
        int level = ActorHandler.Instance.GetVillageLevel(Village);
        float v = ActorHandler.Instance.GetVillageProgression(Village);

        HudInstance.SetVillageName(name);
        HudInstance.SetVillageLevel(level);
        HudInstance.SetVillageLevelProgress(v);

        HudInstance.SetState(L10nHandler.Instance.Get(L10nCode.TAX_COLLECTION));
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
            string actorName = p.Key;
            Actor actor = p.Value;
            if(actor.position != null) {
                Vector3 position = new Vector3(actor.position.x, actor.position.y, actor.position.z);
                Quaternion rotation = Quaternion.Euler(actor.rotation.x, actor.rotation.y, actor.rotation.z);
                GameObject prefab = Resources.Load<GameObject>(actor.mInfo.prefab);
                if(prefab == null) 
                    continue;

                GameObject obj = Instantiate(prefab, position, rotation);
                
                ActorController actorController = obj.GetComponent<ActorController>();
                if(!actorController.Init(actorName, actor))
                    throw new System.Exception("ActorController Init Failure. " + actorName);

                actor.SetCallback(actorController.Callback);
                //scene을 돌아왔을때 하던걸 완료 할 수 없으니 다 리셋시켜 버린다.
                actor.Loop_Release();
                actor.GetTaskContext().lastCount = 0;
                
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
    #nullable enable
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
    //Farming -------------------------------------------------------------------------
    private void CreateFarms(List<ConfigFarming_Detail> p) {
        for(int i = 0; i < p.Count; i++) {
            ConfigFarming_Detail farm = p[i];
            string[] position_rotation = farm.position.Split(':');
            var obj = Util.CreateObjectFromPrefab(farm.prefab, Util.StringToVector3(position_rotation[0]), Util.StringToVector3(position_rotation[1]));
            obj.name = farm.farmId;
        }
    }
    private void OnFarmingCallback(FarmingHandler.CALLBACK_TYPE type, string farmId, string fieldId, string seedId) {
        ConfigFarming_Seed seed = FarmingHandler.Instance.GetSeedInfo(seedId);
        switch(type) {
            case FarmingHandler.CALLBACK_TYPE.PLANT: {
                GameObject obj = Util.CreateChildObjectFromPrefab(seed.prefabPlant, fieldId);
                mFarmingObjects[fieldId] = obj;
                break;
            }
            case FarmingHandler.CALLBACK_TYPE.COMPLETE: {
                mFarmingObjects[fieldId].transform.parent = null;
                Destroy(mFarmingObjects[fieldId]);
                mFarmingObjects.Remove(fieldId);
                GameObject obj = Util.CreateChildObjectFromPrefab(seed.prefabIngredient, fieldId);
                mFarmingObjects[fieldId] = obj;
                break;
            }
            case FarmingHandler.CALLBACK_TYPE.HARVEST: {
                mFarmingObjects[fieldId].transform.parent = null;
                Destroy(mFarmingObjects[fieldId]);
                mFarmingObjects.Remove(fieldId);
                break;
            }
            default:
                break;
        }
        
    }
    //Scene ---------------------------------------------------------------------------
    public void ChangeScene(Actor actor, string scene) {
        var followActor = GetFollowActor();
        if(followActor != null && followActor.mActor.mUniqueId == actor.mUniqueId) {
            LoadingScene.LoadScene(scene);
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
        TextAsset szFarming = Resources.Load<TextAsset>("Config/farming");       
        TextAsset szSeed = Resources.Load<TextAsset>("Config/seed");   
        TextAsset szStockMarket = Resources.Load<TextAsset>("Config/stockmarket");          

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
                                    szVehicle.text,
                                    szFarming.text,
                                    szSeed.text,
                                    szStockMarket.text
                                )) {
            Debug.Log("Failure Loading config");
            return false;
        }
        return true;
    }
}
