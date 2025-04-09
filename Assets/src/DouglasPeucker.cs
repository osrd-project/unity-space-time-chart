using System.Collections.Generic;
using UnityEngine;

namespace src
{
    public static class DouglasPeucker
    {
        public static List<Vector2> Simplify(List<Vector2> points, float tolerance)
        {
            if (points.Count < 2)
            {
                return new List<Vector2>(points);
            }

            float maxDistance = 0.0f;
            int index = 0;

            Vector2 start = points[0];
            Vector2 end = points[points.Count - 1];

            for (int i = 1; i < points.Count - 1; i++)
            {
                float distance = PerpendicularDistance(points[i], start, end);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    index = i;
                }
            }

            if (maxDistance > tolerance)
            {
                List<Vector2> firstLine = Simplify(points.GetRange(0, index + 1), tolerance);
                List<Vector2> lastLine = Simplify(
                    points.GetRange(index, points.Count - index),
                    tolerance
                );

                firstLine.AddRange(lastLine);
                return firstLine;
            }
            return new List<Vector2> { start, end };
        }

        private static float PerpendicularDistance(
            Vector2 point,
            Vector2 lineStart,
            Vector2 lineEnd
        )
        {
            Vector2 lineDirection = lineEnd - lineStart;
            float lineLengthSquared = lineDirection.sqrMagnitude;
            Vector2 pointDirection = point - lineStart;

            float dotProduct = Vector2.Dot(pointDirection, lineDirection);
            float parameter = Mathf.Clamp01(dotProduct / lineLengthSquared);

            Vector2 closestPoint = lineStart + parameter * lineDirection;
            return Vector2.Distance(point, closestPoint);
        }
    }
}
