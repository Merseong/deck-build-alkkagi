using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GreasedProperty : StoneProperty
{
    public GreasedProperty(StoneBehaviour stone, int turn = -1) : base(stone, turn) { }

    public override void OnAdded(bool isReplaced = false)
    {
        base.OnAdded(isReplaced);

        baseStone.OnShootExit += RemoveProperty;
    }

    public override void OnRemoved(bool isReplaced = false)
    {
        base.OnRemoved(isReplaced);

        baseStone.OnShootExit -= RemoveProperty;
    }

    public override float GetDragAccel(float value) { return 0.5f * value; }
}
