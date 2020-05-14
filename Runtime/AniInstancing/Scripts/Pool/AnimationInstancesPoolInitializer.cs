namespace GBG.Rush.AniInstancing.Scripts
{
    using Morpeh;
    using UnityEngine;
    using UnityEngine.Assertions;
    using GBG.Rush.Zombies.Scripts;
    using GBG.Rush.Utils.Pool;
    using System.Collections.Generic;

    [CreateAssetMenu(menuName = "ECS/Initializers/Utils/" + nameof(AnimationInstancesPoolInitializer))]
    public class AnimationInstancesPoolInitializer : Initializer
    {
        public int capacity;

        public ZombieProvider prefabZombie;
        public GameObject prefabAnimation;
        public TextAsset animationData;
        private Filter pools;
        public override void OnAwake()
        {
            Assert.IsNotNull(this.prefabZombie);
            Assert.IsNotNull(this.prefabAnimation);
            Assert.IsNotNull(this.animationData);


            this.World.CreateEntity().SetComponent(new PoolItems
            {
                items = new Dictionary<EntityProvider, Stack<IEntity>>()
            });

            // this.pools = this.World.Filter.With<PoolItems>();
            // ref var poolItems = ref this.pools.First().GetComponent<PoolItems>();

            // var position = new Vector3(-1000, -1000, -1000);
            // var baze = new GameObject("Zombies");
            // for (var i = 0; i < this.capacity; ++i)
            // {

            //     var entity = poolItems.GetInstance(prefabZombie);
            //     ref var zombie = ref entity.GetComponent<Zombie>();

            //     zombie.root.transform.parent = baze.transform;

            //     entity.SetComponent(new AnimationInstancingComponent(
            //         Matrix4x4.TRS(position, Quaternion.Euler(Vector3.zero), Vector3.one), this.prefabAnimation, this.animationData));
            //     entity.AddComponent<RecycleToPool>();
            // }
        }

        public override void Dispose()
        {
        }
    }
}