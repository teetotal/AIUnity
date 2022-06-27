using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;

public class BattleActorUI : MonoBehaviour
{	
    public string targetName = string.Empty;
	private GameObject target;
    private float height;
    [SerializeField]
    private Slider _slider;
    [SerializeField]
    private Text _Name;
    [SerializeField]
    private Text _Message;
    bool mIseSetMSG = false;
    bool mDelayFinishFlag = false;
    DateTime mStartTime;
    
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
    public void SetMessage(string msg, int order, bool isOverlap = true) 
    {
        if(!isOverlap && mIseSetMSG)
            return;
            
        _panel.sortingOrder = order;
        _panel.enabled = true;
        _Message.text = msg;
        _panel.enabled = false;
        mIseSetMSG = true;
        mDelayFinishFlag = false;
        mStartTime = DateTime.Now;
        
    }
    private void Update() {
        if(target == null) {
            target = GameObject.Find(targetName);
            NavMeshAgent nav = target.GetComponent<NavMeshAgent>();     
            height = nav.height;
        }
        if(mIseSetMSG) {
            double interval = (DateTime.Now - mStartTime).TotalMilliseconds;
            if(!mDelayFinishFlag && interval > 1000) {
                _panel.enabled = true;
                mDelayFinishFlag = true;
            } else if(interval > 5500) {                
                mDelayFinishFlag = false;
                _Message.text = "";
                _panel.enabled = false;
                mIseSetMSG = false;
            }
        }
    }
    void LateUpdate()
    {
        if(target != null) {
            transform.position = Camera.main.WorldToScreenPoint(target.transform.position + new Vector3(0, height, 0));
        }
		    
    }

}