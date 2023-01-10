using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private StoneBehaviour selectedStone;

    private bool isSelecting;
    private bool isOpenStoneInform = false;
    
    [SerializeField] private float stoneSelectionThreshold = 1f;
    private float curStoneSelectionTime;
    
    private Vector3 dragStartPoint;
    private Vector3 dragEndPoint;

    // 나와 적한테 하나씩 붙임

    // 게임 덱
    // 현재 덱

    // 손패
    // 현재 코스트

    // 현재 내려놓은 돌

    // 지금까지 한 액션 (내려놓기, 알까기)

    private void Update()
    {
        InputHandler();
    }

    private void InputHandler()
    {   

        if(Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if(selectedStone == null)
            {
                if(touch.phase == TouchPhase.Began)
                {
                    isSelecting = true;
                    selectedStone = GetStoneAroundPoint(touch.position);
                    StartCoroutine(EStoneSelection());
                }

                if(touch.phase == TouchPhase.Ended)
                {
                    isSelecting = false;
                    if(selectedStone != null)
                    {
                        if(isOpenStoneInform) 
                        {
                            //Open Information about selected stone
                            Debug.Log("Information");
                        }
                        else
                        {
                            //Simply select current stone and move to shooting phase
                            Debug.Log("Selected");
                        } 
                    }   
                }
            }
            else
            {
                Vector3 curTouchPosition = Camera.main.ScreenToWorldPoint(touch.position);
                //shooting
                if(touch.phase == TouchPhase.Began)
                {
                    dragStartPoint = curTouchPosition;
                }
                
                if(touch.phase == TouchPhase.Ended)
                {
                    dragEndPoint = curTouchPosition;
                    Vector3 moveVec = dragStartPoint - dragEndPoint;
                    selectedStone.transform.position += moveVec;
                    selectedStone.transform.position = new Vector3(selectedStone.transform.position.x, 1f, selectedStone.transform.position.z);
                }

            }
        }
    }

    private StoneBehaviour GetStoneAroundPoint(Vector3 point)
    {
        Ray ray = Camera.main.ScreenPointToRay(point);
        RaycastHit hit;
        if(Physics.Raycast(ray, out hit))
        {
            if(hit.transform.CompareTag("Stone"))
            {
                return hit.transform.GetComponent<StoneBehaviour>();
            }
        }
        return null;
    }

    private IEnumerator EStoneSelection()
    {
        isOpenStoneInform = false;
        curStoneSelectionTime = stoneSelectionThreshold;
        while(curStoneSelectionTime >= 0)
        {
            curStoneSelectionTime -= Time.deltaTime;
            if(!isSelecting) yield break;
            yield return null;
        }
        isOpenStoneInform = true;
    }
}
