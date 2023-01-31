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

    // 서버에서 선공이 누구인지 정해줘야함
    public bool isLocalGoFirst;
    PlayerEnum FirstPlayer => isLocalGoFirst ? PlayerEnum.LOCAL: PlayerEnum.OPPO;
    PlayerEnum SecondPlayer => isLocalGoFirst ? PlayerEnum.OPPO : PlayerEnum.LOCAL;
    //선,후공과 관계없이 HS를 했는지 여부에 대한 bool
    public bool isPlayerHonorSkip;
    //후공의 경우에 상대가 HS했을 경우 동의 여부에 대한 bool
    public bool isPlayerConsentHonorSkip;

    // 현재 보드

    // 각자 턴의 제어
    // WAIT가 아닌 턴의 종료가 일어나는 PLAYER쪽에서 턴 변경 처리
    // 턴 시작 시 처리되어야하는 일 ex) UI, 코스트 증감, 드로우 등등
    public event Action<TurnState> OnTurnStart;
    public event Action<TurnState> OnTurnEnd;
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

    // 0: 준비턴 선공, 1: 준비턴 후공
    // 2: 1턴 선공, 3: 1턴 후공
    // 이후는 노말턴 선공 및 후공
    private int turnCount = 0;
    private int nextTurnCount;
    public int TurnCount => turnCount / 2;
    public PlayerEnum WhoseTurn => (PlayerEnum)((turnCount + (isLocalGoFirst ? 0 : 1)) % 2);

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
        InitializeTurn();
        //OnTurnEnd += SetNextTurnState;
    }
    
    public void InitializeTurn()
    {
        turnStates[(int)FirstPlayer] = TurnState.PREPARE;
        turnStates[(int)SecondPlayer] = TurnState.WAIT;
    }

    // 네트워크에서 turnCount, playerTurns 받아오기
    // 아마 void가 아니고 패킷과 같은 형태로 주고받아야 할듯
    private async void GetTurnInfo()
    {
        // TODO
    }
    // 네트워크에 turnCount, playerTurns 전송
    // 아마 void가 아니고 패킷과 같은 형태로 주고받아야 할듯
    private async void SendTurnInfo()
    {
        // TODO
    }

    public void TurnEnd()
    {
        OnTurnEnd(LocalTurnState);
        SetNextTurnState();
        // 상대 GameManager의 turn info 변경
        // 예시
        // SomeNetworkPacket result = await SendTurnInfo();
        // if (result.applied)
        LocalTurnState = LocalNextTurnState;
        OppoTurnState = OppoNextTurnState;
        turnCount = nextTurnCount;
        OnTurnStart(LocalTurnState);
    }

    private void SetNextTurnState()
    {
        if (WhoseTurn == PlayerEnum.OPPO)
        {
            Debug.LogError("Waiting player ended turn!");
            return;
        }

        nextTurnCount = TurnCount;

        switch (turnCount)
        {
            // 준비턴 선공
            case 0:
                LocalNextTurnState = TurnState.WAIT;
                OppoNextTurnState = TurnState.PREPARE;
                nextTurnCount++;
                break;
            // 준비턴 후공
            case 1:
                LocalNextTurnState = TurnState.WAITFORHS;
                OppoNextTurnState = TurnState.HONORSKIP;
                nextTurnCount++;
                break;
            // 1턴 선공 (HS or FNORMAL)
            case 2:
                FirstHSTurn();
                break;
            // 1턴 후공 (HS or normal)
            case 3:
                SecondHSTurn();
                break;
            // 2턴 이후
            default:
                OppoNormalTurn();
                turnCount++;
                break;
        }
    }

    private void FirstHSTurn()
    {
        switch (LocalTurnState)
        {
            case TurnState.HONORSKIP:
                if (isPlayerHonorSkip)
                {
                    LocalNextTurnState = TurnState.WAITFORHSCONSENT;
                    OppoNextTurnState = TurnState.HSCONSENT;
                }
                else
                {
                    LocalNormalTurn();
                }
                break;
            case TurnState.HSCONSENT:
                if (isPlayerConsentHonorSkip)
                {
                    LocalNormalTurn();
                    nextTurnCount++;
                }
                else
                {
                    OppoNormalTurn();
                }
                break;
            case TurnState.NORMAL:
                LocalNextTurnState = TurnState.WAITFORHS;
                OppoNextTurnState = TurnState.HONORSKIP;
                nextTurnCount++;
                break;
            default:
                Debug.LogError("Invalid state!");
                break;
        }
    }

    private void SecondHSTurn()
    {
        switch (LocalTurnState)
        {
            case TurnState.HONORSKIP:
                if (isPlayerHonorSkip)
                {
                    OppoNormalTurn();
                    nextTurnCount++;
                }
                else
                {
                    LocalNormalTurn();
                }
                break;
            case TurnState.NORMAL:
                OppoNormalTurn();
                nextTurnCount++;
                break;
            default:
                Debug.LogError("Invalid state!");
                break;
        }
    }

    private void LocalNormalTurn()
    {
        LocalNextTurnState = TurnState.NORMAL;
        OppoNextTurnState = TurnState.WAIT;
    }

    private void OppoNormalTurn()
    {
        LocalNextTurnState = TurnState.WAIT;
        OppoNextTurnState = TurnState.NORMAL;
    }
    

    // 아너스킵
    // 게임중 서버 통신
    // 메세지 받아서 뿌려주기
    // 메세지 보내기
    // 게임보드 동기화 (상대 턴일때)
}