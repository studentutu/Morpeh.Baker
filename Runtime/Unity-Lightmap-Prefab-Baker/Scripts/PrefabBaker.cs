using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrefabLightMapBaker
{
    public class PrefabBaker : MonoBehaviour
    {
        [SerializeField] public LightInfo[] lights;
        [SerializeField] public Renderer[] renderers;
        [SerializeField] public int[] renderersLightmapIndex;
        [SerializeField] public Vector4[] renderersLightmapOffsetScale;
        [SerializeField] public Texture2D[] texturesColor;
        [SerializeField] public Texture2D[] texturesDir;
        [SerializeField] public Texture2D[] texturesShadow;

        [SerializeField]
        private string nameOfOriginalPrefab = null;

        public string GetLightMapHasCode()
        {
            if (string.IsNullOrEmpty(nameOfOriginalPrefab))
            {
                nameOfOriginalPrefab = UniqueHashCodeFromLightMaps();
            }
            return nameOfOriginalPrefab;
        }

        private string UniqueHashCodeFromLightMaps()
        {
            string hashCode = "";
            foreach (var item in texturesColor)
            {
                hashCode += item.GetHashCode();
            }
            foreach (var item in texturesDir)
            {
                hashCode += item.GetHashCode();
            }
            foreach (var item in texturesShadow)
            {
                hashCode += item.GetHashCode();
            }
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
                bool hasColors = RuntimeBakedLightmapUtils.SceneHasAllLightmaps(texturesColor);
                bool hasDirs = RuntimeBakedLightmapUtils.SceneHasAllLightmaps(texturesDir);
                bool hasShadows = RuntimeBakedLightmapUtils.SceneHasAllLightmaps(texturesShadow);

                return hasColors && hasDirs && hasShadows;
            }
        }

        private bool BakeJustApplied { set; get; } = false;

        public void BakeApply(bool addReference)
        {
            if (!HasBakeData)
            {
                BakeJustApplied = false;
                return;
            }

            if (!BakeApplied)
            {
                BakeJustApplied = RuntimeBakedLightmapUtils.AddInstance(this);
#if UNITY_EDITOR
                if (BakeJustApplied) Debug.Log("[PrefabBaker] Addeded prefab lightmap data to current scene");
#endif
            }

            if (addReference)
            {
                RuntimeBakedLightmapUtils.AddInstanceRef(this);
            }
        }

        private void OnEnable()
        {
            BakeApply(true);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            BakeApply(false);
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (RuntimeBakedLightmapUtils.RemoveInstance(this))
            {
                BakeJustApplied = false;
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Update textures from Prefab")]
        public void UpdateFromPrefab()
        {
            var mainObjPath = UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(this);
            var getPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<PrefabBaker>(mainObjPath);
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(getPrefab), this);
            nameOfOriginalPrefab = UniqueHashCodeFromLightMaps();
        }

#endif
    }
}