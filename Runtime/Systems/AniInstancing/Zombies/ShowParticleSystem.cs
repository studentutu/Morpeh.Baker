namespace GBG.Rush.Zombies.Scripts
{
    using GBG.Rush.Utils.Pool;
    using Morpeh;
    using Morpeh.Globals;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Profiling;

    [CreateAssetMenu(menuName = "ECS/Systems/" + nameof(ShowParticleSystem))]
    public class ShowParticleSystem : UpdateSystem
    {
        private Filter particles;

        public override void OnAwake()
        {

            this.particles = this.World.Filter.With<ShowParticle>().Without<DisabledInPool>().Without<RecycleToPool>();

        }

        public override void OnUpdate(float deltaTime)
        {

            foreach (var entity in this.particles)
            {
                ref var particle = ref entity.GetComponent<ShowParticle>();
                particle.Time -= deltaTime;
                if (particle.Time < 0)
                {
                    entity.AddComponent<RecycleToPool>();
                };
            }
        }

    }
}