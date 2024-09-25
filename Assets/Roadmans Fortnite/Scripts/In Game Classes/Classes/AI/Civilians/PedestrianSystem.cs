using Gley.PedestrianSystem;
using Photon.Realtime;
using Redcode.Pools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianSystem : MonoBehaviour
{
    public PoolManager pool_manager;
    public CityGen myCity;
    public List<GameObject> player_lst = new List<GameObject>();
    public float visible_threshold = 10;

    private void Awake()
    {
        init_block_pedestrian();//init secondary brain
    }

    void Update()
    {
        foreach (var pedestrian in GetComponentsInChildren<Pedestrian>(true))
        {
            Dictionary<string, float> distance_dict = calculate_distance_to_players(pedestrian);
            //update visible_dict in pedestrian
            foreach (var player in distance_dict.Keys)
            {
                pedestrian.visible_dict[player] = update_visible_state(distance_dict[player]);
            }
        }
    }
    
    private void init_block_pedestrian()
    {
        foreach (var block in myCity.listOfBlocks)
        {
            foreach (var pedestrian in GetComponentsInChildren<Pedestrian>(true))
            {
                block.try_add_pedestrian(pedestrian);
            }
        }
        
    }

    //Calculate the distance between pedestrian and players
    public Dictionary<string, float> calculate_distance_to_players(Pedestrian pedestrian)
    {
        Dictionary<string, float> distance_dict = new Dictionary<string, float>();
        
        foreach (var player in player_lst)
        {
            Vector2 player_pos = new Vector2(player.transform.position.x, player.transform.position.z);
            Vector2 _pos = new Vector2(pedestrian.transform.position.x, pedestrian.transform.position.z);
            distance_dict[player.name] = Vector2.Distance(player_pos, _pos);
        }
        foreach (var i in distance_dict)
        {
            Debug.Log(i);
        }
        return distance_dict;
    }

    public bool update_visible_state(float distance)
    {
        return distance > visible_threshold ? false : true;
    }
}
