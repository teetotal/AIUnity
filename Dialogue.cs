using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ENGINE.GAMEPLAY.MOTIVATION;

public class ScenarioNode {
    public float time;
    public SCENARIO_NODE_TYPE type;
    public void Init(float time, SCENARIO_NODE_TYPE type) {
        this.time = time;
        this.type = type;
    }
}
//ScenarioNode 인스턴스를 관리하기 위한 pool
public class ScenarioNodePool {
    private Stack<ScenarioNode> mPool = new Stack<ScenarioNode>();
    private static readonly Lazy<ScenarioNodePool> instance =
                        new Lazy<ScenarioNodePool>(() => new ScenarioNodePool());
    public static ScenarioNodePool Instance {
        get {
            return instance.Value;
        }
    }
    private ScenarioNodePool() { }
    public ScenarioNodePool(int initInstances = 3) {
        for(int i = 0; i < initInstances; i++) {
            ReleaeScenarioNode(AllocScenarioNode());
        }
    }    

    public ScenarioNode GetScenarioNode() {
        if(mPool.Count > 0)
            return mPool.Pop();
        
        return AllocScenarioNode();
    }
    public void ReleaeScenarioNode(ScenarioNode p) {
        mPool.Push(p);
    }

    private ScenarioNode AllocScenarioNode() {
        return new ScenarioNode();
    }
}

//Dialogue를 실행하는 본체
public class Dialogue {
    public string uniqueId { get; set; }
    private bool result;
    private StringBuilder sb = new StringBuilder();      
    private ActorController from;
    private ActorController to;
    private string taskId;
    private DateTime startTime;
    private Queue<ScenarioNode> scenarioFrom = new Queue<ScenarioNode>();
    private Queue<ScenarioNode> scenarioTo = new Queue<ScenarioNode>();
    private const string DEFAULT = "default";
    private const string SEP = "-";
    public void Init(ref ActorController from ,ref ActorController to, string taskId) {
        this.from = from;
        this.to = to;
        this.taskId = taskId;
        this.startTime = DateTime.Now;

        sb.Clear();
        sb.Append(taskId);
        sb.Append(SEP);        
        sb.Append(from.name);
        sb.Append(SEP);        
        sb.Append(to.name);
        this.uniqueId = sb.ToString();

        //시나리오 만들기
        ConfigScenario_Detail info = ScenarioInfoHandler.Instance.mInfo.ContainsKey(taskId) ? ScenarioInfoHandler.Instance.mInfo[taskId] : ScenarioInfoHandler.Instance.mInfo[DEFAULT];
        for(int i = 0; i < info.from.Count; i++) {
            ScenarioNode node = ScenarioNodePool.Instance.GetScenarioNode();
            node.Init(info.from[i].time, info.from[i].type);
            scenarioFrom.Enqueue(node);
        }

        for(int i = 0; i < info.to.Count; i++) {
            ScenarioNode node = ScenarioNodePool.Instance.GetScenarioNode();
            node.Init(info.to[i].time, info.to[i].type);
            scenarioTo.Enqueue(node);
        }

        //미리 결과를 알고 진행
        result = from.mActor.DoTaskBefore();        
    }
    public bool IsFinished() {
         if(scenarioFrom.Count == 0 && scenarioTo.Count == 0) {
            return true;   
        }
        return false;
    }
    public void Do() {
        double t = (DateTime.Now - startTime).TotalMilliseconds;
        //to
        if(scenarioTo.Count > 0 && t >= scenarioTo.Peek().time) {            
            Do(scenarioTo.Dequeue());            
        }

        //from
        if(scenarioFrom.Count > 0 &&  t >= scenarioFrom.Peek().time) {            
            Do(scenarioFrom.Dequeue());            
        }
    }

    private void Do(ScenarioNode node) {
        switch(node.type) {
            case SCENARIO_NODE_TYPE.INVALID:
            break;
            case SCENARIO_NODE_TYPE.SAY_FROM:
            from.SetMessage(from.GetScript(to.mActor, taskId));
            break;
            case SCENARIO_NODE_TYPE.ANIMATION_FROM:
            from.SetCurrentTaskAnimation(ActorController.STATE_ANIMATION_CALLBACK.DIALOGUE);
            break;
            case SCENARIO_NODE_TYPE.SAY_ANIMATION_FROM:
            from.SetCurrentTaskAnimation(ActorController.STATE_ANIMATION_CALLBACK.DIALOGUE);
            from.SetMessage(from.GetScript(to.mActor, taskId));
            break;
            case SCENARIO_NODE_TYPE.REACTION_FROM:
            from.mActor.DoTask(!result);
            if(!result) {
                from.SetAnimationContext(from.DisappointedAnimation, 1, ActorController.STATE_ANIMATION_CALLBACK.DISAPPOINTED_FINISH);
            }
            break;
            case SCENARIO_NODE_TYPE.SAY_TO:            
            break;
            case SCENARIO_NODE_TYPE.ANIMATION_TO:
            break;
            case SCENARIO_NODE_TYPE.SAY_ANIMATION_TO:
            break;
            case SCENARIO_NODE_TYPE.FEEDBACK_TO:
            {
                string feedbackTaskId = TaskHandler.Instance.GetTask(taskId).mInfo.target.interaction.taskId;
                to.mActor.SetCurrentTask(feedbackTaskId);                
                to.SetMessage(to.GetScript(from.mActor, feedbackTaskId, !result));        
                if(result)
                    to.SetCurrentTaskAnimation(ActorController.STATE_ANIMATION_CALLBACK.FINISH_TASK);
                else
                    to.mActor.CallCallback(Actor.CALLBACK_TYPE.REFUSAL);
            }
            
            break;
        }

        //pooling
        ScenarioNodePool.Instance.ReleaeScenarioNode(node);
    }
}
//Dialogue 인스턴스를 관리하기 위한 pool
public class DialoguePool {
    private Stack<Dialogue> mPool = new Stack<Dialogue>();
    public DialoguePool(int initInstances = 3) {
        for(int i = 0; i < initInstances; i++) {
            ReleaeDialogue(AllocDialogue());
        }
    }
    public Dialogue GetDialogue(ref ActorController from ,ref ActorController to, string taskId) {        
        Dialogue p = GetDialogue();
        p.Init(ref from, ref to, taskId);
        return p;
    }

    private Dialogue GetDialogue() {
        if(mPool.Count > 0)
            return mPool.Pop();
        
        return AllocDialogue();
    }
    public void ReleaeDialogue(Dialogue p) {
        mPool.Push(p);
    }

    private Dialogue AllocDialogue() {
        return new Dialogue();
    }
}
//ActorController 에서 호출하는 singleton
public class DialogueHandler { 
    private Dictionary<string, Dialogue> mDialogues = new Dictionary<string, Dialogue>();
    private Queue<Dialogue> mReleaseQ = new Queue<Dialogue>();
    private DialoguePool mDialoguePool = new DialoguePool();
    private static readonly Lazy<DialogueHandler> instance =
                        new Lazy<DialogueHandler>(() => new DialogueHandler());
    public static DialogueHandler Instance {
        get {
            return instance.Value;
        }
    }
    private DialogueHandler() { }
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
            mDialoguePool.ReleaeDialogue(p);
        }
    }
    public bool Handover(ref ActorController from ,ref ActorController to, string taskId) {
        Dialogue p = mDialoguePool.GetDialogue(ref from, ref to, taskId);
        mDialogues.Add(p.uniqueId, p);
        
        return true;
    }   
}