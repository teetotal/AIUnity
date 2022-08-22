using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Resizing : MonoBehaviour
{
    [SerializeField]
    private bool Horizontal = true;
    [SerializeField]
    private RectTransform[] Objects;
    [SerializeField]
    private float[] Ratios;
    [SerializeField]
    private float SpacingPercent = 1;
    [SerializeField]
    private Vector2 PaddingLeftRightPercent = new Vector2(5, 5);
    [SerializeField]
    private Vector2 PaddingTopBottomPercent = new Vector2(1, 1);
    private bool lazyInit = false;

    // Start is called before the first frame update
    void Start()
    {
        if(Ratios.Length != Objects.Length)
            throw new System.Exception("Ratios.Length != Objects.Length");
    }

    // Update is called once per frame
    void Update()
    {
        if(!lazyInit) {
            lazyInit = true;
            float sum = 0;
            for(int i = 0; i < Ratios.Length; i++) {
                sum += Ratios[i];
            }
            RectTransform rect = this.gameObject.GetComponent<RectTransform>();
            float w = rect.sizeDelta.x;
            float h = rect.sizeDelta.y;

            //Debug.Log(string.Format("Original {0}, {1}", w, h));
            //padding
            Vector2 paddingLR = new Vector2(w * PaddingLeftRightPercent.x / 100, w * PaddingLeftRightPercent.y / 100);
            Vector2 paddingTB = new Vector2(w * PaddingTopBottomPercent.x / 100, w * PaddingTopBottomPercent.y / 100);

            //Debug.Log(string.Format("Padding LR {0}, {1}", paddingLR.x, paddingLR.y));
            //Debug.Log(string.Format("Padding TB {0}, {1}", paddingTB.x, paddingTB.y));

            w -= (paddingLR.x + paddingLR.y);
            h -= (paddingTB.x + paddingTB.y);

            //Debug.Log(string.Format("Original - Padding {0}, {1}", w, h));

            float space = 0;
            if(Horizontal) {
                space = w * (SpacingPercent / 100);
                w -= space * (Objects.Length -1);
            }
            else {
                space = h * (SpacingPercent / 100);
                h -= space * (Objects.Length -1);
            }

            //Debug.Log(string.Format("Original - Padding  - space {0}, {1} / {2}", w, h, space));
            
            Vector2 Position = Vector2.zero;
            if(Horizontal) {
                Position = new Vector2(paddingLR.x, 0);
            } else {
                Position = new Vector2(paddingLR.x, -paddingTB.x);
            }

            for(int i = 0; i < Objects.Length; i++) {
                float width = 0;
                float height = 0;

                if(Horizontal) {
                    Objects[i].anchorMin = new Vector2(0, 0.5f);
                    Objects[i].anchorMax = new Vector2(0, 0.5f);
                    Objects[i].pivot = new Vector2(0, 0.5f);
                    height = h;
                    width = w * (Ratios[i]/sum);
                } else {
                    Objects[i].anchorMin = new Vector2(0, 1f);
                    Objects[i].anchorMax = new Vector2(0, 1f);
                    Objects[i].pivot = new Vector2(0, 1f);
                    height = h * (Ratios[i]/sum);
                    width = w;
                }
                Objects[i].sizeDelta = new Vector2(width, height);
                Objects[i].anchoredPosition = new Vector2(Position.x, Position.y);

                if(Horizontal) {
                    Position.x += width + space;
                } else {
                    Position.y -= (height + space);
                }
            }

        }
        
    }
}
