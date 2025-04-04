using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Object = System.Object;

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
