using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursedProperty : StoneProperty
{
    public CursedProperty(StoneBehaviour stone, int turn = -1) : base(stone, turn) { }

    public override float GetMass(float value) { return 0.6f * value; }
}
