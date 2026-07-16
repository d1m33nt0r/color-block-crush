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
    public sealed class LevelEditorWindow : EditorWindow
    {
        private const float CellWidth = 70f;
        private const float CellHeight = 42f;

        private readonly LevelJsonParser _jsonParser = new LevelJsonParser();
        private LevelData _levelData = new LevelData();
        private TextAsset _sourceJson;
        private ColorBlockDatabase _blockDatabase;
        private BlockColorKey _paintColor = BlockColorKey.Red;
        private int _paintLayer;
        private bool _eraseMode;
        private Vector2 _scroll;

        [MenuItem("Tools/Block Crush/Level JSON Editor")]
        public static void Open()
        {
            GetWindow<LevelEditorWindow>("Block Crush Levels");
        }

        private void OnGUI()
        {
            DrawToolbar();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawLevelSettings();
            EditorGUILayout.Space(8f);
            DrawPaintSettings();
            EditorGUILayout.Space(8f);
            DrawGrid();
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(60f)))
            {
                _levelData = new LevelData();
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
            EditorGUILayout.LabelField("Level", EditorStyles.boldLabel);
            _levelData.id = EditorGUILayout.TextField("Id", _levelData.id);
            _levelData.width = Mathf.Max(1, EditorGUILayout.IntField("Width", _levelData.width));
            _levelData.height = Mathf.Max(1, EditorGUILayout.IntField("Height", _levelData.height));
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
            _paintLayer = Mathf.Max(0, EditorGUILayout.IntField("Layer", _paintLayer));
            _eraseMode = GUILayout.Toggle(_eraseMode, "Erase", EditorStyles.miniButton, GUILayout.Width(70f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Sort Blocks", GUILayout.Width(110f)))
            {
                SortBlocks();
            }

            if (GUILayout.Button("Clear All", GUILayout.Width(90f)))
            {
                if (EditorUtility.DisplayDialog("Clear level", "Remove every block from this level?", "Clear", "Cancel"))
                {
                    _levelData.blocks.Clear();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawGrid()
        {
            EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);

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
                            RemoveBlockAt(x, y, _paintLayer);
                        }
                        else
                        {
                            UpsertBlock(x, y, _paintLayer, _paintColor);
                        }
                    }

                    GUI.backgroundColor = previousColor;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private string BuildCellLabel(int x, int y)
        {
            List<BlockSpawnData> cellBlocks = _levelData.blocks
                .Where(block => block.x == x && block.y == y)
                .OrderBy(block => block.layer)
                .ToList();

            if (cellBlocks.Count == 0)
            {
                return string.Format("{0},{1}", x, y);
            }

            string stack = string.Join(" ", cellBlocks.Select(block => block.layer + ":" + block.color.ToString()[0]));
            return string.Format("{0},{1}\n{2}", x, y, stack);
        }

        private Color GetCellPreviewColor(int x, int y)
        {
            BlockSpawnData topBlock = _levelData.blocks
                .Where(block => block.x == x && block.y == y)
                .OrderByDescending(block => block.layer)
                .FirstOrDefault();

            if (topBlock == null)
            {
                return Color.white;
            }

            Color color = _blockDatabase != null
                ? _blockDatabase.GetDefinitionOrFallback(topBlock.color).color
                : ColorBlockDefinition.GetFallbackColor(topBlock.color);

            return Color.Lerp(Color.white, color, 0.65f);
        }

        private void UpsertBlock(int x, int y, int layer, BlockColorKey color)
        {
            BlockSpawnData block = _levelData.blocks.FirstOrDefault(
                candidate => candidate.x == x && candidate.y == y && candidate.layer == layer);

            if (block == null)
            {
                block = new BlockSpawnData
                {
                    x = x,
                    y = y,
                    layer = layer
                };
                _levelData.blocks.Add(block);
            }

            block.color = color;
            SortBlocks();
        }

        private void RemoveBlockAt(int x, int y, int layer)
        {
            BlockSpawnData block = _levelData.blocks.FirstOrDefault(
                candidate => candidate.x == x && candidate.y == y && candidate.layer == layer);

            if (block != null)
            {
                _levelData.blocks.Remove(block);
            }
        }

        private void SortBlocks()
        {
            _levelData.blocks = _levelData.blocks
                .OrderBy(block => block.x)
                .ThenBy(block => block.y)
                .ThenBy(block => block.layer)
                .ToList();
        }

        private void LoadSelectedJson()
        {
            if (_sourceJson == null)
            {
                return;
            }

            _levelData = _jsonParser.FromJson(_sourceJson.text);
            SortBlocks();
        }

        private void SaveJson()
        {
            SortBlocks();

            string path = EditorUtility.SaveFilePanelInProject(
                "Save Block Crush Level",
                string.IsNullOrWhiteSpace(_levelData.id) ? "Level" : _levelData.id,
                "json",
                "Choose where to save the level JSON.",
                "Assets");

            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            File.WriteAllText(path, _jsonParser.ToJson(_levelData, true));
            AssetDatabase.ImportAsset(path);
            _sourceJson = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        }
    }
}
