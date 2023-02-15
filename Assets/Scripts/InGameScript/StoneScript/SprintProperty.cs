using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SprintProperty : StoneProperty
{
    // reset every turn
    private bool canSprint = true; 

    public SprintProperty(StoneBehaviour stone, int count = -1) : base(stone, count) { }

    public override bool CanSprint(bool value) { return canSprint; }

    // 애니메이션 같은거 여기다 넣으면 될듯
    public override void OnSet() { }
    public override void OnUnset() { }
}
