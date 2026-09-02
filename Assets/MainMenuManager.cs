using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuManager : MonoBehaviour
{
    [Header("Stage Upgrades")]
    [SerializeField] private GunConfig[] _gunConfigs;
    [SerializeField] private ArenaEnvironmentConfig _arenaEnvironment;

    [Header("Camera Transition")]
    [SerializeField] private Vector3 _menuCameraPosition = new(0f, 9.5f, -7f);
    [SerializeField] private Vector3 _menuCameraEuler = new(52f, 0f, 0f);
    [SerializeField] private Vector3 _menuFocusOffset = new(0f, 1.15f, 0f);
    [SerializeField] private float _menuFieldOfView = 52f;
    [SerializeField] private Vector3 _gameCameraPosition = new(0f, 20f, -3.5f);
    [SerializeField] private Vector3 _gameCameraEuler = new(81f, 0f, 0f);
    [SerializeField] private Vector3 _gameFocusOffset = Vector3.zero;
    [SerializeField] private float _gameFieldOfView = 85f;
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
    [SerializeField] private Sprite _shopIcon;

    private Camera _camera;
    private CameraHitFeedback _cameraRig;
    private Transform _hero;
    private Vector3 _resolvedMenuFocusOffset;
    private CanvasGroup _menuGroup;
    private Button _startButton;
    private Canvas _gameplayHud;
    private OOP.GameStates.GameStateMachineRunner _gameStateMachine;
    private bool _starting;
    private TextMeshProUGUI _goldText;
    private TextMeshProUGUI _stageTitle;
    private UpgradeCardView _healthCard;
    private UpgradeCardView _incomeCard;
    private RectTransform _weaponWindow;
    private RectTransform _featureWindow;
    private RectTransform _shopWindow;
    private RectTransform _navigation;
    private readonly RectTransform[] _navigationTabs = new RectTransform[4];
    private readonly Vector3[] _navigationTabBaseScales = new Vector3[4];
    private TextMeshProUGUI _featureTitle;
    private TextMeshProUGUI _featureBody;
    private TextMeshProUGUI _weaponGoldText;
    private TextMeshProUGUI _shopGoldText;
    private TextMeshProUGUI _shopStatusText;
    private readonly List<WeaponCardView> _weaponCards = new();

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

    private sealed class WeaponCardView
    {
        public int Index;
        public GunConfig Gun;
        public Button ActionButton;
        public TextMeshProUGUI ActionLabel;
        public TextMeshProUGUI Price;
        public Image Background;
        public Outline Outline;
    }

    private static readonly Color Navy = Hex("17213F");
    private static readonly Color Blue = Hex("168EF5");
    private static readonly Color BlueDark = Hex("0754AD");
    private static readonly Color Purple = Hex("7A30D9");
    private static readonly Color Green = Hex("16E96C");
    private static readonly Color Yellow = Hex("FFD628");
    private const string DailyShopClaimKey = "Shop.DailyCache.UtcDay";

    private enum MenuScreen
    {
        Battle,
        Pet,
        Weapon,
        Shop
    }

    private void Awake()
    {
        MetaProgression.ConfigureWeapons(_gunConfigs);
        StageMapRuntimeLoader.ApplyMapForCurrentStage(gameObject.scene);
        Sprite kickerGold = Resources.Load<Sprite>("KickerHUD/gold");
        if (kickerGold != null)
            _goldIcon = kickerGold;
        ArenaEnvironmentBuilder.EnsureBuilt(_arenaEnvironment);
        SetupGameplayPresentation();
    }

    private void Start()
    {
        MetaProgression.BeginStageSession(StageMapProgression.CurrentStageId);
        SetupHeldWeaponPreview();
        AudioManager.Instance?.Play(SoundLabel.MainMenuMusic);
        AudioManager.Instance?.SetStageAmbience(StageMapProgression.CurrentStage);
        BuildMenu();
        RefreshStageTitle();
        if (PlayerPrefs.GetInt(GameOverMenu.RetryRunKey, 0) != 0)
        {
            PlayerPrefs.DeleteKey(GameOverMenu.RetryRunKey);
            StartGame();
        }
        MetaProgression.UpgradesChanged += RefreshUpgradeCards;
        MetaProgression.SelectedWeaponChanged += HandleSelectedWeaponChanged;
        if (GoldWallet.Instance != null)
            GoldWallet.Instance.OnBalanceChanged += HandleGoldChanged;
        RefreshUpgradeCards();
    }

    private void OnDisable()
    {
        MetaProgression.UpgradesChanged -= RefreshUpgradeCards;
        MetaProgression.SelectedWeaponChanged -= HandleSelectedWeaponChanged;
        if (GoldWallet.Instance != null)
            GoldWallet.Instance.OnBalanceChanged -= HandleGoldChanged;
        AudioManager.Instance?.Stop(SoundLabel.MainMenuMusic);
    }

    private void SetupGameplayPresentation()
    {
        _gameStateMachine = FindFirstObjectByType<OOP.GameStates.GameStateMachineRunner>();
        GameObject heroObject = GameObject.FindGameObjectWithTag("Player");
        _hero = heroObject != null ? heroObject.transform : null;
        _resolvedMenuFocusOffset = ResolveHeroVisualFocusOffset();
        PlayerInput.Instance?.InputActions.Player.Disable();
        _camera = Camera.main;
        if (_camera != null)
        {
            foreach (Behaviour behaviour in _camera.GetComponents<Behaviour>())
            {
                if (behaviour != null && behaviour.GetType().Name.Contains("CinemachineBrain"))
                {
                    behaviour.enabled = false;
                    break;
                }
            }
            Vector3 menuPosition = GetCameraPosition(_menuCameraPosition);
            Quaternion menuRotation = GetFocusRotation(menuPosition, _resolvedMenuFocusOffset, _menuCameraEuler);
            _camera.transform.SetPositionAndRotation(menuPosition, menuRotation);
            _camera.fieldOfView = _menuFieldOfView;
            _cameraRig = _camera.GetComponent<CameraHitFeedback>();
            if (_cameraRig == null)
                _cameraRig = _camera.gameObject.AddComponent<CameraHitFeedback>();
            _cameraRig.LockTo(_hero, _menuCameraPosition, menuRotation);
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
        MainMenuCanvasView prefab = Resources.Load<MainMenuCanvasView>("MainMenuCanvas");
        if (prefab != null)
        {
            MainMenuCanvasView view = Instantiate(prefab);
            view.name = "Battle Main Menu";
            BindAuthoredMenu(view);
            return;
        }

        BuildMenuInCode();
    }

    private void BuildMenuInCode()
    {
        GameObject canvasObject = new("Battle Main Menu", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        // Match Endless Kicker's portrait reference layout so its prefab
        // measurements can be transferred without per-device offsets.
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        _menuGroup = canvasObject.GetComponent<CanvasGroup>();

        RectTransform root = canvasObject.GetComponent<RectTransform>();
        SettingsMenu.EnsureExists(root);
        AddPanel(root, "TopBar", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -200f), new Vector2(0f, 400f), new Color(0f, 0f, 0f, 0.38f));
        _stageTitle = AddText(root, "STAGE 1", 48, FontStyles.Bold, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(550f, 90f), Color.white);

        Button settings = AddButton(root, string.Empty, new Vector2(0f, 1f), new Vector2(168f, -120f),
            new Vector2(96f, 96f), Color.clear, Navy, 42);
        settings.name = "Settings Button";
        settings.onClick.AddListener(OpenSettings);
        Sprite kickerSettingsIcon = Resources.Load<Sprite>("KickerHUD/setting_button");
        AddIcon(settings.transform as RectTransform, "Settings Icon",
            kickerSettingsIcon != null ? kickerSettingsIcon : _settingsIcon, Vector2.zero, new Vector2(78f, 78f));
        _goldText = AddResourcePill(root, (GoldWallet.Instance != null ? GoldWallet.Instance.Balance : 0).ToString(), -120f, Yellow, _goldIcon);

        // Share the bottom anchor/safe-area offset with the upgrade cards and
        // Navigation. Y=786 keeps a stable gap above the cards on every aspect.
        _startButton = AddButton(root, "TAP TO START", new Vector2(0.5f, 0f), new Vector2(0f, 786f), new Vector2(400f, 172f), Color.clear, Navy, 42);
        (_startButton.transform as RectTransform).localScale = Vector3.one * 1.1f;
        _startButton.onClick.AddListener(StartGame);
        Button left = AddButton(root, "‹", new Vector2(0.5f, 0f), new Vector2(-285f, 786f), new Vector2(72f, 96f), Purple, Color.white, 54);
        Button right = AddButton(root, "›", new Vector2(0.5f, 0f), new Vector2(285f, 786f), new Vector2(72f, 96f), Purple, Color.white, 54);
        left.name = "Previous Stage Button";
        right.name = "Next Stage Button";
        AddIcon(left.transform as RectTransform, "Left Arrow Icon", _leftArrowIcon, Vector2.zero, new Vector2(40f, 48f));
        AddIcon(right.transform as RectTransform, "Right Arrow Icon", _rightArrowIcon, Vector2.zero, new Vector2(40f, 48f));

        _healthCard = AddUpgradeCard(root, -165f, StageUpgradeType.Health, "MAX HP", Blue, _maxHealthIcon);
        _incomeCard = AddUpgradeCard(root, 165f, StageUpgradeType.Income, "INCOME", Blue, _incomeIcon);

        RectTransform nav = AddPanel(root, "Navigation", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 80f), new Vector2(0f, 160f), Navy);
        _navigation = nav;
        RectTransform battle = AddPanel(nav, "Battle Tab", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-270f, 5f), new Vector2(170f, 145f), Hex("536BE7"));
        AddNavOutline(battle, Hex("92A2FF"));
        Button battleTab = battle.gameObject.AddComponent<Button>();
        battleTab.targetGraphic = battle.GetComponent<Image>();
        battleTab.onClick.AddListener(OpenBattleScreen);
        AddIcon(battle, "Battle Icon", _battleIcon, new Vector2(0f, 25f), new Vector2(68f, 68f));
        AddText(battle, "BATTLE", 25, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(145f, 40f), Color.white);
        Button petTab = AddNavItem(nav, -90f, "PET", _petIcon);
        petTab.onClick.AddListener(OpenPetWindow);
        Button weaponTab = AddNavItem(nav, 90f, "WEAPON", _weaponIcon);
        weaponTab.onClick.AddListener(OpenWeaponWindow);
        Button shopTab = AddNavItem(nav, 270f, "SHOP", _shopIcon);
        shopTab.onClick.AddListener(OpenShopWindow);

        BuildWeaponWindow(root);
        BuildFeatureWindow(root);
        BuildShopWindow(root);
        CacheNavigationTabs(battle, petTab.transform as RectTransform, weaponTab.transform as RectTransform, shopTab.transform as RectTransform);
        ShowScreen(MenuScreen.Battle);
    }

    private void BindAuthoredMenu(MainMenuCanvasView view)
    {
        view.CaptureReferences();
        _menuGroup = view.GetComponent<CanvasGroup>();
        RectTransform root = view.transform as RectTransform;
        SettingsMenu.EnsureExists(root);

        _goldText = view.GoldText;
        foreach (TextMeshProUGUI candidate in view.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (!candidate.text.StartsWith("STAGE ", System.StringComparison.OrdinalIgnoreCase)) continue;
            _stageTitle = candidate;
            break;
        }
        Transform authoredGoldIcon = view.transform.Find("Gold Pill/Gold Icon");
        if (authoredGoldIcon == null)
        {
            foreach (Image candidate in view.GetComponentsInChildren<Image>(true))
                if (candidate.name == "Gold Icon") { authoredGoldIcon = candidate.transform; break; }
        }
        if (authoredGoldIcon != null && authoredGoldIcon.TryGetComponent(out Image goldImage))
        {
            goldImage.sprite = _goldIcon;
            goldImage.preserveAspect = true;
            goldImage.enabled = _goldIcon != null;
        }
        _startButton = view.StartButton;
        BindButton(view.SettingsButton, OpenSettings);
        BindButton(_startButton, StartGame);
        BindButton(view.PetButton, OpenPetWindow);
        BindButton(view.WeaponButton, OpenWeaponWindow);
        BindButton(view.BattleButton, OpenBattleScreen);
        BindButton(view.ShopButton, OpenShopWindow);
        _navigation = view.transform.Find("Navigation") as RectTransform;
        CacheNavigationTabs(view.BattleButton != null ? view.BattleButton.transform as RectTransform : null,
            view.PetButton != null ? view.PetButton.transform as RectTransform : null,
            view.WeaponButton != null ? view.WeaponButton.transform as RectTransform : null,
            view.ShopButton != null ? view.ShopButton.transform as RectTransform : null);

        _healthCard = BindUpgradeCard(view.HealthCard, StageUpgradeType.Health);
        _incomeCard = BindUpgradeCard(view.IncomeCard, StageUpgradeType.Income);

        BuildWeaponWindow(root);
        BuildFeatureWindow(root);
        BuildShopWindow(root);
        ShowScreen(MenuScreen.Battle);
    }

    private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    public static void StyleAuthoredNavigation(MainMenuCanvasView view, bool immediate = false, int selectedIndex = 0)
    {
        RectTransform nav = view.transform.Find("Navigation") as RectTransform;
        if (nav == null) return;
        nav.sizeDelta = new Vector2(nav.sizeDelta.x, 176f);
        nav.anchoredPosition = new Vector2(nav.anchoredPosition.x, 88f);

        Transform inventory = nav.Find("INVENTORY Tab");
        if (inventory != null)
        {
            inventory.gameObject.SetActive(false);
            if (immediate) UnityEngine.Object.DestroyImmediate(inventory.gameObject);
            else Destroy(inventory.gameObject);
        }

        RectTransform pet = view.PetButton != null ? view.PetButton.transform as RectTransform : null;
        RectTransform weapon = view.WeaponButton != null ? view.WeaponButton.transform as RectTransform : null;
        RectTransform battle = nav.Find("Battle Tab") as RectTransform;
        RectTransform shop = view.ShopButton != null ? view.ShopButton.transform as RectTransform : null;
        RectTransform[] tabs = { battle, pet, weapon, shop };
        float[] positions = { -270f, -90f, 90f, 270f };
        for (int i = 0; i < tabs.Length; i++)
        {
            RectTransform tab = tabs[i];
            if (tab == null) continue;
            tab.SetSiblingIndex(i);
            bool selected = i == selectedIndex;
            tab.anchoredPosition = new Vector2(positions[i], selected ? 5f : 0f);
            tab.sizeDelta = new Vector2(170f, selected ? 145f : 132f);
            Image image = tab.GetComponent<Image>();
            if (image != null) image.color = selected ? Hex("536BE7") : Hex("111B38");
            AddNavOutline(tab, selected ? Hex("92A2FF") : Hex("30436F"));

            Button button = tab.GetComponent<Button>();
            if (button != null)
            {
                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = Hex("344D85");
                colors.pressedColor = Hex("0B1229");
                colors.selectedColor = colors.highlightedColor;
                button.colors = colors;
            }

            Image[] images = tab.GetComponentsInChildren<Image>(true);
            foreach (Image child in images)
            {
                if (child.transform == tab || !child.name.Contains("Icon", StringComparison.OrdinalIgnoreCase)) continue;
                RectTransform icon = child.rectTransform;
                icon.anchoredPosition = new Vector2(0f, 19f);
                icon.sizeDelta = new Vector2(54f, 54f);
                child.preserveAspect = true;
            }
            TextMeshProUGUI label = tab.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.rectTransform.anchoredPosition = new Vector2(0f, -31f);
                label.rectTransform.sizeDelta = new Vector2(160f, 34f);
                label.fontSize = selected ? 22f : 19f;
                label.alignment = TextAlignmentOptions.Center;
            }
        }
    }

    private static void AddNavOutline(RectTransform tab, Color color)
    {
        Outline outline = tab.GetComponent<Outline>();
        if (outline == null) outline = tab.gameObject.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(2f, -2f);
    }

    private void RefreshStageTitle()
    {
        if (_stageTitle != null)
            _stageTitle.text = $"STAGE {StageMapProgression.CurrentStage}";
    }

    private UpgradeCardView BindUpgradeCard(RectTransform card, StageUpgradeType type)
    {
        if (card == null) return null;
        TextMeshProUGUI[] labels = card.GetComponentsInChildren<TextMeshProUGUI>(true);
        if (labels.Length < 5)
        {
            Debug.LogError($"Main Menu Canvas card '{card.name}' has an invalid label hierarchy.", card);
            return null;
        }

        Button button = card.GetComponent<Button>();
        Outline outline = card.GetComponent<Outline>();
        BindButton(button, () => PurchaseUpgrade(type));
        return new UpgradeCardView
        {
            Type = type,
            Root = card,
            Button = button,
            Level = labels[0],
            Progress = labels[1],
            Value = labels[3],
            Cost = labels[4],
            Outline = outline,
        };
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
        if (_cameraRig != null)
            _cameraRig.enabled = false;
        Vector3 startPosition = _camera != null ? _camera.transform.position : Vector3.zero;
        Quaternion startRotation = _camera != null ? _camera.transform.rotation : Quaternion.identity;
        Vector3 gamePosition = GetCameraPosition(_gameCameraPosition);
        Quaternion gameRotation = GetFocusRotation(gamePosition, _gameFocusOffset, _gameCameraEuler);
        float startFov = _camera != null ? _camera.fieldOfView : _menuFieldOfView;
        float elapsed = 0f;

        while (elapsed < _transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / _transitionDuration);
            _menuGroup.alpha = 1f - t;
            if (_camera != null)
            {
                _camera.transform.position = Vector3.Lerp(startPosition, gamePosition, t);
                _camera.transform.rotation = Quaternion.Slerp(startRotation, gameRotation, t);
                _camera.fieldOfView = Mathf.Lerp(startFov, _gameFieldOfView, t);
            }
            yield return null;
        }

        if (_camera != null)
        {
            _camera.transform.SetPositionAndRotation(gamePosition, gameRotation);
            _camera.fieldOfView = _gameFieldOfView;
            if (_cameraRig == null)
                _cameraRig = _camera.gameObject.AddComponent<CameraHitFeedback>();
            _cameraRig.LockTo(_hero, _gameCameraPosition, gameRotation);
        }

        if (_gameplayHud != null)
            _gameplayHud.enabled = true;
        MetaProgression.UpgradesChanged -= RefreshUpgradeCards;
        MetaProgression.SelectedWeaponChanged -= HandleSelectedWeaponChanged;
        if (GoldWallet.Instance != null)
            GoldWallet.Instance.OnBalanceChanged -= HandleGoldChanged;
        AudioManager.Instance?.Stop(SoundLabel.MainMenuMusic);
        AudioManager.Instance?.Play(SoundLabel.StageStartSound);
        _gameStateMachine?.BeginGameplay();
        Destroy(_menuGroup.gameObject);
    }

    private Vector3 GetCameraPosition(Vector3 offset)
    {
        return _hero != null ? _hero.position + offset : offset;
    }

    private Vector3 ResolveHeroVisualFocusOffset()
    {
        if (_hero == null) return _menuFocusOffset;

        SkinnedMeshRenderer[] renderers = _hero.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (renderers.Length == 0) return _menuFocusOffset;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return _hero.InverseTransformPoint(bounds.center);
    }

    private Quaternion GetFocusRotation(Vector3 cameraPosition, Vector3 focusOffset, Vector3 fallbackEuler)
    {
        if (_hero == null)
            return Quaternion.Euler(fallbackEuler);

        Vector3 direction = _hero.position + focusOffset - cameraPosition;
        return direction.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(direction, Vector3.up)
            : Quaternion.Euler(fallbackEuler);
    }

    private static void OpenSettings()
    {
        SettingsMenu settings = FindFirstObjectByType<SettingsMenu>(FindObjectsInactive.Include);
        settings?.Show();
    }

    public bool SelectWeapon(int index)
    {
        if (_gunConfigs == null || index < 0 || index >= _gunConfigs.Length)
            return false;
        return MetaProgression.BuyOrSelectWeapon(index, _gunConfigs[index]);
    }

    private void BuildWeaponWindow(RectTransform root)
    {
        _weaponWindow = AddPanel(root, "Weapon Screen", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0.015f, 0.02f, 0.06f, 1f));
        ConfigureScreenRoot(_weaponWindow);

        RectTransform panel = AddPanel(_weaponWindow, "Armory Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 25f), new Vector2(700f, 1110f), Navy);
        Outline panelOutline = panel.gameObject.AddComponent<Outline>();
        panelOutline.effectColor = Blue;
        panelOutline.effectDistance = new Vector2(4f, -4f);

        AddText(panel, "ARMORY", 45, FontStyles.Bold, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(390f, 70f), Color.white);
        AddText(panel, "BUY AND EQUIP WEAPONS", 20, FontStyles.Bold, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0f, -105f), new Vector2(420f, 38f), Hex("9DCFFF"));

        RectTransform wallet = AddPanel(panel, "Armory Gold", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(104f, -58f), new Vector2(165f, 52f), new Color(0.04f, 0.05f, 0.1f, 0.95f));
        AddIcon(wallet, "Gold Icon", _goldIcon, new Vector2(-55f, 0f), new Vector2(32f, 32f));
        _weaponGoldText = AddText(wallet, "0", 22, FontStyles.Bold, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(15f, 0f), new Vector2(110f, 42f), Yellow);

        Button close = AddButton(panel, "X", new Vector2(1f, 1f), new Vector2(-50f, -55f),
            new Vector2(66f, 66f), Hex("D9435F"), Color.white, 30);
        close.onClick.AddListener(OpenBattleScreen);

        RectTransform scrollRoot = AddPanel(panel, "Weapon Scroll", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -85f), new Vector2(638f, 850f), new Color(0.03f, 0.05f, 0.12f, 0.8f));
        ScrollRect scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 34f;

        RectTransform viewport = AddPanel(scrollRoot, "Viewport", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
        viewport.gameObject.AddComponent<RectMask2D>();
        RectTransform content = AddPanel(viewport, "Content", new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, Color.clear);
        content.pivot = new Vector2(0.5f, 1f);
        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 14, 14);
        layout.spacing = 14f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport;
        scroll.content = content;

        _weaponCards.Clear();
        if (_gunConfigs != null)
        {
            for (int i = 0; i < _gunConfigs.Length; i++)
                AddWeaponCard(content, i, _gunConfigs[i]);
        }

        _weaponWindow.gameObject.SetActive(false);
    }

    private void AddWeaponCard(RectTransform content, int index, GunConfig gun)
    {
        RectTransform card = AddPanel(content, $"Weapon {index}", Vector2.zero, Vector2.one, Vector2.zero,
            new Vector2(0f, 152f), Hex("123663"));
        LayoutElement element = card.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = 152f;
        Outline outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = BlueDark;
        outline.effectDistance = new Vector2(3f, -3f);

        Sprite icon = gun != null && gun.Icon != null ? gun.Icon : _weaponIcon;
        AddIcon(card, "Weapon Icon", icon, new Vector2(-245f, 0f), new Vector2(90f, 90f));
        string weaponName = gun != null && !string.IsNullOrWhiteSpace(gun.DisplayName)
            ? gun.DisplayName.ToUpperInvariant()
            : gun != null ? gun.Archetype.ToString().ToUpperInvariant() : $"WEAPON {index + 1}";
        AddText(card, weaponName, 25, FontStyles.Bold, TextAlignmentOptions.Left,
            new Vector2(0.5f, 1f), new Vector2(-75f, -32f), new Vector2(330f, 42f), Color.white);
        string stats = gun != null
            ? $"DMG {gun.BaseDamage}   RATE {gun.BaseShotsPerSecond:0.#}/S   RANGE {gun.BaseRange:0.#}"
            : "NO CONFIG";
        AddText(card, stats, 17, FontStyles.Bold, TextAlignmentOptions.Left,
            new Vector2(0.5f, 0.5f), new Vector2(-75f, -8f), new Vector2(330f, 34f), Hex("BCE4FF"));
        TextMeshProUGUI price = AddText(card, string.Empty, 18, FontStyles.Bold, TextAlignmentOptions.Left,
            new Vector2(0.5f, 0f), new Vector2(-75f, 27f), new Vector2(330f, 34f), Yellow);

        Button action = AddButton(card, "BUY", new Vector2(1f, 0.5f), new Vector2(-91f, 0f),
            new Vector2(150f, 64f), Green, Navy, 21);
        int capturedIndex = index;
        action.onClick.AddListener(() => PurchaseOrEquipWeapon(capturedIndex));
        _weaponCards.Add(new WeaponCardView
        {
            Index = index,
            Gun = gun,
            ActionButton = action,
            ActionLabel = action.GetComponentInChildren<TextMeshProUGUI>(),
            Price = price,
            Background = card.GetComponent<Image>(),
            Outline = outline
        });
    }

    private void OpenWeaponWindow()
    {
        if (_weaponWindow == null || _starting) return;
        ShowScreen(MenuScreen.Weapon);
        RefreshWeaponWindow();
    }

    private void CloseWeaponWindow()
    {
        OpenBattleScreen();
    }

    private void BuildFeatureWindow(RectTransform root)
    {
        _featureWindow = AddPanel(root, "Pet Screen", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0.015f, 0.02f, 0.06f, 1f));
        ConfigureScreenRoot(_featureWindow);
        RectTransform panel = AddPanel(_featureWindow, "Feature Panel", new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(650f, 620f), Navy);
        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = Blue;
        outline.effectDistance = new Vector2(4f, -4f);

        _featureTitle = AddText(panel, "FEATURE", 46, FontStyles.Bold, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(500f, 76f), Color.white);
        _featureBody = AddText(panel, string.Empty, 25, FontStyles.Bold, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(520f, 250f), Hex("BCE4FF"));
        Button close = AddButton(panel, "CLOSE", new Vector2(0.5f, 0f), new Vector2(0f, 85f),
            new Vector2(300f, 78f), Blue, Color.white, 27);
        close.onClick.AddListener(OpenBattleScreen);
        _featureWindow.gameObject.SetActive(false);
    }

    private void BuildShopWindow(RectTransform root)
    {
        RectTransform authoredShop = root.Find("Shop Screen") as RectTransform;
        if (authoredShop == null)
            authoredShop = root.Find("Shop Window") as RectTransform;
        if (authoredShop != null)
        {
            _shopWindow = authoredShop;
            BindAuthoredShopWindow();
            _shopWindow.gameObject.SetActive(false);
            return;
        }

        _shopWindow = AddPanel(root, "Shop Screen", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0.015f, 0.02f, 0.06f, 1f));
        ConfigureScreenRoot(_shopWindow);
        RectTransform panel = AddPanel(_shopWindow, "Supply Shop Panel", new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(720f, 1120f), Navy);
        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = Yellow;
        outline.effectDistance = new Vector2(4f, -4f);

        AddText(panel, "WASTELAND SUPPLY", 43, FontStyles.Bold, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(500f, 68f), Color.white);
        AddText(panel, "DAILY REWARD & GOLD PACKS", 19, FontStyles.Bold, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0f, -105f), new Vector2(460f, 36f), Hex("FFD86A"));

        RectTransform wallet = AddPanel(panel, "Shop Wallet", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(112f, -58f), new Vector2(190f, 54f), new Color(0.04f, 0.05f, 0.1f, 0.96f));
        AddIcon(wallet, "Gold Icon", _goldIcon, new Vector2(-65f, 0f), new Vector2(34f, 34f));
        _shopGoldText = AddText(wallet, "0", 22, FontStyles.Bold, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(19f, 0f), new Vector2(125f, 42f), Yellow);

        Button close = AddButton(panel, "X", new Vector2(1f, 1f), new Vector2(-50f, -55f),
            new Vector2(66f, 66f), Hex("D9435F"), Color.white, 30);
        close.onClick.AddListener(OpenBattleScreen);

        AddShopPack(panel, 285f, "DAILY CACHE", "500 GOLD", "FREE", Hex("174D70"), ClaimDailyCache, true);
        AddShopPack(panel, 85f, "SURVIVOR PACK", "5,000 GOLD", "$0.99", Hex("123663"),
            () => ShowPaidPackPlaceholder("Survivor Pack"));
        AddShopPack(panel, -115f, "WASTELAND PACK", "15,000 GOLD  •  BEST VALUE", "$2.99", Hex("3B286A"),
            () => ShowPaidPackPlaceholder("Wasteland Pack"));
        AddShopPack(panel, -315f, "WARLORD PACK", "50,000 GOLD", "$7.99", Hex("633143"),
            () => ShowPaidPackPlaceholder("Warlord Pack"));

        _shopStatusText = AddText(panel, string.Empty, 19, FontStyles.Bold, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0f), new Vector2(0f, 72f), new Vector2(600f, 58f), Hex("BCE4FF"));
        _shopStatusText.name = "Shop Status";
        _shopWindow.gameObject.SetActive(false);
    }

    private void BindAuthoredShopWindow()
    {
        RectTransform panel = _shopWindow.Find("Supply Shop Panel") as RectTransform;
        if (panel == null)
        {
            Debug.LogWarning("Authored Shop Window is missing 'Supply Shop Panel'.", _shopWindow);
            return;
        }

        Transform wallet = panel.Find("Shop Wallet");
        _shopGoldText = wallet != null ? wallet.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        _shopStatusText = FindAuthoredShopStatus(panel);

        BindButton(panel.Find("X")?.GetComponent<Button>(), OpenBattleScreen);
        BindShopPack(panel, "DAILY CACHE", "FREE", ClaimDailyCache);
        BindShopPack(panel, "SURVIVOR PACK", "$0.99", () => ShowPaidPackPlaceholder("Survivor Pack"));
        BindShopPack(panel, "WASTELAND PACK", "$2.99", () => ShowPaidPackPlaceholder("Wasteland Pack"));
        BindShopPack(panel, "WARLORD PACK", "$7.99", () => ShowPaidPackPlaceholder("Warlord Pack"));
    }

    private static TextMeshProUGUI FindAuthoredShopStatus(RectTransform panel)
    {
        Transform named = panel.Find("Shop Status");
        if (named != null && named.TryGetComponent(out TextMeshProUGUI status))
            return status;

        foreach (TextMeshProUGUI candidate in panel.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            RectTransform rect = candidate.rectTransform;
            if (rect.anchorMin.y <= 0.01f && rect.sizeDelta.x >= 500f && rect.anchoredPosition.y <= 120f)
                return candidate;
        }
        return null;
    }

    private static void BindShopPack(RectTransform panel, string cardName, string buttonName,
        UnityEngine.Events.UnityAction action)
    {
        Transform card = panel.Find(cardName);
        Button button = card != null ? card.Find(buttonName)?.GetComponent<Button>() : null;
        BindButton(button, action);
    }

    private void AddShopPack(RectTransform panel, float y, string title, string reward, string price,
        Color color, UnityEngine.Events.UnityAction action, bool daily = false)
    {
        RectTransform card = AddPanel(panel, title, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, y), new Vector2(620f, 166f), color);
        Outline outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = daily ? Green : Color.Lerp(color, Color.white, 0.35f);
        outline.effectDistance = new Vector2(3f, -3f);
        AddIcon(card, "Gold Icon", _goldIcon, new Vector2(-245f, 0f), new Vector2(82f, 82f));
        AddText(card, title, 26, FontStyles.Bold, TextAlignmentOptions.Left,
            new Vector2(0.5f, 0.5f), new Vector2(-75f, 29f), new Vector2(320f, 42f), Color.white);
        AddText(card, reward, 18, FontStyles.Bold, TextAlignmentOptions.Left,
            new Vector2(0.5f, 0.5f), new Vector2(-75f, -24f), new Vector2(330f, 38f), Hex("D8EDFF"));
        Button buy = AddButton(card, price, new Vector2(1f, 0.5f), new Vector2(-92f, 0f),
            new Vector2(160f, 72f), daily ? Green : Yellow, Navy, 22);
        buy.onClick.AddListener(action);
    }

    private void ClaimDailyCache()
    {
        int today = UtcDay();
        if (PlayerPrefs.GetInt(DailyShopClaimKey, -1) == today)
        {
            if (_shopStatusText != null) _shopStatusText.text = "DAILY CACHE ALREADY CLAIMED";
            return;
        }
        PlayerPrefs.SetInt(DailyShopClaimKey, today);
        PlayerPrefs.Save();
        GoldWallet.Instance?.Add(500);
        if (_shopStatusText != null) _shopStatusText.text = "+500 GOLD  •  COME BACK TOMORROW";
        RefreshShopWindow();
    }

    private void ShowPaidPackPlaceholder(string pack)
    {
        if (_shopStatusText != null)
            _shopStatusText.text = $"{pack.ToUpperInvariant()}  •  UNITY IAP NOT CONNECTED";
    }

    private static int UtcDay() => (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalDays;

    private void RefreshShopWindow()
    {
        if (_shopGoldText != null)
            _shopGoldText.text = (GoldWallet.Instance != null ? GoldWallet.Instance.Balance : 0).ToString("N0");
    }

    private void CloseShopWindow()
    {
        OpenBattleScreen();
    }

    private void OpenPetWindow()
    {
        ShowFeatureWindow("PET", "PET SYSTEM\nCOMING SOON\n\nThis feature is not available in Endless Zombie yet.");
    }

    private void OpenShopWindow()
    {
        if (_shopWindow == null || _starting) return;
        if (_shopStatusText != null)
            _shopStatusText.text = PlayerPrefs.GetInt(DailyShopClaimKey, -1) == UtcDay()
                ? "DAILY CACHE CLAIMED"
                : "FREE DAILY CACHE AVAILABLE";
        RefreshShopWindow();
        ShowScreen(MenuScreen.Shop);
    }

    private void ShowFeatureWindow(string title, string body)
    {
        if (_featureWindow == null || _starting) return;
        _featureTitle.text = title;
        _featureBody.text = body;
        ShowScreen(MenuScreen.Pet);
    }

    private void CloseFeatureWindow()
    {
        OpenBattleScreen();
    }

    private void OpenBattleScreen()
    {
        if (_starting) return;
        ShowScreen(MenuScreen.Battle);
    }

    private void ShowScreen(MenuScreen screen)
    {
        if (_featureWindow != null) _featureWindow.gameObject.SetActive(screen == MenuScreen.Pet);
        if (_weaponWindow != null) _weaponWindow.gameObject.SetActive(screen == MenuScreen.Weapon);
        if (_shopWindow != null) _shopWindow.gameObject.SetActive(screen == MenuScreen.Shop);

        UpdateNavigationSelection((int)screen);
        if (_navigation != null)
            _navigation.SetAsLastSibling();
    }

    private void CacheNavigationTabs(RectTransform battle, RectTransform pet, RectTransform weapon, RectTransform shop)
    {
        _navigationTabs[0] = battle;
        _navigationTabs[1] = pet;
        _navigationTabs[2] = weapon;
        _navigationTabs[3] = shop;
        for (int i = 0; i < _navigationTabs.Length; i++)
        {
            RectTransform tab = _navigationTabs[i];
            _navigationTabBaseScales[i] = tab != null ? tab.localScale : Vector3.one;
            EnsureNavigationSelectionMarker(tab);
        }
    }

    private void UpdateNavigationSelection(int selectedIndex)
    {
        for (int i = 0; i < _navigationTabs.Length; i++)
        {
            RectTransform tab = _navigationTabs[i];
            if (tab == null) continue;
            bool selected = i == selectedIndex;
            tab.localScale = _navigationTabBaseScales[i] * (selected ? 1.1f : 1f);
            Transform marker = tab.Find("Selection Marker");
            if (marker != null) marker.gameObject.SetActive(selected);
        }
    }

    private static void EnsureNavigationSelectionMarker(RectTransform tab)
    {
        if (tab == null || tab.Find("Selection Marker") != null) return;
        RectTransform marker = AddPanel(tab, "Selection Marker", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 5f), new Vector2(72f, 7f), Hex("92A2FF"));
        marker.SetAsLastSibling();
        Image image = marker.GetComponent<Image>();
        image.raycastTarget = false;
    }

    private static void ConfigureScreenRoot(RectTransform screen)
    {
        // Keep the persistent bottom navigation outside every content screen.
        screen.offsetMin = new Vector2(0f, 176f);
        screen.offsetMax = Vector2.zero;
    }

    private void PurchaseOrEquipWeapon(int index)
    {
        SelectWeapon(index);
        RefreshWeaponWindow();
    }

    private void HandleSelectedWeaponChanged(int _)
    {
        RefreshWeaponWindow();
    }

    private void RefreshWeaponWindow()
    {
        int balance = GoldWallet.Instance != null ? GoldWallet.Instance.Balance : 0;
        if (_weaponGoldText != null)
            _weaponGoldText.text = balance.ToString("N0");

        foreach (WeaponCardView card in _weaponCards)
        {
            bool selected = MetaProgression.SelectedWeapon == card.Index;
            bool unlocked = MetaProgression.IsWeaponUnlocked(card.Index);
            int cost = MetaProgression.WeaponCost(card.Index, card.Gun);
            bool affordable = balance >= cost;

            card.Price.text = unlocked ? "OWNED" : $"{cost:N0} GOLD";
            card.Price.color = unlocked || affordable ? Yellow : Hex("FF6B72");
            card.ActionLabel.text = selected ? "EQUIPPED" : unlocked ? "EQUIP" : "BUY";
            card.ActionButton.interactable = !selected && (unlocked || affordable) && !_starting;
            card.Background.color = selected ? Hex("174D70") : Hex("123663");
            card.Outline.effectColor = selected ? Green : BlueDark;
        }
    }

    private void SetupHeldWeaponPreview()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        HeldWeaponPresenter presenter = player.GetComponent<HeldWeaponPresenter>();
        if (presenter == null)
            presenter = player.AddComponent<HeldWeaponPresenter>();
        presenter.SetGunConfigs(_gunConfigs);
    }

    private UpgradeCardView AddUpgradeCard(RectTransform root, float x, StageUpgradeType type, string title, Color accent, Sprite iconSprite)
    {
        // Keep the cards in the same bottom coordinate space as Navigation.
        // Navigation is 176 high; y=432 leaves a 32 px gap below a 448 high card.
        // ResponsiveCanvasController then applies the same safe-area offset to both.
        RectTransform card = AddPanel(root, title, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(x, 432f), new Vector2(260f, 448f), BlueDark);
        Outline outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = accent;
        outline.effectDistance = new Vector2(4f, -4f);
        Button button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = card.GetComponent<Image>();
        TextMeshProUGUI level = AddText(card, string.Empty, 28, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(230f, 40f), Color.white);
        TextMeshProUGUI progress = AddText(card, string.Empty, 21, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(230f, 34f), Hex("BCE4FF"));
        AddText(card, title, 30, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0f, 86f), new Vector2(230f, 54f), Color.white);
        AddIcon(card, title + " Icon", iconSprite, new Vector2(0f, 12f), new Vector2(108f, 108f));
        TextMeshProUGUI value = AddText(card, string.Empty, 25, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0f, -65f), new Vector2(230f, 52f), Color.white);
        TextMeshProUGUI cost = AddText(card, string.Empty, 24, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(230f, 58f), Yellow);
        UpgradeCardView view = new() { Type = type, Root = card, Button = button, Level = level, Progress = progress, Value = value, Cost = cost, Outline = outline };
        button.onClick.AddListener(() => PurchaseUpgrade(type));
        return view;
    }

    private TextMeshProUGUI AddResourcePill(RectTransform root, string value, float y, Color iconColor, Sprite iconSprite)
    {
        RectTransform pill = AddPanel(root, value, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-168f, y), new Vector2(230f, 64f), new Color(0.04f, 0.05f, 0.1f, 0.9f));
        pill.name = "Gold Pill";
        AddIcon(pill, "Gold Icon", iconSprite, new Vector2(-78f, 0f), new Vector2(44f, 44f));
        return AddText(pill, value, 25, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(24f, 0f), new Vector2(165f, 52f), iconColor);
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
        RefreshWeaponWindow();
        RefreshShopWindow();
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

    private Button AddNavItem(RectTransform nav, float x, string label, Sprite iconSprite)
    {
        RectTransform tab = AddPanel(nav, label + " Tab", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(x, 0f), new Vector2(170f, 132f), Hex("111B38"));
        AddNavOutline(tab, Hex("30436F"));
        Button button = tab.gameObject.AddComponent<Button>();
        button.targetGraphic = tab.GetComponent<Image>();
        AddIcon(tab, label + " Icon", iconSprite, new Vector2(0f, 19f), new Vector2(54f, 54f));
        AddText(tab, label, 17, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0f, -31f), new Vector2(135f, 32f), Color.white);
        return button;
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
        KickerUITheme.Apply(text);
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
