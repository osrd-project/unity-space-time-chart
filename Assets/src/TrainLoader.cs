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
                    _tileSize
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
                    res.Add(id);
                }
                page = parsed.next;
            }
        }
    }
}
