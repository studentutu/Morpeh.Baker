namespace GBG.Rush.AniInstancing.Scripts
{
    // using GBG.Rush.Collisions;
    using GBG.Rush.Utils.Pool;
    using Morpeh;
    // using Player;
    using UnityEngine;
    // using Vehicles;

    [CreateAssetMenu(menuName = "ECS/Systems/Utils/" + nameof(CheckSpawnerSystem))]
    public class CheckSpawnerSystem : UpdateSystem
    {
        private Filter readySpawners;
        private Filter fullSpawners;
        private Filter player;

        public float spawnSqrRadius;
        public float despawnSqrRadius;
        private bool once = false;

        public override void OnAwake()
        {
            this.readySpawners = this.World.Filter.With<CrowdSpawnerComponent>().Without<SpawnedMarker>().Without<SpawnMarker>();
            this.fullSpawners = this.World.Filter.With<CrowdSpawnerComponent>().With<SpawnedMarker>();
            // this.player = this.World.Filter.With<IsPlayer>().With<Vehicle>();
        }

        public override void OnUpdate(float deltaTime)
        {

            if (this.player.Length == 0) return;

            foreach (var entity in this.readySpawners)
            {
                ref var spawner = ref entity.GetComponent<CrowdSpawnerComponent>();
                // var player = this.player.First().GetComponent<Vehicle>().root;

                if (10 > spawner.transform.position.x && !once
                    // Vector3.SqrMagnitude(player.position - spawner.transform.position) <= this.spawnSqrRadius
                    )
                {
                    once = true;
                    entity.AddComponent<SpawnMarker>();
                }

            }

            // foreach (var entity in this.fullSpawners)
            // {
            //     ref var spawner = ref entity.GetComponent<CrowdSpawnerComponent>();
            //     // var player = this.player.First().GetComponent<Vehicle>().root;

            //     if (player.position.x < spawner.transform.position.x &&
            //         Vector3.SqrMagnitude(player.position - spawner.transform.position) >= this.despawnSqrRadius)
            //     {

            //         //Debug.Log("FREE!!!");
            //         for (int i = 0; i < spawner.countCrowd; i++)
            //         {
            //             spawner.crowd[i].AddComponent<RecycleToPool>();
            //         }
            //         //foreach (var instance in spawner.crowd) {
            //         //    //  instance.AddComponent<FreeAnimationInstance>();
            //         //    //var colliders = instance.GetComponent<ReadyToTrigger>().listener.colliders;
            //         //    //foreach (var item in colliders) item.enabled = false;

            //         //    instance.AddComponent<RecycleToPool>();
            //         //}
            //         spawner.countCrowd = 0;
            //         //spawner.crowd.Clear();

            //         entity.RemoveComponent<SpawnedMarker>();
            //     }
            // }
        }
    }
}