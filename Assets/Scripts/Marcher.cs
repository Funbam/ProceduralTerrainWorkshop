using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;


public enum Mode
{
    Standard,
    Plane,
}

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Marcher : MonoBehaviour
{
    [SerializeField] float noisePower;
    [SerializeField] int dimension;
    [SerializeField] bool drawGizmos;
    float[,,] gridNodes;
    [SerializeField] float heightThreshold = 0.05f;
    [SerializeField] Vector3 noiseOffset;
    [SerializeField] Mode mode;
    [SerializeField] bool ThreeD;
    [SerializeField] bool interpolateVertices;
    [SerializeField] bool preventDropoff;

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangleIndices = new List<int>();




    //Theory: Generate a heightmap for each (XZ) pair and mark how close each Y step is to the generated "threshold"
    //This will be used later on to determine the "activated" corners of the marched cube
    //Corners close to the heightmap (ie values to 0) are considered activated


    //YOU WILL BE FILLING OUT THIS FUNCTION
    //YOU WILL BE FILLING OUT THIS FUNCTION
    //YOU WILL BE FILLING OUT THIS FUNCTION
    void SetGrid()
    {
        gridNodes = new float[dimension + 1, dimension + 1, dimension + 1];

        for (int x = 0; x < dimension + 1; x++)
        {
            for (int y = 0; y < dimension + 1; y++)
            {
                for (int z = 0; z < dimension + 1; z++)
                {
                    float generatedValue = 0;
                    //TODO: How shall we determine how to generate the value of any given point?
                    //Hint: you may want to use perlin noise!
                    //generatedValue = ???????


                    float finalDist;

                    switch (mode)
                    {
                        //TODO Implement Standard (assign the value as raw noise)
                        case Mode.Standard:
                            //gridNodes[x, y, z] = ?????????
                            break;
                        //TODO Implement Plane (group up similar values along a rough Y value)
                        //Hint: With 2D noise, every point with a given X and Z always maps to the same noise value
                        //How can we use the Y coordinate to generate a gradient?
                        case Mode.Plane:
                            //gridNodes[x, y, z] = ?????
                            break;
                        //Error
                        default:
                            Debug.LogError("Error, nonexistent mode!");
                            break;
                    }


                }
            }
        }
    }

























































    /*
     * void SetGrid()
    {
        gridNodes = new float[dimension + 1, dimension + 1, dimension + 1];

        for (int x = 0; x < dimension + 1; x++)
        {
            for (int y = 0; y < dimension + 1; y++)
            {
                for (int z = 0; z < dimension + 1; z++)
                {
                    //Each (X,Z) coordinate pair has a set height determinted by the perlin noise
                    float height = Mathf.PerlinNoise(x * noisePower + noiseOffset.x, z * noisePower + noiseOffset.z) * dimension;

                    if (ThreeD)
                    {
                        height = Noise3D(noisePower * x + noiseOffset.x, noisePower * y + noiseOffset.x, noisePower * z + noiseOffset.z) * dimension;
                    }

                    //Set borders to air to prevent cutoff
                    if (preventDropoff && (x == 0 || x == dimension || y == 0 || y == dimension || z == 0 || z == dimension))
                    {
                        gridNodes[x, y, z] = 10;
                        continue;
                    }

                    float finalDist;

                    switch (mode)
                    {
                        //Standard mode that uses pure noise scaled from -1 to 1
                        case Mode.Standard:
                            gridNodes[x, y, z] = height / dimension;
                            break;
                        //Plane mode that assigns in respect to y position to make vertical bands of vaLues
                        case Mode.Plane:
                            finalDist = y - height;
                            gridNodes[x, y, z] = finalDist / dimension;
                            break;
                        //Error
                        default:
                            Debug.LogError("Error, nonexistent mode!");
                            break;
                    }


                }
            }
        }
    }
     */








    //Reads settings from config SO
    void Setup()
    {

        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
    }

    private void Awake()
    {
        //Rebake();
    }

    //A function that rebakes the mesh by reading the config, setting the iso grid, marching the cubes, and setting the mesh
    public void Rebake()
    {
        Setup();

        if(meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
            meshCollider = GetComponent<MeshCollider>();
        }
        SetGrid();
        March();
        SetMesh();
    }

    //A function to help visualize the iso field
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos || !Application.isPlaying || gridNodes == null || gridNodes.Length <= 0)
        {
            return;
        }

        for (int x = 1; x < dimension; x++)
        {
            for (int y = 1; y < dimension; y++)
            {
                for (int z = 1; z < dimension; z++)
                {
                    Gizmos.color = new Color(gridNodes[x, y, z], gridNodes[x, y, z], gridNodes[x, y, z], 1);
                    Gizmos.DrawSphere(new Vector3(x, y, z), 0.2f);
                }
            }
        }
    }

    //Returns the corresponding index into the marching table based on the activated corners
    int GetConfigIndex(float[] cornerValues)
    {
        int index = 0;
        for (int i = 0; i < 8; i++)
        {

            //For each corner, if it is close enough to the surface, activate it
            if (cornerValues[i] > heightThreshold)
            {
                //We represent activation as a binary representation, with the corner being on as a 1 in its corresponding bit
                index |= 1 << i;
            }
        }


        return index;
    }

    void MarchCube(Vector3 position, int index)
    {
        //This case occurs if all corners are off (it is in the air) or on (it is inside the ground and there is nothing to show
        if (index == 0 || index == 255)
        {
            return;
        }

        int edgeIndex = 0;

        //There are at most 5 triangles formed in a config
        for (int tri = 0; tri < 5; tri++)
        {
            //Each triangle consits of 3 edges
            for (int v = 0; v < 3; v++)
            {
                //Get the edge mapping for this specific vertex
                int edgeMapping = MarchingTable.Triangles[index, edgeIndex];

                //A -1 in the table indicates the end of the triangles
                if (edgeMapping == -1)
                {
                    return;
                }

                //Extract the edge from the table and combine relative to corner position
                //Note: this edge is between some pair of corners on the cube
                Vector3 edgeStart = position + MarchingTable.Edges[edgeMapping, 0];
                Vector3 edgeEnd = position + MarchingTable.Edges[edgeMapping, 1];

                Vector3 vertex;

                if (interpolateVertices)
                {
                    //Interpolation based on height values of the corners
                    Vector3Int startCorner = new Vector3Int((int)edgeStart.x, (int)edgeStart.y, (int)edgeStart.z);
                    Vector3Int endCorner = new Vector3Int((int)edgeEnd.x, (int)edgeEnd.y, (int)edgeEnd.z);

                    float valueStart = gridNodes[startCorner.x, startCorner.y, startCorner.z];
                    float valueEnd = gridNodes[endCorner.x, endCorner.y, endCorner.z];

                    float t = (heightThreshold - valueStart) / (valueEnd - valueStart);


                    //Note that each triangle's face points toward either a corner (1 triangle) or in a pair face an edge
                    //This is why the vertex exists somewhere along the edge: points along 3 edges that share a corner form a triangle
                    vertex = edgeStart + t * (edgeEnd - edgeStart);
                    
                }
                else
                {
                    vertex = (edgeStart + edgeEnd) / 2;
                }




                    //Store the vertex and its index
                    vertices.Add(vertex);
                //Note: the mesh reconstructs the triangle by reading 3 indices at a time and finding the corresponding vertexes
                triangleIndices.Add(vertices.Count - 1);

                edgeIndex++;
            }
        }
    }

    //Marches through all the cubes and determines triangles
    void March()
    {
        vertices.Clear();
        triangleIndices.Clear();
        for (int x = 0; x < dimension; x++)
        {
            for (int y = 0; y < dimension; y++)
            {
                for (int z = 0; z < dimension; z++)
                {
                    //For each cube, calculate the number of activated corners
                    float[] cornerValues = new float[8];
                    for (int i = 0; i < 8; i++)
                    {
                        //The marching table gives us a nice and easy way to figure out the corners of our cube
                        //Note that a cube is defined with one corner at the local origin (x,y,z) and the opposite corner at (x+1, y+1, z+1)
                        //We use vector3int to make sure we dont have to cast to int to index
                        Vector3Int corner = new Vector3Int(x, y, z) + MarchingTable.Corners[i];
                        cornerValues[i] = gridNodes[corner.x, corner.y, corner.z];
                    }
                    int config = GetConfigIndex(cornerValues);
                    //Performs the march at this cube
                    MarchCube(new Vector3(x, y, z), config);
                }
            }
        }
    }

    //Creates the mesh based on the vertices and triangles found in the cube marching
    void SetMesh()
    {
        Mesh mesh = new Mesh();

        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangleIndices.ToArray();
        mesh.RecalculateNormals();
#if UNITY_EDITOR
        Unwrapping.GenerateSecondaryUVSet(mesh);
#endif

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    //3D noise by averaging all permutations
    private float Noise3D(float x, float y, float z)
    {
        float xy = Mathf.PerlinNoise(x, y);
        float xz = Mathf.PerlinNoise(x, z);
        float yz = Mathf.PerlinNoise(y, z);

        float yx = Mathf.PerlinNoise(y, x);
        float zx = Mathf.PerlinNoise(z, x);
        float zy = Mathf.PerlinNoise(z, y);

        return (xy + xz + yz + yx + zx + zy) / 6;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Marcher))]
public class MarcherEditor : Editor
{
    //Handles the editor GUI functionality to display the button and call the deck maker function
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        //Spawn the grid
        Marcher marcher = (Marcher)target;

        EditorGUILayout.BeginVertical();

        if (GUILayout.Button("Bake", GUILayout.Width(250)))
        {
            marcher.Rebake();
        }



        EditorGUILayout.EndVertical();
    }
   

}
#endif