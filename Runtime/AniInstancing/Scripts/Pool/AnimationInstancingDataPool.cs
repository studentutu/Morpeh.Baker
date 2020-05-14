namespace GBG.Rush.AniInstancing.Scripts {
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;

    public static class AnimationInstancingDataPool {
        public static readonly Dictionary<int, VertexCache>  vertexCachePool = new Dictionary<int, VertexCache>();
        public static readonly Dictionary<int, InstanceData> instanceDataPool = new Dictionary<int, InstanceData>();
        public static readonly List<AnimationTexture> animationTextureList = new List<AnimationTexture>();
        
        public const int INSTANCING_SIZE_PER_PACKAGE = 200;
        
        public static void AddMeshVertex(string prefabName,
            LodInfo[] lodInfo,
            Transform[] bones,
            List<Matrix4x4> bindPose,
            int bonePerVertex,
            string alias = null)
        {
            for (var x = 0; x != lodInfo.Length; ++x)
            {
                var lod = lodInfo[x];
                for (var i = 0; i != lod.skinnedMeshRenderer.Length; ++i)
                {
                    var m = lod.skinnedMeshRenderer[i].sharedMesh;
                    if (m == null)
                        continue;

                    var nameCode = lod.skinnedMeshRenderer[i].name.GetHashCode();
                    int identify = GetIdentify(lod.skinnedMeshRenderer[i].sharedMaterials);
                    VertexCache cache = null;
                    if (vertexCachePool.TryGetValue(nameCode, out cache))
                    {
                        MaterialBlock block = null;
                        if (!cache.instanceBlockList.TryGetValue(identify, out block))
                        {
                            block = CreateBlock(cache, lod.skinnedMeshRenderer[i].sharedMaterials);
                            cache.instanceBlockList.Add(identify, block);
                        }
                        lod.vertexCacheList[i] = cache;
                        lod.materialBlockList[i] = block;
                        continue;
                    }

                    VertexCache vertexCache = CreateVertexCache(prefabName, nameCode, 0, m);
                    vertexCache.bindPose = bindPose.ToArray();
                    MaterialBlock matBlock = CreateBlock(vertexCache, lod.skinnedMeshRenderer[i].sharedMaterials);
                    vertexCache.instanceBlockList.Add(identify, matBlock);
                    SetupVertexCache(vertexCache, matBlock, lod.skinnedMeshRenderer[i], bones, bonePerVertex);
                    lod.vertexCacheList[i] = vertexCache;
                    lod.materialBlockList[i] = matBlock;
                }

                for (int i = 0, j = lod.skinnedMeshRenderer.Length; i != lod.meshRenderer.Length; ++i, ++j)
                {
                    Mesh m = lod.meshFilter[i].sharedMesh;
                    if (m == null)
                        continue;

                    int renderName = lod.meshRenderer[i].name.GetHashCode();
                    int aliasName = (alias != null ? alias.GetHashCode() : 0);
                    int identify = GetIdentify(lod.meshRenderer[i].sharedMaterials);
                    VertexCache cache = null;
                    if (vertexCachePool.TryGetValue(renderName + aliasName, out cache))
                    { 
                        MaterialBlock block = null;
                        if (!cache.instanceBlockList.TryGetValue(identify, out block))
                        {
                            block = CreateBlock(cache, lod.meshRenderer[i].sharedMaterials);
                            cache.instanceBlockList.Add(identify, block);
                        }
                        lod.vertexCacheList[j] = cache;
                        lod.materialBlockList[j] = block;
                        continue;
                    }

                    VertexCache vertexCache = CreateVertexCache(prefabName, renderName, aliasName, m);
                    if (bindPose != null)
                        vertexCache.bindPose = bindPose.ToArray();
                    MaterialBlock matBlock = CreateBlock(vertexCache, lod.meshRenderer[i].sharedMaterials);
                    vertexCache.instanceBlockList.Add(identify, matBlock);
                    SetupVertexCache(vertexCache, matBlock, lod.meshRenderer[i], m, bones, bonePerVertex);
                    lod.vertexCacheList[lod.skinnedMeshRenderer.Length + i] = vertexCache;
                    lod.materialBlockList[lod.skinnedMeshRenderer.Length + i] = matBlock;
                }
            }
        }
        
        public static bool ImportAnimationTexture(string prefabName, BinaryReader reader)
        {
            if (FindTexture_internal(prefabName) >= 0)
            {
                return true;
            }

            ReadTexture(reader, prefabName);
            return true;
        }
        
        private static void ReadTexture(BinaryReader reader, string prefabName)
        {
            var format = TextureFormat.RGBAHalf;
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.OpenGLES2)
            {
                //todo
                format = TextureFormat.RGBA32;
            }
            int count       = reader.ReadInt32();
            int blockWidth  = reader.ReadInt32();
            int blockHeight = reader.ReadInt32();

            AnimationTexture aniTexture = new AnimationTexture();
            aniTexture.boneTexture = new Texture2D[count];
            aniTexture.name        = prefabName;
            aniTexture.blockWidth  = blockWidth;
            aniTexture.blockHeight = blockHeight;
            animationTextureList.Add(aniTexture);

            for (int i = 0; i != count; ++i)
            {
                int    textureWidth  = reader.ReadInt32();
                int    textureHeight = reader.ReadInt32();
                int    byteLength    = reader.ReadInt32();
                byte[] b             = new byte[byteLength];
                b = reader.ReadBytes(byteLength);
                Texture2D texture = new Texture2D(textureWidth, textureHeight, format, false);
                texture.LoadRawTextureData(b);
                texture.filterMode = FilterMode.Point;
                texture.Apply();
                aniTexture.boneTexture[i] = texture;
            }
        }

        private static int GetIdentify(Material[] mat)
        {
            var hash = 0;
            for (var i = 0; i != mat.Length; ++i)
            {
                hash += mat[i].name.GetHashCode();
            }
            return hash;
        }
        
        private static MaterialBlock CreateBlock(VertexCache cache, Material[] materials)
        {
            MaterialBlock block        = new MaterialBlock();
            int           packageCount = GetPackageCount(cache);
            block.instanceData = CreateInstanceData(packageCount);                             
            block.packageList  = new List<InstancingPackage>[packageCount];
            for (int i = 0; i != block.packageList.Length; ++i)
            {
                block.packageList[i] = new List<InstancingPackage>();

                InstancingPackage package = CreatePackage(block.instanceData, 
                    cache.mesh,
                    materials, 
                    i);
                block.packageList[i].Add(package);
                PreparePackageMaterial(package, cache, i);
                package.instancingCount = 1;
            }
            block.runtimePackageIndex = new int[packageCount];
            return block;
        }
        
        private static int GetPackageCount(VertexCache vertexCache)
        {
            var packageCount = 1;
            if (vertexCache.boneTextureIndex >= 0)
            {
                AnimationTexture texture = animationTextureList[vertexCache.boneTextureIndex];
                packageCount = texture.boneTexture.Length;
            }
            return packageCount;
        }
        
        public static InstancingPackage CreatePackage(InstanceData data, Mesh mesh, Material[] originalMaterial, int animationIndex)
        {
            var package = new InstancingPackage();
            package.material     = new Material[mesh.subMeshCount];
            package.subMeshCount = mesh.subMeshCount;
            package.size         = 1;
            for (var i = 0; i != mesh.subMeshCount; ++i)
            {
                package.material[i] = new Material(originalMaterial[i]);
                package.material[i].enableInstancing = true; 
                package.material[i].EnableKeyword("INSTANCING_ON");
                package.propertyBlock = new MaterialPropertyBlock();
                package.material[i].EnableKeyword("USE_CONSTANT_BUFFER");
                package.material[i].DisableKeyword("USE_COMPUTE_BUFFER");
            }

            Matrix4x4[] mat                = new Matrix4x4[INSTANCING_SIZE_PER_PACKAGE];
            float[]     frameIndex         = new float[INSTANCING_SIZE_PER_PACKAGE];
            float[]     preFrameIndex      = new float[INSTANCING_SIZE_PER_PACKAGE];
            float[]     transitionProgress = new float[INSTANCING_SIZE_PER_PACKAGE];
            data.worldMatrix[animationIndex].Add(mat);
            data.frameIndex[animationIndex].Add(frameIndex);
            data.preFrameIndex[animationIndex].Add(preFrameIndex);
            data.transitionProgress[animationIndex].Add(transitionProgress);
            return package;
        }
        
        private static VertexCache CreateVertexCache(string prefabName, int renderName, int alias, Mesh mesh)
        {
            VertexCache vertexCache = new VertexCache();
            int cacheName = renderName + alias;
            vertexCachePool[cacheName] = vertexCache;
            vertexCache.nameCode = cacheName;
            vertexCache.mesh = mesh;
            vertexCache.boneTextureIndex = FindTexture_internal(prefabName);
            vertexCache.weight = new Vector4[mesh.vertexCount];
            vertexCache.boneIndex = new Vector4[mesh.vertexCount];
            int packageCount = GetPackageCount(vertexCache);
            InstanceData data = null;
            int instanceName = prefabName.GetHashCode() + alias;
            if (!instanceDataPool.TryGetValue(instanceName, out data))
            {
                data = CreateInstanceData(packageCount);
                instanceDataPool.Add(instanceName, data);
            }
            vertexCache.instanceBlockList = new Dictionary<int, MaterialBlock>();
            return vertexCache;
        }
        private static void SetupVertexCache(VertexCache vertexCache,
            MaterialBlock block,
            SkinnedMeshRenderer render,
            Transform[] boneTransform,
            int bonePerVertex)
        {
            int[] boneIndex = null;
            if (render.bones.Length != boneTransform.Length)
            {
                if (render.bones.Length == 0)
                {
                    boneIndex = new int[1];
                    int hashRenderParentName = render.transform.parent.name.GetHashCode();
                    for (int k = 0; k != boneTransform.Length; ++k)
                    {
                        if (hashRenderParentName == boneTransform[k].name.GetHashCode())
                        {
                            boneIndex[0] = k;
                            break;
                        }
                    }
                }
                else
                {
                    boneIndex = new int[render.bones.Length];
                    for (int j = 0; j != render.bones.Length; ++j)
                    {
                        boneIndex[j] = -1;
                        Transform trans = render.bones[j];
                        int hashTransformName = trans.name.GetHashCode();
                        for (int k = 0; k != boneTransform.Length; ++k)
                        {
                            if (hashTransformName == boneTransform[k].name.GetHashCode())
                            {
                                boneIndex[j] = k;
                                break;
                            }
                        }
                    }

                    if (boneIndex.Length == 0)
                    {
                        boneIndex = null;
                    }
                }
            }

            Mesh m = render.sharedMesh;
            BoneWeight[] boneWeights = m.boneWeights;
            Debug.Assert(boneWeights.Length > 0);
            for (int j = 0; j != m.vertexCount; ++j)
            {
                vertexCache.weight[j].x = boneWeights[j].weight0;
                Debug.Assert(vertexCache.weight[j].x > 0.0f);
                vertexCache.weight[j].y = boneWeights[j].weight1;
                vertexCache.weight[j].z = boneWeights[j].weight2;
                vertexCache.weight[j].w = boneWeights[j].weight3;
                vertexCache.boneIndex[j].x
                    = boneIndex == null ? boneWeights[j].boneIndex0 : boneIndex[boneWeights[j].boneIndex0];
                vertexCache.boneIndex[j].y
                    = boneIndex == null ? boneWeights[j].boneIndex1 : boneIndex[boneWeights[j].boneIndex1];
                vertexCache.boneIndex[j].z
                    = boneIndex == null ? boneWeights[j].boneIndex2 : boneIndex[boneWeights[j].boneIndex2];
                vertexCache.boneIndex[j].w
                    = boneIndex == null ? boneWeights[j].boneIndex3 : boneIndex[boneWeights[j].boneIndex3];
                Debug.Assert(vertexCache.boneIndex[j].x >= 0);
                if (bonePerVertex == 3)
                {
                    float rate = 1.0f / (vertexCache.weight[j].x + vertexCache.weight[j].y + vertexCache.weight[j].z);
                    vertexCache.weight[j].x = vertexCache.weight[j].x * rate;
                    vertexCache.weight[j].y = vertexCache.weight[j].y * rate;
                    vertexCache.weight[j].z = vertexCache.weight[j].z * rate;
                    vertexCache.weight[j].w = -0.1f;
                }
                else if (bonePerVertex == 2)
                {
                    float rate = 1.0f / (vertexCache.weight[j].x + vertexCache.weight[j].y);
                    vertexCache.weight[j].x = vertexCache.weight[j].x * rate;
                    vertexCache.weight[j].y = vertexCache.weight[j].y * rate;
                    vertexCache.weight[j].z = -0.1f;
                    vertexCache.weight[j].w = -0.1f;
                }
                else if (bonePerVertex == 1)
                {
                    vertexCache.weight[j].x = 1.0f;
                    vertexCache.weight[j].y = -0.1f;
                    vertexCache.weight[j].z = -0.1f;
                    vertexCache.weight[j].w = -0.1f;
                }
            }

            if (vertexCache.materials == null)
                vertexCache.materials = render.sharedMaterials;
            SetupAdditionalData(vertexCache);
            for (int i = 0; i != block.packageList.Length; ++i)
            {
                InstancingPackage package = CreatePackage(block.instanceData, vertexCache.mesh, render.sharedMaterials, i);
                block.packageList[i].Add(package);
                //vertexCache.packageList[i].Add(package);
                PreparePackageMaterial(package, vertexCache, i);
            }
        }


        private static void SetupVertexCache(VertexCache vertexCache,
            MaterialBlock block,
            MeshRenderer render,
            Mesh mesh,
            Transform[] boneTransform,
            int bonePerVertex)
        {
            int boneIndex = -1;
            if (boneTransform != null)
            {
                for (int k = 0; k != boneTransform.Length; ++k)
                {
                    if (render.transform.parent.name.GetHashCode() == boneTransform[k].name.GetHashCode())
                    {
                        boneIndex = k;
                        break;
                    }
                }
            }
            if (vertexCache.materials == null)
                vertexCache.materials = render.sharedMaterials;
            SetupAdditionalData(vertexCache);
            for (int i = 0; i != block.packageList.Length; ++i)
            {
                InstancingPackage package = CreatePackage(block.instanceData, vertexCache.mesh, render.sharedMaterials, i);
                block.packageList[i].Add(package);
                PreparePackageMaterial(package, vertexCache, i);
            }
        }
        
        private static void SetupAdditionalData(VertexCache vertexCache)
        {
            var colors = new Color[vertexCache.weight.Length];            
            for (int i = 0; i != colors.Length; ++i)
            {
                colors[i].r = vertexCache.weight[i].x;
                colors[i].g = vertexCache.weight[i].y;
                colors[i].b = vertexCache.weight[i].z;
                colors[i].a = vertexCache.weight[i].w;
            }
            vertexCache.mesh.colors = colors;

            List<Vector4> uv2 = new List<Vector4>(vertexCache.boneIndex.Length);
            for (int i = 0; i != vertexCache.boneIndex.Length; ++i)
            {
                uv2.Add(vertexCache.boneIndex[i]);
            }
            vertexCache.mesh.SetUVs(2, uv2);
            vertexCache.mesh.UploadMeshData(false);
        }

        public static void PreparePackageMaterial(InstancingPackage package, VertexCache vertexCache, int aniTextureIndex)
        {
            if (vertexCache.boneTextureIndex < 0)
                return;
                
            for (int i = 0; i != package.subMeshCount; ++i)
            {
                AnimationTexture texture = animationTextureList[vertexCache.boneTextureIndex];
                package.material[i].SetTexture("_boneTexture", texture.boneTexture[aniTextureIndex]);
                package.material[i].SetInt("_boneTextureWidth", texture.boneTexture[aniTextureIndex].width);
                package.material[i].SetInt("_boneTextureHeight", texture.boneTexture[aniTextureIndex].height);
                package.material[i].SetInt("_boneTextureBlockWidth", texture.blockWidth);
                package.material[i].SetInt("_boneTextureBlockHeight", texture.blockHeight);
            }
        }
        
        private static InstanceData CreateInstanceData(int packageCount)
        {
            var data = new InstanceData();
            data.worldMatrix        = new List<Matrix4x4[]>[packageCount];
            data.frameIndex         = new List<float[]>[packageCount];
            data.preFrameIndex      = new List<float[]>[packageCount];
            data.transitionProgress = new List<float[]>[packageCount];
            for (int i = 0; i != packageCount; ++i)
            {
                data.worldMatrix[i]        = new List<Matrix4x4[]>();
                data.frameIndex[i]         = new List<float[]>();
                data.preFrameIndex[i]      = new List<float[]>();
                data.transitionProgress[i] = new List<float[]>();
            }   
            return data;    
        }
        
        private static int FindTexture_internal(string name)
        {
            for (int i = 0; i != animationTextureList.Count; ++i)
            {
                var texture = animationTextureList[i];
                if (texture.name == name)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}