using System.Collections;
using UnityEngine;

namespace UI.Views
{
    /// <summary>
    /// Раз в случайный интервал спавнит "пролетающую звезду" из случайной точки у края экрана
    /// в случайном направлении через экран. Партикл-модуль сам содержит только визуал одного
    /// пролёта (размер/цвет/затухание) - позицию и направление на каждый пролёт выставляет этот
    /// скрипт через transform.anchoredPosition + velocityOverLifetime и запускает Emit() вручную,
    /// поэтому у ParticleSystem отключены встроенные Bursts/RateOverTime.
    /// </summary>
    public class ShootingStarSpawner : MonoBehaviour
    {
        [SerializeField] private ParticleSystem shootingStarParticles;
        [SerializeField] private RectTransform spawnArea;
        [SerializeField] private float minInterval = 5f;
        [SerializeField] private float maxInterval = 8f;
        [SerializeField] private float speed = 850f;
        [SerializeField] private int burstMin = 16;
        [SerializeField] private int burstMax = 22;
        [SerializeField, Range(0f, 1f)] private float directionSpread = 0.6f;

        private void OnEnable()
        {
            StartCoroutine(SpawnLoop());
        }

        private IEnumerator SpawnLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
                SpawnOnce();
            }
        }

        public void SpawnOnce()
        {
            if (shootingStarParticles == null || spawnArea == null)
                return;

            var rect = spawnArea.rect;
            Vector2 start;
            Vector2 direction;

            switch (Random.Range(0, 4))
            {
                case 0:
                    start = new Vector2(Random.Range(rect.xMin, rect.xMax), rect.yMax);
                    direction = new Vector2(Random.Range(-directionSpread, directionSpread), -1f);
                    break;
                case 1:
                    start = new Vector2(rect.xMax, Random.Range(rect.yMin, rect.yMax));
                    direction = new Vector2(-1f, Random.Range(-directionSpread, directionSpread));
                    break;
                case 2:
                    start = new Vector2(Random.Range(rect.xMin, rect.xMax), rect.yMin);
                    direction = new Vector2(Random.Range(-directionSpread, directionSpread), 1f);
                    break;
                default:
                    start = new Vector2(rect.xMin, Random.Range(rect.yMin, rect.yMax));
                    direction = new Vector2(1f, Random.Range(-directionSpread, directionSpread));
                    break;
            }

            direction.Normalize();
            ((RectTransform) transform).anchoredPosition = start;

            var velocityOverLifetime = shootingStarParticles.velocityOverLifetime;
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(direction.x * speed);
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(direction.y * speed);

            shootingStarParticles.Emit(Random.Range(burstMin, burstMax + 1));
        }
    }
}
