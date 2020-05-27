using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrefabLightMapBaker
{

    public partial class PrefabBaker : MonoBehaviour
    {
        public enum LightMapType
        {
            LightColor,
            LightDir,
            LightShadow
        }
        [SerializeField] public LightInfo[] lights;
        [SerializeField] public Renderer[] renderers;
        [SerializeField] public int[] renderersLightmapIndex;
        [SerializeField] public Vector4[] renderersLightmapOffsetScale;
        [SerializeField] public Texture2D[] texturesColor;
        [SerializeField] public Texture2D[] texturesDir;
        [SerializeField] public Texture2D[] texturesShadow;

        private int? lightMapId = null;

        public int GetLightMapHashCode()
        {
            if (lightMapId == null)
            {
                lightMapId = UniqueHashCodeFromLightMaps();
            }
            return lightMapId.Value;
        }

        private int UniqueHashCodeFromLightMaps()
        {
            int prime = 31;
            int hashCode = 1;
            int countMaps = texturesColor.Length + texturesDir.Length + texturesShadow.Length;
            if (countMaps == 0)
            {
                return 0;
            }
            foreach (var item in texturesColor)
            {
                hashCode = prime * hashCode + item.GetHashCode();
            }
            foreach (var item in texturesDir)
            {
                hashCode = prime * hashCode + item.GetHashCode();
            }
            foreach (var item in texturesShadow)
            {
                hashCode = prime * hashCode + item.GetHashCode();
            }
            hashCode = prime * hashCode + (countMaps ^ (countMaps >> 32));
            return hashCode;
        }

        public Texture2D[][] AllTextures() => new Texture2D[][]
        {
            texturesColor, texturesDir, texturesShadow
        };

        public bool HasBakeData => (renderers?.Length ?? 0) > 0 && (
                                                            (texturesColor?.Length ?? 0) > 0 ||
                                                            (texturesDir?.Length ?? 0) > 0 ||
                                                            (texturesShadow?.Length ?? 0) > 0
                                                            );

        public bool BakeApplied
        {
            get
            {
                bool hasColors = RuntimeBakedLightmapUtils.SceneHasAllLightmaps(texturesColor, LightMapType.LightColor);
                bool hasDirs = RuntimeBakedLightmapUtils.SceneHasAllLightmaps(texturesDir, LightMapType.LightDir);
                bool hasShadows = RuntimeBakedLightmapUtils.SceneHasAllLightmaps(texturesShadow, LightMapType.LightShadow);

                return hasColors && hasDirs && hasShadows;
            }
        }

        private bool BakeJustApplied = false;

        public bool RefAdded { get; private set; } = false;
        public void BakeApply()
        {
            if (!HasBakeData)
            {
                BakeJustApplied = false;
                return;
            }

            if (!BakeApplied)
            {
                BakeJustApplied = RuntimeBakedLightmapUtils.InitializeInstance(this);
#if UNITY_EDITOR
                if (BakeJustApplied) Debug.Log("[PrefabBaker] Addeded prefab lightmap data to current scene");
                if (!Application.isPlaying)
                {
                    RuntimeBakedLightmapUtils.UpdateUnityLightMaps();
                }
#endif
            }

            if (!RefAdded)
            {
                RuntimeBakedLightmapUtils.AddInstanceRef(this);
                RefAdded = true;
            }
        }

        private void OnEnable()
        {
            // ActionOnEnable(); // uncomment to use textures right away
            PrefabBaker.PrefabBakerManager.AddInstance(this);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            // ActionOnDisable(); // uncomment to use textures right away
            PrefabBaker.PrefabBakerManager.RemoveInstance(this);
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void ReleaseShaders()
        {
            foreach (var item in renderers)
            {
                if (item != null)
                {
                    ReleaseMaterialShader(item.sharedMaterials);
                }
            }
        }
        // required on each instance
        private static void ReleaseMaterialShader(Material[] materials)
        {
            foreach (var mat in materials)
            {
                if (mat == null) continue;
                var shader = Shader.Find(mat.shader.name);
                if (shader == null) continue;
                mat.shader = shader;
            }
        }
        private void ActionOnEnable()
        {
            BakeApply();
        }

        private void ActionOnDisable()
        {
            if (RuntimeBakedLightmapUtils.RemoveInstance(this))
            {
                BakeJustApplied = false;
            }
            RefAdded = false;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            BakeApply();
        }

#if UNITY_EDITOR
        [ContextMenu("Update textures from Prefab")]
        public void UpdateFromPrefab()
        {
            var mainObjPath = UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(this);
            var getPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<PrefabBaker>(mainObjPath);
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(getPrefab), this);
            lightMapId = UniqueHashCodeFromLightMaps();
        }
#endif
    }
}