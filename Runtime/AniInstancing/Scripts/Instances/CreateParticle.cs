namespace GBG.Rush.Zombies.Scripts {
    using System;
    using Morpeh;
    using Sirenix.OdinInspector;
    using UnityEngine;


    [Serializable]
    public struct CreateParticle : IComponent {
        public ShowParticleProvider Prefab;
        public Vector3 Position;
    }


}