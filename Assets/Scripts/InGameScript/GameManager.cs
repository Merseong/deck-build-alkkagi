using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GameManager : SingletonBehavior<GameManager>
{
    public enum PlayerEnum { LOCAL, OPPO }

    // 물리 기록용 recorder
    public AkgRigidbodyRecorder rigidbodyRecorder = new AkgRigidbodyRecorder();

    // 각 플레이어
    public PlayerBehaviour[] players;  // 0: local, 1: oppo
    public PlayerBehaviour LocalPlayer => players[0];
    public PlayerBehaviour OppoPlayer => players[1];
    public PlayerBehaviour CurrentPlayer => players[(int)WhoseTurn];

    // Card List 임시용
    public List<CardData> CardDatas;

    // 돌들
    public Dictionary<int, StoneBehaviour> AllStones = new();

    // 현재 보드
    [SerializeField] private GameBoard gameBoard;
    public GameBoard GameBoard => gameBoard;

    // 서버에서 선공이 누구인지 정해줘야함
    public bool isLocalGoFirst;
    PlayerEnum FirstPlayer => isLocalGoFirst ? PlayerEnum.LOCAL: PlayerEnum.OPPO;
    PlayerEnum SecondPlayer => isLocalGoFirst ? PlayerEnum.OPPO : PlayerEnum.LOCAL;
    //선,후공과 관계없이 HS를 했는지 여부에 대한 bool
    public bool isPlayerHonorSkip;
    //후공의 경우에 상대가 HS했을 경우 동의 여부에 대한 bool
    public bool isPlayerConsentHonorSkip;
    //카드 정보 일람 여부에 대햔 flag
    public bool isInformOpened;
    public bool isCancelOpened;

    // 각자 턴의 제어
    // WAIT가 아닌 턴의 종료가 일어나는 PLAYER쪽에서 턴 변경 처리
    // 턴 시작 시 처리되어야하는 일 ex) UI, 코스트 증감, 드로우 등등
    public event Action<TurnState> OnTurnStart = new Action<TurnState>((_) => { });
    public event Action<TurnState> OnTurnEnd = new Action<TurnState>((_) => { });
    public enum TurnState
    {
        PREPARE,
        WAIT,
        WAITFORHS,
        WAITFORHSCONSENT,
        NORMAL,
        HONORSKIP,
        HSCONSENT,
        LENGTH
    }
    private bool isTurnEndSent;

    /// <summary>
    /// 0: 준비턴 선공, 1: 준비턴 후공, 
    /// 2: 1턴 선공, 3: 1턴 후공, 
    /// 이후는 노말턴 선공 및 후공
    /// </summary>
    [SerializeField] private int totalTurn = 0;
    [SerializeField] private int nextTotalTurn;
    public int TurnCount => totalTurn / 2;
    public PlayerEnum WhoseTurn => (PlayerEnum)((totalTurn + (isLocalGoFirst ? 0 : 1)) % 2);

    public TurnState[] turnStates = { TurnState.PREPARE, TurnState.PREPARE };  // 0: local, 1: oppo
    public TurnState LocalTurnState
    {
        get => turnStates[0];
        set => turnStates[0] = value;
    }
    public TurnState OppoTurnState
    {
        get => turnStates[1];
        set => turnStates[1] = value;
    }
    public TurnState[] nextTurnStates;
    public TurnState LocalNextTurnState
    {
        get => nextTurnStates[0];
        set => nextTurnStates[0] = value;
    }
    public TurnState OppoNextTurnState
    {
        get => nextTurnStates[1];
        set => nextTurnStates[1] = value;
    }

    public int initialTurnCost = 6;
    public int normalTurnCost = 3;

    public void Start()
    {
        OnTurnStart += StartTurnBasis;

        rigidbodyRecorder.InitRecorder();
        NetworkManager.Inst.AddReceiveDelegate(TurnInfoReceiveNetworkAction);

        if (!NetworkManager.Inst.IsNetworkMode) InitializeGame();
    }

    private void OnApplicationQuit()
    {
        NetworkManager.Inst.RemoveReceiveDelegate(TurnInfoReceiveNetworkAction);
    }

    #region Card Data
    public CardData GetCardDataById(int cardId)
    {
        return CardDatas.Find(e => e.CardID == cardId);
    }
    #endregion

    #region Manager Data Control
    private void ProcessWithCoroutine(Func<bool> predicate, Action job)
    {
        StartCoroutine(EProcessLater());

        IEnumerator EProcessLater()
        {
            yield return new WaitUntil(predicate);
            job?.Invoke();
        }
    }

    public void SetPlayerData(Action action)
    {
        if (players.Length == 0)
            ProcessWithCoroutine(() => players.Length > 0, action);
        else
            action();
    }

    #endregion

    #region Stones list control
    public StoneBehaviour FindStone(int stoneId)
    {
        var isFound = AllStones.TryGetValue(stoneId, out var stone);

        if (!isFound)
        {
            Debug.LogError($"[GAME] stone id {stoneId} not found");
        }

        return stone;
    }
    #endregion

    #region Turn Control
    public void InitializeGame()
    {
        turnStates[(int)FirstPlayer] = TurnState.PREPARE;
        turnStates[(int)SecondPlayer] = TurnState.WAIT;

        nextTurnStates = new TurnState[2];
        nextTurnStates[(int)FirstPlayer] = TurnState.WAIT;
        nextTurnStates[(int)SecondPlayer] = TurnState.PREPARE;

        // inspector에서 직접 설정 필요
        players[0].InitPlayer(PlayerEnum.LOCAL);
        players[1].InitPlayer(PlayerEnum.OPPO);
        GameBoard.InitGameBoard();

        // Start game
        LocalPlayer.DrawCards(5);
        LocalPlayer.ResetCost(30); // temp
        LocalPlayer.ShootTokenAvailable = true;
    }

    private void StartTurnBasis(TurnState turnState)
    {
        if (turnState != TurnState.NORMAL) return;

        LocalPlayer.DrawCards(1);
        LocalPlayer.ResetCost();
        LocalPlayer.ShootTokenAvailable = true;
    }

    /// <remarks>
    /// { ROOM_BROADCAST | 0 | TURNEND/ nextTotalTurn stonePosition LocalNextTurnState }
    /// </remarks>
    /// <param name="packet">
    /// { ROOM_BROADCAST | 0 | TURNEND/ nextTotalTurn stonePosition LocalNextTurnState }
    /// </param>
    private void TurnInfoReceiveNetworkAction(MyNetworkData.Packet packet)
    {
        if (packet.Type != (short)MyNetworkData.PacketType.ROOM_BROADCAST) return;

        var msg = MyNetworkData.MessagePacket.Deserialize(packet.Data);

        if (msg.senderID != 0) return;
        if (!msg.message.StartsWith("TURNEND/")) return;

        var msgArr = msg.message.Split(" ");
        //if (!isTurnEndSent) SetNextTurnState();
        /*if (nextTotalTurn != int.Parse(msgArr[1]))
        {
            Debug.LogError("[ROOM] Total turn not matched!");
            return;
        }
        if (LocalNextTurnState != (TurnState)int.Parse(msgArr[3]))
        {
            Debug.LogError("[ROOM] next state not matched!");
            return;
        }; */
        nextTotalTurn = int.Parse(msgArr[1]);
        LocalNextTurnState = (TurnState)int.Parse(msgArr[3]);
        OppoNextTurnState = (TurnState)int.Parse(msgArr[4]);
        TurnEnd(totalTurn != nextTotalTurn);
    }

    /// <summary>
    /// 네트워크에 턴 완료 신호 전송
    /// </summary>
    /// <remarks>
    /// { ROOM_BROADCAST | networkId | TURNEND/ nextTotalTurn stonePosition localnextturnState OpppNextTurnState }
    /// </remarks>
    private void TurnInfoSendNetworkAction()
    {
        NetworkManager.Inst.SendData(new MyNetworkData.MessagePacket
        {
            senderID = NetworkManager.Inst.NetworkId,
            message = $"TURNEND/ {nextTotalTurn} {"hello"} {(short)LocalNextTurnState} {(short)OppoNextTurnState}",
        }, MyNetworkData.PacketType.ROOM_BROADCAST);

        isTurnEndSent = true;
    }

    public void TurnEndButtonAction()
    {
        if (isTurnEndSent)
        {
            Debug.LogWarning("[ME] Turn end already sent!");
        }
        if (WhoseTurn == PlayerEnum.OPPO)
        {
            Debug.LogError("Waiting player ended turn!");
            return;
        }

        SetNextTurnState();

        TurnInfoSendNetworkAction();
    }

    private void TurnEnd(bool turnEndAction = true)
    {
        isTurnEndSent = false;

        if (turnEndAction) OnTurnEnd(LocalTurnState);
        // 상대 GameManager의 turn info 변경
        // 예시
        // SomeNetworkPacket result = await SendTurnInfo();
        // if (result.applied)
        LocalTurnState = LocalNextTurnState;
        OppoTurnState = OppoNextTurnState;
        totalTurn = nextTotalTurn;

        // 선턴 && 현재 HS턴 && 둘 모두의 State가 WAITFORHS
        if (totalTurn == 2 && LocalTurnState == OppoTurnState)
        {
            if (isLocalGoFirst)
            {
                StartCoroutine(EHonorSkipRoutine());
            }
            else
            {
                StartCoroutine(EHonorSkipSecondRoutine());
            }
        }

        // TurnState.NORMAL인경우만 내 턴을 시작
        OnTurnStart(LocalTurnState);
    }

    private void SetNextTurnState()
    {
        switch (totalTurn)
        {
            // 준비턴 선공 종료시
            case 0:
                nextTurnStates[(int)FirstPlayer] = TurnState.WAIT;
                nextTurnStates[(int)SecondPlayer] = TurnState.PREPARE;
                nextTotalTurn++;
                break;
            // 준비턴 후공 종료시
            case 1:
                nextTurnStates[(int)FirstPlayer] = TurnState.WAITFORHS;
                nextTurnStates[(int)SecondPlayer] = TurnState.WAITFORHS;
                nextTotalTurn++;
                break;
            // 2: 1턴 선공 (HS or FNORMAL)
            case 2:
                nextTurnStates[(int)FirstPlayer] = TurnState.WAIT;
                nextTurnStates[(int)SecondPlayer] = TurnState.WAITFORHS;
                nextTotalTurn++;
                break;
            // 3: 1턴 후공 (HS or normal)
            case 3:
                LocalNextTurnState = TurnState.WAIT;
                OppoNextTurnState = TurnState.NORMAL;
                nextTotalTurn++;
                break;
            // 4~: 2턴 이후
            default:
                if (LocalTurnState == TurnState.NORMAL)
                {
                    LocalNextTurnState = TurnState.WAIT;
                    OppoNextTurnState = TurnState.NORMAL;
                }
                else
                {
                    LocalNextTurnState = TurnState.NORMAL;
                    OppoNextTurnState = TurnState.WAIT;
                }
                nextTotalTurn++;
                break;
        }
    }
    #endregion

    #region Honor Skip
    bool isInHonorSkipRoutine = false;

    private IEnumerator EHonorSkipRoutine()
    {
        Debug.Log("Start HS Routine");

        isInHonorSkipRoutine = true;
        NetworkManager.Inst.AddReceiveDelegate(HSReceiveNetworkAction);
        var askingPanel = IngameUIManager.Inst.AskingPanel;

        /// 물어보기
        askingPanel.SetAskingPanelString("Do Snap??");
        askingPanel.SetAskingPanelActions
            (
                () =>
                {
                    turnStates[(int)FirstPlayer] = TurnState.WAITFORHSCONSENT;
                },
                () =>
                {
                    turnStates[(int)FirstPlayer] = TurnState.NORMAL;
                    nextTurnStates[(int)FirstPlayer] = TurnState.NORMAL;
                    nextTurnStates[(int)SecondPlayer] = TurnState.WAITFORHS;
                }
            );
        askingPanel.SetAskingPanelActive();

        // 선공의 HS 여부 선택
        // 이런 부분들 전부 yield return WaitUntil()로 변경 예정
        while (turnStates[(int)FirstPlayer] == TurnState.WAITFORHS)
        {
            // wait for answer
            yield return null;
        }

        // 선공이 HS한 경우, 후공에게 동의를 물어봄
        if (turnStates[(int)FirstPlayer] == TurnState.WAITFORHSCONSENT)
        {
            HSSendNetworkAction("first true");

            while (turnStates[(int)FirstPlayer] == TurnState.WAITFORHSCONSENT)
            {
                // wait for answer
                yield return null;
            }

            if (turnStates[(int)FirstPlayer] == TurnState.HONORSKIP)
            {
                // 선공의 HS 수행, 후공의 normal turn 진행
                TurnInfoSendNetworkAction();
            }
        }

        // 선공이 HS하지 않은 경우 && 후공이 동의하지 않은 경우
        if (turnStates[(int)FirstPlayer] == TurnState.NORMAL)
        {
            // 선공의 normal turn (2) 진행
            TurnInfoSendNetworkAction();

            while (totalTurn == 2)
            {
                yield return null;
            }

            HSSendNetworkAction("first false");

            while (turnStates[(int)SecondPlayer] == TurnState.WAITFORHS)
            {
                // wait for answer
                yield return null;
            }

            // HONORSKIP이든 NORMAL이든 결국 다시 서로 동기화해서 처리해야함
            TurnInfoSendNetworkAction();
        }

        NetworkManager.Inst.RemoveReceiveDelegate(HSReceiveNetworkAction);
        isInHonorSkipRoutine = false;
    }

    private IEnumerator EHonorSkipSecondRoutine()
    {
        NetworkManager.Inst.AddReceiveDelegate(HSReceiveNetworkAction);

        yield return null;

        // 후에는 hs 끝나면 없애는거까지
    }

    private void HSReceiveNetworkAction(MyNetworkData.Packet packet)
    {
        if (packet.Type != (short)MyNetworkData.PacketType.ROOM_OPPONENT) return;

        var msg = MyNetworkData.MessagePacket.Deserialize(packet.Data);

        if (!msg.message.StartsWith("HS/")) return;

        var msgArr = msg.message.Split(' ');

        switch (msgArr[1], msgArr[2])
        {
            // first player 수신
            case ("first", "agree"): // first의 hs에 동의
                // 동의시 next: 3 | HONORSKIP | NORMAL
                nextTotalTurn = 3;
                nextTurnStates[(int)FirstPlayer] = TurnState.HONORSKIP;
                nextTurnStates[(int)SecondPlayer] = TurnState.NORMAL;

                turnStates[(int)FirstPlayer] = TurnState.HONORSKIP;
                break;
            case ("first", "reject"): // first의 hs를 거부 -> first가 normal 1턴을 진행
                // 거부시 now: 2 | NORMAL | WAITFORHS
                nextTotalTurn = 2;
                nextTurnStates[(int)FirstPlayer] = TurnState.NORMAL;
                nextTurnStates[(int)SecondPlayer] = TurnState.WAITFORHS;

                turnStates[(int)FirstPlayer] = TurnState.NORMAL;
                break;
            case ("second", "true"): // second의 hs
                nextTotalTurn = 4;
                nextTurnStates[(int)FirstPlayer] = TurnState.NORMAL;
                nextTurnStates[(int)SecondPlayer] = TurnState.HONORSKIP;

                turnStates[(int)SecondPlayer] = TurnState.HONORSKIP;
                break;
            case ("second", "false"):
                nextTotalTurn = 3;
                nextTurnStates[(int)FirstPlayer] = TurnState.WAIT;
                nextTurnStates[(int)SecondPlayer] = TurnState.NORMAL;

                turnStates[(int)SecondPlayer] = TurnState.NORMAL;
                break;
            // second player의 수신
            case ("first", "true"): // first의 hs 선언에 동의를 구하기
                StartCoroutine(EHSSecondAsking("Enemy snap ok???", "first agree", "first reject"));
                break;
            case ("first", "false"): // first가 normal turn (2)을 진행 (no HS)
                // 상대의 턴 종료시 내가 HS할지를 결정
                StartCoroutine(EHSSecondAsking("Do Snap???", "second true", "second false"));
                break;
        }
    }

    private void HSSendNetworkAction(string message)
    {
        NetworkManager.Inst.SendData(new MyNetworkData.MessagePacket
        {
            senderID = NetworkManager.Inst.NetworkId,
            message = $"HS/ {message}",
        }, MyNetworkData.PacketType.ROOM_OPPONENT);
    }

    private IEnumerator EHSSecondAsking(string question, string messageTrue, string messageFalse)
    {
        bool isAnswerSelected = false;
        var askingPanel = IngameUIManager.Inst.AskingPanel;

        askingPanel.SetAskingPanelString(question);
        askingPanel.SetAskingPanelActions
            (
                () =>
                {
                    HSSendNetworkAction(messageTrue);
                    isAnswerSelected = true;
                },
                () =>
                {
                    HSSendNetworkAction(messageFalse);
                    isAnswerSelected = true;
                }
            );
        askingPanel.SetAskingPanelActive();

        while (!isAnswerSelected)
        {
            yield return null;
        }
    }
    #endregion

    // 아너스킵
    // 게임중 서버 통신
    // 메세지 받아서 뿌려주기
    // 메세지 보내기
    // 게임보드 동기화 (상대 턴일때)
}