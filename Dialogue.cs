using System;
using System.Collections.Generic;
using System.Linq;

public enum SCENARIO_NODE_TYPE {
    INVALID = 0,
    SAY_FROM,
    ANIMATION_FROM,
    SAY_ANIMATION_FROM,
    REACTION_FROM,
    SAY_TO = 11,
    ANIMATION_TO,
    SAY_ANIMATION_TO,
    FEEDBACK_TO,
}
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
// ScenarioInfo ----------------------------------------------------------------------------
public class ScenarioInfo {
    public List<KeyValuePair<float, SCENARIO_NODE_TYPE>> from = new List<KeyValuePair<float, SCENARIO_NODE_TYPE>>();
    public List<KeyValuePair<float, SCENARIO_NODE_TYPE>> to = new List<KeyValuePair<float, SCENARIO_NODE_TYPE>>();
}
public class ScenarioInfoHandler {
    public Dictionary<string, ScenarioInfo> mInfo = new Dictionary<string, ScenarioInfo>();
    private static readonly Lazy<ScenarioInfoHandler> instance =
                        new Lazy<ScenarioInfoHandler>(() => new ScenarioInfoHandler());
    public static ScenarioInfoHandler Instance {
        get {
            return instance.Value;
        }
    }
    private ScenarioInfoHandler() { }
}
//Dialogue를 실행하는 본체
public class Dialogue {
    public string uniqueId { get; set; }
    private ActorController from;
    private ActorController to;
    private string taskId;
    private DateTime startTime;
    private Queue<ScenarioNode> scenarioFrom = new Queue<ScenarioNode>();
    private Queue<ScenarioNode> scenarioTo = new Queue<ScenarioNode>();
    private const string DEFAULT = "default";
    public void Init(ActorController from ,ActorController to, string taskId) {
        this.from = from;
        this.to = to;
        this.taskId = taskId;
        this.startTime = DateTime.Now;

        this.uniqueId = string.Format("{0}-{1}-{2}", taskId, from.name, to.name);

        //시나리오 만들기
        ScenarioInfo info = ScenarioInfoHandler.Instance.mInfo.ContainsKey(taskId) ? ScenarioInfoHandler.Instance.mInfo[taskId] : ScenarioInfoHandler.Instance.mInfo[DEFAULT];
        for(int i = 0; i < info.from.Count; i++) {
            ScenarioNode node = ScenarioNodePool.Instance.GetScenarioNode();
            node.Init(info.from[i].Key, info.from[i].Value);
            scenarioFrom.Enqueue(node);
        }

        for(int i = 0; i < info.to.Count; i++) {
            ScenarioNode node = ScenarioNodePool.Instance.GetScenarioNode();
            node.Init(info.to[i].Key, info.to[i].Value);
            scenarioFrom.Enqueue(node);
        }
    }
    public bool IsFinished() {
         if(scenarioFrom.Count == 0 && scenarioTo.Count == 0) {
            return true;   
        }
        return false;
    }
    public void Do() {
        double t = (DateTime.Now - startTime).TotalMilliseconds;
        //from
        if(t >= scenarioFrom.Peek().time) {            
            Do(true, scenarioFrom.Dequeue());
        }
        //to
        if(t >= scenarioTo.Peek().time) {            
            Do(false, scenarioTo.Dequeue());
        }
    }

    private void Do(bool isFrom, ScenarioNode node) {
        //loader구현하고
        //실행 구현하면 됨.

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
    public Dialogue GetDialogue(ActorController from ,ActorController to, string taskId) {        
        Dialogue p = GetDialogue();
        p.Init(from, to, taskId);
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
            string key = p.Key;

            dialogue.Do();
        }

        List<string> keys = mDialogues.Keys.ToList<string>();
        for(int i = 0; i < keys.Count; i++) {
            string key = keys[i];
            Dialogue dialogue = mDialogues[key];
            
            if(dialogue.IsFinished()) {
                mDialogues.Remove(key);
                mDialoguePool.ReleaeDialogue(dialogue);
            }
        }
    }
    public bool Handover(ActorController from ,ActorController to, string taskId) {
        Dialogue p = mDialoguePool.GetDialogue(from, to, taskId);
        mDialogues.Add(p.uniqueId, p);
        
        return true;
    }   
}