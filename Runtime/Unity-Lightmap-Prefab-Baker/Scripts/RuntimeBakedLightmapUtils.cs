using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace PrefabLightMapBaker
{
    public static class RuntimeBakedLightmapUtils
    {
        private class LightMapPrefabStorage
        {
            public int referenceCount = 0;
            public List<LightmapData> lightData = null;
        }

        private class LightmapWithIndex
        {
            public LightmapData lightData = null;
            public int index = -1;
        }
        private struct ActionStruct
        {
            public PrefabBaker prefab;
            public bool AddOrRemove;
        }
        private static readonly Dictionary<string, LightMapPrefabStorage> prefabToLightmap = new Dictionary<string, LightMapPrefabStorage>();
        private static List<LightmapData> added_lightmaps = new List<LightmapData>();
        private static List<LightmapWithIndex> changed_lightmaps = new List<LightmapWithIndex>();
        private static Dictionary<PrefabBaker.LightMapType, System.Func<Texture2D[], bool>> switchCase = null;
        private static Dictionary<PrefabBaker.LightMapType, System.Func<Texture2D[], bool>> SwitchCase
        {
            get
            {
                if (switchCase == null)
                {
                    switchCase = new Dictionary<PrefabBaker.LightMapType, System.Func<Texture2D[], bool>>()
                    {
                        {PrefabBaker.LightMapType.LightColor, IsInLightColor},
                        {PrefabBaker.LightMapType.LightDir, IsInLightDir},
                        {PrefabBaker.LightMapType.LightShadow, IsInLightShadows},
                    };
                }
                return switchCase;
            }
        }
        private static List<LightmapData> sceneLightData = new List<LightmapData>(50);
        private static List<ActionStruct> actionsToPerform = new List<ActionStruct>(50);
        private static List<LightmapData> CurrentFrameLightData
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    ClearAndAddUnityLightMaps();
                    return sceneLightData;
                }
#endif
                return sceneLightData;
            }
        }

        public static void UpdateUnityLightMaps()
        {
            LightmapSettings.lightmaps = CurrentFrameLightData.ToArray();
            foreach (var item in actionsToPerform)
            {
                if (item.prefab != null)
                {
                    item.prefab.ReleaseShaders();
                }
            }
        }

        public static void ClearAndAddUnityLightMaps()
        {
            CurrentFrameLightData.Clear();
            actionsToPerform.Clear();
            CurrentFrameLightData.AddRange(LightmapSettings.lightmaps);
        }

        public static void AddInstanceRef(PrefabBaker prefab)
        {
            var hashCode = prefab.GetLightMapHashCode();
            if (!prefabToLightmap.TryGetValue(hashCode, out _))
            {
                if (!InitializeInstance(prefab))
                {
                    int max = Mathf.Max(prefab.texturesColor.Length, prefab.texturesDir.Length);
                    max = Mathf.Max(max, prefab.texturesShadow.Length);
                    for (int i = 0; i < max; i++)
                    {
                        var newLightmapData = new LightmapData
                        {
                            lightmapColor = GetElement(prefab.texturesColor, i),
                            lightmapDir = GetElement(prefab.texturesDir, i),
                            shadowMask = GetElement(prefab.texturesShadow, i)
                        };
                        JoinOn(prefab, newLightmapData);
                    }
                }
            }
            prefabToLightmap[hashCode].referenceCount++;
        }

        public static bool RemoveInstance(PrefabBaker prefab)
        {
            bool fullyCleaned = false;
            string hashCode = prefab.GetLightMapHashCode();
            if (prefabToLightmap.TryGetValue(hashCode, out LightMapPrefabStorage storage))
            {
                storage.referenceCount--;
                if (storage.referenceCount <= 0)
                {
                    storage.referenceCount = 0;
                    fullyCleaned = true;
                    RemoveEmpty(prefab, storage);
                    prefabToLightmap.Remove(hashCode);
                }
            }
            return fullyCleaned;
        }

        private static int GetHashCodeCustom(LightmapData objectToGetCode)
        {
            int result = 0;
            result += objectToGetCode.lightmapColor == null ? 0 : objectToGetCode.lightmapColor.GetInstanceID();
            result += objectToGetCode.lightmapDir == null ? 0 : objectToGetCode.lightmapDir.GetInstanceID();
            result += objectToGetCode.shadowMask == null ? 0 : objectToGetCode.shadowMask.GetInstanceID();
            return result;
        }

        private static void RemoveEmpty(PrefabBaker prefab, LightMapPrefabStorage toRemoveData)
        {
            var sceneLightmaps = CurrentFrameLightData;
            int count = sceneLightmaps.Count;
            for (int j = 0; j < count; j++)
            {
                int hash = GetHashCodeCustom(sceneLightmaps[j]);
                foreach (var item in toRemoveData.lightData)
                {
                    if (hash == GetHashCodeCustom(item))
                    {
                        sceneLightmaps[j] = new LightmapData();
                    }
                }
            }
            actionsToPerform.Add(new ActionStruct { prefab = prefab, AddOrRemove = false });
        }

        public static bool InitializeInstance(PrefabBaker prefab)
        {
            if (prefab.renderers == null || prefab.renderers.Length == 0) return false;

            var sceneLightmapsRef = CurrentFrameLightData;
            added_lightmaps.Clear();
            changed_lightmaps.Clear();

            int max = Mathf.Max(prefab.texturesColor.Length, prefab.texturesDir.Length);
            max = Mathf.Max(max, prefab.texturesShadow.Length);

            int[] lightmapArrayOffsetIndex = new int[max];
            Stack<int> emptySlots = new Stack<int>(10);
            int count = CurrentFrameLightData.Count;
            for (int i = 0; i < max; i++)
            {
                bool found = false;
                for (int j = 0; j < count; j++)
                {
                    if (sceneLightmapsRef[j].lightmapColor == null &&
                        sceneLightmapsRef[j].lightmapDir == null &&
                        sceneLightmapsRef[j].shadowMask == null)
                    {
                        emptySlots.Push(j);
                        continue;
                    }
                    found = prefab.texturesColor.Length > i && prefab.texturesColor[i] != null && prefab.texturesColor[i] == sceneLightmapsRef[j].lightmapColor;
                    found |= prefab.texturesDir.Length > i && prefab.texturesDir[i] != null && prefab.texturesDir[i] == sceneLightmapsRef[j].lightmapDir;
                    found |= prefab.texturesShadow.Length > i && prefab.texturesShadow[i] != null && prefab.texturesShadow[i] == sceneLightmapsRef[j].shadowMask;

                    if (found)
                    {
                        lightmapArrayOffsetIndex[i] = j;
                    }
                }

                if (!found)
                {
                    var newLightmapData = new LightmapData
                    {
                        lightmapColor = GetElement(prefab.texturesColor, i),
                        lightmapDir = GetElement(prefab.texturesDir, i),
                        shadowMask = GetElement(prefab.texturesShadow, i)
                    };

                    if (emptySlots.Count > 0)
                    {
                        lightmapArrayOffsetIndex[i] = emptySlots.Pop();
                        changed_lightmaps.Add(new LightmapWithIndex
                        {
                            lightData = newLightmapData,
                            index = lightmapArrayOffsetIndex[i]
                        });
                    }
                    else
                    {
                        lightmapArrayOffsetIndex[i] = added_lightmaps.Count + count;
                        added_lightmaps.Add(newLightmapData);
                    }
                    JoinOn(prefab, newLightmapData);
                }
            }

            bool combined = false;
            if (added_lightmaps.Count > 0 || changed_lightmaps.Count > 0)
            {
                CombineLightmaps(added_lightmaps, changed_lightmaps);
                combined = true;
            }

            // Required for each instance once!
            UpdateLightmaps(prefab, lightmapArrayOffsetIndex);
            return combined;
        }

        private static void JoinOn(PrefabBaker prefab, LightmapData newData)
        {
            string hashCode = prefab.GetLightMapHashCode();
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(hashCode))
            {
                return;
            }
#endif
            if (!prefabToLightmap.TryGetValue(hashCode, out LightMapPrefabStorage storage))
            {
                storage = new LightMapPrefabStorage
                {
                    lightData = new List<LightmapData>(4)
                };
            }
            storage.lightData.Add(newData);
            prefabToLightmap[hashCode] = storage;
        }

        private static T GetElement<T>(T[] array, int index)
        {
            if (array == null) return default;
            if (array.Length < index + 1) return default;
            return array[index];
        }

        private static void CombineLightmaps(List<LightmapData> lightmaps, List<LightmapWithIndex> changed)
        {
            var original = CurrentFrameLightData;
            foreach (var item in changed)
            {
                original[item.index] = item.lightData;
            }

            for (int i = 0; i < lightmaps.Count; i++)
            {
                original.Add(lightmaps[i]);
            }

            // LightmapSettings.lightmaps = combined; // manager should add at the end of frame
        }

        // required on each instance
        private static void UpdateLightmaps(PrefabBaker prefab, int[] lightmapOffsetIndex)
        {
            for (var i = 0; i < prefab.renderers.Length; ++i)
            {
                var renderer = prefab.renderers[i];
                var lightIndex = prefab.renderersLightmapIndex[i];
                var lightScale = prefab.renderersLightmapOffsetScale[i];

                renderer.lightmapIndex = lightmapOffsetIndex[lightIndex];
                renderer.lightmapScaleOffset = lightScale;
            }

            actionsToPerform.Add(new ActionStruct { prefab = prefab, AddOrRemove = true });

            ChangeLightBaking(prefab.lights);
        }

        private static void ChangeLightBaking(LightInfo[] lightsInfo)
        {
            foreach (var info in lightsInfo)
            {
                info.light.bakingOutput = new LightBakingOutput
                {
                    isBaked = true,
                    mixedLightingMode = (MixedLightingMode)info.mixedLightingMode,
                    lightmapBakeType = (LightmapBakeType)info.lightmapBaketype
                };
            }
        }

        public static bool SceneHasAllLightmaps(Texture2D[] textures, PrefabBaker.LightMapType typeLight)
        {
            if ((textures?.Length ?? 0) < 1) return true;

            else if (CurrentFrameLightData.Count < 1) return false;

            return SwitchCase[typeLight](textures);
        }

        private static bool IsInLightColor(Texture2D[] textures)
        {
            foreach (var lmd in CurrentFrameLightData)
            {
                bool found = false;

                if (textures.Contains(lmd.lightmapColor)) found = true;
                if (!found) return false;
            }
            return true;
        }

        private static bool IsInLightDir(Texture2D[] textures)
        {
            foreach (var lmd in CurrentFrameLightData)
            {
                bool found = false;
                if (textures.Contains(lmd.lightmapDir)) found = true;
                if (!found) return false;
            }
            return true;
        }

        private static bool IsInLightShadows(Texture2D[] textures)
        {
            foreach (var lmd in CurrentFrameLightData)
            {
                bool found = false;
                if (textures.Contains(lmd.shadowMask)) found = true;
                if (!found) return false;
            }
            return true;
        }
    }
}