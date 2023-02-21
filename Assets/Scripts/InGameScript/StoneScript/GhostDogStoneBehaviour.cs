using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostDogStoneBehaviour : StoneBehaviour
{
    public override void InitProperty()
    {
        base.InitProperty();

        AddProperty(new GhostProperty(this, 1));
    }
}
