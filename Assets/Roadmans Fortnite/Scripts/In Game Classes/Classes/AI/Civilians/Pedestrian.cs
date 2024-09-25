using Redcode.Pools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pedestrian : MonoBehaviour, IPoolObject
{
    public enum State { Alive, Died }
    public int Health { get; private set; }
    private State _state;
    private PedestrianSystem _system;

    //For server to allocate visible state to players
    //e.g. {player1 : true, plaer2 : false, player3 : true}
    public Dictionary<string, bool> visible_dict = new Dictionary<string, bool>();

    private void Awake()
    {
        _system = GetComponentInParent<PedestrianSystem>();
    }

    // Called when getting this object from pool.
    public void OnGettingFromPool()
    {
        _state = State.Alive;
        Health = 100;
    }

    public void OnCreatedInPool()
    {
        throw new System.NotImplementedException();
    }
}

