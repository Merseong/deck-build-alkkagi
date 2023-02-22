using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CherryIceCreamStoneBehaviour : StoneBehaviour
{
    private int stack = 1;
    private int Stack
    {
        get => stack;
        set
        {
            stack = value;

            transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = GetSpriteState($"Idle_{Mathf.Min(stack, 3)}");
        }
    }

    public override void OnEnter(bool calledByPacket = false, string options = "")
    {
        stack = 1;

        if (calledByPacket)
        {
            OnHit += OppoMerge;
        }
        else
        {
            OnHit += Merge;
        }

        base.OnEnter(calledByPacket, options);
    }

    public override void OnExit(bool calledByPacket = false, string options = "")
    {
        OnHit -= Merge;
        OnHit -= OppoMerge;

        base.OnExit(calledByPacket, options);
    }

    public override float GetMass(float init)
    {
        float stackValue = 0.5f * (1 + stack);
        return base.GetMass(stackValue * init);
    }

    private void Merge(AkgRigidbody other)
    {
        if (other.TryGetComponent<CherryIceCreamStoneBehaviour>(out CherryIceCreamStoneBehaviour stone) && BelongingPlayerEnum == stone.BelongingPlayerEnum)
        {
            AkgRigidbody akg = GetComponent<AkgRigidbody>();
            CherryIceCreamStoneBehaviour slower = akg.velocity.magnitude > other.velocity.magnitude ? stone : this;
            CherryIceCreamStoneBehaviour faster = akg.velocity.magnitude > other.velocity.magnitude ? this : stone;

            slower.Upgrade(faster.stack);

            faster.RemoveStoneFromGame();
            faster.StartCoroutine(faster.EIndirectExit(true));
        }
    }

    private void OppoMerge(AkgRigidbody other)
    {
        if (other.TryGetComponent<CherryIceCreamStoneBehaviour>(out CherryIceCreamStoneBehaviour stone) && BelongingPlayerEnum == stone.BelongingPlayerEnum)
        {
            AkgRigidbody akg = GetComponent<AkgRigidbody>();
            CherryIceCreamStoneBehaviour slower = akg.velocity.magnitude > other.velocity.magnitude ? stone : this;
            CherryIceCreamStoneBehaviour faster = akg.velocity.magnitude > other.velocity.magnitude ? this : stone;

            slower.Upgrade(faster.stack);
            faster.GetComponent<AkgRigidbody>().isDisableCollide = true;
            faster.transform.position = slower.transform.position;
        }
    }

    private void Upgrade(int diff)
    {
        stack += diff;
    }
}
