using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class ChessTactic_SoldierUI : MonoBehaviour
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
    private GameObject _messagePanel;
    [SerializeField]
    private GameObject _infoPanel;

    void Start() {    
        RectTransform rtMSG = _messagePanel.GetComponent<RectTransform>();
        rtMSG.sizeDelta = Scale.GetScaledSize(rtMSG.sizeDelta);
        _messagePanel.SetActive(false);

        RectTransform rt = _infoPanel.GetComponent<RectTransform>();
        rt.sizeDelta =  Scale.GetScaledSize(rt.sizeDelta);


        if(target == null) {
            target = GameObject.Find(targetName);
            height = 2.2f;
        }
    }
    public void Hide() {
        _messagePanel.SetActive(false);
        _infoPanel.SetActive(false);
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
        _messagePanel.SetActive(true);
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
                    _messagePanel.SetActive(true);
                    ScriptNode node = msgQ.Dequeue();
                    _message.text = node.msg;
                }
            }
            else if(interval > endTime) {                
                _message.text = string.Empty;
                _messagePanel.SetActive(false);
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