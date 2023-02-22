using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GreasedProperty : StoneProperty
{
    public GreasedProperty(StoneBehaviour stone, int turn = -1) : base(stone, turn) { }

    public override void OnAdded(bool isReplaced = false)
    {
        base.OnAdded(isReplaced);
        if (!isReplaced)
        {
            baseStone.transform.GetChild(4).GetChild(0).gameObject.SetActive(true);
        }

        baseStone.OnShootExit += RemoveProperty;
    }

    public override void OnRemoved(bool isReplaced = false)
    {
        base.OnRemoved(isReplaced);
        if (!isReplaced)
        {
            baseStone.transform.GetChild(4).GetChild(0).gameObject.SetActive(false);
        }

        baseStone.OnShootExit -= RemoveProperty;
    }

    public override float GetDragAccel(float value) { return 0.5f * value; }
}
