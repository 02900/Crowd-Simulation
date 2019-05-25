/*
 *  LineObstacle.h
 *  
 *  
 *  All rights are retained by the authors and the University of Minnesota.
 *  Please contact sjguy@cs.umn.edu for licensing inquiries.
 *  
 *  Authors: Ioannis Karamouzas, Brian Skinner, and Stephen J. Guy
 *  Contact: ioannis@cs.umn.edu
 */

/*!
 *  @file       LineObstacle.h
 *  @brief      Contains the LineObstacle class.
 */

using UnityEngine;

namespace PowerLaw
{
    // A line segment obstacle class. 
    public class LineObstacle
    {
        protected Vector2 p1;          // The first endpoint of the obstacle. 
        protected Vector2 p2;          // The second endpoint of the obstacle. 
        protected Vector2 normal;      // The normal vector of the line obstacle.

        /// <summary>
        /// Constructor. Constructs an obstacle.
        /// </summary>
        /// <param name="a">The first endpoint of the obstacle</param>
        /// <param name="b">The second endpoint of the obstacle</param>
        public LineObstacle(Vector2 a, Vector2 b)
        {
            p1 = a;
            p2 = b;
            normal = Vector2.Perpendicular(p2 - p1).normalized;
        }

        // Returns the fist endpoint of the line segment.  
        public Vector2 P1 { get { return p1; } }
        
        // Returns the second endpoint of the line segment.  
        public Vector2 P2 { get { return p2; } }
        
        // Returns the normal of the line obstacle.  
        public Vector2 Normal { get { return normal; } }
    }
}
