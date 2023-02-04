using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyNetworkData;

public class AkgRigidbodyRecorder
{
    private readonly List<VelocityRecord> records = new();
    private bool isRecording;
    private float startTime;
    public bool IsRecording => isRecording;

    public void StartRecord(float starting)
    {
        isRecording = true;
        startTime = starting;
    }

    public List<VelocityRecord> EndRecord()
    {
        isRecording = false;
        var exporting = new List<VelocityRecord>(records);
        records.Clear();

        return exporting;
    }

    public void AppendVelocity(VelocityRecord record)
    {
        record.time -= startTime;
        records.Add(record);
    }

    public void PlayRecord(List<VelocityRecord> records)
    {
        // zz
    }
}
