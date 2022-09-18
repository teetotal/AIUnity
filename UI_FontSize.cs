using UnityEngine;
using TMPro;
public class UI_FontSize : MonoBehaviour
{
    [SerializeField]
    private float size = 24; 
    // Start is called before the first frame update
    void Start()
    {
        //1920 : size = Screen.safeArea.width : x
        float fontSize = size * Screen.safeArea.width / 1920;
        TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
        if(text.enableAutoSizing)
            text.fontSizeMax = fontSize;
        else
            text.fontSize = fontSize;
    }
}
