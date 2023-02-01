using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class PlayerBehaviour : MonoBehaviour
{
    //Temp
    public GameObject Stone;

    // 나와 적한테 하나씩 붙임

    [SerializeField] private bool isLocalPlayer = true;
    
    //TODO : should move to UIManager
    [SerializeField] private RectTransform cancelPanel;
    [SerializeField] private RectTransform informPanel;
    [SerializeField] private TextMeshProUGUI costTextUi;

    [SerializeField] private int cost;
    public int Cost
    {
        get => cost;

        private set
        {
            if (value < 0)
                Debug.LogError("Cost is less than 0! Fix this!");

            costTextUi.text = value.ToString();
            cost = value;
        }
    }

    [SerializeField] private Card[] deck;
    [SerializeField] private Card[] hand;

    [SerializeField] private StoneBehaviour[] stones;

    // 이거는 상황 봐서 액션 자체에 대한 클래스를 만들어서 HistoryAction 클래스랑 합칠 수도 있음
    private delegate void ActionDelegate();
    [SerializeField] private ActionDelegate actionQueue;
    [SerializeField] private HistoryAction[] hitory;

    [SerializeField] private StoneBehaviour selectedStone;
    [SerializeField] private Card selectedCard;
    [SerializeField] private GameObject followedStone;

    private StateMachine stateMachine;
    public readonly Dictionary<GameManager.TurnState, Action<Vector3>[]> turnActionDic = new Dictionary<GameManager.TurnState, Action<Vector3>[]>();

    private bool isSelecting;
    private bool isOpenStoneInform = false;
    [SerializeField] private bool isInformOpened = false;
    
    private float curStoneSelectionTime;
    [SerializeField] private bool isDragging = false;
    [SerializeField] private bool startOnCancel;    
    private Vector3 dragStartPoint;
    private Vector3 dragEndPoint;
    [SerializeField] private ArrowGenerator stoneArrowObj;
    
    //FIXME :Temporarily get board script by inspector
    [SerializeField] private GameBoard gameBoard;


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
        turnActionDic.Add(GameManager.TurnState.PREPARE, new Action<Vector3>[]{PrepareTouchBegin, PrepareInTouch, PrepareTouchEnd});
        turnActionDic.Add(GameManager.TurnState.WAIT, new Action<Vector3>[]{WaitTouchBegin, WaitInTouch, WaitTouchEnd});  
        turnActionDic.Add(GameManager.TurnState.NORMAL, new Action<Vector3>[]{NormalTouchBegin, NormalInTouch, NormalTouchEnd});
        turnActionDic.Add(GameManager.TurnState.HONORSKIP, new Action<Vector3>[]{HonorskipTouchBegin, HonorskipInTouch, HonorskipTouchEnd});
        turnActionDic.Add(GameManager.TurnState.HSCONSENT, new Action<Vector3>[]{HSConsentTouchBegin, HSConsentInTouch, HSConsentTouchEnd});

        //FIXME : Later turn control by GameManager
        stateMachine = new StateMachine(this, GameManager.TurnState.NORMAL);
        stateMachine.OperateEnter();

        NetworkManager.Inst.AddReceiveDelegate(PlayCardReceiveNetworkAction);
    }

    private void Update()
    {
        // if(!isLocalPlayer) return;
        stateMachine.DoOperateUpdate();
    }

    // temp: AppQuit-Disable-Destroy 순이길래 적당히 넣음
    private void OnApplicationQuit()
    {
        NetworkManager.Inst.RemoveReceiveDelegate(PlayCardReceiveNetworkAction);
    }

    private bool IsTouchOnBoard(Vector3 point)
    {
        Ray ray = Camera.main.ScreenPointToRay(point);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.transform.CompareTag("Board") || hit.transform.CompareTag("PutMarker") || hit.transform.CompareTag("Card"))
            {
                return true;
            }
        }
        return false;
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

    // 여기서부터 액션들

    // 이건 사실 액션은 아님
    private void SpendCost(int i)
    {
        Cost -= i;
    }
    public void ResetCost()
    {
        if (GameManager.Inst.TurnCount == 0)
            Cost = GameManager.Inst.initialTurnCost;
        else
            Cost = GameManager.Inst.normalTurnCost;
    }

    private void DrawCards(int number)
    {
        // TODO
    }

    // 카드 내기; 하스스톤에서는 카드를 "내다"가 play인듯
    // TODO: 위치도 인자로 같이 받아서 하게
    private void PlayCard(Card card, Vector3 nearbyPos)
    {
        // TODO
        if (card.CardData.cardCost > Cost || !GameBoard.IsPossibleToPut(nearbyPos, 1f))
        {
            return;
        }

        // 끌어놓은 위치에 Stone 생성
        //FIXME : 카드에 맞는 스톤을 런타임에 생성해줘야 함
        followedStone = Instantiate(Stone, nearbyPos, Quaternion.identity);
        selectedCard = null;
        
        followedStone.GetComponent<StoneBehaviour>().SetCardData(card.CardData);

        Destroy(card.gameObject);
        SpendCost(card.CardData.cardCost);
        PlayCardSendNetworkAction(card);
    }

    // PlayCard 함수에서 사용되는 패킷 전송
    private void PlayCardSendNetworkAction(Card card)
    {
        Debug.Log($"[{NetworkManager.Inst.NetworkId}] PLAYCARD/ {card.CardData.CardID}");
        return; // temp: 일단 끊어둠
        NetworkManager.Inst.SendData(new MyNetworkData.MessagePacket
            {
                senderID = NetworkManager.Inst.NetworkId,
                message = "PLAYCARD/ (TODO: 카드, 내는위치, 이후코스트)",
            }, MyNetworkData.PacketType.ROOM_OPPONENT);
    }

    private void PlayCardReceiveNetworkAction(MyNetworkData.Packet packet)
    {
        if (packet.Type != (short)MyNetworkData.PacketType.ROOM_OPPONENT) return;

        var msg = MyNetworkData.MessagePacket.Deserialize(packet.Data);

        if (!msg.message.StartsWith("PLAYCARD/")) return;

        Debug.Log($"[{NetworkManager.Inst.NetworkId}] PLAYCARD/ ");
        return; // temp: 일단 끊어둠

        // parse message
        var dataArr = msg.message.Split(' ');
        GameManager.Inst.OppoPlayer.PlayCard(new Card(), Vector3.zero); // 카드 ID, 내는 위치를 통해 넣어야 함
        if (GameManager.Inst.OppoPlayer.Cost != Int16.Parse(dataArr[3]))
        {
            // 상대의 남은 코스트와 내가 계산한 코스트가 안맞음
            Debug.LogError("[OPPO] PLAYCARD cost not matched!");
            return;
        }
    }

    private void ShootStone(Vector3 vec) // vec이 velocity인지 force인지 명확하게 해야함
    {
        if (pauseEditorOnShoot) UnityEditor.EditorApplication.isPaused = true;
        selectedStone.GetComponent<AkgRigidbody>().AddForce(vec);
        // Debug.Log(vec);
    }

    private void PrepareTurnEnd()
    {
        // TODO
    }

    private void NormalTurnEnd()
    {
        // TODO
    }

    private void ConsentHS()
    {
        // TODO
    }

    private void HonorSkip()
    {
        // TODO
    }

    private void DenyHS()
    {
        // TODO
    }

    private void SetInformPanel(CardData data)
    {
        isInformOpened = true;
        //sprite
        informPanel.GetChild(0).GetComponent<Image>().sprite = data.sprite;
        //size
        informPanel.GetChild(1).GetComponent<TextMeshProUGUI>().text = "Size : " + data.stoneSize.ToString();
        //weight
        informPanel.GetChild(2).GetComponent<TextMeshProUGUI>().text = "Weight : " + data.stoneWeight.ToString();
        //description
        informPanel.GetChild(3).GetComponent<TextMeshProUGUI>().text = data.description.ToString();
    }

    private Vector3 ScreenPosToNormalized(Vector3 vec)
    {
        Vector3 curTouchPosition = Camera.main.ScreenToWorldPoint(vec);
        return new Vector3(curTouchPosition.x, 0f, curTouchPosition.z);
    }

    ///<summary>
    ///Arrange cards in hand regarding hand[]
    ///</summary>
    private void ArrangeHand()
    {
        Debug.Log("Rearrange Hand!");
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

#region TouchInputActions

    public void NormalTouchBegin(Vector3 curScreenTouchPosition)
    {
        Vector3 curTouchPosition = Camera.main.ScreenToWorldPoint(curScreenTouchPosition);
        Vector3 curTouchPositionNormalized = new Vector3(curTouchPosition.x, 0f, curTouchPosition.z);

        //Card
        selectedCard = GetCardAroundPoint(curScreenTouchPosition);
        if (selectedCard != null)
        {
            dragStartPoint = curTouchPositionNormalized;
            return;
        }

        if (selectedStone == null)
        {
            isSelecting = true;
            StartCoroutine(EStoneSelection());
        }
        else
        {
            bool isTouchOnCancel = RectTransformUtility.RectangleContainsScreenPoint(cancelPanel, curScreenTouchPosition, null);
            isDragging = !isTouchOnCancel;

            dragStartPoint = curTouchPositionNormalized;
            startOnCancel = isTouchOnCancel;

            if(isDragging && !isInformOpened)
            {
                dragEffectObj.gameObject.SetActive(true);
                dragEffectObj.SetPosition(0, curTouchPositionNormalized);
                dragEffectObj.SetPosition(1, curTouchPositionNormalized);

                stoneArrowObj = selectedStone.transform.GetChild(0).GetComponent<ArrowGenerator>();
                stoneArrowObj.gameObject.SetActive(true);
            }
        }
    }

    public void NormalInTouch(Vector3 curScreenTouchPosition)
    {
        Vector3 curTouchPosition = Camera.main.ScreenToWorldPoint(curScreenTouchPosition);
        Vector3 curTouchPositionNormalized = new Vector3(curTouchPosition.x, 0f, curTouchPosition.z);

        bool isTouchOnCancel = RectTransformUtility.RectangleContainsScreenPoint(cancelPanel, curScreenTouchPosition, null);
        isDragging = !isTouchOnCancel;
        
        if(selectedStone != null && !startOnCancel && !isInformOpened)
        {
            isDragging = !isTouchOnCancel;

            dragEndPoint = curTouchPositionNormalized;
            Vector3 moveVec = dragStartPoint - dragEndPoint;

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
    
        //Card Dragging
        if(selectedCard != null)
        {
            selectedCard.transform.position = curTouchPositionNormalized;
            GameBoard.HighlightPossiblePos(1, 1f);
        }
    }

    public void NormalTouchEnd(Vector3 curScreenTouchPosition)
    {
        Vector3 curTouchPosition = Camera.main.ScreenToWorldPoint(curScreenTouchPosition);
        Vector3 curTouchPositionNormalized = new Vector3(curTouchPosition.x, 0f, curTouchPosition.z);

        //UI handle
        if(isInformOpened)
        {
            informPanel.gameObject.SetActive(false);
            isInformOpened = false;
            selectedCard = null;
            selectedStone = null;
            return;
        }

        if (selectedCard != null)
        {
            if(IsTouchOnBoard(curScreenTouchPosition))
            {
                Vector3 nearbyPos = gameBoard.GiveNearbyPos(curTouchPositionNormalized, 1, 10f);
                if (nearbyPos != gameBoard.isNullPos)
                {
                    PlayCard(selectedCard, nearbyPos);
                }
                ArrangeHand();
                selectedCard = null;
                GameBoard.UnhightlightPossiblePos();
                return;
            }
            else if ((dragStartPoint - curTouchPositionNormalized).sqrMagnitude < 1.0f)
            {
                SetInformPanel(selectedCard.CardData);
                informPanel.gameObject.SetActive(true);
                return;
            }
        }

        if(selectedStone == null)
        {
            isSelecting = false;
            selectedStone = GetStoneAroundPoint(curScreenTouchPosition);

            if(selectedStone != null)
            {
                selectedStone.isClicked = true;
                if(isOpenStoneInform) 
                {
                    //Open Information about selected stone
                    SetInformPanel(selectedStone.CardData);
                    informPanel.gameObject.SetActive(true);
                    Debug.Log("Information");
                    return;
                }
                else
                {
                    //Simply select current stone and move to shooting phase
                    cancelPanel.gameObject.SetActive(true);
                    Debug.Log("Selected");
                    return;
                }
            }
        }
        else
        {
            dragEndPoint = curTouchPositionNormalized;
            Vector3 moveVec = dragStartPoint - dragEndPoint;
            
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
            return;
        }
    }

    public void PrepareTouchBegin(Vector3 curScreenTouchPosition)
    {

    }

    public void PrepareInTouch(Vector3 curScreenTouchPosition)
    {

    }

    public void PrepareTouchEnd(Vector3 curScreenTouchPosition)
    {

    }

    public void WaitTouchBegin(Vector3 curScreenTouchPosition)
    {

    }

    public void WaitInTouch(Vector3 curScreenTouchPosition)
    {

    }

    public void WaitTouchEnd(Vector3 curScreenTouchPosition)
    {

    }

    public void HonorskipTouchBegin(Vector3 curScreenTouchPosition)
    {

    }

    public void HonorskipInTouch(Vector3 curScreenTouchPosition)
    {

    }

    public void HonorskipTouchEnd(Vector3 curScreenTouchPosition)
    {

    }

    public void HSConsentTouchBegin(Vector3 curScreenTouchPosition)
    {

    }

    public void HSConsentInTouch(Vector3 curScreenTouchPosition)
    {

    }

    public void HSConsentTouchEnd(Vector3 curScreenTouchPosition)
    {

    }

#endregion TouchInputActions

}

