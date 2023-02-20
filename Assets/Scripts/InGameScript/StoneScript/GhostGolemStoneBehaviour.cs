using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostGolemStoneBehaviour : StoneBehaviour
{
    public override void OnEnter()
    {
        AddProperty(new GhostProperty(this, 2));
    }
}
