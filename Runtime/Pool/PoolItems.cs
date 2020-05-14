namespace GBG.Rush.Utils.Pool {
    using System;
    using System.Collections.Generic;
    using Morpeh;
    using UnityEngine;

    [Serializable]
    public struct PoolItems : IComponent {
        public Dictionary<EntityProvider, Stack<IEntity>> items;
    }

    public static class PoolItemsExtensions {
        public static IEntity GetInstance(this in PoolItems items, EntityProvider prefab) {
            if (items.items.TryGetValue(prefab, out var pool) && pool.Count > 0) {
                return pool.Pop();
            }

            return PoolSystem.InstantiatePoolableEntity(prefab, Vector3.zero, Quaternion.identity);
        }
    }
}