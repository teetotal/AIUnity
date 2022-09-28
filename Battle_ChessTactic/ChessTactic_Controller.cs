using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using ENGINE;
using ENGINE.GAMEPLAY.BATTLE_CHESS_TACTIC;
public class ChessTactic_Controller : MonoBehaviour
{
    [SerializeField]
    private Vector2Int MapSize;
    [SerializeField]
    private float Interval = 1;

    private bool mIsReady = true;
    private Battle mBattle;
    private Map mMap;
    private float mTimer = 0;
    private List<Rating> ret = new List<Rating>();
    private List<List<Vector3>> mTiles = new List<List<Vector3>>();
    private Dictionary<int, GameObject> mHomeSoldiers = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> mAwaySoldiers = new Dictionary<int, GameObject>();
    void Start()
    {
        mMap = CreateMap();
        TextAsset sz = Resources.Load<TextAsset>("Config/battle_chesstactic");
        if(sz == null)
            throw new System.Exception("Loading Config Failure. battle_chesstactic");
        var info = new Loader().Load(sz.text);
        
        List<Soldier> home = CreateSolidiers(true, mMap, info["my"].soldiers);
        List<Soldier> away = CreateSolidiers(false, mMap, info["opp"].soldiers);
        Tactic homeTactic = info["my"].tactic;
        Tactic awayTactic = info["opp"].tactic;

        mBattle = new Battle(mMap, home, away, homeTactic, awayTactic);
        GameObject.Find("BtnStart").GetComponent<Button>().onClick.AddListener(OnStart);
    }

    // Update is called once per frame
    void Update()
    {
        if(!mIsReady)
            return;

        mTimer += Time.deltaTime;
        if(Interval > mTimer) {
            float process = Mathf.Min(mTimer / Interval, 1.0f);
            for(int i = 0; i < mHomeSoldiers.Count; i++) {
                mHomeSoldiers[i].GetComponent<ChessTactic_SoldierController>().ActionUpdate(process);
            }
            for(int i = 0; i < mAwaySoldiers.Count; i++) {
                mAwaySoldiers[i].GetComponent<ChessTactic_SoldierController>().ActionUpdate(process);
            }
            
            return;
        }

        mTimer = 0;
        //반영
        mBattle.ResetState();
        for(int i = 0; i < ret.Count; i++) {
            mBattle.Action(ret[i]);
        }
        List<Soldier.State> states = mBattle.GetActionResult();
        for(int i = 0; i < states.Count; i++) {
            SoldierInfo info = states[i].mSoldier.GetInfo();
            if(info.isHome) {
                mHomeSoldiers[info.id].GetComponent<ChessTactic_SoldierController>().ActionFinish(states[i]);
            } else {
                mAwaySoldiers[info.id].GetComponent<ChessTactic_SoldierController>().ActionFinish(states[i]);
            }
        }

        if(mBattle.IsFinish()) {
            OnFinish();
            return;
        }

        ret = mBattle.Update();   
        //Action 
        for(int i = 0; i < ret.Count; i++) {
            if(ret[i].isHome) {
                mHomeSoldiers[ret[i].soldierId].GetComponent<ChessTactic_SoldierController>().ActionStart(ret[i]);
            } else {
                mAwaySoldiers[ret[i].soldierId].GetComponent<ChessTactic_SoldierController>().ActionStart(ret[i]);
            }
        }
    }
    public GameObject GetSoldierObject(bool isHome, int id) {
        if(isHome)
            return mHomeSoldiers[id];
        else 
            return mAwaySoldiers[id];
    }
    private void OnFinish() {
        mIsReady = false;
    }
    public Vector3 GetTilePosition(float x, float y) {
        return mTiles[(int)x][(int)y];
    }

    private Map CreateMap() {
        Map m = new Map(MapSize.x, MapSize.y);
        for(int x = 0; x < MapSize.x; x++) {
            mTiles.Add(new List<Vector3>());
            for(int y = 0; y < MapSize.y; y++) {
                string name = string.Format("m{0}-{1}", x, y);
                mTiles[x].Add(GameObject.Find(name).transform.position + new Vector3(-2.5f, 0, -2.5f));
            }
        }
        m.AddObstacle(1,1);
        m.AddObstacle(3,3);
        return m;
    }
    private List<Soldier> CreateSolidiers(bool isHome, Map map, List<SoldierInfo> info) {
        List<Soldier> list = new List<Soldier>();
        for(int i = 0; i < info.Count; i++) {
            Soldier soldier = new Soldier(info[i], map, isHome);
            list.Add(soldier);
            if(isHome)
                mHomeSoldiers.Add(soldier.GetID(), InstantiateSoldier(soldier));
            else
                mAwaySoldiers.Add(soldier.GetID(), InstantiateSoldier(soldier));
        }
        return list;
    }
    private GameObject InstantiateSoldier(Soldier soldier) {
        Position pos = soldier.GetPosition();
        
        Vector3 position = mTiles[(int)pos.x][(int)pos.y];
        //Quaternion rotation = Quaternion.Euler(actor.rotation.x, actor.rotation.y, actor.rotation.z);
        Quaternion rotation = Quaternion.identity;
        GameObject prefab = Resources.Load<GameObject>("Actors/battle/Soldier1");
        if(prefab == null) 
            throw new System.Exception("Invalid prefab");

        GameObject obj = Instantiate(prefab, position, rotation);
        obj.name = string.Format("Soldier-{0}-{1}", soldier.IsHome(), soldier.GetID());
        obj.GetComponent<ChessTactic_SoldierController>().Init(this, soldier);
        //obj.GetComponent<ActorController>().enabled = false;
        //obj.GetComponent<NavMeshAgent>().enabled = false;

        obj.GetComponent<Animator>().runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("Animation/Battle_ChessTactic");
        return obj;
    }
    private Tactic CreateTactic(bool isHome) {
        Tactic tactic = new Tactic();
        return tactic;
    }
    private void OnStart() {
        mIsReady = true;
    }
}
