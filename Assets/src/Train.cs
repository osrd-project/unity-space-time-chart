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
        private PathData? _pathData;
        private List<OccupancyBlock> _occupancyBlocks;
        private DateTime _timeOrigin;
        private TrainLoader _trainLoader;

        private float _scaleSecondsPerMeter;
        private bool _rendered = false;

        private struct PathData
        {
            public dynamic TrackSections;
            public dynamic Blocks;
            public dynamic Routes;
            public int Length;

            public PathData(dynamic trackSections, dynamic blocks, dynamic routes, int length)
            {
                TrackSections = trackSections;
                Blocks = blocks;
                Routes = routes;
                Length = length;
            }
        }

        private struct OccupancyBlock
        {
            public float StartTime;
            public float EndTime;
            public float StartOffset;
            public float EndOffset;
            public Color Color;

            public OccupancyBlock(
                float startTime,
                float endTime,
                float startOffset,
                float endOffset,
                Color color
            )
            {
                StartTime = startTime;
                EndTime = endTime;
                StartOffset = startOffset;
                EndOffset = endOffset;
                Color = color;
            }
        }

        public static Train CreateTrain(
            GameObject parent,
            String editoastUrl,
            int trainId,
            int infraId,
            int zoomLevel,
            int originTileIndexX,
            int originTileIndexY,
            float tileSize,
            DateTime timeOrigin,
            float scale
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
            train._timeOrigin = timeOrigin;
            train._trainLoader = parent.GetComponent<TrainLoader>();
            train._scaleSecondsPerMeter = scale;
            train.StartCoroutine(train.Run());
            return train;
        }

        public void Update()
        {
            if (_rendered)
                if (transform.position.y != _trainLoader.currentVerticalOffset)
                    transform.position = new Vector3(0, _trainLoader.currentVerticalOffset, 0);
        }

        private IEnumerator Run()
        {
            yield return GetGeoPoints();
            yield return GetSpaceTimeData();
            if (_occupancyBlocks == null || _geoPoints == null)
            {
                Destroy(gameObject);
                yield break;
            }
            foreach (var block in _occupancyBlocks)
            {
                RenderOccupancy(block);
                yield return new WaitForSeconds(0.01f);
            }

            _rendered = true;
        }

        private void RenderOccupancy(OccupancyBlock block)
        {
            var points = Helpers.SliceLine(_geoPoints, block.StartOffset, block.EndOffset);
            var start = block.StartTime / _scaleSecondsPerMeter;
            var end = block.EndTime / _scaleSecondsPerMeter;

            Render(points, start, end, block.Color);
        }

        private IEnumerator GetSpaceTimeData()
        {
            yield return LoadPathData();
            var projectPathUrl = $"{_editoastUrl}api/train_schedule/project_path";
            dynamic inputPayload = new System.Dynamic.ExpandoObject();
            inputPayload.ids = new[] { _id };
            inputPayload.infra_id = _infraId;
            inputPayload.path = new System.Dynamic.ExpandoObject();
            inputPayload.path.blocks = _pathData?.Blocks;
            inputPayload.path.routes = _pathData?.Routes;
            inputPayload.path.track_section_ranges = _pathData?.TrackSections;
            dynamic projectionResponse = null;
            Action<dynamic> callback = result => projectionResponse = result;
            yield return Helpers.PostJson(projectPathUrl, inputPayload, callback);
            if (projectionResponse == null)
                yield break;

            dynamic trainResponse = projectionResponse[_id.ToString()];
            DateTime departure = trainResponse.departure_time;
            var departureTimeDelta = departure - _timeOrigin;
            _occupancyBlocks = new List<OccupancyBlock>();
            foreach (var signalUpdate in trainResponse.signal_updates)
                _occupancyBlocks.Add(
                    ParseSignalUpdate(signalUpdate, departureTimeDelta.TotalMilliseconds)
                );
        }

        private OccupancyBlock ParseSignalUpdate(dynamic signalUpdate, double departureTimeDelta)
        {
            int timeStart = signalUpdate.time_start + departureTimeDelta;
            int timeEnd = signalUpdate.time_end + departureTimeDelta;
            int positionStart = signalUpdate.position_start;
            int positionEnd = signalUpdate.position_end;
            int color = signalUpdate.color;
            int r = (color >> 16) & 0xff;
            int g = (color >> 8) & 0xff;
            int b = color & 0xff;
            System.Diagnostics.Debug.Assert(_pathData != null);
            float pathLength = _pathData.Value.Length;
            return new OccupancyBlock(
                timeStart / 1000f,
                timeEnd / 1000f,
                positionStart / pathLength,
                positionEnd / pathLength,
                new Color(r / 255f, g / 255f, b / 255f)
            );
        }

        private IEnumerator LoadPathData()
        {
            if (_pathData == null)
            {
                dynamic pathResponse = null;
                var pathUrl = $"{_editoastUrl}api/train_schedule/{_id}/path?infra_id={_infraId}";
                yield return Helpers.GetJson(pathUrl, result => pathResponse = result);
                if (pathResponse == null || pathResponse.length == null)
                    yield break;
                var tracks = pathResponse.track_section_ranges;
                var blocks = pathResponse.blocks;
                var routes = pathResponse.routes;
                int length = pathResponse.length;
                _pathData = new PathData(tracks, blocks, routes, length);
            }
        }

        private IEnumerator GetGeoPoints()
        {
            if (_geoPoints == null)
            {
                yield return LoadPathData();
                var pathPropsUrl =
                    $"{_editoastUrl}api/infra/{_infraId}/path_properties?props[]=geometry";
                dynamic inputPayload = new System.Dynamic.ExpandoObject();
                inputPayload.track_section_ranges = _pathData?.TrackSections;
                dynamic pathPropsResponse = null;
                Action<dynamic> callback = result => pathPropsResponse = result;
                yield return Helpers.PostJson(pathPropsUrl, inputPayload, callback);
                if (pathPropsResponse == null)
                    yield break;

                var geometry = pathPropsResponse.geometry;

                var result = new List<Vector2>();
                foreach (var geometryPoint in geometry.coordinates)
                {
                    float lon = geometryPoint[0];
                    float lat = geometryPoint[1];
                    var mvtIndex = Helpers.LonLatToMvt(lon, lat, _zoomLevel);
                    var tileX = (float)(mvtIndex.tileX - _originTileIndexX);
                    var tileY = (float)(mvtIndex.tileY - _originTileIndexY - 1);
                    result.Add(new(tileX * _tileSize, -tileY * _tileSize));
                }

                _geoPoints = DouglasPeucker.Simplify(result, 0.005f * _tileSize);
            }
        }

        private void Render(List<Vector2> points, float minHeight, float maxHeight, Color color)
        {
            var newGameObject = new GameObject();
            newGameObject.transform.parent = gameObject.transform;
            MeshFilter meshFilter = newGameObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = newGameObject.AddComponent<MeshRenderer>();

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
            material.color = new Color(color.r, color.g, color.b, 0.2f);
            meshRenderer.material = material;
        }
    }
}
