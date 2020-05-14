namespace GBG.Rush.Utils.Pool {
    using System;
    using Morpeh;

    [Serializable]
    public struct WarmPoolEvent : IComponent {
        public EntityProvider[] prefabs;
        public int[]            counts;
    }
}