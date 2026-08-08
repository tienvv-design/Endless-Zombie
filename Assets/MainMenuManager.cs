using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuManager : MonoBehaviour
{
    [Header("Stage Upgrades")]
    [SerializeField] private string _stageId = "Stage1";

    [Header("Camera Transition")]
    [SerializeField] private Vector3 _menuCameraPosition = new(0f, 6.5f, -5.2f);
    [SerializeField] private Vector3 _menuCameraEuler = new(47f, 0f, 0f);
    [SerializeField] private float _menuFieldOfView = 42f;
    [SerializeField] private Vector3 _gameCameraPosition = new(0f, 15.5f, -2.56f);
    [SerializeField] private Vector3 _gameCameraEuler = new(81f, 0f, 0f);
    [SerializeField] private float _gameFieldOfView = 80f;
    [SerializeField, Min(0.1f)] private float _transitionDuration = 1.15f;

    [Header("UI Icon Sprites")]
    [SerializeField] private Sprite _settingsIcon;
    [SerializeField] private Sprite _goldIcon;
    [SerializeField] private Sprite _energyIcon;
    [SerializeField] private Sprite _leftArrowIcon;
    [SerializeField] private Sprite _rightArrowIcon;
    [SerializeField] private Sprite _maxHealthIcon;
    [SerializeField] private Sprite _incomeIcon;
    [SerializeField] private Sprite _petIcon;
    [SerializeField] private Sprite _weaponIcon;
    [SerializeField] private Sprite _battleIcon;
    [SerializeField] private Sprite _inventoryIcon;
    [SerializeField] private Sprite _shopIcon;

    private Camera _camera;
    private CanvasGroup _menuGroup;
    private Button _startButton;
    private Canvas _gameplayHud;
    private Behaviour _cameraDriver;
    private OOP.GameStates.GameStateMachineRunner _gameStateMachine;
    private bool _starting;
    private TextMeshProUGUI _goldText;
    private UpgradeCardView _healthCard;
    private UpgradeCardView _incomeCard;

    private sealed class UpgradeCardView
    {
        public StageUpgradeType Type;
        public RectTransform Root;
        public Button Button;
        public TextMeshProUGUI Level;
        public TextMeshProUGUI Progress;
        public TextMeshProUGUI Value;
        public TextMeshProUGUI Cost;
        public Outline Outline;
    }

    private static readonly Color Navy = Hex("17213F");
    private static readonly Color Blue = Hex("168EF5");
    private static readonly Color BlueDark = Hex("0754AD");
    private static readonly Color Purple = Hex("7A30D9");
    private static readonly Color Green = Hex("16E96C");
    private static readonly Color Yellow = Hex("FFD628");

    private void Awake()
    {
        SetupGameplayPresentation();
    }

    private void Start()
    {
        MetaProgression.BeginStageSession(_stageId);
        AudioManager.Instance?.Play(SoundLabel.MainMenuMusic);
        BuildMenu();
        MetaProgression.UpgradesChanged += RefreshUpgradeCards;
        if (GoldWallet.Instance != null)
            GoldWallet.Instance.OnBalanceChanged += HandleGoldChanged;
        RefreshUpgradeCards();
    }

    private void OnDisable()
    {
        MetaProgression.UpgradesChanged -= RefreshUpgradeCards;
        if (GoldWallet.Instance != null)
            GoldWallet.Instance.OnBalanceChanged -= HandleGoldChanged;
        AudioManager.Instance?.Stop(SoundLabel.MainMenuMusic);
    }

    private void SetupGameplayPresentation()
    {
        _gameStateMachine = FindFirstObjectByType<OOP.GameStates.GameStateMachineRunner>();
        PlayerInput.Instance?.InputActions.Player.Disable();
        _camera = Camera.main;
        if (_camera != null)
        {
            foreach (Behaviour behaviour in _camera.GetComponents<Behaviour>())
            {
                if (behaviour != null && behaviour.GetType().Name.Contains("CinemachineBrain"))
                {
                    _cameraDriver = behaviour;
                    _cameraDriver.enabled = false;
                    break;
                }
            }
            _camera.transform.SetPositionAndRotation(_menuCameraPosition, Quaternion.Euler(_menuCameraEuler));
            _camera.fieldOfView = _menuFieldOfView;
        }

        GameObject hudObject = GameObject.Find("HUDCanvas");
        if (hudObject != null && hudObject.TryGetComponent(out Canvas hud))
        {
            _gameplayHud = hud;
            _gameplayHud.enabled = false;
        }
    }

    private void BuildMenu()
    {
        GameObject canvasObject = new("Battle Main Menu", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(786f, 1402f);
        scaler.matchWidthOrHeight = 0.5f;
        _menuGroup = canvasObject.GetComponent<CanvasGroup>();

        RectTransform root = canvasObject.GetComponent<RectTransform>();
        AddPanel(root, "Top Fade", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -115f), new Vector2(0f, 230f), new Color(0f, 0f, 0f, 0.38f));
        AddText(root, "STAGE 1", 48, FontStyles.Bold, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0f, -136f), new Vector2(380f, 70f), Color.white);

        Button settings = AddButton(root, "⚙", new Vector2(0f, 1f), new Vector2(62f, -62f), new Vector2(72f, 72f), Hex("DCE9FF"), Navy, 37);
        settings.onClick.AddListener(OpenSettings);
        AddIcon(settings.transform as RectTransform, "Settings Icon", _settingsIcon, Vector2.zero, new Vector2(48f, 48f));
        _goldText = AddResourcePill(root, (GoldWallet.Instance != null ? GoldWallet.Instance.Balance : 0).ToString(), -48f, Yellow, _goldIcon);

        AddPanel(root, "Energy", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -218f), new Vector2(188f, 74f), Blue);
        AddText(root, "FULL\n12 / 5  ⚡", 25, FontStyles.Bold, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0f, -218f), new Vector2(180f, 70f), Color.white);
        AddIcon(root, "Energy Icon", _energyIcon, new Vector2(75f, -218f), new Vector2(46f, 58f), new Vector2(0.5f, 1f));

        _startButton = AddButton(root, "START  ·  1 ⚡", new Vector2(0.5f, 0f), new Vector2(0f, 470f), new Vector2(320f, 86f), Green, Navy, 34);
        _startButton.onClick.AddListener(StartGame);
        Button left = AddButton(root, "‹", new Vector2(0.5f, 0f), new Vector2(-205f, 470f), new Vector2(62f, 72f), Purple, Color.white, 50);
        Button right = AddButton(root, "›", new Vector2(0.5f, 0f), new Vector2(205f, 470f), new Vector2(62f, 72f), Purple, Color.white, 50);
        AddIcon(left.transform as RectTransform, "Left Arrow Icon", _leftArrowIcon, Vector2.zero, new Vector2(40f, 48f));
        AddIcon(right.transform as RectTransform, "Right Arrow Icon", _rightArrowIcon, Vector2.zero, new Vector2(40f, 48f));

        _healthCard = AddUpgradeCard(root, -105f, StageUpgradeType.Health, "MAX HP", Blue, _maxHealthIcon);
        _incomeCard = AddUpgradeCard(root, 105f, StageUpgradeType.Income, "INCOME", Blue, _incomeIcon);

        RectTransform nav = AddPanel(root, "Navigation", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 67f), new Vector2(0f, 134f), Navy);
        AddNavItem(nav, -300f, "PET", _petIcon);
        AddNavItem(nav, -150f, "WEAPON", _weaponIcon);
        RectTransform battle = AddPanel(nav, "Battle Tab", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(155f, 165f), Hex("536BE7"));
        AddIcon(battle, "Battle Icon", _battleIcon, new Vector2(0f, 25f), new Vector2(68f, 68f));
        AddText(battle, "BATTLE", 25, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(145f, 40f), Color.white);
        AddNavItem(nav, 150f, "INVENTORY", _inventoryIcon);
        AddNavItem(nav, 300f, "SHOP", _shopIcon);

    }

    private void StartGame()
    {
        if (!_starting)
            StartCoroutine(StartTransition());
    }

    private IEnumerator StartTransition()
    {
        _starting = true;
        _startButton.interactable = false;
        RefreshUpgradeCards();
        Vector3 startPosition = _camera != null ? _camera.transform.position : Vector3.zero;
        Quaternion startRotation = _camera != null ? _camera.transform.rotation : Quaternion.identity;
        float startFov = _camera != null ? _camera.fieldOfView : _menuFieldOfView;
        float elapsed = 0f;

        while (elapsed < _transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / _transitionDuration);
            _menuGroup.alpha = 1f - t;
            if (_camera != null)
            {
                _camera.transform.position = Vector3.Lerp(startPosition, _gameCameraPosition, t);
                _camera.transform.rotation = Quaternion.Slerp(startRotation, Quaternion.Euler(_gameCameraEuler), t);
                _camera.fieldOfView = Mathf.Lerp(startFov, _gameFieldOfView, t);
            }
            yield return null;
        }

        if (_cameraDriver != null)
            _cameraDriver.enabled = true;
        if (_gameplayHud != null)
            _gameplayHud.enabled = true;
        MetaProgression.UpgradesChanged -= RefreshUpgradeCards;
        if (GoldWallet.Instance != null)
            GoldWallet.Instance.OnBalanceChanged -= HandleGoldChanged;
        AudioManager.Instance?.Stop(SoundLabel.MainMenuMusic);
        _gameStateMachine?.BeginGameplay();
        Destroy(_menuGroup.gameObject);
    }

    private static void OpenSettings()
    {
        GameObject settings = GameObject.Find("SettingsMenu");
        if (settings != null)
            settings.SetActive(true);
    }

    private UpgradeCardView AddUpgradeCard(RectTransform root, float x, StageUpgradeType type, string title, Color accent, Sprite iconSprite)
    {
        RectTransform card = AddPanel(root, title, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(x, 245f), new Vector2(188f, 270f), BlueDark);
        Outline outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = accent;
        outline.effectDistance = new Vector2(4f, -4f);
        Button button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = card.GetComponent<Image>();
        TextMeshProUGUI level = AddText(card, string.Empty, 23, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0f, -25f), new Vector2(170f, 30f), Color.white);
        TextMeshProUGUI progress = AddText(card, string.Empty, 17, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0f, -51f), new Vector2(170f, 26f), Hex("BCE4FF"));
        AddText(card, title, 25, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0f, 55f), new Vector2(170f, 45f), Color.white);
        AddIcon(card, title + " Icon", iconSprite, new Vector2(0f, 5f), new Vector2(80f, 80f));
        TextMeshProUGUI value = AddText(card, string.Empty, 22, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0f, -28f), new Vector2(170f, 44f), Color.white);
        TextMeshProUGUI cost = AddText(card, string.Empty, 20, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(0f, 27f), new Vector2(170f, 46f), Yellow);
        UpgradeCardView view = new() { Type = type, Root = card, Button = button, Level = level, Progress = progress, Value = value, Cost = cost, Outline = outline };
        button.onClick.AddListener(() => PurchaseUpgrade(type));
        return view;
    }

    private TextMeshProUGUI AddResourcePill(RectTransform root, string value, float y, Color iconColor, Sprite iconSprite)
    {
        RectTransform pill = AddPanel(root, value, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-76f, y), new Vector2(145f, 38f), new Color(0.04f, 0.05f, 0.1f, 0.9f));
        AddIcon(pill, "Gold Icon", iconSprite, new Vector2(-48f, 0f), new Vector2(28f, 28f));
        return AddText(pill, value, 19, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(12f, 0f), new Vector2(105f, 34f), iconColor);
    }

    private void PurchaseUpgrade(StageUpgradeType type)
    {
        if (MetaProgression.TryPurchase(type))
        {
            UpgradeCardView view = type == StageUpgradeType.Health ? _healthCard : _incomeCard;
            if (view?.Root != null)
                StartCoroutine(PulseCard(view.Root));
        }
        RefreshUpgradeCards();
    }

    private static IEnumerator PulseCard(RectTransform card)
    {
        const float duration = 0.22f;
        float elapsed = 0f;
        while (elapsed < duration && card != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float pulse = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);
            card.localScale = Vector3.one * (1f + pulse * 0.08f);
            yield return null;
        }
        if (card != null)
            card.localScale = Vector3.one;
    }

    private void HandleGoldChanged(int balance)
    {
        if (_goldText != null)
            _goldText.text = balance.ToString("N0");
        RefreshUpgradeCards();
    }

    private void RefreshUpgradeCards()
    {
        RefreshUpgradeCard(_healthCard);
        RefreshUpgradeCard(_incomeCard);
        if (_goldText != null)
            _goldText.text = (GoldWallet.Instance != null ? GoldWallet.Instance.Balance : 0).ToString("N0");
    }

    private void RefreshUpgradeCard(UpgradeCardView view)
    {
        if (view == null) return;
        StageUpgradeSnapshot snapshot = MetaProgression.GetSnapshot(view.Type);
        int balance = GoldWallet.Instance != null ? GoldWallet.Instance.Balance : 0;
        bool affordable = balance >= snapshot.Cost;
        view.Level.text = $"Lv.{snapshot.Level}";
        view.Progress.text = snapshot.IsBreakthrough ? "9/9  BREAKTHROUGH" : $"{snapshot.Progress}/9";

        if (view.Type == StageUpgradeType.Health)
        {
            CharacterHealthManager health = FindFirstObjectByType<CharacterHealthManager>();
            float baseHealth = health != null ? health.BaseHealth : 0f;
            float current = baseHealth + snapshot.CurrentValue - MetaProgression.HealthMarker;
            float next = baseHealth + snapshot.NextValue - MetaProgression.HealthMarker;
            view.Value.text = $"{FormatNumber(current)} → {FormatNumber(next)}";
        }
        else
        {
            view.Value.text = $"x{FormatNumber(snapshot.CurrentValue)} → x{FormatNumber(snapshot.NextValue)}";
        }

        view.Cost.text = $"{snapshot.Cost:N0} GOLD";
        view.Cost.color = affordable ? Yellow : Hex("FF6B72");
        view.Button.interactable = affordable && !_starting;
        view.Outline.effectColor = snapshot.IsBreakthrough ? Yellow : Blue;
        view.Outline.effectDistance = snapshot.IsBreakthrough ? new Vector2(6f, -6f) : new Vector2(4f, -4f);
    }

    private static string FormatNumber(float value) => Mathf.Approximately(value, Mathf.Round(value))
        ? Mathf.RoundToInt(value).ToString("N0")
        : value.ToString("0.##");

    private void AddNavItem(RectTransform nav, float x, string label, Sprite iconSprite)
    {
        AddIcon(nav, label + " Icon", iconSprite, new Vector2(x, 19f), new Vector2(54f, 54f));
        AddText(nav, label, 17, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(x, -31f), new Vector2(135f, 32f), Color.white);
    }

    private static Image AddIcon(RectTransform parent, string name, Sprite sprite, Vector2 position, Vector2 size, Vector2? anchor = null)
    {
        Vector2 iconAnchor = anchor ?? new Vector2(0.5f, 0.5f);
        GameObject item = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = iconAnchor;
        rect.anchorMax = iconAnchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = item.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.enabled = sprite != null;
        return image;
    }

    private static RectTransform AddPanel(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color color)
    {
        GameObject item = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        item.GetComponent<Image>().color = color;
        return rect;
    }

    private static TextMeshProUGUI AddText(RectTransform parent, string value, float size, FontStyles style, TextAlignmentOptions alignment, Vector2 anchor, Vector2 position, Vector2 dimensions, Color color)
    {
        GameObject item = new("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        TextMeshProUGUI text = item.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(12f, size * 0.65f);
        text.fontSizeMax = size;
        text.raycastTarget = false;
        return text;
    }

    private static Button AddButton(RectTransform parent, string label, Vector2 anchor, Vector2 position, Vector2 size, Color background, Color foreground, float fontSize)
    {
        RectTransform rect = AddPanel(parent, label, anchor, anchor, position, size, background);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = Color.Lerp(background, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(background, Color.black, 0.18f);
        button.colors = colors;
        AddText(rect, label, fontSize, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), Vector2.zero, size - new Vector2(10f, 8f), foreground);
        return button;
    }

    private static Color Hex(string value)
    {
        ColorUtility.TryParseHtmlString("#" + value, out Color color);
        return color;
    }
}
