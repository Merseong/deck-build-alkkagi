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
    [SerializeField] private AnimationCurve emphasizeCurve;

    private int curMaxCost;
    private int curCostCnt;
    private List<GameObject> gaugeList = new();
    private int curEmphasized;
    private bool isEmphasized = false;
    
    public void CostEmphasize(int tot)
    {
        if(tot > curCostCnt)
        {
            costTextBackground.color = Color.red;
            return;
        }
    
        costTextBackground.color = Color.green;
        
        if(curEmphasized != tot)
        {
            SetCost(curCostCnt);
        }
        curEmphasized = tot;

        if(isEmphasized)
        {    
            return;
        }

        StartCoroutine(ECostEmphasize());
    }

    private IEnumerator ECostEmphasize()
    {
        isEmphasized = true;
        float curTime = 0;
        while(curEmphasized != 0)
        {
            for(int i=curCostCnt-1; i>curCostCnt - curEmphasized - 1; i--)
            {   
                gaugeList[i].GetComponent<Image>().color = Color.Lerp(Color.green, Color.gray, emphasizeCurve.Evaluate(curTime % 1.0f));
            }
            curTime += Time.deltaTime;
            yield return null;
        }

        SetCost(curCostCnt);
        isEmphasized = false;
    }

    public void SetCost(int cost)
    {
        costText.text = cost.ToString();

        if (cost == 0)
        {
            costTextBackground.color = Color.white;
            costTextBackground.sprite = IngameUIManager.Inst.UIAtlas.GetSprite("UI_Cost_1");
        }
        else
        {
            costTextBackground.color = Color.white;
            costTextBackground.sprite = IngameUIManager.Inst.UIAtlas.GetSprite("UI_Cost_2");
        }
        //코스트의 최대치가 갱신되는 경우
        if (cost > curMaxCost)
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
                go.GetComponent<Image>().color = Color.white;
                go.GetComponent<Image>().sprite = IngameUIManager.Inst.UIAtlas.GetSprite("UI_CostBar_2");
                go.SetActive(true);
            }
        }
        //현재 최대 코스트 내에서 코스트가 변동하는 경우
        else
        {
            foreach(var go in gaugeList)
            {
                go.GetComponent<Image>().color = Color.white;
                go.GetComponent<Image>().sprite = IngameUIManager.Inst.UIAtlas.GetSprite("UI_CostBar_1");
            }

            for(int i=0; i<cost; i++)
            {
                gaugeList[i].GetComponent<Image>().color = Color.white;
                gaugeList[i].GetComponent<Image>().sprite = IngameUIManager.Inst.UIAtlas.GetSprite("UI_CostBar_2");
            }
        }
        curCostCnt = cost;
    }

    public void ResetCost(int cost)
    {
        curMaxCost = cost;
        curCostCnt = cost;

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
            gaugeList[i].GetComponent<Image>().color = Color.white;
            gaugeList[i].GetComponent<Image>().sprite = IngameUIManager.Inst.UIAtlas.GetSprite("UI_CostBar_2"); ;
            gaugeList[i].SetActive(true);
        }
    }
}
