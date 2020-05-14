namespace GBG.Rush.AniInstancing.Scripts
{
    using System;
    using System.Collections.Generic;
    using Morpeh;
    using UnityEngine;
    using UnityEngine.PlayerLoop;
    using UnityEngine.Rendering;
    using Random = UnityEngine.Random;

    [Serializable]
    public struct AnimationInstancingComponent : IComponent
    {
        public bool isInitialized;
        public Matrix4x4 worldMatrix;
        public GameObject prototype;
        public TextAsset animationData;

        public float playSpeed;
        public ShadowCastingMode shadowCastingMode;
        public bool receiveShadow;
        public int layer;

        public float speedParameter;
        public WrapMode wrapMode;

        public int bonePerVertex;
        public float curFrame;
        public float preAniFrame;
        public int aniIndex;
        public int preAniIndex;
        public int aniTextureIndex;
        public float transitionDuration;
        public bool isInTransition;
        public float transitionTimer;
        public float transitionProgress;

        public List<AnimationInfo> aniInfo;
        private ComparerHash comparer;
        private AnimationInfo searchInfo;

        public LodInfo[] lodInfo;
        public Transform[] allTransforms;
        internal GameObject go;

        public bool IsPause() => this.speedParameter == 0.0f;
        public bool IsLoop() => this.wrapMode == WrapMode.Loop;

        public void UpdateTransform(Matrix4x4 world)
        {
            this.worldMatrix = world;
        }

        public AnimationInstancingComponent(Matrix4x4 world, GameObject prototype, TextAsset animationData) : this()
        {
            this.isInitialized = true;
            this.playSpeed = 1.0f;
            this.speedParameter = 1.0f;
            this.aniIndex = -1;
            this.preAniIndex = -1;
            this.aniTextureIndex = -1;
            this.transitionDuration = 0.0f;
            this.isInTransition = false;
            this.transitionTimer = 0.0f;
            this.transitionProgress = 0.0f;

            this.animationData = animationData;
            this.worldMatrix = world;
            this.prototype = prototype;
            this.layer = prototype.gameObject.layer;
            this.bonePerVertex = 2;

            this.lodInfo = new LodInfo[1];
            var info = new LodInfo();
            info.lodLevel = 0;
            info.skinnedMeshRenderer = this.prototype.GetComponentsInChildren<SkinnedMeshRenderer>();
            info.meshRenderer = this.prototype.GetComponentsInChildren<MeshRenderer>();
            info.meshFilter = this.prototype.GetComponentsInChildren<MeshFilter>();
            info.vertexCacheList = new VertexCache[info.skinnedMeshRenderer.Length + info.meshRenderer.Length];
            info.materialBlockList = new MaterialBlock[info.vertexCacheList.Length];
            this.lodInfo[0] = info;

            this.searchInfo = new AnimationInfo();
            this.comparer = new ComparerHash();
            var animationInfo = AnimationInfoReader.FindAnimationInfo(prototype, this);
            this.aniInfo = animationInfo.listAniInfo;
            this.Prepare(this.aniInfo, animationInfo.extraBoneInfo);
        }

        private void Prepare(List<AnimationInfo> infoList, ExtraBoneInfo extraBoneInfo)
        {
            this.aniInfo = infoList;

            var bindPose = new List<Matrix4x4>(150);

            // to optimize, MergeBone don't need to call every time
            var bones = RuntimeHelper.MergeBone(this.lodInfo[0].skinnedMeshRenderer, bindPose);
            this.allTransforms = bones;

            if (extraBoneInfo != null)
            {
                var list = new List<Transform>();
                list.AddRange(bones);
                var transforms = this.prototype.gameObject.GetComponentsInChildren<Transform>();
                for (int i = 0; i != extraBoneInfo.extraBone.Length; ++i)
                {
                    for (int j = 0; j != transforms.Length; ++j)
                    {
                        if (extraBoneInfo.extraBone[i] == transforms[j].name)
                        {
                            list.Add(transforms[j]);
                        }
                    }
                    bindPose.Add(extraBoneInfo.extraBindPose[i]);
                }

                this.allTransforms = list.ToArray();
            }

            AnimationInstancingDataPool.AddMeshVertex(this.prototype.name, this.lodInfo, this.allTransforms,
                bindPose, this.bonePerVertex);

            foreach (var lod in this.lodInfo)
            {
                foreach (var cache in lod.vertexCacheList)
                {
                    cache.shadowcastingMode = this.shadowCastingMode;
                    cache.receiveShadow = this.receiveShadow;
                    cache.layer = this.layer;
                }
            }

            this.PlayAnimation(0);
        }

        public void PlayAnimation(string name)
        {
            var hash = name.GetHashCode();
            var index = this.FindAnimationInfo(hash);
            this.PlayAnimation(index);
        }

        public void UpdateAnimation()
        {
            if (this.aniInfo == null || this.IsPause())
                return;

            var weight = this.transitionTimer / this.transitionDuration;
            if (this.isInTransition)
            {
                this.transitionTimer += Time.deltaTime;
                this.transitionProgress = Mathf.Min(weight, 1.0f);
                if (this.transitionProgress >= 1.0f)
                {
                    this.isInTransition = false;
                    this.preAniIndex = -1;
                    this.preAniFrame = -1;
                }
            }
            var speed = this.playSpeed * this.speedParameter;
            this.curFrame += speed * Time.deltaTime * this.aniInfo[this.aniIndex].fps;
            var totalFrame = this.aniInfo[this.aniIndex].totalFrame;
            switch (this.wrapMode)
            {
                case WrapMode.Loop:
                    {
                        if (this.curFrame < 0f)
                            this.curFrame += (totalFrame - 1);
                        else if (this.curFrame > totalFrame - 1)
                            this.curFrame -= (totalFrame - 1);
                        break;
                    }
                case WrapMode.PingPong:
                    {
                        if (this.curFrame < 0f)
                        {
                            this.speedParameter = Mathf.Abs(this.speedParameter);
                            this.curFrame = Mathf.Abs(this.curFrame);
                        }
                        else if (this.curFrame > totalFrame - 1)
                        {
                            this.speedParameter = -Mathf.Abs(this.speedParameter);
                            this.curFrame = 2 * (totalFrame - 1) - this.curFrame;
                        }
                        break;
                    }
                case WrapMode.Default:
                case WrapMode.Once:
                    {
                        if (this.curFrame < 0f || this.curFrame > totalFrame - 1.0f)
                        {
                        }
                        break;
                    }
            }

            this.curFrame = Mathf.Clamp(this.curFrame, 0f, totalFrame - 1);
        }

        private void PlayAnimation(int animationIndex)
        {
            if (animationIndex == this.aniIndex && !this.IsPause())
            {
                return;
            }

            this.transitionDuration = 0.0f;
            this.transitionProgress = 1.0f;
            this.isInTransition = false;

            if (0 <= animationIndex && animationIndex < this.aniInfo.Count)
            {
                this.preAniIndex = this.aniIndex;
                this.aniIndex = animationIndex;
                this.preAniFrame = (float)(int)(curFrame + 0.5f);
                this.curFrame = Random.Range(0.0f, 60.0f);
                this.aniTextureIndex = this.aniInfo[this.aniIndex].textureIndex;
                this.wrapMode = this.aniInfo[this.aniIndex].wrapMode;
                this.speedParameter = 1.0f;
            }
            else
            {
                Debug.LogWarning("The requested animation index is out of the count.");
                return;
            }
        }

        private int FindAnimationInfo(int hash)
        {
            this.searchInfo.animationNameHash = hash;
            return this.aniInfo.BinarySearch(this.searchInfo, this.comparer);
        }

        public int GetAnimationCount() => this.aniInfo?.Count ?? 0;
    }
    [Serializable]
    public class AnimationInstancingProvider : MonoProvider<AnimationInstancingComponent> { }
}