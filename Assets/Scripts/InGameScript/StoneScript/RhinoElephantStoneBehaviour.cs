using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RhinoElephantStoneBehaviour : StoneBehaviour
{
    public override void InitProperty()
    {
        base.InitProperty();

        if (GameManager.Inst.TurnCount > 0)
            AddProperty(new PinnedProperty(this, 1));
    }
}
