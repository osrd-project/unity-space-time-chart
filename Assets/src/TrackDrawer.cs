using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Mapbox.Vector.Tile;
using UnityEngine;
using UnityEngine.Networking;

namespace src
{
    public class TrackDrawer : MonoBehaviour
    {
        private float _lineSize;
        private float _tileSize;

        public static TrackDrawer CreateTrackDrawer(GameObject gameObject, int zoomLevel, int x, int y, int infraId, String url, float lineSize, float tileSize, Vector2 tileOrigin)
        {
            var trackDrawer = gameObject.AddComponent<TrackDrawer>();
            trackDrawer._tileSize = tileSize;
            trackDrawer._lineSize = lineSize;
            trackDrawer.StartCoroutine(trackDrawer.FetchTile(url, zoomLevel, x, y, infraId, tileOrigin));
            return trackDrawer;
        }

        /** Sends the request to editoast and create any track. */
        IEnumerator FetchTile(string url, int zoom, int x, int y, int infra, Vector2 tileOrigin)
        {
            string formattedUrl = url.Replace("{z}", zoom.ToString())
                .Replace("{x}", x.ToString())
                .Replace("{y}", y.ToString())
                .Replace("{infra}", infra.ToString());

            using (UnityWebRequest request = UnityWebRequest.Get(formattedUrl))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    byte[] tileData = request.downloadHandler.data;
                    ParseTile(tileData, tileOrigin);
                }
                else
                {
                    Debug.LogError("Failed to fetch tile: " + request.error);
                }
            }
        }

        /** Parse the raw editoast response and create the game object. */
        void ParseTile(byte[] tileData, Vector2 tileOrigin)
        {
            if (tileData != null && tileData.Length > 0)
            {
                Stream stream = new MemoryStream(tileData);
                var layerInfos = VectorTileParser.Parse(stream);
                var layer = layerInfos.Find(l => l.Name == "track_sections");
                foreach (var track in layer.VectorTileFeatures)
                {
                    var geometry = track.Geometry[0];
                    var points = new List<Vector3>();
                    foreach (var p in geometry)
                    {
                        points.Add(
                            new(
                                tileOrigin.x + p.X * _tileSize / 4096f,
                                0,
                                tileOrigin.y + _tileSize * (1 - p.Y / 4096f)
                            )
                        );
                    }
                    RenderLine(
                        points,
                        track.Attributes.Find(entry => entry.Key == "id").Value.ToString()
                    );
                }
            }
        }

        /** Turns a list of points into a game object. Inputs given as unity coordinates. */
        private void RenderLine(List<Vector3> points, String name)
        {
            var child = new GameObject();
            var lineRenderer = child.AddComponent<LineRenderer>();
            lineRenderer.name = name;
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = _lineSize;
            lineRenderer.endWidth = _lineSize;
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = points.Count;
            lineRenderer.SetPositions(points.ToArray());
            Material material = new Material(Shader.Find("Unlit/Color"));
            material.color = Color.black;
            lineRenderer.material = material;
        }
    }
}