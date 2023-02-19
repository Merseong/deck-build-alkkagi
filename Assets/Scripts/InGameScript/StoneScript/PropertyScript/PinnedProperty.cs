using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PinnedProperty : StoneProperty
{
    public PinnedProperty(StoneBehaviour stone, int turn = -1) : base(stone, turn) { }

    public override bool IsStatic(bool value) { return value || (remainingTurn != 0); }
}
