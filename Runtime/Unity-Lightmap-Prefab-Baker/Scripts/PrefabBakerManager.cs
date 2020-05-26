using System.Collections;
using UnityEngine;
using Dao.ConcurrentDictionaryLazy;

namespace PrefabLightMapBaker
{

    public partial class PrefabBaker
    {
        public class PrefabBakerManager : MonoBehaviour
        {
            private static Coroutine toRun = null;
            private static PrefabBakerManager manager = null;
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
                    }
                    return manager;
                }
            }
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
                if (toRun == null)
                {
                    toRun = Manager.StartCoroutine(WorkingCoroutine());
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
                    RuntimeBakedLightmapUtils.ClearAndAddUnityLightMaps();
                    if (item.Key != null)
                    {
                        count++;
                        if (item.Value)
                        {
                            if (!item.Key.RefAdded)
                            {
                                item.Key.ActionOnEnable();
                            }
                        }
                        else
                        {
                            if (item.Key.RefAdded)
                            {
                                item.Key.ActionOnDisable();
                            }
                        }
                        if (count % 1000 == 0)
                        {
                            RuntimeBakedLightmapUtils.UpdateUnityLightMaps();
                            yield return null;
                        }
                    }
                }

                RuntimeBakedLightmapUtils.UpdateUnityLightMaps();
#if UNITY_EDITOR
                UnityEngine.Profiling.Profiler.EndSample();
#endif
                toRun = null;
            }
        }
    }
}