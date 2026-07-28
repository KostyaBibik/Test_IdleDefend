using UnityEngine;
using System.Collections;

namespace EpicToonFX
{
    public class ETFXLightFade : MonoBehaviour
    {
        [Header("Seconds to dim the light")]
        public float life = 0.2f;
        public bool killAfterLife = true;

        private Light li;
        private float initIntensity;

        // Use this for initialization
        void Start()
        {
            if (gameObject.GetComponent<Light>())
            {
                li = gameObject.GetComponent<Light>();
                initIntensity = li.intensity;
            }
            else
                print("No light object found on " + gameObject.name);
        }

        // Update is called once per frame
        void Update()
        {
            if (li == null)
                return;

            li.intensity -= initIntensity * (Time.deltaTime / life);

            if (!killAfterLife || li.intensity > 0)
                return;

            // Оригинал делал Destroy(GetComponent<Light>()), но в URP на объекте со светом лежит
            // UniversalAdditionalLightData, который от Light зависит - удаление молча проваливается
            // с ошибкой "Can't remove Light because UniversalAdditionalLightData depends on it",
            // и т.к. Light остаётся, попытка повторяется каждый кадр до конца жизни эффекта.
            // Гасим свет и отключаем сам компонент: результат для игрока тот же, спама в консоли нет,
            // и лишний Update не крутится (важно для WebGL, где эффектов на экране много).
            li.enabled = false;
            enabled = false;
        }
    }
}