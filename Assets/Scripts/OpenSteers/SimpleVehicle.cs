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

using UnityEngine;

namespace OpenSteer
{
    public class SimpleVehicle : SteerLibrary
    {
        float _mass;       // mass (defaults to unity so acceleration = force)
        float _radius;     // size of bounding sphere, for obstacle avoidance, etc.
        float _speed;      // speed along Forward direction.  Because local space
                           // is velocity-aligned, velocity = Forward * Speed

        float _maxForce;   // the maximum steering force this vehicle can apply
                           // (steering force is clipped to this magnitude)

        float _maxSpeed;   // the maximum speed this vehicle is allowed to move
                           // (velocity is clipped to this magnitude)

        float _curvature;
        Vector3 _lastForward;
        Vector3 _lastPosition;
        Vector3 _smoothedPosition;
        float _smoothedCurvature;
        Vector3 _smoothedAcceleration;

        //static int serialNumberCounter = 0;

        //int serialNumber;

        // Constructor
        public SimpleVehicle()
        {
            // set inital state
            reset();

            // maintain unique serial numbers
            //serialNumber = serialNumberCounter++;
        }

        // Reset vehicle state
        public virtual void reset ()
        {
            // reset LocalSpace state
            ResetLocalSpace ();

            // reset SteerLibraryMixin state
            // (XXX this seems really fragile, needs to be redesigned XXX)
            //SimpleVehicle_3.reset ();
            resetSteering();

            SetMass (1);          // mass (defaults to 1 so acceleration=force)
            SetSpeed (0);         // speed along Forward direction.

            SetRadius (0.5f);     // size of bounding sphere

            SetMaxForce (0.1f);   // steering force is clipped to this magnitude
            SetMaxSpeed (1.0f);   // velocity is clipped to this magnitude

            // reset bookkeeping to do running averages of these quanities
            resetSmoothedPosition (Vector3.zero);
            resetSmoothedCurvature(0);
            resetSmoothedAcceleration(Vector3.zero);
        }

        // get/set mass
        public override float Mass () {return _mass;}
        public override float SetMass(float m) { return _mass = m; }

        // get velocity of vehicle
        public override Vector3 Velocity() { return Forward() * _speed; }

        // get/set speed of vehicle  (may be faster than taking mag of velocity)
        public override float Speed() { return _speed; }
        public override float SetSpeed(float s) { return _speed = s; }

        // size of bounding sphere, for obstacle avoidance, etc.
        public override float Radius() { return _radius; }
        public override float SetRadius(float m) { return _radius = m; }

        // get/set maxForce
        public override float MaxForce() { return _maxForce; }
        public override float SetMaxForce(float mf) { return _maxForce = mf; }

        // get/set maxSpeed
        public override  float MaxSpeed() { return _maxSpeed; }
        public override float SetMaxSpeed(float ms) { return _maxSpeed = ms; }

        // get instantaneous curvature (since last update)
        float curvature () {return _curvature;}

        // get/reset smoothedCurvature, smoothedAcceleration and smoothedPosition
        float smoothedCurvature () {return _smoothedCurvature;}
        float resetSmoothedCurvature (float value)
        {
            _lastForward = Vector3.zero;
            _lastPosition = Vector3.zero;
            return _smoothedCurvature = _curvature = value;
        }
        Vector3 smoothedAcceleration () {return _smoothedAcceleration;}
        Vector3 resetSmoothedAcceleration (Vector3 value)
        {
            return _smoothedAcceleration = value;
        }
        Vector3 smoothedPosition () {return _smoothedPosition;}
        Vector3 resetSmoothedPosition (Vector3 value)
        {
            return _smoothedPosition = value;
        }

        void randomizeHeadingOnXZPlane ()
        {
            SetUp (Vector3.up);
            SetForward (OpenSteerUtility.RandomUnitVectorOnXZPlane ());
            SetSide (LocalRotateForwardToSide (Forward()));
        }
    
        // From CPP
        // ----------------------------------------------------------------------------
        // adjust the steering force passed to applySteeringForce.
        //
        // allows a specific vehicle class to redefine this adjustment.
        // default is to disallow backward-facing steering at low speed.
        //
        // xxx should the default be this ad-hocery, or no adjustment?
        // xxx experimental 8-20-02
        //
        // parameter names commented out to prevent compiler warning from "-W"
        public Vector3 adjustRawSteeringForce(Vector3 force)//, const float /* deltaTime */)
        {
            float maxAdjustedSpeed = 0.2f * MaxSpeed();

            if ((Speed() > maxAdjustedSpeed) || (force == Vector3.zero))
            {
                return force;
            }
            else
            {
                float range = Speed() / maxAdjustedSpeed;
                float cosine = OpenSteerUtility.interpolate((float) System.Math.Pow(range, 20), 1.0f, -1.0f);
                return OpenSteerUtility.limitMaxDeviationAngle(force, cosine, Forward());
            }
        }

        // ----------------------------------------------------------------------------
        // xxx experimental 9-6-02
        //
        // apply a given braking force (for a given dt) to our momentum.
        //
        // (this is intended as a companion to applySteeringForce, but I'm not sure how
        // well integrated it is.  It was motivated by the fact that "braking" (as in
        // "capture the flag" endgame) by using "forward * speed * -rate" as a steering
        // force was causing problems in adjustRawSteeringForce.  In fact it made it
        // get NAN, but even if it had worked it would have defeated the braking.
        //
        // maybe the guts of applySteeringForce should be split off into a subroutine
        // used by both applySteeringForce and applyBrakingForce?
        void applyBrakingForce (float rate, float deltaTime)
        {
            float rawBraking = Speed () * rate;
            float clipBraking = ((rawBraking < MaxForce ()) ?
                                       rawBraking :
                                       MaxForce ());

            SetSpeed (Speed () - (clipBraking * deltaTime));
        }

        // ----------------------------------------------------------------------------
        // apply a given steering force to our momentum,
        // adjusting our orientation to maintain velocity-alignment.
        public void applySteeringForce(Vector3 force, float elapsedTime)
        {
            Vector3 adjustedForce = adjustRawSteeringForce (force);//, elapsedTime);

            // enforce limit on magnitude of steering force
            //Vector3 clippedForce = adjustedForce.truncateLength (maxForce ());
            //Vector3 clippedForce = adjustedForce.truncateLength(maxForce());

            Vector3 clippedForce = truncateLength(adjustedForce, MaxForce());

            // compute acceleration and velocity
            Vector3 newAcceleration = (clippedForce / Mass());
            Vector3 newVelocity = Velocity();

            // damp out abrupt changes and oscillations in steering acceleration
            // (rate is proportional to time step, then clipped into useful range)
            if (elapsedTime > 0)
            {
                float smoothRate = OpenSteerUtility.clip(9 * elapsedTime, 0.15f, 0.4f);
                _smoothedAcceleration = OpenSteerUtility.blendIntoAccumulator(smoothRate,
                                      newAcceleration,
                                      _smoothedAcceleration);
            }

            // Euler integrate (per frame) acceleration into velocity
            newVelocity += _smoothedAcceleration * elapsedTime;

            // enforce speed limit
            
            //newVelocity = newVelocity.truncateLength (maxSpeed ());
            newVelocity = truncateLength(newVelocity,MaxSpeed());


            // update Speed
            SetSpeed (newVelocity.magnitude);

           
            // Euler integrate (per frame) velocity into position
            SetPosition (Position + (newVelocity * elapsedTime));

            // regenerate local space (by default: align vehicle's forward axis with
            // new velocity, but this behavior may be overridden by derived classes.)
            regenerateLocalSpace (newVelocity);//, elapsedTime);

            // maintain path curvature information
            measurePathCurvature (elapsedTime);

            // running average of recent positions
            _smoothedPosition=OpenSteerUtility.blendIntoAccumulator(elapsedTime * 0.06f, // QQQ
                                  Position ,
                                  _smoothedPosition);
        }

        // ----------------------------------------------------------------------------
        // the default version: keep FORWARD parallel to velocity, change UP as
        // little as possible.
        //
        // parameter names commented out to prevent compiler warning from "-W"
        void regenerateLocalSpace (Vector3 newVelocity)
        {
            // adjust orthonormal basis vectors to be aligned with new velocity
            if (Speed() > 0) RegenerateOrthonormalBasisUF (newVelocity / Speed());
        }

        // ----------------------------------------------------------------------------
        // alternate version: keep FORWARD parallel to velocity, adjust UP according
        // to a no-basis-in-reality "banking" behavior, something like what birds and
        // airplanes do

        // XXX experimental cwr 6-5-03
        public void regenerateLocalSpaceForBanking (Vector3 newVelocity, float elapsedTime)
        {
            // the length of this global-upward-pointing vector controls the vehicle's
            // tendency to right itself as it is rolled over from turning acceleration
            Vector3 globalUp =new Vector3(0, 0.2f, 0);

            // acceleration points toward the center of local path curvature, the
            // length determines how much the vehicle will roll while turning
            Vector3 accelUp = _smoothedAcceleration * 0.05f;

            // combined banking, sum of UP due to turning and global UP
            Vector3 bankUp = accelUp + globalUp;

            // blend bankUp into vehicle's UP basis vector
            float smoothRate = elapsedTime * 3;
            Vector3 tempUp = Up();
            tempUp=OpenSteerUtility.blendIntoAccumulator(smoothRate, bankUp, tempUp);
            tempUp.Normalize();
            SetUp (tempUp);

        //  annotationLine (position(), position() + (globalUp * 4), gWhite);  // XXX
        //  annotationLine (position(), position() + (bankUp   * 4), gOrange); // XXX
        //  annotationLine (position(), position() + (accelUp  * 4), gRed);    // XXX
        //  annotationLine (position(), position() + (up ()    * 1), gYellow); // XXX

            // adjust orthonormal basis vectors to be aligned with new velocity
            if (Speed() > 0) RegenerateOrthonormalBasisUF (newVelocity / Speed());
        }

        // ----------------------------------------------------------------------------
        // measure path curvature (1/turning-radius), maintain smoothed version
        void measurePathCurvature (float elapsedTime)
        {
            if (elapsedTime > 0)
            {
                Vector3 dP = _lastPosition - Position;
                Vector3 dF = (_lastForward - Forward ()) / dP.magnitude;
                //SI - BIT OF A WEIRD FIX HERE . NOT SURE IF ITS CORRECT
                //Vector3 lateral = dF.perpendicularComponent (forward ());
                Vector3 lateral = OpenSteerUtility.perpendicularComponent( dF,Forward());

                float sign = (Vector3.Dot(lateral, Side()) < 0) ? 1.0f : -1.0f;
                _curvature = lateral.magnitude * sign;
                //OpenSteerUtility.blendIntoAccumulator(elapsedTime * 4.0f, _curvature,_smoothedCurvature);
                _smoothedCurvature=OpenSteerUtility.blendIntoAccumulator(elapsedTime * 4.0f, _curvature, _smoothedCurvature);

                _lastForward = Forward ();
                _lastPosition = Position;
            }
        }

        // ----------------------------------------------------------------------------
        // draw lines from vehicle's position showing its velocity and acceleration
        void annotationVelocityAcceleration (float maxLengthA,  float maxLengthV)
        {
            //float desat = 0.4f;
            //float aScale = maxLengthA / MaxForce ();
            //float vScale = maxLengthV / MaxSpeed ();
            //Vector3 p = Position;
            //Vector3 aColor = new Vector3(desat, desat, 1); // bluish
            //Vector3 vColor = new Vector3 (1, desat, 1); // pinkish

            //annotationLine (p, p + (velocity ()           * vScale), vColor);
            //annotationLine (p, p + (_smoothedAcceleration * aScale), aColor);
        }

        // ----------------------------------------------------------------------------
        // predict position of this vehicle at some time in the future
        // (assumes velocity remains constant, hence path is a straight line)
        //
        // XXX Want to encapsulate this since eventually I want to investigate
        // XXX non-linear predictors.  Maybe predictFutureLocalSpace ?
        //
        // XXX move to a vehicle utility mixin?
        public override Vector3 PredictFuturePosition(float predictionTime)
        {
            return Position + (Velocity() * predictionTime);
        }
    }
}

