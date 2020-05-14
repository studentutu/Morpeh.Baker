namespace GBG.Rush.Zombies.Scripts {
    using System;
    using System.Collections.Generic;
    using Morpeh;
    using Sirenix.OdinInspector;
    using UnityEngine;


    [Serializable]
    public struct ShowParticle : IComponent {
        [Required]
        [SceneObjectsOnly]
        public GameObject root;
        public List<ParticleSystem> Particles;
        public float ShowTime;
        [HideInInspector]
        public float Time;
    }
   

    public class ShowParticleProvider : MonoProvider<ShowParticle> {
        private void OnValidate() {
            ref var data = ref this.GetData();
            if (data.root == null) {
                data.root = this.gameObject;
            }

            if (data.root == null) {
                Debug.LogError($"{data.root} must have {nameof(GameObject)}");
            }
        }
    }

}