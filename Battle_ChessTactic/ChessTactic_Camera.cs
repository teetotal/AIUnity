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
    // Start is called before the first frame update
    void Start()
    { 
        SetTarget();
    }

    // Update is called once per frame
    void LateUpdate()
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
