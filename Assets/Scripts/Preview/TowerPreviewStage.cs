using Db;
using Enums;

using Systems.RunTime.Bullets;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Views.Impl;

namespace Preview
{
    /// <summary>
    /// Живая "витрина" башни: собственная камера + свет + слой Preview, рендер в RenderTexture,
    /// который показывает UI (TowerPreviewSurfaceView). Инстанцирует ровно те же боевые префабы,
    /// что и GameInstaller, поэтому любая правка визуала башни видна в магазине без дублирования
    /// ассетов.
    ///
    /// Живёт только пока витрина показана: камера включается вместе со Show и гасится в Clear,
    /// RenderTexture освобождается - на главном экране и в бою нагрузки нет.
    /// </summary>
    [DisallowMultipleComponent]
    public class TowerPreviewStage : MonoBehaviour
    {
        public const string PreviewLayerName = "Preview";

        [SerializeField] private TowerPreviewSettings settings;
        [SerializeField] private Camera previewCamera;
        [Tooltip("Точка, в которой стоит башня. Враги подлетают к ней со всех сторон")]
        [SerializeField] private Transform towerRoot;
        [Tooltip("Родитель для манекенов, снарядов и эффектов - чтобы витрина чистилась одним махом")]
        [SerializeField] private Transform combatRoot;
        [SerializeField] private PreviewCombatDriver combatDriver;
        [Tooltip("Локальный Volume витрины. Лежит на слое Preview, поэтому глобальные Volume сцены " +
                 "в него не вмешиваются, а он - в них")]
        [SerializeField] private Volume postProcessingVolume;

        /// <summary>Имя объекта-ауры внутри prefab-вариантов башни (одинаково у всех четырёх).</summary>
        private const string AuraObjectName = "Aura";

        private RenderTexture _texture;
        private TowerView _towerView;
        private bool _transparentBackground;

        /// <summary>Половина высоты кадра в покое: тело башни занимает TowerFillRatio кадра.</summary>
        public float IdleFrameRadius { get; private set; }

        /// <summary>Половина высоты кадра, пока в нём есть манекен - иначе врагу негде подлетать.</summary>
        public float CombatFrameRadius { get; private set; }

        public RenderTexture Texture => _texture;
        public TowerPreviewSettings Settings => settings;
        public Transform TowerRoot => towerRoot;
        public Transform CombatRoot => combatRoot;
        public int PreviewLayer { get; private set; }

        private void Awake()
        {
            PreviewLayer = LayerMask.NameToLayer(PreviewLayerName);

            if (PreviewLayer < 0)
            {
                // Слой не заведён в TagManager — витрина отрендерится, но её объекты попадут
                // и в основную камеру. Ронять из-за этого меню нельзя.
                Debug.LogWarning($"[{nameof(TowerPreviewStage)}] Слой '{PreviewLayerName}' не найден, " +
                                 "витрина может протечь в основную камеру.");
                PreviewLayer = gameObject.layer;
            }

            SetActiveState(false);
        }

        /// <summary>
        /// Показывает башню товара. Для товаров без тела башни (бусты, эмеральды) возвращает false -
        /// вызывающий UI в этом случае откатывается на статичную иконку.
        ///
        /// transparentBackground нужен там, где витрина стоит прямо на фоне экрана (главное меню):
        /// в попапе фон, наоборот, рисуется, чтобы кадр читался как окно в бой.
        /// </summary>
        public bool Show(ShopItemDefinition item, bool transparentBackground = false)
        {
            return Show(item != null ? item.TowerBody : null, transparentBackground);
        }

        public bool Show(TowerBodyDefinition body, bool transparentBackground = false)
        {
            Clear();

            _transparentBackground = transparentBackground;

            var prefab = ResolveTowerPrefab(body);
            if (prefab == null || settings == null)
                return false;

            _towerView = Instantiate(prefab, towerRoot.position, Quaternion.identity, towerRoot);
            BulletImpactVfx.SetLayerRecursively(_towerView.gameObject, PreviewLayer);

            if (body != null && body.HideAuraInPreview)
                HideAura(_towerView.transform);

            ApplyBody(_towerView, body);
            MeasureFraming(_towerView);
            SetupCamera();
            SetActiveState(true);
            combatDriver.Begin(this, _towerView);

            return true;
        }

        public void Clear()
        {
            combatDriver.StopAndCleanup();

            if (_towerView != null)
                Destroy(_towerView.gameObject);

            _towerView = null;
            SetActiveState(false);
        }

        /// <summary>
        /// Тот же выбор prefab-варианта, что делает GameInstaller.BindAndCreateTowerView:
        /// вариант тела, а если его нет - базовый префаб башни из боевого конфига.
        /// </summary>
        private TowerView ResolveTowerPrefab(TowerBodyDefinition body)
        {
            if (body != null && body.TowerPrefabVariant != null)
                return body.TowerPrefabVariant;

            return settings != null && settings.TowerConfigSettings != null
                ? settings.TowerConfigSettings.PrefabViewTower
                : null;
        }

        /// <summary>
        /// Переносит на витринную башню те же поля, что проставляет TowerInitializeSystem в бою.
        /// Урон/здоровье не нужны - витрина не считает бой, но тип атаки и параметры эффектов
        /// определяют, что именно игрок увидит при попадании.
        /// </summary>
        private void ApplyBody(TowerView towerView, TowerBodyDefinition body)
        {
            var towerConfig = settings.TowerConfigSettings;

            // Без боевого конфига берём заведомо большую дистанцию: витрина всё равно кадрирована так,
            // что манекен появляется рядом с башней, и "не достал" здесь было бы просто багом показа.
            towerView.attackDistance = towerConfig != null ? towerConfig.RangeAttack : 10f;
            towerView.attackSpeed = towerConfig != null ? towerConfig.AttackSpeed : 1f;
            towerView.attackDamage = towerConfig != null ? towerConfig.AttackDamage : 1;
            towerView.ratioRange = 1f;

            if (body == null)
            {
                towerView.attackType = EMainTowerAttackType.Default;
                return;
            }

            towerView.attackType = body.AttackType;

            towerView.pierceCount = body.PierceCount;
            towerView.pierceJumpRadius = body.PierceJumpRadius;
            towerView.pierceFalloff = body.PierceFalloff;
            towerView.pierceLineDuration = body.PierceLineDuration;
            towerView.pierceLineRemaining = 0f;

            towerView.frostSlowPercent = body.FrostSlowPercent;
            towerView.frostSlowDuration = body.FrostSlowDuration;

            towerView.splashRadius = body.SplashRadius;
            towerView.splashFalloff = body.SplashFalloff;
            towerView.splashImpactEffectPrefab = body.SplashImpactEffectPrefab;
            towerView.splashImpactEffectReferenceRadius = body.SplashImpactEffectReferenceRadius;
        }

        /// <summary>
        /// Кадр считается от фактических габаритов тела башни, а не от числа в настройках: партиклы
        /// (аура, искры) и вырожденные Trail/Line-рендереры в расчёт не идут - иначе аркановая аура
        /// радиусом в пол-экрана диктовала бы масштаб, и сама башня оставалась бы точкой.
        /// </summary>
        private void MeasureFraming(TowerView towerView)
        {
            var bounds = new Bounds(towerView.transform.position, Vector3.zero);
            var measured = false;

            foreach (var renderer in towerView.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer)
                    continue;

                if (!measured)
                {
                    bounds = renderer.bounds;
                    measured = true;
                    continue;
                }

                bounds.Encapsulate(renderer.bounds);
            }

            var radius = measured
                ? Mathf.Max(bounds.extents.x, bounds.extents.y)
                : settings.FallbackTowerRadius;

            IdleFrameRadius = radius / Mathf.Max(0.01f, settings.TowerFillRatio);
            CombatFrameRadius = IdleFrameRadius * settings.CombatFrameMultiplier;
        }

        /// <summary>
        /// Аура прячется только в витрине - в бою prefab-вариант остаётся нетронутым, потому что
        /// выключается инстанс, а не ассет.
        /// </summary>
        private static void HideAura(Transform towerTransform)
        {
            foreach (var child in towerTransform.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == AuraObjectName)
                    child.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (_towerView == null || !previewCamera.enabled)
                return;

            // Крупный план, пока башня одна в кадре; на время боя кадр плавно отъезжает, чтобы
            // манекену было откуда лететь.
            var target = combatDriver.HasEnemies ? CombatFrameRadius : IdleFrameRadius;

            previewCamera.orthographicSize = Mathf.Lerp(
                previewCamera.orthographicSize,
                target,
                settings.FrameLerpSpeed * Time.unscaledDeltaTime);
        }

        private void SetupCamera()
        {
            if (_texture == null)
            {
                var resolution = settings.TextureResolution;
                // ARGB32 (а не Default) — иначе прозрачный фон витрины на главном экране
                // не переживёт запись в текстуру.
                _texture = new RenderTexture(resolution, resolution, 16, RenderTextureFormat.ARGB32)
                {
                    name = $"{nameof(TowerPreviewStage)}RT",
                    antiAliasing = 1,
                    useMipMap = false,
                    autoGenerateMips = false
                };
                _texture.Create();
            }

            previewCamera.targetTexture = _texture;
            previewCamera.orthographic = true;
            previewCamera.orthographicSize = IdleFrameRadius;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            var background = settings.BackgroundColor;
            previewCamera.backgroundColor = _transparentBackground
                ? new Color(background.r, background.g, background.b, 0f)
                : background;
            previewCamera.cullingMask = 1 << PreviewLayer;
            previewCamera.transform.position = towerRoot.position + Vector3.back * 10f;
            previewCamera.transform.rotation = Quaternion.identity;
            previewCamera.nearClipPlane = 0.1f;
            previewCamera.farClipPlane = 100f;

            SetupPostProcessing();
        }

        /// <summary>
        /// Витрина должна выглядеть как бой, а бой рисуется с HDR и глобальным Volume (Bloom +
        /// ColorAdjustments) - без них картинка тусклее игровой. Профиль берётся тот же самый
        /// (ссылка на боевой ассет), а не копия, поэтому правки грейдинга видны в магазине сами.
        ///
        /// Volume витрины лежит на слое Preview и камера смотрит только на этот слой: глобальный
        /// Volume сцены на витрину не влияет, а витринный - на сцену.
        /// </summary>
        private void SetupPostProcessing()
        {
            // URP 14 (Unity 2022.3) не умеет отдавать альфу после пост-обработки: UberPost пишет
            // alpha = 1, и витрина с прозрачным фоном превращается в белый прямоугольник. Поэтому
            // там, где фон прозрачный (главный экран), пост-обработка не включается вовсе.
            var wantsPost = settings.UsePostProcessing
                            && settings.PostProcessingProfile != null
                            && !_transparentBackground;

            if (postProcessingVolume != null)
            {
                postProcessingVolume.gameObject.layer = PreviewLayer;
                postProcessingVolume.isGlobal = true;
                postProcessingVolume.sharedProfile = settings.PostProcessingProfile;
                postProcessingVolume.enabled = wantsPost;
            }

            previewCamera.allowHDR = settings.UseHdr;

            var cameraData = previewCamera.GetUniversalAdditionalCameraData();
            if (cameraData == null)
                return;

            cameraData.renderType = CameraRenderType.Base;
            cameraData.renderPostProcessing = wantsPost;
            cameraData.volumeLayerMask = 1 << PreviewLayer;
            cameraData.volumeTrigger = transform;
            // Сглаживание и тени остаются выключенными: это лишние проходы на кадр в WebGL,
            // а на «сочность» картинки они не влияют - её дают Bloom и грейдинг.
            cameraData.antialiasing = AntialiasingMode.None;
            cameraData.renderShadows = false;
        }

        private void SetActiveState(bool active)
        {
            if (previewCamera != null)
                previewCamera.enabled = active;

            if (!active && _texture != null)
            {
                if (previewCamera != null)
                    previewCamera.targetTexture = null;

                _texture.Release();
                Destroy(_texture);
                _texture = null;
            }
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
