using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestSphere : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        AkgRigidbody arb = GetComponent<AkgRigidbody>();
        arb.AddForce(10 * new Vector3(-1, 0, -1));
    }
}
