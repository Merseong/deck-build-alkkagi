using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarlockStoneBehaviour : StoneBehaviour
{
    public override void OnEnter(bool calledByPacket = false, string options = "")
    {
        if (calledByPacket)
        {
            StoneBehaviour stone = GameManager.Inst.FindStone(int.Parse(options));
            stone.AddProperty(new CursedProperty(stone));
        }
        else
        {
            PlayerBehaviour oppo = GameManager.Inst.GetOppoPlayer(BelongingPlayerEnum);

            if (oppo.Stones.Count > 0)
            {
                int randNum = UnityEngine.Random.Range(0, oppo.Stones.Count);
                StoneBehaviour randStone = oppo.Stones[randNum];
                options = randStone.StoneId.ToString();

                randStone.AddProperty(new CursedProperty(randStone));
            }
        }


        base.OnEnter(calledByPacket, options);
    }
}
