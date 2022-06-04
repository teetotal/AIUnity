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
    private Dictionary<string, Actor> mActors;
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
            if(mActorTask[p.Key] == null) {
                Actor actor = mActors[p.Key];
                string? taskid = actor.GetTaskId();
                if(taskid == null) 
                    continue;             
                var task = TaskHandler.Instance.GetTask(taskid);
                if(task == null) 
                    continue;   
                mActorTask[p.Key] = task;
                Debug.Log(string.Format("{0}, {1}", p.Key, task.mTaskTitle));
                //task.DoTask(actor);
            }
        }
    }
    private bool CreateActors() {        
        GameObject prefab = Resources.Load<GameObject>("Actors/actor1");
        if(prefab == null) 
            return false;

        int nCount = 0;
        foreach(var p in mActors) {
            string actorName = p.Key;
            //95,0,135 - 10,0,135
            Vector3 position = new Vector3(90 - (5 * nCount), 0, 132.5f);
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
