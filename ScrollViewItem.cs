using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Mosframe;

public class ScrollViewItem : UIBehaviour, IDynamicScrollViewItem
{
    public void onUpdateItem( int index ){
        Debug.Log(index);
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
