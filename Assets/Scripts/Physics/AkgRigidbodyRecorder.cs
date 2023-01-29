using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AkgRigidbodyRecorder : MonoBehaviour
{
    private readonly List<VelocityRecord> records = new();
    private bool isRecording;
    public bool IsRecording => isRecording;

    public void StartRecord()
    {
        isRecording = true;
    }

    public List<VelocityRecord> EndRecord()
    {
        isRecording = false;
        var exporting = new List<VelocityRecord>(records);
        records.Clear();

        return exporting;
    }

    public void PlayRecord(List<VelocityRecord> records)
    {
        // zz
    }

    public struct VelocityRecord
    {
        public float time;
        public int stoneId;
        public float xVelocity;
        public float zVelocity;
    }
}
