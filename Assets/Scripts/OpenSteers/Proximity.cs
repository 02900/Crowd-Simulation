// ----------------------------------------------------------------------------
//
// OpenSteerDotNet - pure .net port
// Port by Simon Oliver - http://www.handcircus.com
//
// OpenSteer -- Steering Behaviors for Autonomous Characters
//
// Copyright (c) 2002-2003, Sony Computer Entertainment America
// Original author: Craig Reynolds <craig_reynolds@playstation.sony.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//
//
// ----------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpenSteer
{
    public class AbstractTokenForProximityDatabase
    {
        public virtual void UpdateForNewPosition (Vector3 position) { }

        // find all neighbors within the given sphere (as center and radius)
        public virtual void FindNeighbors(Vector3 center, float radius, ref List<ProximityDatabaseItem> results) { }
    }

    // type for the "tokens" manipulated by this spatial database
    public class AbstractProximityDatabase
    {
        // returns the number of tokens in the proximity database
        public virtual int GetPopulation() { return 0; }

        public virtual ProximityDatabaseItem GetNearestVehicle(Vector3 position, float radius) { return null; }

        public virtual Vector3 GetMostPopulatedBinCenter() { return Vector3.zero; } 
    };

   public class BruteForceProximityDatabase : AbstractProximityDatabase
    {
       // STL vector containing all tokens in database
       public ArrayList group;

        // constructor
        public BruteForceProximityDatabase ()
        {
            group = new ArrayList();
        }

        // allocate a token to represent a given client object in this database
       public TokenType2 AllocateToken(AbstractVehicle parentObject)
       {
           TokenType2 tToken = new TokenType2 (parentObject, this);
           return tToken;
       }

        // return the number of tokens currently in the database
       public override int GetPopulation()
        {
            return group.Count;
        }
    };

    // "token" to represent objects stored in the database
    public class TokenType2 : AbstractTokenForProximityDatabase
    {
        BruteForceProximityDatabase bfpd;
        AbstractVehicle tParentObject;
        Vector3 position;

        // constructor
        public TokenType2(AbstractVehicle parentObject, BruteForceProximityDatabase pd)
        {
            // store pointer to our associated database and the object this
            // token represents, and store this token on the database's vector
            bfpd = pd;
            tParentObject = parentObject;
            bfpd.group.Add(this);
        }

        // destructor
        ~TokenType2()
        {
            // remove this token from the database's ArrayList
            bfpd.group.Remove(this);
        }

        // the client object calls this each time its position changes
        public override void UpdateForNewPosition(Vector3 newPosition)
        {
            position = newPosition;
        }

        // find all neighbors within the given sphere (as center and radius)
        public override void FindNeighbors(Vector3 center, float radius, ref List<ProximityDatabaseItem> results)
        {
            // loop over all tokens
            float r2 = radius * radius;

            for (int i = 0; i < bfpd.group.Count; i++)
            {
                TokenType2 tToken=(TokenType2) bfpd.group[i]; 

                Vector3 offset = center - tToken.position;
                float d2 = offset.sqrMagnitude;

                // push onto result vector when within given radius
                if (d2 < r2) results.Add(tToken.tParentObject);
            };
        }
    };
}
