using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dao.ConcurrentDictionaryLazy;

namespace PrefabLightMapBaker
{
    public partial class PrefabBaker
    {
        public class PrefabBakerManager : MonoBehaviour
        {
            public const string PATH_TO_RESOURCE = "PrefabLightBaker";
            private static readonly ConcurrentDictionaryLazy<PrefabBaker, bool> AddOrRemove = new ConcurrentDictionaryLazy<PrefabBaker, bool>(50);
            private static Coroutine toRun = null;
            private static PrefabBakerManager manager = null;
            private static int COUNT_FRAMES = 5;

            public static PrefabBakerManager Manager
            {
                get
                {
                    if (manager == null)
                    {
                        var go = new GameObject();
                        manager = go.AddComponent<PrefabBakerManager>();
                        go.isStatic = true;
                        go.hideFlags = HideFlags.HideAndDontSave;
                        GameObject.DontDestroyOnLoad(go);
                        var loadedResources = Resources.LoadAll<PrefabBakerManagerSettings>(PATH_TO_RESOURCE);
                        foreach (var item in loadedResources)
                        {
                            COUNT_FRAMES = item.NumberOfLightMapSetPassesForSingleFrame;
                            Resources.UnloadAsset(item);
                        }

                    }
                    return manager;
                }
            }

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
                if (toRun == null)
                {
                    toRun = Manager.StartCoroutine(WorkingCoroutine());
                }
            }

            private static IEnumerator WorkingCoroutine()
            {
                yield return null;
                // Lazy loaded enumerated
                int count = 0;
                int adding;
                RuntimeBakedLightmapUtils.ClearAndAddUnityLightMaps();
                foreach (var item in AddOrRemove)
                {
                    adding = 0;
                    if (item.Key != null)
                    {
                        if (item.Value)
                        {
                            if (!item.Key.RefAdded)
                            {
                                adding = 1;
                                item.Key.ActionOnEnable();
                            }
                        }
                        else
                        {
                            if (item.Key.RefAdded)
                            {
                                adding = 1;
                                item.Key.ActionOnDisable();
                            }
                        }
                        if (RuntimeBakedLightmapUtils.IsLightMapsChanged)
                        {
                            RuntimeBakedLightmapUtils.IsLightMapsChanged = false;
                            count += adding;
                            if (count % COUNT_FRAMES == 0)
                            {
                                adding = 0;
                                RuntimeBakedLightmapUtils.UpdateUnityLightMaps();
                                yield return null;
                                RuntimeBakedLightmapUtils.ClearAndAddUnityLightMaps();
                            }
                        }
                    }
                }
                if (count > 0)
                {
                    RuntimeBakedLightmapUtils.UpdateUnityLightMaps();
                }
                RuntimeBakedLightmapUtils.ClearAndAddUnityLightMaps(false);
                AddOrRemove.Clear();
                toRun = null;
            }
        }
    }
}