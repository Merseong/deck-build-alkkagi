using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerBehaviour : MonoBehaviour
{
    //Temp
    public GameObject Stone;

    [SerializeField] private bool isLocalPlayer = true;
    
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
    private bool isOpenStoneInform = false;
    
    
    private float curStoneSelectionTime;

    [SerializeField] private bool isDragging = false;
    [SerializeField] private bool startOnCancel;    
    private Vector3 dragStartPoint;
    private Vector3 dragEndPoint;
    private ArrowGenerator stoneArrowObj;
    

    [Header("SelectionVariable")]
    [SerializeField] private float stoneSelectionThreshold = 1f;
    
    [Header("ShootVelocityDecider")]
    [SerializeField] private int minShootVelocity;
    [SerializeField] private int maxShootVelocity;
    [SerializeField] private float velocityMultiplier;

    [Header("DragEffect")]
    [SerializeField] private LineRenderer dragEffectObj;
    [SerializeField] private float maxDragLimit;
    [SerializeField] private Color dragStartColor;
    [SerializeField] private Color dragEndColor;
    [SerializeField] private AnimationCurve dragColorCurve;
    [Range(0f, 1.0f)][SerializeField] private float alphaOnCancel;

    [Header("DebugTools"), SerializeField]
    private bool pauseEditorOnShoot = false;



    private void Start()
    {
        cancelPanel = GameObject.Find("InformPanel").GetComponent<RectTransform>();
        informPanel = GameObject.Find("CancelPanel").GetComponent<RectTransform>();
    }

    private void Update()
    {
        // if(!isLocalPlayer) return;
        NormalTurnInputHandler();
    }

    private void NormalTurnInputHandler()
    {
        bool isTouchBeginning, isTouching, isTouchEnded;
        Vector3 curScreenTouchPosition, curTouchPosition, curTouchPositionNormalized, moveVec;

//플랫폼별 테스트를 위한 분기 코드        
#if     UNITY_EDITOR
        
        isTouchBeginning = Input.GetMouseButtonDown(0);
        isTouching = Input.GetMouseButton(0);
        isTouchEnded = Input.GetMouseButtonUp(0);
        
        if(!isTouchBeginning && !isTouching && !isTouchEnded) return;

        curTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        curTouchPositionNormalized = new Vector3(curTouchPosition.x, 0f, curTouchPosition.z);
        curScreenTouchPosition = Input.mousePosition;

#elif   UNITY_ANDROID

        if(Input.touchCount < 1) return; 
        Touch touch = Input.GetTouch(0);

        isTouchBeginning = touch.phase == TouchPhase.Began;
        isTouching = touch.phase == TouchPhase.Moved;
        isTouchEnded = touch.phase == TouchPhase.Ended;

        curTouchPosition = Camera.main.ScreenToWorldPoint(touch.position);
        curTouchPositionNormalized = new Vector3(curTouchPosition.x, 0f, curTouchPosition.z);
        curScreenTouchPosition = touch.position;

#endif   
        
        //UI handle
        if(isTouchEnded)
        {
            if(selectedCard != null || selectedStone != null)
            {
                informPanel.gameObject.SetActive(false);
                // return;
            }
        }

        //Temp put the stone
        GameBoard isMousePointOnBoard = IsMouseOnBoard(curScreenTouchPosition);

        if (isMousePointOnBoard != null)
        {
            Vector3 nearbyPos = isMousePointOnBoard.GiveNearbyPos(curTouchPositionNormalized, 1);
            if (nearbyPos != isMousePointOnBoard.isNullPos)
            {
                // 투명돌 생성

                if (isTouchEnded)
                {
                    if (isMousePointOnBoard.IsPossibleToPut(nearbyPos, 1))
                    {
                        Instantiate(Stone, nearbyPos, new Quaternion(0, 0, 0, 0));
                    }
                }
            }
        }

        //Stone related input handle
        if (selectedStone == null)
        {
            if(isTouchBeginning)
            {
                isSelecting = true;
                StartCoroutine(EStoneSelection());
            }

            if(isTouchEnded)
            {
                isSelecting = false;
                selectedStone = GetStoneAroundPoint(curScreenTouchPosition);

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
            bool isTouchOnCancel = RectTransformUtility.RectangleContainsScreenPoint(cancelPanel, curScreenTouchPosition, null);
            isDragging = !isTouchOnCancel;
            if(isTouchBeginning)
            {
                dragStartPoint = curTouchPositionNormalized;
                startOnCancel = isTouchOnCancel;

                if(isDragging)
                {
                    dragEffectObj.gameObject.SetActive(true);
                    dragEffectObj.SetPosition(0, curTouchPositionNormalized);
                    dragEffectObj.SetPosition(1, curTouchPositionNormalized);

                    stoneArrowObj = selectedStone.transform.GetChild(0).GetComponent<ArrowGenerator>();
                    stoneArrowObj.gameObject.SetActive(true);
                } 
            }

            if(isTouching && !startOnCancel)
            {
                isDragging = !isTouchOnCancel;

                dragEndPoint = curTouchPositionNormalized;
                moveVec = dragStartPoint - dragEndPoint;

                dragEffectObj.startColor = Color.Lerp(dragStartColor, dragEndColor, dragColorCurve.Evaluate(Mathf.Min(moveVec.magnitude, maxDragLimit)/maxDragLimit));
                dragEffectObj.endColor = dragEffectObj.startColor;
                stoneArrowObj.GetComponent<MeshRenderer>().material.color = dragEffectObj.startColor;

                if(moveVec.magnitude >= maxDragLimit) 
                {
                    dragEffectObj.SetPosition(1, dragStartPoint - moveVec.normalized * maxDragLimit);
                    stoneArrowObj.stemLength = maxDragLimit;
                }
                else
                {    
                    dragEffectObj.SetPosition(1, curTouchPositionNormalized);
                    stoneArrowObj.stemLength = moveVec.magnitude;
                }
                
                if(moveVec.z >= 0) 
                {
                    float angle = Mathf.Acos(Vector3.Dot(Vector3.left, moveVec.normalized)) * 180 / Mathf.PI + 180;
                    stoneArrowObj.transform.rotation = Quaternion.Euler(90, angle, 0);
                }
                else
                {
                    float angle = Mathf.Acos(Vector3.Dot(Vector3.right, moveVec.normalized)) * 180 / Mathf.PI;
                    stoneArrowObj.transform.rotation = Quaternion.Euler(90, angle, 0);
                }
                
                if(!isDragging)
                {
                    Color temp = dragEffectObj.startColor;
                    temp.a = alphaOnCancel;
                    dragEffectObj.startColor = temp;
                    dragEffectObj.endColor = temp;
                    stoneArrowObj.GetComponent<MeshRenderer>().material.color = temp;
                }
            }

            if(isTouchEnded) 
            {
                dragEndPoint = curTouchPositionNormalized;
                moveVec = dragStartPoint - dragEndPoint;
                
                cancelPanel.gameObject.SetActive(false);
                
                if(isDragging && !startOnCancel) 
                {
                    //FIXME: Same velocity for every stone, set min max velocity for shooting (different form dragLimit)
                    float VelocityCalc = Mathf.Lerp(minShootVelocity, maxShootVelocity, Mathf.Min(moveVec.magnitude, maxDragLimit) / maxDragLimit) * velocityMultiplier;
                    ShootStone( moveVec.normalized * selectedStone.GetComponent<AkgRigidbody>().mass * VelocityCalc);   
                }
                selectedStone = null;

                dragEffectObj.gameObject.SetActive(false);
                stoneArrowObj.gameObject.SetActive(false);
            }
        }
    
        //Card related input handle
        if(isTouchEnded)
        {
            selectedCard = GetCardAroundPoint(curScreenTouchPosition);
            if(selectedCard != null)
            {
                SetInformPanel(selectedCard.CardData);
                informPanel.gameObject.SetActive(true);
            }
        }
    }
    private GameBoard IsMouseOnBoard(Vector3 point)
    {
        Ray ray = Camera.main.ScreenPointToRay(point);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.transform.CompareTag("Board"))
            {
                return hit.transform.GetComponent<GameBoard>();
            }
        }
        return null;
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
        if (pauseEditorOnShoot) UnityEditor.EditorApplication.isPaused = true;
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
