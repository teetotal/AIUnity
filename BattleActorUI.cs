using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleActorUI : MonoBehaviour
{	
    public string targetName;
	private Transform target;
    [SerializeField]
    private Slider _slider;
    [SerializeField]
    private Text _Name;
    [SerializeField]
    private Text _Message;
    bool mIseSetMSG = false;
    float timer = 0;
    [SerializeField]
    private Canvas _panel;

    void Start() {        
        _panel.enabled = false;
    }
	public void SetHP(float value)
	{
		_slider.value = value;
	}
    public void SetName(string name) 
    {
        _Name.text = name;
    }
    public void SetMessage(string msg, int order) 
    {
        _panel.sortingOrder = order;
        _panel.enabled = true;
        _Message.text = msg;
        mIseSetMSG = true;
        timer = 0;        
    }
    private void Update() {
        timer += Time.deltaTime;
        if(timer > 4) {
            _Message.text = "";
            _panel.enabled = false;
        }
        if(target == null) {
            target = GameObject.Find(targetName).transform;
        }
    }
    void LateUpdate()
    {
		transform.position = Camera.main.WorldToScreenPoint(target.position + new Vector3(0, 0.8f, 0));
    }

}