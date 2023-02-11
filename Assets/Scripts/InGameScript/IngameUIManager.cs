using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class IngameUIManager : SingletonBehavior<IngameUIManager>
{
    [SerializeField] private AskingPanel askingPanel;
    public AskingPanel AskingPanel => askingPanel;

    [SerializeField] private TextMeshProUGUI tempCurrentTurnText;
    public TextMeshProUGUI TempCurrentTurnText => tempCurrentTurnText;
}
