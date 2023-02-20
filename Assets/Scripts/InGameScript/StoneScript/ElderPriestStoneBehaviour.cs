using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ElderPriestStoneBehaviour : StoneBehaviour
{
    protected override void StoneCollisionProperty(AkgRigidbody collider, Vector3 collidePoint, bool isCollided)
    {
        if(isCollided) return;
        if(!collider.layerMask.HasFlag(AkgLayerMask.LOCAL) || !collider.layerMask.HasFlag(AkgLayerMask.STONE)) return;
        if(GameManager.Inst.players[(int)BelongingPlayer].StrikingStone == (this as StoneBehaviour))
        {
            ApplyShield(collider.GetComponent<StoneBehaviour>());
            
            AkgPhysicsManager.Inst.rigidbodyRecorder.SendEventOnly(new EventRecord
            {
                eventEnum = EventEnum.POWER,
                stoneId = StoneId,
                eventMessage = collider.GetComponent<StoneBehaviour>().StoneId.ToString(),
                time = Time.time,
            });
        }
    }

    public void ApplyShield(StoneBehaviour SB)
    {
        // Debug.Log(SB.StoneId);
        AddProperty(new ShieldProperty(SB, 1));
    }

    public override void ParseActionString(string actionStr)
    {
        base.ParseActionString(actionStr);
        ApplyShield(GameManager.Inst.FindStone(Int32.Parse(actionStr)));
    }
}
