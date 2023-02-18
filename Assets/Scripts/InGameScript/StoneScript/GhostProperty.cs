using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostProperty : StoneProperty
{
    public GhostProperty(StoneBehaviour stone, int turn = -1) : base(stone, turn) { }

    public override bool IsGhost(bool value) { return value || (remainingTurn != 0); }
}
