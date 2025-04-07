using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace src
{
    public class Helpers
    {
        public static (
            double lonLeft,
            double lonRight,
            double latTop,
            double latBottom
        ) MvtToLatLonBounds(int zoom, int x, int y)
        {
            int numTiles = 1 << zoom;

            // Calculate longitude borders
            double lonLeft = (x / (double)numTiles) * 360 - 180;
            double lonRight = ((x + 1) / (double)numTiles) * 360 - 180;

            // Calculate latitude borders
            double latBottom = ToDegrees(Math.PI - (2 * Math.PI * y) / numTiles);
            double latTop = ToDegrees(Math.PI - (2 * Math.PI * (y + 1)) / numTiles);

            return (lonLeft, lonRight, latTop, latBottom);
        }

        public static (double tileX, double tileY) LatLonToMvt(double lon, double lat, int zoom)
        {
            int numTiles = 1 << zoom;

            double tileX = (lon + 180.0) / 360.0 * numTiles;
            double latRad = ToRadians(lat);
            double tileY =
                (1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI)
                / 2.0
                * numTiles;

            return (tileX, tileY);
        }

        public static IEnumerator GetJson(String url, Action<dynamic> callback)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    String payload = request.downloadHandler.text;
                    callback(JObject.Parse(payload));
                }
                else
                {
                    Debug.LogError("request failed for " + url + ": " + request.error);
                }
            }
        }

        public static IEnumerator PostJson(
            String url,
            dynamic inputPayload,
            Action<dynamic> callback
        )
        {
            using (
                UnityWebRequest request = UnityWebRequest.Post(
                    url,
                    JObject.FromObject(inputPayload).ToString(),
                    "application/json"
                )
            )
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    String payload = request.downloadHandler.text;
                    callback(JObject.Parse(payload));
                }
                else
                {
                    Debug.LogError("request failed for " + url + ": " + request.error);
                }
            }
        }

        public static String ListToStr<T>(List<T> list)
        {
            var strList = new List<String>();
            foreach (var x in list)
                strList.Add(x.ToString());
            return "[" + String.Join(", ", strList) + "]";
        }

        /** Slices the given line to the given range. Inputs are in [0, 1]. */
        public static List<Vector2> SliceLine(
            List<Vector2> line,
            float relativeFrom,
            float relativeTo
        )
        {
            var length = 0f;
            for (int i = 0; i < line.Count - 1; i++)
            {
                length += (line[i] - line[i + 1]).magnitude;
            }

            var result = new List<Vector2>();

            var distanceToRemoveAtStart = relativeFrom * length;
            var distanceToEnd = relativeTo * length;
            for (int i = 0; i < line.Count - 1; i++)
            {
                var segment = line[i + 1] - line[i];
                var segmentLength = segment.magnitude;
                if (distanceToRemoveAtStart <= 0f)
                    result.Add(line[i]);
                else if (distanceToRemoveAtStart < segmentLength)
                    result.Add(line[i] + segment * (distanceToRemoveAtStart / segmentLength));

                if (distanceToEnd <= segmentLength)
                {
                    result.Add(line[i] + segment * (distanceToEnd / segmentLength));
                    break;
                }
                distanceToRemoveAtStart -= segmentLength;
                distanceToEnd -= segmentLength;
            }

            var resultNoDuplicate = new List<Vector2>();
            resultNoDuplicate.Add(result[0]);
            for (var i = 1; i < result.Count; i++)
                if ((resultNoDuplicate.Last() - result[i]).magnitude >= 0.001)
                    resultNoDuplicate.Add(result[i]);

            return resultNoDuplicate;
        }

        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        private static double ToDegrees(double radians)
        {
            return 180 / Math.PI * (2 * Math.Atan(Math.Exp(radians)) - Math.PI / 2);
        }
    }
}
