namespace GBG.Rush.Zombies.Scripts
{
    using GBG.Rush.AniInstancing.Scripts;
    // using GBG.Rush.Collisions;
    // using GBG.Rush.Healthcare;
    // using GBG.Rush.Player;
    // using GBG.Rush.Utils.Pool;
    // using GBG.Rush.Vehicles;
    using Morpeh;
    using Morpeh.Globals;
    using UnityEngine;
    using UnityEngine.Profiling;

    [CreateAssetMenu(menuName = "ECS/Systems/" + nameof(ZombieDieSystem))]
    public class ZombieDieSystem : UpdateSystem
    {
        public ShowParticleProvider PrefabFxBood;
        public GlobalVariableInt DeadZombies;
        private Filter zombies;

        public override void OnAwake()
        {
            // this.zombies = this.World.Filter.With<IsDead>().With<Zombie>().Without<StopAnimationMarker>().Without<DisabledInPool>();
        }

        public override void OnUpdate(float deltaTime)
        {
            // foreach (var entity in zombies) {
            //     ref var zombie = ref entity.GetComponent<Zombie>();

            //     DeadZombies.Value++;
            //     entity.RemoveComponent<IsDead>();
            //     entity.AddComponent<StopAnimationMarker>();
            //     var colliders = entity.GetComponent<ReadyToTrigger>().listener.colliders;
            //     foreach (var item in colliders) item.enabled = false;
            //     // entity.AddComponent<RecycleToPool>();

            //     ref var particle =ref World.CreateEntity().AddComponent<CreateParticle>();
            //     particle.Position = zombie.root.transform.position;
            //     particle.Prefab = PrefabFxBood;

            // }
        }
    }
}