using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class StoneProperty
{
    protected StoneBehaviour baseStone;
    protected int leftCount;

    public StoneProperty(StoneBehaviour stone, int count = -1)
    {
        baseStone = stone;
        leftCount = count;
    }

    // for sprint
    public virtual bool CanSprint(bool value) { return value; }
    // for ghost
    public virtual bool IsGhost(bool value) { return value; }
    // for shield
    public virtual int ShieldCount(int value) { return value; }
    // for accel shield
    public virtual bool HasAccelShield(bool value) { return value; }
    // for curse
    public virtual float GetMass(float value) { return value; }
    // for pinned
    public virtual bool IsStatic(bool value) { return value; }
    // for grease
    public virtual float GetDragForce(float value) { return value; }

    // 애니메이션 같은거 여기다 넣으면 될듯
    public abstract void OnSet();
    public abstract void OnUnset();
}
