using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.U2D;
using UnityEngine.SceneManagement;

public class GameManager : SingletonBehavior<GameManager>
{
    public enum PlayerEnum { LOCAL, OPPO }

    // 각 플레이어
    public PlayerBehaviour[] players;  // 0: local, 1: oppo
    public PlayerBehaviour LocalPlayer => players[0];
    public PlayerBehaviour OppoPlayer => players[1];
    public PlayerBehaviour CurrentPlayer => players[(int)WhoseTurn];

    public PlayerBehaviour GetPlayer(PlayerEnum playerEnum) => players[(int)playerEnum];
    public PlayerBehaviour GetOppoPlayer(PlayerEnum playerEnum) => players[1 - (int)playerEnum];

    // Card List 임시용
    public List<CardData> CardDatas;

    // 돌들
    public Dictionary<int, StoneBehaviour> AllStones = new();

    //아틀라스
    public SpriteAtlas stoneAtlas;

    // 현재 보드
    [SerializeField] private GameBoard gameBoard;
    public GameBoard GameBoard => gameBoard;

    // 서버에서 선공이 누구인지 정해줘야함
    public bool isLocalGoFirst;
    PlayerEnum FirstPlayer => isLocalGoFirst ? PlayerEnum.LOCAL : PlayerEnum.OPPO;
    PlayerEnum SecondPlayer => isLocalGoFirst ? PlayerEnum.OPPO : PlayerEnum.LOCAL;

    // 각자 턴의 제어
    // WAIT가 아닌 턴의 종료가 일어나는 PLAYER쪽에서 턴 변경 처리
    // 턴 시작 시 처리되어야하는 일 ex) UI, 코스트 증감, 드로우 등등
    public event Action OnTurnStart;
    public event Action OnTurnEnd;
    public enum TurnState
    {
        PREPARE,
        WAIT,
        WAITFORHS,
        WAITFORHSCONSENT,
        NORMAL,
        HONORSKIP,
        END,
        LENGTH
    }
    private bool isTurnEndSent;

    /// <summary>
    /// 0: 준비턴 선공, 1: 준비턴 후공, 
    /// 2: 1턴 선공, 3: 1턴 후공, 
    /// 이후는 노말턴 선공 및 후공
    /// </summary>
    [SerializeField] private int totalTurn = 0;
    private int lastTurn = -1;
    [SerializeField] private int nextTotalTurn;
    public int TurnCount => totalTurn / 2;
    public PlayerEnum WhoseTurn => (PlayerEnum)((totalTurn + (isLocalGoFirst ? 0 : 1)) % 2);

    public TurnState[] turnStates = { TurnState.PREPARE, TurnState.PREPARE };  // 0: local, 1: oppo
    public TurnState LocalTurnState
    {
        get => turnStates[0];
        set
        {
            turnStates[0] = value;
        }
    }
    public TurnState OppoTurnState
    {
        get => turnStates[1];
        set => turnStates[1] = value;
    }
    public TurnState[] nextTurnStates = { TurnState.PREPARE, TurnState.PREPARE };
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
    private uint hsPlayerUid = 0;
    public bool isHSPerformed => hsPlayerUid != 0;
    public bool isLocalHS => NetworkManager.Inst.NetworkId == hsPlayerUid;

    public ushort initialTurnCost = 6;
    public ushort normalTurnCost = 3;

    public int knightEnterCount = 0;
    public List<int> localDeadStones = new();
    
    public void Start()
    {
        NetworkManager.Inst.AddReceiveDelegate(TurnInfoReceiveNetworkAction);
        NetworkManager.Inst.AddReceiveDelegate(RoomExitReceiveNetworkAction);

#if UNITY_EDITOR
        if (!NetworkManager.Inst.IsNetworkMode) InitializeGame(isLocalGoFirst, "0109190E12", 0, 100.ToString());
#endif
    }

    private void OnDestroy()
    {
        if (NetworkManager.IsEnabled)
        {
            NetworkManager.Inst.RemoveReceiveDelegate(TurnInfoReceiveNetworkAction);
            NetworkManager.Inst.RemoveReceiveDelegate(RoomExitReceiveNetworkAction);
        }
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
    public void InitializeGame(bool localFirst, string localDeckCode, uint oppoUid, string oppoDeckCount)
    {
        OnTurnStart += StartTurnBasis;
        OnTurnStart += OppoStartTurnBasis;

        isLocalGoFirst = localFirst;

        turnStates[(int)FirstPlayer] = TurnState.PREPARE;
        turnStates[(int)SecondPlayer] = TurnState.WAIT;
        nextTurnStates[(int)FirstPlayer] = TurnState.WAIT;
        nextTurnStates[(int)SecondPlayer] = TurnState.PREPARE;

        knightEnterCount = 0;

        // inspector에서 직접 설정 필요
        players[0].InitPlayer(PlayerEnum.LOCAL, NetworkManager.Inst.NetworkId);
        players[0].InitDeck(localDeckCode);
        players[1].InitPlayer(PlayerEnum.OPPO, oppoUid);
        players[1].InitDeck(oppoDeckCount);
        GameBoard.InitGameBoard();

        StartGame();
    }

    public void StartGame()
    {
        LocalPlayer.DrawCards(5);
        LocalPlayer.ResetCost();
        LocalPlayer.ShootTokenAvailable = true;
        OppoPlayer.DrawCards(5);
        OppoPlayer.ResetCost();
        OppoPlayer.ShootTokenAvailable = true;
        UpdateTurnEndButtonText();
        IngameUIManager.Inst.NotificationPanel.Show(isLocalGoFirst ? "선공!" : "후공!");
    }

    public void SurrenderButtonAction()
    {
        GameOverAction(PlayerEnum.LOCAL);
    }

    public void GameOverAction(PlayerEnum loser)
    {
        if (LocalTurnState == TurnState.END || OppoTurnState == TurnState.END) return;

        if (loser == PlayerEnum.LOCAL)
        {
            Debug.Log("Game over!");
            LocalTurnState = TurnState.END;
            OppoTurnState = TurnState.END;
            StartCoroutine(ERoomExitSendNetworkAction());
        }
        else
        {
            Debug.Log("You win!");
            LocalTurnState = TurnState.END;
            OppoTurnState = TurnState.END;
        }
        //Debug.Break();
    }

    private IEnumerator ERoomExitSendNetworkAction()
    {
        // 2초 후 전송
        yield return new WaitForSeconds(2f);

        NetworkManager.Inst.SendData(new MessagePacket
        {
            senderID = NetworkManager.Inst.NetworkId,
            message = $"BREAK",
        }, PacketType.ROOM_CONTROL);

        if (!NetworkManager.Inst.IsNetworkMode)
        {
            RoomExitReceiveNetworkAction(new Packet().Pack(PacketType.ROOM_CONTROL, new MessagePacket
            {
                senderID = 0,
                message = "EXIT/ 0 L",
            }));
        }
    }

    private void RoomExitReceiveNetworkAction(Packet packet)
    {
        if (packet.Type != (short)PacketType.ROOM_CONTROL) return;

        var msg = MessagePacket.Deserialize(packet.Data);

        if (msg.senderID != 0) return;
        if (!msg.message.StartsWith("EXIT/")) return;

        GameObject resultContainer = new();
        resultContainer.name = "ResultContainer";
        var container = resultContainer.AddComponent<ResultContainer>();

        var msgArr = msg.message.Split(" ");

        switch (msgArr[2])
        {
            case "W":
                container.isLocalWin = true;
                break;
            case "L":
                container.isLocalWin = false;
                break;
            default:
                break;
        }

        IngameUIManager.Inst.DeactivateUI();
        IngameUIManager.Inst.SetResultPanel(container.isLocalWin);
        IngameUIManager.Inst.ActivateUI(IngameUIManager.Inst.ResultPanel);

        NetworkManager.Inst.RemoveReceiveDelegate(RoomExitReceiveNetworkAction);
        //SceneManager.LoadScene(2); // load result scene
    }

    private void UpdateTurnEndButtonText()
    {
        IngameUIManager.Inst.TurnEndButtonText.text = $"{(CurrentPlayer is LocalPlayerBehaviour ? "Turn End" : "Wait..")}";
        gameBoard.SetBoardBackground(CurrentPlayer is LocalPlayerBehaviour);
    }

    private void StartTurnBasis()
    {
        if (CurrentPlayer is LocalPlayerBehaviour)
        {
            IngameUIManager.Inst.NotificationPanel.Show("My Turn!");
        }
        switch (LocalTurnState)
        {
            case TurnState.NORMAL:
            case TurnState.WAITFORHS:
                LocalPlayer.StartTurn();
                break;
            default:
                break;
        }
    }

    private void OppoStartTurnBasis()
    {
        switch (OppoTurnState)
        {
            case TurnState.NORMAL:
            case TurnState.WAITFORHS:
                OppoPlayer.StartTurn();
                break;
            default:
                break;
        }
    }

    /// <remarks>
    /// { ROOM_BROADCAST | 0 | TURNEND/ nextTotalTurn stonePosition LocalNextTurnState }
    /// </remarks>
    /// <param name="packet">
    /// { ROOM_BROADCAST | 0 | TURNEND/ nextTotalTurn stonePosition LocalNextTurnState }
    /// </param>
    private void TurnInfoReceiveNetworkAction(Packet packet)
    {
        if (packet.Type != (short)PacketType.ROOM_BROADCAST) return;

        var msg = MessagePacket.Deserialize(packet.Data);

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
        TurnEnd(lastTurn < nextTotalTurn);
    }

    /// <summary>
    /// 네트워크에 턴 완료 신호 전송
    /// </summary>
    /// <remarks>
    /// { ROOM_BROADCAST | networkId | TURNEND/ nextTotalTurn stonePosition localnextturnState OpppNextTurnState }
    /// </remarks>
    private void TurnInfoSendNetworkAction()
    {
        NetworkManager.Inst.SendData(new MessagePacket
        {
            senderID = NetworkManager.Inst.NetworkId,
            message = $"TURNEND/ {nextTotalTurn} {"hello"} {(short)LocalNextTurnState} {(short)OppoNextTurnState}",
        }, PacketType.ROOM_BROADCAST);

        isTurnEndSent = true;
    }

    public void TurnEndButtonAction()
    {
#if UNITY_EDITOR
        if (!NetworkManager.Inst.IsNetworkMode)
        {
            SetNextTurnState();
            TurnEnd();
            return;
        }
#endif
        if (isTurnEndSent)
        {
            Debug.Log("[ME] Turn end already sent!");
            return;
        }
        if (WhoseTurn == PlayerEnum.OPPO)
        {
            Debug.Log("Waiting player ended turn!");
            return;
        }
        if (isInHonorSkipRoutine)
        {
            isTurnEnded = true;
            return;
        }

        SetNextTurnState();

        TurnInfoSendNetworkAction();
    }

    private void TurnEnd(bool newTurnAction = true)
    {
        isTurnEndSent = false;

        if (newTurnAction)
        {
            CurrentPlayer.EndTurn();
            OnTurnEnd?.Invoke();
        }
        // 상대 GameManager의 turn info 변경
        // 예시
        // SomeNetworkPacket result = await SendTurnInfo();
        // if (result.applied)
        LocalTurnState = LocalNextTurnState;
        OppoTurnState = OppoNextTurnState;
        totalTurn = nextTotalTurn;

        // 선턴 && 현재 HS턴
        if (turnStates[(int)FirstPlayer] == TurnState.WAITFORHS && !isInHonorSkipRoutine)
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

        (players[0] as LocalPlayerBehaviour).stateMachine.SetState(LocalTurnState);
        UpdateTurnEndButtonText();

        // TurnState.NORMAL인경우만 내 턴을 시작
        if (newTurnAction) OnTurnStart?.Invoke();

        lastTurn = Math.Max(lastTurn, totalTurn);
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
                nextTurnStates[(int)SecondPlayer] = TurnState.WAIT;
                nextTotalTurn++;
                break;
            // 2: 1턴 선공 (HS or FNORMAL)
            case 2:
                nextTurnStates[(int)FirstPlayer] = TurnState.WAIT;
                nextTurnStates[(int)SecondPlayer] = TurnState.NORMAL;
                nextTotalTurn++;
                break;
            // 3: 1턴 후공 (HS or normal)
            case 3:
                nextTurnStates[(int)FirstPlayer] = TurnState.NORMAL;
                nextTurnStates[(int)SecondPlayer] = TurnState.WAIT;
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
    [Header("HS control")]
    [SerializeField] bool isInHonorSkipRoutine = false;
    [SerializeField] bool isLocalDoAction = false;
    [SerializeField] bool isTurnEnded = false;

    private IEnumerator EHonorSkipRoutine()
    {
        Debug.Log("Start HS Routine");

        isInHonorSkipRoutine = true;

        // 2 | WAITFORHS | WAIT
        isTurnEnded = false;
        yield return new WaitUntil(() => isTurnEnded);
        if (isLocalDoAction) // 행동을 한 경우 -> 3 | WAIT | WAITFORHS
        {
            turnStates[(int)FirstPlayer] = TurnState.WAIT;
            nextTotalTurn = 3;
            nextTurnStates[(int)FirstPlayer] = TurnState.WAIT;
            nextTurnStates[(int)SecondPlayer] = TurnState.WAITFORHS;
            TurnInfoSendNetworkAction();
            yield return new WaitUntil(() => turnStates[(int)FirstPlayer] == TurnState.NORMAL);
            // 4 | NORMAL | WAIT or HS
            if (turnStates[(int)SecondPlayer] == TurnState.HONORSKIP)
            {
                IngameUIManager.Inst.HonorSkipPanel.Show(false);
                SetHSPlayer(OppoPlayer);
            }
        }
        else // 선공이 아무 행동 안하고 턴종시 HS
        {
            IngameUIManager.Inst.HonorSkipPanel.Show(true);
            nextTotalTurn = 3;
            nextTurnStates[(int)FirstPlayer] = TurnState.WAITFORHSCONSENT;
            nextTurnStates[(int)SecondPlayer] = TurnState.NORMAL;
            TurnInfoSendNetworkAction();
            // 상대의 동의동안 대기
            // 3 | WAITFORCON | NORMAL
            yield return new WaitWhile(() => nextTurnStates[(int)FirstPlayer] == TurnState.WAITFORHSCONSENT);
            // 상대의 동의 first agree
            // 4 = 선공 | NORMAL | WAIT
            // 상대방의 거부 first reject
            // 2 = 선공 | NORMAL | WAIT
            if (totalTurn == 4)
            {
                SetHSPlayer(LocalPlayer);
            }
            else if (totalTurn == 2)
            {
                IngameUIManager.Inst.HonorSkipPanel.Show();
            }
        }

        isInHonorSkipRoutine = false;
        Debug.Log("End HS Routine");
    }

    private IEnumerator EHonorSkipSecondRoutine()
    {
        Debug.Log("Start HS Routine");

        isInHonorSkipRoutine = true;

        // 선공의 HS 혹은 일반턴 진행까지 대기
        // 2 | WAITFORHS | WAIT
        yield return new WaitUntil(() =>
            turnStates[(int)FirstPlayer] == TurnState.WAITFORHSCONSENT ||
            turnStates[(int)FirstPlayer] == TurnState.WAIT
        );

        // 선공의 HS -> 후공의 동의
        if (turnStates[(int)FirstPlayer] == TurnState.WAITFORHSCONSENT)
        {
            IngameUIManager.Inst.HonorSkipPanel.Show(false);
            // 3 | WAITFORCONSENT | NORMAL
            isTurnEnded = false;
            yield return new WaitUntil(() => isTurnEnded);

            nextTurnStates[(int)FirstPlayer] = TurnState.NORMAL;
            nextTurnStates[(int)SecondPlayer] = TurnState.WAIT;
            if (isLocalDoAction)
            {
                nextTotalTurn = 4;
                SetHSPlayer(OppoPlayer);
                TurnInfoSendNetworkAction();
                // 후공이 동작을 진행함 == 선공의 HS에 동의 -> 후공의 3턴을 진행한것으로 간주
            }
            else
            {
                totalTurn = 2;
                nextTotalTurn = 2;
                IngameUIManager.Inst.HonorSkipPanel.Show();
                TurnInfoSendNetworkAction();
                // 후공이 동작 없이 턴종함 == 선공의 HS를 거부 -> 선공은 다시 2턴 진행
            }
        }
        else if (turnStates[(int)SecondPlayer] == TurnState.WAITFORHS)
        {
            // 후공의 HS가 가능한 상황 ( 3 | WAIT | WAITFORHS )
            isTurnEnded = false;
            yield return new WaitUntil(() => isTurnEnded);

            nextTotalTurn = 4;
            if (isLocalDoAction)
            {
                // 후공이 액션을 한 경우, HS하지 않음 -> ( 4 | NORMAL | WAIT )
                nextTurnStates[(int)FirstPlayer] = TurnState.NORMAL;
                nextTurnStates[(int)SecondPlayer] = TurnState.WAIT;
                TurnInfoSendNetworkAction();
            }
            else
            {
                // 후공의 HS
                IngameUIManager.Inst.HonorSkipPanel.Show(true);
                nextTurnStates[(int)FirstPlayer] = TurnState.NORMAL;
                nextTurnStates[(int)SecondPlayer] = TurnState.HONORSKIP;
                SetHSPlayer(LocalPlayer);
                TurnInfoSendNetworkAction();
            }
        }

        isInHonorSkipRoutine = false;
        Debug.Log("End HS Routine");
    }
    public void SetLocalDoAction()
    {
        if (!isInHonorSkipRoutine) return;
        isLocalDoAction = true;
    }

    private void SetHSPlayer(PlayerBehaviour player)
    {
        Debug.Log($"HS : {player.Player}");
        hsPlayerUid = player.Uid;
        //IngameUIManager.Inst.NotificationPanel.Show($"HS : {player.Player}");
        IngameUIManager.Inst.HonorMarkImage.gameObject.SetActive(true);
        Sprite sprite;
        if (player == LocalPlayer)
        {
            sprite = IngameUIManager.Inst.UIAtlas.GetSprite("UI_Honor_1");
        }
        else
        {
            sprite = IngameUIManager.Inst.UIAtlas.GetSprite("UI_Honor_0");
        }
        IngameUIManager.Inst.HonorMarkImage.sprite = sprite;

        NetworkManager.Inst.AddReceiveDelegate(HSPlayerReceiveNetworkAction);
        HSPlayerSendNetworkAction(hsPlayerUid);
    }

    private void HSPlayerSendNetworkAction(uint uid)
    {
        NetworkManager.Inst.SendData(new MessagePacket
        {
            senderID = NetworkManager.Inst.NetworkId,
            message = $"HS/ {uid}"
        }, PacketType.ROOM_BROADCAST);
    }

    private void HSPlayerReceiveNetworkAction(Packet p)
    {
        if (p.Type != (short)PacketType.ROOM_BROADCAST) return;
        var msg = MessagePacket.Deserialize(p.Data);

        // 내가 보낸건 무시
        if (msg.senderID == NetworkManager.Inst.NetworkId) return;

        if (msg.message.StartsWith("HS/"))
        {
            var msgArr = msg.message.Split(' ');
            StartCoroutine(CheckHSPlayer(uint.Parse(msgArr[1])));
            NetworkManager.Inst.RemoveReceiveDelegate(HSPlayerReceiveNetworkAction);
        }

        IEnumerator CheckHSPlayer(uint toCheck)
        {
            yield return new WaitUntil(() => hsPlayerUid != 0);

            if (toCheck != hsPlayerUid)
            {
                NetworkManager.Inst.ProblemSendNetworkAction("HS player not matched!");
            }
        }
    }
    #endregion
}