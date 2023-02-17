using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class IngameUIManager : SingletonBehavior<IngameUIManager>
{
    private List<RectTransform> currentActivatedUI = new();

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

    [SerializeField] private TextMeshProUGUI deckCountText;
    public TextMeshProUGUI DeckCountText => deckCountText;
    [SerializeField] private TextMeshProUGUI turnEndButtonText;
    public TextMeshProUGUI TurnEndButtonText => turnEndButtonText;

    [SerializeField] private RectTransform cancelPanel;
    public RectTransform CancelPanel => cancelPanel;

    [SerializeField] private InformationPanel informationPanel;
    public InformationPanel InformationPanel => informationPanel;

    [SerializeField] private CostPanel costPanel;
    public CostPanel CostPanel => costPanel;

    [SerializeField] private Transform handCardTransform;
    public Transform HandCardTransform => handCardTransform;

    [SerializeField] private RectTransform shootReadyEmphasizeUI;
    public RectTransform ShootReadyEmphasizeUI => shootReadyEmphasizeUI;

    [SerializeField] private NotificationPanel notificationPanel;
    public NotificationPanel NotificationPanel => notificationPanel;
    [SerializeField] private NotificationPanel honorSkipPanel;
    public NotificationPanel HonorSkipPanel => honorSkipPanel;

    [SerializeField] private RectTransform BlurImage;
    [SerializeField] private Button BlurImageButton;

    [SerializeField] private RectTransform settingPanel;
    public RectTransform SettingPanel => settingPanel;
    [SerializeField] private Button optionButton;

    [SerializeField] private RectTransform enemyInfoPanel;
    public RectTransform EnemyInfoPanel => enemyInfoPanel;
    [SerializeField] private Button enemyInfoButton;
    [SerializeField] private Button enemyInfoPanelButton;

    [SerializeField] private RectTransform resultPanel;
    public RectTransform ResultPanel => resultPanel;

    private void Start()
    {
        enemyInfoButton.onClick.AddListener(() => {
            ActivateUI(enemyInfoPanel);
            SetEnemyInfoPanel();
        });

        enemyInfoPanelButton.onClick.AddListener(() => {
            DeactivateUI(enemyInfoPanel);
        });

        optionButton.onClick.AddListener(() => {
            ActivateUI(settingPanel);
            ActivateUI(BlurImage);
        });

        BlurImageButton.onClick.AddListener(() => {
            DeactivateUI();

        });
    }

    public void ActivateUI(RectTransform rect)
    {
        currentActivatedUI.Add(rect);
        rect.gameObject.SetActive(true);
    }

    public void DeactivateUI()
    {
        foreach(var ui in currentActivatedUI)
        {
            ui.gameObject.SetActive(false);
        }
        currentActivatedUI.Clear();
    }

    public void DeactivateUI(RectTransform rect)
    {
        rect.gameObject.SetActive(false);
        currentActivatedUI.Remove(rect);
    }

    public bool isThereActivatedUI()
    {
        return currentActivatedUI.Count > 0 ? true : false;
    }

    public bool isThereActivatedUI(RectTransform rect)
    {
        return currentActivatedUI.Contains(rect);
    }

    public void SetResultPanel()
    {
        //TODO : Set result from gamemanager's data
    }

    private void SetEnemyInfoPanel()
    {
        //TODO : Set enemy info from gagmemanager's data
        EnemyInfoPanel.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Deck : " + " / Hand : " + GameManager.Inst.OppoPlayer.HandCount;
        //닉네임
        //승패전적
    }
}
