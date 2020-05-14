namespace GBG.Rush.AniInstancing.Scripts {
    using System;
    using System.Collections.Generic;
    using Morpeh;
    using UnityEditor;
    using UnityEngine;

    [Serializable]
    public struct CrowdSpawnerComponent : IComponent {
        public Vector2 spawnRange;
        public Vector2 offsetStep;
        public Vector3 angles;
        public Vector3 anglesRange;

        public Transform transform;

        [HideInInspector]
        public IEntity[] crowd;
        [HideInInspector]
        public int countCrowd;

    }
    
    [Serializable]
    public class CrowdSpawnerProvider : MonoProvider<CrowdSpawnerComponent> {
        private void Awake() {
            this.GetData().crowd = new IEntity[100]; //new List<IEntity>();
        }

        private void OnDrawGizmos() {
            var position = this.transform.position;
            
            Gizmos.color = Color.green;
            
            Gizmos.DrawLine(new Vector3(position.x - this.GetData().spawnRange.x, position.y, position.z - this.GetData().spawnRange.y), 
                new Vector3(position.x - this.GetData().spawnRange.x, position.y, position.z + this.GetData().spawnRange.y));
            
            Gizmos.DrawLine(new Vector3(position.x - this.GetData().spawnRange.x, position.y, position.z + this.GetData().spawnRange.y), 
                new Vector3(position.x + this.GetData().spawnRange.x, position.y, position.z + this.GetData().spawnRange.y));
            
            Gizmos.DrawLine(new Vector3(position.x + this.GetData().spawnRange.x, position.y, position.z + this.GetData().spawnRange.y), 
                new Vector3(position.x + this.GetData().spawnRange.x, position.y, position.z - this.GetData().spawnRange.y));
            
            Gizmos.DrawLine(new Vector3(position.x + this.GetData().spawnRange.x, position.y, position.z - this.GetData().spawnRange.y), 
                new Vector3(position.x - this.GetData().spawnRange.x, position.y, position.z - this.GetData().spawnRange.y));

            var positions = SpawneHelper.DoWhileInRange(this.transform, this.GetData().spawnRange, this.GetData().offsetStep);
            foreach (var item in positions) {
                DrawZombieSphere(item);
            }
            var amount = positions.Count;

#if UNITY_EDITOR
            Handles.Label(this.transform.position + new Vector3(0, 2, 0), amount.ToString());
#endif
        }
        
        private static void DrawZombieSphere(Vector3 position) {
            Gizmos.DrawSphere(position, 0.05f);
        }
    }
}