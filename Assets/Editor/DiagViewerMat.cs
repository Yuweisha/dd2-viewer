using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class DiagViewerMat
{
    const string LogPath = "diagmat_log.txt";

    [MenuItem("Viewer/Diag Materials")]
    public static void Run()
    {
        var sb = new StringBuilder();
        try
        {
            var fbxPath = "Assets/Heroes/vestal/mdl_vestal.fbx";
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            sb.AppendLine("fbx: " + (go != null));
            if (go == null) { File.WriteAllText(LogPath, sb.ToString()); return; }

            var smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            sb.AppendLine("smrs: " + smrs.Length);
            foreach (var s in smrs)
            {
                sb.AppendLine("SMR " + s.name + " mats=" + s.sharedMaterials.Length);
                foreach (var m in s.sharedMaterials)
                {
                    if (m == null) { sb.AppendLine("   null"); continue; }
                    string mp = AssetDatabase.GetAssetPath(m);
                    sb.AppendLine("   " + m.name + "  path=" + (string.IsNullOrEmpty(mp) ? "<embedded>" : mp)
                        + "  shader=" + (m.shader != null ? m.shader.name : "null"));
                    if (m.HasProperty("_MainTex"))
                    {
                        var t = m.GetTexture("_MainTex");
                        sb.AppendLine("      _MainTex=" + (t != null ? t.name : "NULL"));
                    }
                }
            }

            // 也看独立材质
            sb.AppendLine();
            sb.AppendLine("=== standalone materials in Assets/Heroes/vestal ===");
            foreach (var g in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Heroes/vestal" }))
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g));
                sb.AppendLine("  " + mat.name + " shader=" + (mat.shader != null ? mat.shader.name : "null")
                    + " tex=" + (mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") != null ? mat.GetTexture("_MainTex").name : "NULL"));
            }
        }
        catch (System.Exception e) { sb.AppendLine("EX " + e); }
        File.WriteAllText(LogPath, sb.ToString());
    }
}
