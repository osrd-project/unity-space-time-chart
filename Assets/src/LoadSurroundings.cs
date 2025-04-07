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
        public int tileSightDistance = 3;

        // Centers the game origin to a place near small infra
        public int originTileIndexX = 1053;
        public int originTileIndexY = 730;

        public bool useMapBackground = true;

        // Cache location for map background (saving API calls)
        string cacheDirectory = "Assets/TextureCache/";

        private HashSet<Tuple<int, int>> _loadedTiles = new();

        void Start()
        {
            // Forces floats to be formatted as `1.23` and not `1,23` on French windows,
            // as it would mess with the API calls. (seriously what the hell)
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");
            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
            }
            TrainLoader.CreateTrainLoader(
                timetableId,
                infraId,
                editoastUrl,
                tileSize,
                zoomLevel,
                originTileIndexX,
                originTileIndexY
            );
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
                    createTile(tileUrl, originTileIndexX + x, originTileIndexY + y, origin);
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
