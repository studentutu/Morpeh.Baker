namespace GBG.Rush.AniInstancing.Scripts
{
    using System.Collections.Generic;
    using Morpeh;
    using UnityEngine;
    using UnityEngine.Profiling;

    [CreateAssetMenu(menuName = "ECS/Systems/Utils/" + nameof(AnimationInstancingRenderSystem))]
    public sealed class AnimationInstancingRenderSystem : UpdateSystem
    {
        private bool IsSupported = false;
        public override void OnAwake()
        {
            IsSupported = SystemInfo.supportsInstancing;
        }

        public override void OnUpdate(float deltaTime)
        {
            Profiler.BeginSample("AnimationInstancingRenderSystem");
            this.Render();
            Profiler.EndSample();
        }

        private void Render()
        {
            foreach (var obj in AnimationInstancingDataPool.vertexCachePool)
            {
                VertexCache vertexCache = obj.Value;
                foreach (var block in vertexCache.instanceBlockList)
                {
                    List<InstancingPackage>[] packageList = block.Value.packageList;
                    for (int k = 0; k != packageList.Length; ++k)
                    {
                        for (int i = 0; i != packageList[k].Count; ++i)
                        {
                            InstancingPackage package = packageList[k][i];
                            if (package.instancingCount == 0)
                                continue;
                            for (int j = 0; j != package.subMeshCount; ++j)
                            {
                                InstanceData data = block.Value.instanceData;

                                if (IsSupported)
                                {
#if UNITY_EDITOR
                                    AnimationInstancingDataPool.PreparePackageMaterial(package, vertexCache, k);
#endif
                                    package.propertyBlock.SetFloatArray("frameIndex", data.frameIndex[k][i]);
                                    package.propertyBlock.SetFloatArray("preFrameIndex", data.preFrameIndex[k][i]);
                                    package.propertyBlock.SetFloatArray("transitionProgress", data.transitionProgress[k][i]);
                                    Graphics.DrawMeshInstanced(vertexCache.mesh,
                                        j,
                                        package.material[j],
                                        data.worldMatrix[k][i],
                                        package.instancingCount,
                                        package.propertyBlock,
                                        vertexCache.shadowcastingMode,
                                        vertexCache.receiveShadow,
                                        vertexCache.layer);
                                }
                                else
                                {
                                    package.material[j].SetFloat("frameIndex", data.frameIndex[k][i][0]);
                                    package.material[j].SetFloat("preFrameIndex", data.preFrameIndex[k][i][0]);
                                    package.material[j].SetFloat("transitionProgress", data.transitionProgress[k][i][0]);
                                    Graphics.DrawMesh(vertexCache.mesh,
                                        data.worldMatrix[k][i][0],
                                        package.material[j],
                                        0,
                                        null,
                                        j);
                                }

                            }

                            package.instancingCount = 0;
                        }

                        block.Value.runtimePackageIndex[k] = 0;
                    }
                }
            }
        }
    }
}