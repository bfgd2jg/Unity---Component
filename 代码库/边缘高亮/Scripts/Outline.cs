using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// 把这个脚本挂在需要描边的物体上，或者它的父物体上（会自动应用到所有子物体）
// 它会在运行时为每个 Renderer 组件生成一个新的 Mesh，并添加两个额外的材质来实现描边效果
// 你可以通过调整 outlineColor 和 outlineWidth 来控制描边的颜色和宽度
// 记得在切换场景时调用 Outline.ClearMeshCache() 来清理生成的 Mesh，避免内存泄漏

[DisallowMultipleComponent]
public class Outline : MonoBehaviour
{
    [Header("描边设置")]
    [SerializeField] Color outlineColor = Color.white;
    [SerializeField, Range(0f, 10f)] float outlineWidth = 2f;

    Renderer[] renderers;
    Material outlineMaskMaterial;
    Material outlineFillMaterial;
    MaterialPropertyBlock mpb;

    // 全局 Mesh 缓存
    static readonly Dictionary<Mesh, Mesh> meshCache = new Dictionary<Mesh, Mesh>();

    static readonly int ZTestId = Shader.PropertyToID("_ZTest");
    static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");

    void Awake()
    {
        Initialize();
    }

    void OnEnable()
    {
        ApplyMaterials();
        UpdateMaterialProperties();
    }

    void OnDisable()
    {
        RemoveMaterials();
    }

    void OnDestroy()
    {
        RemoveMaterials();

        if (outlineMaskMaterial) Destroy(outlineMaskMaterial);
        if (outlineFillMaterial) Destroy(outlineFillMaterial);
    }

    void Initialize()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        mpb = new MaterialPropertyBlock();

        outlineMaskMaterial = Instantiate(Resources.Load<Material>("Materials/OutlineMask"));
        outlineFillMaterial = Instantiate(Resources.Load<Material>("Materials/OutlineFill"));

        LoadMeshes();
    }

    void LoadMeshes()
    {
        foreach (var r in renderers)
        {
            Mesh sourceMesh = GetMesh(r);
            if (sourceMesh == null) continue;

            // 优先使用缓存
            if (meshCache.TryGetValue(sourceMesh, out Mesh cachedMesh) && cachedMesh != null)
            {
                ApplyMesh(r, cachedMesh);
                continue;
            }

            // 没缓存才生成
            Mesh outlineMesh = Instantiate(sourceMesh);
            outlineMesh.name = sourceMesh.name + " (Outline)";

            var smoothNormals = ComputeSmoothNormals(outlineMesh);
            outlineMesh.SetUVs(3, smoothNormals);

            CombineSubmeshes(outlineMesh, r.sharedMaterials.Length);

            meshCache[sourceMesh] = outlineMesh;
            ApplyMesh(r, outlineMesh);
        }
    }

    Mesh GetMesh(Renderer r)
    {
        if (r is SkinnedMeshRenderer smr) return smr.sharedMesh;

        var mf = r.GetComponent<MeshFilter>();
        return mf ? mf.sharedMesh : null;
    }

    void ApplyMesh(Renderer r, Mesh mesh)
    {
        if (r is SkinnedMeshRenderer smr) smr.sharedMesh = mesh;
        else
        {
            var mf = r.GetComponent<MeshFilter>();
            if (mf) mf.sharedMesh = mesh;
        }
    }

    List<Vector3> ComputeSmoothNormals(Mesh mesh)
    {
        var vertices = mesh.vertices;
        var normals = mesh.normals;
        var smoothNormals = new List<Vector3>(normals);

        var groups = new Dictionary<Vector3, List<int>>();

        for (int i = 0; i < vertices.Length; i++)
        {
            if (!groups.ContainsKey(vertices[i]))
                groups[vertices[i]] = new List<int>();

            groups[vertices[i]].Add(i);
        }

        foreach (var group in groups.Values)
        {
            if (group.Count <= 1) continue;

            Vector3 avg = Vector3.zero;
            foreach (int i in group) avg += normals[i];
            avg.Normalize();

            foreach (int i in group) smoothNormals[i] = avg;
        }

        return smoothNormals;
    }

    void ApplyMaterials()
    {
        foreach (var r in renderers)
        {
            var mats = new List<Material>(r.sharedMaterials);

            if (!mats.Contains(outlineMaskMaterial))
                mats.Add(outlineMaskMaterial);

            if (!mats.Contains(outlineFillMaterial))
                mats.Add(outlineFillMaterial);

            r.sharedMaterials = mats.ToArray();
        }
    }

    void RemoveMaterials()
    {
        foreach (var r in renderers)
        {
            var mats = new List<Material>(r.sharedMaterials);
            mats.Remove(outlineMaskMaterial);
            mats.Remove(outlineFillMaterial);
            r.sharedMaterials = mats.ToArray();
        }
    }

    void UpdateMaterialProperties()
    {
        outlineMaskMaterial.SetFloat(ZTestId, (float)CompareFunction.Always);
        outlineFillMaterial.SetFloat(ZTestId, (float)CompareFunction.Always);

        foreach (var r in renderers)
        {
            r.GetPropertyBlock(mpb);
            mpb.SetColor(OutlineColorId, outlineColor);
            mpb.SetFloat(OutlineWidthId, outlineWidth);
            r.SetPropertyBlock(mpb);
        }
    }

    void CombineSubmeshes(Mesh mesh, int materialCount)
    {
        if (mesh.subMeshCount == 1 || materialCount < mesh.subMeshCount) return;

        mesh.subMeshCount++;
        mesh.SetTriangles(mesh.triangles, mesh.subMeshCount - 1);
    }

    // 手动清缓存（切场景用）
    public static void ClearMeshCache()
    {
        foreach (var mesh in meshCache.Values)
        {
            if (mesh != null)
                Destroy(mesh);
        }
        meshCache.Clear();
    }
}