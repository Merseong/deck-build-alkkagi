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

    [SerializeField] private UserAlertPanel userAlertPanel;
    public UserAlertPanel UserAlertPanel => userAlertPanel;

    [SerializeField] private RectTransform cancelPanel;
    public RectTransform CancelPanel => cancelPanel;

    [SerializeField] private InformationPanel informationPanel;
    public InformationPanel InformationPanel => informationPanel;

}
