using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BladelessManStoneBehaviour : StoneBehaviour
{
    public override void InitProperty()
    {
        base.InitProperty();

        AddProperty(new SprintProperty(this));
    }
}
