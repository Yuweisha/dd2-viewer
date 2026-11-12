using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class FixViewerMaterials
{
    const string HeroRoot = "Assets/Heroes";
    const string LogPath = "fixmats_log.txt";

    [MenuItem("Viewer/Fix Materials Textures")]
    public static void Run()
    {
        var sb = new StringBuilder();
        try
        {
            var std = Shader.Find("Standard");
            // 收集所有贴图（按文件名索引）
            var texByName = new Dictionary<string, Texture2D>();
            foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { HeroRoot }))
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                if (t != null) texByName[Path.GetFileNameWithoutExtension(p)] = t;
            }
            sb.AppendLine("textures indexed: " + texByName.Count);

            int fixedCount = 0, noTex = 0;
            foreach (var g in AssetDatabase.FindAssets("t:Material", new[] { HeroRoot }))
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (mat == null) continue;

                // mat_xxx -> tex_xxx_col / tex_xxx
                string baseName = Path.GetFileNameWithoutExtension(p);   // mat_leper
                string key = baseName.StartsWith("mat_") ? baseName.Substring(4) : baseName;
                Texture2D tex = null;
                foreach (var cand in new[] { "tex_" + key + "_col", "tex_" + key, "tex_" + key + "_diff", "tex_" + key + "_col_01" })
                {
                    if (texByName.TryGetValue(cand, out var t)) { tex = t; break; }
                }
                // 模糊匹配：任意以 tex_key 开头的
                if (tex == null)
                {
                    var fuzzy = texByName.Keys.FirstOrDefault(k => k.StartsWith("tex_" + key));
                    if (fuzzy != null) tex = texByName[fuzzy];
                }

                if (tex == null) { noTex++; continue; }

                mat.shader = std;
                mat.SetTexture("_MainTex", tex);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.1f);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
                EditorUtility.SetDirty(mat);
                fixedCount++;
                sb.AppendLine("  " + baseName + " <- " + tex.name);
            }
            AssetDatabase.SaveAssets();
            sb.AppendLine("fixed=" + fixedCount + " noTex=" + noTex);
        }
        catch (System.Exception e) { sb.AppendLine("EX " + e); }
        File.WriteAllText(LogPath, sb.ToString());
    }
}
