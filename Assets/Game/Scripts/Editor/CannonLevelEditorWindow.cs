using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.BlockCrush.Level;
using Game.BlockCrush.Shared;
using UnityEditor;
using UnityEngine;
using Game.BlockCrush.Block.Shared;
using Game.BlockCrush.Level.Shared;

namespace Game.BlockCrush.Editor
{
    public sealed class CannonLevelEditorWindow : EditorWindow
    {
        private const float CellWidth = 76f;
        private const float CellHeight = 44f;

        private readonly CannonJsonParser _jsonParser = new CannonJsonParser();
        private CannonLevelData _levelData = new CannonLevelData();
        private TextAsset _sourceJson;
        private ColorBlockDatabase _blockDatabase;
        private BlockColorKey _paintColor = BlockColorKey.Red;
        private int _paintShots = 1;
        private float _paintShootRate = CannonSpawnData.DefaultShootRate;
        private bool _eraseMode;
        private Vector2 _scroll;

        [MenuItem("Tools/Block Crush/Cannon JSON Editor")]
        public static void Open()
        {
            GetWindow<CannonLevelEditorWindow>("Block Crush Cannons");
        }

        private void OnGUI()
        {
            DrawToolbar();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawLevelSettings();
            EditorGUILayout.Space(8f);
            DrawPaintSettings();
            EditorGUILayout.Space(8f);
            DrawSlotsPreview();
            EditorGUILayout.Space(4f);
            DrawGrid();
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(60f)))
            {
                _levelData = new CannonLevelData();
            }

            _sourceJson = (TextAsset)EditorGUILayout.ObjectField(_sourceJson, typeof(TextAsset), false);

            if (GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(60f)))
            {
                LoadSelectedJson();
            }

            if (GUILayout.Button("Save JSON", EditorStyles.toolbarButton, GUILayout.Width(90f)))
            {
                SaveJson();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLevelSettings()
        {
            EditorGUILayout.LabelField("Cannon Level", EditorStyles.boldLabel);
            _levelData.id = EditorGUILayout.TextField("Id", _levelData.id);
            _levelData.width = Mathf.Max(1, EditorGUILayout.IntField("Width", _levelData.width));
            _levelData.height = Mathf.Max(1, EditorGUILayout.IntField("Height", _levelData.height));
            _levelData.slotCount = Mathf.Max(0, EditorGUILayout.IntField("Top Slot Count", _levelData.slotCount));
            _blockDatabase = (ColorBlockDatabase)EditorGUILayout.ObjectField(
                "Block Database",
                _blockDatabase,
                typeof(ColorBlockDatabase),
                false);
        }

        private void DrawPaintSettings()
        {
            EditorGUILayout.LabelField("Paint", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _paintColor = (BlockColorKey)EditorGUILayout.EnumPopup("Color", _paintColor);
            _paintShots = Mathf.Max(0, EditorGUILayout.IntField("Shots", _paintShots));
            _paintShootRate = Mathf.Max(0.01f, EditorGUILayout.FloatField("Shoot Rate", _paintShootRate));
            _eraseMode = GUILayout.Toggle(_eraseMode, "Erase", EditorStyles.miniButton, GUILayout.Width(70f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Sort Cannons", GUILayout.Width(120f)))
            {
                SortCannons();
            }

            if (GUILayout.Button("Clear All", GUILayout.Width(90f)))
            {
                if (EditorUtility.DisplayDialog("Clear cannons", "Remove every cannon from this JSON?", "Clear", "Cancel"))
                {
                    _levelData.cannons.Clear();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSlotsPreview()
        {
            EditorGUILayout.LabelField("Top Slots", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(GetSlotIndent());

            for (int i = 0; i < _levelData.slotCount; i++)
            {
                GUI.enabled = false;
                GUILayout.Button("Slot " + i, GUILayout.Width(CellWidth), GUILayout.Height(26f));
                GUI.enabled = true;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawGrid()
        {
            EditorGUILayout.LabelField("Cannon Grid", EditorStyles.boldLabel);

            for (int y = _levelData.height - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();

                for (int x = 0; x < _levelData.width; x++)
                {
                    Color previousColor = GUI.backgroundColor;
                    GUI.backgroundColor = GetCellPreviewColor(x, y);

                    if (GUILayout.Button(BuildCellLabel(x, y), GUILayout.Width(CellWidth), GUILayout.Height(CellHeight)))
                    {
                        if (_eraseMode)
                        {
                            RemoveCannonAt(x, y);
                        }
                        else
                        {
                            UpsertCannon(x, y, _paintColor, _paintShots, _paintShootRate);
                        }
                    }

                    GUI.backgroundColor = previousColor;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private string BuildCellLabel(int x, int y)
        {
            CannonSpawnData cannon = _levelData.cannons
                .FirstOrDefault(candidate => candidate.x == x && candidate.y == y);

            if (cannon == null)
            {
                return string.Format("{0},{1}", x, y);
            }

            return string.Format(
                "{0},{1}\n{2}:{3} {4:0.#}/s",
                x,
                y,
                cannon.color.ToString()[0],
                cannon.shots,
                cannon.shootRate);
        }

        private Color GetCellPreviewColor(int x, int y)
        {
            CannonSpawnData cannon = _levelData.cannons
                .FirstOrDefault(candidate => candidate.x == x && candidate.y == y);

            if (cannon == null)
            {
                return Color.white;
            }

            Color color = _blockDatabase != null
                ? _blockDatabase.GetDefinitionOrFallback(cannon.color).color
                : ColorBlockDefinition.GetFallbackColor(cannon.color);

            return Color.Lerp(Color.white, color, 0.65f);
        }

        private void UpsertCannon(int x, int y, BlockColorKey color, int shots, float shootRate)
        {
            CannonSpawnData cannon = _levelData.cannons.FirstOrDefault(
                candidate => candidate.x == x && candidate.y == y);

            if (cannon == null)
            {
                cannon = new CannonSpawnData
                {
                    x = x,
                    y = y
                };
                _levelData.cannons.Add(cannon);
            }

            cannon.color = color;
            cannon.shots = Mathf.Max(0, shots);
            cannon.shootRate = shootRate > 0f ? shootRate : CannonSpawnData.DefaultShootRate;
            SortCannons();
        }

        private void RemoveCannonAt(int x, int y)
        {
            CannonSpawnData cannon = _levelData.cannons.FirstOrDefault(
                candidate => candidate.x == x && candidate.y == y);

            if (cannon != null)
            {
                _levelData.cannons.Remove(cannon);
            }
        }

        private void SortCannons()
        {
            _levelData.cannons = _levelData.cannons
                .OrderBy(cannon => cannon.x)
                .ThenBy(cannon => cannon.y)
                .ToList();
        }

        private void LoadSelectedJson()
        {
            if (_sourceJson == null)
            {
                return;
            }

            _levelData = _jsonParser.FromJson(_sourceJson.text);
            SortCannons();
        }

        private void SaveJson()
        {
            SortCannons();

            string path = EditorUtility.SaveFilePanelInProject(
                "Save Block Crush Cannons",
                string.IsNullOrWhiteSpace(_levelData.id) ? "Cannons" : _levelData.id,
                "json",
                "Choose where to save the cannon JSON.",
                "Assets");

            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            File.WriteAllText(path, _jsonParser.ToJson(_levelData, true));
            AssetDatabase.ImportAsset(path);
            _sourceJson = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        }

        private float GetSlotIndent()
        {
            float gridWidth = _levelData.width * CellWidth;
            float slotsWidth = _levelData.slotCount * CellWidth;
            return Mathf.Max(0f, (gridWidth - slotsWidth) * 0.5f);
        }
    }
}
