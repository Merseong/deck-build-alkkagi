using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CannonShooterStoneBehaviour : StoneBehaviour
{
    public override void InitProperty()
    {
        base.InitProperty();

        AddProperty(new AccelShieldProperty(this));
    }

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
        var bulletAkg = bullet.GetComponent<AkgRigidbody>();
        var thisAkg = GetComponent<AkgRigidbody>();
        bulletAkg.IgnoreCollide.Add(thisAkg);
        thisAkg.IgnoreCollide.Add(bulletAkg);

        // TODO: 쏜 stone이 가만히 있음, 호출 시점의 velocity == 0
        bullet._Shoot(-akgRigidbody.velocity, true);
    }

}