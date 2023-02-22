using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CherryIceCreamStoneBehaviour : StoneBehaviour
{
    [SerializeField] private int stack = 1;
    private int Stack
    {
        get => stack;
        set
        {
            stack = value;

            transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = GetSpriteState("Idle");
        }
    }

    public override void OnEnter(bool calledByPacket = false, string options = "")
    {
        Stack = 1;

        OnHit += Merge;

        base.OnEnter(calledByPacket, options);
    }

    public override void OnExit(bool calledByPacket = false, string options = "")
    {
        OnHit -= Merge;

        base.OnExit(calledByPacket, options);
    }

    public override float GetMass(float init)
    {
        float stackValue = 0.5f * (1 + Stack);
        return base.GetMass(stackValue * init);
    }

    private void Merge(AkgRigidbody other)
    {
        if (other.TryGetComponent(out CherryIceCreamStoneBehaviour stone) && BelongingPlayerEnum == stone.BelongingPlayerEnum)
        {
            AkgRigidbody akg = GetComponent<AkgRigidbody>();
            CherryIceCreamStoneBehaviour slower = akg.velocity.magnitude > other.velocity.magnitude ? stone : this;
            CherryIceCreamStoneBehaviour faster = akg.velocity.magnitude > other.velocity.magnitude ? this : stone;

            if (this == slower)
            {
                Debug.Log($"{Stack} + {stone.Stack}");

                Upgrade(faster.Stack);

                if (BelongingPlayerEnum == GameManager.PlayerEnum.LOCAL)
                {
                    stone.RemoveStoneFromGame();
                    Destroy(stone.gameObject);
                }
                else
                {
                    stone.GetComponent<AkgRigidbody>().isDisableCollide = true;
                    stone.transform.position = transform.position;
                }
            }
        }
    }

    public override Sprite GetSpriteState(string state)
    {
        if (state.Equals("Idle"))
            state = $"Idle_{Mathf.Min(stack, 3)}";

        Sprite sprite = GameManager.Inst.stoneAtlas.GetSprite($"{cardData.cardEngName}_{state}");
        if (sprite == null)
        {
            sprite = GameManager.Inst.stoneAtlas.GetSprite($"{cardData.cardEngName}_Idle_{Mathf.Min(stack, 3)}");
            Debug.Log($"There is no sprite named \"{cardData.cardEngName}_{state}\"");
        }
        else
        {
            Debug.Log($"Sprite is changed to \"{cardData.cardEngName}_{state}\"");
        }
        return sprite;
    }

    private void Upgrade(int diff)
    {
        Stack += diff;
    }
}
