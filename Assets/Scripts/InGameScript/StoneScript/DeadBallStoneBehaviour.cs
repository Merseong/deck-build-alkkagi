using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeadBallStoneBehaviour : StoneBehaviour
{
    public override void OnEnter()
    {
        OnHit += (akg) =>
        {
            StoneBehaviour stone = akg.gameObject.GetComponent<StoneBehaviour>();
            foreach (var property in stone.Properties)
            {
                if (property is CursedProperty)
                {
                    stone.RemoveStoneFromGame();
                    stone.StartCoroutine(stone.EIndirectExit(true));
                }
            }
        };
    }
}
