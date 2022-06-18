using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleActorUI : MonoBehaviour
{	
    public string targetName = string.Empty;
	private Transform target;
    [SerializeField]
    private Slider _slider;
    [SerializeField]
    private Text _Name;
    [SerializeField]
    private Text _Message;
    bool mIseSetMSG = false;
    bool mDelayFinishFlag = false;
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
        _panel.enabled = false;
        mIseSetMSG = true;
        mDelayFinishFlag = false;
        timer = 0;        
    }
    private void Update() {
        if(target == null) {
            target = GameObject.Find(targetName).transform;
        }
        if(mIseSetMSG) {
            timer += Time.deltaTime;
            if(!mDelayFinishFlag && timer > 1.0f) {
                _panel.enabled = true;
                mDelayFinishFlag = true;
            } else if(timer > 5.5f) 
            {
                mDelayFinishFlag = false;
                _Message.text = "";
                _panel.enabled = false;
                mIseSetMSG = false;
            }
        }
    }
    void LateUpdate()
    {
		transform.position = Camera.main.WorldToScreenPoint(target.position + new Vector3(0, 1.8f, 0));
    }

}