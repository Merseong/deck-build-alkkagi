using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WaitTurn : IState
{
    private PlayerBehaviour player;
    public WaitTurn(PlayerBehaviour player)
    {
        this.player = player;
    }

    protected override void InTouchAction()
    {

    }

    protected override void TouchBeginAction(InputAction.CallbackContext context)
    {

    }

    protected override void TouchEndAction(InputAction.CallbackContext context)
    {

    }
    
}
