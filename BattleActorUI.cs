using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using TMPro;
public class BattleActorUI : MonoBehaviour
{	
    public class ScriptNode {
        public int time;
        public string msg;
        public ScriptNode(int time, string msg) {
            this.time = time;
            this.msg = msg;
        }
    }
    public string targetName = string.Empty;
	private GameObject target;
    private float height;
    [SerializeField]
    private Slider _slider;
    [SerializeField]
    private TextMeshProUGUI _name;
    [SerializeField]
    private TextMeshProUGUI _message;
    private Queue<ScriptNode> msgQ = new Queue<ScriptNode>();
    bool mIseSetMSG = false;    
    DateTime mStartTime;
    int startTime = 1000;
    int endTime = 5500;
    
    [SerializeField]
    private Canvas _canvas;
    [SerializeField]
    private RectTransform _panel;

    void Start() {        
        //_panel.sizeDelta.x : 1366 = x : Screen.safeArea.width
        _panel.sizeDelta = Scale.GetScaledSize(_panel.sizeDelta);
        _canvas.enabled = false;

        if(target == null) {
            target = GameObject.Find(targetName);
            NavMeshAgent nav = target.GetComponent<NavMeshAgent>();     
            if(nav != null)
                height = nav.height;
            else
                height = 2f;
        }
    }
	public void SetHP(float value)
	{
		_slider.value = value;
	}
    public void SetName(string name) 
    {
        _name.text = name;
    }
    public void SetMessage(string msg, bool isOverlap = true) 
    {
        if(!isOverlap && mIseSetMSG)
            return;
            
        //_canvas.sortingOrder = order;
        _canvas.enabled = true;
        _canvas.enabled = false;
        mIseSetMSG = true;        
        mStartTime = DateTime.Now;
        
        //나중에 pooling으로 바꿔야 함
        msgQ.Clear();
        string[] arr = msg.Split('\n');
        int time = (int)((endTime - startTime) / arr.Length);
        for(int i=0; i < arr.Length; i++) {            
            msgQ.Enqueue(new ScriptNode(startTime + (time * i), arr[i]));
        }
    }
    private void Update() {
        if(mIseSetMSG) {
            double interval = (DateTime.Now - mStartTime).TotalMilliseconds;
            if(msgQ.Count > 0 && interval <= endTime) {                                
                if(msgQ.Peek().time <= interval) {
                    _canvas.enabled = true;
                    ScriptNode node = msgQ.Dequeue();
                    _message.text = node.msg;
                }
            }
            else if(interval > endTime) {                
                _message.text = string.Empty;
                _canvas.enabled = false;
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