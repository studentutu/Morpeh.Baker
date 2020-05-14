namespace GBG.Rush.Zombies.Scripts {
    using System;
    using Morpeh;
    using Sirenix.OdinInspector;
    using UnityEngine;


    [Serializable]
    public struct Zombie: IComponent {
        [Required]
        [SceneObjectsOnly]
        public GameObject root;
    }


    public class ZombieProvider : MonoProvider<Zombie> {
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