using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class NormalTurn : IState
{
    private PlayerBehaviour player;

    public NormalTurn(PlayerBehaviour player)
    {
        this.player = player;
    }

    protected override void TouchBeginAction(InputAction.CallbackContext context)
    {
        Vector3 curScreenTouchPosition = TouchManager.Inst.touchPositionAction.ReadValue<Vector2>();

        isInTouch = true;
        player.NormalTouchBegin(curScreenTouchPosition);
    }

    protected override void InTouchAction()
    {
        Vector3 curScreenTouchPosition = TouchManager.Inst.touchPositionAction.ReadValue<Vector2>();

        player.NormalInTouch(curScreenTouchPosition);
    }
    protected override void TouchEndAction(InputAction.CallbackContext context)
    {   
        Vector3 curScreenTouchPosition = TouchManager.Inst.touchPositionAction.ReadValue<Vector2>();

        player.NormalTouchEnd(curScreenTouchPosition);
        isInTouch = false;
    }
}
