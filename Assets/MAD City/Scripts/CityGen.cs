using System.Collections;
using Roadmans_Fortnite.Scripts.Classes.Stats.Enums;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static DistrictData;
using Redcode.Pools;

[ExecuteInEditMode]
public class CityGen : MonoBehaviour
{
    [Range(1, 9)]
    public int gridX = 5;
    [Range(1, 9)]
    public int gridZ = 5;

    public Vector3 gridOrigin = Vector3.zero;
    public float gridOffset = 2f;
    public bool generateOnEnable;

    public bool cityEdgeWalls;
    public bool hasBeach;
    public bool hasPier;
    public bool hasExit;

    public List<RegionsData> _regions;
    public List<DistrictData> _districtData;

    [System.Serializable]
    public class CityPerimeterPrefabs
    {
        public GameObject prefabEdge;
        public GameObject[] prefabEdgeGap;
        public GameObject prefabCorner;
        public GameObject PrefabEdgeOuter;
        public GameObject PrefabEdgeInner;
        public GameObject RiverEnd;
        public GameObject RiverEndInner;
        public GameObject RiverEndOuter;
        public GameObject[] Rivermiddle;
        public GameObject BeachFiller;
        public GameObject BeachEdge;
        public GameObject Pier;
        public GameObject BeachEdgeEnd;
    }

    public CityPerimeterPrefabs cityPerimeterPrefabs;

    [SerializeField] private Quaternion[] placementAngle;

    public _block[] listOfBlocks;
    public District[] listOfDistricts;
    public _building[] listOfBuildings;
    public _buildingLarge[] listOfLargeBuildings;
    public _poi[] listOfPOIs;

    public GameObject GeneratedBlocks;

    public GameObject Perimeter;

    public GameObject GeneratedBuildings;

    public GameObject BuildingEmptyGameObject;

    // [Header("Buildings")]
    public GameObject[] POIs;

    public bool AddRiver = false;
    public bool RiverPositionRandom = false;

    public GameObject OceanTile;
    public GameObject GeneratedWater;

    [Range(1, 9)]
    public int RiverPositionX = 5;
    //[Header("Colours")]
    //public Color[] Colors;

    [System.Serializable]
    public class OverrideClass
    {
        public int GridXPosition = 0;
        public int GrivdZPosition = 0;
        public Quaternion GridRotation = new Quaternion(0, 0, 0, 0);
        public GameObject Block;
    }
    public List<OverrideClass> Overrides = new List<OverrideClass>();

    public int ovrn = 0;
    private int randFlip = 0;

    void OnEnable()
    {
        if (generateOnEnable)
        {
            Generate();
        }
        //numberOfRegions = regionsData.Length;
        // Use modulus division to determine if slider value is odd
        if (gridX % 2 == 0)
        {
            gridX = gridX--;

        }

        if (GeneratedBlocks == null)
        {
            GeneratedBlocks = new GameObject("GeneratedBlocks");
            GeneratedBlocks.transform.parent = transform;
        }

        if (Perimeter == null)
        {

            Perimeter = new GameObject("Perimeter");
            Perimeter.transform.parent = transform;
        }

        if (GeneratedBuildings == null)
        {
            GeneratedBuildings = new GameObject("GeneratedBuildings");
            GeneratedBuildings.transform.parent = transform;
        }

        if (BuildingEmptyGameObject == null)
        {
            BuildingEmptyGameObject = new GameObject("BuildingEmptyGameObject");
            BuildingEmptyGameObject.transform.parent = transform;
        }
    }

    public void RandomValues()
    {
        if (Random.value >= 0.66)
        {
            gridX = 7;
        }
        else if (Random.value <= 0.33)
        {
            gridX = 3;
        }
        else
        {
            gridX = 5;
        }

        gridZ = Random.Range(2, 7);

        hasBeach = (Random.value > 0.5);
        hasPier = (Random.value > 0.5);
        AddRiver = (Random.value > 0.5);
        RiverPositionRandom = true;
    }

    /* public void AddOcean()
     {
         for (int x = 0; x < gridX; x++)
         {
             for (int z = 0; z < gridZ; z++)
             {
                 GameObject clone = Instantiate(OceanTile, transform.position + 
                                     (gridOrigin - new Vector3(gridOffset*2, -1f, gridOffset*2)) + 
                                     new Vector3(gridOffset * x*2, 1f, gridOffset * z*2), transform.rotation);
                 clone.transform.SetParent(GeneratedWater.transform);
             }
         }
     }*/

    public void Generate()
    {
        SpawnBlocks();
    }

    //Spawn City Blocks and perimeter
    public void SpawnBlocks()
    {
        //overrides variable
        ovrn = 0;

        if (RiverPositionRandom)
        {
            RiverPositionX = Random.Range(1, gridX - 1);
        }

        for (int x = 0; x < gridX; x++)
        {

            int stagger = 0;
            int[] array = { -1, 1, 0 };
            int rand = Random.Range(0, 3);
            int gridOff = (int)gridOffset * array[rand] / 2;

            for (int z = 0; z < gridZ; z++)
            {
                #region Blocks
                //if line x-axis is even then stagger = 0 this means
                //only odd numbers will move (stability of models joining together)
                if (x % 2 == 0) { stagger = 0; }
                else if (x % 2 == 1) { stagger = gridOff; }

                //Instantiates a random block from the list into each x & y position
                int chooseRegion = Random.Range(0, _regions.Count);
                int chooseBlock = Random.Range(0, _regions[chooseRegion].Blocks.Length);
                GameObject clone = Instantiate(_regions[chooseRegion].Blocks[chooseBlock],
                        transform.position + gridOrigin + new Vector3(gridOffset * x, 0, gridOffset * z + stagger),
                        placementAngle[Random.Range(0, 3)]);

                clone.transform.SetParent(GeneratedBlocks.transform);
                clone.GetComponentInParent<_block>()._Region = (_block.Regions)chooseRegion;
                #endregion

                Vector3 pos = transform.position + gridOrigin +
                    new Vector3(gridOffset * x, 0, gridOffset * z + stagger);
                Collider[] col;

                #region River
                if (!AddRiver)
                {
                    if (Overrides.Count() > ovrn &&
                        x == Overrides[ovrn].GridXPosition && z == Overrides[ovrn].GrivdZPosition)
                    {
                        //Generate Overrides[ovrn].Block instead of a random block
                        DestroyImmediate(clone.gameObject);
                        GameObject OvrrideGO = Instantiate(Overrides[ovrn].Block, pos, Overrides[ovrn].GridRotation);

                        OvrrideGO.transform.SetParent(GeneratedBlocks.transform);
                        ovrn++;
                    }
                }
                else
                {
                    if (RiverPositionX >= x)
                    {
                    }

                    if (RiverPositionX == x)
                    {
                        if (z != 0 && z != gridZ - 1)
                        {
                            DestroyImmediate(clone.gameObject);

                            GameObject clone1 = Instantiate(cityPerimeterPrefabs.Rivermiddle[Random.Range(0, cityPerimeterPrefabs.Rivermiddle.Length)],
                                pos, transform.rotation);

                            clone1.transform.SetParent(GeneratedBlocks.transform);
                        }
                        if (z == 0 && stagger == 0)
                        {
                            DestroyImmediate(clone.gameObject);

                            GameObject clone1 = Instantiate(cityPerimeterPrefabs.RiverEnd,
                                pos, placementAngle[2]);

                            clone1.transform.SetParent(GeneratedBlocks.transform);
                        }
                        if (z == gridZ - 1 && stagger == 0)
                        {
                            DestroyImmediate(clone.gameObject);

                            GameObject clone1 = Instantiate(cityPerimeterPrefabs.RiverEnd,
                                pos, placementAngle[0]);
                            
                            clone1.transform.SetParent(GeneratedBlocks.transform);
                        }
                        if (z == 0 && stagger < 0)
                        {
                            DestroyImmediate(clone.gameObject);

                            GameObject clone1 = Instantiate(cityPerimeterPrefabs.RiverEndOuter,
                                pos, placementAngle[2]);
                            
                            clone1.transform.SetParent(GeneratedBlocks.transform);
                        }
                        if (z == gridZ - 1 && stagger < 0)
                        {
                            DestroyImmediate(clone.gameObject);

                            GameObject clone1 = Instantiate(cityPerimeterPrefabs.RiverEndInner,
                                pos, placementAngle[0]);
                            
                            clone1.transform.SetParent(GeneratedBlocks.transform);
                        }
                        if (z == 0 && stagger > 0)
                        {
                            DestroyImmediate(clone.gameObject);

                            GameObject clone1 = Instantiate(cityPerimeterPrefabs.RiverEndInner,
                                pos, placementAngle[2]);
                            
                            clone1.transform.SetParent(GeneratedBlocks.transform);
                        }
                        if (z == gridZ - 1 && stagger > 0)
                        {
                            DestroyImmediate(clone.gameObject);

                            GameObject clone1 = Instantiate(cityPerimeterPrefabs.RiverEndOuter,
                                pos, placementAngle[0]);
                            
                            clone1.transform.SetParent(GeneratedBlocks.transform);
                        }
                    }

                    if (Overrides.Count() > ovrn && 
                        x == Overrides[ovrn].GridXPosition && z == Overrides[ovrn].GrivdZPosition)
                    {
                        DestroyImmediate(clone.gameObject);

                        GameObject OvrrideGO = Instantiate(Overrides[ovrn].Block,
                            pos, Overrides[ovrn].GridRotation);

                        OvrrideGO.transform.SetParent(GeneratedBlocks.transform);
                        ovrn++;
                    }
                }
                #endregion


                #region Straight Edges
                // FillEdges - these are additional to the  grid size already stated
                //First X Row (x=0)
                pos = transform.position + gridOrigin + 
                    new Vector3(gridOffset * x - gridOffset / 2, 0, gridOffset * z);
                if (x == 0)
                {
                    GameObject edge = Instantiate(cityPerimeterPrefabs.prefabEdge, pos, placementAngle[0]);
                    edge.transform.SetParent(Perimeter.transform);

                    if (cityEdgeWalls)
                    {
                        edge.GetComponent<MeshRenderer>().enabled = true;
                        edge.GetComponent<BoxCollider>().enabled = true;
                    }
                    else
                    {
                        edge.GetComponent<MeshRenderer>().enabled = false;
                        edge.GetComponent<BoxCollider>().enabled = false;
                    }
                }

                //Furthest (+X Row)
                pos = transform.position + gridOrigin +
                            new Vector3(gridOffset * x + gridOffset / 2, 0, gridOffset * z);
                if (!hasBeach)
                {
                    if (x == gridX - 1)
                    {
                        

                        GameObject edge = Instantiate(cityPerimeterPrefabs.prefabEdge, pos, placementAngle[2]);
                        edge.transform.SetParent(Perimeter.transform);

                        col = edge.GetComponents<BoxCollider>();
                        if (cityEdgeWalls)
                        {
                            edge.GetComponent<MeshRenderer>().enabled = true;
                            foreach (Collider collider in col)
                            {
                                collider.enabled = true;
                            }
                        }
                        else
                        {
                            edge.GetComponent<MeshRenderer>().enabled = false;
                            foreach (Collider collider in col)
                            {
                                collider.enabled = false;
                            }
                        }
                    }
                }
                else
                {
                    if (!hasPier)
                    {
                        if (x == gridX - 1)
                        {
                            GameObject edge = Instantiate(cityPerimeterPrefabs.BeachEdge, pos, placementAngle[2]);
                            edge.transform.SetParent(Perimeter.transform);
                        }
                    }
                    else
                    {
                        int pierSpace = Random.Range(1, gridZ - 1);

                        if (x == gridX - 1 && z != pierSpace)
                        {
                            GameObject edge = Instantiate(cityPerimeterPrefabs.BeachEdge, pos, placementAngle[2]);
                            edge.transform.SetParent(Perimeter.transform);
                        }

                        if (x == gridX - 1 & z == pierSpace)
                        {
                            GameObject edge = Instantiate(cityPerimeterPrefabs.Pier, pos, placementAngle[2]);
                            edge.transform.SetParent(Perimeter.transform);
                        }
                    }
                }
                #endregion

                #region Z=Z-1 Row (Final Z Row)
                pos = transform.position + gridOrigin + 
                    new Vector3(gridOffset * x, 0, gridOffset * z + gridOffset / 2);
                Vector3 offset_pos = transform.position + gridOrigin +
                    new Vector3(gridOffset * x, 0, gridOffset * z + gridOffset / 2 + gridOff);

                //staggered inner and outer prefabs (+z Row)
                if (z == gridZ - 1 && (RiverPositionX != x || !AddRiver))
                {
                    if (stagger == 0)
                    {
                        GameObject edge = Instantiate(cityPerimeterPrefabs.prefabEdge, pos, placementAngle[1]);
                        edge.transform.SetParent(Perimeter.transform);
                        col = edge.GetComponents<BoxCollider>();
                        if (cityEdgeWalls)
                        {
                            edge.GetComponent<MeshRenderer>().enabled = true;
                            foreach (Collider collider in col)
                            {
                                collider.enabled = true;
                            }
                        }
                        else
                        {
                            edge.GetComponent<MeshRenderer>().enabled = false;
                            foreach (Collider collider in col)
                            {
                                collider.enabled = false;
                            }
                        }
                    }
                    else if (stagger > 0)
                    {
                        GameObject edge = Instantiate(cityPerimeterPrefabs.PrefabEdgeOuter, offset_pos, placementAngle[1]);
                        edge.transform.SetParent(Perimeter.transform);
                        if (cityEdgeWalls)
                        {
                            edge.GetComponent<MeshRenderer>().enabled = true;
                            col = edge.GetComponents<BoxCollider>();
                            foreach (Collider collider in col)
                            {
                                collider.enabled = true;
                            }
                        }
                        else
                        {
                            edge.GetComponent<MeshRenderer>().enabled = false;
                            col = edge.GetComponents<BoxCollider>();
                            foreach (Collider collider in col)
                            {
                                collider.enabled = false;
                            }
                        }
                    }
                    else if(stagger < 0)
                    {
                        GameObject edge = Instantiate(cityPerimeterPrefabs.PrefabEdgeInner, offset_pos, placementAngle[1]);
                        edge.transform.SetParent(Perimeter.transform);
                        col = edge.GetComponents<BoxCollider>();
                        if (cityEdgeWalls)
                        {
                            edge.GetComponent<MeshRenderer>().enabled = true;
                            foreach (Collider collider in col)
                            {
                                collider.enabled = true;
                            }
                        }
                        else
                        {
                            edge.GetComponent<MeshRenderer>().enabled = false;
                            foreach (Collider collider in col)
                            {
                                collider.enabled = false;
                            }
                        }
                    }
                }

                #endregion

                #region Z=0 Row (First Z Row)
                pos = transform.position + gridOrigin + 
                    new Vector3(gridOffset * x, 0, gridOffset * z - gridOffset / 2);
                offset_pos = transform.position + gridOrigin + 
                    new Vector3(gridOffset * x, 0, gridOffset * z - gridOffset / 2 + gridOff);
                //staggered inner and outer prefabs (z Row)
                if (z == 0 && (RiverPositionX != x || !AddRiver))
                {
                    if (stagger == 0)
                    {
                        GameObject edge = Instantiate(cityPerimeterPrefabs.prefabEdge, pos, placementAngle[3]);
                        edge.transform.SetParent(Perimeter.transform);
                        if (cityEdgeWalls)
                        {
                            edge.GetComponent<MeshRenderer>().enabled = true;
                            edge.GetComponent<BoxCollider>().enabled = true;
                        }
                        else
                        {
                            edge.GetComponent<MeshRenderer>().enabled = false;
                            edge.GetComponent<BoxCollider>().enabled = false;
                        }
                    }
                    if (stagger > 0)
                    {
                        GameObject edge = Instantiate(cityPerimeterPrefabs.PrefabEdgeInner, offset_pos, placementAngle[3]);
                        edge.transform.SetParent(Perimeter.transform);
                        col = edge.GetComponents<BoxCollider>();
                        if (cityEdgeWalls)
                        {
                            edge.GetComponent<MeshRenderer>().enabled = true;
                            foreach (Collider collider in col)
                            {
                                collider.enabled = true;
                            }
                        }
                        else
                        {
                            edge.GetComponent<MeshRenderer>().enabled = false;
                            foreach (Collider collider in col)
                            {
                                collider.enabled = false;
                            }
                        }
                    }
                    if (stagger < 0)
                    {
                        GameObject edge = Instantiate(cityPerimeterPrefabs.PrefabEdgeOuter, offset_pos, placementAngle[3]);
                        edge.transform.SetParent(Perimeter.transform);
                        col = edge.GetComponents<BoxCollider>();
                        if (cityEdgeWalls)
                        {
                            edge.GetComponent<MeshRenderer>().enabled = true;
                            foreach (Collider collider in col)
                            {
                                collider.enabled = true;
                            }
                        }
                        else
                        {
                            edge.GetComponent<MeshRenderer>().enabled = false;
                            foreach (Collider collider in col)
                            {
                                collider.enabled = false;
                            }
                        }
                    }
                }
                #endregion

                #region Corners
                //Fill Corners - these are additional to the  grid size already stated
                if (stagger == 0)
                {
                    if (x == 0 && z == 0)
                    {
                        pos = transform.position + gridOrigin +
                            new Vector3(gridOffset * x - gridOffset / 2, 0, gridOffset * z - gridOffset / 2);
                        GameObject corner = Instantiate(cityPerimeterPrefabs.prefabCorner, pos, placementAngle[0]);
                        corner.transform.SetParent(Perimeter.transform);
                        col = corner.GetComponents<BoxCollider>();
                        if (cityEdgeWalls)
                        {
                            corner.GetComponent<MeshRenderer>().enabled = true;
                            foreach (Collider collider in col)
                            {
                                collider.enabled = true;
                            }
                        }
                        else
                        {
                            corner.GetComponent<MeshRenderer>().enabled = false;
                            foreach (Collider collider in col)
                            {
                                collider.enabled = false;
                            }
                        }
                    }

                    if (x == gridX - 1 && z == 0)
                    {
                        pos = transform.position + gridOrigin + 
                            new Vector3(gridOffset * x + gridOffset / 2, 0, gridOffset * z - gridOffset / 2);
                        GameObject corner = Instantiate(cityPerimeterPrefabs.prefabCorner, pos, placementAngle[3]);
                        corner.transform.SetParent(Perimeter.transform);
                        col = corner.GetComponents<BoxCollider>();
                        if (cityEdgeWalls)
                        {
                            corner.GetComponent<MeshRenderer>().enabled = true;
                            foreach (Collider collider in col)
                            {
                                collider.enabled = true;
                            }
                        }
                        else
                        {
                            corner.GetComponent<MeshRenderer>().enabled = false;
                            foreach (Collider collider in col)
                            {
                                collider.enabled = false;
                            }
                        }
                    }

                    pos = transform.position + gridOrigin + 
                        new Vector3(gridOffset * x - gridOffset / 2, 0, gridOffset * z + gridOffset / 2);
                    if (x == 0 && z == gridZ - 1)
                    {
                        pos = transform.position + gridOrigin +
                            new Vector3(gridOffset * x - gridOffset / 2, 0, gridOffset * z + gridOffset / 2);
                        GameObject corner = Instantiate(cityPerimeterPrefabs.prefabCorner, pos, placementAngle[1]);
                        corner.transform.SetParent(Perimeter.transform);
                        col = corner.GetComponents<BoxCollider>();
                        if (cityEdgeWalls)
                        {
                            corner.GetComponent<MeshRenderer>().enabled = true;
                            foreach (Collider collider in col)
                            {
                                collider.enabled = true;
                            }
                        }
                        else
                        {
                            corner.GetComponent<MeshRenderer>().enabled = false;
                            foreach (Collider collider in col)
                            {
                                collider.enabled = false;
                            }
                        }
                    }

                    if (x == gridX - 1 && z == gridZ - 1)
                    {
                        pos = transform.position + gridOrigin + 
                            new Vector3(gridOffset * x + gridOffset / 2, 0, gridOffset * z + gridOffset / 2);
                        GameObject corner = Instantiate(cityPerimeterPrefabs.prefabCorner, pos, placementAngle[2]);
                        corner.transform.SetParent(Perimeter.transform);
                        col = corner.GetComponents<BoxCollider>();
                        if (cityEdgeWalls)
                        {
                            corner.GetComponent<MeshRenderer>().enabled = true;
                            foreach (Collider collider in col)
                            {
                                collider.enabled = true;
                            }
                        }
                        else
                        {
                            corner.GetComponent<MeshRenderer>().enabled = false;
                            foreach (Collider collider in col)
                            {
                                collider.enabled = false;
                            }
                        }
                    }
                }
                #endregion

                #region Fill Gaps on X
                int EdgeFiller;

                if (hasExit)
                {
                    EdgeFiller = Random.Range(0, cityPerimeterPrefabs.prefabEdgeGap.Length);
                }
                else
                {
                    EdgeFiller = 0;
                }

                
                //if we are on the last z row && Odd X Row && next x row has no stagger && not needed on the last x row - then add gap filler prefab
                if (z == gridZ - 1 && x % 2 == 1 && stagger == 0 && x < gridX - 1)
                {
                    pos = transform.position + gridOrigin +
                        new Vector3(gridOffset * x + gridOffset / 2, 0, gridOffset * z + gridOffset / 2);
                    GameObject edge = Instantiate(cityPerimeterPrefabs.prefabEdgeGap[EdgeFiller], pos, placementAngle[1]);
                    edge.transform.SetParent(Perimeter.transform);
                    col = edge.GetComponents<BoxCollider>();
                    if (cityEdgeWalls)
                    {
                        edge.GetComponent<MeshRenderer>().enabled = true;
                        foreach (Collider collider in col)
                        {
                            collider.enabled = true;
                        }
                    }
                    else
                    {
                        edge.GetComponent<MeshRenderer>().enabled = false;
                        foreach (Collider collider in col)
                        {
                            collider.enabled = false;
                        }
                    }

                    pos = transform.position + gridOrigin +
                        new Vector3(gridOffset * x - gridOffset / 2, 0, gridOffset * z + gridOffset / 2);
                    GameObject edge2 = Instantiate(cityPerimeterPrefabs.prefabEdgeGap[EdgeFiller], pos, placementAngle[1]);
                    edge2.transform.SetParent(Perimeter.transform);
                    col = edge2.GetComponents<BoxCollider>();
                    if (cityEdgeWalls)
                    {
                        edge2.GetComponent<MeshRenderer>().enabled = true;
                        foreach (Collider collider in col)
                        {
                            collider.enabled = true;
                        }
                    }
                    else
                    {
                        edge2.GetComponent<MeshRenderer>().enabled = false;
                        foreach (Collider collider in col)
                        {
                            collider.enabled = false;
                        }
                    }
                }

                //if we are on the first z row && Odd X Row && next x row has no stagger && not needed on the last x row - then add gap filler prefab
                if (z == 0 && x % 2 == 1 && stagger == 0 && x < gridX - 1)
                {
                    pos = transform.position + gridOrigin +
                        new Vector3(gridOffset * x + gridOffset / 2, 0, gridOffset * z - gridOffset / 2);
                    GameObject edge = Instantiate(cityPerimeterPrefabs.prefabEdgeGap[EdgeFiller], pos, placementAngle[3]);
                    edge.transform.SetParent(Perimeter.transform);
                    if (cityEdgeWalls)
                    {
                        edge.GetComponent<MeshRenderer>().enabled = true;
                        col = edge.GetComponents<BoxCollider>();
                        foreach (Collider collider in col)
                        {
                            collider.enabled = true;
                        }
                    }
                    else
                    {
                        edge.GetComponent<MeshRenderer>().enabled = false;
                        col = edge.GetComponents<BoxCollider>();
                        foreach (Collider collider in col)
                        {
                            collider.enabled = false;
                        }
                    }

                    pos = transform.position + gridOrigin + 
                        new Vector3(gridOffset * x - gridOffset / 2, 0, gridOffset * z - gridOffset / 2);
                    GameObject edge2 = Instantiate(cityPerimeterPrefabs.prefabEdgeGap[EdgeFiller], pos, placementAngle[3]);
                    edge2.transform.SetParent(Perimeter.transform);
                    col = edge.GetComponents<BoxCollider>();
                    if (cityEdgeWalls)
                    {
                        edge2.GetComponent<MeshRenderer>().enabled = true;
                        
                        foreach (Collider collider in col)
                        {
                            collider.enabled = true;
                        }
                    }
                    else
                    {
                        edge2.GetComponent<MeshRenderer>().enabled = false;
                        foreach (Collider collider in col)
                        {
                            collider.enabled = false;
                        }
                    }
                }
                #endregion

                #region Fill Gaps
                //if First X Row (not last z row) and zis last than last z -1
                if (x == 0 && z < gridZ - 1)
                {
                    pos = transform.position + gridOrigin + 
                        new Vector3(gridOffset * x - gridOffset / 2, 0, gridOffset * z + gridOffset / 2);
                    GameObject edge = Instantiate(cityPerimeterPrefabs.prefabEdgeGap[EdgeFiller], pos, placementAngle[0]);
                    edge.transform.SetParent(Perimeter.transform);
                    col = edge.GetComponents<BoxCollider>();
                    if (cityEdgeWalls)
                    {
                        edge.GetComponent<MeshRenderer>().enabled = true;
                        foreach (Collider collider in col)
                        {
                            collider.enabled = true;
                        }
                    }
                    else
                    {
                        edge.GetComponent<MeshRenderer>().enabled = false;
                        foreach (Collider collider in col)
                        {
                            collider.enabled = false;
                        }
                    }
                }

                if (!hasBeach)
                {
                    if (x == gridX - 1 && z < gridZ - 1)
                    {
                        pos = transform.position + gridOrigin + 
                            new Vector3(gridOffset * x + gridOffset / 2, 0, gridOffset * z + gridOffset / 2);
                        GameObject edge = Instantiate(cityPerimeterPrefabs.prefabEdgeGap[EdgeFiller], pos, placementAngle[2]);
                        edge.transform.SetParent(Perimeter.transform);
                        col = edge.GetComponents<BoxCollider>();
                        if (cityEdgeWalls)
                        {
                            edge.GetComponent<MeshRenderer>().enabled = true;
                            foreach (Collider collider in col)
                            {
                                collider.enabled = true;
                            }
                        }
                        else
                        {
                            edge.GetComponent<MeshRenderer>().enabled = false;
                            foreach (Collider collider in col)
                            {
                                collider.enabled = false;
                            }
                        }
                    }
                }
                if (hasBeach)
                {
                    if (x == gridX - 1 && z < gridZ - 1)
                    {
                        pos = transform.position + gridOrigin + 
                            new Vector3(gridOffset * x + gridOffset / 2, 0, gridOffset * z + gridOffset / 2);
                        GameObject edge = Instantiate(cityPerimeterPrefabs.BeachFiller, pos, placementAngle[2]);
                        edge.transform.SetParent(Perimeter.transform);
                    }
                    if (x == gridX - 1 && z == gridZ - 1)
                    {
                        pos = transform.position + gridOrigin + 
                            new Vector3(gridOffset * x + gridOffset / 2, 0, gridOffset * z + gridOffset / 2);
                        GameObject edgeEnd = Instantiate(cityPerimeterPrefabs.BeachEdgeEnd, pos, placementAngle[2]);
                        edgeEnd.transform.SetParent(Perimeter.transform);
                    }
                    if (x == gridX - 1 && z == 0)
                    {
                        pos = transform.position + gridOrigin + 
                            new Vector3(gridOffset * x + gridOffset / 2, 0, gridOffset * z - gridOffset / 2);
                        GameObject edgeEnd = Instantiate(cityPerimeterPrefabs.BeachEdgeEnd, pos, placementAngle[2]);
                        edgeEnd.transform.SetParent(Perimeter.transform);
                        edgeEnd.transform.localScale = new Vector3(1, 1, -1);
                    }
                }
                #endregion
            }
        }
        #region District
        DistrictData.Religion religion = (DistrictData.Religion)Random.Range(0, (int)DistrictData.Religion.RELIGION_NUM);
        int district_num = 0;
        foreach (var i in GeneratedBlocks.GetComponentsInChildren<District>())
        {
            i.init_district(_districtData, district_num);
            district_num++;
        }

        listOfDistricts = FindObjectsOfType<District>();
        #endregion
    }

    public void GenerateBuildings()
    {
        Build();
    }

    //Spawn Buildings (with Noise)
    void Build()
    {

        listOfBlocks = FindObjectsOfType<_block>();
        listOfBuildings = FindObjectsOfType<_building>();
        listOfLargeBuildings = FindObjectsOfType<_buildingLarge>();
        listOfPOIs = FindObjectsOfType<_poi>();

        int buildingnumber = 0;
        int thisRegion = 0;

        #region small_buildings
        foreach (_building mybuilding in listOfBuildings)
        {
            buildingnumber++;

            //each building get its blocks region number
            thisRegion = (int)mybuilding.GetComponentInParent<_block>()._Region;

            GameObject Building = Instantiate(BuildingEmptyGameObject, GeneratedBuildings.transform);//mybuilding.GetComponentInParent<_block>().gameObject.transform);
            Building.name = _regions[thisRegion].name + " Small - No:" + buildingnumber;

            //Flip buildings on the x-axis
            if (mybuilding.corner == false)
            {
                int[] array = { -1, 1 };
                randFlip = array[Random.Range(0, 2)];
            }

            Quaternion rotation = mybuilding.transform.gameObject.transform.rotation;
            Color RandomColor = _regions[thisRegion].buildingColors.Colors[Random.Range(0, _regions[thisRegion].buildingColors.Colors.Length)];

            int randomBuilding = Random.Range(0, _regions[thisRegion].SmallBuildings.Length);
            int targetPieces = Random.Range(_regions[thisRegion].SmallBuildings[randomBuilding].minPieces, _regions[thisRegion].SmallBuildings[randomBuilding].maxPieces);
            float heightOffset = 0f;

            //base layer(one)
            heightOffset += SpawnPieceLayer(_regions[thisRegion].SmallBuildings[randomBuilding].baseParts, heightOffset, mybuilding.transform, Building.transform, rotation, RandomColor, randFlip);

            //middle layers(multi)
            for (int i = 2; i < targetPieces; i++)
            {
                if (_regions[thisRegion].SmallBuildings[randomBuilding].middleParts.Length > 0)
                {
                    heightOffset += SpawnPieceLayer(_regions[thisRegion].SmallBuildings[randomBuilding].middleParts, heightOffset, mybuilding.transform, Building.transform, rotation, RandomColor, randFlip);
                }
            }
            //top layer(one)
            if (_regions[thisRegion].SmallBuildings[randomBuilding].topParts.Length > 0)
            {
                SpawnPieceLayer(_regions[thisRegion].SmallBuildings[randomBuilding].topParts, heightOffset, mybuilding.transform, Building.transform, rotation, RandomColor, randFlip);
            }

        }
        #endregion

        #region large_bulidings
        foreach (_buildingLarge mybuilding in listOfLargeBuildings)
        {
            buildingnumber++;

            //each building get its blocks region number
            thisRegion = (int)mybuilding.GetComponentInParent<_block>()._Region;

            GameObject Building = Instantiate(BuildingEmptyGameObject, GeneratedBuildings.transform);
            Building.name = _regions[thisRegion].name + " Large - No:" + buildingnumber;

            int randomBuilding = Random.Range(0, _regions[thisRegion].LargeBuildings.Length);

            //Flip buildings on the x-axis
            int[] array = { -1, 1 };
            int randFlip = array[Random.Range(0, 2)];
            // mybuilding.transform.localScale = new Vector3(array[rand], 1, 1);

            Quaternion rotation = mybuilding.transform.gameObject.transform.rotation;
            Color RandomColor = _regions[thisRegion].buildingColors.Colors[Random.Range(0, _regions[thisRegion].buildingColors.Colors.Length)];

            int targetPieces = Random.Range(_regions[thisRegion].LargeBuildings[randomBuilding].minPieces, _regions[thisRegion].LargeBuildings[randomBuilding].maxPieces);
            float heightOffset = 0f;

            heightOffset += SpawnPieceLayer(_regions[thisRegion].LargeBuildings[randomBuilding].baseParts, heightOffset, mybuilding.transform, Building.transform, rotation, RandomColor, randFlip);
            
            for (int i = 2; i < targetPieces; i++)
            {
                if (_regions[thisRegion].LargeBuildings[randomBuilding].middleParts.Length > 0)
                {
                    //SpawnPieceLayer(_regions[thisRegion].SmallBuildings.midProps, heightOffset, mybuilding.transform, Building.transform, rotation, RandomColor);
                    heightOffset += SpawnPieceLayer(_regions[thisRegion].LargeBuildings[randomBuilding].middleParts, heightOffset, mybuilding.transform, Building.transform, rotation, RandomColor, randFlip);
                }
            }

            if (_regions[thisRegion].LargeBuildings[randomBuilding].topParts.Length > 0)
            {
                SpawnPieceLayer(_regions[thisRegion].LargeBuildings[randomBuilding].topParts, heightOffset, mybuilding.transform, Building.transform, rotation, RandomColor, randFlip);
            }
        }
        #endregion

        #region POI
        foreach (_poi mybuilding in listOfPOIs)
        {
            buildingnumber++;

            //each building get its blocks region number
            thisRegion = (int)mybuilding.GetComponentInParent<_block>()._Region;

            GameObject Building = Instantiate(BuildingEmptyGameObject, GeneratedBuildings.transform);
            Building.name = _regions[thisRegion].name + " POI - No:" + buildingnumber;

            int randomBuilding = Random.Range(0, _regions[thisRegion].PointsOfInterest.Length);

            //Flip buildings on thex-axis
            int[] array = { -1, 1 };
            int randFlip = array[Random.Range(0, 2)];
            // mybuilding.transform.localScale = new Vector3(array[rand], 1, 1);

            Quaternion rotation = mybuilding.transform.gameObject.transform.rotation;
            Color RandomColor = _regions[thisRegion].buildingColors.Colors[Random.Range(0, _regions[thisRegion].buildingColors.Colors.Length)];

            int targetPieces = Random.Range(_regions[thisRegion].PointsOfInterest[randomBuilding].minPieces, _regions[thisRegion].PointsOfInterest[randomBuilding].maxPieces);
            float heightOffset = 0f;


            heightOffset += SpawnPieceLayer(_regions[thisRegion].PointsOfInterest[randomBuilding].baseParts, heightOffset, mybuilding.transform, Building.transform, rotation, RandomColor, randFlip);

            /* for (int i = 2; i < targetPieces; i++)
              {
                  //SpawnPieceLayer(_regions[thisRegion].SmallBuildings.midProps, heightOffset, mybuilding.transform, Building.transform, rotation, RandomColor);
                  heightOffset += SpawnPieceLayer(_regions[thisRegion].PointsOfInterest[randomBuilding].middleParts, heightOffset, mybuilding.transform, Building.transform, rotation, RandomColor, randFlip);
              }*/

            //SpawnPieceLayer(_regions[thisRegion].PointsOfInterest[randomBuilding].topParts, heightOffset, mybuilding.transform, Building.transform, rotation, RandomColor, randFlip);
        }
        #endregion
    }

    float SpawnPieceLayer(GameObject[] pieceArray, float inputHeight, Transform buildingPos, Transform Parent, Quaternion rotation, Color color, int buildingScale)
    {
        Transform randomTransform = pieceArray[Random.Range(0, pieceArray.Length)].transform;
        GameObject clone = Instantiate(randomTransform.gameObject, buildingPos.position + new Vector3(0, inputHeight, 0), rotation) as GameObject;
        Mesh cloneMesh = clone.GetComponentInChildren<MeshFilter>().sharedMesh;
        clone.transform.localScale = new Vector3(buildingScale, 1, 1);

        Bounds bounds = cloneMesh.bounds;
        float heightOffset = bounds.size.y;

        Material tempMaterial = new Material(clone.GetComponentInChildren<MeshRenderer>().sharedMaterial);
        //Can't find "MAD_Color_Main"
        if (tempMaterial.GetColor("MAD_Color_Main") != null)
        {
            tempMaterial.SetColor("MAD_Color_Main", color);
        }
        else
        {

        }
        clone.GetComponentInChildren<MeshRenderer>().sharedMaterial = tempMaterial;

        MeshRenderer[] OtherMesh = clone.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer rend in OtherMesh)
        {
            rend.sharedMaterial = tempMaterial;
        }

        clone.transform.SetParent(Parent);

        return heightOffset;
    }

    /* public void AddPOIs()
     {
         foreach (_poi mybuilding in listOfPOIs)
         {

             GameObject clone = Instantiate(POIs[Random.Range(0, POIs.Length)],mybuilding.transform);
         }
     }*/

    public void GenerateRiver()
    {
        if (AddRiver == false)
        {
            AddRiver = true;
        }
        else if (AddRiver == true)
        {
            AddRiver = false;
        }
    }


    public void ClearPOIs()
    {
        foreach (_poi mybuilding in listOfPOIs)
        {
            while (mybuilding.transform.childCount != 0)
            {
                DestroyImmediate(mybuilding.transform.GetChild(0).gameObject);
            }
        }
    }


    //Spawn Random City Prefabs
    public void ClearBlocks()
    {
        while (GeneratedBlocks.transform.childCount != 0)
        {
            DestroyImmediate(GeneratedBlocks.transform.GetChild(0).gameObject);
        }
    }

    public void ClearPerimeter()
    {
        while (Perimeter.transform.childCount != 0)
        {
            DestroyImmediate(Perimeter.transform.GetChild(0).gameObject);
        }
    }

    public void ClearBuildings()
    {
        while (GeneratedBuildings.transform.childCount != 0)
        {
            DestroyImmediate(GeneratedBuildings.transform.GetChild(0).gameObject);
        }
    }

    public void CleanUp()
    {

    }

}
