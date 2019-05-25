using UnityEngine;

namespace PowerLaw
{
    public static class Global
    {
        /// <summary>
        /// Caps the magnitude of a vector to a maximum value. 
        /// </summary>
        /// <param name="v">A force vector</param>
        /// <param name="maxValue">The maximum magnitude of the force.</param>
        public static Vector2 Clamp(Vector2 v, float maxValue)
        {
            float lengthV = v.sqrMagnitude;
            if (lengthV > maxValue)
            {
                float mult = (maxValue / lengthV);
                v.x *= mult;
                v.y *= mult;
            }
            return v;
        }

        /// <summary>
        /// The cross product between two vectors
        /// </summary>
        /// <param name="v1">A vector</param>
        /// <param name="v2">A vector</param>
        /// <returns>Returns the cross product between the two vectors
        /// i.e determinant of the 2x2 matrix formed by using v1 as the first row and v2 as the second row.</returns>
        public static float Det(Vector2 v1, Vector2 v2)
        {
            return v1.x * v2.y - v1.y * v2.x;
        }

        /// <summary>
        /// Determine the closest point on a line segment given a test point.
        /// </summary>
        /// <param name="lineStart">The start point of the line segment.</param>
        /// <param name="lineEnd">The end point of the line segment.</param>
        /// <param name="p">The test point.</param>
        /// <returns>The closest point on the line segment.</returns>
        public static Vector2 ClosestPointLineSegment(Vector2 lineStart, Vector2 lineEnd, Vector2 p)
        {
            float dota = Vector2.Dot((p - lineStart), (lineEnd - lineStart));
            if (dota <= 0) // point line_start is closest to p
                return lineStart;

            float dotb = Vector2.Dot((p - lineEnd), (lineStart - lineEnd));
            if (dotb <= 0) // point line_end is closest to p
                return lineEnd;

            // find closest point
            float slope = dota / (dota + dotb);
            return lineStart + (lineEnd - lineStart) * slope;
        }
    }
}