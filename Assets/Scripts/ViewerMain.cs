using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// DD2 角色动画查看器：左侧角色，右侧动画，自由视角。
/// 挂在场景任意对象上。
/// </summary>
public class ViewerMain : MonoBehaviour
{
    public List<HeroEntry> heroes = new List<HeroEntry>();

    [Serializable]
    public class HeroEntry
    {
        public string displayName;
        public string heroId;
        public GameObject modelPrefab;
        public RuntimeAnimatorController controller;
        public List<Material> materials = new List<Material>();
        public List<string> clipStates = new List<string>();   // 与 _clips 一一对应，存状态名
    }

    GameObject _current;
    Animator _animator;
    AnimationClip _clip;
    float _time;
    float _speed = 1f;
    bool _playing = true;
    Vector2 _scrollHero, _scrollClip;
    List<AnimationClip> _clips = new List<AnimationClip>();
    List<string> _clipStates = new List<string>();
    int _heroIndex = -1;
    int _clipIndex = -1;
    bool _showUI = true;
    string _clipFilter = "";
    string _heroFilter = "";

    void Start()
    {
        if (heroes.Count > 0) SelectHero(0);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) _showUI = !_showUI;

        // 自由视角（沿用 OrbitCamera，若未挂则跳过）
        if (_clip != null && _animator != null && _playing)
        {
            _time += Time.deltaTime * _speed;
            if (_time > _clip.length) _time = 0f;
            _clip.SampleAnimation(_animator.gameObject, _time);
        }
    }

    void SelectHero(int idx)
    {
        if (idx < 0 || idx >= heroes.Count) return;
        _heroIndex = idx;
        var h = heroes[idx];

        if (_current != null) Destroy(_current);
        _clips.Clear();
        _clipIndex = -1;
        _clip = null;

        if (h.modelPrefab == null) return;
        _current = Instantiate(h.modelPrefab);
        _current.name = "viewer_" + h.displayName;
        _current.transform.position = Vector3.zero;
        _current.transform.rotation = Quaternion.Euler(0, 180, 0);

        // FBX 的 SMR 用 Default-Material，按名字匹配赋我们的材质
        foreach (var sr in _current.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            ApplyMaterial(sr, h);

        _animator = _current.GetComponentInChildren<Animator>();
        if (_animator == null) _animator = _current.AddComponent<Animator>();
        if (h.controller != null)
        {
            _animator.runtimeAnimatorController = h.controller;
            _clips.AddRange(h.controller.animationClips.Where(c => c != null));
            // clip -> state 名（同长度补位）
            _clipStates.Clear();
            for (int i = 0; i < _clips.Count; i++)
                _clipStates.Add(i < h.clipStates.Count ? h.clipStates[i] : "");
        }
        _animator.enabled = false;   // 关闭，避免覆盖采样

        // 相机对准
        var orbit = FindObjectOfType<OrbitCamera>();
        var smr = _current.GetComponentInChildren<SkinnedMeshRenderer>();
        if (orbit != null && smr != null)
        {
            orbit.target = smr.transform;
            orbit.distance = smr.bounds.size.y * 2.4f + 1f;
            orbit.pivotOffset = new Vector3(0, smr.bounds.size.y * 0.55f, 0);
        }
    }

    void ApplyMaterial(SkinnedMeshRenderer smr, HeroEntry h)
    {
        if (smr == null || h.materials == null || h.materials.Count == 0) return;

        // msh_vestal -> mat_vestal ; msh_vestal_wpn_bible -> mat_vestal_wpn_bible
        string n = smr.name;
        string want = n.StartsWith("msh_") ? "mat_" + n.Substring(4) : "mat_" + n;

        var mat = h.materials.FirstOrDefault(m => m != null && m.name == want);
        if (mat == null)
            mat = h.materials.Where(m => m != null)
                             .OrderByDescending(m => want.Contains(m.name) || m.name.Contains(n))
                             .FirstOrDefault();
        if (mat == null) return;

        var arr = new Material[Mathf.Max(1, smr.sharedMaterials.Length)];
        for (int i = 0; i < arr.Length; i++) arr[i] = mat;
        smr.sharedMaterials = arr;
    }

    static bool Match(string text, string filter)
    {
        if (string.IsNullOrEmpty(filter)) return true;
        if (string.IsNullOrEmpty(text)) return false;
        return text.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    void PlayClip(int idx)
    {
        if (idx < 0 || idx >= _clips.Count) return;
        _clipIndex = idx;
        _clip = _clips[idx];
        _time = 0f;
        _playing = true;
    }

    void OnGUI()
    {
        if (!_showUI) return;
        GUI.skin.label.fontSize = 13;
        GUI.skin.button.fontSize = 12;

        // 左：角色
        GUILayout.BeginArea(new Rect(8, 8, 240, Screen.height - 16), GUI.skin.box);
        GUILayout.Label("角色  (" + heroes.Count + ")");
        _heroFilter = GUILayout.TextField(_heroFilter ?? "", GUILayout.Height(22));
        _scrollHero = GUILayout.BeginScrollView(_scrollHero);
        for (int i = 0; i < heroes.Count; i++)
        {
            if (!Match(heroes[i].displayName, _heroFilter)) continue;
            bool sel = i == _heroIndex;
            if (GUILayout.Toggle(sel, heroes[i].displayName, "Button")) { if (!sel) SelectHero(i); }
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();

        // 右：动画
        GUILayout.BeginArea(new Rect(Screen.width - 320, 8, 312, Screen.height - 16), GUI.skin.box);
        int shownCount = 0;
        for (int i = 0; i < _clips.Count; i++)
            if (Match(_clips[i].name + " " + (i < _clipStates.Count ? _clipStates[i] : ""), _clipFilter)) shownCount++;
        GUILayout.Label("动画  (" + shownCount + "/" + _clips.Count + ")");
        _clipFilter = GUILayout.TextField(_clipFilter ?? "", GUILayout.Height(22));
        _scrollClip = GUILayout.BeginScrollView(_scrollClip);
        for (int i = 0; i < _clips.Count; i++)
        {
            string st = i < _clipStates.Count ? _clipStates[i] : "";
            if (!Match(_clips[i].name + " " + st, _clipFilter)) continue;
            bool sel = i == _clipIndex;
            string label = (string.IsNullOrEmpty(st) ? "" : st + "  ←  ") + _clips[i].name + "  " + _clips[i].length.ToString("F2") + "s";
            if (GUILayout.Toggle(sel, label, "Button")) { if (!sel) PlayClip(i); }
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();

        // 底部状态
        GUILayout.BeginArea(new Rect(256, Screen.height - 44, Screen.width - 584, 38));
        GUILayout.BeginHorizontal();
        GUILayout.Label(_clip != null ? _clip.name + "  " + _time.ToString("F2") + "/" + _clip.length.ToString("F2") + "s" : "选择动画", GUILayout.Width(380));
        if (GUILayout.Button(_playing ? "暂停" : "播放", GUILayout.Width(56))) _playing = !_playing;
        if (GUILayout.Button("重播", GUILayout.Width(56))) { _time = 0f; _playing = true; }
        GUILayout.Label("速度", GUILayout.Width(32));
        _speed = GUILayout.HorizontalSlider(_speed, 0.05f, 2f, GUILayout.Width(140));
        GUILayout.Label(_speed.ToString("F2") + "x", GUILayout.Width(48));
        GUILayout.Label("Tab 隐藏 UI  ·  左键旋转 · 右键平移 · 滚轮缩放 · F 复位", GUILayout.Width(400));
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }
}
