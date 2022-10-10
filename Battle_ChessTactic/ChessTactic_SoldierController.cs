using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ENGINE;
using ENGINE.GAMEPLAY.BATTLE_CHESS_TACTIC;

public class ChessTactic_SoldierController : MonoBehaviour
{
    [SerializeField]
    private Transform bullet;
    [SerializeField]
    private GameObject firstAid;
    [SerializeField]
    private GameObject particleHit;
    [SerializeField]
    private List<GameObject> characters;
    [SerializeField]
    private List<GameObject> weapons;

    private Vector3 bulletStartPoint, bulletInitLocalPosition;
    private ParticleSystem mParticleHit;

    private bool IsReady = false;
    private Soldier mSoldier;
    private ChessTactic_Controller mController;
    private Vector3 mStartPosition, mEndPosition;
    // ---------------------------------------
    private BehaviourType mCurrentActionType;
    private int mCurrentActionTargetSide;
    private int mCurrentActionTargetId;
    // ---------------------------------------
    private Animator mAnimator;
    private Vector3 ADJUST_ROTATION_VECTOR = new Vector3(0, 45, 0);
    private enum AnimationCode {
        Idle = 0,
        Reload,
        Recovery,
        Death,
        Walk,
        
        Fire = 10,
        FireStanding,
        Reaction1=20,
        Reaction2
    }
    private const string AnimationId = "AnimationId";
    private const string UIPrefab = "ChessTactic_SoldierUI";
    private ChessTactic_SoldierUI mUI;
    private GameObject mUIObject;
    public void Init(ChessTactic_Controller controller, Soldier soldier) {
        mController = controller;
        mSoldier = soldier;
    }
    public Soldier GetSoldier() {
        return mSoldier;
    }
    public void ShowHold() {
        mUI.ShowHold();
    }
    public void HideHold() {
        mUI.HideHold();
    }
    private void SetAnimation(AnimationCode code) {
        mAnimator.SetInteger(AnimationId, (int)code);
    }
    public void OnFinish() {
        if(!mSoldier.IsDie())
            SetAnimation(AnimationCode.Idle);
    }
    public List<MapNode> GetMovalbleArea() {
        return mSoldier.GetMovableAreaList();
    }
    public void ActionStart(Rating rating) {
        mStartPosition = transform.position;
        mCurrentActionType = rating.type;
        mCurrentActionTargetSide = rating.targetSide;
        mCurrentActionTargetId = rating.targetId;
        IsReady = true;

        if(!mSoldier.IsEqualPreTargetPosition()) {
            mController.HideMovableAreas(mSoldier.GetID());
            mUI.HideHold();
        }

        switch(rating.type) {
            //Recovery
            case BehaviourType.RECOVERY: {
                SetAnimation(AnimationCode.Recovery);
            }
            break;
            //Avoidance
            case BehaviourType.AVOIDANCE:
            //Move
            case BehaviourType.MOVE: {
                Position pos = mSoldier.GetMap().GetPosition(rating.targetId);
                mEndPosition = mController.GetTilePosition(pos.x, pos.y) + new Vector3(Random.Range(-1.5f, 1.5f), 0.2f , Random.Range(-1.5f, 1.5f));
                /*
                if(Vector3.Distance(mStartPosition, mEndPosition) < 5)
                    SetAnimation(AnimationCode.Walk);
                else
                    SetAnimation(AnimationCode.Run);
                */
                
                SetAnimation(AnimationCode.Walk);
            }
            break;
            //Attack
            case BehaviourType.ATTACK: {
                //bullet
                bullet.gameObject.SetActive(true);
                bullet.localPosition = bulletInitLocalPosition;
                bulletStartPoint = bullet.position;

                GameObject target = mController.GetSoldierObject(mCurrentActionTargetSide, mCurrentActionTargetId);
                mEndPosition = target.transform.position;
                switch(mSoldier.GetInfo().movingType) {
                    case MOVING_TYPE.OVER_CROSS:
                    case MOVING_TYPE.OVER_STRAIGHT:
                    SetAnimation(AnimationCode.FireStanding);
                    break;
                    default:
                    SetAnimation(AnimationCode.Fire);
                    break;
                }
            }
            break;
            //Keep
            case BehaviourType.KEEP: {
                Position pos = mSoldier.GetPosition();
                mEndPosition = mController.GetTilePosition(pos.x, pos.y);
                SetAnimation(AnimationCode.Reload);

            }
            break;
        }

        //Debug.Log(string.Format("home: {0}, id: {1}, target: {2}", mSoldier.IsHome(), mSoldier.GetID(), mCurrentActionTarget));
    }
    public void ActionUpdate(float process) {
        if(!IsReady)
            return;

        switch(mCurrentActionType) {
            case BehaviourType.ATTACK: 
            {
                Soldier.State state = mSoldier.GetState();
                if(process > 0.3f && process < 0.6f && state.damagePre > 0) {
                    switch(mSoldier.GetInfo().movingType) {
                        case MOVING_TYPE.OVER_CROSS:
                        case MOVING_TYPE.OVER_STRAIGHT:
                        SetAnimation(AnimationCode.Reaction2);
                        break;
                        default:
                        SetAnimation(AnimationCode.Reaction1);
                        break;
                    }
                } else {
                    transform.LookAt(mEndPosition);
                    Vector3 rot = transform.rotation.eulerAngles + ADJUST_ROTATION_VECTOR;
                    transform.rotation = Quaternion.Euler(rot);
                    //bulet
                    Vector3 bulletTarget = mEndPosition;
                    switch(mSoldier.GetInfo().movingType) {
                        case MOVING_TYPE.OVER_CROSS:
                        case MOVING_TYPE.OVER_STRAIGHT:
                        bulletTarget += new Vector3(0, 1.5f, 0);
                        break;
                        default:
                        bulletTarget += new Vector3(0, 1f, 0);
                        break;
                    }
                    float distance = Vector3.Distance(bulletStartPoint, bulletTarget);
                    //알파값(0.07)을 작게 할 수록 빨라짐
                    float p =  (distance / mSoldier.GetAbility().attackRange) * 0.07f;
                    float b = 1 / p;
                    float rate = (process % p) * b;
                    bullet.position = Vector3.Lerp(bulletStartPoint, bulletTarget, rate);
                }
            }
            break;
            case BehaviourType.AVOIDANCE:
            case BehaviourType.MOVE: 
                transform.position = Vector3.Lerp(mStartPosition, mEndPosition, process);
                transform.LookAt(mEndPosition);
            break;
            default:
            break;
        }
    }
    public void ActionFinish(Soldier.State state) {
        bullet.gameObject.SetActive(false);

        if(!IsReady)
            return;

        switch(mCurrentActionType) {
            case BehaviourType.ATTACK: {
            }
            break;
            case BehaviourType.RECOVERY: {
                if(mSoldier.GetItem().firstAid <= 0)
                    firstAid.SetActive(false);
            }
            break;
            case BehaviourType.AVOIDANCE:
            case BehaviourType.MOVE: {
                transform.position = mEndPosition;
            }
            break;
        }
        // ---------------------------------
        string sz = string.Empty;
        if(state.attack > 0) {
            sz += state.attack.ToString();
        }
        if(state.damage > 0) {
            sz += " -" + state.damage.ToString();
            sz += " =" + mSoldier.GetHP().ToString();
            mParticleHit.Play();
        }
        //if(sz.Length > 0) mUI.SetMessage(sz);
        // -------------------------------------
        //if(state.isHit) mUI.SetMessage("명중");
        //if(state.isRetreat) mUI.SetMessage("회피");
        if(state.isDie) {
            mUI.Hide();
            SetAnimation(AnimationCode.Death);
            mSoldier.SetDie();
            IsReady = false;
            gameObject.layer = 0;
        }
        mUI.SetHP(mSoldier.GetHP());

    }
    void Start()
    {
        for(int i = 0; i < characters.Count; i++)
        {
            if((int)mSoldier.GetInfo().movingType == i) {
                characters[i].SetActive(true);
                weapons[i].SetActive(true);
            } 
        }
        //rotation. home team
        //if(mSoldier.IsHome()) transform.rotation = Quaternion.Euler(new Vector3(0, 180, 0));
        mAnimator = GetComponent<Animator>();
        //UI 
        var prefab = Resources.Load<GameObject>(UIPrefab);
        var canvas = GameObject.Find("Canvas");
        if(prefab != null && canvas != null) {
            mUIObject = Instantiate<GameObject>(prefab, Vector3.zero, Quaternion.identity);
            mUIObject.name = UIPrefab + "_" + name;
            mUIObject.transform.SetParent(canvas.transform);
            mUI = mUIObject.GetComponent<ChessTactic_SoldierUI>();
            mUI.targetName = this.name;
            mUI.Init(mSoldier.GetName(), mSoldier.GetSide());
            mUI.SetHP(mSoldier.GetHP());
        }
        //bullet
        bulletInitLocalPosition = bullet.localPosition;
        bulletStartPoint = bullet.position;
        //particle
        mParticleHit = particleHit.GetComponent<ParticleSystem>();
    }
}
