using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace VehicleEnterExit
{
    public class VehicleEnter : MonoBehaviour
    {
        public VehicleSync VehicleSync;
        public int SeatID;

        void Start() 
        {
            /// <summary>
            /// I had to put "EnterColliders" on their own layer because sometimes bullets were hitting them and vehicles avoided getting damage
            /// </summary>
            gameObject.layer = 9;
        }

    }
}