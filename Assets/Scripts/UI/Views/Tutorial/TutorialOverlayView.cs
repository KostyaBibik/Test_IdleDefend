using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Db;
using Enums;
using Game.Localization;
using Signals;
using TMPro;
using Tutorial;
using UI.Views.Buffs;
using UI.Views.Game;
using UI.Views.Upgradable;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Helpers;

namespace UI.Views.Tutorial
{
    /// <summary>
    /// Screen-space tutorial presentation. The dim layer is split around one or more holes,
    /// so only the highlighted controls remain raycastable.
    /// </summary>
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
    public sealed class TutorialOverlayView : MonoBehaviour
    {
        private const float TargetPadding = 14f;
        private const float LegendaryPopupSettleSeconds = 0.9f;
        private static readonly Color DimColor = new(0.015f, 0.025f, 0.055f, 0.82f);
        private static readonly Color AccentColor = new(1f, 0.72f, 0.12f, 1f);

        [SerializeField] private TMP_FontAsset tutorialFont;
        [SerializeField] private Material tutorialFontMaterial;

        private readonly List<Image> _blockers = new();
        private readonly List<RectTransform> _focusFrames = new();

        private RectTransform _visualRoot;
        private RectTransform _blockersRoot;
        private RectTransform _framesRoot;
        private RectTransform _tooltip;
        private TMP_Text _title;
        private TMP_Text _instruction;
        private Button _continueButton;

        private SignalBus _signalBus;
        private TutorialRuntimeState _runtime;
        private TutorialScenarioConfig _config;
        private TutorialDirector _director;
        private UpgradeViewsHandler _upgradeViews;
        private TowerExperienceBarView _experienceBar;
        private TowerLevelUpPopupView _levelUpPopup;
        private Camera _worldCamera;
        private UltimateButtonView _ultimateButton;
        private SceneHandler _sceneHandler;
        private Coroutine _deferredRoutine;
        private bool _isSubscribed;
        private RectTransform _trackedTarget;
        private float _trackedTargetPadding;

        private void Awake()
        {
            EnsureVisualTree();
            Hide();

            if (_runtime != null)
                ShowPhase(_runtime.Phase);
        }

        [Inject]
        public void Construct(
            SignalBus signalBus,
            TutorialRuntimeState runtime,
            TutorialScenarioConfig config,
            TutorialDirector director,
            UpgradeViewsHandler upgradeViews,
            TowerExperienceBarView experienceBar,
            TowerLevelUpPopupView levelUpPopup,
            Camera worldCamera,
            UltimateButtonView ultimateButton,
            SceneHandler sceneHandler)
        {
            _signalBus = signalBus;
            _runtime = runtime;
            _config = config;
            _director = director;
            _upgradeViews = upgradeViews;
            _experienceBar = experienceBar;
            _levelUpPopup = levelUpPopup;
            _worldCamera = worldCamera;
            _ultimateButton = ultimateButton;
            _sceneHandler = sceneHandler;

            _signalBus.Subscribe<TutorialPhaseChangedSignal>(OnPhaseChanged);
            _isSubscribed = true;

            if (_visualRoot != null)
                ShowPhase(_runtime.Phase);
        }

        private void OnPhaseChanged(TutorialPhaseChangedSignal signal)
        {
            ShowPhase(signal.current);
        }

        private void LateUpdate()
        {
            RefreshTrackedTargetLayout();
        }

        private void ShowPhase(ETutorialPhase phase)
        {
            StopDeferredRoutine();

            switch (phase)
            {
                case ETutorialPhase.DamageUpgrade:
                    _deferredRoutine = StartCoroutine(ShowDamageNextFrame());
                    break;
                case ETutorialPhase.ExperienceExplanation:
                    _deferredRoutine = StartCoroutine(ShowExperienceNextFrame());
                    break;
                case ETutorialPhase.BuffSelection:
                    _deferredRoutine = StartCoroutine(ShowLegendaryNextFrame());
                    break;
                case ETutorialPhase.UltimateShowcase:
                    ShowForTarget(
                        _ultimateButton.transform as RectTransform,
                        GameLocalization.Text(LocalizationKey.tutorial_ultimate_title, "Ultimate"),
                        GameLocalization.Text(LocalizationKey.tutorial_ultimate_body, "Use the ultimate to crush the whole wave."));
                    break;
                case ETutorialPhase.SideTowerSlot:
                    _deferredRoutine = StartCoroutine(ShowSideTowerSlotNextFrame());
                    break;
                case ETutorialPhase.SideTowerChoice:
                    _deferredRoutine = StartCoroutine(ShowSideTowerChoiceNextFrame());
                    break;
                case ETutorialPhase.IntroCombat:
                case ETutorialPhase.FirstEnemy:
                case ETutorialPhase.ExperienceTravel:
                case ETutorialPhase.MultishotShowcase:
                case ETutorialPhase.UltimateResolution:
                    BlockInputWithoutDimming();
                    break;
                default:
                    Hide();
                    break;
            }
        }

        private IEnumerator ShowDamageNextFrame()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();

            var upgradeView = _upgradeViews.GetViewByType(EUpgradeType.AttackDamage);
            ShowForTarget(
                upgradeView != null ? upgradeView.UpgradeBtn.transform as RectTransform : null,
                GameLocalization.Text(LocalizationKey.tutorial_damage_title, "Upgrade damage"),
                GameLocalization.Text(LocalizationKey.tutorial_damage_body, "Spend coins to make every shot stronger."));
            _deferredRoutine = null;
        }

        private IEnumerator ShowExperienceNextFrame()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            _trackedTarget = null;

            var holes = new List<Rect>
            {
                GetScreenRect(_experienceBar.transform as RectTransform, 18f)
            };

            var deathScreenPoint = RectTransformUtility.WorldToScreenPoint(_worldCamera, _director.FirstEnemyDeathPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_visualRoot, deathScreenPoint, null, out var localPoint);
            holes.Add(new Rect(localPoint - new Vector2(105f, 105f), new Vector2(210f, 210f)));

            Show(
                holes,
                GameLocalization.Text(LocalizationKey.tutorial_experience_title, "Experience"),
                GameLocalization.Text(LocalizationKey.tutorial_experience_body, "Defeated enemies drop experience for the tower."),
                allowContinue: true);
            _deferredRoutine = null;
        }

        private IEnumerator ShowLegendaryNextFrame()
        {
            var elapsed = 0f;
            while (elapsed < LegendaryPopupSettleSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Canvas.ForceUpdateCanvases();

            var card = _levelUpPopup.GetCardFor(_config.HighlightedLegendaryBuff);
            if (card == null)
            {
                Hide();
                _deferredRoutine = null;
                yield break;
            }

            ShowForTarget(
                card.SelectButton.transform as RectTransform,
                GameLocalization.Text(LocalizationKey.tutorial_legendary_title, "Legendary upgrade"),
                GameLocalization.Text(LocalizationKey.tutorial_legendary_body, "Multishot repeats every volley. Take the gift!"),
                10f,
                trackTarget: false);
            _deferredRoutine = null;
        }

        private IEnumerator ShowSideTowerSlotNextFrame()
        {
            yield return null;
            var marker = _director.TutorialSideTowerMarker;
            ShowForTarget(
                marker != null ? marker.Button.transform as RectTransform : null,
                GameLocalization.Text(LocalizationKey.tutorial_side_tower_title, "Support tower"),
                GameLocalization.Text(LocalizationKey.tutorial_side_tower_body, "Build a support tower before the next attack."));
            _deferredRoutine = null;
        }

        private IEnumerator ShowSideTowerChoiceNextFrame()
        {
            yield return null;
            var option = _sceneHandler.SideTowerPickerView.GetOptionFor(_config.TutorialSideTower);
            ShowForTarget(
                option != null ? option.Button.transform as RectTransform : null,
                GameLocalization.Text(LocalizationKey.tutorial_side_tower_choice_title, "Projectile tower"),
                GameLocalization.Text(LocalizationKey.tutorial_side_tower_choice_body, "Choose a reliable support tower."));
            _deferredRoutine = null;
        }

        private void ShowForTarget(
            RectTransform target,
            string title,
            string instruction,
            float padding = TargetPadding,
            bool trackTarget = true)
        {
            if (target == null)
            {
                Hide();
                return;
            }

            _trackedTarget = trackTarget ? target : null;
            _trackedTargetPadding = trackTarget ? padding : 0f;
            Canvas.ForceUpdateCanvases();
            Show(new[] { GetScreenRect(target, padding) }, title, instruction, allowContinue: false);
        }

        private void RefreshTrackedTargetLayout()
        {
            if (_trackedTarget == null || _visualRoot == null || !_visualRoot.gameObject.activeInHierarchy)
                return;

            var holes = new[] { GetScreenRect(_trackedTarget, _trackedTargetPadding) };
            LayoutBlockers(holes);
            LayoutFrames(holes);
            PositionTooltip(holes);
        }

        private void Show(IReadOnlyList<Rect> holes, string title, string instruction, bool allowContinue)
        {
            ApplyTutorialFont();
            _visualRoot.gameObject.SetActive(true);
            _tooltip.gameObject.SetActive(true);
            _title.text = title;
            _instruction.text = allowContinue
                ? instruction + "\n\n" + GameLocalization.Text(LocalizationKey.tutorial_tap_continue, "Tap to continue")
                : instruction;

            LayoutBlockers(holes);
            LayoutFrames(holes);
            PositionTooltip(holes);
            _continueButton.gameObject.SetActive(allowContinue);
        }

        private void BlockInputWithoutDimming()
        {
            _trackedTarget = null;
            _visualRoot.gameObject.SetActive(true);
            _tooltip.gameObject.SetActive(false);
            foreach (var blocker in _blockers)
                blocker.gameObject.SetActive(false);
            foreach (var frame in _focusFrames)
                frame.gameObject.SetActive(false);
            _continueButton.gameObject.SetActive(true);
        }

        private void Hide()
        {
            _trackedTarget = null;
            if (_visualRoot != null)
                _visualRoot.gameObject.SetActive(false);
        }

        private void OnContinueClicked()
        {
            _director?.ContinueExperienceExplanation();
        }

        private Rect GetScreenRect(RectTransform target, float padding)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);

            var targetCanvas = target.GetComponentInParent<Canvas>();
            var eventCamera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? targetCanvas.worldCamera
                : null;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _visualRoot, RectTransformUtility.WorldToScreenPoint(eventCamera, corners[0]), null, out var min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _visualRoot, RectTransformUtility.WorldToScreenPoint(eventCamera, corners[2]), null, out var max);

            return Rect.MinMaxRect(min.x - padding, min.y - padding, max.x + padding, max.y + padding);
        }

        private void LayoutBlockers(IReadOnlyList<Rect> rawHoles)
        {
            var bounds = _visualRoot.rect;
            var holes = rawHoles.Select(hole => Intersect(hole, bounds))
                .Where(hole => hole.width > 0f && hole.height > 0f)
                .ToList();

            var xCuts = new List<float> { bounds.xMin, bounds.xMax };
            foreach (var hole in holes)
            {
                xCuts.Add(hole.xMin);
                xCuts.Add(hole.xMax);
            }

            xCuts.Sort();
            var rects = new List<Rect>();
            for (var xIndex = 0; xIndex < xCuts.Count - 1; xIndex++)
            {
                var xMin = xCuts[xIndex];
                var xMax = xCuts[xIndex + 1];
                if (xMax - xMin < 0.1f)
                    continue;

                var intervals = holes
                    .Where(hole => hole.xMin < xMax && hole.xMax > xMin)
                    .Select(hole => new Vector2(hole.yMin, hole.yMax))
                    .OrderBy(interval => interval.x)
                    .ToList();

                var y = bounds.yMin;
                foreach (var interval in intervals)
                {
                    if (interval.x > y)
                        rects.Add(Rect.MinMaxRect(xMin, y, xMax, interval.x));
                    y = Mathf.Max(y, interval.y);
                }

                if (y < bounds.yMax)
                    rects.Add(Rect.MinMaxRect(xMin, y, xMax, bounds.yMax));
            }

            EnsureBlockerCount(rects.Count);
            for (var i = 0; i < _blockers.Count; i++)
            {
                var active = i < rects.Count;
                _blockers[i].gameObject.SetActive(active);
                if (active)
                    ApplyRect(_blockers[i].rectTransform, rects[i]);
            }
        }

        private void LayoutFrames(IReadOnlyList<Rect> holes)
        {
            EnsureFrameCount(holes.Count);
            for (var i = 0; i < _focusFrames.Count; i++)
            {
                var active = i < holes.Count;
                _focusFrames[i].gameObject.SetActive(active);
                if (active)
                    ApplyRect(_focusFrames[i], holes[i]);
            }
        }

        private void PositionTooltip(IReadOnlyList<Rect> holes)
        {
            if (holes == null || holes.Count == 0)
            {
                _tooltip.anchoredPosition = Vector2.zero;
                return;
            }

            var primary = holes[0];
            var bounds = _visualRoot.rect;
            var desiredY = primary.center.y >= 0f
                ? primary.yMin - _tooltip.rect.height * 0.5f - 32f
                : primary.yMax + _tooltip.rect.height * 0.5f + 32f;
            var x = Mathf.Clamp(primary.center.x, bounds.xMin + 230f, bounds.xMax - 230f);
            var y = Mathf.Clamp(desiredY, bounds.yMin + 120f, bounds.yMax - 120f);
            _tooltip.anchoredPosition = new Vector2(x, y);
        }

        private void BuildVisualTree()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 1000;

            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            _visualRoot = CreateRect("VisualRoot", transform);
            Stretch(_visualRoot);
            _blockersRoot = CreateRect("Blockers", _visualRoot);
            Stretch(_blockersRoot);
            _framesRoot = CreateRect("FocusFrames", _visualRoot);
            Stretch(_framesRoot);

            _continueButton = CreateButton("Continue", _visualRoot);
            Stretch(_continueButton.transform as RectTransform);
            _continueButton.onClick.AddListener(OnContinueClicked);

            _tooltip = CreateRect("Tooltip", _visualRoot);
            _tooltip.sizeDelta = new Vector2(520f, 190f);
            var tooltipImage = _tooltip.gameObject.AddComponent<Image>();
            tooltipImage.color = new Color(0.055f, 0.08f, 0.16f, 0.97f);
            tooltipImage.raycastTarget = false;

            _title = CreateText("Title", _tooltip, 40f, FontStyles.Bold);
            _title.rectTransform.anchorMin = new Vector2(0f, 0.55f);
            _title.rectTransform.anchorMax = new Vector2(1f, 1f);
            _title.rectTransform.offsetMin = new Vector2(28f, 0f);
            _title.rectTransform.offsetMax = new Vector2(-28f, -16f);
            _title.color = AccentColor;

            _instruction = CreateText("Instruction", _tooltip, 27f, FontStyles.Normal);
            _instruction.rectTransform.anchorMin = Vector2.zero;
            _instruction.rectTransform.anchorMax = new Vector2(1f, 0.62f);
            _instruction.rectTransform.offsetMin = new Vector2(28f, 18f);
            _instruction.rectTransform.offsetMax = new Vector2(-28f, 0f);
            _instruction.color = Color.white;
        }

        private void EnsureVisualTree()
        {
            if (_visualRoot != null)
                return;

            BuildVisualTree();
        }

        private void ApplyTutorialFont()
        {
            if (tutorialFont == null || _title == null || _instruction == null)
                return;

            var material = tutorialFontMaterial != null
                ? tutorialFontMaterial
                : tutorialFont.material;
            if (material == null)
                return;

            // This legacy font asset has no default material assigned, although its atlas is valid.
            // Give TMP the project's matching material before assigning the font to avoid atlas warnings.
            if (tutorialFont.material == null)
                tutorialFont.material = material;

            _title.fontSharedMaterial = material;
            _title.font = tutorialFont;
            _instruction.fontSharedMaterial = material;
            _instruction.font = tutorialFont;
        }

        private void EnsureBlockerCount(int count)
        {
            while (_blockers.Count < count)
            {
                var rect = CreateRect($"Dim_{_blockers.Count}", _blockersRoot);
                var image = rect.gameObject.AddComponent<Image>();
                image.color = DimColor;
                image.raycastTarget = true;
                _blockers.Add(image);
            }
        }

        private void EnsureFrameCount(int count)
        {
            while (_focusFrames.Count < count)
            {
                var frame = CreateRect($"Frame_{_focusFrames.Count}", _framesRoot);
                CreateFrameEdge(frame, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -3f), new Vector2(0f, 3f));
                CreateFrameEdge(frame, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, -3f), new Vector2(0f, 3f));
                CreateFrameEdge(frame, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(-3f, 0f), new Vector2(3f, 0f));
                CreateFrameEdge(frame, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-3f, 0f), new Vector2(3f, 0f));
                _focusFrames.Add(frame);
            }
        }

        private static void CreateFrameEdge(RectTransform parent, string name, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var edge = CreateRect(name, parent);
            edge.anchorMin = anchorMin;
            edge.anchorMax = anchorMax;
            edge.offsetMin = offsetMin;
            edge.offsetMax = offsetMax;
            var image = edge.gameObject.AddComponent<Image>();
            image.color = AccentColor;
            image.raycastTarget = false;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        private static Button CreateButton(string name, Transform parent)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.001f);
            image.raycastTarget = true;
            return rect.gameObject.AddComponent<Button>();
        }

        private static TMP_Text CreateText(string name, Transform parent, float size, FontStyles style)
        {
            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ApplyRect(RectTransform target, Rect rect)
        {
            target.anchorMin = target.anchorMax = new Vector2(0.5f, 0.5f);
            target.pivot = new Vector2(0.5f, 0.5f);
            target.anchoredPosition = rect.center;
            target.sizeDelta = rect.size;
        }

        private static Rect Intersect(Rect a, Rect b)
        {
            return Rect.MinMaxRect(
                Mathf.Max(a.xMin, b.xMin),
                Mathf.Max(a.yMin, b.yMin),
                Mathf.Min(a.xMax, b.xMax),
                Mathf.Min(a.yMax, b.yMax));
        }

        private void StopDeferredRoutine()
        {
            if (_deferredRoutine == null)
                return;

            StopCoroutine(_deferredRoutine);
            _deferredRoutine = null;
        }

        private void OnDestroy()
        {
            StopDeferredRoutine();
            _continueButton?.onClick.RemoveListener(OnContinueClicked);
            if (_isSubscribed)
                _signalBus.Unsubscribe<TutorialPhaseChangedSignal>(OnPhaseChanged);
        }
    }
}
