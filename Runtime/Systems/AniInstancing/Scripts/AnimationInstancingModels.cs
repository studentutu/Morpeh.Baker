namespace GBG.Rush.AniInstancing.Scripts {
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Rendering;

    [Serializable]
    public struct LodInfo {
        public int                                    lodLevel;
        public SkinnedMeshRenderer[]                  skinnedMeshRenderer;
        public MeshRenderer[]                         meshRenderer;
        public MeshFilter[]                           meshFilter;
        public VertexCache[]   vertexCacheList;
        public MaterialBlock[] materialBlockList;
    }
    
    [Serializable]
    public class InstanceData
    {
        public List<Matrix4x4[]>[] worldMatrix;
        public List<float[]>[]     frameIndex;
        public List<float[]>[]     preFrameIndex;
        public List<float[]>[]     transitionProgress;
    }

    [Serializable]
    public class InstancingPackage
    {
        public Material[]            material;
        public int                   animationTextureIndex = 0;
        public int                   subMeshCount = 1;
        public int                   instancingCount;
        public int                   size;
        public MaterialPropertyBlock propertyBlock;
    }
    
    [Serializable]
    public class MaterialBlock
    {
        public InstanceData instanceData;
        public int[]        runtimePackageIndex;
        // array[index base on texture][package index]
        public List<InstancingPackage>[] packageList;
    }

    [Serializable]
    public class VertexCache
    {
        public int                            nameCode;
        public Mesh                           mesh;
        public Dictionary<int, MaterialBlock> instanceBlockList;
        public Vector4[]                      weight;
        public Vector4[]                      boneIndex;
        public Material[]                     materials;
        public Matrix4x4[]                    bindPose;
        public Transform[]                    bonePose;
        public int                            boneTextureIndex = -1;

        // these are temporary, should be moved to InstancingPackage
        public ShadowCastingMode shadowcastingMode;
        public bool              receiveShadow;
        public int               layer;
    }

    [Serializable]
    public class AnimationTexture
    {
        public string      name        { get; set; }
        public Texture2D[] boneTexture { get; set; }
        public int         blockWidth  { get; set; }
        public int         blockHeight { get; set; }
    }
    
    [Serializable]
    public struct AnimationInfo
    {
        public string               animationName;
        public int                  animationNameHash;
        public int                  totalFrame;
        public int                  fps;
        public int                  animationIndex;
        public int                  textureIndex;
        public bool                 rootMotion;
        public WrapMode             wrapMode;
        public Vector3[]            velocity;
        public Vector3[]            angularVelocity;
        public List<AnimationEvent> eventList; 
    }

    [Serializable]
    public class ExtraBoneInfo
    {
        public string[]    extraBone;
        public Matrix4x4[] extraBindPose;
    }
    
    [Serializable]
    public struct InstanceAnimationInfo 
    {
        public List<AnimationInfo> listAniInfo;
        public ExtraBoneInfo       extraBoneInfo;
    }


    public class ComparerHash : IComparer<AnimationInfo>
    {
        public int Compare(AnimationInfo x, AnimationInfo y)
        {
            return x.animationNameHash.CompareTo(y.animationNameHash);
        }
    }
}