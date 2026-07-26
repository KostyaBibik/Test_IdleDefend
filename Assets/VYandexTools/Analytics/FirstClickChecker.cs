using System.Collections;
using UnityEngine;

namespace VYandexTools.Analytics
{
    public class FirstClickChecker : MonoBehaviour
    {
        private IEnumerator Start()
        {
            yield return GaEventProvider.FirstClickCheck();
            Destroy(gameObject);
        }
    }
}