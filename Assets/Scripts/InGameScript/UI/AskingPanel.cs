using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Set 3종세트 다 불러야함
/// </summary>
public class AskingPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI askingTextMesh;
    [SerializeField] private TextMeshProUGUI trueTextMesh;
    [SerializeField] private TextMeshProUGUI falseTextMesh;

    private List<Action> trueButtonAction = new List<Action>();
    private List<Action> falseButtonAction = new List<Action>();

    private void Awake()
    {
        ResetAskingPanel();
    }

    private void ResetAskingPanel()
    {
        trueButtonAction.Clear();
        falseButtonAction.Clear();

        transform.GetChild(0).gameObject.SetActive(false);
    }

    public void InvokeTrueActions()
    {
        trueButtonAction.ForEach(action =>
        {
            action();
        });
        ResetAskingPanel();
    }

    public void InvokeFalseActions()
    {
        falseButtonAction.ForEach(action =>
        {
            action();
        });
        ResetAskingPanel();
    }

    public void SetAskingPanelString(string asking, string trueString = "True", string falseString = "False")
    {
        askingTextMesh.text = asking;
        trueTextMesh.text = trueString;
        falseTextMesh.text = falseString;
    }

    public void SetAskingPanelActions(Action trueAction, Action falseAction)
    {
        trueButtonAction.Insert(0, trueAction);
        falseButtonAction.Insert(0, falseAction);
    }

    public void SetAskingPanelActive()
    {
        transform.GetChild(0).gameObject.SetActive(true);
    }
}
