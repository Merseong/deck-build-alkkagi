using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

[Flags]
public enum AkgLayerMask
{
    DEFAULT = 0,
    LOCAL = 1,
    OPPO = 1 << 1,
    STONE = 1 << 2,
    GHOST = 1 << 3,
    SHIELD = 1 << 4,
    COLLIDED = 1 << 5,
}

public class AkgPhysicsManager : SingletonBehavior<AkgPhysicsManager>
{
    /// <summary>
    /// 물리 기록용 recorder
    /// </summary>
    public readonly AkgRigidbodyRecorder rigidbodyRecorder = new AkgRigidbodyRecorder();
    public bool IsRecordPlaying => rigidbodyRecorder.IsPlaying;

    /// <summary>
    /// 충돌 감지용 게임 내의 AkgRigidbody
    /// </summary>
    private HashSet<AkgRigidbody> rigidbodies;
    [SerializeField] private int rigidbodyCounter = 0;

    //[Tooltip(@"
    //    비트마스킹으로 쓰면됨
    //    ex) OPPOSTONE(0)은 OPPOGUARD와 OPPOSTONE끼리 -> (10001),
    //        LOCALSTONE과 충돌 가능 -> LOCALSTONE에 (0100'1'1)
    //")]
    //[SerializeField]
    //private Dictionary<AkgLayerMaskEnum, short> layerMasks = new Dictionary<AkgLayerMaskEnum, short>
    //{
    //    { AkgLayerMaskEnum.LOCALSTONE,          0b_0001110011 },
    //    { AkgLayerMaskEnum.OPPOSTONE,           0b_0010110010 },
    //    { AkgLayerMaskEnum.LOCALGHOST,          0b_0001000000 },
    //    { AkgLayerMaskEnum.OPPOGHOST,           0b_0010000000 },
    //    { AkgLayerMaskEnum.LOCALSHIELD,         0b_0101110000 },
    //    { AkgLayerMaskEnum.OPPOSHIELD,          0b_1010110000 },
    //    { AkgLayerMaskEnum.LOCALGUARD,          0b_0000000000 },
    //    { AkgLayerMaskEnum.OPPOGUARD,           0b_0000000000 },
    //    { AkgLayerMaskEnum.LOCALCOLLIDEDGUARD,  0b_0000000000 },
    //    { AkgLayerMaskEnum.OPPOCOLLIDEDGUARD,   0b_0000000000 },
    //};

    protected override void Awake()
    {
        base.Awake();
        rigidbodies = new();
        rigidbodyRecorder.InitRecorder();
    }

    #region Rigidbody List control
    public void AddAkgRigidbody(AkgRigidbody newObject)
    {
        rigidbodies.Add(newObject);
        rigidbodyCounter = rigidbodies.Count;
    }

    public void RemoveAkgRigidbody(AkgRigidbody newObject)
    {
        // Debug.Log($"Remove {newObject.GetInstanceID()}");
        rigidbodies.Remove(newObject);
        rigidbodyCounter = rigidbodies.Count;
    }

    public AkgRigidbody[] GetFilteredRigidbodies(System.Func<AkgRigidbody, bool> func)
    {
        return rigidbodies.Where(func).ToArray();
    }
    #endregion

    #region Layer control
    public bool GetLayerCollide(AkgLayerMask l1, AkgLayerMask l2)
    {
        if (l1 == AkgLayerMask.DEFAULT || l2 == AkgLayerMask.DEFAULT)
            return true;

        if (!l1.HasFlag(AkgLayerMask.STONE))
            return false;

        if (!l2.HasFlag(AkgLayerMask.STONE))
        {
            if (l1.HasFlag(AkgLayerMask.SHIELD))
                return true;

            if (!IsSameSide(l1, l2))
                return false;

            return !l2.HasFlag(AkgLayerMask.COLLIDED);
        }

        return !l1.HasFlag(AkgLayerMask.GHOST) && !l2.HasFlag(AkgLayerMask.GHOST);
    }

    private bool IsSameSide(AkgLayerMask l1, AkgLayerMask l2)
    {
        return (~(l1 ^ l2) & (AkgLayerMask.LOCAL | AkgLayerMask.OPPO)) != 0;
    }
    #endregion
}
