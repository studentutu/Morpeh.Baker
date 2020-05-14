namespace GBG.Rush.AniInstancing.Scripts
{
    // using GBG.Rush.Collisions;
    // using GBG.Rush.Healthcare;
    using GBG.Rush.Utils.Pool;
    using GBG.Rush.Zombies.Scripts;
    using Morpeh;
    using UnityEngine;

    [CreateAssetMenu(menuName = "ECS/Systems/Utils/" + nameof(CrowdSpawnSystem))]
    public class CrowdSpawnSystem : UpdateSystem
    {

        public ZombieProvider PrefabZombie;
        private Filter spawners;
        private Filter needClear;
        private Filter pools;
        private int currentPoolIndex = 0;

        public override void OnAwake()
        {

            this.pools = this.World.Filter.With<PoolItems>();

            this.spawners = this.World.Filter.With<CrowdSpawnerComponent>().With<SpawnMarker>().Without<SpawnedMarker>();
            this.needClear = this.World.Filter.With<AnimationInstancingComponent>().With<DisabledInPool>().With<StopAnimationMarker>();

        }
        public override void OnUpdate(float deltaTime)
        {
            this.currentPoolIndex = 0;
            foreach (var entity in this.needClear)
                entity.RemoveComponent<StopAnimationMarker>();


            ref var poolItems = ref this.pools.First().GetComponent<PoolItems>();

            foreach (var entity in this.spawners)
            {
                ref var spawner = ref entity.GetComponent<CrowdSpawnerComponent>();
                var transform = spawner.transform;
                var spawnRange = spawner.spawnRange;
                var offsetStep = spawner.offsetStep;
                var angles = spawner.angles;
                var angleRange = spawner.anglesRange;

                var positions = SpawneHelper.DoWhileInRange(transform, spawnRange, offsetStep);
                foreach (var pos in positions)
                {
                    spawner.crowd[spawner.countCrowd] = CreateZombieEntity(pos, angles, angleRange, ref poolItems);
                    spawner.countCrowd++;
                }
                entity.RemoveComponent<SpawnMarker>();
                entity.AddComponent<SpawnedMarker>();
            }
        }

        private IEntity CreateZombieEntity(Vector3 position, Vector3 angles, Vector3 anglesRange, ref PoolItems poolItems)
        {
            if (poolItems.items[PrefabZombie].Count <= this.currentPoolIndex)
                return null;

            var entity = poolItems.GetInstance(PrefabZombie);
            ref var zombie = ref entity.GetComponent<Zombie>();
            zombie.root.transform.position = position;

            ref var component = ref entity.GetComponent<AnimationInstancingComponent>();
            angles.y += Random.Range(-anglesRange.y, anglesRange.y);
            const float stepRange = 0.25f;
            position.x += Random.Range(-stepRange, +stepRange);
            position.z += Random.Range(-stepRange, +stepRange);
            component.UpdateTransform(Matrix4x4.TRS(position, Quaternion.Euler(angles), Vector3.one));


            // var colliders = entity.GetComponent<ReadyToTrigger>().listener.colliders;
            // foreach (var item in colliders) item.enabled = true;

            entity.RemoveComponent<DisabledInPool>();
            this.currentPoolIndex++;
            return entity;
        }
    }
}