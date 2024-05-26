using StarterAssets;
using System;
using UnityEngine;

namespace UnityStandardAssets.Vehicles.Aeroplane
{
    [RequireComponent(typeof (AeroplaneController))]
    public class AeroplaneUserControl2Axis : Vehicle
    {
        // these max angles are only used on mobile, due to the way pitch and roll input are handled
        public float maxRollAngle = 80;
        public float maxPitchAngle = 80;
        private float m_Throttle;
        private StarterAssetsInputs _input;

        // reference to the aeroplane that we're controlling
        private AeroplaneController m_Aeroplane;

       // public bool canFly = false;


        private void Awake()
        {
            // Set up the reference to the aeroplane controller.
            m_Aeroplane = GetComponent<AeroplaneController>();
        }


        private void FixedUpdate()
        {
            if(_input == null)
                _input = GameObject.FindGameObjectWithTag("InputManager").GetComponent<StarterAssetsInputs>();
            // if (canFly == true)
            if (canDrive == true)
            {
                // Read input for the pitch, yaw, roll and throttle of the aeroplane.
                float roll = _input.move.x;
                float pitch = _input.move.y;
                bool airBrakes = _input.aim;
                m_Throttle = -_input.look.y;
#if MOBILE_INPUT
            AdjustInputForMobileControls(ref roll, ref pitch, ref m_Throttle);
#endif
                // Pass the input to the aeroplane
                m_Aeroplane.Move(roll, pitch, 0, m_Throttle, airBrakes);
            }
        }


        private void AdjustInputForMobileControls(ref float roll, ref float pitch, ref float throttle)
        {
            // because mobile tilt is used for roll and pitch, we help out by
            // assuming that a centered level device means the user
            // wants to fly straight and level!

            // this means on mobile, the input represents the *desired* roll angle of the aeroplane,
            // and the roll input is calculated to achieve that.
            // whereas on non-mobile, the input directly controls the roll of the aeroplane.

            float intendedRollAngle = roll*maxRollAngle*Mathf.Deg2Rad;
            float intendedPitchAngle = pitch*maxPitchAngle*Mathf.Deg2Rad;
            roll = Mathf.Clamp((intendedRollAngle - m_Aeroplane.RollAngle), -1, 1);
            pitch = Mathf.Clamp((intendedPitchAngle - m_Aeroplane.PitchAngle), -1, 1);

            // similarly, the throttle axis input is considered to be the desired absolute value, not a relative change to current throttle.
            float intendedThrottle = throttle*0.5f + 0.5f;
            throttle = Mathf.Clamp(intendedThrottle - m_Aeroplane.Throttle, -1, 1);
        }
    }
}
