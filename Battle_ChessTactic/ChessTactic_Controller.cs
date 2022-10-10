using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using ENGINE;
using ENGINE.GAMEPLAY.BATTLE_CHESS_TACTIC;
public class ChessTactic_Controller : MonoBehaviour
{
    [SerializeField]
    private Vector3 StartMapPosition;
    [SerializeField]
    private Vector2 Dimension = new Vector2(5,5);
    [SerializeField]
    private bool MinusIncreasementX = false;
    [SerializeField]
    private bool MinusIncreasementY = false;
    [SerializeField]
    private Vector2Int MapSize;
    [SerializeField]
    private float Interval = 1;
    [SerializeField]
    private List<Vector2Int> Obstacles;
    [SerializeField]
    private int LayerId = 7;

    private bool mIsReady = true;
    private Battle mBattle;
    private Map mMap;
    private float mTimer = 0;
    private List<Rating> ret = new List<Rating>();
    private List<List<Vector3>> mTiles = new List<List<Vector3>>();
    private List<List<GameObject>> mMovableAreas = new List<List<GameObject>>();
    private bool mIsSetMovableArea = false;
    private int mSelectedSoldierId = -1;

    private Dictionary<int, Dictionary<int, GameObject>> mSoldierObjects = new Dictionary<int, Dictionary<int, GameObject>>();
    void Start()
    {
        CreateMap();
        TextAsset sz = Resources.Load<TextAsset>("Config/battle_chesstactic");
        if(sz == null)
            throw new System.Exception("Loading Config Failure. battle_chesstactic");
        
        var info = new Loader().Load(sz.text);
        mBattle = new Battle(mMap, info);

        CreateSolidiers();
        //GameObject.Find("BtnStart").GetComponent<Button>().onClick.AddListener(OnStart);
    }
    public float GetInterval() {
        return Interval;
    }

    // Update is called once per frame
    void Update()
    {
        if(!mIsReady)
            return;
        
        //raycast
        if(Input.GetMouseButtonUp(0)) 
            RaycastByMouse();
        else if(Input.touchCount == 1)
            RaycastByTouch();


        mTimer += Time.deltaTime;
        if(Interval > mTimer) {
            float process = Mathf.Min(mTimer / Interval, 1.0f);
            foreach(var side in mSoldierObjects) {
                foreach(var soldier in side.Value) {
                    soldier.Value.GetComponent<ChessTactic_SoldierController>().ActionUpdate(process);
                }
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
            Soldier soldier = states[i].mSoldier;
            GetSoldierController(soldier.GetSide(), soldier.GetID()).ActionFinish(states[i]);
        }

        if(mBattle.IsFinish()) {
            OnFinish();
            return;
        }

        ret = mBattle.Update();   
        //Action 
        for(int i = 0; i < ret.Count; i++) {
            Rating rating = ret[i];
            GetSoldierController(rating.side, rating.soldierId).ActionStart(rating);
        }
    }
    public GameObject GetSoldierObject(int side, int id) {
        return mSoldierObjects[side][id];
    }
    public ChessTactic_SoldierController GetSoldierController(int side, int id) {
        return GetSoldierObject(side, id).GetComponent<ChessTactic_SoldierController>();
    }
    private void OnFinish() {
        mIsReady = false;
        foreach(var side in mSoldierObjects) {
            foreach(var soldier in side.Value) {
                soldier.Value.GetComponent<ChessTactic_SoldierController>().OnFinish();
            }
        }
    }
    public Vector3 GetTilePosition(float x, float y) {
        return mTiles[(int)x][(int)y] + new Vector3(Random.Range(-1.0f, 1.0f), 0.2f , Random.Range(-1.0f, 1.0f));
    }

    private void CreateMap() {
        mMap = new Map(MapSize.x, MapSize.y);
        for(int x = 0; x < MapSize.x; x++) {
            mTiles.Add(new List<Vector3>());
            mMovableAreas.Add(new List<GameObject>());

            for(int y = 0; y < MapSize.y; y++) {
                /*
                string name = string.Format("t{0}-{1}", x, y);
                GameObject obj = GameObject.Find(name);
                if(obj != null) {
                    Transform tr = GameObject.Find(name).transform;
                    mTiles[x].Add(tr.position);
                    mMovableAreas[x].Add(AllocMovalbleArea(tr.position, x, y));
                }
                */
                float xIncreasement = MinusIncreasementX ? -1 : 1;
                float yIncreasement = MinusIncreasementY ? -1 : 1;
                Vector3 position = StartMapPosition + new Vector3(x * Dimension.x * xIncreasement, 0, y * Dimension.y * yIncreasement);
                mTiles[x].Add(position);
                mMovableAreas[x].Add(AllocMovalbleArea(position, x, y));
                //Debug.Log(string.Format("{0}, {1} - {2}", x, y, position));
            }
        }

        for(int i = 0; i < Obstacles.Count; i++) {
            mMap.AddObstacle(Obstacles[i].x, Obstacles[i].y);
        }
    }
    private void CreateSolidiers() {
        var sides = mBattle.GetSoldiers();
        Vector3 center = GetTilePosition((mMap.GetWidth() / 2), (mMap.GetHeight() / 2));

        foreach(var side in sides) {
            foreach(var soldier in side.Value) {
                if(!mSoldierObjects.ContainsKey(side.Key))
                    mSoldierObjects.Add(side.Key, new Dictionary<int, GameObject>());
                mSoldierObjects[side.Key].Add(soldier.Key, InstantiateSoldier(soldier.Value, center));
            }
        }
    }
    private GameObject InstantiateSoldier(Soldier soldier, Vector3 center) {
        Position pos = soldier.GetPosition();
        
        Vector3 position = GetTilePosition(pos.x, pos.y);
        Quaternion rotation = Quaternion.identity;
        GameObject prefab = Resources.Load<GameObject>("Actors/battle/Soldier1");
        if(prefab == null) 
            throw new System.Exception("Invalid prefab");

        GameObject obj = Instantiate(prefab, position, rotation);
        if(soldier.GetSide() == 0) {
            obj.layer = LayerId;
            obj.name = string.Format("h{0}", soldier.GetID());
        } else {
            obj.name = string.Format("e{0}-{1}", soldier.GetSide(), soldier.GetID());
        }
        //map 중앙 바라보기
        obj.transform.LookAt(center);
        
        obj.GetComponent<ChessTactic_SoldierController>().Init(this, soldier);
        
        //obj.GetComponent<ActorController>().enabled = false;
        //obj.GetComponent<NavMeshAgent>().enabled = false;

        obj.GetComponent<Animator>().runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("Animation/Battle_ChessTactic");
        return obj;
    }
    private void OnStart() {
        mIsReady = true;
    }
    private void Raycast(Vector3 pos) {
        //Debug.Log(pos);
        int layerMask = 1 << LayerId;
        Ray ray = Camera.main.ScreenPointToRay(pos);
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(ray.origin, ray.direction, out hit, Mathf.Infinity, layerMask))
        {
            switch(hit.collider.name.ToCharArray()[0]) {
                case 'h': {
                    mSelectedSoldierId= int.Parse(hit.collider.name.Substring(1));
                    OnSelectedSoldier();
                }
                break;
                case 'A': {
                    //Debug.Log(hit.collider.name);
                    GetSoldierController(0, mSelectedSoldierId).GetSoldier().SetReserve(BehaviourType.MOVE, 0, int.Parse(hit.collider.name.Substring(1)));
                    HideMovableAreas();
                    HideHold();
                }
                break;
            }
        } else {
            HideMovableAreas();
            HideHold();
        }
    }
    private void RaycastByMouse() {
        Raycast(Input.mousePosition);
    }
    private void RaycastByTouch() {
        Touch touch = Input.GetTouch(0);
        Raycast(new Vector3(touch.position.x, touch.position.y, 100));
    }
    private GameObject AllocMovalbleArea(Vector3 position, int x, int y) {
        GameObject prefab = Resources.Load<GameObject>("BattleMovableArea");
        if(prefab == null) 
            throw new System.Exception("Invalid prefab");

        GameObject obj = Instantiate(prefab, position, Quaternion.identity);
        obj.name = string.Format("A{0}", mMap.GetPositionId(x, y));
        obj.layer = LayerId;
        obj.SetActive(false);
        return obj;
    }
    public void HideMovableAreas(int soldierId) {
        if(soldierId == mSelectedSoldierId) {
            if(!mSoldierObjects[0][soldierId].GetComponent<ChessTactic_SoldierController>().GetSoldier().IsEqualPreTargetPosition()) {
                HideMovableAreas();
            }
        }
            
    }
    private void HideMovableAreas() {
        if(!mIsSetMovableArea)
            return;

        for(int x =0; x < mMovableAreas.Count; x ++) {
            for(int y = 0; y < mMovableAreas[0].Count; y++) {
                mMovableAreas[x][y].SetActive(false);
            }
        }
    }
    private void OnSelectedSoldier() {
        //hold 가리기
        HideHold();

        ChessTactic_SoldierController soldier = GetSoldierController(0, mSelectedSoldierId);// mSoldierObjects[0][mSelectedSoldierId].GetComponent<ChessTactic_SoldierController>();
        soldier.ShowHold();

        //area
        HideMovableAreas();
    
        List<MapNode> list = soldier.GetMovalbleArea();
        for(int i = 0 ; i < list.Count; i ++) {
            Position pos = list[i].position;
            mMovableAreas[(int)pos.x][(int)pos.y].SetActive(true);
        }

        mIsSetMovableArea = true;
    }
    private void HideHold() {
        foreach(var s in mSoldierObjects[0]) {
            ChessTactic_SoldierController p = s.Value.GetComponent<ChessTactic_SoldierController>();
            if(!p.GetSoldier().IsHold())
                p.HideHold();
        }
    }
}
