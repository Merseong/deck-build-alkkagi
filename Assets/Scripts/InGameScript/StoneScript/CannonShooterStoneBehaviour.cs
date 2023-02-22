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

        //base.OnEnter(calledByPacket, options);
    }

    public override void OnExit(bool calledByPacket = false, string options = "")
    {
        OnShootEnter -= ShootBullet;

        //base.OnExit(calledByPacket, options);
    }

    public override void ParseActionString(string actionStr)
    {
        base.ParseActionString(actionStr);

        if (actionStr.StartsWith("SPAWN"))
        {
            var actionArr = actionStr.Split(' ');
            var stoneId = int.Parse(actionArr[1]);

            CardData cardData = Util.GetCardDataFromID(500, GameManager.Inst.CardDatas);
            GameManager.Inst.OppoPlayer.SpawnStone(cardData, transform.position, stoneId, true);
            StoneBehaviour bullet = GameManager.Inst.FindStone(stoneId);
            bullet.OnEnter();
            var bulletAkg = bullet.GetComponent<AkgRigidbody>();
            var thisAkg = GetComponent<AkgRigidbody>();
            bulletAkg.IgnoreCollide.Add(thisAkg);
            thisAkg.IgnoreCollide.Add(bulletAkg);
        }
    }

    private void ShootBullet()
    {
        if (BelongingPlayerEnum == GameManager.PlayerEnum.LOCAL)
        {
            CardData cardData = Util.GetCardDataFromID(500, GameManager.Inst.CardDatas);
            var stoneID = GameManager.Inst.LocalPlayer.SpawnStone(cardData, transform.position, -1, true);
            AkgPhysicsManager.Inst.rigidbodyRecorder.AppendEventRecord(StoneId, $"SPAWN {stoneID}");
            StoneBehaviour bullet = GameManager.Inst.FindStone(stoneID);
            var bulletAkg = bullet.GetComponent<AkgRigidbody>();
            var thisAkg = GetComponent<AkgRigidbody>();
            bulletAkg.IgnoreCollide.Add(thisAkg);
            thisAkg.IgnoreCollide.Add(bulletAkg);

            bullet.ChildShoot(-akgRigidbody.velocity, true);
        }
    }
}