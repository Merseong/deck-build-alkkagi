using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface AkgRigidbodyInterface
{
    public abstract void OnCollide(AkgRigidbody collider, Vector3 collidePoint);
}
