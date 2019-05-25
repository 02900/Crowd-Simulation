﻿using System.Collections.Generic;
using UnityEngine;

namespace AnticipatoryModel
{
    public static class Behaviours
    {
        const float EPSILON = 0.02f;
        const float maxAcc = 1000;
        const float k = 2.5f;  // k is a tunable parameter that controls the strength of the goal force

        /// Only move to goal direction with vpref
        public static Vector2 GetSteering(Vector2 position, Vector2 goal, float prefSpeed)
        {
            return (goal - position).normalized * prefSpeed;
        }

        /// Basic algorithm to avoidance collisions
        public static Vector2 CollisionAvoidance(Vector2 position, Vector2 velocity,
            Vector2 goalVelocity, float timeHorizon, Dictionary<int, float> targets, 
            Dictionary<int, float> targetGroups)
        {
            Vector2 acceleration = k * (goalVelocity - velocity);
            acceleration += ComputeForces(position, velocity, timeHorizon, targets);
            acceleration += ComputeForces(position, velocity, timeHorizon, targetGroups, true);
            return acceleration * Engine.timeStep;
        }

        static Vector2 ComputeForces(Vector2 position, Vector2 velocity, float tH, 
            Dictionary<int, float> targets, bool group = false) {
            Vector2 acceleration = Vector2.zero, FAvoid;
            float k = 0;
            foreach (var id in targets.Keys)
            {
                // compute time to collision
                float t = targets[id];
                if (float.IsInfinity(t)) continue;

                Agent agent_tmp;

                if (!group) agent_tmp = Engine.Instance.GetAgent(id);
                else agent_tmp = Engine.Instance.GetVirtualAgent(id);

                // Compute collision avoidance force
                FAvoid = position + velocity * t
                    - agent_tmp.position - agent_tmp.velocity * t;

                FAvoid.Normalize();

                // Force Magnitude
                if (t >= 0 && t <= tH) k = (tH - t) / t + 0.1f;
                acceleration += FAvoid * Mathf.Clamp(k, 0, maxAcc);
            }
            return acceleration;
        }

        public static Vector2 FollowStrategy(float radius, float prefSpeed, 
            Vector2 posA, Vector2 velA, Vector2 posB, Vector2 velB)
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

        public static Vector2 ChangeDirectionStrategy(Vector2 velocity, Vector2 dir,
            bool lateral, float ttc, bool debug)
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