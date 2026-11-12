using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SetupViewer
{
    const string ScenePath = "Assets/Scenes/Viewer.unity";
    const string LogPath = "setup_log.txt";

    [MenuItem("Viewer/Setup Scene")]
    public static void Run()
    {
        var sb = new System.Text.StringBuilder();
        try
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            // 1. 材质 shader 替换（新工程没有 Red Hook 的 shader）
            int fixedMats = 0;
            var std = Shader.Find("Standard");
            foreach (var g in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Heroes" }))
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
                        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                    }
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
                    EditorUtility.SetDirty(mat);
                    fixedMats++;
                }
            }
            sb.AppendLine("materials fixed: " + fixedMats);
            AssetDatabase.SaveAssets();

            // 2. 建场景
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var sysGo = new GameObject("Viewer");
            var viewer = sysGo.AddComponent<ViewerMain>();

            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Heroes/leper/mdl_leper.fbx");
            var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Heroes/leper/animation/leper_animation_controller.controller");
            sb.AppendLine("fbx=" + (fbx != null) + " ctrl=" + (ctrl != null));
            if (fbx != null && ctrl != null)
            {
                viewer.heroes = new System.Collections.Generic.List<ViewerMain.HeroEntry>
                {
                    new ViewerMain.HeroEntry { displayName = "麻风病人 (Leper)", modelPrefab = fbx, controller = ctrl }
                };
            }

            // 相机
            var camGo = new GameObject("MainCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.03f, 0.04f);
            cam.fieldOfView = 35;
            cam.nearClipPlane = 0.05f;
            camGo.tag = "MainCamera";
            var orbit = camGo.AddComponent<OrbitCamera>();
            orbit.distance = 5f;
            orbit.pivotOffset = new Vector3(0, 1f, 0);

            // 地面（黑色）
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(4, 1, 4);
            var gmat = new Material(Shader.Find("Standard"));
            gmat.color = new Color(0.02f, 0.02f, 0.03f);
            if (gmat.HasProperty("_Glossiness")) gmat.SetFloat("_Glossiness", 0.05f);
            ground.GetComponent<Renderer>().sharedMaterial = gmat;

            // 灯光
            var keyGo = new GameObject("KeyLight");
            var key = keyGo.AddComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.3f;
            key.color = new Color(1f, 0.96f, 0.9f);
            keyGo.transform.rotation = Quaternion.Euler(45, 30, 0);

            var fillGo = new GameObject("FillLight");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.55f;
            fill.color = new Color(0.62f, 0.72f, 1f);
            fillGo.transform.rotation = Quaternion.Euler(15, -140, 0);

            var rimGo = new GameObject("RimLight");
            var rim = rimGo.AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.intensity = 0.9f;
            rim.color = new Color(1f, 0.82f, 0.62f);
            rimGo.transform.rotation = Quaternion.Euler(-25, 195, 0);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.2f, 0.22f, 0.28f);
            RenderSettings.skybox = null;

            EditorSceneManager.SaveScene(scene, ScenePath);
            sb.AppendLine("scene saved: " + ScenePath);

            // 3. 加入 Build Settings
            var scenes = new[] { ScenePath };
            EditorBuildSettings.scenes = scenes.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
            sb.AppendLine("build scenes set");

            AssetDatabase.Refresh();
        }
        catch (System.Exception e) { sb.AppendLine("EX " + e); }
        File.WriteAllText(LogPath, sb.ToString());
    }
}
