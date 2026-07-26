using TMPro;
using UnityEngine;

namespace VYandexTools.Advertisements.NoAds.NoAdsButton.Scripts
{
    public class TMPTextAdapter : TextAdapter
    {
        [SerializeField] private TMP_Text textComponent;

        protected override void SetText(string value) => textComponent.text = value;
        protected override string GetText() => textComponent.text;
    }
}