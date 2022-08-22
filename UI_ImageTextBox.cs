using UnityEngine;
using UnityEngine.UI;
public class UI_ImageTextBox : MonoBehaviour
{
    [SerializeField]
    private RectTransform ImageObject; 
    [SerializeField]
    private RectTransform TextObject;
    [SerializeField]
    private float SpacingPercent = 1;
    [SerializeField]
    private Vector2 PaddingLeftRightPercent = new Vector2(5, 5);
    [SerializeField]
    private float PaddingTopBottomPercent = 1;
    [SerializeField]
    private float ImageSizePercent = 110;
    private bool lazyInit = false;
   
    private void Update() {
        if(!lazyInit) {

            ImageObject.anchorMin = new Vector2(0, 0.5f);
            ImageObject.anchorMax = new Vector2(0, 0.5f);
            ImageObject.pivot = new Vector2(0, 0.5f);

            TextObject.anchorMin = new Vector2(0, 0.5f);
            TextObject.anchorMax = new Vector2(0, 0.5f);
            TextObject.pivot = new Vector2(0, 0.5f);

            RectTransform rect = this.gameObject.GetComponent<RectTransform>();
            float w = rect.sizeDelta.x;
            float h = rect.sizeDelta.y;

            float height  = h - (h * PaddingTopBottomPercent / 100 * 2);
            float heightImage = height * ImageSizePercent / 100;

            ImageObject.sizeDelta = new Vector2(heightImage, heightImage);
            float leftPadding = w * PaddingLeftRightPercent.x / 100;
            ImageObject.anchoredPosition = new Vector2(leftPadding, 0);

            float x = leftPadding + heightImage + (w * SpacingPercent / 100);
            TextObject.anchoredPosition = new Vector2(x, 0);
            float rightPadding = w * PaddingLeftRightPercent.y / 100;

            TextObject.sizeDelta = new Vector2(w - x - rightPadding, height);
            
            lazyInit = true;
        }
        
    }
}
