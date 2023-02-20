using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostDogStoneBehaviour : StoneBehaviour
{
    public override void OnEnter()
    {
        AddProperty(new GhostProperty(this, 1));
    }
}
