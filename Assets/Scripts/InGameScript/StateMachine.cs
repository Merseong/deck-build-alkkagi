using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class StateMachine
{
    public GameManager.TurnState curState { get; private set; }
    public PlayerBehaviour player;
    protected bool isInTouch = false;

    // 생성자
    public StateMachine(PlayerBehaviour player, GameManager.TurnState defaultState)
    {
        curState = defaultState;
        this.player = player;
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
        Vector3 curScreenTouchPosition = TouchManager.Inst.touchPositionAction.ReadValue<Vector2>();

        isInTouch = true;
        player.turnActionDic[curState][0](curScreenTouchPosition);
    }

    private void InTouchAction()
    {
        Vector3 curScreenTouchPosition = TouchManager.Inst.touchPositionAction.ReadValue<Vector2>();

        player.turnActionDic[curState][1](curScreenTouchPosition);
    }
    private void TouchEndAction(InputAction.CallbackContext context)
    {   
        Vector3 curScreenTouchPosition = TouchManager.Inst.touchPositionAction.ReadValue<Vector2>();

        player.turnActionDic[curState][2](curScreenTouchPosition);
        isInTouch = false;
    }
}