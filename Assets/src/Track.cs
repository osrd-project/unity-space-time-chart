using System;
using System.Collections.Generic;
using UnityEngine;

namespace src
{
    public class Track : MonoBehaviour
    {
        private float _lineSize = 0.02f;
        private List<Vector3> _points;

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
            return track;
        }

        /** Turns a list of points into a game object. Inputs given as unity coordinates. */
        private void RenderLine()
        {
            var lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.name = name;
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = _lineSize;
            lineRenderer.endWidth = _lineSize;
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = _points.Count;
            lineRenderer.SetPositions(_points.ToArray());
            Material material = new Material(Shader.Find("Unlit/Color"));
            material.color = Color.black;
            lineRenderer.material = material;
        }
    }
}
