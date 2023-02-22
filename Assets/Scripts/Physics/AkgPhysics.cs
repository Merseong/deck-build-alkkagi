using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AkgPhysics
{
    public static float dragAccel = 80.0f;

    public static Vector3 Collide1D(float mass1, Vector3 velocity1, float mass2, Vector3 velocity2, float cor)
    {
        Vector3 newVelocity = mass1 * velocity1 + mass2 * velocity2 + mass2 * cor * (velocity2 - velocity1);
        newVelocity = newVelocity / (mass1 + mass2);
        return newVelocity;
    }
}
