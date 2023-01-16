using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    [SerializeField] private Card selectedCard;

    private bool isSelecting;
    [SerializeField] private bool isOpenStoneInform = false;
    
    [SerializeField] private float stoneSelectionThreshold = 1f;
    private float curStoneSelectionTime;
    
    [SerializeField] private float maxDragLimit;
    [SerializeField] private LineRenderer dragEffectObj;
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
//플랫폼별 테스트를 위한 분기 코드        
#if UNITY_EDITOR

#elif UNITY_ANDROID

#endif  
        if(Input.touchCount < 1) return; 
        
        Touch touch = Input.GetTouch(0);
        
        //UI handle
        if(touch.phase == TouchPhase.Ended)
        {
            if(selectedCard != null || selectedStone != null)
            {
                informPanel.gameObject.SetActive(false);
            }
        }

        //Stone related input handle
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

                if(selectedStone != null)
                {
                    if(isOpenStoneInform) 
                    {
                        //Open Information about selected stone
                        SetInformPanel(selectedStone.CardData);
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
        }
        else
        {
            Vector3 curTouchPosition = Camera.main.ScreenToWorldPoint(touch.position);
            Vector3 curTouchPositionNormalized = new Vector3(curTouchPosition.x, 0f, curTouchPosition.z);
            Vector3 moveVec;
            bool isTouchOnCancel = RectTransformUtility.RectangleContainsScreenPoint(cancelPanel, touch.position, null);
            isDragging = !isTouchOnCancel;
            if(touch.phase == TouchPhase.Began)
            {
                dragStartPoint = curTouchPositionNormalized;
                startOnCancel = isTouchOnCancel;
                if(isDragging)
                {
                    dragEffectObj.gameObject.SetActive(true);
                    dragEffectObj.SetPosition(0, curTouchPositionNormalized);
                    dragEffectObj.SetPosition(1, curTouchPositionNormalized);
                } 
            }

            if(touch.phase == TouchPhase.Moved && !startOnCancel)
            {
                dragEffectObj.gameObject.SetActive(false);
                isDragging = !isTouchOnCancel;

                dragEndPoint = curTouchPositionNormalized;
                moveVec = dragStartPoint - dragEndPoint;

                if(isDragging && moveVec.magnitude >= maxDragLimit)
                {
                    dragEffectObj.gameObject.SetActive(true);
                    dragEffectObj.SetPosition(1, dragStartPoint - moveVec.normalized * maxDragLimit);
                }
                else if(isDragging)
                {
                    dragEffectObj.gameObject.SetActive(true);
                    dragEffectObj.SetPosition(1, curTouchPositionNormalized);
                }
            }                

            if(touch.phase == TouchPhase.Ended) 
            {
                dragEndPoint = curTouchPositionNormalized;
                moveVec = dragStartPoint - dragEndPoint;
                
                cancelPanel.gameObject.SetActive(false);
                
                if(isDragging && !startOnCancel) 
                {
                    ShootStone( 2 * moveVec.normalized * Mathf.Min(moveVec.magnitude, maxDragLimit));   
                }
                selectedStone = null;

                dragEffectObj.gameObject.SetActive(false);
            }
        }
    
        //Card related input handle
        if(touch.phase == TouchPhase.Ended)
        {
            selectedCard = GetCardAroundPoint(touch.position);
            if(selectedCard != null)
            {
                SetInformPanel(selectedCard.CardData);
                informPanel.gameObject.SetActive(true);
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

    private Card GetCardAroundPoint(Vector3 point)
    {
        Ray ray = Camera.main.ScreenPointToRay(point);
        RaycastHit hit;
        if(Physics.Raycast(ray, out hit))
        {
            if(hit.transform.CompareTag("Card"))
            {
                return hit.transform.GetComponent<Card>();
            }
        }
        return null;
    }

    private void ShootStone(Vector3 vec)
    {
        selectedStone.GetComponent<AkgRigidbody>().AddForce(vec);
        // Debug.Log(vec);
    }

    private void SetInformPanel(CardData data)
    {
        //sprite
        informPanel.GetChild(0).GetComponent<Image>().sprite = data.sprite;
        //size
        informPanel.GetChild(1).GetComponent<TextMeshProUGUI>().text = "Size : " + data.stoneSize.ToString();
        //weight
        informPanel.GetChild(2).GetComponent<TextMeshProUGUI>().text = "Weight : " + data.stoneWeight.ToString();
        //description
        informPanel.GetChild(3).GetComponent<TextMeshProUGUI>().text = data.description.ToString();
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
