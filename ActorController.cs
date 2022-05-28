using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActorController : MonoBehaviour
{
    Animator mAnimator;
    bool mIsDie = false;
    float mDieCounter = 0;
    // Start is called before the first frame update
    void Start()
    {
        mAnimator = gameObject.GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if(mIsDie) {
            mDieCounter += Time.deltaTime;
            if(mDieCounter > 5) {
                this.gameObject.SetActive(false);
            }
        }        
    }
    public void SetDie() {    
        mAnimator.SetBool("IsDie", true);
        mAnimator.SetBool("IsIdle", false);
        mAnimator.SetBool("IsWalk", false);
        mAnimator.SetBool("IsAttack", false);        
        mAnimator.SetBool("IsAttacked", false);
        mIsDie = true;        
    }
    public void SetWalk() {        
        mAnimator.SetBool("IsIdle", false);
        mAnimator.SetBool("IsWalk", true);
        mAnimator.SetBool("IsAttack", false);
        mAnimator.SetBool("IsDie", false);
        mAnimator.SetBool("IsAttacked", false);
    }
    public void SetAttack() {
        mAnimator.SetBool("IsIdle", false);
        mAnimator.SetBool("IsWalk", false);
        mAnimator.SetBool("IsAttack", true);
        mAnimator.SetBool("IsDie", false);
        mAnimator.SetBool("IsAttacked", false);
    }
    public void SetAttacked() {
        mAnimator.SetBool("IsIdle", false);
        mAnimator.SetBool("IsWalk", false);
        mAnimator.SetBool("IsAttack", false);
        mAnimator.SetBool("IsDie", false);
        mAnimator.SetBool("IsAttacked", true);
    }
    public void SetIdle() {
        mAnimator.SetBool("IsIdle", true);
        mAnimator.SetBool("IsWalk", false);
        mAnimator.SetBool("IsAttack", false);
        mAnimator.SetBool("IsDie", false);
        mAnimator.SetBool("IsAttacked", false);
    }
}