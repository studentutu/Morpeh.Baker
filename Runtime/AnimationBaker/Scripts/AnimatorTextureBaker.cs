using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Animations;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

public class AnimatorTextureBaker : MonoBehaviour
{
    public ComputeShader infoTexGen;
    public Shader playShader;
    [SerializeField] private int FPSBaked = 30;
    [SerializeField] private bool Bake = false;
    public struct VertInfo
    {
        public Vector3 position;
        public Vector3 normal;
    }

    private void OnValidate()
    {
        if (Bake)
        {
            if (Application.isPlaying)
            {
                StartCoroutine(Baking());
            }
            else
            {
                Debug.LogWarning(" Works only in play mode");
            }
            Bake = false;
        }
    }

    // Use this for initialization
    private IEnumerator Baking()
    {
        yield return null;
        var deltaTime = Time.deltaTime;
        var animator = GetComponentInChildren<Animator>();
        var clips = animator.runtimeAnimatorController.animationClips;
        Dictionary<string, AnimationClip> AnimStateNames = new Dictionary<string, AnimationClip>(); // state to name of animation
        foreach (var item in clips)
        {
            AnimStateNames.Add(item.name, item);
        }

        var ac = animator.runtimeAnimatorController as AnimatorController;
        var acLayers = ac.layers;
        AnimatorStateMachine stateMachine;
        ChildAnimatorState[] ch_animStates;
        Dictionary<string, AnimationClip> AnimStateNamesAndCLip = new Dictionary<string, AnimationClip>(); // state to name of animation
        float fullProgress = 0;
        foreach (AnimatorControllerLayer i in acLayers) //for each layer
        {
            stateMachine = i.stateMachine;
            ch_animStates = null;
            ch_animStates = stateMachine.states;
            foreach (ChildAnimatorState j in ch_animStates) //for each state
            {
                AnimStateNamesAndCLip.Add(j.state.name, AnimStateNames[j.state.motion.name]);
                fullProgress += AnimStateNames[j.state.motion.name].length;
            }
        }

        var skin = GetComponentInChildren<SkinnedMeshRenderer>();
        var vCount = skin.sharedMesh.vertexCount;
        var texWidth = Mathf.NextPowerOfTwo(vCount);
        var mesh = new Mesh();

        animator.speed = 0;
        AnimationClip Clip;
        foreach (var item in AnimStateNamesAndCLip)
        {
            var stateName = item.Key;
            Clip = item.Value;

            var frames = Mathf.NextPowerOfTwo((int)(Clip.length / 0.05f));
            var infoList = new List<VertInfo>();

            var pRt = new RenderTexture(texWidth, frames, 0, RenderTextureFormat.ARGBHalf);
            pRt.name = string.Format("{0}.{1}.posTex", name, stateName);
            var nRt = new RenderTexture(texWidth, frames, 0, RenderTextureFormat.ARGBHalf);
            nRt.name = string.Format("{0}.{1}.normTex", name, stateName);

            foreach (var rt in new[] { pRt, nRt })
            {
                rt.enableRandomWrite = true;
                rt.Create();
                RenderTexture.active = rt;
                GL.Clear(true, true, Color.clear);
            }

            animator.Play(stateName);
            yield return 0;

            for (var i = 0; i < frames; i++)
            {
                animator.Play(stateName, 0, (float)i / frames);
                yield return 0;
                skin.BakeMesh(mesh);

                infoList.AddRange(Enumerable.Range(0, vCount)
                    .Select(idx => new VertInfo()
                    {
                        position = mesh.vertices[idx],
                        normal = mesh.normals[idx]
                    })
                );
            }
            var buffer = new ComputeBuffer(infoList.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(VertInfo)));
            buffer.SetData(infoList.ToArray());

            var kernel = infoTexGen.FindKernel("CSMain");
            uint x, y, z;
            infoTexGen.GetKernelThreadGroupSizes(kernel, out x, out y, out z);

            infoTexGen.SetInt("VertCount", vCount);
            infoTexGen.SetBuffer(kernel, "Info", buffer);
            infoTexGen.SetTexture(kernel, "OutPosition", pRt);
            infoTexGen.SetTexture(kernel, "OutNormal", nRt);
            infoTexGen.Dispatch(kernel, vCount / (int)x + 1, frames / (int)y + 1, 1);

            buffer.Release();

#if UNITY_EDITOR
            var folderName = "BakedAnimationTex";
            var folderPath = Path.Combine("Assets", folderName);
            if (!AssetDatabase.IsValidFolder(folderPath))
                AssetDatabase.CreateFolder("Assets", folderName);

            var subFolder = name;
            var subFolderPath = Path.Combine(folderPath, subFolder);
            if (!AssetDatabase.IsValidFolder(subFolderPath))
                AssetDatabase.CreateFolder(folderPath, subFolder);

            var posTex = RenderTextureToTexture2D.Convert(pRt);
            var normTex = RenderTextureToTexture2D.Convert(nRt);
            Graphics.CopyTexture(pRt, posTex);
            Graphics.CopyTexture(nRt, normTex);

            var mat = new Material(playShader);
            mat.SetTexture("_MainTex", skin.sharedMaterial.mainTexture);
            mat.SetTexture("_PosTex", posTex);
            mat.SetTexture("_NmlTex", normTex);
            mat.SetFloat("_Length", Clip.length);
            if (Clip.isLooping)
            {
                mat.SetFloat("_Loop", 1f);
                mat.EnableKeyword("ANIM_LOOP");
            }

            var go = new GameObject(name + "." + stateName + " " + Clip.name);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            go.AddComponent<MeshFilter>().sharedMesh = skin.sharedMesh;

            AssetDatabase.CreateAsset(posTex, Path.Combine(subFolderPath, pRt.name + ".asset"));
            AssetDatabase.CreateAsset(normTex, Path.Combine(subFolderPath, nRt.name + ".asset"));
            AssetDatabase.CreateAsset(mat, Path.Combine(subFolderPath, string.Format("{0}.{1}.animTex.asset", name, stateName + " " + Clip.name)));
            PrefabUtility.CreatePrefab(Path.Combine(folderPath, go.name + ".prefab").Replace("\\", "/"), go);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }
    }
}
