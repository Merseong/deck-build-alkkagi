using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class StateMachine
{
    public GameManager.TurnState curState { get; private set; }
    public LocalPlayerBehaviour player;
    protected bool isInTouch = false;

    // 생성자
    public StateMachine(LocalPlayerBehaviour player, GameManager.TurnState defaultState)
    {
        curState = defaultState;
        this.player = player;
        TouchManager.Inst.touchPressAction.started  += TouchBeginAction;
        TouchManager.Inst.touchPressAction.canceled += TouchEndAction;
    }

    // State 전이
    public void SetState(GameManager.TurnState state)
    {
        // 같은 상태로 전환
        if (curState == state)
        {
            return;
        }

        OperateExit();

        //TODO : 코루틴으로 네트워크 처리후에 다음 턴 액션 바인딩되도록 변경
        curState = state;

        OperateEnter();
    }

    public void DoOperateUpdate()
    {
        OperateUpdate();
    }

    public void OperateEnter()
    {   
        TouchManager.Inst.touchPressAction.started  += TouchBeginAction;
        TouchManager.Inst.touchPressAction.canceled += TouchEndAction;
    }

    public void OperateUpdate()
    {
        if(isInTouch) InTouchAction();
    }

    public void OperateExit()
    {
        TouchManager.Inst.touchPressAction.started  -= TouchBeginAction;
        TouchManager.Inst.touchPressAction.canceled -= TouchEndAction;
    }

    private void TouchBeginAction(InputAction.CallbackContext context)
    {
        Vector3 curScreenTouchPosition = TouchManager.Inst.GetTouchPosition();

        isInTouch = true;
        player.turnActionDic[curState][0](curScreenTouchPosition);
    }

    private void InTouchAction()
    {
        Vector3 curScreenTouchPosition = TouchManager.Inst.GetTouchPosition();

        player.turnActionDic[curState][1](curScreenTouchPosition);
    }
    
    private void TouchEndAction(InputAction.CallbackContext context)
    {   
        Vector3 curScreenTouchPosition = TouchManager.Inst.GetTouchPosition();

        player.turnActionDic[curState][2](curScreenTouchPosition);
        isInTouch = false;
    }
}