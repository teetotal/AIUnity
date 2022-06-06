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
    private Dictionary<string, FnTask?> mActorTask = new Dictionary<string, FnTask?>();
    // Start is called before the first frame update
    void Start()
    {
        if(!Load()) {
            Debug.Log("Loading Failure");
            return;
        }
        //Actor생성
        mActors = ActorHandler.Instance.GetActors(1);
        if(mActors == null) {
            Debug.Log("Loading Actor Failure");
            return;
        }
        if(!CreateActors()) {
            Debug.Log("Creating Actors Failure");
            return;
        }
    }

    // Update is called once per frame
    
    private void FixedUpdate() {
        mTimer += Time.deltaTime;
        if(mTimer > Interval) {
            Next();            
            mTimer = 0;
        }        
    }
    public void Ack(string AckActorId, string fromActorId, FnTask task) {
        if(AckActorId == "김밥이") {
            int i = 0;
            i++;
        }
        mActorObjects[AckActorId].GetComponent<ActorController>().Ack(task, fromActorId);
    }
    private void Next() {
        long counter = CounterHandler.Instance.Next();
        foreach(var p in mActors) {
            if(mActorTask.ContainsKey(p.Key) && mActorTask[p.Key] == null) {
                Actor actor = mActors[p.Key];
                //Reserved상태면 대기
                if(actor.GetReserve()) 
                    continue;
                
                var actorObject = mActorObjects[p.Key];
                ActorController actorController = actorObject.GetComponent<ActorController>();            
                
                if(actor.TakeTask() == false || actor.mTaskTarget == null || actor.mCurrentTask == null) {
                    Debug.Log(actor.mUniqueId + " Taking task failure");
                    continue;            
                }
                    
                FnTask task = actor.mCurrentTask;       
                //target object                
                if(actor.mTaskTarget.Item2.Length == 0) {
                    //바로 실행
                    if(actor.DoTask()) {
                        //animation                        
                        actorController.SetAnimation(task.GetAnimation());                        
                    }
                } else {
                    mActorTask[p.Key] = task;
                    //쫒아가기
                    actorController.MoveTo(actor.mTaskTarget.Item2);
                }
                Debug.Log(string.Format("{0}, {1}", p.Key, task.mTaskTitle));
            }
        }
    }
    public FnTask? GetTask(string actorId) {
        if(mActorTask.ContainsKey(actorId) == false) {
            return null;
        }
        return mActorTask[actorId];
    }
    public GameObject? GetActorObject(string actorId) {
        if(mActorObjects.ContainsKey(actorId)) {
            return mActorObjects[actorId];
        }
        return null;
    }
    public bool DoTask(string actorId) {
        if(mActors.ContainsKey(actorId) == false || mActorTask.ContainsKey(actorId) == false || mActorTask[actorId] == null) {
            return false;
        }        
        mActorTask[actorId] = null;

        if(!mActors[actorId].DoTask()) 
            return false;
        
        //UI update
        
        return true;
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
                mActorObjects.Add(actorName, obj);
                mActorTask.Add(actorName, null);
            }            
        }
        return true;
    }
    private bool Load() {
        var pLoader = new Loader();
        TextAsset szSatisfaction = Resources.Load<TextAsset>("Config/satisfactions");
        TextAsset szActor = Resources.Load<TextAsset>("Config/actors");
        TextAsset szItem = Resources.Load<TextAsset>("Config/item");
        TextAsset szLevel = Resources.Load<TextAsset>("Config/level");
        TextAsset szQuest = Resources.Load<TextAsset>("Config/quest");
        

        if(!pLoader.Load(szSatisfaction.text, szActor.text, szItem.text, szLevel.text, szQuest.text)) {
            Debug.Log("Failure Loading config");
            return false;
        }
        return true;
    }
}
