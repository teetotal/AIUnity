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
public class AStarSample : MonoBehaviour
{
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
        int[,] map =
        {
            {0,      0,      0,      0,      0},
            {0,      0,      0,      0,      0},
            {0,      0,      0,      0,      0},
            {0,      0,      0,      0,      0},
            {0,      0,      0,      0,      0},
            {0,      0,      0,      0,      0}
        };

        mBattle = new Battle(mTilesY[0].X.Length, mTilesY.Length);
        if(!mBattle.Init(map, map)) {            
            Debug.Log("Map init failure");
            return;
        }
        //recources경로로 복사해서 써야함. assets에서는 접근이 안됨
        BattleActorAbility abilityForward = new BattleActorAbility();
        abilityForward.HP = 9;
        abilityForward.AttackStyle = BattleActorAbility.ATTACK_STYLE.DEFENSE;
        abilityForward.AttackPower = 1;
        abilityForward.AttackDistance = 1;        
        abilityForward.AttackAccuracy = 1;
        abilityForward.Sight = 1;        
        abilityForward.Speed = 1;
        abilityForward.MoveForward = 3f;
        abilityForward.MoveBack = 0;
        abilityForward.MoveSide = 0;

        BattleActorAbility abilityBack = new BattleActorAbility();
        abilityBack.HP = 9;
        abilityBack.AttackStyle = BattleActorAbility.ATTACK_STYLE.DEFENSE;
        abilityBack.AttackPower = 1;
        abilityBack.AttackDistance = 1;   
        abilityBack.AttackAccuracy = 1;     
        abilityBack.Sight = 1;        
        abilityBack.Speed = 1;
        abilityBack.MoveForward = 0;
        abilityBack.MoveBack = 1.5f;
        abilityBack.MoveSide = 0;

        BattleActorAbility abilitySide = new BattleActorAbility();
        abilitySide.HP = 9;
        abilitySide.AttackStyle = BattleActorAbility.ATTACK_STYLE.DEFENSE;
        abilitySide.AttackPower = 1;
        abilitySide.AttackDistance = 1;      
        abilitySide.AttackAccuracy = 1;  
        abilitySide.Sight = 1;        
        abilitySide.Speed = 1;
        abilitySide.MoveForward = 0;
        abilitySide.MoveBack = 0;
        abilitySide.MoveSide = 1.5f;

        //GaemObject랑 연결
        string[] names = {"hf", "hb", "hs", "af", "ab", "as"};
        Vector2Int[] positions = {
            new Vector2Int(1, 1),
            new Vector2Int(2, 1),
            new Vector2Int(3, 1),

            new Vector2Int(4, 3),
            new Vector2Int(3, 3),
            new Vector2Int(2, 3)
        };
        BattleActorAbility[] abilities = {
            abilityForward,
            abilityBack,
            abilitySide,
            abilityForward,
            abilityBack,
            abilitySide
        };

        for(int i=0; i < names.Length; i++) {
            string actorName = names[i];
            Vector2Int pos = positions[i];

            GameObject prefab;
            if(i < 3)
                prefab = Resources.Load<GameObject>("Characters/Character_Dummy_Female_01");
            else 
                prefab = Resources.Load<GameObject>("Characters/Character_Dummy_Male_01");

            if(prefab == null) {
                Debug.Log("Prefab loading failure");
                return;
            }
            Vector3 dest = mTilesY[pos.y].X[pos.x].transform.position;
            dest = new Vector3(dest.x + 0.5f, dest.y, dest.z + 0.5f);
            GameObject obj = Instantiate<GameObject>(prefab, dest, Quaternion.identity);
            obj.name = actorName;
            Debug.Log(string.Format("{0} {1} {2}", actorName, dest.x, dest.z));

            var actor = ActorHandler.Instance.GetActor(actorName);
            if(actor == null) {
                Debug.Log("Actor loading failure");
                return;
            }
            if(!mBattle.AppendActor(pos.x, pos.y, actor, i < 3 ? BATTLE_SIDE.HOME: BATTLE_SIDE.AWAY, abilities[i])) {
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
        if(delta > 2) {
            Next();
            delta = 0;
        }        
    }
    // Update is called once per frame
    void Update()
    {
        foreach(var p in mMappingTable) {
            if(p.Value.activeSelf == false) continue;
            string actorName = p.Key;
            GameObject obj = p.Value;
            var agent = obj.GetComponent<NavMeshAgent>();
            var animator = obj.GetComponent<Animator>();

            if(agent != null && agent.remainingDistance < 0.3f) {
                agent.destination = obj.transform.position;
                mBattle.Occupy(actorName);
            } 
        }
        return;
    }
    void Next() {
        Dictionary<string, BattleActorAction> next = mBattle.Next();
        foreach(var p in next) {
            string actorName = p.Key;
            BattleActorAction action = p.Value;
            GameObject actor = mMappingTable[actorName];
            var actorController = actor.GetComponent<ActorController>();

            switch(action.Type) {
                case BATTLE_ACTOR_ACTION_TYPE.NONE:
                actorController.SetIdle();
                break;
                case BATTLE_ACTOR_ACTION_TYPE.MOVING:
                
                var agent = actor.GetComponent<NavMeshAgent>();
                int[] position = mBattle.mMap.GetPositionInt(action.TargetPosition);
                
                Vector3 toVec3 = mTilesY[position[1]].X[position[0]].transform.position;
                Vector3 dest = new Vector3(toVec3.x + 0.5f, 0, toVec3.z + 0.5f);            
                agent.destination = dest;
                actorController.SetWalk();
                break;

                case BATTLE_ACTOR_ACTION_TYPE.ATTACKING:
                GameObject actorTarget = mMappingTable[action.TargetActorId];
                float remain = mBattle.Attack(actorName, action);     
                float hpRatio = mBattle.GetHPRatio(action.TargetActorId);
                float currHP = mBattle.GetHP(action.TargetActorId);

                actor.transform.LookAt(actorTarget.transform);
                actorController.SetAttack();
                
                if(currHP <= 0) {
                    //삭제                    
                    actorTarget.GetComponent<ActorController>().SetDie();                    
                    mActorUI[action.TargetActorId].SetActive(false);
                } else {
                    ActorUI actorUI = mActorUI[action.TargetActorId].GetComponent<ActorUI>();
                    actorUI.SetHP(hpRatio);
                    actorUI.SetMessage(string.Format("-{0}", action.AttackAmount));
                }
                //Debug.Log(string.Format("Attack {0} > {1} {2}({3})", actorName, action.TargetActorId, damage, hpRatio));
                break;
            }
        }
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
