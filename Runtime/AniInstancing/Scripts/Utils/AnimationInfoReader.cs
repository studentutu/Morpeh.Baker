namespace GBG.Rush.AniInstancing.Scripts {
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;

    public static class AnimationInfoReader {
        private static readonly Dictionary<GameObject, InstanceAnimationInfo> AnimationInfo = new Dictionary<GameObject, InstanceAnimationInfo>();
        
        public static InstanceAnimationInfo FindAnimationInfo(GameObject prefab, AnimationInstancingComponent instance)
        {
            Debug.Assert(prefab != null);
            return AnimationInfo.TryGetValue(prefab, out var info) ? info : LoadAnimationInfoFromPrototype(instance);
        }
        
        private static InstanceAnimationInfo LoadAnimationInfoFromPrototype(AnimationInstancingComponent animInstance)
        {
            var info = new InstanceAnimationInfo();
            var asset = animInstance.animationData;
            var reader = new BinaryReader(new MemoryStream(asset.bytes));
            
            info.listAniInfo = ReadAnimationInfo(reader);
            info.extraBoneInfo = ReadExtraBoneInfo(reader);
            
            AnimationInstancingDataPool.ImportAnimationTexture(animInstance.prototype.name, reader);
            AnimationInfo.Add(animInstance.prototype, info);
            
            return info;
        }
        
        private static List<AnimationInfo> ReadAnimationInfo(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            List<AnimationInfo>  listInfo = new List<AnimationInfo>();
            for (int i = 0; i != count; ++i)
            {
                AnimationInfo info = new AnimationInfo();
                //info.animationNameHash = reader.ReadInt32();
                info.animationName = reader.ReadString();
                info.animationNameHash = info.animationName.GetHashCode();
                info.animationIndex = reader.ReadInt32();
                info.textureIndex = reader.ReadInt32();
                info.totalFrame = reader.ReadInt32();
                info.fps = reader.ReadInt32();
                info.rootMotion = reader.ReadBoolean();
                info.wrapMode = (WrapMode)reader.ReadInt32();
                if (info.rootMotion)
                {
                    info.velocity = new Vector3[info.totalFrame];
                    info.angularVelocity = new Vector3[info.totalFrame];
                    for (int j = 0; j != info.totalFrame; ++j)
                    {
                        info.velocity[j].x = reader.ReadSingle();
                        info.velocity[j].y = reader.ReadSingle();
                        info.velocity[j].z = reader.ReadSingle();

                        info.angularVelocity[j].x = reader.ReadSingle();
                        info.angularVelocity[j].y = reader.ReadSingle();
                        info.angularVelocity[j].z = reader.ReadSingle();
                    }
                }
                int evtCount = reader.ReadInt32();
//                info.eventList = new List<AnimationEvent>();
//                for (int j = 0; j != evtCount; ++j)
//                {
//                    AnimationEvent evt = new AnimationEvent();
//                    evt.function        = reader.ReadString();
//                    evt.floatParameter  = reader.ReadSingle();
//                    evt.intParameter    = reader.ReadInt32();
//                    evt.stringParameter = reader.ReadString();
//                    evt.time            = reader.ReadSingle();
//                    evt.objectParameter = reader.ReadString();
//                    info.eventList.Add(evt);
//                }
                info.eventList = new List<AnimationEvent>();
                listInfo.Add(info);
            }
            listInfo.Sort(new ComparerHash());
            return listInfo;
        }

        private static ExtraBoneInfo ReadExtraBoneInfo(BinaryReader reader)
        {
            ExtraBoneInfo info = default;
            if (!reader.ReadBoolean()) {
                return info;
            }

            info = new ExtraBoneInfo();
            var count = reader.ReadInt32();
            info.extraBone     = new string[count];
            info.extraBindPose = new Matrix4x4[count];
            for (int i = 0; i != info.extraBone.Length; ++i)
            {
                info.extraBone[i] = reader.ReadString();
            }
            for (int i = 0; i != info.extraBindPose.Length; ++i)
            {
                for (int j = 0; j != 16; ++j)
                {
                    info.extraBindPose[i][j] = reader.ReadSingle();
                }
            }
            return info;
        }
    }
}