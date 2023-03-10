using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.U2D;

public class IngameUIManager : SingletonBehavior<IngameUIManager>
{
    private List<RectTransform> currentActivatedUI = new();

    public SpriteAtlas UIAtlas;

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

    [SerializeField] private TextMeshProUGUI deckCountText1;
    public TextMeshProUGUI DeckCountText1 => deckCountText1;
    [SerializeField] private TextMeshProUGUI deckCountText2;
    public TextMeshProUGUI DeckCountText2 => deckCountText2;
    [Header("Images")]
    public Sprite GrayButton;
    public Sprite YelloButton;
    public Sprite OrangeButton;
    public Sprite HonorSkip;
    [Header("Turn end")]
    [SerializeField] private Image turnEndButtonImage;
    public Image TurnEndButtonImage => turnEndButtonImage;
    [SerializeField] private Image turnEndButtonOverlayImage;
    public Image TurnEndButtonOverlayImage => turnEndButtonOverlayImage;
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

    [Header("Enemy")]
    [SerializeField] private TextMeshProUGUI enemyCostText;
    [SerializeField] private Image enemyTokenImage;
    [SerializeField] private TextMeshProUGUI enemyNickname;

    [SerializeField] private RectTransform enemyInfoPanel;
    public RectTransform EnemyInfoPanel => enemyInfoPanel;
    [SerializeField] private Button enemyInfoButton;
    [SerializeField] private Button enemyInfoPanelButton;
    [SerializeField] private TextMeshProUGUI enemyInfoDeckHandText;

    [Header("Result")]
    [SerializeField] private RectTransform resultPanel;
    public RectTransform ResultPanel => resultPanel;
    [SerializeField] private Image honorMarkImage;
    public Image HonorMarkImage => honorMarkImage;

    private void Start()
    {
        enemyInfoButton.onClick.AddListener(() => {
            ActivateUI(enemyInfoPanel, true);
            SetEnemyData(GameManager.Inst.OppoPlayer as OppoPlayerBehaviour);
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

    public void ActivateUI(RectTransform rect, bool useToggle = false)
    {
        if (useToggle)
        {
            if (rect.gameObject.activeSelf)
            {
                DeactivateUI(rect);
            }
            else
            {
                ActivateUI(rect);
            }
        }
        else
        {
            currentActivatedUI.Add(rect);
            rect.gameObject.SetActive(true);
        }
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

    public void SetResultPanel(bool isLocalWin)
    {
        //TODO : Set result from gamemanager's data
        resultPanel.GetComponent<ResultPanel>().SetData(isLocalWin);
    }

    /// <summary>
    /// ????????? ???????????? ?????? ?????????
    /// </summary>
    /// <param name="enemyPlayer"></param>
    public void SetEnemyInfo(string nickname, uint win, uint lose)
    {
        //?????????
        enemyNickname.text = nickname;
        //????????????
        enemyInfoPanel.GetChild(1).GetComponent<TextMeshProUGUI>().text = $"{win} Win | {lose} Lose";
    }

    /// <summary>
    /// ???????????? ????????? ????????? ???????????? (??????, ???, ????????? ???)
    /// </summary>
    /// <param name="enemyPlayer"></param>
    public void SetEnemyData(OppoPlayerBehaviour enemyPlayer)
    {
        //TODO : Set enemy info from gagmemanager's data
        enemyInfoDeckHandText.text = $"Deck: {enemyPlayer.DeckCount} / Hand: {enemyPlayer.HandCount}";
        enemyCostText.text = enemyPlayer.Cost.ToString();
        string str = enemyPlayer.ShootTokenAvailable ? "UI_Token" : "UI_Token_0";
        enemyTokenImage.sprite = UIAtlas.GetSprite(str);
    }
}
