using Services;
using TMPro;
using UnityEngine;
using Zenject;

namespace UI.Views
{
    public class CoinsUiView : MonoBehaviour
    {
        [SerializeField] private TMP_Text coinsTxt;
        
        [Inject]
        public void Construct(CoinService coinService)
        {
            coinService.onUpdateCountCoins += UpdateCoinsView;
        }

        private void UpdateCoinsView(int newCount)
        {
            coinsTxt.text = newCount.ToString();
        }
    }
}