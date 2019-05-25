using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace OpenSteer
{
    public class LQProximityDatabase : AbstractProximityDatabase
    {
        LocationQueryDatabase lq;

        // constructor
        public LQProximityDatabase(Vector3 center, Vector3 dimensions, Vector3 divisions)
        {
            Vector3 halfsize = dimensions * 0.5f;
            Vector3 origin = center - halfsize;
            
            lq = new LocationQueryDatabase(origin.x, origin.y, origin.z,
                                   dimensions.x, dimensions.y, dimensions.z,
                                   (int)System.Math.Round(divisions.x),
                                   (int)System.Math.Round(divisions.y),
                                   (int)System.Math.Round(divisions.z));
        }

        // "token" to represent objects stored in the database
        public class TokenType : AbstractTokenForProximityDatabase
        {
            LqClientProxy proxy;
            LocationQueryDatabase lq;

            // constructor
            public TokenType(ProximityDatabaseItem parentObject, LQProximityDatabase lqsd)
            {
                proxy = new LqClientProxy(parentObject);
                lq = lqsd.lq;
            }

            // destructor
            ~TokenType()
            {
                lq.LqRemoveFromBin(proxy);
            }

            // the client object calls this each time its position changes
            public override void UpdateForNewPosition(Vector3 p)
            {
                lq.LqUpdateForNewLocation(proxy, p.x, p.y, p.z);
            }

            // find all neighbors within the given sphere (as center and radius)
            public override void FindNeighbors(Vector3 center, float radius, ref List<ProximityDatabaseItem> results)
            {
                ArrayList tList = lq.GetAllObjectsInLocality(center.x, center.y, center.z, radius);
                for (int i = 0; i < tList.Count; i++)
                {
                    LqClientProxy tProxy = (LqClientProxy)tList[i];
                    results.Add(tProxy.clientObject);
                }
            }
        };

        // allocate a token to represent a given client object in this database
        public TokenType AllocateToken(ProximityDatabaseItem parentObject)
        {
            return new TokenType(parentObject, this);
        }

        // count the number of tokens currently in the database
        public override int GetPopulation()
        {
            int count = lq.GetAllObjects().Count;
            return count;
        }

        public override ProximityDatabaseItem GetNearestVehicle(Vector3 position,float radius)
        {
            LqClientProxy tProxy = lq.LqFindNearestNeighborWithinRadius(position.x, position.y, position.z, radius, null);
            ProximityDatabaseItem tItem = null;

            if (tProxy != null)
                tItem = tProxy.clientObject;

            return tItem;
        }

        public override Vector3 GetMostPopulatedBinCenter()
        {
            return lq.GetMostPopulatedBinCenter();
        } 
    }
}
