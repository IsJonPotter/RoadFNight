using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class _block : MonoBehaviour
{
   // public int _regionNumber;
    public Color color;
    public bool ShowBlockContainer;

    public enum Regions {Region0, Region1, Region2,Region3,Region4,Region5,Region6,Region7}
    public Regions _Region = 0;
    public List<District> _districts = new List<District>();

    public PedestrianSystem pedestrianSystem;
    public List<Pedestrian> pedestrian_lst;

    public void try_add_pedestrian(Pedestrian pedestrian)
    {
        if (math.abs(pedestrian.transform.position.x - transform.position.x) < 40 &&
            math.abs(pedestrian.transform.position.z - transform.position.z) < 40)
        {
            pedestrian_lst.Add(pedestrian);
        }
    }

    private void OnDrawGizmos()
    {
        if (ShowBlockContainer)
        {
            if ((int)_Region == 0 )
            {
                color = Color.blue;
                color.a = 0.5f;
            }
            if ((int)_Region == 1)
            {
                color = Color.red;
                color.a = 0.5f;
            }
            if ((int)_Region == 2)
            {
                color = Color.green;
                color.a = 0.5f;
            }
            if ((int)_Region == 3)
            {
                color = Color.yellow;
                color.a = 0.5f;
            }
            if ((int)_Region == 4)
            {
                color = Color.magenta;
                color.a = 0.5f;
            }
            if ((int)_Region == 5)
            {
                color = Color.cyan;
                color.a = 0.5f;
            }
            if ((int)_Region == 6)
            {
                color = Color.white;
                color.a = 0.5f;
            }
            if ((int)_Region == 7)
            {
                color = Color.black;
                color.a = 0.5f;
            }

            Gizmos.color = color;
            Gizmos.DrawWireCube(transform.position + new Vector3(0, 40f, 0), new Vector3(80f, 80f, 80f));

            Gizmos.color = color;
            Gizmos.DrawCube(transform.position + new Vector3(0, 40f, 0), new Vector3(80f, 80f, 80f));
        }
    }

    
}
