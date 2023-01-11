using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerBehaviour : MonoBehaviour
{

    [SerializeField] private RectTransform cancelPanel;

    // 플레이어 정보
    
    // 카드를 내는 동작

    // 드로우

    // 알까기

    // 등등 내턴에 할수있는것들

    [SerializeField] private StoneBehaviour selectedStone;

    private bool isSelecting;
    private bool isOpenStoneInform = false;
    
    [SerializeField] private float stoneSelectionThreshold = 1f;
    [SerializeField] private float curStoneSelectionTime;
    
    [SerializeField] private bool isDragging = false;
    private Vector3 dragStartPoint;
    private Vector3 dragEndPoint;

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
                    StartCoroutine(EStoneSelection());
                }

                if(touch.phase == TouchPhase.Ended)
                {
                    isSelecting = false;
                    
                    if(isOpenStoneInform) 
                    {
                        //Open Information about selected stone
                        selectedStone = null;
                        Debug.Log("Information");
                    }
                    else
                    {
                        //Simply select current stone and move to shooting phase
                        selectedStone = GetStoneAroundPoint(touch.position);
                        Debug.Log("Selected");
                    }    
                }
            }
            else
            {
                Vector3 curTouchPosition = Camera.main.ScreenToWorldPoint(touch.position);
                //shooting
                if(touch.phase == TouchPhase.Began)
                {
                    isDragging = true;
                    dragStartPoint = curTouchPosition;
                    cancelPanel.gameObject.SetActive(true);
                }

                if(touch.phase == TouchPhase.Moved)
                {
                    isDragging = true;
                    if(RectTransformUtility.RectangleContainsScreenPoint(cancelPanel, touch.position, null))
                    {
                        isDragging = false;
                    }
                }                

                if(touch.phase == TouchPhase.Ended) 
                {
                    dragEndPoint = curTouchPosition;
                    cancelPanel.gameObject.SetActive(false);
                    Vector3 moveVec = dragStartPoint - dragEndPoint;
                    if(isDragging) ShootStone(moveVec);
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

    private void ShootStone(Vector3 vec)
    {
        //Shoot stone...
        //TODO act through physics
        selectedStone.transform.position += vec;
        selectedStone.transform.position = new Vector3(selectedStone.transform.position.x, 1f, selectedStone.transform.position.z);
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
