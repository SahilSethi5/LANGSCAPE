using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ProceduralGenerationScript : MonoBehaviour
{
    //VARIABLES
    [Header("Building")]
    [SerializeField] private GameObject middleBuilding;
    [SerializeField] private bool havePivotPointOnCenter = false;

    [Space(20)]

    [Header("Grid")]
    [SerializeField] private Vector2 gridSize;
    [SerializeField] private Vector2 cellSize;
    [SerializeField] private Color gridColor = Color.red;

    [Space(20)]

    [Header("Props")]

    [SerializeField] private List<GeneratableObject> objects = new List<GeneratableObject>();





    //UNITY FUNCTIONS
    private void Start()
    {
        GenerateObjects();
    }

    private void OnDrawGizmosSelected()
    {
        DrawGrid();
    }




    //FUNCTIONS
    private void GenerateObjects()
    {
        //MAIN BUILDING
        GameObject go = Instantiate(middleBuilding, transform);

        if (!havePivotPointOnCenter)
        {
            Vector3 bounds = middleBuilding.GetComponent<MeshRenderer>().bounds.size;
            go.transform.localPosition = new Vector3(bounds.x / 2, 0, bounds.z / 2);
        }



        //WEIGHTED ABUNDANCE
        float totalWeight = 0f;

       
        for (int i = 0; i < objects.Count; i++)
        {
            totalWeight += objects[i].abundance;
        }

        for (int i = 0; i < objects.Count; i++)
        {
            objects[i].weight = objects[i].abundance / totalWeight;
        }


        //PROPS
        //loop through x
        for (int x = 0; x < gridSize.x; x++)
        {
            //loop through y
            for (int y = 0; y < gridSize.y; y++)
            {
                int propIndex = objects.Count - 1;

                float randomNumber = Random.value;
                float weightIndex = 0f;

                

                for (int i = 0; i < objects.Count; i++)
                {
                    if (randomNumber < objects[i].weight + weightIndex)
                    {
                        propIndex = i;
                        break;
                    }
                    else
                        weightIndex += objects[i].weight;
                }


                    GameObject prop = Instantiate(objects[propIndex].obj,transform);


                Debug.Log(randomNumber + prop.name);



                //ROTATION

                if (objects[propIndex].randomRotation)
                    prop.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);



                //SCALE

                //calculate random scale factor
                float scaleF = 0;

                if (objects[propIndex].randomScale)
                    scaleF = Random.Range(objects[propIndex].scale- objects[propIndex].scale * objects[propIndex].randomScaleFactor, objects[propIndex].scale + objects[propIndex].scale * objects[propIndex].randomScaleFactor);
                else
                    scaleF = objects[propIndex].scale;

                prop.transform.localScale = new Vector3(scaleF, scaleF, scaleF);




                //POSITION

                Vector3 pos = Vector3.zero;

                if (objects[propIndex].randomPosition)
                    pos = transform.position + new Vector3(x * cellSize.x + (float)Random.Range(-cellSize.x / (2 / objects[propIndex].randomPositionFactor), cellSize.x / (2 / objects[propIndex].randomPositionFactor)) - (int)gridSize.x / 2 * cellSize.x, 0, y * cellSize.y + (float)Random.Range(-cellSize.y / (2 / objects[propIndex].randomPositionFactor), cellSize.y / (2 / objects[propIndex].randomPositionFactor)) - (int)gridSize.y / 2 * cellSize.y);
                else
                    pos = transform.position + new Vector3(x * cellSize.x - (int)gridSize.x / 2 * cellSize.x, 0, y * cellSize.y - (int)gridSize.y / 2 * cellSize.y);

                prop.transform.position = pos;
            }
        }

    }

    private void DrawGrid()
    {
        Gizmos.color = gridColor;

        //loop through x
        for (int x = 0; x < gridSize.x; x++)
        {
            //loop through y
            for(int y = 0; y < gridSize.y; y++)
            {
                Vector3 pos = transform.position + new Vector3(x * cellSize.x - (int)gridSize.x/2 * cellSize.x, .1f, y * cellSize.y - (int)gridSize.y / 2 * cellSize.y);

                Gizmos.DrawWireCube(pos, new Vector3(cellSize.x, 0, cellSize.y));
            }
        }

    }
}

[System.Serializable]
public class GeneratableObject
{
    public GameObject obj;

    [Space(10)]

    [Range(0,1)] public float abundance = 1f;
    [Range(0,1)] public float scale = 1f;
    [Range(0,1)] public float randomScaleFactor = .4f; //Used only with use random scale, is the percantage of scale
    [Range(0,1)] public float randomPositionFactor = .1f; //Used only with use random position, is the percentage of halfcube

    [Space(10)]

    public bool randomRotation = false;
    public bool randomScale = false;
    public bool randomPosition = false;


    //HIDDEN
    [HideInInspector] public float weight;
}

