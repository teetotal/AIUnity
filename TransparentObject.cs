using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderObj {
    public Shader originalShader;
    public MeshRenderer originalRenderer; //shader만 저장해서 다시 설정 했더니 안되서 meshRenderer도 저장
    public RenderObj(Shader originalShader, MeshRenderer originalRenderer) {
        this.originalShader = originalShader;
        this.originalRenderer = originalRenderer;
    }
}

public class TransparentObject : MonoBehaviour
{
    public string TargetObject = string.Empty;
    private GameObject mTargetObject; //대상 
    public string TargetLayer = string.Empty; //raycast 비용이 비싸서 정해진 layer만 대상으로 하게끔    
    public Color ColorTransparent = Color.white;
    public float RecoveryTime = 5; //원래 shader로 복구 시간
    private int mLayerMask; 
    private Shader mTransparentShader;
    private Dictionary<string, RenderObj> mDictShader = new Dictionary<string, RenderObj>();
    private float mCounter = 0;
    private float mSmoothRotation = 1.0f;
    private float mHeight = 5.0f;
    private float mDistance = 10.0f;
    private Transform mTransform;
    private bool mIsTargeted = false;
    // Start is called before the first frame update
    void Start()
    {
        mTransform = GetComponent<Transform>();        
        mTransparentShader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
        mLayerMask = 1 << LayerMask.NameToLayer(TargetLayer);        
        SetTargetObject();
    }  
    bool SetTargetObject() {        
        mTargetObject = GameObject.Find(TargetObject);
        if(mTargetObject == null) {
            Debug.Log("Invalid Target Object " + TargetObject);
            mIsTargeted = false;
            return false;
        }
        mIsTargeted = true;
        return true;
    }

    // Update is called once per frame
    void Update()
    {
        if(!mIsTargeted) {
            SetTargetObject();
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

                RenderObj renderObj = new RenderObj(render.material.shader, render);

                mDictShader.Add(obj.name, renderObj);
                render.material.shader = mTransparentShader;
                render.material.SetColor("_Color", ColorTransparent);
                //Debug.Log(string.Format("Transparent {0} {1} > {2}", obj.name, mDictShader[obj.name].originalShader.name, render.material.shader.name));
            }            
        }
    }
    void LateUpdate()
    {        
        //전면을 보게 하고 싶으면 180 + mTargetObject.transform.eulerAngles.y
        //뒤에서 따라갈거면 mTargetObject.transform.eulerAngles.y
        float currYAngle = Mathf.LerpAngle(mTransform.eulerAngles.y, mTargetObject.transform.eulerAngles.y, mSmoothRotation * Time.deltaTime);
        Quaternion rot = Quaternion.Euler(0, currYAngle, 0 );
        mTransform.position = mTargetObject.transform.position - (rot * Vector3.forward * mDistance) + (Vector3.up *  mHeight);
        mTransform.LookAt(mTargetObject.transform);
    }
    void Recovery() {
        mCounter += Time.deltaTime;        
        if(mCounter < 5 || mDictShader.Keys.Count == 0) 
            return;
        
        mCounter = 0;

        List<string> keys = new List<string>();        
        foreach(string key in mDictShader.Keys) {
            keys.Add(key);
        }
        for(int i = 0; i < keys.Count; i++) {
            string key = keys[i];
            RenderObj obj = mDictShader[key];
            obj.originalRenderer.material.shader = obj.originalShader;
            //Debug.Log(string.Format("Recovery {0} > {1}", key, obj.originalShader.name));
            mDictShader.Remove(key);
        }
    }
}
