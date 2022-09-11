using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ENGINE.GAMEPLAY.MOTIVATION;
using ENGINE;

public class ScenarioNode {
    public float time;
    public SCENARIO_NODE_TYPE type;
    public void Init(float time, SCENARIO_NODE_TYPE type) {
        this.time = time;
        this.type = type;
    }
}
//ScenarioNode 인스턴스를 관리하기 위한 pool
public class ScenarioNodePool : Singleton<ScenarioNodePool> {
    private ObjectPool<ScenarioNode> mPool = new ObjectPool<ScenarioNode>();
    public ScenarioNodePool() { }
    public ObjectPool<ScenarioNode> GetPool() {
        return mPool;
    }
}
//Dialogue를 실행하는 본체
public class Dialogue {
    public string uniqueId { get; set; }
    private bool pause = false;
    private bool result;
    private StringBuilder sb = new StringBuilder();      
    public ActorController from;
    public ActorController to;
    public string taskId, feedbackTaskId;
    public FnTask task, taskFeedback;
    private DateTime lastTime;
    private Queue<ScenarioNode> scenario = new Queue<ScenarioNode>();
    private const string DEFAULT = "default";
    private const string SEP = "-";
    public void Init(ActorController from ,ActorController to, string taskId) {
        this.from = from;
        this.to = to;
        this.taskId = taskId;
        var fnTask = TaskHandler.Instance.GetTask(this.taskId); 
        if(fnTask == null)
            throw new Exception("Invalid Task. " + taskId);
        this.task = fnTask;
        this.feedbackTaskId = TaskHandler.Instance.GetTask(taskId).mInfo.target.interaction.taskId;
        var fnTaskFeedback = TaskHandler.Instance.GetTask(this.feedbackTaskId); 
        if(fnTaskFeedback == null)
            throw new Exception("Invalid Task. " + this.feedbackTaskId);
        taskFeedback = fnTaskFeedback;
        this.lastTime = DateTime.Now;

        sb.Clear();
        sb.Append(taskId);
        sb.Append(SEP);        
        sb.Append(from.name);
        sb.Append(SEP);        
        sb.Append(to.name);
        this.uniqueId = sb.ToString();

        //시나리오 만들기
        List<ConfigScenario_Node> info = ScenarioInfoHandler.Instance.mInfo.ContainsKey(taskId) ? ScenarioInfoHandler.Instance.mInfo[taskId] : ScenarioInfoHandler.Instance.mInfo[DEFAULT];
        for(int i = 0; i < info.Count; i++) {
            ScenarioNode node = ScenarioNodePool.Instance.GetPool().Alloc();
            node.Init(info[i].time, info[i].type);
            scenario.Enqueue(node);
        }
        if(DialogueHandler.Instance.GetGamePlayController().FollowActorId == to.mActor.mUniqueId) {
            DialogueHandler.Instance.GetHud().ReleaseTask();
        }
    }
    public bool IsFinished() {
         if(scenario.Count == 0) {
            return true;   
        }
        return false;
    }
    public void Do() {
        if(pause) 
            return;

        double t = (DateTime.Now - lastTime).TotalMilliseconds;
        if(scenario.Count > 0 && t >= scenario.Peek().time) {            
            Do(scenario.Dequeue());    
            lastTime = DateTime.Now;        
        }
    }
    public void Accept() {
        pause = false;
        result = true;
        Feedback();
    }
    public void Decline() {
        pause = false;
        result = false;
        Feedback();
    }

    private void Do(ScenarioNode node) {
        //Debug.Log(DateTime.Now.ToLongTimeString() + "- " + node.time.ToString() + " " + node.type.ToString() + " " + from.mActor.mUniqueId + "/" + to.mActor.mUniqueId);
        switch(node.type) {
            case SCENARIO_NODE_TYPE.FROM_STOP:
            {
                from.SetAnimation(from.StopAnimation);
            }
            break;
            case SCENARIO_NODE_TYPE.FROM_SAY:
            {
                from.SetMessage(from.GetScript(to.mActor, taskId));
            }            
            break;
            case SCENARIO_NODE_TYPE.FROM_ANIMATION:            
            {
                from.SetAnimation(this.task.mInfo.animation);
            }            
            break;
            case SCENARIO_NODE_TYPE.FROM_SAY_ANIMATION:
            {
                from.SetAnimation(this.task.mInfo.animation);
                from.SetMessage(from.GetScript(to.mActor, taskId));
            }            
            break;
            case SCENARIO_NODE_TYPE.FROM_REACTION:            
            {
                if(result) {
                    from.mActor.Loop_DoTask();
                } else {
                    from.mActor.Loop_Refusal();
                }
            }                                   
            break;      
            case SCENARIO_NODE_TYPE.TO_STOP:
            {
                to.SetAnimation(to.StopAnimation);
            }                  
            break;
            case SCENARIO_NODE_TYPE.TO_FEEDBACK:
            {     
                //Hud task가 열린 상태에서의 문제 해결해야 함
                if(DialogueHandler.Instance.GetGamePlayController().FollowActorId == to.mActor.mUniqueId) {
                    pause = true;
                    DialogueHandler.Instance.GetHud().OpenAsk(this); 
                } else {
                    result = to.mActor.Loop_Decide();
                    Feedback();
                }
            }            
            break;
            case SCENARIO_NODE_TYPE.TO_DECIDE:    
            {
                if(result)
                    to.mActor.Loop_AutoDoTask(feedbackTaskId);
            }                           
            break;
            case SCENARIO_NODE_TYPE.TO_RELEASE:
            {
                to.mActor.Loop_Release();
            }
            break;
        }

        //pooling
        ScenarioNodePool.Instance.GetPool().Release(node);
    }    
    private void Feedback() {
        to.SetMessage(to.GetScript(from.mActor, feedbackTaskId, !result));
        if(result)
            to.SetAnimation(taskFeedback.mInfo.animation);
    }
}
//ActorController 에서 호출하는 singleton
public class DialogueHandler { 
    private Dictionary<string, Dialogue> mDialogues = new Dictionary<string, Dialogue>();
    private Queue<Dialogue> mReleaseQ = new Queue<Dialogue>();
    private ObjectPool<Dialogue> mDialoguePool = new ObjectPool<Dialogue>();
    private Hud mHud;
    private GamePlayController mGamePlayController;
    private static readonly Lazy<DialogueHandler> instance =
                        new Lazy<DialogueHandler>(() => new DialogueHandler());
    public static DialogueHandler Instance {
        get {
            return instance.Value;
        }
    }
    private DialogueHandler() { }
    public void Init(Hud hudInstance, GamePlayController gamePlayController) {
        mHud = hudInstance;
        mGamePlayController = gamePlayController;
    }
    public GamePlayController GetGamePlayController() {
        return mGamePlayController;
    }
    public void Update(float deltaTime) {        
        foreach(var p in mDialogues) {
            Dialogue dialogue = p.Value;

            dialogue.Do();
            if(dialogue.IsFinished()) {
                mReleaseQ.Enqueue(dialogue);
            }
        }
        
        while(mReleaseQ.Count > 0) {
            var p = mReleaseQ.Dequeue();
            mDialogues.Remove(p.uniqueId);
            mDialoguePool.Release(p);
        }
    }
    public void Handover(ActorController from ,ActorController to, string taskId) {        
        Dialogue p = mDialoguePool.Alloc();
        p.Init(from, to, taskId);
        mDialogues.Add(p.uniqueId, p);
    }   
    public Hud GetHud() {
        return mHud;
    }
}