using Components;
using UnityEngine;
using UnityEngine.UI;

namespace Views
{
    public class EnemyView : MonoBehaviour
    {
        [SerializeField] private Transform mesh;
        [SerializeField] private Slider healthSlider;

        [HideInInspector] public HealthComponent healthComponent;
        
        public Transform Mesh => mesh;
        public Slider HealthSlider => healthSlider;
    }
}