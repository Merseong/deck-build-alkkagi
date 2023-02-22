using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursedProperty : StoneProperty
{
    public CursedProperty(StoneBehaviour stone, int turn = -1) : base(stone, turn) { }

    public override void OnAdded(bool isReplaced = false)
    {
        base.OnAdded(isReplaced);

        if (!isReplaced)
        {
            baseStone.transform.GetChild(4).GetChild(0).gameObject.SetActive(true);
        }
    }

    public override void OnRemoved(bool isReplaced = false)
    {
        base.OnRemoved(isReplaced);

        if (!isReplaced)
        {
            baseStone.transform.GetChild(4).GetChild(0).gameObject.SetActive(true);
        }
    }

    public override float GetMass(float value) { return 0.6f * value; }
}
