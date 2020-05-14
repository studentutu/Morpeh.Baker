namespace GBG.Rush.AniInstancing.Scripts {
    using GBG.Rush.Utils.Pool;
    using Morpeh;
    using UnityEngine;
    using UnityEngine.Profiling;

    [CreateAssetMenu(menuName = "ECS/Systems/Utils/" + nameof(AnimationInstancingProcessSystem))]
    public sealed class AnimationInstancingProcessSystem : UpdateSystem {

        private Filter animationInstances;
        
        public override void OnAwake() {
            this.animationInstances = this.World.Filter.With<AnimationInstancingComponent>().Without<DisabledInPool>().Without<StopAnimationMarker>();
        }

        public override void OnUpdate(float deltaTime) {
#if UNITY_EDITOR
            //Debug.Log("Process: " + this.animationInstances.Length);
            Profiler.BeginSample("AnimationInstancingProcessSystem");     
#endif
            this.ApplyBoneMatrix();

#if UNITY_EDITOR
            Profiler.EndSample();  
#endif
        }
        
        private void ApplyBoneMatrix()
        {
            foreach (var entity in this.animationInstances) {
                ref var instance = ref entity.GetComponent<AnimationInstancingComponent>();

                instance.UpdateAnimation();

                var lod = instance.lodInfo[0];
                var aniTextureIndex = instance.aniTextureIndex;

                for (var j = 0; j != lod.vertexCacheList.Length; ++j)
                {
                    var cache = lod.vertexCacheList[j];
                    var block = lod.materialBlockList[j];
                    var packageIndex = block.runtimePackageIndex[aniTextureIndex];
                    var package = block.packageList[aniTextureIndex][packageIndex];
                    
                    if (package.instancingCount + 1 > AnimationInstancingDataPool.INSTANCING_SIZE_PER_PACKAGE)
                    {
                        ++block.runtimePackageIndex[aniTextureIndex];
                        packageIndex = block.runtimePackageIndex[aniTextureIndex];
                        if (packageIndex >= block.packageList[aniTextureIndex].Count)
                        {
                            InstancingPackage newPackage = AnimationInstancingDataPool.CreatePackage(block.instanceData,
                                cache.mesh,
                                cache.materials,
                                aniTextureIndex);
                            block.packageList[aniTextureIndex].Add(newPackage);
                            AnimationInstancingDataPool.PreparePackageMaterial(newPackage, cache, aniTextureIndex);
                            newPackage.instancingCount = 1;
                        }
                        block.packageList[aniTextureIndex][packageIndex].instancingCount = 1;
                    }
                    else
                        ++package.instancingCount;

                    {
                        VertexCache vertexCache = cache;
                        InstanceData data = block.instanceData;
                        int index = block.runtimePackageIndex[aniTextureIndex];
                        InstancingPackage pkg = block.packageList[aniTextureIndex][index];
                        int count = pkg.instancingCount - 1;
                        if (count >= 0) {
                            ref Matrix4x4 worldMat = ref instance.worldMatrix;
                            Matrix4x4[] arrayMat = data.worldMatrix[aniTextureIndex][index];
                            // убеем ненужное копирование так как вращаем только по y и scale  = 1
                            arrayMat[count].m00 = worldMat.m00;
                            //arrayMat[count].m01 = worldMat.m01;
                            arrayMat[count].m02 = worldMat.m02;
                            arrayMat[count].m03 = worldMat.m03;
                            //arrayMat[count].m10 = worldMat.m10;
                            arrayMat[count].m11 = worldMat.m11;
                            //arrayMat[count].m12 = worldMat.m12;
                            arrayMat[count].m13 = worldMat.m13;
                            arrayMat[count].m20 = worldMat.m20;
                            //arrayMat[count].m21 = worldMat.m21;
                            arrayMat[count].m22 = worldMat.m22;
                            arrayMat[count].m23 = worldMat.m23;
                            //arrayMat[count].m30 = worldMat.m30;
                            //arrayMat[count].m31 = worldMat.m31;
                            //arrayMat[count].m32 = worldMat.m32;
                            arrayMat[count].m33 = worldMat.m33;
                            float frameIndex = 0, preFrameIndex = -1, transition = 0f;

                            frameIndex = instance.aniInfo[instance.aniIndex].animationIndex + instance.curFrame;
                            if (instance.preAniIndex >= 0)
                                preFrameIndex = instance.aniInfo[instance.preAniIndex].animationIndex + instance.preAniFrame;
                            transition = instance.transitionProgress;
                            
                            data.frameIndex[aniTextureIndex][index][count] = frameIndex;
                            data.preFrameIndex[aniTextureIndex][index][count] = preFrameIndex;
                            data.transitionProgress[aniTextureIndex][index][count] = transition;
                        }
                    }
                }
            }
        }
    }
}