using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mapbox.Vector.Tile;
using UnityEngine;
using UnityEngine.Networking;

public class CreateLine : MonoBehaviour
{
    public string editoastUrl = "http://localhost:4000/";
    public int? infraId = null;
    public int? defaultZoom = null;
    public int? defaultX = null; // TODO: actually iterate and get the right tiles
    public int? defaultY = null;

    private List<LineRenderer> _lineRenderers = new();

    void Start()
    {
        string tileUrl =
            "{editoast_url}api/layers/tile/track_sections/geo/{z}/{x}/{y}/?infra={infra}".Replace(
                "{editoast_url}",
                editoastUrl
            );
        StartCoroutine(
            FetchTile(tileUrl, defaultZoom ?? 10, defaultX ?? 511, defaultY ?? 349, infraId ?? 1)
        );
    }

    IEnumerator FetchTile(string url, int zoom, int x, int y, int infra)
    {
        string formattedUrl = url.Replace("{z}", zoom.ToString())
            .Replace("{x}", x.ToString())
            .Replace("{y}", y.ToString())
            .Replace("{infra}", infra.ToString());
        Debug.Log("fetching data at " + formattedUrl);

        using (UnityWebRequest request = UnityWebRequest.Get(formattedUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                byte[] tileData = request.downloadHandler.data;
                ParseTile(tileData);
            }
            else
            {
                Debug.LogError("Failed to fetch tile: " + request.error);
            }
        }
    }

    void ParseTile(byte[] tileData)
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
                var mult = 1e-2f;
                points.Add(new(p.X * mult, 0, p.Y * mult - 39));
            }

            var child = new GameObject();
            var lineRenderer = child.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = 0.01f;
            lineRenderer.endWidth = 0.01f;
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = points.Count;
            lineRenderer.SetPositions(points.ToArray());
            Material whiteDiffuseMat = new Material(Shader.Find("Unlit/Texture"));
            lineRenderer.material = whiteDiffuseMat;
            Debug.Log("New line: " + String.Join(", ", points.Select(p => p.ToString()).ToArray()));
            _lineRenderers.Add(lineRenderer);
        }
    }
}
