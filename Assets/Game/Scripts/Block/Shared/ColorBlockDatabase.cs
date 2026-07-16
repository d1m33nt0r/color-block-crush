using System;
using System.Collections.Generic;
using Game.BlockCrush.Block;
using UnityEngine;

namespace Game.BlockCrush.Block.Shared
{
    [Serializable]
    public sealed class ColorBlockDefinition
    {
        public BlockColorKey key;
        public Color color = Color.white;
        public Material material;
        public ColorBlock prefab;
        [Min(0.01f)] public float height = 1f;

        public static ColorBlockDefinition CreateFallback(BlockColorKey key)
        {
            return new ColorBlockDefinition
            {
                key = key,
                color = GetFallbackColor(key),
                height = 1f
            };
        }

        public static Color GetFallbackColor(BlockColorKey key)
        {
            switch (key)
            {
                case BlockColorKey.Red:
                    return new Color(0.95f, 0.14f, 0.18f);
                case BlockColorKey.Green:
                    return new Color(0.18f, 0.75f, 0.29f);
                case BlockColorKey.Blue:
                    return new Color(0.16f, 0.38f, 0.95f);
                case BlockColorKey.Yellow:
                    return new Color(1f, 0.85f, 0.12f);
                case BlockColorKey.Orange:
                    return new Color(1f, 0.48f, 0.11f);
                case BlockColorKey.Purple:
                    return new Color(0.56f, 0.23f, 0.95f);
                case BlockColorKey.Cyan:
                    return new Color(0.1f, 0.82f, 0.9f);
                case BlockColorKey.Pink:
                    return new Color(1f, 0.35f, 0.66f);
                default:
                    return Color.white;
            }
        }
    }

    [CreateAssetMenu(fileName = "ColorBlockDatabase", menuName = "Game/Block Crush/Color Block Database")]
    public sealed class ColorBlockDatabase : ScriptableObject
    {
        [SerializeField] private List<ColorBlockDefinition> _definitions = new List<ColorBlockDefinition>();

        public IReadOnlyList<ColorBlockDefinition> Definitions
        {
            get { return _definitions; }
        }

        public bool TryGetDefinition(BlockColorKey key, out ColorBlockDefinition definition)
        {
            for (int i = 0; i < _definitions.Count; i++)
            {
                ColorBlockDefinition candidate = _definitions[i];
                if (candidate != null && candidate.key == key)
                {
                    definition = candidate;
                    return true;
                }
            }

            definition = null;
            return false;
        }

        public ColorBlockDefinition GetDefinitionOrFallback(BlockColorKey key)
        {
            if (TryGetDefinition(key, out ColorBlockDefinition definition))
            {
                return definition;
            }

            return ColorBlockDefinition.CreateFallback(key);
        }
    }
}
