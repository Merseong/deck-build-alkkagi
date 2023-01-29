using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class StateMachine
{
    public IState CurruentState { get; private set; }

    // 생성자
    public StateMachine(IState defaultState)
    {
        CurruentState = defaultState;
    }

    // State 전이
    public void SetState(IState state)
    {
        // 같은 상태로 전환
        if (CurruentState == state)
        {
            return;
        }

        CurruentState.OperateExit();

        CurruentState = state;

        CurruentState.OperateEnter();
    }

    public void DoOperateUpdate()
    {
        CurruentState.OperateUpdate();
    }
}

// State 인터페이스
public abstract class IState
{
    protected bool isInTouch = false;
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

    protected abstract void TouchBeginAction(InputAction.CallbackContext context);
    protected abstract void InTouchAction();
    protected abstract void TouchEndAction(InputAction.CallbackContext context);
}