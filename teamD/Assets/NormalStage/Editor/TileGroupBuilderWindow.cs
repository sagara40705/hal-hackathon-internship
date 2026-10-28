using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NormalStage.EditorTools
{
    public sealed class TileGroupBuilderWindow : EditorWindow
    {
        private const string ViewPath = "Assets/NormalStage/Editor/TileGroupBuilderWindow.uxml";
        [SerializeField] private TileGroupLayoutData _data = new TileGroupLayoutData();
        [SerializeField] private GameObject _brush;
        [SerializeField] private int _radius = 5;
        [SerializeField] private bool _compact = true;
        [SerializeField] private bool _erase;
        [SerializeField] private TileGroupLayout _sourceLayout;
        [SerializeField] private GameObject _lastSaved;
        private readonly List<GameObject> _palette = new List<GameObject>();
        private readonly Dictionary<Vector2Int, VisualElement> _slots = new Dictionary<Vector2Int, VisualElement>();
        private VisualElement _board;
        private IVisualElementScheduledItem _previewRefresh;
        private int _strokeUndo = -1;

        [MenuItem("Tools/Normal Stage/Tile Group Builder")]
        public static void Open()
        {
            bool alreadyOpen = HasOpenInstances<TileGroupBuilderWindow>();
            var window = GetWindow<TileGroupBuilderWindow>();
            window.titleContent = new GUIContent("Tile Group Builder");
            window.minSize = new Vector2(940, 620);
            if (!alreadyOpen)
            {
                Rect main = EditorGUIUtility.GetMainWindowPosition();
                window.position = new Rect(main.x + Mathf.Max(0, (main.width - 1180) * 0.5f),
                    main.y + Mathf.Max(0, (main.height - 780) * 0.5f), 1180, 780);
            }
            window.Show();
        }

        [OnOpenAsset]
        private static bool OnOpenLayout(int instanceId, int line)
        {
            if (EditorUtility.EntityIdToObject(instanceId) is not TileGroupLayout layout) return false;
            Open();
            GetWindow<TileGroupBuilderWindow>().LoadLayout(layout);
            return true;
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EndStroke();
            _previewRefresh?.Pause();
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        public void CreateGUI()
        {
            _previewRefresh?.Pause();
            rootVisualElement.Clear();
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ViewPath);
            if (tree == null) { rootVisualElement.Add(new Label("UIファイルが見つかりません: " + ViewPath)); return; }
            tree.CloneTree(rootVisualElement);
            _board = rootVisualElement.Q("board");
            _board.AddManipulator(new GridPainter(this));
            Q<Button>("refreshPalette").clicked += RefreshPalette;
            Q<ToolbarSearchField>("search").RegisterValueChangedCallback(_ => DrawPalette());
            var custom = Q<ObjectField>("customCell");
            custom.objectType = typeof(GameObject);
            custom.allowSceneObjects = false;
            custom.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue is not GameObject prefab) return;
                if (!TileGroupPrefabWriter.IsCellPrefab(prefab))
                {
                    ShowNotification(new GUIContent("ルートにTileCellを持つ単独セルのPrefabを選んでください。"));
                    custom.SetValueWithoutNotify(null);
                    return;
                }
                if (!_palette.Contains(prefab)) _palette.Add(prefab);
                SelectBrush(prefab);
            });
            Q<Button>("paintMode").clicked += () => { _erase = false; RefreshBrush(); };
            Q<Button>("eraseMode").clicked += () => { _erase = true; RefreshBrush(); };
            Q<Button>("undo").clicked += Undo.PerformUndo;
            Q<Button>("redo").clicked += Undo.PerformRedo;
            Q<Button>("clear").clicked += () => Change("Clear tile layout", () => { _data.cells.Clear(); _data.origin = Vector2Int.zero; });
            Q<SliderInt>("radius").RegisterValueChangedCallback(evt => { _radius = evt.newValue; DrawBoard(); RefreshStatus(); });
            Q<Toggle>("compact").RegisterValueChangedCallback(evt => { _compact = evt.newValue; _board.EnableInClassList("compact", _compact); });
            Q<TextField>("prefabName").isDelayed = true;
            Q<TextField>("prefabName").RegisterValueChangedCallback(evt => Change("Name tile group", () => _data.prefabName = evt.newValue));
            Q<Toggle>("fixed").RegisterValueChangedCallback(evt => Change("Change tile group type", () => _data.isStatic = evt.newValue));
            Q<FloatField>("tileSize").isDelayed = true;
            Q<FloatField>("tileSize").RegisterValueChangedCallback(evt => Change("Change tile size", () => _data.tileSize = evt.newValue));
            var folder = Q<ObjectField>("outputFolder");
            folder.objectType = typeof(DefaultAsset);
            folder.allowSceneObjects = false;
            folder.RegisterValueChangedCallback(evt =>
            {
                string path = AssetDatabase.GetAssetPath(evt.newValue);
                if (!AssetDatabase.IsValidFolder(path) || !TileGroupPrefabWriter.IsOutputFolder(path))
                {
                    ShowNotification(new GUIContent("Assets/NormalStage内のフォルダーを選んでください。"));
                    SyncFields();
                    return;
                }
                Change("Change output folder", () => _data.outputFolder = path);
            });
            var layout = Q<ObjectField>("layoutAsset");
            layout.objectType = typeof(TileGroupLayout);
            layout.allowSceneObjects = false;
            layout.RegisterValueChangedCallback(evt => _sourceLayout = evt.newValue as TileGroupLayout);
            Q<Button>("load").clicked += () => { if (_sourceLayout != null) LoadLayout(_sourceLayout); };
            Q<Button>("save").clicked += Save;
            Q<Button>("selectSaved").clicked += () => { if (_lastSaved != null) { Selection.activeObject = _lastSaved; EditorGUIUtility.PingObject(_lastSaved); } };
            RefreshPalette();
            SyncFields();
            DrawBoard();
            RefreshStatus();
            // AssetPreviewは非同期生成なので、未生成のサムネイルだけ再取得する。
            _previewRefresh = rootVisualElement.schedule.Execute(RefreshImages).Every(500);
        }

        private T Q<T>(string name) where T : VisualElement => rootVisualElement.Q<T>(name);

        private void RefreshPalette()
        {
            _palette.Clear();
            var folders = new[] { "Assets/Tile/Prefab/TileCell", "Assets/Gimmick/Prefab/TileCell", "Assets/NormalStage" }
                .Where(AssetDatabase.IsValidFolder).ToArray();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", folders))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (TileGroupPrefabWriter.IsCellPrefab(prefab) && !_palette.Contains(prefab)) _palette.Add(prefab);
            }
            foreach (var stamp in _data.cells)
                if (TileGroupPrefabWriter.IsCellPrefab(stamp.prefab) && !_palette.Contains(stamp.prefab)) _palette.Add(stamp.prefab);
            _palette.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            if (_brush == null) _brush = _palette.FirstOrDefault(p => p.name == "Cell") ?? _palette.FirstOrDefault();
            if (_brush != null && !_palette.Contains(_brush) && TileGroupPrefabWriter.IsCellPrefab(_brush)) _palette.Add(_brush);
            DrawPalette();
            RefreshBrush();
        }

        private void DrawPalette()
        {
            var palette = Q<VisualElement>("palette");
            palette.Clear();
            string search = Q<ToolbarSearchField>("search").value ?? "";
            foreach (var prefab in _palette)
            {
                if (prefab == null) continue;
                string display = DisplayName(prefab);
                if ((display + prefab.name).IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;
                var button = new Button(() => SelectBrush(prefab)) { tooltip = AssetDatabase.GetAssetPath(prefab) };
                button.AddToClassList("palette-button");
                button.EnableInClassList("selected", prefab == _brush && !_erase);
                var thumbnail = new Image { userData = prefab, pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.ScaleToFit };
                thumbnail.AddToClassList("palette-image");
                button.Add(thumbnail);
                var label = new Label(display) { pickingMode = PickingMode.Ignore };
                label.AddToClassList("palette-name");
                button.Add(label);
                palette.Add(button);
            }
            RefreshImages();
        }

        private void SelectBrush(GameObject prefab)
        {
            _brush = prefab;
            _erase = false;
            DrawPalette();
            RefreshBrush();
        }

        private void RefreshBrush()
        {
            Q<Label>("brushLabel").text = _erase ? "削除モード" : "選択中：" + DisplayName(_brush);
            Q<Button>("paintMode").EnableInClassList("selected", !_erase);
            Q<Button>("eraseMode").EnableInClassList("selected", _erase);
        }

        private void DrawBoard()
        {
            _board.Clear();
            _slots.Clear();
            _board.EnableInClassList("compact", _compact);
            for (int z = _radius; z >= -_radius; z--)
            {
                var row = new VisualElement();
                row.AddToClassList("board-row");
                _board.Add(row);
                for (int x = -_radius; x <= _radius; x++)
                {
                    var position = new Vector2Int(x, z);
                    var slot = new VisualElement { userData = position };
                    slot.AddToClassList("cell");
                    var thumbnail = new Image { pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.ScaleToFit };
                    thumbnail.AddToClassList("cell-image");
                    slot.Add(thumbnail);
                    var label = new Label { pickingMode = PickingMode.Ignore };
                    label.AddToClassList("cell-label");
                    slot.Add(label);
                    row.Add(slot);
                    _slots.Add(position, slot);
                    RefreshSlot(position);
                }
            }
            RefreshImages();
        }

        private void RefreshSlot(Vector2Int position)
        {
            if (!_slots.TryGetValue(position, out var slot)) return;
            var stamp = _data.cells.Find(c => c.position == position);
            bool origin = position == _data.origin;
            slot.EnableInClassList("origin", origin);
            slot.EnableInClassList("axis", stamp == null && (position.x == _data.origin.x || position.y == _data.origin.y));
            slot.EnableInClassList("occupied", stamp != null);
            slot.EnableInClassList("empty", stamp == null);
            slot.EnableInClassList("invalid", stamp != null && stamp.prefab == null);
            string cellName = stamp != null ? ShortName(stamp.prefab) : $"{position.x},{position.y}";
            slot.Q<Label>().text = (origin ? "◎ " : "") + cellName;
            var thumbnail = slot.Q<Image>();
            thumbnail.userData = stamp?.prefab;
            thumbnail.image = stamp?.prefab != null ? AssetPreview.GetAssetPreview(stamp.prefab) : null;
            slot.tooltip = $"X={position.x}, Z={position.y}\n" + (stamp != null ? DisplayName(stamp.prefab) : "空きマス") +
                           (origin ? "\nPrefabの原点" : "\nShift＋クリックで原点に設定");
        }

        private void RefreshImages()
        {
            rootVisualElement.Query<Image>().ForEach(img =>
            {
                if (img.userData is GameObject prefab && prefab != null && img.image == null)
                    img.image = AssetPreview.GetAssetPreview(prefab);
            });
        }

        private void SyncFields()
        {
            Q<TextField>("prefabName").SetValueWithoutNotify(_data.prefabName);
            Q<Toggle>("fixed").SetValueWithoutNotify(_data.isStatic);
            Q<FloatField>("tileSize").SetValueWithoutNotify(_data.tileSize);
            Q<ObjectField>("outputFolder").SetValueWithoutNotify(AssetDatabase.LoadAssetAtPath<DefaultAsset>(_data.outputFolder));
            Q<Label>("folderPath").text = _data.outputFolder;
            Q<ObjectField>("layoutAsset").SetValueWithoutNotify(_sourceLayout);
            Q<SliderInt>("radius").SetValueWithoutNotify(_radius);
            Q<Toggle>("compact").SetValueWithoutNotify(_compact);
        }

        private void RefreshStatus()
        {
            string error = TileGroupPrefabWriter.Validate(_data);
            if (EditorApplication.isPlayingOrWillChangePlaymode) error = "保存はPlayモードを終了してから行ってください。";
            var messages = new List<string>();
            int hidden = _data.cells.Count(c => Mathf.Abs(c.position.x) > _radius || Mathf.Abs(c.position.y) > _radius);
            if (hidden > 0) messages.Add($"表示範囲外に{hidden}セルあります。保存には含まれます。表示範囲を広げて確認できます。");
            int islands = TileGroupPrefabWriter.CountIslands(_data);
            if (islands > 1) messages.Add($"形が{islands}か所に分かれています。すべて一緒に動きます。");
            if (_data.cells.Count > 0 && !_data.cells.Any(c => c.position == _data.origin)) messages.Add("原点は空きマスです。黄色の枠がステージ配置時の基準になります。");
            if (_data.cells.Any(c => c.prefab != null && c.prefab.GetComponentInChildren<ChangeSpeed>(true) != null)) messages.Add("減速床にはシーン側のSlowTileSpeedController設定が必要です。");
            var box = Q<HelpBox>("validation");
            box.text = error ?? (messages.Count > 0 ? string.Join("\n", messages) : "保存できます。セルのイベント・見た目・Colliderは元のPrefabを引き継ぎます。");
            box.messageType = error != null && _data.cells.Count > 0 ? HelpBoxMessageType.Error : messages.Count > 0 ? HelpBoxMessageType.Warning : HelpBoxMessageType.Info;
            Q<Button>("save").SetEnabled(error == null);
            Q<Button>("selectSaved").SetEnabled(_lastSaved != null);
            Q<Label>("stats").text = $"{_data.cells.Count}セル  |  原点 X={_data.origin.x}, Z={_data.origin.y}  |  {(_data.isStatic ? "固定" : "移動可能")}";
        }

        private void Change(string name, Action action)
        {
            Undo.RegisterCompleteObjectUndo(this, name);
            action();
            SyncFields();
            foreach (var position in _slots.Keys) RefreshSlot(position);
            RefreshStatus();
        }

        private void OnUndoRedo()
        {
            if (_board == null) return;
            SyncFields();
            DrawBoard();
            RefreshPalette();
            RefreshStatus();
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (_board != null) RefreshStatus();
        }

        private void LoadLayout(TileGroupLayout layout)
        {
            Change("Load tile layout", () =>
            {
                _data = layout.data.Copy();
                _sourceLayout = layout;
                int max = _data.cells.Count == 0 ? 5 : _data.cells.Max(c => Mathf.Max(Mathf.Abs(c.position.x), Mathf.Abs(c.position.y)));
                _radius = Mathf.Clamp(Mathf.Max(5, max), 2, 12);
            });
            RefreshPalette();
            DrawBoard();
        }

        private void Save()
        {
            try
            {
                _lastSaved = TileGroupPrefabWriter.SaveAsNew(_data, out _sourceLayout);
                Q<ObjectField>("layoutAsset").SetValueWithoutNotify(_sourceLayout);
                Q<Label>("saveResult").text = "保存済み：\n" + AssetDatabase.GetAssetPath(_lastSaved);
                EditorGUIUtility.PingObject(_lastSaved);
                RefreshStatus();
                ShowNotification(new GUIContent("Prefabとレイアウトを保存しました"));
            }
            catch (Exception exception)
            {
                Q<Label>("saveResult").text = "保存できませんでした：" + exception.Message;
                Debug.LogException(exception);
            }
        }

        private void BeginStroke()
        {
            EndStroke();
            Undo.IncrementCurrentGroup();
            _strokeUndo = Undo.GetCurrentGroup();
            Undo.RegisterCompleteObjectUndo(this, "Paint tile cells");
        }

        private void EndStroke()
        {
            if (_strokeUndo < 0) return;
            Undo.CollapseUndoOperations(_strokeUndo);
            _strokeUndo = -1;
        }

        private void Paint(Vector2Int position, bool erase)
        {
            var existing = _data.cells.Find(c => c.position == position);
            if (erase)
            {
                if (existing == null) return;
                _data.cells.Remove(existing);
            }
            else
            {
                if (!TileGroupPrefabWriter.IsCellPrefab(_brush) || existing?.prefab == _brush) return;
                if (existing == null) _data.cells.Add(new TileGroupCellStamp(position, _brush));
                else existing.prefab = _brush;
            }
            RefreshSlot(position);
            RefreshStatus();
        }

        private static string DisplayName(GameObject prefab)
        {
            if (prefab == null) return "未選択 / 参照切れ";
            return prefab.name switch
            {
                "Cell" => "通常床", "Cell_Direction_F" => "↑ 方向転換 +Z", "Cell_Direction_B" => "↓ 方向転換 -Z",
                "Cell_Direction_R" => "→ 方向転換 +X", "Cell_Direction_L" => "← 方向転換 -X",
                "Cell_Jump" => "ジャンプ台", "Cell_Warp" => "ワープ", "Cell_Damage" => "ダメージ床",
                "Cell_Slow" => "減速床", "Cell_Breakable" => "崩れる床（1セル用）", "Cell_PeriodicBlock" => "周期ブロック",
                "Cell_Attack" => "攻撃床", _ => prefab.name
            };
        }

        private static string ShortName(GameObject prefab)
        {
            if (prefab == null) return "欠損";
            return prefab.name switch
            {
                "Cell" => "床", "Cell_Direction_F" => "↑", "Cell_Direction_B" => "↓",
                "Cell_Direction_R" => "→", "Cell_Direction_L" => "←", "Cell_Jump" => "跳",
                "Cell_Warp" => "Warp", "Cell_Damage" => "危険", "Cell_Slow" => "減速",
                "Cell_Breakable" => "崩", "Cell_PeriodicBlock" => "周期", "Cell_Attack" => "攻撃",
                _ => prefab.name.Length > 6 ? prefab.name.Substring(0, 6) : prefab.name
            };
        }

        private sealed class GridPainter : PointerManipulator
        {
            private readonly TileGroupBuilderWindow _window;
            private int _pointer = -1;
            private bool _eraseStroke;
            private Vector2Int? _previous;

            public GridPainter(TileGroupBuilderWindow window) { _window = window; }
            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<PointerDownEvent>(Down);
                target.RegisterCallback<PointerMoveEvent>(Move);
                target.RegisterCallback<PointerUpEvent>(Up);
                target.RegisterCallback<PointerCaptureOutEvent>(Lost);
                target.RegisterCallback<PointerCancelEvent>(Cancel);
            }
            protected override void UnregisterCallbacksFromTarget()
            {
                Finish();
                target.UnregisterCallback<PointerDownEvent>(Down);
                target.UnregisterCallback<PointerMoveEvent>(Move);
                target.UnregisterCallback<PointerUpEvent>(Up);
                target.UnregisterCallback<PointerCaptureOutEvent>(Lost);
                target.UnregisterCallback<PointerCancelEvent>(Cancel);
            }
            private bool Pick(Vector2 point, out Vector2Int position)
            {
                foreach (var pair in _window._slots)
                    if (pair.Value.worldBound.Contains(point) && _window.Q<ScrollView>("boardScroll").contentViewport.worldBound.Contains(point))
                    { position = pair.Key; return true; }
                position = default;
                return false;
            }
            private void Down(PointerDownEvent evt)
            {
                if (_pointer >= 0 || (evt.button != 0 && evt.button != 1) || !Pick(evt.position, out var position)) return;
                target.Focus();
                evt.StopPropagation();
                if (evt.shiftKey && evt.button == 0)
                {
                    _window.Change("Set tile group origin", () => _window._data.origin = position);
                    return;
                }
                if (evt.altKey && evt.button == 0)
                {
                    var stamp = _window._data.cells.Find(c => c.position == position);
                    if (stamp?.prefab != null) _window.SelectBrush(stamp.prefab);
                    return;
                }
                _pointer = evt.pointerId;
                _eraseStroke = evt.button == 1 || _window._erase;
                _previous = position;
                _window.BeginStroke();
                target.CapturePointer(_pointer);
                _window.Paint(position, _eraseStroke);
            }
            private void Move(PointerMoveEvent evt)
            {
                if (_pointer != evt.pointerId) return;
                if (!Pick(evt.position, out var position)) { _previous = null; return; }
                // 速いドラッグでもイベント間のセルを抜かさない。
                var from = _previous ?? position;
                int steps = Mathf.Max(Mathf.Abs(position.x - from.x), Mathf.Abs(position.y - from.y));
                for (int i = 0; i <= steps; i++)
                {
                    var sample = Vector2.Lerp(from, position, steps == 0 ? 0f : (float)i / steps);
                    _window.Paint(Vector2Int.RoundToInt(sample), _eraseStroke);
                }
                _previous = position;
                evt.StopPropagation();
            }
            private void Up(PointerUpEvent evt) { if (evt.pointerId == _pointer) { Finish(); evt.StopPropagation(); } }
            private void Lost(PointerCaptureOutEvent evt) { if (evt.pointerId == _pointer) Finish(); }
            private void Cancel(PointerCancelEvent evt) { if (evt.pointerId == _pointer) Finish(); }
            private void Finish()
            {
                int pointer = _pointer;
                _pointer = -1;
                _previous = null;
                _window.EndStroke();
                if (pointer >= 0 && target.HasPointerCapture(pointer)) target.ReleasePointer(pointer);
            }
        }
    }
}
