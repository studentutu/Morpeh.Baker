using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Dao.ConcurrentDictionaryLazy;

namespace PrefabLightMapBaker
{

    public class PrefabBaker : MonoBehaviour
    {
        public class PrefabBakerManager : MonoBehaviour
        {
            private static Coroutine toRun = null;
            private static PrefabBakerManager manager = null;
            private static ConcurrentDictionaryLazy<PrefabBaker, bool> AddOrRemove = new ConcurrentDictionaryLazy<PrefabBaker, bool>(50);

            public static void AddInstance(PrefabBaker instance)
            {
                if (!AddOrRemove.TryGetValue(instance, out _))
                {
                    AddOrRemove.TryAdd(instance, true);
                }
                AddOrRemove[instance] = true;
                RunCoroutine();
            }

            public static void RemoveInstance(PrefabBaker instance)
            {
                if (!AddOrRemove.TryGetValue(instance, out _))
                {
                    AddOrRemove.TryAdd(instance, false);
                }
                AddOrRemove[instance] = false;
                RunCoroutine();
            }

            private static void RunCoroutine()
            {
                if (manager == null)
                {
                    var go = new GameObject();
                    manager = go.AddComponent<PrefabBakerManager>();
                    go.isStatic = true;
                    go.hideFlags = HideFlags.HideAndDontSave;
                    GameObject.DontDestroyOnLoad(go);
                }

                if (toRun == null)
                {
                    toRun = manager.StartCoroutine(WorkingCoroutine());
                }
            }

            private static IEnumerator WorkingCoroutine()
            {
                int count = 0;
#if UNITY_EDITOR
                UnityEngine.Profiling.Profiler.BeginSample("BakingApply");
#endif
                // Lazy loaded enumerated
                foreach (var item in AddOrRemove)
                {
                    if (item.Key != null)
                    {
                        count++;
                        if (item.Value)
                        {
                            item.Key.ActionOnEnable();
                        }
                        else
                        {
                            item.Key.ActionOnDisable();
                        }
                        if (count % 4 == 0)
                        {
                            yield return null;
                        }
                    }
                }

#if UNITY_EDITOR
                UnityEngine.Profiling.Profiler.EndSample();
#endif
                toRun = null;
            }
        }
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

        [SerializeField]
        private string nameOfOriginalPrefab = null;

        public string GetLightMapHashCode()
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
                bool hasColors = RuntimeBakedLightmapUtils.SceneHasAllLightmaps(texturesColor, LightMapType.LightColor);
                bool hasDirs = RuntimeBakedLightmapUtils.SceneHasAllLightmaps(texturesDir, LightMapType.LightDir);
                bool hasShadows = RuntimeBakedLightmapUtils.SceneHasAllLightmaps(texturesShadow, LightMapType.LightShadow);

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
            // ActionOnEnable(); // uncomment to use textures right away
            PrefabBakerManager.AddInstance(this);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }


        private void OnDisable()
        {
            // ActionOnDisable(); // uncomment to use textures right away
            PrefabBakerManager.RemoveInstance(this);
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void ActionOnEnable()
        {
            BakeApply(true);
        }

        private void ActionOnDisable()
        {
            if (RuntimeBakedLightmapUtils.RemoveInstance(this))
            {
                BakeJustApplied = false;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            BakeApply(false);
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