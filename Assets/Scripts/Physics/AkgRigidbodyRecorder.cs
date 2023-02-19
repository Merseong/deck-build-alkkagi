using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AkgRigidbodyRecorder
{
    private readonly List<VelocityRecord> records = new();
    private readonly List<EventRecord> eventRecords = new();
    private float startTime;

    private bool isRecording;
    public bool IsRecording => isRecording;

    private bool isPlaying;
    public bool IsPlaying => isPlaying;

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

    public void SendEventOnly(EventRecord eventRecord)
    {
        StartRecord(Time.time);
        AppendEventRecord(eventRecord);
        EndRecord(out _, out var eventRecs);
        SendRecord(new List<VelocityRecord>(), eventRecs);
    }

    public IEnumerator EPlayRecord(StoneActionPacket records)
    {
        isPlaying = true;
        var vRecords = records.velocityRecords;
        var pRecords = records.positionRecords;
        var eRecords = records.eventRecords;

        (GameManager.Inst.OppoPlayer as OppoPlayerBehaviour).SetCostHandValue(records.finalCost, records.finalHand);

        yield return null;
        // play vRecords, eRecords
        var startTime = Time.time;
        var vrIdx = 0;
        var erIdx = 0;
        while (vrIdx < vRecords.Length || erIdx < eRecords.Length)
        {
            yield return new WaitUntil(() => vrIdx >= vRecords.Length ||
                                            vRecords[vrIdx].time <= Time.time - startTime ||
                                            erIdx >= eRecords.Length ||
                                            eRecords[erIdx].time <= Time.time - startTime);
            while (vrIdx < vRecords.Length && vRecords[vrIdx].time <= Time.time - startTime)
            {
                var stone = GameManager.Inst.FindStone(vRecords[vrIdx].stoneId);
                stone.GetComponent<AkgRigidbody>().SetVelocity(new Vector3(vRecords[vrIdx].xVelocity, 0, vRecords[vrIdx].zVelocity));
                vrIdx++;
            }
            while (erIdx < eRecords.Length && eRecords[erIdx].time <= Time.time - startTime)
            {
                StoneBehaviour stone;
                var eventRec = eRecords[erIdx];
                switch (eventRec.eventEnum)
                {
                    case EventEnum.SPENDTOKEN:
                        // stoneId => 토큰을 사용한 stone의 번호
                        if (!GameManager.Inst.OppoPlayer.ShootTokenAvailable)
                        {
                            Debug.LogError("[OPPO] Shoot token already spent!");
                        }
                        GameManager.Inst.OppoPlayer.ShootTokenAvailable = false;
                        break;
                    case EventEnum.GUARDCOLLIDE:
                        // stoneId => 충돌한 guard의 번호
                        GameManager.Inst.GameBoard.RemoveGuard(eventRec.stoneId);
                        break;
                    case EventEnum.DROPOUT:
                        stone = GameManager.Inst.FindStone(eventRec.stoneId);
                        stone.transform.position = new Vector3(eventRec.xPosition, 0, eventRec.zPosition);
                        stone.isExitingByPlaying = true;
                        break;
                    case EventEnum.POWER:
                        // 돌의 각자 특성상의 action
                        stone = GameManager.Inst.FindStone(eventRec.stoneId);
                        stone.ParseActionString(eventRec.eventMessage);
                        break;
                    default:
                        break;
                }
                erIdx++;
            }
        }

        // check pRecords and current stones
        yield return new WaitUntil(() =>
        {
            return GameManager.Inst.AllStones.Count == 0 ||
                GameManager.Inst.AllStones.Values.All(x => !x.isMoving);
        });

        for (var prIdx = 0; prIdx < pRecords.Length; ++prIdx)
        {
            var stone = GameManager.Inst.FindStone(pRecords[prIdx].stoneId);
            stone.transform.position = new Vector3(pRecords[prIdx].xPosition, 0, pRecords[prIdx].zPosition);
        }

        isPlaying = false;
    }

    public void SendRecord(List<VelocityRecord> records, List<EventRecord> events)
    {
        var positionRecords = new List<PositionRecord>();
        foreach (var stone in GameManager.Inst.AllStones)
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
        _velocityRecords.Sort((n1, n2) => n1.time.CompareTo(n2.time));
        _eventRecords.Sort((n1, n2) => n1.time.CompareTo(n2.time));

        var data = new StoneActionPacket
        {
            senderID = NetworkManager.Inst.NetworkId,

            velocityRecords = _velocityRecords.ToArray(),
            positionRecords = _positionRecords.ToArray(),
            eventRecords = _eventRecords.ToArray(),

            velocityCount = (short)_velocityRecords.Count,
            positionCount = (short)_positionRecords.Count,
            eventCount = (short)_eventRecords.Count,

            finalCost = (short)GameManager.Inst.LocalPlayer.Cost,
            finalHand = (short)GameManager.Inst.LocalPlayer.HandCount,            
        };

        NetworkManager.Inst.SendData(data, PacketType.ROOM_OPPO_STONEACTION);

        Debug.Log(JsonUtility.ToJson(data));
    }

    private void ShootRecordReceiveNetworkAction(Packet packet)
    {
        if (packet.Type != (short)PacketType.ROOM_OPPO_STONEACTION) return;

        var msg = StoneActionPacket.Deserialize(packet.Data);

        msg.velocityRecords = msg.velocityRecords[..msg.velocityCount];
        msg.positionRecords = msg.positionRecords[..msg.positionCount];
        msg.eventRecords = msg.eventRecords[..msg.eventCount];

        Debug.Log(JsonUtility.ToJson(msg));

        GameManager.Inst.StartCoroutine(EPlayRecord(msg));
    }
}
