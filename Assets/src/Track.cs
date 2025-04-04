using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace src
{
    public class Track : MonoBehaviour
    {
        private float _lineSize;
        private List<Vector3> _points;
        private LineRenderer _lineRenderer;
        private Camera _mainCamera;

        public float maxScaleDistance = 50.0f;
        public float maxScaleFactor = 100.0f;

        public static Track CreateTrack(
            GameObject parent,
            List<Vector3> points,
            String name,
            float lineSize
        )
        {
            var trackObject = new GameObject(name);
            trackObject.transform.SetParent(parent.transform, true);
            var track = trackObject.AddComponent<Track>();
            track._lineSize = lineSize;
            track._points = points;
            track.RenderLine();
            track._mainCamera = Camera.main;
            return track;
        }

        /** Turns a list of points into a game object. Inputs given as unity coordinates. */
        private void RenderLine()
        {
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.name = name;
            _lineRenderer.positionCount = 2;
            _lineRenderer.startWidth = _lineSize;
            _lineRenderer.endWidth = _lineSize;
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.positionCount = _points.Count;
            _lineRenderer.SetPositions(_points.ToArray());
            Material material = new Material(Shader.Find("Unlit/Color"));
            material.color = Color.black;
            _lineRenderer.material = material;
        }

        void Update()
        {
            // Scale the lines to be thicker when far away
            if (_mainCamera != null && _lineRenderer != null)
            {
                float distance = _points.Min(point =>
                    Vector3.Distance(_mainCamera.transform.position, point)
                );
                float scaleFactor = Mathf.Clamp01(distance / maxScaleDistance);
                float newWidth = Mathf.Lerp(_lineSize, _lineSize * maxScaleFactor, scaleFactor);

                _lineRenderer.startWidth = newWidth;
                _lineRenderer.endWidth = newWidth;
            }
        }
    }
}
