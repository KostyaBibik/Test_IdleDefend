using UnityEngine;

namespace VYandexTools.Advertisements.NoAds.NoAdsButton.Scripts
{
    public abstract class TextAdapter : MonoBehaviour
    {
        public string Text
        {
            get => GetText();
            set => SetText(value);
        }

        protected abstract void SetText(string value);
        protected abstract string GetText();
    }
}