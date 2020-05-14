namespace GBG.Rush.Zombies.Scripts
{
    using GBG.Rush.AniInstancing.Scripts;
    // using GBG.Rush.Collisions;
    // using GBG.Rush.Healthcare;
    // using GBG.Rush.Player;
    // using GBG.Rush.Vehicles;
    using Morpeh;
    using Morpeh.Globals;
    using UnityEngine;
    using UnityEngine.Profiling;

    [CreateAssetMenu(menuName = "ECS/Systems/" + nameof(ZombieCollisionSystem))]
    public class ZombieCollisionSystem : UpdateSystem
    {
        private Filter zombies;

        public override void OnAwake()
        {
            // this.zombies = this.World.Filter.With<TriggerEnterEvent>();
        }

        public Vector3 positionZombie;
        public override void OnUpdate(float deltaTime)
        {
            Profiler.BeginSample("ZombieCollisionSystem");
            // foreach (var entity in zombies)
            // {
            //     ref var triggerEE = ref entity.GetComponent<TriggerEnterEvent>();
            //     if (!triggerEE.ownerEntity.Has<Zombie>() || triggerEE.ownerEntity.Has<IsDead>()) return;
            //     triggerEE.ownerEntity.AddComponent<IsDead>();
            // }

            Profiler.EndSample();
        }
    }
}