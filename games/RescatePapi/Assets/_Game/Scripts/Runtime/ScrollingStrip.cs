using System.Collections.Generic;
using UnityEngine;

namespace RescatePapi
{
    /// <summary>
    /// Tira horizontal que se repite y se desplaza con la distancia recorrida (suelo y fondos con parallax).
    /// factor 1 = se mueve igual que el mundo; 0.1 = muy lejos (se mueve poco).
    /// </summary>
    public sealed class ScrollingStrip : MonoBehaviour
    {
        readonly List<Transform> _tiles = new List<Transform>();
        float _tileWidth, _factor, _startX;

        public static ScrollingStrip Create(string name, Sprite sprite, float bottomY, float factor, int order, float viewHalfWidth)
        {
            var go = new GameObject(name);
            var strip = go.AddComponent<ScrollingStrip>();
            strip._tileWidth = sprite.bounds.size.x;
            strip._factor = factor;
            int count = Mathf.CeilToInt(viewHalfWidth * 2f / strip._tileWidth) + 2;
            strip._startX = -viewHalfWidth - strip._tileWidth * 0.5f;
            for (int i = 0; i < count; i++)
            {
                var sr = new GameObject($"{name}_{i}").AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = order;
                sr.transform.SetParent(go.transform, false);
                sr.transform.localPosition = new Vector3(0f, bottomY, 0f);
                strip._tiles.Add(sr.transform);
            }
            strip.SetDistance(0f);
            return strip;
        }

        public void SetDistance(float distance)
        {
            float offset = -Mathf.Repeat(distance * _factor, _tileWidth);
            for (int i = 0; i < _tiles.Count; i++)
            {
                var p = _tiles[i].localPosition;
                p.x = _startX + offset + i * _tileWidth;
                _tiles[i].localPosition = p;
            }
        }
    }
}
