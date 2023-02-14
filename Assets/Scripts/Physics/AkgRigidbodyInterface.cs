using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface AkgRigidbodyInterface
{
    /// <param name="collider">충돌한 상대의 정보</param>
    /// <param name="collidePoint">충돌점의 좌표</param>
    /// <param name="isCollided">충돌 당했는지의 여부</param>
    public abstract void OnCollide(AkgRigidbody collider, Vector3 collidePoint, bool isCollided);
}
