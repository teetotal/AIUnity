using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ENGINE.GAMEPLAY;
using ENGINE.GAMEPLAY.MOTIVATION;
#nullable enable
public class GamePlayController : MonoBehaviour
{
    public float Interval = 3;
    private float mTimer = 0;
    private Dictionary<string, Actor> mActors = new Dictionary<string, Actor>();
    private Dictionary<string, GameObject> mActorObjects = new Dictionary<string, GameObject>();    
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
                Debug.Log(p.Key + " Take Task Failure");
                continue;            
            }
        }
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
                GameObject prefab = Resources.Load<GameObject>(actor.prefab);
                if(prefab == null) 
                    continue;
                GameObject obj = Instantiate(prefab, position, Quaternion.identity);
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
