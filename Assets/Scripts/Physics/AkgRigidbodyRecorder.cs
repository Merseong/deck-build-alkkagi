using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor.PackageManager;
using System;

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

    public void AppendEventRecord(int stoneId, string eventString)
    {
        eventRecords.Add(new EventRecord
        {
            eventEnum = EventEnum.POWER,
            stoneId = stoneId,
            eventMessage = eventString,
            time = Time.time - startTime,
        });
    }

    public void SendEventOnly(EventRecord eventRecord)
    {
        eventRecord.time -= Time.time;
        SendRecord(new List<VelocityRecord>(), new List<EventRecord>
        {
            eventRecord
        }, false);
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
        bool isShoot = false;
        StoneBehaviour shootStone = null;
        while (vrIdx < vRecords.Length || erIdx < eRecords.Length)
        {
            yield return new WaitUntil(() => vrIdx >= vRecords.Length ||
                                            vRecords[vrIdx].time <= Time.time - startTime ||
                                            erIdx >= eRecords.Length ||
                                            eRecords[erIdx].time <= Time.time - startTime);

            while (erIdx < eRecords.Length && eRecords[erIdx].time <= Time.time - startTime)
            {
                try
                {
                    StoneBehaviour stone;
                    Vector3 point;
                    var eventRec = eRecords[erIdx];
                    switch (eventRec.eventEnum)
                    {
                        case EventEnum.SHOOT:
                            // stoneId => striking stone의 번호
                            isShoot = true;
                            shootStone = GameManager.Inst.FindStone(eventRec.stoneId);
                            string[] msgArr = eventRec.eventMessage.Split(' ');
                            bool useShootToken = bool.Parse(msgArr[0]);
                            bool isRotated = bool.Parse(msgArr[1]);
                            //if (useShootToken)
                            //{
                            //    if (!GameManager.Inst.OppoPlayer.ShootTokenAvailable)
                            //        Debug.LogError("[OPPO] Shoot token already spent!");
                            //    GameManager.Inst.OppoPlayer.ShootTokenAvailable = false;
                            //}
                            GameManager.Inst.OppoPlayer.StrikingStone = shootStone;
                            shootStone.Shoot(Vector3.zero, isRotated, false, msgArr[2] == "true");
                            shootStone.OnShootExit += GameManager.Inst.OppoPlayer.OnShootExit;
                            break;
                        case EventEnum.COLLIDE:
                            // eventMessage -> (colStoneId || STATIC) (COLLIDED)
                            if (eventRec.eventMessage.StartsWith("STA")) break; // -> STATICCOLLIDE에서 같이 처리
                            var collMsgArr = eventRec.eventMessage.Split(' ');
                            stone = GameManager.Inst.FindStone(eventRec.stoneId);
                            var targetStone = GameManager.Inst.FindStone(int.Parse(collMsgArr[0]));
                            bool collided = collMsgArr.Length != 1 && collMsgArr[1] == "COL";
                            point = Util.SlicedStringsToVector3(eventRec.xPosition, eventRec.zPosition);
                            if (collided)
                                AudioManager.Inst.HitSound(stone.GetComponent<AkgRigidbody>());
                            stone.OnCollide(targetStone.GetComponent<AkgRigidbody>(), point, collided, true);
                            break;
                        case EventEnum.STATICCOLLIDE:
                            stone = GameManager.Inst.FindStone(eventRec.stoneId);
                            Guard guard = GameManager.Inst.GameBoard.FindGuard(int.Parse(eventRec.eventMessage));
                            point = Util.SlicedStringsToVector3(eventRec.xPosition, eventRec.zPosition);
                            AudioManager.Inst.HitSound(guard.GetComponent<AkgRigidbody>());
                            stone.OnCollide(guard.GetComponent<AkgRigidbody>(), point, false, true);
                            guard.OnCollide(stone.GetComponent<AkgRigidbody>(), point, true, true);
                            break;
                        case EventEnum.POWER:
                            // 돌의 각자 특성상의 action
                            stone = GameManager.Inst.FindStone(eventRec.stoneId);
                            stone.ParseActionString(eventRec.eventMessage);
                            break;
                        case EventEnum.DROPOUT:
                            stone = GameManager.Inst.FindStone(eventRec.stoneId);
                            stone.transform.position = Util.SlicedStringsToVector3(eventRec.xPosition, eventRec.zPosition);
                            stone.OnExit(true, eventRec.eventMessage);
                            stone.isExitingByPlaying = true;
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception _)
                {
                    
                }
                finally
                {
                    erIdx++;
                }
            }
            while (vrIdx < vRecords.Length && vRecords[vrIdx].time <= Time.time - startTime)
            {
                try
                {
                    var vRec = vRecords[vrIdx];
                    var stone = GameManager.Inst.FindStone(vRec.stoneId);
                    stone.GetComponent<AkgRigidbody>().SetVelocity(
                        Util.SlicedStringsToVector3(vRec.xVelocity, vRec.zVelocity),
                        Util.SlicedStringsToVector3(vRec.xPosition, vRec.zPosition)
                    );
                    
                }
                catch (Exception _)
                { }
                finally { vrIdx++; }
            }
        }

        // check pRecords and current stones
        yield return new WaitUntil(() =>
        {
            return GameManager.Inst.AllStones.Count == 0 ||
                GameManager.Inst.AllStones.Values.All(x => !x.isMoving);
        });

        foreach (StoneBehaviour stone in GameManager.Inst.AllStones.Values)
        {
            stone.ChangeSpriteAndRot("Idle", !GameManager.Inst.isLocalGoFirst);
        }

        for (var prIdx = 0; prIdx < pRecords.Length; ++prIdx)
        {
            var stone = GameManager.Inst.FindStone(pRecords[prIdx].stoneId);
            if (stone == null) continue;
            stone.transform.position = Util.SlicedStringsToVector3(pRecords[prIdx].xPosition, pRecords[prIdx].zPosition);
        }

        isPlaying = false;
    }

    public void SendRecord(List<VelocityRecord> records, List<EventRecord> events, bool includePosition = true)
    {
        var positionRecords = new List<PositionRecord>();
        if (includePosition)
        {
            foreach (var stone in GameManager.Inst.AllStones)
            {
                positionRecords.Add(new PositionRecord
                {
                    stoneId = stone.Key,
                    xPosition = Util.FloatToSlicedString(stone.Value.transform.position.x),
                    zPosition = Util.FloatToSlicedString(stone.Value.transform.position.z),
                });
            }
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

        if (msg.senderID == NetworkManager.Inst.NetworkId) return;

        msg.velocityRecords = msg.velocityRecords[..msg.velocityCount];
        msg.positionRecords = msg.positionRecords[..msg.positionCount];
        msg.eventRecords = msg.eventRecords[..msg.eventCount];

        Debug.Log(JsonUtility.ToJson(msg));

        GameManager.Inst.StartCoroutine(EPlayRecord(msg));
    }
}
