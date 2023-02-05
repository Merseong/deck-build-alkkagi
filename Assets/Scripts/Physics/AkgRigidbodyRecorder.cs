using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyNetworkData;

public class AkgRigidbodyRecorder
{
    private readonly List<VelocityRecord> records = new();
    private readonly List<EventRecord> eventRecords = new();
    private bool isRecording;
    private float startTime;
    public bool IsRecording => isRecording;

    public void InitRecorder()
    {
        NetworkManager.Inst.AddReceiveDelegate(ShootRecordReceiveNetworkAction);
    }

    public void StartRecord(float starting)
    {
        isRecording = true;
        startTime = starting;
    }

    public bool EndRecord(out List<VelocityRecord> exportVelocityRecords, out List<EventRecord> exportEventRecords)
    {
        isRecording = false;
        exportVelocityRecords = new List<VelocityRecord>(records);
        exportEventRecords = new List<EventRecord>(eventRecords);
        records.Clear();
        eventRecords.Clear();

        return true;
    }

    public void AppendVelocity(VelocityRecord record)
    {
        record.time -= startTime;
        records.Add(record);
    }

    public void AppendEventRecord(EventRecord eventRecord)
    {
        eventRecord.time -= startTime;
        eventRecords.Add(eventRecord);
    }

    public void PlayRecord(List<VelocityRecord> records)
    {
        // zz
        Debug.Log(JsonUtility.ToJson(records));
    }

    public void SendRecord(List<VelocityRecord> records, List<EventRecord> events)
    {
        var positionRecords = new List<PositionRecord>();
        foreach (var stone in GameManager.Inst.LocalStones)
        {
            positionRecords.Add(new PositionRecord
            {
                stoneId = stone.Key,
                xPosition = stone.Value.transform.position.x,
                zPosition = stone.Value.transform.position.z,
            });
        }
        foreach (var stone in GameManager.Inst.OppoStones)
        {
            positionRecords.Add(new PositionRecord
            {
                stoneId = stone.Key,
                xPosition = stone.Value.transform.position.x,
                zPosition = stone.Value.transform.position.z,
            });
        }

        ShootRecordSendNetworkAction(records, positionRecords, events);
    }

    private void ShootRecordSendNetworkAction(List<VelocityRecord> _velocityRecords, List<PositionRecord> _positionRecords, List<EventRecord> _eventRecords)
    {
        var data = new ShootStonePacket
        {
            senderID = NetworkManager.Inst.NetworkId,
            velocityRecords = _velocityRecords.ToArray(),
            positionRecords = _positionRecords.ToArray(),
            eventRecords = _eventRecords.ToArray(),
            velocityCount = (short)_velocityRecords.Count,
            positionCount = (short)_positionRecords.Count,
            eventCount = (short)_eventRecords.Count,
        };

        NetworkManager.Inst.SendData(data, PacketType.ROOM_OPPO_SHOOTSTONE);

        Debug.Log(JsonUtility.ToJson(data));
    }

    private void ShootRecordReceiveNetworkAction(Packet packet)
    {
        if (packet.Type != (short)PacketType.ROOM_OPPO_SHOOTSTONE) return;

        var msg = ShootStonePacket.Deserialize(packet.Data);

        Debug.Log(JsonUtility.ToJson(msg));

        PlayRecord(new List<VelocityRecord>(msg.velocityRecords));
    }
}
