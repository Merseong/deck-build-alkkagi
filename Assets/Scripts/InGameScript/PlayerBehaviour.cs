using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerBehaviour : MonoBehaviour
{

    //TODO : should move to UIManager
    [SerializeField] private RectTransform cancelPanel;
    [SerializeField] private RectTransform informPanel;

    // 플레이어 정보
    
    // 카드를 내는 동작

    // 드로우

    // 알까기

    // 등등 내턴에 할수있는것들

    [SerializeField] private StoneBehaviour selectedStone;

    private bool isSelecting;
    [SerializeField] private bool isOpenStoneInform = false;
    
    [SerializeField] private float stoneSelectionThreshold = 1f;
    private float curStoneSelectionTime;
    
    [SerializeField] private bool isDragging = false;
    private bool startOnCancel;
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
                    selectedStone = GetStoneAroundPoint(touch.position);

                    if(selectedStone == null)
                    {
                        
                    }
                    else if(isOpenStoneInform) 
                    {
                        //Open Information about selected stone
                        SetInformPanel(selectedStone);
                        informPanel.gameObject.SetActive(true);
                        Debug.Log("Information");
                    }
                    else
                    {
                        //Simply select current stone and move to shooting phase
                        cancelPanel.gameObject.SetActive(true);
                        Debug.Log("Selected");
                    }    
                }
            }
            else
            {
                Vector3 curTouchPosition = Camera.main.ScreenToWorldPoint(touch.position);
                bool isTouchOnCancel = RectTransformUtility.RectangleContainsScreenPoint(cancelPanel, touch.position, null);
                isDragging = !isTouchOnCancel;
                if(touch.phase == TouchPhase.Began)
                {
                    dragStartPoint = curTouchPosition;
                    startOnCancel = isTouchOnCancel;
                }

                if(touch.phase == TouchPhase.Moved)
                {
                    isDragging = !isTouchOnCancel;
                }                

                if(touch.phase == TouchPhase.Ended) 
                {
                    dragEndPoint = curTouchPosition;
                    Vector3 moveVec = dragStartPoint - dragEndPoint;
                    
                    cancelPanel.gameObject.SetActive(false);
                    informPanel.gameObject.SetActive(false);
                    
                    Debug.Log(isDragging + ", " + startOnCancel);
                    
                    if(isDragging && !startOnCancel) 
                    {
                        ShootStone(moveVec);   
                    }
                    selectedStone = null;
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
        selectedStone.GetComponent<AkgRigidbody>().AddForce(vec);
        Debug.Log(vec);
    }

    private void SetInformPanel(StoneBehaviour stone)
    {
        //TODO : Set inform panel to designated stons's data
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
