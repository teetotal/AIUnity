using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Outside : MonoBehaviour
{
    private RectTransform ParentObject;
    [SerializeField]
    private RectTransform TargetObject;
    [SerializeField]
    private float TargetWidthSizePercent = 10;
    private bool lazyInit = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(!lazyInit) {
            lazyInit = true;

            ParentObject = GetComponent<RectTransform>();

            TargetObject.anchorMin = new Vector2(0, 1);
            TargetObject.anchorMax = new Vector2(0, 1);
            TargetObject.pivot = new Vector2(0, 1);

            TargetObject.anchoredPosition = new Vector2(ParentObject.sizeDelta.x, 0);
            float width = ParentObject.sizeDelta.x * TargetWidthSizePercent / 100;
            TargetObject.sizeDelta = new Vector2(width, width);
        }
    }
}
