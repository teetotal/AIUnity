using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChessTactic_Camera : MonoBehaviour
{
    [SerializeField]
    private List<string> TargetObjectNames;
    [SerializeField]
    private float SmoothRotation = 4.0f;
    public float Angle = 0;
    public float Distance = 10.0f;
    public float Height = 1.0f;
    private Transform mTargetTransform;
    private string targetName = string.Empty;
    private int targetIdx = 0;
    private float time = 0;
    //----------------------
    private Camera mCamera;
    private float Speed = 0.25f, speedMouse = 80.0f;
    private Vector2 nowPos, prePos;
    private Vector3 movePos;
    // Start is called before the first frame update
    void Start()
    { 
        mCamera = this.gameObject.GetComponent<Camera>();
        //SetTarget();
    }
    void Update() {
        float mouseWheel = Input.mouseScrollDelta.y;
        if(Input.GetMouseButton(0)) 
            MoveByMouse();
        else if(mouseWheel != 0)
            ZoomByMouse(mouseWheel);
        else if(Input.touchCount == 1)
            MoveByTouch();
    }
    void MoveByMouse() {
        if (Input.GetMouseButtonDown(0)) {
            prePos = Input.mousePosition;
        } 

        Vector3 position
            = Camera.main.ScreenToViewportPoint(prePos - (Vector2)Input.mousePosition);

        position.z = position.y;
        position.y = .0f;

        Vector3 move = position * (Time.deltaTime * speedMouse);

        float y = transform.position.y;

        transform.Translate(move);
        transform.transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }
    void ZoomByMouse(float amount) {
        if(mCamera.fieldOfView <= 10 && amount > 0)
            mCamera.fieldOfView = 10;
        else if(mCamera.fieldOfView >= 60 && amount < 0)
            mCamera.fieldOfView = 60;
        else {
            mCamera.fieldOfView -= amount;
        }
    }
    void MoveByTouch() {
        Touch touch = Input.GetTouch (0);
        if(touch.phase == TouchPhase.Began)
        {
            prePos = touch.position - touch.deltaPosition;
        }
        else if(touch.phase == TouchPhase.Moved)
        {
            nowPos = touch.position - touch.deltaPosition;
            movePos = (Vector3)(prePos - nowPos) * Time.deltaTime * Speed;
            this.transform.Translate(movePos); 
            prePos = touch.position - touch.deltaPosition;
        }
    }
    void _LateUpdate()
    {
        time += Time.deltaTime;
        if(time > 10 || mTargetTransform == null ) {
            SetTarget();
            targetIdx++;
            time = 0;
        }

        //전면을 보게 하고 싶으면 180 + mTargetObject.transform.eulerAngles.y
        //뒤에서 따라갈거면 mTargetObject.transform.eulerAngles.y
        float currYAngle = Mathf.LerpAngle(transform.eulerAngles.y, Angle + mTargetTransform.eulerAngles.y, SmoothRotation * Time.deltaTime);
        Quaternion rot = Quaternion.Euler(0, currYAngle, 0 );
        Vector3 position = mTargetTransform.position - (rot * Vector3.forward * Distance) + (Vector3.up *  Height);
        transform.position = Vector3.Lerp(transform.position, position, SmoothRotation * Time.deltaTime);
        transform.LookAt(mTargetTransform);
    }
    void SetTarget() { 
        mTargetTransform = GameObject.Find(TargetObjectNames[targetIdx % TargetObjectNames.Count]).transform;
    }
}
