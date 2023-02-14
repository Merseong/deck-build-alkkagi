using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CostPanel : MonoBehaviour
{
    [SerializeField] private GameObject gaugePrefab;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Image costTextBackground;
    [SerializeField] private Transform gaugeParent;

    private int curMaxCost;

    private List<GameObject> gaugeList = new();

    public void SetCost(int cost)
    {
        costText.text = cost.ToString();

        if(cost == 0) costTextBackground.color = Color.gray;
        else costTextBackground.color = Color.green;

        //코스트의 최대치가 갱신되는 경우
        if(cost > curMaxCost)
        {
            if(cost > gaugeList.Count)
            {
                int temp = cost - gaugeList.Count;
                for(int i=0; i< temp; i++)
                {
                    gaugeList.Add(Instantiate(gaugePrefab, Vector3.zero, Quaternion.identity, gaugeParent));
                }
            }
            
            foreach(var go in gaugeList)
            {
                go.GetComponent<Image>().color = Color.green;
                go.SetActive(true);
            }
        }
        //현재 최대 코스트 내에서 코스트가 변동하는 경우
        else
        {
            foreach(var go in gaugeList)
            {
                go.GetComponent<Image>().color = Color.gray;
            }

            for(int i=0; i<cost; i++)
            {
                gaugeList[i].GetComponent<Image>().color = Color.green;
            }
        }
    }

    public void ResetCost(int cost)
    {
        curMaxCost = cost;

        if(cost > gaugeList.Count)
        {
            int temp = cost - gaugeList.Count;
            for(int i=0; i< temp; i++)
            {
                gaugeList.Add(Instantiate(gaugePrefab, Vector3.zero, Quaternion.identity, gaugeParent));
            }
        }

        foreach(var go in gaugeList)
        {
            go.SetActive(false);
        }

        for(int i=0; i<cost; i++)
        {
            gaugeList[i].GetComponent<Image>().color = Color.green;
            gaugeList[i].SetActive(true);
        }
    }
}
