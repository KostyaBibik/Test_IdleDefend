using UnityEngine;
using UnityEngine.UI;

namespace VYandexTools.Advertisements.NoAds.NoAdsButton.Scripts
{
    public class LegacyTextAdapter : TextAdapter
    {
        [SerializeField] private Text textComponent;

        protected override void SetText(string value) => textComponent.text = value;
        protected override string GetText() => textComponent.text;
    }
}