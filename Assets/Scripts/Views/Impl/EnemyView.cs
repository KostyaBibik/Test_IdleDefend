using Components;
using Db;
using Enums;
using UnityEngine;
using UnityEngine.UI;

namespace Views.Impl
{
    public class EnemyView : MonoBehaviour, IEntityView
    {
        [SerializeField] private Transform mesh;
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Animator animator;

        [HideInInspector] public HealthComponent healthComponent;
        [HideInInspector] public float speedMoving;
        [HideInInspector] public EEnemyType type;
        [HideInInspector] public EnemyDefinition definition;

        [HideInInspector] public float orbitAngleDeg = float.NaN;
        [HideInInspector] public float orbitRadius = -1f;

        public Transform Mesh => mesh;
        public Slider HealthSlider => healthSlider;
        public bool isDestroyed { get; set; }

        public void PlayDeathAnimation()
        {
            if (animator != null)
                animator.SetTrigger("Death");
            else
                mesh.gameObject.SetActive(false);
        }
    }
}
