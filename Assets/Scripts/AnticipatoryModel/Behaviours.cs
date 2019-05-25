using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnticipatoryModel
{
    public static class Behaviours
    {
        const float EPSILON = 0.02f;

        /// Only move to goal direction with vpref
        public static Vector2 GetSteering(Vector2 position, Vector2 goal, float prefSpeed)
        {
            Vector2 dir = goal - position;
            return dir.normalized * prefSpeed;
        }

        /// Basic algorithm to avoidance collisions
        public static Vector2 CollisionAvoidance(Vector2 position, Vector2 velocity,
            Vector2 goalVelocity, float timeHorizon,Dictionary<int, float> targets)
        {
            Vector2 FAvoid;

            // k is a tunable parameter that controls the strength of the goal force
            float k = 2;

            // Compute goal force
            Vector2 acceleration = k * (goalVelocity - velocity);

            foreach (var id in targets.Keys)
            {
                // compute time to collision
                float t = targets[id];
                if (float.IsInfinity(t)) continue;

                AMAgent agent_tmp = Engine.Instance.GetAgent(id);

                // Compute collision avoidance force
                FAvoid = position + velocity * t
                    - agent_tmp.position - agent_tmp.velocity * t;

                if (System.Math.Abs(FAvoid.x) > EPSILON &&
                    System.Math.Abs(FAvoid.y) > EPSILON)
                    FAvoid /= Mathf.Sqrt(Vector2.Dot(FAvoid, FAvoid));

                // Force Magnitude
                float mag = 0;
                if (t >= 0 && t <= timeHorizon) mag = (timeHorizon - t) / t + 0.1f;
                mag = Mathf.Clamp(mag, 0, 100);
                FAvoid *= mag;
                acceleration += FAvoid;
            }

            return acceleration * Engine.timeStep;
        }

        public static Vector2 FollowStrategy(float radius, float prefSpeed, Vector2 posA, Vector2 velA, Vector2 posB, Vector2 velB)
        {
            float magnitude;
            float ttr = 1.5f;                     // reaction time
            float df = radius * 3 + 1.5f;         // zone contact + personal distances

            // Posicion futura del leader
            Vector2 pl = posB + velB * Engine.timeStep;

            // distance to future position of leader
            float dstLeader = (pl - posA).magnitude;
            float vf = (dstLeader - df) / (Engine.timeStep + ttr);

            magnitude = vf > prefSpeed ? prefSpeed : vf;
            return velA.normalized * magnitude;
        }

        public static float BearingAngle(Vector2 velocity, Vector2 dir)
        {
            float a = -Vector2.SignedAngle(velocity, dir);
            if (a < 0) a += 360;
            return a;
        }

        public static Vector2 ChangeDirectionStrategy(Vector2 velocity, Vector2 dir, bool lateral, float ttc, bool debug)
        {
            // dir equal 1 is left, -1 is right
            float bearingAngle = BearingAngle(velocity, dir), turn;

            if (debug) Debug.Log(bearingAngle);

            if (!lateral)
            {
                if (bearingAngle < 180) turn = 1;
                else turn = -1;

                if (System.Math.Abs(bearingAngle) < EPSILON
                    || System.Math.Abs(bearingAngle - 360) < EPSILON
                    || System.Math.Abs(bearingAngle - 180) < EPSILON)
                {
                    turn = Random.Range(0, 11) > 5 ? -1 : 1;
                }
            }

            else
            {
                if (bearingAngle >= 10 && bearingAngle < 170
                    || bearingAngle >= 185 && bearingAngle < 350) turn = 1;
                else
                {
                    if (bearingAngle < 180) turn = 1;
                    else turn = -1;
                }
            }

            float w = 10 / (Mathf.Pow(ttc, 2) + 0.25f);
            return ExtensionMethods.RotateVector(velocity, turn * w);
        }

        public static Vector2 DecelerationStrategy(float ttc, Vector2 velocity)
        {
            float k = ttc < 2 ? 2 : ttc;
            k = Mathf.Exp(-0.15f * k * k);
            return velocity * (1 - k);
        }
    }
}