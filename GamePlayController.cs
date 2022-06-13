using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ENGINE.GAMEPLAY;
using ENGINE.GAMEPLAY.MOTIVATION;
#nullable enable
public class GamePlayController : MonoBehaviour
{
    public enum ANIMATION_ID : int {
        Invalid = -4,
        Min = -3,
        Levelup = -2,
        Walk = -1,
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
    public float Interval = 3;
    private float mTimer = 0;
    private Dictionary<string, Actor> mActors = new Dictionary<string, Actor>();
    private Dictionary<string, GameObject> mActorObjects = new Dictionary<string, GameObject>();    
    private Dictionary<string, int> mDicAnimation = new Dictionary<string, int>();    
    // Start is called before the first frame update
    void Start()
    {
        if(!Load()) {
            Debug.Log("Loading Failure");
            return;
        }
        //Actor생성
        mActors = ActorHandler.Instance.GetActors();
        if(mActors == null) {
            Debug.Log("Loading Actor Failure");
            return;
        }
        if(!CreateActors()) {
            Debug.Log("Creating Actors Failure");
            return;
        }

        for(int i = (int)ANIMATION_ID.Min; i < (int)ANIMATION_ID.Max; i++ ) {
            mDicAnimation.Add(((ANIMATION_ID)i).ToString(), i);
        }
    }
   
    private void Update() {
        mTimer += Time.deltaTime;
        if(mTimer > Interval) {
            Next();            
            mTimer = 0;
        }        
    }    
    private void Next() {
        long counter = CounterHandler.Instance.Next();
        foreach(var p in mActors) {
            Actor actor = mActors[p.Key];
            if(actor.GetState() == Actor.STATE.READY && actor.TakeTask() == false) {                    
                throw new System.Exception(p.Key + " Take Task Failure");
            }
        }
    }
    public int GetAnimationId(string aniName) {
        if(mDicAnimation.ContainsKey(aniName)) {
            return mDicAnimation[aniName];
        }
        return (int)ANIMATION_ID.Invalid;
    }
    public GameObject? GetActorObject(string actorId) {
        if(mActorObjects.ContainsKey(actorId)) {
            return mActorObjects[actorId];
        }
        return null;
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
                obj.name = actorName;
                actor.SetCallback(obj.GetComponent<ActorController>().Callback);
                mActorObjects.Add(actorName, obj);
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

        if(!pLoader.Load(szSatisfaction.text, szTask.text, szActor.text, szItem.text, szLevel.text, szQuest.text, szScript.text)) {
            Debug.Log("Failure Loading config");
            return false;
        }
        return true;
    }
}
