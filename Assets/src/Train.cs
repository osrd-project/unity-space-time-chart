using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace src
{
    public class Train : MonoBehaviour
    {
        private String _editoastUrl;
        private int _id;
        private int _infraId;
        private int _zoomLevel;
        private int _originTileIndexX;
        private int _originTileIndexY;
        private float _tileSize;
        private List<Vector2> _geoPoints;

        public static Train CreateTrain(
            GameObject parent,
            String editoastUrl,
            int trainId,
            int infraId,
            int zoomLevel,
            int originTileIndexX,
            int originTileIndexY,
            float tileSize
        )
        {
            GameObject newObj = new GameObject($"train-{trainId}");
            newObj.transform.parent = parent.transform;
            Train train = newObj.AddComponent<Train>();
            train._editoastUrl = editoastUrl;
            train._id = trainId;
            train._infraId = infraId;
            train._zoomLevel = zoomLevel;
            train._originTileIndexX = originTileIndexX;
            train._originTileIndexY = originTileIndexY;
            train._tileSize = tileSize;
            train.StartCoroutine(train.GetGeoPoints());
            return train;
        }

        private IEnumerator GetGeoPoints()
        {
            dynamic pathResponse = null;
            var pathUrl = $"{_editoastUrl}api/train_schedule/{_id}/path?infra_id={_infraId}";
            yield return Helpers.GetJson(pathUrl, result => pathResponse = result);
            var tracks = pathResponse.track_section_ranges;

            var pathPropsUrl =
                $"{_editoastUrl}api/infra/{_infraId}/path_properties?props[]=geometry";
            dynamic inputPayload = new System.Dynamic.ExpandoObject();
            inputPayload.track_section_ranges = tracks;
            dynamic pathPropsResponse = null;
            Action<dynamic> callback = result => pathPropsResponse = result;
            yield return Helpers.PostJson(pathPropsUrl, inputPayload, callback);

            var geometry = pathPropsResponse.geometry;

            var result = new List<Vector2>();
            foreach (var geometryPoint in geometry.coordinates)
            {
                float lon = geometryPoint[0];
                float lat = geometryPoint[1];
                var mvtIndex = Helpers.LatLonToMvt(lon, lat, _zoomLevel);
                var tileX = (float)(mvtIndex.tileX - _originTileIndexX);
                var tileY = (float)(mvtIndex.tileY - _originTileIndexY - 1);
                result.Add(new(tileX * _tileSize, -tileY * _tileSize));
            }

            Render(result, 1f, 2f);
        }

        private void Render(List<Vector2> points, float minHeight, float maxHeight)
        {
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();

            Mesh mesh = new Mesh();

            int vertexCount = points.Count * 2;
            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            int[] triangles = new int[(points.Count - 1) * 12];

            for (int i = 0; i < points.Count; i++)
            {
                // Bottom vertices
                vertices[i * 2] = new Vector3(points[i].x, minHeight, points[i].y);
                // Top vertices
                vertices[i * 2 + 1] = new Vector3(points[i].x, maxHeight, points[i].y);

                uvs[i * 2] = new Vector2(0, 0);
                uvs[i * 2 + 1] = new Vector2(0, 1);
            }

            for (int i = 0; i < points.Count - 1; i++)
            {
                int index = i * 6;
                int vertIndex = i * 2;

                triangles[index] = vertIndex;
                triangles[index + 1] = vertIndex + 1;
                triangles[index + 2] = vertIndex + 2;

                triangles[index + 3] = vertIndex + 2;
                triangles[index + 4] = vertIndex + 1;
                triangles[index + 5] = vertIndex + 3;
            }

            // Double the triangles the other way around,
            // to make the mesh visible from either side
            for (int i = 0; i < points.Count - 1; i++)
            {
                int index = triangles.Length / 2 + i * 6;
                int vertIndex = i * 2;

                triangles[index] = vertIndex + 2;
                triangles[index + 1] = vertIndex + 1;
                triangles[index + 2] = vertIndex;

                triangles[index + 3] = vertIndex + 3;
                triangles[index + 4] = vertIndex + 1;
                triangles[index + 5] = vertIndex + 2;
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;

            Material material = new Material(Shader.Find("UI/Unlit/Transparent"));
            material.color = new Color(1, 0, 0, 0.2f);
            meshRenderer.material = material;
        }
    }
}
