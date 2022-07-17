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
    [SerializeField]
    private float Interval;
    [SerializeField]
    private TileX[] mTilesY;    
    //actor id, obj
    private Dictionary<string, GameObject> mMappingTable = new Dictionary<string, GameObject>();    
    //private Dictionary<string, GameObject> mActorUI = new Dictionary<string, GameObject>();
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
        
        string[] names = {"hf", "hb", "hs", "h1", "a1", "af", "ab", "as"};
        Vector2Int[] positions = {
            new Vector2Int(0, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, 2),
            new Vector2Int(0, 3),

            new Vector2Int(2, 3),
            new Vector2Int(2, 2),
            new Vector2Int(2, 1),
            new Vector2Int(2, 0)
        };
        BattleActorAbility[] abilities = {
            abilityForward,
            abilityBack,
            abilitySide,
            abilityForward,
            abilityForward,
            abilityBack,
            abilitySide,
            abilityForward
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
            Next();            
            delta = 0;
        }        
    }
    public void Occupy(string actorId) {
        mBattle.Occupy(actorId);
    }    
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
            var actorController = actor.GetComponent<BattleActorController>();
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
                {
                    float remain = mBattle.Attacked(actorId, action);     
                    float hpRatio = mBattle.GetHPRatio(actorId);
                    float currHP = mBattle.GetHP(actorId);

                    if(currHP <= 0) {
                        //삭제                    
                        actorController.SetDie();                                        
                    } else {
                        actorController.SetAttacked(hpRatio, action);                                        
                    }
                }                
                break;
                case BATTLE_ACTOR_ACTION_TYPE.ATTACKING:
                {
                    actorTarget = mMappingTable[action.TargetActorId];
                    actor.transform.LookAt(actorTarget.transform);                
                    
                    float hpRatio = mBattle.GetHPRatio(actorId);
                    actorController.SetAttack(hpRatio, action);
                }
                
                break;
            }
        }
    }    
    bool Load() {
        var pLoader = new Loader();
        TextAsset szSatisfaction = Resources.Load<TextAsset>("Config/satisfactions");
        TextAsset szTask = Resources.Load<TextAsset>("Config/task");
        TextAsset szActor = Resources.Load<TextAsset>("Config/actors");
        TextAsset szItem = Resources.Load<TextAsset>("Config/item");
        TextAsset szLevel = Resources.Load<TextAsset>("Config/level");
        TextAsset szQuest = Resources.Load<TextAsset>("Config/quest");
        TextAsset szScript = Resources.Load<TextAsset>("Config/script");
        TextAsset szScenario = Resources.Load<TextAsset>("Config/scenario");    
        TextAsset szVillage = Resources.Load<TextAsset>("Config/village");        

        if(!pLoader.Load(szSatisfaction.text, szTask.text, szActor.text, szItem.text, szLevel.text, szQuest.text, szScript.text, szScenario.text, szVillage.text)) {
            Debug.Log("Failure Loading config");
            return false;
        }
        return true;
    }
}
