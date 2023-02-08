using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AkgPhysicsManager : SingletonBehavior<AkgPhysicsManager>
{
    private HashSet<AkgRigidbody> rigidbodies;
    [SerializeField] private int rigidbodyCounter = 0;

    public enum AkgLayerMaskEnum
    {
        DEFAULT = 0,
        LOCALSTONE = 1,
        OPPOSTONE = 1 << 1,
        LOCALGHOST = 1 << 2,
        OPPOGHOST = 1 << 3,
        LOCALGUARD = 1 << 4,
        OPPOGUARD = 1 << 5,
    }
    [Tooltip(@"
        비트마스킹으로 쓰면됨
        ex) OPPOSTONE(0)은 OPPOGUARD와 OPPOSTONE끼리 -> (10001),
            LOCALSTONE과 충돌 가능 -> LOCALSTONE에 (0100'1'1)
    ")]
    [SerializeField]
    private Dictionary<AkgLayerMaskEnum, short> layerMasks = new Dictionary<AkgLayerMaskEnum, short>
    {
        { AkgLayerMaskEnum.LOCALSTONE,      0b_010011 },
        { AkgLayerMaskEnum.OPPOSTONE,       0b_10001_0 },
        { AkgLayerMaskEnum.LOCALGHOST,      0b_0100_00 },
        { AkgLayerMaskEnum.OPPOGHOST,       0b_100_000 },
        { AkgLayerMaskEnum.LOCALGUARD,      0b_00_0000 },
        { AkgLayerMaskEnum.OPPOGUARD,       0b_0_00000 },
    };

    private void Awake()
    {
        rigidbodies = new();
    }

    #region Rigidbody List control
    public void AddAkgRigidbody(AkgRigidbody newObject)
    {
        rigidbodies.Add(newObject);
        rigidbodyCounter = rigidbodies.Count;
    }

    public void RemoveAkgRigidbody(AkgRigidbody newObject)
    {
        rigidbodies.Remove(newObject);
        rigidbodyCounter = rigidbodies.Count;
    }

    public AkgRigidbody[] GetFilteredRigidbodies(System.Func<AkgRigidbody, bool> func)
    {
        return rigidbodies.Where(func).ToArray();
    }
    #endregion

    #region Layer control
    public bool GetLayerCollide(AkgLayerMaskEnum l1, AkgLayerMaskEnum l2)
    {
        return l1 == AkgLayerMaskEnum.DEFAULT || l2 == AkgLayerMaskEnum.DEFAULT || (layerMasks[l1] & (short)l2) != 0;
    }
    #endregion
}
