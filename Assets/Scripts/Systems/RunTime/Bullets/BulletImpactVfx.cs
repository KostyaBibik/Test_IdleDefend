using System.Collections.Generic;
using UnityEngine;
using Views.Impl;

namespace Systems.RunTime.Bullets
{
    /// <summary>
    /// Визуал попадания снаряда, общий для боя (BulletHitSystem / TowerAttackSystem) и для
    /// витрины магазина (PreviewCombatDriver). Здесь нет ни урона, ни сервисов - только эффекты,
    /// поэтому превью показывает ровно то же, что игрок увидит в бою, и не разъезжается с ним
    /// при правках визуала.
    /// </summary>
    public static class BulletImpactVfx
    {
        private const float ImpactEffectLifetime = 2f;

        /// <summary>
        /// Разовый эффект в точке попадания сплэш-снаряда, отмасштабированный под фактический
        /// радиус поражения (в т.ч. увеличенный ультимейтом "Перегрузка"), а не под дефолт из
        /// ассета - иначе эффект соврёт про реальную зону.
        /// </summary>
        public static void SpawnSplashImpact(
            GameObject effectPrefab,
            Vector3 center,
            float splashRadius,
            float referenceRadius,
            int layer = -1)
        {
            if (effectPrefab == null)
                return;

            var instance = Object.Instantiate(effectPrefab, center, Quaternion.identity);

            var scale = splashRadius / Mathf.Max(0.01f, referenceRadius);
            instance.transform.localScale = Vector3.one * scale;

            // Партиклы Epic Toon FX несут собственный AudioSource с playOnAwake — при быстрой
            // стрельбе это дало бы наложение взрывов на каждый сплэш-хит, поэтому глушим звук
            // и оставляем только визуал.
            var audioSource = instance.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.Stop();
                audioSource.enabled = false;
            }

            if (layer >= 0)
                SetLayerRecursively(instance, layer);

            Object.Destroy(instance, ImpactEffectLifetime);
        }

        /// <summary>
        /// Линия-разряд от башни через всех пробитых врагов. LineRenderer есть только у
        /// prefab-варианта башни с Pierce - у остальных null, это нормально.
        /// </summary>
        public static void ShowPierceLine(TowerView towerView, Vector3 towerPos, IReadOnlyList<Vector3> hitPoints)
        {
            var line = towerView.PierceLine;
            if (line == null)
                return;

            line.positionCount = hitPoints.Count + 1;
            line.SetPosition(0, towerPos);
            for (var i = 0; i < hitPoints.Count; i++)
                line.SetPosition(i + 1, hitPoints[i]);

            towerView.pierceLineRemaining = towerView.pierceLineDuration;
        }

        /// <summary>
        /// Гасит линию-разряд по истечении её времени жизни. Дёргается каждый тик и в бою,
        /// и в превью.
        /// </summary>
        public static void TickPierceLine(TowerView towerView, float deltaTime)
        {
            if (towerView.pierceLineRemaining <= 0f)
                return;

            towerView.pierceLineRemaining -= deltaTime;

            if (towerView.pierceLineRemaining <= 0f && towerView.PierceLine != null)
                towerView.PierceLine.positionCount = 0;
        }

        public static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;

            foreach (Transform child in target.transform)
                SetLayerRecursively(child.gameObject, layer);
        }
    }
}
