namespace GBG.Rush.Utils.Pool {
    using System;
    using Morpeh;
    using UnityEngine;

    [Serializable]
    public struct Poolable : IComponent {
        public EntityProvider originPrefab;
        public Transform      transform;
    }
}