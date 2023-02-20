using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarlockStoneBehaviour : StoneBehaviour
{
    public override void OnEnter()
    {
        List<StoneBehaviour> oppoStones = new List<StoneBehaviour>();
        foreach (var stone in GameManager.Inst.AllStones.Values)
        {
            if (stone.BelongingPlayer == GameManager.PlayerEnum.OPPO)
            {
                oppoStones.Add(stone);
            }
        }
        if (oppoStones.Count != 0)
        {
            int randNum = UnityEngine.Random.Range(0, oppoStones.Count);
            oppoStones[randNum].AddProperty(new CursedProperty(oppoStones[randNum]));
        }
    }
}
