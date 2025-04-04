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
            var result3d = new List<Vector3>();
            foreach (var geometryPoint in geometry.coordinates)
            {
                float lon = geometryPoint[0];
                float lat = geometryPoint[1];
                var mvtIndex = Helpers.LatLonToMvt(lon, lat, _zoomLevel);
                var tileX = (float)(mvtIndex.tileX - _originTileIndexX);
                var tileY = (float)(mvtIndex.tileY - _originTileIndexY - 1);
                result.Add(new(tileX * _tileSize, -tileY * _tileSize));
                result3d.Add(new(tileX * _tileSize, 0f, -tileY * _tileSize));
            }

            RenderLine(result3d);
        }

        /** Turns a list of points into a game object. Inputs given as unity coordinates. */
        private void RenderLine(List<Vector3> points)
        {
            var lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.name = name;
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = points.Count;
            lineRenderer.SetPositions(points.ToArray());
            Material material = new Material(Shader.Find("Unlit/Color"));
            material.color = Color.black;
            lineRenderer.material = material;
        }
    }
}
