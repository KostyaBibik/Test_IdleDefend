using UnityEngine;

namespace Views.Impl
{
    public class EffectConnector : MonoBehaviour
    {
        [SerializeField] private Transform _activeObjectTransform;
        public Transform Target { get; private set; }
        public float Width { get; set; } = 1f;

        public void Connect(Transform target)
        {
            Target = target;
        }

        private void Update()
        {
            if (!Target) return;
            var direction = (Target.position - transform.position).normalized;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            var distance = Vector3.Distance(transform.position, Target.position);
            _activeObjectTransform.localScale = new Vector3(distance, Width, 1f);
        }
    }
}
