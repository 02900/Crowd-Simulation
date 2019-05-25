using UnityEngine;

namespace AnticipatoryModel
{
    public static class Global
    {
        // To check if circle 2 inside circle 1
        public static bool CircleInside
            (Vector2 center1, float radius1,
            Vector2 center2, float radius2)
        {

            float d = Vector2.Distance(center1, center2);
            return radius1 >= (d + radius2 - radius2 / 4);
        }

        public static void FindEqLine(
            Point P1, Point P2,
            out float m, out float b)
        {
            m = (P2.y - P1.y) / (P2.x - P1.x);
            b = -m * P1.x + P1.y;
        }


        // http://csharphelper.com/blog/2014/12/find-the-tangent-lines-between-two-circles-in-c/

        // Find the points where the two circles intersect.
        public static int FindCircleCircleIntersections(
            float cx0, float cy0, float radius0,
            float cx1, float cy1, float radius1,
            out Point intersection1, out Point intersection2)
        {
            // Find the distance between the centers.
            float dx = cx0 - cx1;
            float dy = cy0 - cy1;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);

            // See how many solutions there are.
            if (dist > radius0 + radius1)
            {
                // No solutions, the circles are too far apart.
                intersection1 = new Point(float.NaN, float.NaN);
                intersection2 = new Point(float.NaN, float.NaN);
                return 0;
            }
            else if (dist < Mathf.Abs(radius0 - radius1))
            {
                // No solutions, one circle contains the other.
                intersection1 = new Point(float.NaN, float.NaN);
                intersection2 = new Point(float.NaN, float.NaN);
                return 0;
            }
            else if ((dist == 0) && (radius0 == radius1))
            {
                // No solutions, the circles coincide.
                intersection1 = new Point(float.NaN, float.NaN);
                intersection2 = new Point(float.NaN, float.NaN);
                return 0;
            }
            else
            {
                // Find a and h.
                float a = (radius0 * radius0 -
                    radius1 * radius1 + dist * dist) / (2 * dist);
                float h = Mathf.Sqrt(radius0 * radius0 - a * a);

                // Find P2.
                float cx2 = cx0 + a * (cx1 - cx0) / dist;
                float cy2 = cy0 + a * (cy1 - cy0) / dist;

                // Get the points P3.
                intersection1 = new Point(
                    cx2 + h * (cy1 - cy0) / dist,
                    cy2 - h * (cx1 - cx0) / dist);
                intersection2 = new Point(
                    cx2 - h * (cy1 - cy0) / dist,
                    cy2 + h * (cx1 - cx0) / dist);

                // See if we have 1 or 2 solutions.
                if (dist == radius0 + radius1) return 1;
                return 2;
            }
        }


        // Find the tangent points for this circle and external point.
        // Return true if we find the tangents, false if the point is
        // inside the circle.
        public static bool FindTangents(Point center, float radius,
            Point external_point, out Point pt1, out Point pt2)
        {
            // Find the distance squared from the
            // external point to the circle's center.
            float dx = center.x - external_point.x;
            float dy = center.y - external_point.y;
            float D_squared = dx * dx + dy * dy;
            if (D_squared < radius * radius)
            {
                pt1 = new Point(-1, -1);
                pt2 = new Point(-1, -1);
                return false;
            }

            // Find the distance from the external point
            // to the tangent points.
            float L = Mathf.Sqrt(D_squared - radius * radius);

            // Find the points of intersection between
            // the original circle and the circle with
            // center external_point and radius dist.
            FindCircleCircleIntersections(
                center.x, center.y, radius,
                external_point.x, external_point.y, L,
                out pt1, out pt2);

            return true;
        }

        // Find the outer tangent points for these two circles.
        // Return the number of tangents: 2 or 0.
        public static int FindCircleCircleTangents(
            Point c1, float radius1, Point c2, float radius2,
            out Point outer1_p1, out Point outer1_p2,
            out Point outer2_p1, out Point outer2_p2)
        {
            // Make sure radius1 <= radius2.
            if (radius1 > radius2)
            {
                // Call this method switching the circles.
                return FindCircleCircleTangents(
                    c2, radius2, c1, radius1,
                    out outer1_p2, out outer1_p1,
                    out outer2_p2, out outer2_p1);
            }

            // Initialize the return values in case
            // some tangents are missing.
            outer1_p1 = new Point(-1, -1);
            outer1_p2 = new Point(-1, -1);
            outer2_p1 = new Point(-1, -1);
            outer2_p2 = new Point(-1, -1);

            // ***************************
            // * Find the outer tangents *
            // ***************************
            {
                float radius2a = radius2 - radius1;
                if (!FindTangents(c2, radius2a, c1,
                    out outer1_p2, out outer2_p2))
                {
                    // There are no tangents.
                    return 0;
                }

                // Get the vector perpendicular to the
                // first tangent with length radius1.
                float v1x = -(outer1_p2.y - c1.y);
                float v1y = outer1_p2.x - c1.x;
                float v1_length = Mathf.Sqrt(v1x * v1x + v1y * v1y);
                v1x *= radius1 / v1_length;
                v1y *= radius1 / v1_length;
                // Offset the tangent vector's points.
                outer1_p1 = new Point(c1.x + v1x, c1.y + v1y);
                outer1_p2 = new Point(
                    outer1_p2.x + v1x,
                    outer1_p2.y + v1y);

                // Get the vector perpendicular to the
                // second tangent with length radius1.
                float v2x = outer2_p2.y - c1.y;
                float v2y = -(outer2_p2.x - c1.x);
                float v2_length = (float)Mathf.Sqrt(v2x * v2x + v2y * v2y);
                v2x *= radius1 / v2_length;
                v2y *= radius1 / v2_length;
                // Offset the tangent vector's points.
                outer2_p1 = new Point(c1.x + v2x, c1.y + v2y);
                outer2_p2 = new Point(
                    outer2_p2.x + v2x,
                    outer2_p2.y + v2y);
            }
            return 2;
        }

        public static float TTC(Vector2 posA, Vector2 velA, float radA,
            Vector2 posB, Vector2 velB, float radB)
        {
            float r = radA + radB;
            Vector2 w = posB - posA;
            float c = Vector2.Dot(w, w) - r * r;

            if (c < 0)
            {
                //Debug.Log("Agents are colliding");
                return 0;
            }

            Vector2 v = velA - velB;
            float a = Vector2.Dot(v, v);
            float b = Vector2.Dot(w, v);

            float discr = b * b - a * c;
            if (discr <= 0)
                return Mathf.Infinity;

            float tau = (b - Mathf.Sqrt(discr)) / a;
            if (tau < 0)
                return Mathf.Infinity;

            return tau;
        }

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