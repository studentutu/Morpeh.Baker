namespace GBG.Rush.Zombies.Scripts
{
    using GBG.Rush.AniInstancing.Scripts;
    // using GBG.Rush.Collisions;
    // using GBG.Rush.Player;
    // using GBG.Rush.Vehicles;
    using Morpeh;
    using Morpeh.Globals;
    using UnityEngine;
    using UnityEngine.Profiling;
    using System.Collections.Generic;
    using GBG.Rush.Utils.Pool;

    [CreateAssetMenu(menuName = "ECS/Systems/" + nameof(CreateParticleSystem))]
    public class CreateParticleSystem : UpdateSystem
    {
        private Filter particles;
        private Filter pools;
        public override void OnAwake()
        {
            this.pools = this.World.Filter.With<PoolItems>();
            this.particles = this.World.Filter.With<CreateParticle>();

        }

        public override void OnUpdate(float deltaTime)
        {
            Profiler.BeginSample("CreateParticleSystem");
            ref var poolItems = ref this.pools.First().GetComponent<PoolItems>();
            foreach (var entity in this.particles)
            {
                ref var createData = ref entity.GetComponent<CreateParticle>();
                var entityParticle = poolItems.GetInstance(createData.Prefab);
                entityParticle.RemoveComponent<DisabledInPool>();
                ref var newparticle = ref entityParticle.GetComponent<ShowParticle>();
                newparticle.root.transform.position = createData.Position;
                newparticle.Time = newparticle.ShowTime;
                newparticle.root.SetActive(true);
                foreach (var item in newparticle.Particles)
                    item.Play();

                World.RemoveEntity(entity);
            }
            Profiler.EndSample();

        }
    }
}