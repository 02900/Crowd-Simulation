using UnityEngine;

namespace AnticipatoryModel
{
    public struct Point
    {
        public float x, y;
        public Point(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public Point(Vector2 v)
        {
            x = v.x;
            y = v.y;
        }

        public static float Distance(Point p1, Point p2)
        {
            return Mathf.Sqrt (Mathf.Pow(p2.x - p1.x, 2) + Mathf.Pow(p2.y - p1.y, 2));
        }

        public static Vector2 MakeVector(Point p1, Point p2) {
            return new Vector2(p2.x - p1.x, p2.y - p1.y);
        }
    }
}