using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CannonShooterStoneBehaviour : StoneBehaviour
{
    public override void OnEnter(bool calledByPacket = false, string options = "")
    {
        OnShootEnter += ShootBullet;

        base.OnEnter(calledByPacket, options);
    }

    public override void OnExit(bool calledByPacket = false, string options = "")
    {
        OnShootEnter -= ShootBullet;

        base.OnExit(calledByPacket, options);
    }

    private void ShootBullet()
    {
        CardData cardData = Util.GetCardDataFromID(500, GameManager.Inst.CardDatas);
        var stoneID = GameManager.Inst.LocalPlayer.SpawnStone(cardData, transform.position, -1, true);   
        StoneBehaviour bullet = GameManager.Inst.FindStone(stoneID);

        bullet._Shoot(-akgRigidbody.velocity, true);
    }

}