using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SetupViewerAll
{
    const string ScenePath = "Assets/Scenes/Viewer.unity";
    const string HeroRoot = "Assets/Heroes";
    const string LogPath = "setupall_log.txt";

    static readonly Dictionary<string, string> HeroCn = new Dictionary<string, string>
    {
        { "abomination_beast", "畸变体（兽形）" },
        { "abomination_human", "畸变体（人形）" },
        { "bounty_hunter", "赏金猎人" },
        { "crusader", "十字军" },
        { "duelist", "决斗者" },
        { "flagellant", "苦修者" },
        { "grave_robber", "盗墓贼" },
        { "hellion", "蛮女" },
        { "highwayman", "强盗" },
        { "jester", "小丑" },
        { "leper", "麻风病人" },
        { "man_at_arms", "武装者" },
        { "occultist", "神秘学者" },
        { "plague_doctor", "瘟疫医生" },
        { "runaway", "逃亡者" },
        { "vestal", "修女" },
    };

    [MenuItem("Viewer/Setup All Heroes")]
    public static void Run()
    {
        var sb = new System.Text.StringBuilder();
        try
        {
            // 1. 修所有材质
            int fixedMats = 0;
            var std = Shader.Find("Standard");
            foreach (var g in AssetDatabase.FindAssets("t:Material", new[] { HeroRoot }))
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g));
                if (mat == null) continue;
                if (mat.shader == null || mat.shader.name.StartsWith("Red Hook") || mat.shader.name == "Hidden/InternalErrorShader")
                {
                    var tex = mat.HasProperty("_Base") ? mat.GetTexture("_Base") : null;
                    if (tex == null && mat.HasProperty("_MainTex")) tex = mat.GetTexture("_MainTex");
                    mat.shader = std;
                    if (tex != null)
                    {
                        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
                    }
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
                    EditorUtility.SetDirty(mat);
                    fixedMats++;
                }
            }
            AssetDatabase.SaveAssets();
            sb.AppendLine("materials fixed: " + fixedMats);

            // 2. 扫描英雄
            var entries = new List<ViewerMain.HeroEntry>();
            foreach (var dir in Directory.GetDirectories(Path.GetFullPath(HeroRoot)))
            {
                string hero = Path.GetFileName(dir);
                if (hero.Contains(".") || hero == "hero_template") continue;
                string rel = HeroRoot + "/" + hero;
                var fbxGuids = AssetDatabase.FindAssets("t:GameObject", new[] { rel })
                    .Where(g => AssetDatabase.GUIDToAssetPath(g).EndsWith(".fbx"))
                    .Where(g => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g)).StartsWith("mdl_"))
                    .ToList();
                if (fbxGuids.Count == 0) continue;

                var model = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(fbxGuids[0]));
                if (model == null) continue;

                var ctrlGuid = AssetDatabase.FindAssets("t:AnimatorController", new[] { rel + "/animation" }).FirstOrDefault();
                var ctrl = ctrlGuid != null
                    ? AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AssetDatabase.GUIDToAssetPath(ctrlGuid))
                    : null;

                string cn = HeroCn.ContainsKey(hero) ? HeroCn[hero] : hero;
                // 收集 state -> clip 映射（与 controller.animationClips 顺序对齐）
                var stateNames = new List<string>();
                if (ctrl is UnityEditor.Animations.AnimatorController ac)
                {
                    var clipToState = new Dictionary<string, string>();
                    foreach (var layer in ac.layers)
                        foreach (var st in layer.stateMachine.states)
                        {
                            var m = st.state.motion as AnimationClip;
                            if (m != null && !clipToState.ContainsKey(m.name))
                                clipToState[m.name] = st.state.name;
                        }
                    foreach (var c in ctrl.animationClips)
                        stateNames.Add(c != null && clipToState.ContainsKey(c.name) ? clipToState[c.name] : "");
                }

                var mats = AssetDatabase.FindAssets("t:Material", new[] { rel })
                    .Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)))
                    .Where(m => m != null).ToList();
                entries.Add(new ViewerMain.HeroEntry
                {
                    displayName = cn + "  (" + hero + ")",
                    heroId = hero,
                    modelPrefab = model,
                    controller = ctrl,
                    materials = mats,
                    clipStates = stateNames
                });
                sb.AppendLine(string.Format("  {0}: model={1} ctrl={2} clips={3}",
                    hero, model.name, ctrl != null ? ctrl.name : "null",
                    ctrl != null ? ctrl.animationClips.Length : 0));
            }
            sb.AppendLine("heroes: " + entries.Count);

            // 3. 更新场景
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var viewer = Object.FindObjectOfType<ViewerMain>();
            if (viewer == null)
            {
                var go = new GameObject("Viewer");
                viewer = go.AddComponent<ViewerMain>();
            }
            viewer.heroes = entries.OrderBy(e => e.displayName).ToList();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            sb.AppendLine("scene updated: " + ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        catch (System.Exception e) { sb.AppendLine("EX " + e); }
        File.WriteAllText(LogPath, sb.ToString());
    }
}
