using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CycloneStoneBehaviour : StoneBehaviour
{
    public override void OnEnter()
    {
        PlayerBehaviour player = GameManager.Inst.players[(int)ownerPlayer];
        foreach (StoneBehaviour stone in player.Stones.Values)
        {
            stone.AddProperty(new SprintProperty(stone));
        }
    }
}
