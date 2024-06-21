using UnityEngine;

public class DrawShape : MonoBehaviour
{
    public Material material;
    public Vector3 vertex1 = new Vector3(0, 0, 0);
    public Vector3 vertex2 = new Vector3(1, 0, 0);
    public Vector3 vertex3 = new Vector3(0, 1, 0);

    private Mesh mesh;

    void Start()
    {
        // Create the mesh
        mesh = new Mesh();

        // Define the vertices
        Vector3[] vertices = new Vector3[3];
        vertices[0] = vertex1;
        vertices[1] = vertex2;
        vertices[2] = vertex3;

        // Define the triangle
        int[] triangles = new int[3];
        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;

        // Assign to mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        // Optional: Set up UVs and normals if needed
        mesh.RecalculateNormals();
    }

    void OnRenderObject()
    {
        if (material)
        {
            material.SetPass(0);
            Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
        }
    }
}
