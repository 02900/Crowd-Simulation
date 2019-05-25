using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class ConcaveHull {

    private float EuclideanDistance(Vector2 a, Vector2 b)
    {
        return Mathf.Sqrt(Mathf.Pow(a.x - b.x, 2) + Mathf.Pow(a.y - b.y, 2));
    }

    private List<Vector2> KNearestNeighbors(List<Vector2> l, Vector2 q, int k)
    {
        List<Tuple<float, Vector2>> nearestList = new List<Tuple<float, Vector2>>();

        foreach (Vector2 o in l)
            nearestList.Add(new Tuple<float, Vector2>(EuclideanDistance(q, o), o));

        nearestList = nearestList.OrderBy(x => x.Item1).ToList();

        List<Vector2> result = new List<Vector2>();

        for (int i = 0; i<Mathf.Min(k, nearestList.Count()); i++)
            result.Add(nearestList[i].Item2);

        return result;
    }

    private Vector2 FindMinYPoint(List<Vector2> l)
    {
        l = l.OrderBy(v => v.y).ToList();
        if (l[0].y > l[1].y) Debug.LogError("Estas ordenando mal en la funcion FindMinYPoint");
        return l[0];
    }

    private float CalculateAngle(Vector2 o1, Vector2 o2)
    {
        return Mathf.Atan2(o2.y - o1.y, o2.x - o1.x);
    }

    private float AngleDifference(float a1, float a2)
    {
        // calculate angle difference in clockwise directions as radians
        if ((a1 > 0 && a2 >= 0) && a1 > a2) return Mathf.Abs(a1 - a2);
        else if ((a1 >= 0 && a2 > 0) && a1 < a2) return 2 * Mathf.PI + a1 - a2;
        else if ((a1 < 0 && a2 <= 0) && a1 < a2) return 2 * Mathf.PI + a1 + Mathf.Abs(a2);
        else if ((a1 <= 0 && a2 < 0) && a1 > a2) return Mathf.Abs(a1 - a2);
        else if (a1 <= 0 && 0 < a2) return 2 * Mathf.PI + a1 - a2;
        else if (a1 >= 0 && 0 >= a2) return a1 + Mathf.Abs(a2);
        else return 0.0f;
    }

    private List<Vector2> SortByAngle(List<Vector2> l, Vector2 q, float angle)
    {
        List<Vector2> vertList = new List<Vector2>(l);
        vertList.Sort((v1, v2) => AngleDifference(angle, CalculateAngle(q, v1)).CompareTo(AngleDifference(angle, CalculateAngle(q, v2))));
        return vertList;
    }

    private bool Intersect(Vector2 l1p1, Vector2 l1p2, Vector2 l2p1, Vector2 l2p2)
    {
        // calculate part equations for line-line intersection
        float a1 = l1p2.x - l1p1.x;
        float b1 = l1p1.x - l1p2.x;
        float c1 = a1 * l1p1.x + b1 * l1p1.y;
        float a2 = l2p2.y - l2p1.y;
        float b2 = l2p1.x - l2p2.x;
        float c2 = a2 * l2p1.x + b2 * l2p1.y;
        // calculate the divisor
        float tmp = (a1 * b2 - a2 * b1);

        // calculate intersection point x coordinate
        float pX = (c1 * b2 - c2 * b1) / tmp;

        // check if intersection x coordinate lies in line line segment
        if ((pX > l1p1.x && pX > l1p2.x) || (pX > l2p1.x && pX > l2p2.x)
                || (pX < l1p1.x && pX < l1p2.x) || (pX < l2p1.x && pX < l2p2.x))
            return false;

        // calculate intersection point y coordinate
        float pY = (a1 * c2 - a2 * c1) / tmp;

        // check if intersection y coordinate lies in line line segment
        if ((pY > l1p1.y && pY > l1p2.y) || (pY > l2p1.y && pY > l2p2.y)
                || (pY < l1p1.y && pY < l1p2.y) || (pY < l2p1.y && pY < l2p2.y))
            return false;

        return true;
    }

    private bool PointInPolygon(Vector2 p, List<Vector2> pp)
    {
        bool result = false;
        for (int i = 0, j = pp.Count - 1; i < pp.Count; j = i++)
        {
            if ((pp[i].y > p.y) != (pp[j].y > p.y) &&
                    (p.x < (pp[j].x - pp[i].x) * (p.y - pp[i].y) / (pp[j].y - pp[i].y) + pp[i].x))
            {
                result = !result;
            }
        }
        return result;
    }

    public List<Vector2> CalculateConcaveHull(List<Vector2> pointArrayList, int k)
    {
        // the resulting concave hull
        List<Vector2> concaveHull = new List<Vector2>();

        // optional remove duplicates
        HashSet<Vector2> set = new HashSet<Vector2>(pointArrayList);
        List<Vector2> pointArraySet = new List<Vector2>(set);

        // k has to be greater than 3 to execute the algorithm
        int kk = Mathf.Max(k, 3);

        // return Points if already Concave Hull
        if (pointArraySet.Count < 3)
            return pointArraySet;

        // make sure that k neighbors can be found
        kk = Mathf.Min(kk, pointArraySet.Count - 1);

        // find first point and remove from point list
        Vector2 firstPoint = FindMinYPoint(pointArraySet);
        concaveHull.Add(firstPoint);
        Vector2 currentPoint = firstPoint;
        pointArraySet.Remove(firstPoint);

        float previousAngle = 0.0f;
        int step = 2;

        while ((currentPoint != firstPoint || step == 2) && pointArraySet.Count > 0)
        {
            // after 3 steps add first point to dataset, otherwise hull cannot be closed
            if (step == 5)
                pointArraySet.Add(firstPoint);

            // get k nearest neighbors of current point
            List<Vector2> kNearestPoints = KNearestNeighbors(pointArraySet, currentPoint, kk);

            // sort points by angle clockwise
            List<Vector2> clockwisePoints = SortByAngle(kNearestPoints, currentPoint, previousAngle);

            // check if clockwise angle nearest neighbors are candidates for concave hull
            Boolean its = true;
            int i = -1;
            while (its && i < clockwisePoints.Count - 1)
            {
                i++;

                int lastPoint = 0;
                if (clockwisePoints[i] == firstPoint)
                    lastPoint = 1;

                // check if possible new concave hull point intersects with others
                int j = 2;
                its = false;
                while (!its && j < concaveHull.Count - lastPoint)
                {
                    its = Intersect(concaveHull[step - 2], clockwisePoints[i], concaveHull[step - 2 - j], concaveHull[step - 1 - j]);
                    j++;
                }
            }

            // if there is no candidate increase k - try again
            if (its)
            {
                return CalculateConcaveHull(pointArrayList, k + 1);
            }

            // add candidate to concave hull and remove from dataset
            currentPoint = clockwisePoints[i];
            concaveHull.Add(currentPoint);
            pointArraySet.Remove(currentPoint);

            // calculate last angle of the concave hull line
            previousAngle = CalculateAngle(concaveHull[step - 1], concaveHull[step - 2]);

            step++;
        }

        // Check if all points are contained in the concave hull
        Boolean insideCheck = true;
        int ii = pointArraySet.Count - 1;

        while (insideCheck && ii > 0)
        {
            insideCheck = PointInPolygon(pointArraySet[ii], concaveHull);
            ii--;
        }

        // if not all points inside -  try again
        if (!insideCheck)
            return CalculateConcaveHull(pointArrayList, k + 1);
        else
            return concaveHull;
    }
}
