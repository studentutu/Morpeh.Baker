namespace GBG.Rush.Zombies.Scripts
{
    using GBG.Rush.AniInstancing.Scripts;
    // using GBG.Rush.Collisions;
    // using GBG.Rush.Player;
    using GBG.Rush.Utils.Pool;
    // using GBG.Rush.Vehicles;
    using Morpeh;
    using Morpeh.Globals;
    using UnityEngine;
    using UnityEngine.Profiling;

    [CreateAssetMenu(menuName = "ECS/Systems/" + nameof(ZombieMovingSystem))]
    public class ZombieMovingSystem : UpdateSystem
    {
        public float ZombySpeed = 0.5f;
        private Filter player;
        private Filter zombies;

        public override void OnAwake()
        {
            // this.player = this.World.Filter.With<IsPlayer>().With<Vehicle>();
            this.zombies = this.World.Filter.With<Zombie>().Without<DisabledInPool>().Without<StopAnimationMarker>();
        }

        Vector2 positonZombie;
        Vector3 directPlayer;
        public override void OnUpdate(float deltaTime)
        {
            Profiler.BeginSample("ZombieMovingSystem");
            // var posplayer = player.First().GetComponent<Vehicle>().root.position;


            // foreach (var entityZombi in zombies)
            // {

            //     ref var zombieObject = ref entityZombi.GetComponent<Zombie>();
            //     ref var zombi = ref entityZombi.GetComponent<AnimationInstancingComponent>();
            //     directPlayer.x = posplayer.x - zombi.worldMatrix.m03;
            //     directPlayer.y = 0;
            //     directPlayer.z = posplayer.z - zombi.worldMatrix.m23;
            //     directPlayer.Normalize();
            //     zombi.worldMatrix.m03 += ZombySpeed * directPlayer.x * deltaTime;
            //     zombi.worldMatrix.m23 += ZombySpeed * directPlayer.z * deltaTime;
            //     zombi.worldMatrix.m00 = directPlayer.z;
            //     zombi.worldMatrix.m02 = directPlayer.x;
            //     zombi.worldMatrix.m20 = -directPlayer.x;
            //     zombi.worldMatrix.m22 = directPlayer.z;
            //     zombieObject.root.transform.position = zombi.worldMatrix.GetColumn(3);
            // }

            Profiler.EndSample();
        }
    }
}