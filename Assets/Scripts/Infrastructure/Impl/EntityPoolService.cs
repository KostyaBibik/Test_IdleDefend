using System;
using System.Collections.Generic;
using Installers;
using UnityEngine;
using Views;
using Object = UnityEngine.Object;

namespace Infrastructure.Impl
{
    public interface IEntityPoolService
    {
        T Rent<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component, IEntityView;
        void Return<T>(T instance) where T : Component, IEntityView;

        /// <summary>
        /// Заранее создаёт count неактивных экземпляров prefab и кладёт их в пул (см.
        /// EntityPoolPrewarmSystem) - чтобы первые Rent на уровне брали готовый объект, а не
        /// платили Instantiate прямо во время геймплея.
        /// </summary>
        void Prewarm<T>(T prefab, int count) where T : Component, IEntityView;
    }

    /// <summary>
    /// Единая точка входа для пулинга снарядов/врагов (см. Docs/план пулинга): вместо
    /// Instantiate/Destroy на каждый спавн/смерть GameObject-ы переиспользуются. Здесь же
    /// сосредоточена вся ответственность за isDestroyed/poolVersion (см. IEntityView) - ни один
    /// вызывающий код не выставляет их сам, чтобы не разъезжалось по разным местам и не терялось.
    /// </summary>
    public class EntityPoolService : IEntityPoolService, IDisposable
    {
        private readonly Dictionary<Type, object> _poolsByType = new();

        // Кто прямо сейчас физически лежит в каком-то пуле - защита от двойного Return одного и
        // того же инстанса (иначе один и тот же объект мог бы попасть в стек дважды и Rent выдал
        // бы его одновременно двум разным логическим сущностям).
        private readonly HashSet<Component> _currentlyPooled = new();

        public T Rent<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component, IEntityView
        {
            var pool = GetPool<T>();
            var instance = pool.Rent(prefab, position, rotation);

            _currentlyPooled.Remove(instance);
            instance.isDestroyed = false;
            instance.poolVersion++;

            // Именно здесь, а не в Return: объект уже перемещён в новую точку и включён, поэтому
            // хвост (TrailRenderer) стартует с чистого листа. Если чистить при возврате в пул,
            // он всё равно дорисует отрезок из старой позиции в новую на первом же кадре.
            if (instance is Views.Impl.BulletView bulletView)
                bulletView.ResetVisualsOnRent();

            return instance;
        }

        public void Return<T>(T instance) where T : Component, IEntityView
        {
            if (instance == null)
                return;

            if (!_currentlyPooled.Add(instance))
                return; // уже возвращён этим же вызовом ранее - игнорируем повтор

            instance.isDestroyed = true;

            if (instance is Views.Impl.EnemyView enemyView)
                enemyView.ResetForPool();

            GetPool<T>().Return(instance);
        }

        public void Prewarm<T>(T prefab, int count) where T : Component, IEntityView
        {
            if (prefab == null || count <= 0)
                return;

            GetPool<T>().Prewarm(prefab, count);
        }

        public void Dispose()
        {
            foreach (var pool in _poolsByType.Values)
                ((IDisposablePool) pool).DestroyAll();

            _poolsByType.Clear();
            _currentlyPooled.Clear();
        }

        private PrefabPool<T> GetPool<T>() where T : Component, IEntityView
        {
            var type = typeof(T);
            if (_poolsByType.TryGetValue(type, out var pool))
                return (PrefabPool<T>) pool;

            var created = new PrefabPool<T>();
            _poolsByType[type] = created;
            return created;
        }

        private interface IDisposablePool
        {
            void DestroyAll();
        }

        /// <summary>
        /// Простой пул на Stack&lt;T&gt; по каждому префабу отдельно (снаряды/враги используют
        /// разные префабы на тип). Не знает про isDestroyed/poolVersion - этим занимается
        /// EntityPoolService, чтобы сам пул оставался простым переиспользуемым куском.
        /// </summary>
        private class PrefabPool<T> : IDisposablePool where T : Component
        {
            private readonly Dictionary<T, Stack<T>> _pools = new();
            private readonly Dictionary<T, T> _instancePrefab = new();

            public T Rent(T prefab, Vector3 position, Quaternion rotation)
            {
                var stack = GetOrCreateStack(prefab);
                while (stack.Count > 0)
                {
                    var pooled = stack.Pop();
                    if (pooled == null)
                        continue; // сцена могла почистить объект под нами

                    pooled.transform.SetPositionAndRotation(position, rotation);
                    pooled.gameObject.SetActive(true);
                    return pooled;
                }

                var instance = DiContainerRef.Container.InstantiatePrefabForComponent<T>(
                    prefab, position, rotation, null);
                _instancePrefab[instance] = prefab;
                return instance;
            }

            public void Prewarm(T prefab, int count)
            {
                var stack = GetOrCreateStack(prefab);
                for (var i = 0; i < count; i++)
                {
                    var instance = DiContainerRef.Container.InstantiatePrefabForComponent<T>(
                        prefab, Vector3.zero, Quaternion.identity, null);
                    instance.gameObject.SetActive(false);
                    _instancePrefab[instance] = prefab;
                    stack.Push(instance);
                }
            }

            public void Return(T instance)
            {
                instance.gameObject.SetActive(false);

                if (_instancePrefab.TryGetValue(instance, out var prefab))
                {
                    GetOrCreateStack(prefab).Push(instance);
                }
                else
                {
                    // Не из этого пула (не должно происходить) - подстраховка, чтобы не потерять объект молча.
                    Object.Destroy(instance.gameObject);
                }
            }

            public void DestroyAll()
            {
                foreach (var stack in _pools.Values)
                {
                    while (stack.Count > 0)
                    {
                        var instance = stack.Pop();
                        if (instance != null)
                            Object.Destroy(instance.gameObject);
                    }
                }

                _pools.Clear();
                _instancePrefab.Clear();
            }

            private Stack<T> GetOrCreateStack(T prefab)
            {
                if (_pools.TryGetValue(prefab, out var stack))
                    return stack;

                stack = new Stack<T>();
                _pools[prefab] = stack;
                return stack;
            }
        }
    }
}
