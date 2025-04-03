using System.Collections;
using System.Collections.Generic;
using System.IO;
using Mapbox.Vector.Tile;
using UnityEngine;
using UnityEngine.Networking;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class CreateLine : MonoBehaviour
{
    public string editoastUrl = "http://localhost:4000/";
    public int infraId = 1;
    public float lineSize = 0.03f;

    private List<LineRenderer> _lineRenderers = new();

    void Start()
    {
        string tileUrl =
            "{editoast_url}api/layers/tile/track_sections/geo/{z}/{x}/{y}/?infra={infra}".Replace(
                "{editoast_url}",
                editoastUrl
            );
        for (var x = -10; x < 10; x++)
        {
            for (var y = -10; y < 10; y++)
            {
                var tileSize = 100f;
                var origin = new Vector2(tileSize * x, tileSize * y);
                StartCoroutine(
                    FetchTile(
                        tileUrl,
                        10,
                        511 + x,
                        349 + y,
                        infraId,
                        origin,
                        new(tileSize, tileSize)
                    )
                );
            }
        }
    }

    IEnumerator FetchTile(
        string url,
        int zoom,
        int x,
        int y,
        int infra,
        Vector2 tileOrigin,
        Vector2 tileSize
    )
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
                if (tileData != null && tileData.Length > 0)
                    ParseTile(tileData, tileOrigin, tileSize);
            }
            else
            {
                Debug.LogError("Failed to fetch tile: " + request.error);
            }
        }
    }

    void ParseTile(byte[] tileData, Vector2 tileOrigin, Vector2 tileSize)
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
                        tileOrigin.x + p.X * tileSize.x / 4096f,
                        0,
                        tileOrigin.y + tileSize.y * (1 - p.Y / 4096f)
                    )
                );
            }

            var child = new GameObject();
            var lineRenderer = child.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = lineSize;
            lineRenderer.endWidth = lineSize;
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = points.Count;
            lineRenderer.SetPositions(points.ToArray());
            Material whiteDiffuseMat = new Material(Shader.Find("Unlit/Texture"));
            lineRenderer.material = whiteDiffuseMat;
            _lineRenderers.Add(lineRenderer);
        }
    }
}
