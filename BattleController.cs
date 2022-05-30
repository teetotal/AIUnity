using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ENGINE.GAMEPLAY.MOTIVATION;

[System.Serializable]
 public class TileX
 {
     public Transform[] X;
 }
public class BattleController : MonoBehaviour
{
    public float Interval;
    public TileX[] mTilesY;    
    //actor id, obj
    private Dictionary<string, GameObject> mMappingTable = new Dictionary<string, GameObject>();    
    private Dictionary<string, GameObject> mActorUI = new Dictionary<string, GameObject>();
    private Battle mBattle;
    // Start is called before the first frame update
    void Start()
    {
        if(!Load()) {
            return;
        }

        //map
        /*
        int[,] map =
        {
            {0,      0,      0,      0,      0},
            {1,      0,      0,      0,      1},
            {2,      0,      0,      0,      2},
            {2,      0,      0,      0,      2},
            {1,      0,      0,      0,      1},
            {0,      0,      0,      0,      0}
        };
        */
        int[,] map = new int[mTilesY.Length, mTilesY[0].X.Length];        

        mBattle = new Battle(mTilesY[0].X.Length, mTilesY.Length);
        if(!mBattle.Init(map, map)) {            
            Debug.Log("Map init failure");
            return;
        }
        //recources경로로 복사해서 써야함. assets에서는 접근이 안됨
        BattleActorAbility abilityForward = new BattleActorAbility();
        abilityForward.HP = 9;
        abilityForward.AttackStyle = BattleActorAbility.ATTACK_STYLE.ATTACKER;
        abilityForward.AttackPower = 1;
        abilityForward.AttackDistance = 1;        
        abilityForward.AttackAccuracy = 1;
        abilityForward.Sight = 3;        
        abilityForward.Speed = 3;
        abilityForward.MoveForward = 3f;
        abilityForward.MoveBack = 0;
        abilityForward.MoveSide = 0;

        BattleActorAbility abilityBack = new BattleActorAbility();
        abilityBack.HP = 9;
        abilityBack.AttackStyle = BattleActorAbility.ATTACK_STYLE.ATTACKER;
        abilityBack.AttackPower = 1;
        abilityBack.AttackDistance = 1;   
        abilityBack.AttackAccuracy = 1;     
        abilityBack.Sight = 3;        
        abilityBack.Speed = 1;
        abilityBack.MoveForward = 0;
        abilityBack.MoveBack = 1.5f;
        abilityBack.MoveSide = 0;

        BattleActorAbility abilitySide = new BattleActorAbility();
        abilitySide.HP = 9;
        abilitySide.AttackStyle = BattleActorAbility.ATTACK_STYLE.ATTACKER;
        abilitySide.AttackPower = 1;
        abilitySide.AttackDistance = 1;      
        abilitySide.AttackAccuracy = 1;  
        abilitySide.Sight = 3;        
        abilitySide.Speed = 1;
        abilitySide.MoveForward = 0;
        abilitySide.MoveBack = 0;
        abilitySide.MoveSide = 1.5f;

        //GaemObject랑 연결
        
        string[] names = {"hf", "hb", "hs", "af", "ab", "as"};
        Vector2Int[] positions = {
            new Vector2Int(0, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, 2),

            new Vector2Int(2, 3),
            new Vector2Int(2, 2),
            new Vector2Int(2, 1)
        };
        BattleActorAbility[] abilities = {
            abilityForward,
            abilityBack,
            abilitySide,
            abilityForward,
            abilityBack,
            abilitySide
        };
        
        /*
        string[] names = {"hf", "af"};
        Vector2Int[] positions = {
            new Vector2Int(2, 2),
            new Vector2Int(3, 2)            
        };
        BattleActorAbility[] abilities = {
            abilityForward,            
            abilityForward
        };
        */

        for(int i=0; i < names.Length; i++) {
            string actorName = names[i];
            Vector2Int pos = positions[i];

            GameObject prefab;
            if(actorName[0] == 'h')
                prefab = Resources.Load<GameObject>("Characters/Character_Dummy_Female_01");
            else 
                prefab = Resources.Load<GameObject>("Characters/Character_Dummy_Male_01");

            if(prefab == null) {
                Debug.Log("Prefab loading failure");
                return;
            }
            Vector3 dest = mTilesY[pos.y].X[pos.x].transform.position;
            dest = new Vector3(dest.x - 2.5f, dest.y, dest.z - 2.5f);
            
            GameObject obj = Instantiate<GameObject>(prefab, dest, Quaternion.identity);
            obj.name = actorName;
            Debug.Log(string.Format("{0} {1} {2}", actorName, dest.x, dest.z));

            var actor = ActorHandler.Instance.GetActor(actorName);
            if(actor == null) {
                Debug.Log("Actor loading failure");
                return;
            }
            if(!mBattle.AppendActor(pos.x, pos.y, actor, actorName[0] == 'h' ? BATTLE_SIDE.HOME: BATTLE_SIDE.AWAY, abilities[i])) {
                Debug.Log("Fail Append Actor " + actorName);
            }

            mMappingTable.Add(actorName, obj);

            //ui
            GameObject ui = GameObject.Find("ActorUI_" + actorName.ToUpper());
            if(ui == null) {
                break;
            }            
            ActorUI p = ui.GetComponent<ActorUI>();
            p.SetName(actorName);
            mActorUI.Add(actorName, ui);
        }

        if(!mBattle.Validate()) {
            Debug.Log("Invalid");
            return;
        }  
    }
    float delta = 0;
    private void FixedUpdate() {
        delta += Time.deltaTime;
        if(delta > Interval) {
            //Occupy();
            Next();            
            delta = 0;
        }        
    }
    public void Occupy(string actorId) {
        mBattle.Occupy(actorId);
    }
    /*
    void Occupy()
    {
        foreach(var p in mMappingTable) {
            if(p.Value.activeSelf == false) continue;
            string actorName = p.Key;
            GameObject obj = p.Value;
            var agent = obj.GetComponent<NavMeshAgent>();
            var animator = obj.GetComponent<Animator>();

            if(agent != null && agent.remainingDistance < 0.1f) {
                agent.destination = obj.transform.position;
                mBattle.Occupy(actorName);
            } 
        }
        return;
    }
    */
    void Next() {
        Dictionary<string, BattleActorAction> next = mBattle.Next();
        foreach(var p in next) {
            string actorId = p.Key;            
            var battleActor = mBattle.GetBattleActor(actorId);            
            BattleActorAction action = p.Value;
            GameObject actor = mMappingTable[actorId];
            if(battleActor == null || actor == null) {
                continue;
            }
            GameObject actorTarget;
            var actorController = actor.GetComponent<ActorController>();
            var agent = actor.GetComponent<NavMeshAgent>();
            

            switch(action.Type) {
                case BATTLE_ACTOR_ACTION_TYPE.NONE:
                case BATTLE_ACTOR_ACTION_TYPE.READY_ATTACKING:
                case BATTLE_ACTOR_ACTION_TYPE.READY_ATTACKED:
                actorController.SetIdle();
                break;
                case BATTLE_ACTOR_ACTION_TYPE.MOVING:
                int[] position = mBattle.mMap.GetPositionInt(action.TargetPosition);
                
                Vector3 toVec3 = mTilesY[position[1]].X[position[0]].transform.position;
                Vector3 dest = new Vector3(toVec3.x - 2.5f, toVec3.y, toVec3.z - 2.5f);            
                //agent.destination = dest;
                actorController.SetWalk(dest, battleActor.mAbility.Speed);
                Debug.Log(string.Format("{0} Moving {1} > {2}", action.Counter, actorId, dest));
                break;
                case BATTLE_ACTOR_ACTION_TYPE.ATTACKED:
                float remain = mBattle.Attacked(actorId, action);     
                float hpRatio = mBattle.GetHPRatio(actorId);
                float currHP = mBattle.GetHP(actorId);
                
                //actor.transform.position = agent.destination;               
                
                if(currHP <= 0) {
                    //삭제                    
                    actorController.SetDie();                    
                    mActorUI[actorId].SetActive(false);
                } else {
                    actorController.SetAttacked();                    
                    SetHP(actorId, hpRatio);
                    SetMessage(actorId, string.Format("{0} {1}({2})", action.Counter, action.TargetActorId, action.AttackAmount));
                }
                //Debug.Log(string.Format("{0} Attacked {1}({2}) > {3}({4})",action.Counter, action.TargetActorId, action.FromPosition, actorId, action.TargetPosition));
                break;
                case BATTLE_ACTOR_ACTION_TYPE.ATTACKING:
                actorTarget = mMappingTable[action.TargetActorId];
                actor.transform.LookAt(actorTarget.transform);                
                SetMessage(actorId, string.Format("{0} {1}({2})", action.Counter, action.TargetActorId, action.AttackAmount));
                actorController.SetAttack();
                //Debug.Log(string.Format("{0} Attacking {1} > {2}", action.Counter, actorId, action.TargetActorId));
                break;
            }
        }
    }
    void SetHP(string actorId, float amount) {
        ActorUI actorUI = mActorUI[actorId].GetComponent<ActorUI>();
        actorUI.SetHP(amount);
    }
    void SetMessage(string actorId, string message) {
        ActorUI actorUI = mActorUI[actorId].GetComponent<ActorUI>();
        actorUI.SetMessage(message);
    }
    bool Load() {
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
