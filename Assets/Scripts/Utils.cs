using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RPS
{
    public Vector3 pos;
    public Quaternion rot;
    public Vector3 scale;

    public RPS(Vector3 pos, Quaternion rot, Vector3 scale)
    {
        this.pos = pos;
        this.rot = rot;
        this.scale = scale;
    }
}

public class Utils
{
    
}
