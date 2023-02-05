using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;

public class PlayerBehaviour : MonoBehaviour
{
    //Temp
    public GameObject StonePrefab;

    // 나와 적한테 하나씩 붙임

    [SerializeField] private bool isLocalPlayer = true;
    
    //TODO : should move to UIManager
    [SerializeField] private RectTransform cancelPanel;
    [SerializeField] private RectTransform informPanel;
    [SerializeField] private TextMeshProUGUI costTextUi;
    [SerializeField] private Image shootTokenImage;
    [SerializeField] private RectTransform enemyPanel;

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

    [SerializeField] private List<Card> deck;
    [SerializeField] private List<Card> hand;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] Transform cardSpawnPoint;
    [SerializeField] Transform handPileLeft;
    [SerializeField] Transform handPileRight;
    private int maxHandSize = 7;

    [SerializeField] private StoneBehaviour[] stones;
    [SerializeField] private bool shootTokenAvailable;
    public bool ShootTokenAvailable {
        get => shootTokenAvailable;
        set
        {
            shootTokenAvailable = value;

            ColorUtility.TryParseHtmlString(value ? "#C0FFBD" : "#FF8D91", out Color color);
            shootTokenImage.color = color;
        }
    }

    // 이거는 상황 봐서 액션 자체에 대한 클래스를 만들어서 HistoryAction 클래스랑 합칠 수도 있음
    private delegate void ActionDelegate();
    [SerializeField] private ActionDelegate actionQueue;
    [SerializeField] private HistoryAction[] hitory;

    [SerializeField] private StoneBehaviour selectedStone;
    [SerializeField] private Card selectedCard;

    private StateMachine stateMachine;
    public readonly Dictionary<GameManager.TurnState, Action<Vector3>[]> turnActionDic = new Dictionary<GameManager.TurnState, Action<Vector3>[]>();

    private bool isSelecting;
    private bool isOpenStoneInform = false;
    [SerializeField] private bool isInformOpened = false;

    private float curStoneSelectionActionTime;
    private bool isDragging = false;
    private bool reachedAvg = false;
    private bool leftHighEnd = false;
    private bool leftLowEnd = false;
    private bool startOnCancel;
    private Vector3 dragStartPoint;
    private Vector3 dragEndPoint;
    private float curDragMagnitude;
    private ArrowGenerator stoneArrowObj;
    
    //FIXME :Temporarily get board script by inspector
    [SerializeField] private GameBoard gameBoard;


    [Header("SelectionVariable")]
    [SerializeField] private float StoneSelectionActionThreshold = 1f;
    [SerializeField] private float cardDragThreshold = 1f;
    
    [Header("ShootVelocityDecider")]
    [SerializeField] private float minShootVelocity;
    [SerializeField] private float maxShootVelocity;
    [SerializeField] private float velocityMultiplier;

    [Header("DragEffect")]
    [SerializeField] private LineRenderer dragEffectObj;
    [SerializeField] private float maxDragLimit;
    [SerializeField][Tooltip ("Indicates clipping range plus-minus maxDragLimit's average")][Range(0f, 1.0f)]
    private float maxClippingPercentage;
    [SerializeField] private Color Cost1Color;
    [SerializeField] private Color Cost2Color;
    // [SerializeField] private AnimationCurve dragColorCurve;
    [Range(0f, 1.0f)][SerializeField] private float alphaOnCancel;

    [Header("DebugTools"), SerializeField]
    private bool pauseEditorOnShoot = false;

    private void Awake()
    {
        GameManager.Inst.SetPlayerData(() =>
        {
            GameManager.Inst.players[isLocalPlayer ? 0 : 1] = this;
        });
    }

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

        // temp:
        ShootTokenAvailable = true;
        cost = 9;
    }

    private void Update()
    {
        // if(!isLocalPlayer) return;
        stateMachine.DoOperateUpdate();

        //Temp test code for card draw
        if (Input.GetKeyDown(KeyCode.Q))
        {
            DrawCards(1);
        }
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
            if (hit.transform.CompareTag("Board") || hit.transform.CompareTag("PutMarker") || hit.transform.CompareTag("Card") || hit.transform.CompareTag("Guard"))
            {
                return true;
            }
        }
        return false;
    }

    private bool IsPlayingCardOnBoard(Vector3 point)
    {
        if(point.x < gameBoard.BoardData.width * 5 && point.x > -gameBoard.BoardData.width * 5 && point.z < gameBoard.BoardData.height * 5 && point.z > -gameBoard.BoardData.height * 5) return true;
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

    private float GetRadiusFromStoneSize(CardData.StoneSize size)
    {
        switch(size)
        {
            case CardData.StoneSize.Small:
                return .5f;
            
            case CardData.StoneSize.Medium:
                return .65f;

            case CardData.StoneSize.Large:
                return .8f;

            case CardData.StoneSize.SuperLarge:
                return .95f;

            default:
                Debug.Log("Invalid Stone Size!");
                return 1f;
        }
    }

    private float GetMassFromStoneWeight(CardData.StoneSize size, CardData.StoneWeight weight)
    {
        switch(weight)
        {
            case CardData.StoneWeight.Light:
                switch(size)
                {
                    case CardData.StoneSize.Small:
                        return .110f;
                    
                    case CardData.StoneSize.Medium:
                        return .115f;

                    case CardData.StoneSize.Large:
                        return .12f;

                    case CardData.StoneSize.SuperLarge:
                        return .13f;

                    default:
                        Debug.Log("Invalid Stone Size!");
                        return 0f;
                }

            case CardData.StoneWeight.Standard:
                switch(size)
                {
                    case CardData.StoneSize.Small:
                        return .12f;

                    case CardData.StoneSize.Medium:
                        return .13f;

                    case CardData.StoneSize.Large:
                        return .14f;

                    case CardData.StoneSize.SuperLarge:
                        return .16f;

                    default:
                        Debug.Log("Invalid Stone Size!");
                        return 0f;
                }
            
            case CardData.StoneWeight.Heavy:
                switch(size)
                {
                    case CardData.StoneSize.Small:
                        return .13f;

                    case CardData.StoneSize.Medium:
                        return .145f;

                    case CardData.StoneSize.Large:
                        return .16f;

                    case CardData.StoneSize.SuperLarge:
                        return .19f;

                    default:
                        Debug.Log("Invalid Stone Size!");
                        return 0f;
                }

            default:
                Debug.LogError("Invalid stone weight!");
                return 0f;
        }
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
        if (hand.Count + number > 7)
        {
            number = 7 - hand.Count;
        }
        if(deck.Count < number)
        {
            Debug.LogError("Card 부족");
            return;
        }
        if(number == 0)
        {
            Debug.LogError("손패가 가득 찼습니다!");
            return;
        }
        for (int i = 0; i < number; i++)
        {
            Card drawCard = deck[0];
            deck.RemoveAt(0);
            var cardObject = Instantiate(cardPrefab, cardSpawnPoint.position, Quaternion.identity);
            var card = cardObject.GetComponent<Card>();
            card.Setup(drawCard);
            hand.Add(card);

            SetOriginOrder();
        }
        ArrangeHand(true);
    }

    private void SetOriginOrder()
    {
        int count = hand.Count;
        for (int i = 0; i < count; i++)
        {
            var targetCard = hand[i];
            targetCard?.GetComponent<Card>().SetOriginOrder(i);
        }
    }

    List<RPS> RoundAlignment(Transform leftTr, Transform rightTr, int objCount, float height, Vector3 scale)
    {
        float[] objLerps = new float[objCount];
        List<RPS> results = new List<RPS>(objCount);

        switch (objCount)
        {
            case 1: objLerps = new float[] { 0.5f }; break;
            case 2: objLerps = new float[] { 0.27f, 0.73f }; break;
            case 3: objLerps = new float[] { 0.1f, 0.5f, 0.9f }; break;
            default:
                float interval = 1f / (objCount - 1);
                for (int i = 0; i < objCount; i++)
                {
                    objLerps[i] = interval * i;
                }
                break;
        }


        for (int i = 0; i < objCount; i++)
        {
            var targetPos = Vector3.Lerp(leftTr.position, rightTr.position, objLerps[i]);
            var targetRot = Quaternion.identity;
            if (objCount >= 4)
            {
                float curve = Mathf.Sqrt(Mathf.Pow(height, 2) - Mathf.Pow(objLerps[i] - 0.5f, 2));
                curve = height >= 0 ? curve : -curve;
                targetPos.z += curve;
                targetRot = Quaternion.Slerp(leftTr.rotation, rightTr.rotation, objLerps[i]);
            }
            results.Add(new RPS(targetPos, targetRot, scale));
        }
        return results;
    }

    // 카드 내기; 하스스톤에서는 카드를 "내다"가 play인듯
    // TODO: 위치도 인자로 같이 받아서 하게
    private void PlayCard(Card card, Vector3 nearbyPos, int oppoStoneId = -1)
    {
        
        if (card.CardData.cardCost > Cost || !GameBoard.IsPossibleToPut(nearbyPos, GetRadiusFromStoneSize(card.CardData.stoneSize)))
        {
            return;
        }

        // 끌어놓은 위치에 Stone 생성
        //FIXME : 카드에 맞는 스톤을 런타임에 생성해줘야 함
        GameObject spawnedStone = Instantiate(StonePrefab, nearbyPos, Quaternion.identity);
        selectedCard = null;

        var stoneBehaviour = spawnedStone.GetComponent<StoneBehaviour>();
        var stoneId = GameManager.Inst.AddStone(stoneBehaviour, isLocalPlayer, oppoStoneId);
        stoneBehaviour.SetCardData(card.CardData, stoneId, isLocalPlayer);

        //temp code
        spawnedStone.transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = card.CardData.sprite;
        spawnedStone.transform.localScale = new Vector3(GetRadiusFromStoneSize(card.CardData.stoneSize), .15f, GetRadiusFromStoneSize(card.CardData.stoneSize));
        spawnedStone.GetComponent<AkgRigidbody>().mass = GetMassFromStoneWeight(card.CardData.stoneSize, card.CardData.stoneWeight);

        hand.Remove(card);
        Destroy(card.gameObject);
        SpendCost(card.CardData.cardCost);
        PlayCardSendNetworkAction(card, nearbyPos, stoneId);
        ArrangeHand(false);
    }

    /// <summary>
    /// CardData만으로도 낼 수 있도록 (enemy에서 사용)
    /// </summary>
    /// <param name="cardData"></param>
    /// <param name="nearbyPos"></param>
    /// <param name="oppoStoneId"></param>
    private void PlayCard(CardData cardData, Vector3 nearbyPos, int oppoStoneId = -1)
    {
        if (cardData.cardCost > Cost || !GameBoard.IsPossibleToPut(nearbyPos, GetRadiusFromStoneSize(cardData.stoneSize)))
        {
            return;
        }

        GameObject spawnedStone = Instantiate(StonePrefab, nearbyPos, Quaternion.identity);
        var stoneBehaviour = spawnedStone.GetComponent<StoneBehaviour>();
        var stoneId = GameManager.Inst.AddStone(stoneBehaviour, isLocalPlayer, oppoStoneId);
        stoneBehaviour.SetCardData(cardData, stoneId, isLocalPlayer);

        //temp code
        spawnedStone.transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = cardData.sprite;
        spawnedStone.transform.localScale = new Vector3(GetRadiusFromStoneSize(cardData.stoneSize), .15f, GetRadiusFromStoneSize(cardData.stoneSize));
        spawnedStone.GetComponent<AkgRigidbody>().mass = GetMassFromStoneWeight(cardData.stoneSize, cardData.stoneWeight);
    }

    // PlayCard 함수에서 사용되는 패킷 전송
    private void PlayCardSendNetworkAction(Card card, Vector3 position, int stoneId)
    {
        Debug.Log($"[{NetworkManager.Inst.NetworkId}] PLAYCARD/ {card.CardData.CardID} {position} {stoneId} {Cost}");

        NetworkManager.Inst.SendData(new MyNetworkData.MessagePacket
            {
                senderID = NetworkManager.Inst.NetworkId,
                message = $"PLAYCARD/ {card.CardData.CardID} {Vector3ToString(position)} {stoneId} {Cost}",
            }, MyNetworkData.PacketType.ROOM_OPPONENT);
    }

    private void PlayCardReceiveNetworkAction(MyNetworkData.Packet packet)
    {
        if (packet.Type != (short)MyNetworkData.PacketType.ROOM_OPPONENT) return;

        var msg = MyNetworkData.MessagePacket.Deserialize(packet.Data);

        if (!msg.message.StartsWith("PLAYCARD/")) return;

        Debug.Log($"[OPPO] {msg.message}");

        // parse message
        var dataArr = msg.message.Split(' ');
        // TODO: Oppo의 playcard를 불러야하긴한데
        PlayCard
        (
            GameManager.Inst.GetCardDataById(int.Parse(dataArr[1])),
            StringToVector3(dataArr[2]),
            Int16.Parse(dataArr[3])
        );
        //if (GameManager.Inst.OppoPlayer.Cost != Int16.Parse(dataArr[4]))
        //{
        //    // 상대의 남은 코스트와 내가 계산한 코스트가 안맞음
        //    Debug.LogError("[OPPO] PLAYCARD cost not matched!");
        //    return;
        //}
    }

    private Vector3 StringToVector3(string vec3)
    {
        var stringList = vec3.Split('|');
        return new Vector3(float.Parse(stringList[1]), float.Parse(stringList[2]), float.Parse(stringList[3]));
    }

    private string Vector3ToString(Vector3 vec3)
    {
        return $"|{vec3.x}|{vec3.y}|{vec3.z}|";
    }

    private void ShootStone(Vector3 vec) // vec이 velocity인지 force인지 명확하게 해야함
    {
        // shoot token이 없는 경우, 쏘지 못하게 리셋
        // if (!ShootTokenAvailable)
        // {
        //     Debug.LogWarning("공격토큰이 존재하지 않습니다.");
        //     return;
        // }

        if (pauseEditorOnShoot) UnityEditor.EditorApplication.isPaused = true;

        StartCoroutine(EShootStone());

        selectedStone.GetComponent<AkgRigidbody>().AddForce(vec);
        ShootTokenAvailable = false;
    }

    private IEnumerator EShootStone()
    {
        var recorder = GameManager.Inst.rigidbodyRecorder;
        recorder.StartRecord(Time.time);

        yield return null;
        bool isAllStoneStop = false;

        while (!isAllStoneStop)
        {
            yield return null;
            yield return new WaitUntil(() =>
            {
                return GameManager.Inst.LocalStones.Count == 0 ||
                    GameManager.Inst.LocalStones.Values.All(x => !x.isMoving);
            });
            yield return new WaitUntil(() =>
            {
                return GameManager.Inst.OppoStones.Count == 0 ||
                    GameManager.Inst.OppoStones.Values.All(x => !x.isMoving);
            });

            isAllStoneStop = true;
        }

        // send physics records, stone final poses, event list
        recorder.EndRecord(out var velocityRecords, out var eventRecords);
        recorder.SendRecord(velocityRecords, eventRecords);
    }

    private void PrepareTurnEnd()
    {
        // TODO
    }

    private void NormalTurnEnd()
    {
        // TODO
    }

    private void SetInformPanel(CardData data)
    {
        GameManager.Inst.isInformOpened = isInformOpened = true;
        //sprite
        informPanel.GetChild(0).GetComponent<Image>().sprite = data.sprite;
        //size
        informPanel.GetChild(1).GetComponent<TextMeshProUGUI>().text = "Size : " + data.stoneSize.ToString();
        //weight
        informPanel.GetChild(2).GetComponent<TextMeshProUGUI>().text = "Weight : " + data.stoneWeight.ToString();
        //description
        informPanel.GetChild(3).GetComponent<TextMeshProUGUI>().text = data.description.ToString();
    }

    private void SetEnemyInfoPanel()
    {
        if (isLocalPlayer) return;

        // enemy cost
        enemyPanel.GetChild(1).GetComponent<TextMeshProUGUI>().text = Cost.ToString();

        // enemy card count
        enemyPanel.GetChild(3).GetComponent<TextMeshProUGUI>().text = hand.Count.ToString();
    }

    private Vector3 ScreenPosToNormalized(Vector3 vec)
    {
        Vector3 curTouchPosition = Camera.main.ScreenToWorldPoint(vec);
        return new Vector3(curTouchPosition.x, 0f, curTouchPosition.z);
    }

    ///<summary>
    ///Arrange cards in hand regarding card index at hand[]<br></br>
    ///</summary>
    ///<param name = "isDrawPhase">true인 경우 부드럽게 손패로 이동, false일 경우 텀 없이 바로 손패로 이동</param>
    private void ArrangeHand(bool isDrawPhase)
    {
        List<RPS> originCardRPSs = new List<RPS>();
        originCardRPSs = RoundAlignment(handPileLeft, handPileRight, hand.Count, 0.5f, new Vector3(2.0f, 0.5f, 3.0f));

        var targetCards = hand;

        for (int i = 0; i < targetCards.Count; i++)
        {
            var targetCard = targetCards[i];

            targetCard.originRPS = originCardRPSs[i];
            if(isDrawPhase)
                targetCard.MoveTransform(targetCard.originRPS, true, 0.7f);
            else
                targetCard.MoveTransform(targetCard.originRPS, false, 0.7f);
        }
    }

    private IEnumerator EStoneSelectionAction()
    {
        isOpenStoneInform = false;
        curStoneSelectionActionTime = StoneSelectionActionThreshold;
        while(curStoneSelectionActionTime >= 0)
        {
            curStoneSelectionActionTime -= Time.deltaTime;
            if(!isSelecting) yield break;
            yield return null;
        }
        isOpenStoneInform = true;
    }

    // 드래그중 토큰이 강조되는 애니메이션
    private IEnumerator EShootTokenAlert()
    {
        float time;
        RectTransform alertRect = shootTokenImage.transform.GetChild(0).GetComponent<RectTransform>();
        alertRect.gameObject.SetActive(true);

        while (isDragging)
        {
            time = 0;
            while (isDragging && time < 1f)
            {
                alertRect.localScale = new Vector3(1 + time / 2, 1 + time / 2, 1);
                time += Time.deltaTime;
                yield return null;
            }
        }

        alertRect.gameObject.SetActive(false);
    }

#region TouchInputActions

    public void NormalTouchBegin(Vector3 curScreenTouchPosition)
    {
        Vector3 curTouchPosition = Camera.main.ScreenToWorldPoint(curScreenTouchPosition);
        Vector3 curTouchPositionNormalized = new Vector3(curTouchPosition.x, 0f, curTouchPosition.z);
        dragStartPoint = curTouchPositionNormalized;

        //Card
        selectedCard = GetCardAroundPoint(curScreenTouchPosition);

        if(selectedStone == null)
        {
            isSelecting = true;
            StartCoroutine(EStoneSelectionAction());
            return;
        }
        
        if(selectedStone != null)
        {
            StoneDragAction_Begin(curScreenTouchPosition, curTouchPositionNormalized);
            return;
        }
    }

    public void NormalInTouch(Vector3 curScreenTouchPosition)
    {
        if(isInformOpened) return;

        Vector3 curTouchPosition = Camera.main.ScreenToWorldPoint(curScreenTouchPosition);
        Vector3 curTouchPositionNormalized = new Vector3(curTouchPosition.x, 0f, curTouchPosition.z);

        //Stone Dragging for shoot
        if(selectedStone != null && !startOnCancel && (TouchManager.Inst.GetTouchDelta().x != 0 || TouchManager.Inst.GetTouchDelta().y != 0))
        {
            StoneDragAction(curScreenTouchPosition, curTouchPositionNormalized);
        }
    
        //Card Dragging for play card
        if(selectedCard != null)
        {
            CardDragAction(curTouchPositionNormalized);
        }
    }

    public void NormalTouchEnd(Vector3 curScreenTouchPosition)
    {
        Vector3 curTouchPosition = Camera.main.ScreenToWorldPoint(curScreenTouchPosition);
        Vector3 curTouchPositionNormalized = new Vector3(curTouchPosition.x, 0f, curTouchPosition.z);

        //UI handle
        if(isInformOpened)
        {
            UICloseAction();
            return;
        }

        if(selectedStone == null)
        {
            StoneSelectionAction(true, curScreenTouchPosition);
        }
        else
        {
            StoneShootAction(curTouchPositionNormalized);
            return;
        }

        if (selectedCard != null)
        {
            if ((dragStartPoint - curTouchPositionNormalized).magnitude < cardDragThreshold)
            {
                CardSelectionAction();
            }
            else if(IsTouchOnBoard(curScreenTouchPosition))
            {
                CardPlayAction(curTouchPositionNormalized);
            }
        }
    }

    public void PrepareTouchBegin(Vector3 curScreenTouchPosition)
    {
        Vector3 curTouchPosition = Camera.main.ScreenToWorldPoint(curScreenTouchPosition);
        Vector3 curTouchPositionNormalized = new Vector3(curTouchPosition.x, 0f, curTouchPosition.z);
        dragStartPoint = curTouchPositionNormalized;

        //Card
        selectedCard = GetCardAroundPoint(curScreenTouchPosition);
    }

    public void PrepareInTouch(Vector3 curScreenTouchPosition)
    {
        if(isInformOpened) return;

        Vector3 curTouchPosition = Camera.main.ScreenToWorldPoint(curScreenTouchPosition);
        Vector3 curTouchPositionNormalized = new Vector3(curTouchPosition.x, 0f, curTouchPosition.z);
        
        //Card Dragging for play card
        if(selectedCard != null)
        {
            CardDragAction(curTouchPositionNormalized);
        }
    }

    public void PrepareTouchEnd(Vector3 curScreenTouchPosition)
    {
        Vector3 curTouchPosition = Camera.main.ScreenToWorldPoint(curScreenTouchPosition);
        Vector3 curTouchPositionNormalized = new Vector3(curTouchPosition.x, 0f, curTouchPosition.z);

        //UI handle
        if(isInformOpened)
        {
            UICloseAction();
            return;
        }

        if(selectedStone == null)
        {
            StoneSelectionAction(false, curScreenTouchPosition);
        }
                
        if (selectedCard != null)
        {
            if ((dragStartPoint - curTouchPositionNormalized).magnitude < cardDragThreshold)
            {
                CardSelectionAction();
                return;
            }
            
            if(IsTouchOnBoard(curScreenTouchPosition))
            {
                CardPlayAction(curTouchPositionNormalized);
                return;
            }
        }
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

#region TouchBeginActionSet

    private void StoneDragAction_Begin(Vector3 curScreenTouchPosition, Vector3 curTouchPositionNormalized)
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

#endregion TouchEndActionSet


#region InTouchActionSet

    private void StoneDragAction(Vector3 curScreenTouchPosition, Vector3 curTouchPositionNormalized)
    {
        bool isTouchOnCancel = RectTransformUtility.RectangleContainsScreenPoint(cancelPanel, curScreenTouchPosition, null);
        isDragging = !isTouchOnCancel;

        isDragging = !isTouchOnCancel;
        StartCoroutine(EShootTokenAlert());

        dragEndPoint = curTouchPositionNormalized;
        Vector3 moveVec = dragStartPoint - dragEndPoint;
        Color dragColor;
        Vector3 deltaVec = new Vector3(TouchManager.Inst.GetTouchDelta().x, 0, TouchManager.Inst.GetTouchDelta().y);

        if(moveVec.magnitude >= maxDragLimit) 
        {
            curDragMagnitude = maxDragLimit;
            dragColor = Cost2Color;
        }
        else
        {
            float previous = curDragMagnitude;
            float avg = maxDragLimit / 2;
            float cur = Mathf.Min(moveVec.magnitude, maxDragLimit);

            if((avg > previous && avg <= cur) || (avg < previous && avg >= cur))
            {
                reachedAvg = true;
            } 
            else if(cur <= avg * (1f - maxClippingPercentage))
            {
                leftLowEnd = true;
                reachedAvg = false;
            }
            else if(cur >= avg * (1f + maxClippingPercentage))
            {
                leftHighEnd = true;
                reachedAvg = false;
            }
            else
            {
                leftHighEnd = false;
                leftLowEnd = false;
            }

            curDragMagnitude = moveVec.magnitude;
            //Decreasing Power
            if(cur < avg && cur > avg * (1f - maxClippingPercentage)  && reachedAvg && (!leftLowEnd || Vector3.Dot(deltaVec, moveVec) > 0))
            {
                curDragMagnitude = maxDragLimit / 2;
                dragColor = Cost1Color;
            }
            //Increasing Power
            else if(cur > avg && cur < avg * (1f + maxClippingPercentage) && reachedAvg && (!leftHighEnd || Vector3.Dot(deltaVec, moveVec) < 0))
            {
                curDragMagnitude = maxDragLimit / 2;
                dragColor = Cost1Color;
            }
            else
            {
                if(moveVec.magnitude > maxDragLimit / 2) dragColor = Cost2Color;
                else dragColor = Cost1Color;
            }
        }

        //dragEffectObj.endColor = dragEffectObj.startColor = Color.Lerp(Cost1Color, Cost2Color, dragColorCurve.Evaluate(Mathf.Min(moveVec.magnitude, maxDragLimit)/maxDragLimit));
        dragEffectObj.endColor = dragEffectObj.startColor = dragColor;
        stoneArrowObj.GetComponent<MeshRenderer>().material.color = dragEffectObj.startColor;

        dragEffectObj.SetPosition(1, dragStartPoint - moveVec.normalized * curDragMagnitude);
        stoneArrowObj.stemLength = curDragMagnitude;
        
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

    private void CardDragAction(Vector3 curTouchPositionNormalized)
    {
        if(IsPlayingCardOnBoard(curTouchPositionNormalized))
        {
            //TODO : 스톤 미리보기 추가해주어야 함
            selectedCard.GetComponent<MeshRenderer>().enabled = false;
            GameBoard.HighlightPossiblePos(1, 1f);
        }
        else
        {
            selectedCard.GetComponent<MeshRenderer>().enabled = true;
            GameBoard.UnhightlightPossiblePos();
        }
        selectedCard.transform.position = new Vector3(curTouchPositionNormalized.x, 5f, curTouchPositionNormalized.z);
        
    }


#endregion InTouchActionSet


#region TouchEndActionSet

    private void UICloseAction()
    {
        informPanel.gameObject.SetActive(false);
        GameManager.Inst.isInformOpened = isInformOpened = false;
        selectedCard = null;
        selectedStone = null;
        GameBoard.UnhightlightPossiblePos();
    }

    private void StoneSelectionAction(bool canShoot, Vector3 curScreenTouchPosition)
    {
        isSelecting = false;
        selectedStone = GetStoneAroundPoint(curScreenTouchPosition);

        if(selectedStone != null)
        {
            selectedStone.isClicked = true;
            if(canShoot && !isOpenStoneInform)
            {
                //Simply select current stone and move to shooting phase
                GameManager.Inst.isCancelOpened = true;
                cancelPanel.gameObject.SetActive(true);
                // Debug.Log("Selected");
                return;
            }
            else
            {
                //Open Information about selected stone
                SetInformPanel(selectedStone.CardData);
                informPanel.gameObject.SetActive(true);
                // Debug.Log("Information");
                return;
            }
        }
    }

    private void StoneShootAction(Vector3 curTouchPositionNormalized)
    {

        dragEndPoint = curTouchPositionNormalized;
        Vector3 moveVec = dragStartPoint - dragEndPoint;
        
        GameManager.Inst.isCancelOpened = false;
        cancelPanel.gameObject.SetActive(false);
        
        dragEffectObj?.gameObject.SetActive(false);
        stoneArrowObj?.gameObject.SetActive(false);
        
        if(isDragging && !startOnCancel) 
        {
            int needCost = curDragMagnitude <= maxDragLimit / 2 ? 1 : 2;

            if(cost < needCost)
            {
                //Not enough cost for shooting
                isDragging = false;
                selectedStone = null;
                Debug.LogError("You have not enough cost for shooting stone!");
                return;
            }

            SpendCost(needCost);
            float VelocityCalc = Mathf.Lerp(minShootVelocity, maxShootVelocity, Mathf.Min(moveVec.magnitude, maxDragLimit) / maxDragLimit) * velocityMultiplier;
            ShootStone( moveVec.normalized * selectedStone.GetComponent<AkgRigidbody>().mass * VelocityCalc);
        }

        // 여기넣는게 맞는지 모름
        isDragging = false;

        selectedStone = null;
    }

    private void CardPlayAction(Vector3 curTouchPositionNormalized)
    {
        selectedCard.GetComponent<MeshRenderer>().enabled = true;
        Vector3 nearbyPos = gameBoard.GiveNearbyPos(curTouchPositionNormalized, 1, 10f);
        if (nearbyPos != gameBoard.isNullPos)
        {
            PlayCard(selectedCard, nearbyPos);
        }
        ArrangeHand(false);
        selectedCard = null;
        GameBoard.UnhightlightPossiblePos();
    }

    private void CardSelectionAction()
    {
        selectedCard.GetComponent<MeshRenderer>().enabled = true;
        SetInformPanel(selectedCard.CardData);
        informPanel.gameObject.SetActive(true);
        GameBoard.UnhightlightPossiblePos();
    }

#endregion InputActionSet

}