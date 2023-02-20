using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RhinoElephantStoneBehaviour : StoneBehaviour
{
    public override void InitProperty()
    {
        base.InitProperty();

        AddProperty(new PinnedProperty(this, 1));
    }
}
