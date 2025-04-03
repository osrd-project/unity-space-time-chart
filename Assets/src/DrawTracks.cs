using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Mapbox.Vector.Tile;
using src;
using UnityEngine;
using UnityEngine.Networking;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class CreateLine : MonoBehaviour
{
    public string editoastUrl = "http://localhost:4000/";
    public int infraId = 2;
    public float lineSize = 0.03f;
    public float tileSize = 100.0f;
    public int zoomLevel = 10; // As per MVT specs
    public int tileSightDistance = 2;

    // Centers the game origin to a place near small infra
    public int originTileIndexX = 527;
    public int originTileIndexY = 364;

    public bool useMapBackground = true;

    // Cache location for map background (saving API calls)
    string cacheDirectory = "Assets/TextureCache/";

    private List<LineRenderer> _lineRenderers = new();
    private HashSet<Tuple<int, int>> _loadedTiles = new();

    void Start()
    {
        if (!Directory.Exists(cacheDirectory))
        {
            Directory.CreateDirectory(cacheDirectory);
        }

        StartCoroutine(LoadTiles());
    }

    /** Infinite loop that keeps loading the most relevant tile, if any. Does not interrupt game engine (unity coroutine). */
    IEnumerator LoadTiles()
    {
        string tileUrl =
            "{editoast_url}api/layers/tile/track_sections/geo/{z}/{x}/{y}/?infra={infra}".Replace(
                "{editoast_url}",
                editoastUrl
            );
        while (true)
        {
            var nextTileIndex = FindNextTile();
            if (nextTileIndex != null)
            {
                var x = nextTileIndex.Item1;
                var y = nextTileIndex.Item2;
                _loadedTiles.Add(Tuple.Create(x, y));
                var origin = new Vector2(tileSize * x, -tileSize * y);
                StartCoroutine(
                    FetchTile(
                        tileUrl,
                        zoomLevel,
                        originTileIndexX + x,
                        originTileIndexY + y,
                        infraId,
                        origin
                    )
                );
            }
            yield return new WaitForSeconds(.1f); // Limit editoast requests to 10/s
        }
    }

    /** Returns the next best tile coordinates to load, based on the camera position. */
    [CanBeNull]
    Tuple<int, int> FindNextTile()
    {
        var position = transform.position;
        int x0 = (int)(position.x / tileSize);
        int y0 = (int)(-position.z / tileSize);

        for (int distance = 0; distance <= tileSightDistance; distance++)
        {
            for (int dx = -distance; dx <= distance; dx++)
            {
                for (int dy = -distance; dy <= distance; dy++)
                {
                    int x = x0 + dx;
                    int y = y0 + dy;

                    Tuple<int, int> candidate = Tuple.Create(x, y);
                    if (!_loadedTiles.Contains(candidate))
                        return candidate;
                }
            }
        }

        return null;
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
                ParseTile(tileData, tileOrigin, zoom, x, y);
            }
            else
            {
                Debug.LogError("Failed to fetch tile: " + request.error);
            }
        }
    }

    /** Parse the raw editoast response and create the game object. */
    void ParseTile(byte[] tileData, Vector2 tileOrigin, int zoom, int x, int y)
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
                            tileOrigin.x + p.X * tileSize / 4096f,
                            0,
                            tileOrigin.y + tileSize * (1 - p.Y / 4096f)
                        )
                    );
                }
                RenderLine(
                    points,
                    track.Attributes.Find(entry => entry.Key == "id").Value.ToString()
                );
            }
        }
        RenderTileFloor(tileOrigin, zoom, x, y);
    }

    /** Turns a list of points into a game object. Inputs given as unity coordinates. */
    private void RenderLine(List<Vector3> points, String name)
    {
        var child = new GameObject();
        var lineRenderer = child.AddComponent<LineRenderer>();
        lineRenderer.name = name;
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = lineSize;
        lineRenderer.endWidth = lineSize;
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
        Material material = new Material(Shader.Find("Unlit/Color"));
        material.color = Color.black;
        lineRenderer.material = material;
        _lineRenderers.Add(lineRenderer);
    }

    /** Renders a large square under loaded area.
     * If a mapbox key is set, it displays a map of the actual area.
     * Otherwise, it still keeps track of which area are loaded.
     */
    private void RenderTileFloor(Vector2 origin, int zoom, int x, int y)
    {
        var name = "floor_tile:" + x + "," + y;
        Vector3[] vertices = new Vector3[4];
        var height = -0.2f;
        vertices[0] = new Vector3(origin.x, height, origin.y + tileSize); // Bottom left
        vertices[1] = new Vector3(origin.x + tileSize, height, origin.y + tileSize); // Bottom right
        vertices[2] = new Vector3(origin.x, height, origin.y); // Top left
        vertices[3] = new Vector3(origin.x + tileSize, height, origin.y); // Top right

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = new[] { 1, 2, 0, 1, 3, 2 };

        Vector3[] normals =
        {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
        };
        mesh.normals = normals;

        Vector2[] uv = { new(0, 1), new(1, 1), new(0, 0), new(1, 0) };
        mesh.uv = uv;

        mesh.RecalculateBounds();

        Material material = new Material(Shader.Find("Unlit/Texture"));
        GameObject quad = new GameObject(name);
        MeshFilter meshFilter = quad.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        MeshRenderer meshRenderer = quad.AddComponent<MeshRenderer>();
        meshRenderer.material = material;
        if (useMapBackground)
            StartCoroutine(LoadTexture(zoom, x, y, t => material.mainTexture = t));
    }

    /** Fetch a map texture for the given tile. */
    public IEnumerator LoadTexture(int zoom, int x, int y, Action<Texture2D> callback)
    {
        var keyFile = "mapbox.key";
        var textureSize = 200;
        Texture2D texture = new Texture2D(textureSize, textureSize);
        var coordinates = MvtToLatLon.MvtToLatLonBounds(zoom, x, y);
        string fileName = Path.GetFileName($"{zoom}-{x}-{y}.png");
        string cachePath = Path.Combine(cacheDirectory, fileName);

        if (File.Exists(cachePath))
        {
            byte[] fileData = File.ReadAllBytes(cachePath);
            texture.LoadImage(fileData);
            callback(texture);
        }
        else if (File.Exists(keyFile))
        {
            var apiKey = File.ReadAllText(keyFile);
            var template =
                "https://api.mapbox.com/styles/v1/mapbox/outdoors-v12/static/[{0},{1},{2},{3}]/{4}x{4}?access_token={5}";
            var url = String.Format(
                template,
                coordinates.lonLeft,
                coordinates.latTop,
                coordinates.lonRight,
                coordinates.latBottom,
                textureSize,
                apiKey
            );
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    byte[] tileData = request.downloadHandler.data;
                    texture.LoadImage(tileData);
                    File.WriteAllBytes(cachePath, tileData);
                    callback(texture);
                }
                else
                {
                    Debug.LogError("Failed to fetch tile image: " + request.error);
                }
            }
        }
    }
}
