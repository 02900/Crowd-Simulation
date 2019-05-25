using UnityEngine;

namespace SocialForces
{
    public struct Line
    {
        public Vector3 start;
        public Vector3 end;
    }

    public class Wall
    {
        public Line wall;

        public Wall()
        {
            wall.start = new Vector3(0.0f, 0.0f, 0.0f);
            wall.end = new Vector3(0.0f, 0.0f, 0.0f);
        }

        public Wall(float x1, float z1, float x2, float z2)
        {
            wall.start = new Vector3(x1, 0.0f, z1);
            wall.end = new Vector3(x2, 0.0f, z2);
        }

        public Vector3 GetStartPoint() { return wall.start; }
        public Vector3 GetEndPoint() { return wall.end; }

        // Computes distance between 'position_i' and wall
        public Vector3 GetNearestPoint(Vector3 position_i)
        {
            Vector3 relativeEnd, relativePos, relativeEndScal, relativePosScal;
            float dotProduct;
            Vector3 nearestPoint;

            // Create Vector Relative to Wall's 'start'
            relativeEnd = wall.end - wall.start;    // Vector from wall's 'start' to 'end'
            relativePos = position_i - wall.start;  // Vector from wall's 'start' to agent i 'position'

            // Scale Both Vectors by the Length of the Wall
            relativeEndScal = relativeEnd;
            relativeEndScal.Normalize();

            relativePosScal = relativePos * (1.0F / relativeEnd.magnitude);

            // Compute Dot Product of Scaled Vectors
            dotProduct = Vector3.Dot(relativeEndScal, relativePosScal);

            if (dotProduct < 0.0)       // Position of Agent i located before wall's 'start'
                nearestPoint = wall.start;
            else if (dotProduct > 1.0)  // Position of Agent i located after wall's 'end'
                nearestPoint = wall.end;
            else                        // Position of Agent i located between wall's 'start' and 'end'
                nearestPoint = (relativeEnd * dotProduct) + wall.start;

            return nearestPoint;
        }
    }
}