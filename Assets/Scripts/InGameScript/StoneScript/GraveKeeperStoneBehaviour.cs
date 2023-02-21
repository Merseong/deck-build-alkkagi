using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraveKeeperStoneBehaviour : StoneBehaviour
{
    public override void OnEnter(bool calledByPacket = false, string options = "")
    {
        if(calledByPacket)
        {
            if(!options.Equals(""))
            {
                var stringList = options.Split(' ');
                SpawnStone(stringList[0], stringList[1]);
            }
        }
        else
        {
            if(GameManager.Inst.localDeadStones.Count == 0) return;
            var temp = Random.Range(0, GameManager.Inst.localDeadStones.Count);
            var stone = GameManager.Inst.localDeadStones[temp];
            GameManager.Inst.localDeadStones.Remove(stone);
            options = SpawnStone(stone);
        }

        base.OnEnter(calledByPacket, options);
    }

    private string SpawnStone(int cardID)
    {
        List<Transform> spawnablePos = GameManager.Inst.GameBoard.GetPossiblePutTransform(GameManager.PlayerEnum.LOCAL, CardData);
        CardData cardData = Util.GetCardDataFromID(cardID, GameManager.Inst.CardDatas);
        if(spawnablePos.Count == 0 || cardData == null) 
        {
            Debug.Log("Unable to spawn Stone!");
            return "";    
        }
        Vector3 spawnPos = spawnablePos[Random.Range(0, spawnablePos.Count-1)].position;
        GameManager.Inst.LocalPlayer.SpawnStone(cardData, spawnPos);
        return Util.ConvertIDtoString(cardData.CardID) + " " + Vector3ToString(spawnPos);
    }

    private void SpawnStone(string cardID, string _spawnPos)
    {
        CardData cardData = Util.GetCardDataFromID(int.Parse(cardID), GameManager.Inst.CardDatas);
        Vector3 spawnPos = StringToVector3(_spawnPos);
        GameManager.Inst.OppoPlayer.SpawnStone(cardData, spawnPos);
    }

    protected Vector3 StringToVector3(string vec3)
    {
        var stringList = vec3.Split('|');
        return new Vector3(float.Parse(stringList[0]), float.Parse(stringList[1]), float.Parse(stringList[2]));
    }

    protected string Vector3ToString(Vector3 vec3)
    {
        return $"{vec3.x}|{vec3.y}|{vec3.z}";
    }
}