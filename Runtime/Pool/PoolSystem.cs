namespace GBG.Rush.Utils.Pool
{
    using System;
    using System.Collections.Generic;
    using Morpeh;
    using UnityEngine;
    using UnityEngine.Profiling;

    [CreateAssetMenu(menuName = "Rush/Utils/Systems/" + nameof(PoolSystem))]
    public sealed class PoolSystem : UpdateSystem
    {
        public Vector3 poolPosition;
        public int startStackCapacity;

        private Filter pools;
        private Filter warmEvents;
        private Filter entitiesToRecycle;
        private Filter entitiesToReset;

        public override void OnAwake()
        {
            //this.World.CreateEntity().SetComponent(new PoolItems {
            //    items = new Dictionary<EntityProvider, Stack<IEntity>>()
            //});

            this.pools = this.World.Filter.With<PoolItems>();
            this.warmEvents = this.World.Filter.With<WarmPoolEvent>();
            this.entitiesToReset = this.World.Filter.With<ResetAfterPool>();
            this.entitiesToRecycle = this.World.Filter.With<Poolable>().With<RecycleToPool>();
        }

        public override void OnUpdate(float deltaTime)
        {
            Profiler.BeginSample("PoolSystem");
            this.ProcessWarmEvents();
            this.RecycleEntities();

            foreach (var ent in this.entitiesToReset)
            {
                ent.RemoveComponent<ResetAfterPool>();
            }
            Profiler.EndSample();
        }

        public override void Dispose()
        {
            var created = this.World.Filter.With<PoolItems>();
            foreach (var ent in created)
            {
                this.World.RemoveEntity(ent);
            }
        }

        private void ProcessWarmEvents()
        {
            ref var poolItems = ref this.pools.First().GetComponent<PoolItems>();
            foreach (var ent in this.warmEvents)
            {
                var evt = ent.GetComponent<WarmPoolEvent>();
                for (int i = 0, length = evt.prefabs.Length; i < length; i++)
                {
                    this.CreateInPool(evt.prefabs[i], ref poolItems, evt.counts[i]);
                }

                ent.RemoveComponent<WarmPoolEvent>();
            }
        }

        private void RecycleEntities()
        {
            ref var poolItems = ref this.pools.First().GetComponent<PoolItems>();
            foreach (var ent in this.entitiesToRecycle)
            {
                this.Recycle(ent, ref poolItems);
                ent.RemoveComponent<RecycleToPool>();
            }
        }

        private void CreateInPool(EntityProvider prefab, ref PoolItems poolItems, int count = 1)
        {
            if (poolItems.items.TryGetValue(prefab, out var stack))
            {
                count -= stack.Count;
            }

            for (var i = 0; i < count; i++)
            {
                var entity = InstantiatePoolableEntity(prefab, this.poolPosition, Quaternion.identity);
                this.AddToPool(prefab, entity, ref poolItems);
            }
        }

        private void Recycle(IEntity entity, ref PoolItems poolItems)
        {
            if (entity.Has<DisabledInPool>())
            {
                throw new Exception($"Attempt to recycle Entity {entity.ID} that already {nameof(DisabledInPool)}");
            }

            ref var poolable = ref entity.GetComponent<Poolable>();
            poolable.transform.position = this.poolPosition;
            this.AddToPool(poolable.originPrefab, entity, ref poolItems);
        }

        private void AddToPool(EntityProvider prefab, IEntity entity, ref PoolItems poolItems)
        {
            entity.AddComponent<DisabledInPool>();
            if (!poolItems.items.TryGetValue(prefab, out var pool))
            {
                pool = new Stack<IEntity>(this.startStackCapacity);
                poolItems.items.Add(prefab, pool);
            }

            pool.Push(entity);
        }

        public static IEntity InstantiatePoolableEntity(EntityProvider prefab, Vector3 spawnPosition, Quaternion rotation)
        {
            var entityProvider = Instantiate(prefab, spawnPosition, rotation);
            entityProvider.gameObject.SetActive(true);

            var entity = entityProvider.Entity;
            entity.SetComponent(new Poolable
            {
                transform = entityProvider.transform,
                originPrefab = prefab
            });

            return entity;
        }

        public static PoolSystem Create() => CreateInstance<PoolSystem>();
    }
}