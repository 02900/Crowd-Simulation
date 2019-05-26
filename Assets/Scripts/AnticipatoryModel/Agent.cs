using UnityEngine;
using OpenSteer;
using System.Collections.Generic;

namespace AnticipatoryModel
{
    public abstract class Agent : ProximityDatabaseItem
    {
        public int id { get; set; }
        public float radius { get; set; }
        [HideInInspector] public Vector2 position;
        [HideInInspector] public Vector2 velocity;

        // dir: 1 is left, -1 is right
        int turnTo = 0;
        public int TurnTo { get { return turnTo; } set { turnTo = value; } }

        protected bool isVirtual;
        public bool IsVirtual { get { return isVirtual; } }

        private LQProximityDatabase.TokenType proximityToken;                               // interface object for the proximity database
        private List<ProximityDatabaseItem> proximityNeighbors = new List<ProximityDatabaseItem>();     // The proximity neighbors

        public List<ProximityDatabaseItem> ProximityNeighbors
        { get { return proximityNeighbors; } set { proximityNeighbors = value; } }

        public void AddToDB() {
            // Add to the database
            proximityToken = Engine.Instance.GetSpatialDatabase.AllocateToken(this);
            UpdateDB();
        }

        // Notify proximity database that our position has changed
        public void UpdateDB() {
            proximityToken.UpdateForNewPosition(ExtensionMethods.Vector2ToVector3(position));
        }

        public void SearchNeighbors(float radius) {
            proximityNeighbors.Clear();
            proximityToken.FindNeighbors(ExtensionMethods.Vector2ToVector3(position), radius, ref proximityNeighbors);
        }
    }
}