using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class IngameUIManager : SingletonBehavior<IngameUIManager>
{
    [SerializeField] private AskingPanel askingPanel;
    public AskingPanel AskingPanel => askingPanel;

    [SerializeField] private TextMeshProUGUI tempCurrentTurnText;
    public TextMeshProUGUI TempCurrentTurnText => tempCurrentTurnText;

    [SerializeField] private UserAlertPanel userAlertPanel;
    public UserAlertPanel UserAlertPanel => userAlertPanel;

    [SerializeField] private Image shootTokenImage;
    public Image ShootTokenImage  => shootTokenImage;

    [SerializeField] private TextMeshProUGUI handCountText;
    public TextMeshProUGUI HandCountText => handCountText;

    [SerializeField] private RectTransform cancelPanel;
    public RectTransform CancelPanel => cancelPanel;

    [SerializeField] private InformationPanel informationPanel;
    public InformationPanel InformationPanel => informationPanel;

    [SerializeField] private CostPanel costPanel;
    public CostPanel CostPanel => costPanel;

}
