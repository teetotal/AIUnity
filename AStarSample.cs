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
    public GameObject[] PrefabHome;
    public GameObject[] PrefabAway;
    public GameObject Prefab;

    //actor id, obj
    private Dictionary<string, GameObject> mMappingTable = new Dictionary<string, GameObject>();    
    private Battle mBattle;
    // Start is called before the first frame update
    void Start()
    {
        if(!Load()) {
            return;
        }

        //map
        int[,] map =
        {
            {0,      0,      0,      0,      0,    0},
            {1,      0,      0,      0,      0,    1},
            {2,      0,      0,      0,      0,    2},
            {2,      0,      0,      0,      0,    2},
            {1,      0,      0,      0,      0,    1},
            {0,      0,      0,      0,      0,    0}
        };
        /*
        int[,] map =
        {
            {0,     -1,      0,       0,      0,    0},
            {0,     -1,      0,      -1,      0,    0},
            {0,     -1,      0,      -1,      0,    0},
            {0,     -1,      0,      -1,      0,    0},
            {0,      0,      0,      0,       0,    0},
            {0,      0,      0,      -1,      0,    0}
        };
        */

        mBattle = new Battle(mTilesY[0].X.Length, mTilesY.Length);
        if(!mBattle.Init(map, map)) {            
            Debug.Log("Map init failure");
            return;
        }
        //recources경로로 복사해서 써야함. assets에서는 접근이 안됨
        BattleActorAbility abilityForward = new BattleActorAbility();
        abilityForward.Sight = 1;        
        abilityForward.Speed = 1;
        abilityForward.MoveForward = 1.5f;
        abilityForward.MoveBack = 0;
        abilityForward.MoveSide = 0;

        BattleActorAbility abilityBack = new BattleActorAbility();
        abilityBack.Sight = 1;        
        abilityBack.Speed = 1;
        abilityBack.MoveForward = 0;
        abilityBack.MoveBack = 1.5f;
        abilityBack.MoveSide = 0;

        BattleActorAbility abilitySide = new BattleActorAbility();
        abilitySide.Sight = 1;        
        abilitySide.Speed = 1;
        abilitySide.MoveForward = 0;
        abilitySide.MoveBack = 0;
        abilitySide.MoveSide = 1.5f;

        //GaemObject랑 연결
        string[] names = {"hf", "hb", "hs", "af", "ab", "as"};
        Vector2Int[] positions = {
            new Vector2Int(0, 0),
            new Vector2Int(2, 0),
            new Vector2Int(2, 2),

            new Vector2Int(5, 5),
            new Vector2Int(3, 5),
            new Vector2Int(3, 3)
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

            var actor = ActorHandler.Instance.GetActor(actorName);
            if(actor == null) {
                Debug.Log("Actor loading failure");
                return;
            }
            if(!mBattle.AppendActor(pos.x, pos.y, actor, i < 3 ? BATTLE_SIDE.HOME: BATTLE_SIDE.AWAY, abilities[i])) {
                Debug.Log("Fail Append Actor " + actorName);
            }

            mMappingTable.Add(actorName, obj);
        }

        if(!mBattle.Validate()) {
            Debug.Log("Invalid");
            return;
        }

           
    }

    // Update is called once per frame
    void Update()
    {
        foreach(var p in mMappingTable) {
            string actorName = p.Key;
            GameObject obj = p.Value;
            var agent = obj.GetComponent<NavMeshAgent>();
            var animator = obj.GetComponent<Animator>();

            if(agent != null && agent.remainingDistance < 0.3f) {                    
                if(!animator.GetBool("IsIdle")) {
                    animator.SetBool("IsIdle", true);
                    animator.SetBool("IsWalk", false);                        
                }   
                agent.destination = obj.transform.position;
                mBattle.Occupy(actorName);
                Next();

            } else {
                if(animator.GetBool("IsIdle")) {
                    animator.SetBool("IsIdle", false);
                    animator.SetBool("IsWalk", true);
                }
            }
        }
        return;            

    }    

    void Next() {
        Dictionary<string, string[]> next = mBattle.Next();
        foreach(var p in next) {
            string actorName = p.Key;
            string from = p.Value[0];
            string to = p.Value[1];
            Debug.Log("Move " + actorName + " " + from + "->" + to);

            GameObject actor = mMappingTable[actorName];
            var agent = actor.GetComponent<NavMeshAgent>();
            int[] position = mBattle.mMap.GetPositionInt(to);
            var animator = actor.GetComponent<Animator>();
            
            Vector3 toVec3 = mTilesY[position[1]].X[position[0]].transform.position;
            Vector3 dest = new Vector3(toVec3.x + 0.5f, 0, toVec3.z + 0.5f);            
            agent.destination = dest;
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
