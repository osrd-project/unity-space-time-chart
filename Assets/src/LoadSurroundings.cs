using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

namespace src
{
    public class CreateLine : MonoBehaviour
    {
        public string editoastUrl = "http://localhost:4000/";
        public int infraId = 2;
        public int timetableId = 2;
        public float lineSize = 0.001f;
        public float tileSize = 15.0f;
        public int zoomLevel = 11; // As per MVT specs
        public int tileSightDistance = 2;

        public float startLat = 49.5327827f;
        public float startLon = -0.4015937f;

        // Cache location for map background (saving API calls)
        string cacheDirectory = "Assets/TextureCache/";

        private HashSet<Tuple<int, int>> _loadedTiles = new();

        private TrainLoader _trainLoader;
        private List<GameObject> _tileObjects = new();

        void Start()
        {
            // Forces floats to be formatted as `1.23` and not `1,23` on French windows,
            // as it would mess with the API calls. (seriously what the hell)
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");
            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
            }

            var tileOrigin = Helpers.LonLatToMvt(startLon, startLat, zoomLevel);
            Debug.Log($"Tile origin: {(int)tileOrigin.tileX}, {(int)tileOrigin.tileY}");
            _trainLoader = TrainLoader.CreateTrainLoader(
                timetableId,
                infraId,
                editoastUrl,
                tileSize,
                zoomLevel,
                (int)tileOrigin.tileX,
                (int)tileOrigin.tileY
            );

            StartCoroutine(LoadTiles());
        }

        public void Update()
        {
            var zoomIn = Input.GetKeyDown(KeyCode.O);
            var zoomOut = Input.GetKeyDown(KeyCode.P);
            if (zoomIn || zoomOut)
            {
                int zoomLevelDelta = zoomIn ? 1 : -1;
                ResetZoom(zoomLevelDelta);
            }
        }

        private void ResetZoom(int zoomLevelDelta)
        {
            var tileOrigin = Helpers.LonLatToMvt(startLon, startLat, zoomLevel);
            var (cameraLat, cameraLon) = Helpers.MvtToLatLon(
                zoomLevel,
                (int)tileOrigin.tileX + transform.position.x / tileSize,
                (int)tileOrigin.tileY - transform.position.z / tileSize
            );
            startLat = (float)cameraLat;
            startLon = (float)cameraLon;

            zoomLevel += zoomLevelDelta;

            if (_trainLoader)
                Destroy(_trainLoader.gameObject);
            foreach (var tileObject in _tileObjects)
                Destroy(tileObject);
            _loadedTiles.Clear();

            var newTileOrigin = Helpers.LonLatToMvt(startLon, startLat, zoomLevel);
            _trainLoader = TrainLoader.CreateTrainLoader(
                timetableId,
                infraId,
                editoastUrl,
                tileSize,
                zoomLevel,
                (int)newTileOrigin.tileX,
                (int)newTileOrigin.tileY
            );

            var newCameraCoordinates = Helpers.LonLatToMvt(startLon, startLat, zoomLevel);
            transform.position = new Vector3(
                ((float)newCameraCoordinates.tileX - (int)newTileOrigin.tileX) * tileSize,
                transform.position.y,
                ((float)newCameraCoordinates.tileY - (int)newTileOrigin.tileY - 1) * tileSize
            );

            var (finalCameraLat, finalCameraLon) = Helpers.MvtToLatLon(
                zoomLevel,
                (int)tileOrigin.tileX + transform.position.x / tileSize,
                (int)tileOrigin.tileY - transform.position.z / tileSize
            );
            Debug.Log($"from ({cameraLat}, {cameraLon} to {finalCameraLat}, {finalCameraLon})");
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
                    var tileOrigin = Helpers.LonLatToMvt(startLon, startLat, zoomLevel);
                    var x = nextTileIndex.Item1;
                    var y = nextTileIndex.Item2;
                    _loadedTiles.Add(Tuple.Create(x, y));
                    var origin = new Vector2(tileSize * x, -tileSize * y);
                    createTile(
                        tileUrl,
                        (int)tileOrigin.tileX + x,
                        (int)tileOrigin.tileY + y,
                        origin
                    );
                }

                yield return new WaitForSeconds(.1f); // Limit editoast requests to 10/s
            }
        }

        private void createTile(string tileUrl, int x, int y, Vector2 origin)
        {
            var name = "floor_tile:" + x + "," + y;
            GameObject quad = new GameObject(name);
            TileHandler.CreateTileHandler(quad, zoomLevel, x, y, infraId, origin, tileSize);
            TrackLoader.CreateTrackLoader(
                quad,
                zoomLevel,
                x,
                y,
                infraId,
                tileUrl,
                lineSize,
                tileSize,
                origin
            );
            _tileObjects.Add(quad);
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
    }
}
