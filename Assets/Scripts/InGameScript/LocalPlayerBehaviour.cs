using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;

public class LocalPlayerBehaviour : PlayerBehaviour
{
    [Header("Local player Settings")]
    //Temp
    public GameObject StonePrefab;

    private RectTransform cancelPanel;
    private InformationPanel informPanel;

    [SerializeField] private List<Card> deck;
    [SerializeField] private List<Card> hand;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] Transform cardSpawnPoint;
    [SerializeField] Transform handPileLeft;
    [SerializeField] Transform handPileRight;
    private GameObject stoneGhost;
    private bool isLocalRotated = false;
    public bool IsLocalRotated => isLocalRotated;
    private int maxHandSize = 7;

    // 이거는 상황 봐서 액션 자체에 대한 클래스를 만들어서 HistoryAction 클래스랑 합칠 수도 있음
    private delegate void ActionDelegate();
    [SerializeField] private ActionDelegate actionQueue;
    [SerializeField] private HistoryAction[] hitory;

    [SerializeField] private StoneBehaviour selectedStone;
    [SerializeField] private Card selectedCard;

    public StateMachine stateMachine;
    public readonly Dictionary<GameManager.TurnState, Action<Vector3>[]> turnActionDic = new Dictionary<GameManager.TurnState, Action<Vector3>[]>();

    private bool isSelecting;
    private bool isOpenStoneInform = false;

    private float curStoneSelectionActionTime;
    private bool isDragging = false;
    private int needCost;
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
    [SerializeField]
    [Tooltip("Indicates clipping range plus-minus maxDragLimit's average")]
    [Range(0f, 1.0f)]
    private float maxClippingPercentage;
    [SerializeField] private Color Cost1Color;
    [SerializeField] private Color Cost2Color;
    // [SerializeField] private AnimationCurve dragColorCurve;
    [Range(0f, 1.0f)] [SerializeField] private float alphaOnCancel;

    [Header("DebugTools"), SerializeField]
    private bool pauseEditorOnShoot = false;

    private void Start()
    {
        cancelPanel = IngameUIManager.Inst.CancelPanel;
        informPanel = IngameUIManager.Inst.InformationPanel;
    }

    private void Update()
    {
        if (stateMachine != null)
            stateMachine.DoOperateUpdate();

        //Temp test code for card draw
        if (Input.GetKeyDown(KeyCode.Q))
            DrawCards(1);
    }

    public override void InitPlayer(GameManager.PlayerEnum pEnum)
    {
        base.InitPlayer(pEnum);
        if (pEnum != GameManager.PlayerEnum.LOCAL)
        {
            Debug.LogError("[LOCAL] player enum not matched!!");
        }

        if (!GameManager.Inst.isLocalGoFirst)
        {
            isLocalRotated = true;
            transform.Rotate(Vector3.up, 180f);
            Camera.main.transform.Rotate(Vector3.forward, 180f);
            Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, 1f);
        }

        turnActionDic.Add(GameManager.TurnState.PREPARE, new Action<Vector3>[] { PrepareTouchBegin, PrepareInTouch, PrepareTouchEnd });
        turnActionDic.Add(GameManager.TurnState.WAIT, new Action<Vector3>[] { WaitTouchBegin, WaitInTouch, WaitTouchEnd });
        turnActionDic.Add(GameManager.TurnState.WAITFORHS, new Action<Vector3>[] { WaitTouchBegin, WaitInTouch, WaitTouchEnd });
        turnActionDic.Add(GameManager.TurnState.WAITFORHSCONSENT, new Action<Vector3>[] { WaitTouchBegin, WaitInTouch, WaitTouchEnd });
        turnActionDic.Add(GameManager.TurnState.NORMAL, new Action<Vector3>[] { NormalTouchBegin, NormalInTouch, NormalTouchEnd });
        turnActionDic.Add(GameManager.TurnState.HONORSKIP, new Action<Vector3>[] { HonorskipTouchBegin, HonorskipInTouch, HonorskipTouchEnd });

        stateMachine = new StateMachine(this, GameManager.Inst.LocalTurnState);

        //Init ghost Card
        stoneGhost = Instantiate(StonePrefab, Vector3.zero, Quaternion.identity);
        Destroy(stoneGhost.GetComponent<CapsuleCollider>());
        Destroy(stoneGhost.GetComponent<AkgRigidbody>());
        Color temp = stoneGhost.transform.GetChild(1).GetComponent<SpriteRenderer>().material.color;
        temp.a = .6f;
        stoneGhost.transform.GetChild(1).GetComponent<SpriteRenderer>().material.color = temp;
        stoneGhost.SetActive(false);
    }

    public override void RefreshUI()
    {
        base.RefreshUI();

        IngameUIManager.Inst.CostPanel.SetCost(Cost);
        // TODO: 현재 표시부분 없어서 주석처리
        //IngameUIManager.Inst.HandCountText.text = HandCount.ToString();
        IngameUIManager.Inst.DeckCountText.text = DeckCount.ToString();

        ColorUtility.TryParseHtmlString(ShootTokenAvailable ? "#C0FFBD" : "#FF8D91", out Color color);
        IngameUIManager.Inst.ShootTokenImage.color = color;
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
        if (point.x < gameBoard.BoardData.width * 5 && point.x > -gameBoard.BoardData.width * 5 && point.z < gameBoard.BoardData.height * 5 && point.z > -gameBoard.BoardData.height * 5) return true;
        return false;
    }

    private StoneBehaviour GetStoneAroundPoint(Vector3 point)
    {
        Ray ray = Camera.main.ScreenPointToRay(point);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.transform.CompareTag("Stone"))
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
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.transform.CompareTag("Card"))
            {
                return hit.transform.GetComponent<Card>();
            }
        }
        return null;
    }

    // 여기서부터 액션들

    public override void DrawCards(int number)
    {
        // TODO
        if (hand.Count + number > maxHandSize)
        {
            number = 7 - hand.Count;
        }
        if (deck.Count < number)
        {
            Debug.LogError("Card 부족");
            IngameUIManager.Inst.UserAlertPanel.Alert("No cards in deck"); //"덱에 카드가 부족합니다"
            return;
        }
        if (number == 0)
        {
            Debug.LogError("손패가 가득 찼습니다!");
            IngameUIManager.Inst.UserAlertPanel.Alert("Hand is full"); // "손패가 가득 찼습니다"
            return;
        }
        var cardRot = Quaternion.identity;
        if (IsLocalRotated)
            cardRot = Quaternion.Euler(0, 180, 0);
        for (int i = 0; i < number; i++)
        {
            Card drawCard = deck[0];
            deck.RemoveAt(0);
            var cardObject = Instantiate(cardPrefab, cardSpawnPoint.position, cardRot, IngameUIManager.Inst.HandCardTransform);
            var card = cardObject.GetComponent<Card>();
            card.Setup(drawCard);
            hand.Add(card);

            SetOriginOrder();
        }
        ArrangeHand(true);
        HandCount = (ushort)hand.Count;
        DeckCount = (ushort)deck.Count;
    }

    protected override void RemoveCards(int idx)
    {
        base.RemoveCards(idx);
        // TODO: 나중에 Playcard에 있는 카드 컨트롤 부분 옮기던가
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
            if (IsLocalRotated)
                targetRot = Quaternion.Euler(0, 180, 0);

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
    private void PlayCard(Card card, Vector3 nearbyPos)
    {
        // 끌어놓은 위치에 Stone 생성
        var stoneId = SpawnStone(card.CardData, nearbyPos);
        selectedCard = null;
        
        hand.Remove(card);
        RemoveCards(0);
        PlayCardSendNetworkAction(card.CardData, nearbyPos, stoneId);
        Destroy(card.gameObject);
        ArrangeHand(false);
    }

    public override int SpawnStone(CardData cardData, Vector3 spawnPosition, int stoneId = -1)
    {
        if (!gameBoard.IsPossibleToPut(spawnPosition, Util.GetRadiusFromStoneSize(cardData.stoneSize)))
        {
            return -1;
        }
        
        // TODO: 코스트 소모를 여기서 하는게 아님??

        //FIXME : 카드에 맞는 스톤을 런타임에 생성해줘야 함
        GameObject spawnedStone = Instantiate(StonePrefab, spawnPosition, Quaternion.identity);
        var stoneBehaviour = spawnedStone.GetComponent<StoneBehaviour>();
        var newStoneId = AddStone(stoneBehaviour);
        stoneBehaviour.SetCardData(cardData, newStoneId, Player);

        //temp code
        spawnedStone.transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = stoneBehaviour.GetSpriteState("Idle");
        if (isLocalRotated)
            spawnedStone.transform.GetChild(1).GetComponent<SpriteRenderer>().transform.rotation = Quaternion.Euler(90, 180, 0);
        spawnedStone.transform.GetChild(4).GetComponent<SpriteRenderer>().material.color = Color.blue;
        spawnedStone.transform.localScale = new Vector3(Util.GetRadiusFromStoneSize(cardData.stoneSize)*2, .15f, Util.GetRadiusFromStoneSize(cardData.stoneSize)*2);
        spawnedStone.GetComponent<AkgRigidbody>().Init(Util.GetMassFromStoneWeight(cardData.stoneSize, cardData.stoneWeight));

        return newStoneId;
    }

    // PlayCard 함수에서 사용되는 패킷 전송
    private void PlayCardSendNetworkAction(CardData cardData, Vector3 position, int stoneId)
    {
        var toSend = $"PLAYCARD/ {cardData.CardID} {Vector3ToString(position)} {stoneId} {Cost}";

        Debug.Log($"[{NetworkManager.Inst.NetworkId}] {toSend}");

        NetworkManager.Inst.SendData(new MessagePacket
        {
            senderID = NetworkManager.Inst.NetworkId,
            message = toSend,
        }, PacketType.ROOM_OPPONENT);
    }

    private void ShootStone(Vector3 vec) // vec이 velocity인지 force인지 명확하게 해야함
    {
#if UNITY_EDITOR
        if (pauseEditorOnShoot) UnityEditor.EditorApplication.isPaused = true;
#endif

        StartCoroutine(EShootStone());

        selectedStone.transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = selectedStone.GetSpriteState("Shoot");
        selectedStone.GetComponent<AkgRigidbody>().AddForce(vec);
        ShootTokenAvailable = false;
    }

    private IEnumerator EShootStone()
    {
        var recorder = AkgPhysicsManager.Inst.rigidbodyRecorder;
        recorder.StartRecord(Time.time);

        yield return null;
        bool isAllStoneStop = false;

        while (!isAllStoneStop)
        {
            yield return new WaitUntil(() =>
                (GameManager.Inst.AllStones.Count == 0 ||
                GameManager.Inst.AllStones.Values.All(x => !x.isMoving))
            );

            isAllStoneStop = true;
        }

        foreach (StoneBehaviour stone in GameManager.Inst.AllStones.Values)
        {
            stone.transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = stone.GetSpriteState("Idle");
            if (isLocalRotated)
            {
                stone.transform.GetChild(1).GetComponent<SpriteRenderer>().transform.rotation = Quaternion.Euler(90, 180, 0);
            }
            else
            {
                stone.transform.GetChild(1).GetComponent<SpriteRenderer>().transform.rotation = Quaternion.Euler(90, 0, 0);
            }
        }
        // send physics records, stone final poses, event list
        recorder.EndRecord(out var velocityRecords, out var eventRecords);
        recorder.SendRecord(velocityRecords, eventRecords);
    }

    private void SetInformPanel(CardData data)
    {
        informPanel.SetInformation(data);
    }

    private Vector3 ScreenPosToNormalized(Vector3 vec)
    {
        Vector3 curTouchPosition = Camera.main.ScreenToWorldPoint(vec);
        return new Vector3(curTouchPosition.x, 0f, curTouchPosition.z);
    }

    ///<summary>
    ///Arrange cards in hand regarding card index at hand[]<br/>
    ///</summary>
    ///<param name = "isDrawPhase">true인 경우 부드럽게 손패로 이동, false일 경우 텀 없이 바로 손패로 이동</param>
    private void ArrangeHand(bool isDrawPhase)
    {
        List<RPS> originCardRPSs = new List<RPS>();
        originCardRPSs = RoundAlignment(handPileLeft, handPileRight, hand.Count, isLocalRotated ? -0.5f : 0.5f, new Vector3(2.0f, 0.5f, 3.0f));

        var targetCards = hand;

        for (int i = 0; i < targetCards.Count; i++)
        {
            var targetCard = targetCards[i];

            targetCard.originRPS = originCardRPSs[i];
            if (isDrawPhase)
                targetCard.MoveTransform(targetCard.originRPS, true, 0.7f);
            else
                targetCard.MoveTransform(targetCard.originRPS, false, 0.7f);
        }
    }

    private IEnumerator EStoneSelectionAction()
    {
        isOpenStoneInform = false;
        curStoneSelectionActionTime = StoneSelectionActionThreshold;
        while (curStoneSelectionActionTime >= 0)
        {
            curStoneSelectionActionTime -= Time.deltaTime;
            if (!isSelecting) yield break;
            yield return null;
        }
        isOpenStoneInform = true;
    }

    // 드래그중 토큰이 강조되는 애니메이션
    private IEnumerator EShootTokenAlert()
    {
        float time;
        // RectTransform alertRect = IngameUIManager.Inst.ShootTokenImage.transform.GetChild(0).GetComponent<RectTransform>();
        RectTransform alertRect = IngameUIManager.Inst.ShootTokenImage.transform.GetComponent<RectTransform>();
        // alertRect.gameObject.SetActive(true);

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

        // alertRect.gameObject.SetActive(false);
    }

    private bool isMoving = false;
    private bool isMoveDownward = false;
    private void ShootDragRoutine(bool isBeginning)
    {
        isMoveDownward = isBeginning;

        if(isMoving)
        {    
            return;
        }

        if(isBeginning)
        {
            StartCoroutine(EShootDragBeginUIRoutine());
        }
        else
        {
            StartCoroutine(EShootDragEndUIRoutine());
        }
    }

    float beginTotTime = .5f;
    float endTotTime = .5f;
    private IEnumerator EShootDragBeginUIRoutine()
    {
        isMoving = true;
        float curTime = IngameUIManager.Inst.ShootReadyEmphasizeUI.anchoredPosition.y / -400;
        while(curTime < beginTotTime)
        {
            if(!isMoveDownward)
            {
                StartCoroutine(EShootDragEndUIRoutine());
                yield break;
            }

            if(GameManager.Inst.isLocalGoFirst)
                IngameUIManager.Inst.HandCardTransform.position = new Vector3(0f, 0f, Mathf.Lerp(0, -4, curTime / beginTotTime));
            else
                IngameUIManager.Inst.HandCardTransform.position = new Vector3(0f, 0f, Mathf.Lerp(0, 4, curTime / beginTotTime));

            IngameUIManager.Inst.ShootReadyEmphasizeUI.anchoredPosition = new Vector2(0f, Mathf.Lerp(0, -400, curTime / beginTotTime));
            curTime += Time.deltaTime;
            yield return null;
        }
        
        IngameUIManager.Inst.ActivateUI(cancelPanel.GetComponent<RectTransform>());
        isMoving = false;
    }

    private IEnumerator EShootDragEndUIRoutine()
    {
        isMoving = true;
        float curTime = (IngameUIManager.Inst.ShootReadyEmphasizeUI.anchoredPosition.y + 400) / 400;
        while(curTime < endTotTime)
        {
            if(isMoveDownward)
            {
                StartCoroutine(EShootDragBeginUIRoutine());
                yield break;
            }

            if(GameManager.Inst.isLocalGoFirst)
                IngameUIManager.Inst.HandCardTransform.position = new Vector3(0f, 0f, Mathf.Lerp(-4, 0, curTime / endTotTime));
            else
                IngameUIManager.Inst.HandCardTransform.position = new Vector3(0f, 0f, Mathf.Lerp(4, 0, curTime / endTotTime));

            IngameUIManager.Inst.ShootReadyEmphasizeUI.anchoredPosition = new Vector2(0f, Mathf.Lerp(-400, 0, curTime / endTotTime));
            curTime += Time.deltaTime;
            yield return null;
        }
        isMoving = false;
    }

    #region TouchInputActions

    public void NormalTouchBegin(Vector3 curScreenTouchPosition)
    {
        Vector3 curTouchPosition = Camera.main.ScreenToWorldPoint(curScreenTouchPosition);
        Vector3 curTouchPositionNormalized = new Vector3(curTouchPosition.x, 0f, curTouchPosition.z);
        dragStartPoint = curTouchPositionNormalized;

        //Card
        selectedCard = GetCardAroundPoint(curScreenTouchPosition);
        if(selectedCard != null)
        {
            CardDragAction_Begin();
        }

        if (selectedStone == null)
        {
            isSelecting = true;
            StartCoroutine(EStoneSelectionAction());
            return;
        }

        if (selectedStone != null)
        {
            StoneDragAction_Begin(curScreenTouchPosition, curTouchPositionNormalized);
            return;
        }
    }

    public void NormalInTouch(Vector3 curScreenTouchPosition)
    {
        if (IngameUIManager.Inst.isThereActivatedUI(informPanel.GetComponent<RectTransform>())) return;

        Vector3 curTouchPosition = Camera.main.ScreenToWorldPoint(curScreenTouchPosition);
        Vector3 curTouchPositionNormalized = new Vector3(curTouchPosition.x, 0f, curTouchPosition.z);

        //Stone Dragging for shoot
        if (selectedStone != null && !startOnCancel && (TouchManager.Inst.GetTouchDelta().x != 0 || TouchManager.Inst.GetTouchDelta().y != 0))
        {
            StoneDragAction(curScreenTouchPosition, curTouchPositionNormalized);
        }

        //Card Dragging for play card
        if (selectedCard != null)
        {
            CardDragAction(curTouchPositionNormalized);
        }
    }

    public void NormalTouchEnd(Vector3 curScreenTouchPosition)
    {
        Vector3 curTouchPosition = Camera.main.ScreenToWorldPoint(curScreenTouchPosition);
        Vector3 curTouchPositionNormalized = new Vector3(curTouchPosition.x, 0f, curTouchPosition.z);

        IngameUIManager.Inst.CostPanel.CostEmphasize(0);

        //UI handle
        if (IngameUIManager.Inst.isThereActivatedUI(informPanel.GetComponent<RectTransform>()))
        {
            UICloseAction();
            return;
        }

        if (selectedStone == null)
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
            else if (IsTouchOnBoard(curScreenTouchPosition))
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
        if(selectedCard != null)
        {
            CardDragAction_Begin();
        }
    }

    public void PrepareInTouch(Vector3 curScreenTouchPosition)
    {
        if (IngameUIManager.Inst.isThereActivatedUI(informPanel.GetComponent<RectTransform>())) return;

        Vector3 curTouchPosition = Camera.main.ScreenToWorldPoint(curScreenTouchPosition);
        Vector3 curTouchPositionNormalized = new Vector3(curTouchPosition.x, 0f, curTouchPosition.z);

        //Card Dragging for play card
        if (selectedCard != null)
        {
            CardDragAction(curTouchPositionNormalized);
        }
    }

    public void PrepareTouchEnd(Vector3 curScreenTouchPosition)
    {
        Vector3 curTouchPosition = Camera.main.ScreenToWorldPoint(curScreenTouchPosition);
        Vector3 curTouchPositionNormalized = new Vector3(curTouchPosition.x, 0f, curTouchPosition.z);

        IngameUIManager.Inst.CostPanel.CostEmphasize(0);

        //UI handle
        if (IngameUIManager.Inst.isThereActivatedUI(informPanel.GetComponent<RectTransform>()))
        {
            UICloseAction();
            return;
        }

        if (selectedStone == null)
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

            if (IsTouchOnBoard(curScreenTouchPosition))
            {
                CardPlayAction(curTouchPositionNormalized);
                return;
            }
        }
    }

    public void WaitTouchBegin(Vector3 curScreenTouchPosition)
    {
        Vector3 curTouchPosition = Camera.main.ScreenToWorldPoint(curScreenTouchPosition);
        Vector3 curTouchPositionNormalized = new Vector3(curTouchPosition.x, 0f, curTouchPosition.z);
        dragStartPoint = curTouchPositionNormalized;

        //Card
        selectedCard = GetCardAroundPoint(curScreenTouchPosition);
    }

    public void WaitInTouch(Vector3 curScreenTouchPosition)
    {
        if (IngameUIManager.Inst.isThereActivatedUI(informPanel.GetComponent<RectTransform>())) return;

        Vector3 curTouchPosition = Camera.main.ScreenToWorldPoint(curScreenTouchPosition);
        Vector3 curTouchPositionNormalized = new Vector3(curTouchPosition.x, 0f, curTouchPosition.z);

    }

    public void WaitTouchEnd(Vector3 curScreenTouchPosition)
    {
        Vector3 curTouchPosition = Camera.main.ScreenToWorldPoint(curScreenTouchPosition);
        Vector3 curTouchPositionNormalized = new Vector3(curTouchPosition.x, 0f, curTouchPosition.z);

        //UI handle
        if (IngameUIManager.Inst.isThereActivatedUI(informPanel.GetComponent<RectTransform>()))
        {
            UICloseAction();
            return;
        }
        
        if (selectedStone == null)
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
        }
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

    #endregion TouchInputActions

    #region TouchBeginActionSet

    private void StoneDragAction_Begin(Vector3 curScreenTouchPosition, Vector3 curTouchPositionNormalized)
    {
        if(selectedStone.BelongingPlayer == GameManager.PlayerEnum.OPPO) return;

        bool isTouchOnCancel = RectTransformUtility.RectangleContainsScreenPoint(cancelPanel, curScreenTouchPosition, null);
        isDragging = !isTouchOnCancel;

        dragStartPoint = curTouchPositionNormalized;
        startOnCancel = isTouchOnCancel;

        if (isDragging && !IngameUIManager.Inst.isThereActivatedUI(informPanel.GetComponent<RectTransform>()))
        {
            //temp
            selectedStone.transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = selectedStone.GetSpriteState("Ready");

            dragEffectObj.gameObject.SetActive(true);
            dragEffectObj.SetPosition(0, curTouchPositionNormalized);
            dragEffectObj.SetPosition(1, curTouchPositionNormalized);

            stoneArrowObj = selectedStone.transform.GetChild(0).GetComponent<ArrowGenerator>();
            stoneArrowObj.gameObject.SetActive(true);

            IngameUIManager.Inst.CostPanel.CostEmphasize(1);
        }
    }

    private void CardDragAction_Begin()
    {
        stoneGhost.transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = Util.GetSpriteState(selectedCard.CardData,"Idle");
        if (isLocalRotated)
            stoneGhost.transform.GetChild(1).GetComponent<SpriteRenderer>().transform.rotation = Quaternion.Euler(90, 180, 0);
        stoneGhost.transform.localScale = new Vector3(Util.GetRadiusFromStoneSize(selectedCard.CardData.stoneSize) *2, .15f, Util.GetRadiusFromStoneSize(selectedCard.CardData.stoneSize)*2);

        IngameUIManager.Inst.CostPanel.CostEmphasize(selectedCard.CardData.cardCost);
    }

    #endregion TouchEndActionSet


    #region InTouchActionSet

    private void StoneDragAction(Vector3 curScreenTouchPosition, Vector3 curTouchPositionNormalized)
    {
        if(selectedStone.BelongingPlayer == GameManager.PlayerEnum.OPPO) return;
        
        bool isTouchOnCancel = RectTransformUtility.RectangleContainsScreenPoint(cancelPanel, curScreenTouchPosition, null);
        isDragging = !isTouchOnCancel;

        isDragging = !isTouchOnCancel;
        StartCoroutine(EShootTokenAlert());

        dragEndPoint = curTouchPositionNormalized;
        Vector3 moveVec = dragStartPoint - dragEndPoint;
        Color dragColor;
        Vector3 deltaVec = new Vector3(TouchManager.Inst.GetTouchDelta().x, 0, TouchManager.Inst.GetTouchDelta().y);

        if(Cost < 2)
        {
            curDragMagnitude = moveVec.magnitude;
            needCost = 1;
            if(moveVec.magnitude >= maxDragLimit/2)
            {
                curDragMagnitude = maxDragLimit/2;
            }
            dragColor = Cost1Color;
        }
        else if (moveVec.magnitude >= maxDragLimit)
        {
            curDragMagnitude = maxDragLimit;
            dragColor = Cost2Color;
            needCost = 2;
        }
        else
        {
            float previous = curDragMagnitude;
            float avg = maxDragLimit / 2;
            float cur = Mathf.Min(moveVec.magnitude, maxDragLimit);

            if ((avg > previous && avg <= cur) || (avg < previous && avg >= cur))
            {
                reachedAvg = true;
            }
            else if (cur <= avg * (1f - maxClippingPercentage))
            {
                leftLowEnd = true;
                reachedAvg = false;
            }
            else if (cur >= avg * (1f + maxClippingPercentage))
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
            if (cur < avg && cur > avg * (1f - maxClippingPercentage) && reachedAvg && (!leftLowEnd || Vector3.Dot(deltaVec, moveVec) > 0))
            {
                needCost = 1;
                curDragMagnitude = maxDragLimit / 2;
                dragColor = Cost1Color;
                IngameUIManager.Inst.CostPanel.CostEmphasize(1);
            }
            //Increasing Power
            else if (cur > avg && cur < avg * (1f + maxClippingPercentage) && reachedAvg && (!leftHighEnd || Vector3.Dot(deltaVec, moveVec) < 0))
            {
                needCost = 1;
                curDragMagnitude = maxDragLimit / 2;
                dragColor = Cost1Color;
                IngameUIManager.Inst.CostPanel.CostEmphasize(1);
            }
            else if (moveVec.magnitude > maxDragLimit / 2)
            {
                needCost = 2;
                dragColor = Cost2Color;
                IngameUIManager.Inst.CostPanel.CostEmphasize(2);
            } 
            else
            {
                needCost = 1;
                dragColor = Cost1Color;
                IngameUIManager.Inst.CostPanel.CostEmphasize(1);
            }
        }

        //dragEffectObj.endColor = dragEffectObj.startColor = Color.Lerp(Cost1Color, Cost2Color, dragColorCurve.Evaluate(Mathf.Min(moveVec.magnitude, maxDragLimit)/maxDragLimit));
        dragEffectObj.endColor = dragEffectObj.startColor = dragColor;
        stoneArrowObj.GetComponent<MeshRenderer>().material.color = dragEffectObj.startColor;

        dragEffectObj.SetPosition(1, dragStartPoint - moveVec.normalized * curDragMagnitude);
        stoneArrowObj.stemLength = curDragMagnitude;

        if (moveVec.z >= 0)
        {
            float angle = Mathf.Acos(Vector3.Dot(Vector3.left, moveVec.normalized)) * 180 / Mathf.PI + 180;
            stoneArrowObj.transform.rotation = Quaternion.Euler(90, angle, 0);
            selectedStone.transform.GetChild(1).GetComponent<SpriteRenderer>().transform.rotation = Quaternion.Euler(90, angle + 90, 0);
        }
        else
        {
            float angle = Mathf.Acos(Vector3.Dot(Vector3.right, moveVec.normalized)) * 180 / Mathf.PI;
            stoneArrowObj.transform.rotation = Quaternion.Euler(90, angle, 0);
            selectedStone.transform.GetChild(1).GetComponent<SpriteRenderer>().transform.rotation = Quaternion.Euler(90, angle + 90, 0);
        }

        if (!isDragging)
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
        stoneGhost.transform.position = selectedCard.transform.position = new Vector3(curTouchPositionNormalized.x, 5f, curTouchPositionNormalized.z);

        if (IsPlayingCardOnBoard(curTouchPositionNormalized))
        {
            //TODO : 스톤 미리보기 추가해주어야 함
            selectedCard.GetComponent<MeshRenderer>().enabled = false;
            stoneGhost.SetActive(true);

            gameBoard.ResetMarkState();
            GameManager.Inst.GameBoard.HighlightPossiblePos(GameManager.PlayerEnum.LOCAL, Util.GetRadiusFromStoneSize(selectedCard.CardData.stoneSize));
            Transform putMarkTransform = gameBoard.GiveNearbyPos(curTouchPositionNormalized, GameManager.PlayerEnum.LOCAL, Util.GetRadiusFromStoneSize(selectedCard.CardData.stoneSize));
            if(putMarkTransform != null)
            {                
                putMarkTransform.GetComponent<SpriteRenderer>().material.color = Color.red;
                stoneGhost.transform.position = putMarkTransform.position;
            }
        }
        else
        {
            selectedCard.GetComponent<MeshRenderer>().enabled = true;
            stoneGhost.SetActive(false);

            GameManager.Inst.GameBoard.UnhightlightPossiblePos();
        }
    
    }

    #endregion InTouchActionSet


    #region TouchEndActionSet

    private void UICloseAction()
    {
        IngameUIManager.Inst.DeactivateUI();
        selectedCard = null;
        selectedStone = null;
        GameManager.Inst.GameBoard.UnhightlightPossiblePos();
    }

    private void StoneSelectionAction(bool canShoot, Vector3 curScreenTouchPosition)
    {
        isSelecting = false;
        selectedStone = GetStoneAroundPoint(curScreenTouchPosition);

        if (selectedStone != null)
        {
            selectedStone.isClicked = true;
            if (canShoot && !isOpenStoneInform && selectedStone.BelongingPlayer == GameManager.PlayerEnum.LOCAL && Cost > 0 && ShootTokenAvailable)
            {
                //Simply select current stone and move to shooting phase
                ShootDragRoutine(true);
                // Debug.Log("Selected");
                return;
            }
            else
            {
                //Open Information about selected stone
                SetInformPanel(selectedStone.CardData);
                IngameUIManager.Inst.ActivateUI(informPanel.GetComponent<RectTransform>());
                // Debug.Log("Information");
                return;
            }
        }
    }

    private void StoneShootAction(Vector3 curTouchPositionNormalized)
    {

        dragEndPoint = curTouchPositionNormalized;
        Vector3 moveVec = dragStartPoint - dragEndPoint;

        ShootDragRoutine(false);
        IngameUIManager.Inst.DeactivateUI(cancelPanel.GetComponent<RectTransform>());

        dragEffectObj?.gameObject.SetActive(false);
        stoneArrowObj?.gameObject.SetActive(false);

        if(moveVec.sqrMagnitude == 0)
        {
            IngameUIManager.Inst.UserAlertPanel.Alert("You need to drag stone for shot!");
            isDragging = false;
            selectedStone = null;
            selectedStone.transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = selectedStone.GetSpriteState("Idle");
            if (isLocalRotated)
            {
                selectedStone.transform.GetChild(1).GetComponent<SpriteRenderer>().transform.rotation = Quaternion.Euler(90, 180, 0);
            }
            else
            {
                selectedStone.transform.GetChild(1).GetComponent<SpriteRenderer>().transform.rotation = Quaternion.Euler(90, 0, 0);
            }
            return;
        }

        if (isDragging && !startOnCancel)
        {

            // shoot token이 없는 경우, 쏘지 못하게 리셋
            if (!ShootTokenAvailable)
            {
                Debug.LogWarning("공격토큰이 존재하지 않습니다.");
                IngameUIManager.Inst.UserAlertPanel.Alert("No attack token"); // "공격 토큰이 존재하지 않습니다"
                selectedStone.transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = selectedStone.GetSpriteState("Idle");
                if (isLocalRotated)
                {
                    selectedStone.transform.GetChild(1).GetComponent<SpriteRenderer>().transform.rotation = Quaternion.Euler(90, 180, 0);
                }
                else
                {
                    selectedStone.transform.GetChild(1).GetComponent<SpriteRenderer>().transform.rotation = Quaternion.Euler(90, 0, 0);
                }
                isDragging = false;
                selectedStone = null;
                return;
            }

            if(!SpendCost(needCost))
            {
                //Not enough cost for shooting
                selectedStone.transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = selectedStone.GetSpriteState("Idle");
                if (isLocalRotated)
                {
                    selectedStone.transform.GetChild(1).GetComponent<SpriteRenderer>().transform.rotation = Quaternion.Euler(90, 180, 0);
                }
                else
                {
                    selectedStone.transform.GetChild(1).GetComponent<SpriteRenderer>().transform.rotation = Quaternion.Euler(90, 0, 0);
                }
                isDragging = false;
                selectedStone = null;
                Debug.LogError("You have not enough cost for shooting stone!");
                IngameUIManager.Inst.UserAlertPanel.Alert("Not enough cost for shooting stone!"); // "코스트가 부족합니다"
                return;
            }

            float VelocityCalc = Mathf.Lerp(minShootVelocity, maxShootVelocity, Mathf.Min(moveVec.magnitude, maxDragLimit) / maxDragLimit) * velocityMultiplier;
            ShootStone(moveVec.normalized * selectedStone.GetComponent<AkgRigidbody>().Mass * VelocityCalc);
        }

        // 여기넣는게 맞는지 모름
        selectedStone.transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = selectedStone.GetSpriteState("Idle");
        if (isLocalRotated)
        {
            selectedStone.transform.GetChild(1).GetComponent<SpriteRenderer>().transform.rotation = Quaternion.Euler(90, 180, 0);
        }
        else
        {
            selectedStone.transform.GetChild(1).GetComponent<SpriteRenderer>().transform.rotation = Quaternion.Euler(90, 0, 0);
        }
        isDragging = false;

        selectedStone = null;
    }

    private void CardPlayAction(Vector3 curTouchPositionNormalized)
    {
        selectedCard.GetComponent<MeshRenderer>().enabled = true;
        stoneGhost.SetActive(false);

        Transform nearPutTransform = gameBoard.GiveNearbyPos(curTouchPositionNormalized, GameManager.PlayerEnum.LOCAL, Util.GetRadiusFromStoneSize(selectedCard.CardData.stoneSize));
        if(nearPutTransform == null)
        {
            Debug.LogError("Unavailiable place to spawn stone!");
            IngameUIManager.Inst.UserAlertPanel.Alert("Unavailiable place to spawn stone"); // 돌을 놓을 수 있는 위치가 아닙니다
            GameManager.Inst.GameBoard.UnhightlightPossiblePos();
            selectedCard.EnlargeCard(false);
            return;
        }
        else if(!SpendCost(selectedCard.CardData.cardCost))
        {
            Debug.LogError("Not enough cost to play card!");
            IngameUIManager.Inst.UserAlertPanel.Alert("Not enough cost to play card"); // 코스트가 부족합니다
            GameManager.Inst.GameBoard.UnhightlightPossiblePos();
            selectedCard.EnlargeCard(false);
            return;
        }

        Vector3 nearbyPos = nearPutTransform.position;
        PlayCard(selectedCard, nearbyPos);
        ArrangeHand(false);
        selectedCard = null;
        GameManager.Inst.GameBoard.UnhightlightPossiblePos();
    }

    private void CardSelectionAction()
    {
        selectedCard.GetComponent<MeshRenderer>().enabled = true;
        stoneGhost.SetActive(false);

        SetInformPanel(selectedCard.CardData);
        IngameUIManager.Inst.ActivateUI(informPanel.GetComponent<RectTransform>());
        GameManager.Inst.GameBoard.UnhightlightPossiblePos();
    }

    #endregion InputActionSet

}
