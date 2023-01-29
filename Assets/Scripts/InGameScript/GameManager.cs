using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GameManager : SingletonBehavior<GameManager>
{
    // 각 플레이어
    public bool isLocalGoFirst;
    //선,후공과 관계없이 HS를 했는지 여부에 대한 bool
    public bool isPlayerHonorSkip;
    //후공의 경우에 상대가 HS했을 경우 동의 여부에 대한 bool
    public bool isPlayerConsentHonorSkip;
    public PlayerBehaviour localPlayer;
    public PlayerBehaviour opponentPlayer;
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
        NORMAL,
        FNORMAL,
        HONORSKIP,
        HSCONSENT,
        LENGTH
    }

    public TurnState localTurn;
    public TurnState oppoNextTurn;
    public TurnState localNextTurn;

    public void Start()
    {
        InitializeTurn();
        OnTurnEnd += SetNextTurnState;
    }
    
    public void InitializeTurn()
    {
        if(isLocalGoFirst)
        {
            localTurn = TurnState.PREPARE;
        }
        else
        {
            localTurn = TurnState.WAIT;
        }
    }

    public void SetTurn()
    {
        OnTurnEnd(localTurn);
        //상대 턴 변경(네트워킹 작업 필요)
        //(상대 게임메니저).SetTurn(oppoNextTurn);
        localTurn = localNextTurn;
        OnTurnStart(localNextTurn);
    }
    public void SetTurn(TurnState dest)
    {
        OnTurnEnd(localTurn);
        localTurn = dest;
        OnTurnStart(dest);
    }

    private void SetNextTurnState(TurnState curTurn)
    {
        if(curTurn == TurnState.WAIT) return;

        if(isLocalGoFirst)
            switch(curTurn){
                case TurnState.FNORMAL :
                    localNextTurn = TurnState.WAIT;
                    oppoNextTurn = TurnState.HONORSKIP;
                    break;
                case TurnState.PREPARE :
                    localNextTurn = TurnState.WAIT;
                    oppoNextTurn = TurnState.PREPARE;
                    break;
                case TurnState.HONORSKIP :
                    if(isPlayerHonorSkip)
                    {
                        localNextTurn = TurnState.WAIT;
                        oppoNextTurn = TurnState.HSCONSENT;
                    }
                    else
                    {
                        localNextTurn = TurnState.FNORMAL;
                        oppoNextTurn = TurnState.WAIT;
                    }
                    break;    
                case TurnState.NORMAL : 
                    localNextTurn = TurnState.WAIT;
                    oppoNextTurn = TurnState.NORMAL;
                    break;
                default :
                    Debug.LogError("Undefined state!");
                    break;
            }
        else
        {
            switch(curTurn){
                case TurnState.PREPARE :
                    localNextTurn = TurnState.WAIT; 
                    oppoNextTurn = TurnState.HONORSKIP;
                    break;
                case TurnState.HONORSKIP : 
                    if(isPlayerHonorSkip)
                    {
                        localNextTurn = TurnState.WAIT; 
                        oppoNextTurn = TurnState.NORMAL;
                    }
                    else
                    {
                        localNextTurn = TurnState.NORMAL; 
                        oppoNextTurn = TurnState.WAIT;
                    }
                    break;
                case TurnState.HSCONSENT :
                    //Agree
                    if(isPlayerConsentHonorSkip)
                    {
                        localNextTurn = TurnState.NORMAL; 
                        oppoNextTurn = TurnState.WAIT;
                    }
                    //Denial
                    else
                    {
                        localNextTurn = TurnState.WAIT;
                        oppoNextTurn = TurnState.FNORMAL; 
                    }
                    break;    
                case TurnState.NORMAL : 
                    localNextTurn = TurnState.WAIT; 
                    oppoNextTurn = TurnState.NORMAL;
                    break;
                default :
                    Debug.LogError("Undefined state!");
                    break;
            }
        }
    }

    // 코스트 관련 동작
    // 아너스킵

    // 게임중 서버 통신
    // 메세지 받아서 뿌려주기
    // 메세지 보내기
    // 게임보드 동기화 (상대 턴일때)
}