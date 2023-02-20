using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicianStoneBehaviour: StoneBehaviour
{
    public override void InitProperty()
    {
        base.InitProperty();

        AddProperty(new ShieldProperty(this));
    }
}
