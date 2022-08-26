using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderObj {
    public GameObject gameObject;
    public Shader originalShader;
    public MeshRenderer originalRenderer; //shader만 저장해서 다시 설정 했더니 안되서 meshRenderer도 저장
    public RenderObj(GameObject gameObject, Shader originalShader, MeshRenderer originalRenderer) {
        this.gameObject = gameObject;
        this.originalShader = originalShader;
        this.originalRenderer = originalRenderer;
    }
}
public class CameraContext {
    public class Context {
        public float angle = 0;
        public float distance;
        public float height;
    }
    private bool isInteractionMode = false;
    private float timer = 0;

    private float duration;
    private Context defaultContext = new Context();
    private Context interactionContext = new Context();

    public void Init(   float angle, float distance, float height, 
                        float angleInteraction, float defaultDistance, float defaultHeight,
                        float duration) {

        this.defaultContext.angle       = angle;
        this.defaultContext.distance    = distance;
        this.defaultContext.height      = height;

        this.interactionContext.angle       = angleInteraction;
        this.interactionContext.distance    = defaultDistance;
        this.interactionContext.height      = defaultHeight;

        this.timer = 0;
        this.duration = duration;
    }
    public void Check(float deltaTime) {
        if(isInteractionMode) {
            timer += deltaTime;
            if(timer > duration)
                Release();
        }
    }
    private void Release() {
        this.timer = 0;
        this.isInteractionMode = false;
    }
    public Context GetContext() {
        return isInteractionMode ? interactionContext : defaultContext;
    }
    public void SetInteractionMode() {
        isInteractionMode = true;
    }
}

public class TransparentObject : MonoBehaviour
{
    //저사양 모드. true면 투명 처리 대신 SetActive(false)한다
    public bool IsLowMode = false;
    public GameObject GameController;
    private string mFollowActorId = string.Empty;
    private GameObject mTargetObject; //대상 
    public string TargetLayer = "Bld"; //raycast 비용이 비싸서 정해진 layer만 대상으로 하게끔    
    public Color ColorTransparent = new Color(1,1,1,0.4f);
    public float RecoveryTime = 5; //원래 shader로 복구 시간
    
    public float SmoothRotation = 4.0f;
    public float ContextChangeDuration = 8;

    public float Angle = 0;
    public float Distance = 10.0f;
    public float Height = 1.0f;

    public float InteractionAngle = -45;
    public float InteractionDistance = 8;
    public float InteractionHeight = 2.5f;
    

    private int mLayerMask; 
    private Shader mTransparentShader;
    private Dictionary<string, RenderObj> mDictShader = new Dictionary<string, RenderObj>();
    private float mCounter = 0;
    private Transform mTransform;

    private CameraContext mContext = new CameraContext();

    // Start is called before the first frame update
    void Start()
    {
        mContext.Init(Angle, Distance, Height, InteractionAngle, InteractionDistance, InteractionHeight, ContextChangeDuration);
        
        mFollowActorId = GameController.GetComponent<GamePlayController>().FollowActorId;
        mTransform = GetComponent<Transform>();        
        mTransparentShader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
        mLayerMask = 1 << LayerMask.NameToLayer(TargetLayer);        
        SetTargetObject();
    }  
    bool SetTargetObject() {        
        mTargetObject = GameObject.Find(mFollowActorId);
        if(mTargetObject == null) {
            //Debug.Log("Invalid Target Object " + mFollowActorId);
            return false;
        }
        
        return true;
    }
    public string GetFollowingActorId() {
        return mFollowActorId;
    }

    // Update is called once per frame
    void Update()
    {
        if(mTargetObject == null) {
            if(!SetTargetObject())
                return;
        }
        Recovery();

        Vector3 from = gameObject.transform.position;
        Vector3 to = mTargetObject.transform.position;
        float distance = Vector3.Distance(from, to);
        Vector3 direction = (to - from).normalized;
        RaycastHit[] hits = Physics.RaycastAll(from, direction, distance, mLayerMask);
        for (int i = 0; i < hits.Length; i++)
        {
            GameObject obj = hits[i].collider.gameObject;
            if(mDictShader.ContainsKey(obj.name) == false) {                
                MeshRenderer render = obj.GetComponent<MeshRenderer>();     

                RenderObj renderObj = new RenderObj(obj, render.material.shader, render);

                mDictShader.Add(obj.name, renderObj);
                if(IsLowMode)
                    obj.SetActive(false);
                else {
                    render.material.shader = mTransparentShader;
                    render.material.SetColor("_Color", ColorTransparent);
                }
            }            
        }
    }
    void LateUpdate()
    {        
        if(mTargetObject == null)
            return;

        mContext.Check(Time.deltaTime);
        CameraContext.Context context = mContext.GetContext();
        
        //전면을 보게 하고 싶으면 180 + mTargetObject.transform.eulerAngles.y
        //뒤에서 따라갈거면 mTargetObject.transform.eulerAngles.y
        float currYAngle = Mathf.LerpAngle(mTransform.eulerAngles.y, context.angle + mTargetObject.transform.eulerAngles.y, SmoothRotation * Time.deltaTime);
        Quaternion rot = Quaternion.Euler(0, currYAngle, 0 );
        Vector3 position = mTargetObject.transform.position - (rot * Vector3.forward * context.distance) + (Vector3.up *  context.height);
        mTransform.position = Vector3.Lerp(mTransform.position, position, SmoothRotation * Time.deltaTime);
        mTransform.LookAt(mTargetObject.transform);
    }
    void Recovery() {
        mCounter += Time.deltaTime;        
        if(mCounter < RecoveryTime || mDictShader.Keys.Count == 0) 
            return;
        
        mCounter = 0;

        List<string> keys = new List<string>();        
        foreach(string key in mDictShader.Keys) {
            keys.Add(key);
        }
        for(int i = 0; i < keys.Count; i++) {
            string key = keys[i];
            RenderObj obj = mDictShader[key];

            if(IsLowMode)
                obj.gameObject.SetActive(true);
            else {
                obj.originalRenderer.material.shader = obj.originalShader;
                //Debug.Log(string.Format("Recovery {0} > {1}", key, obj.originalShader.name));
            }
            mDictShader.Remove(key);
        }
    }
    public void SetInteractionAngle() {
        //Debug.Log("SetInteractionAngle");        
        mContext.SetInteractionMode();
    }
}
