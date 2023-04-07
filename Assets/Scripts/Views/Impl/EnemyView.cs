using Components;
using Enums;
using UnityEngine;
using UnityEngine.UI;

namespace Views.Impl
{
    public class EnemyView : MonoBehaviour, IEntityView
    {
        [SerializeField] private Transform mesh;
        [SerializeField] private Slider healthSlider;

        [HideInInspector] public HealthComponent healthComponent;
        [HideInInspector] public EEnemyType type;
        
        public Transform Mesh => mesh;
        public Slider HealthSlider => healthSlider;
        public bool isDestroyed { get; set; }
    }
}