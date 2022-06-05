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
    private void Next() {
        long counter = CounterHandler.Instance.Next();
        foreach(var p in mActors) {
            if(mActorTask.ContainsKey(p.Key) && mActorTask[p.Key] == null) {
                Actor actor = mActors[p.Key];
                
                var actorObject = mActorObjects[p.Key];
                ActorController actorController = actorObject.GetComponent<ActorController>();
            
                var task = TaskHandler.Instance.GetTask(actor.GetTaskId());
                if(task == null) 
                    continue;                   
                //target object
                string targetObject = task.GetTargetObject(actor);
                if(targetObject.Length == 0) {
                    //바로 실행
                    if(task.DoTask(actor)) {
                        //animation                        
                        actorController.SetAnimation(task.GetAnimation());                        
                    }
                } else {
                    mActorTask[p.Key] = task;
                    //쫒아가기
                    actorController.MoveTo(targetObject);
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
    public bool DoTask(string actorId) {
        if(mActors.ContainsKey(actorId) == false || mActorTask.ContainsKey(actorId) == false || mActorTask[actorId] == null) {
            return false;
        }
        var task = mActorTask[actorId];
        mActorTask[actorId] = null;

        if(!task.DoTask(mActors[actorId])) 
            return false;
        
        //UI update
        
        return true;
    }
    private bool CreateActors() {        
        int nCount = 0;
        foreach(var p in mActors) {
            string actorName = p.Key;
            Actor actor = p.Value;
            //95,0,135 - 10,0,135
            Vector3 position = new Vector3(-20 - (5 * nCount), 0, -122.5f);
            GameObject prefab = Resources.Load<GameObject>(actor.prefab);
            if(prefab == null) 
                continue;
            GameObject obj = Instantiate(prefab, position, Quaternion.identity);
            obj.name = actorName;
            mActorObjects.Add(actorName, obj);
            mActorTask.Add(actorName, null);
            nCount ++;
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
