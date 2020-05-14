namespace GBG.Rush.AniInstancing.Scripts {
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public static class SpawneHelper  {
        public static List<Vector3> DoWhileInRange( Transform transform, Vector2 spawnRange, Vector2 offsetStep) {
            var result = new List<Vector3>();
            if(offsetStep.x == 0 || offsetStep.y == 0) return result;
            
            var position = transform.position;
            //var angles   = transform.localRotation;
            
            var startXPos  = position.x - spawnRange.x;
            var targetXPos = position.x + spawnRange.x;
            
            var startZPos  = position.z - spawnRange.y;
            var targetZPos = position.z + spawnRange.y;

            var amount = 0;

            var xpos = startXPos;
            while (xpos <= targetXPos) {
                var zpos = startZPos;
                while (zpos <= targetZPos) {
                    var spawnPosition = new Vector3(xpos, position.y, zpos);

                    result.Add(spawnPosition);
                    
                    zpos += offsetStep.y;
                    amount++;
                }

                xpos += offsetStep.x;
            }

            return result;
        }
    }
}
