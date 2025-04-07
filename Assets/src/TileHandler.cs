using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace src
{
    public class TileHandler : MonoBehaviour
    {
        public bool useMapBackground = true;

        // Cache location for map background (saving API calls)
        string cacheDirectory = "Assets/TextureCache/";
        private float _tileSize;
        private string _tileUrl;

        public static TileHandler CreateTileHandler(
            GameObject gameObject,
            int zoomLevel,
            int x,
            int y,
            int infraId,
            Vector2 origin,
            float tileSize
        )
        {
            var tileHandler = gameObject.AddComponent<TileHandler>();
            tileHandler._tileSize = tileSize;
            tileHandler.RenderTileFloor(origin, zoomLevel, x, y);
            return tileHandler;
        }

        /** Renders a large square under loaded area.
         * If a mapbox key is set, it displays a map of the actual area.
         * Otherwise, it still keeps track of which area are loaded.
         */
        private void RenderTileFloor(Vector2 origin, int zoom, int x, int y)
        {
            Vector3[] vertices = new Vector3[4];
            var height = -0.1f;
            vertices[0] = new Vector3(origin.x, height, origin.y + _tileSize); // Bottom left
            vertices[1] = new Vector3(origin.x + _tileSize, height, origin.y + _tileSize); // Bottom right
            vertices[2] = new Vector3(origin.x, height, origin.y); // Top left
            vertices[3] = new Vector3(origin.x + _tileSize, height, origin.y); // Top right

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
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
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
            var coordinates = Helpers.MvtToLatLonBounds(zoom, x, y);
            string fileName = Path.GetFileName($"{zoom}-{x}-{y}.png");
            string cachePath = Path.Combine(cacheDirectory, fileName);

            if (File.Exists(cachePath))
            {
                byte[] fileData = File.ReadAllBytes(cachePath);
                texture.LoadImage(fileData);
                callback(texture);
            }
            else if (File.Exists(keyFile) && !File.ReadAllText(keyFile).Contains("API key"))
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
                        Debug.LogError(
                            "Failed to fetch tile image: at " + url + ": " + request.error
                        );
                    }
                }
            }
        }
    }
}
