using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace src
{
    public class TrainLoader : MonoBehaviour
    {
        private int _timetableId;
        private int _infraId;
        private String _edioastUrl;
        private float _tileSize;
        private int _zoomLevel;
        private int _originTileIndexX;
        private int _originTileIndexY;

        private DateTime? _earliestDeparture;

        public float currentVerticalOffset;

        public static TrainLoader CreateTrainLoader(
            int timetableId,
            int infraId,
            String edioastUrl,
            float tileSize,
            int zoomLevel,
            int originTileIndexX,
            int originTileIndexY
        )
        {
            var obj = new GameObject("TrainLoader");
            var trainLoader = obj.AddComponent<TrainLoader>();
            trainLoader._timetableId = timetableId;
            trainLoader._infraId = infraId;
            trainLoader._edioastUrl = edioastUrl;
            trainLoader._tileSize = tileSize;
            trainLoader._zoomLevel = zoomLevel;
            trainLoader._originTileIndexX = originTileIndexX;
            trainLoader._originTileIndexY = originTileIndexY;
            trainLoader.StartCoroutine(trainLoader.LoadTrains());
            return trainLoader;
        }

        public void Update()
        {
            var scrollSpeed = 3f; // Meters (unity unit) per second pressed
            if (Input.GetKey(KeyCode.LeftShift))
                scrollSpeed *= 3f;
            if (Input.GetKey(KeyCode.LeftControl))
                scrollSpeed *= 10f;

            if (Input.GetKey(KeyCode.Q))
            {
                currentVerticalOffset += Time.deltaTime * scrollSpeed;
            }
            if (Input.GetKey(KeyCode.E))
            {
                currentVerticalOffset -= Time.deltaTime * scrollSpeed;
            }
        }

        IEnumerator LoadTrains()
        {
            var trainIds = new List<int>();
            yield return GetTrainIds(trainIds);
            foreach (var trainId in trainIds)
            {
                Train.CreateTrain(
                    gameObject,
                    _edioastUrl,
                    trainId,
                    _infraId,
                    _zoomLevel,
                    _originTileIndexX,
                    _originTileIndexY,
                    _tileSize,
                    _earliestDeparture.Value
                );
                yield return new WaitForSeconds(.1f); // Limit editoast requests
            }
        }

        private IEnumerator GetTrainIds(List<int> res)
        {
            int? page = 1;
            while (page != null)
            {
                string timetableUrl =
                    $"{_edioastUrl}api/timetable/{_timetableId}/train_schedules/?page={page}";
                dynamic parsed = null;
                yield return Helpers.GetJson(timetableUrl, result => parsed = result);
                foreach (var schedule in parsed.results)
                {
                    int id = schedule.id;
                    string rawDepartureTime = schedule.start_time;
                    DateTime departureTime = DateTime.Parse(rawDepartureTime);
                    if (_earliestDeparture == null || departureTime < _earliestDeparture)
                        _earliestDeparture = departureTime;
                    res.Add(id);
                }
                page = parsed.next;
            }
        }
    }
}
