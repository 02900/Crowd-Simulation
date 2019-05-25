using UnityEngine;

namespace AnticipatoryModel
{
    public struct Line
    {
        public float m;
        public float b;

        public Line(float m, float b)
        {
            this.m = m;
            this.b = b;
        }

        public float GetX(float y) { return (y - b) / m; }
        public float GetY(float x) { return m*x + b; }

        public static Vector2 IntersetionPoint(Line l1, Line l2)
        {
            Vector2 ip;
            ip.x = (l2.b - l1.b) / (l1.m - l2.m);
            ip.y = l1.m * ip.x + l1.b;
            return ip;
        }
    }
}
