using System;
using UnityEngine;
using System.Collections.Generic;
using MIConvexHull;
using System.Linq;
using System.Globalization;
using System.IO;


class Triangulation : MonoBehaviour{

    List<Vector3> localVertices = new();
    List<Vector3> worldVertices = new();
    public List<Triangle> triangles = new();

    string modelPath;

    public Triangulation(string modelPath){
        this.modelPath = modelPath;
        GetVertices();
        ConvertToWorldCoordinates();
        Delaunay();
    }

    //implement vertex extraction here
    public void GetVertices(){
        //string path = "C:/Users/ABD/Desktop/car.obj";

        foreach (string line in File.ReadLines(modelPath))
        {
            if (line.StartsWith("v "))
            {
                string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 4)
                {
                    float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
                    float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
                    float z = float.Parse(parts[3], CultureInfo.InvariantCulture);

                    localVertices.Add(new Vector3(x, y, z));
                }
            }
        }
    }
    
    public void ConvertToWorldCoordinates()
    {
        foreach (Vector3 vertex in localVertices)
        {
            Vector3 worldVertex = transform.TransformPoint(vertex);
            worldVertices.Add(worldVertex);
        }
    }

    public List<Triangle> Delaunay()
    {
        List<Vertex> vertices = worldVertices.Select(v => new Vertex(v.x, v.y, v.z)).ToList();

        // Perform the triangulation
        var delaunayTriangulation = DelaunayTriangulation<Vertex, DefaultTriangulationCell<Vertex>>.Create(vertices, 1e-9);

        foreach (var cell in delaunayTriangulation.Cells)
        {
            // Convert vertices back to Vector3
            Vector3 a = new Vector3((float)cell.Vertices[0].Position[0], (float)cell.Vertices[0].Position[1], (float)cell.Vertices[0].Position[2]);
            Vector3 b = new Vector3((float)cell.Vertices[1].Position[0], (float)cell.Vertices[1].Position[1], (float)cell.Vertices[1].Position[2]);
            Vector3 c = new Vector3((float)cell.Vertices[2].Position[0], (float)cell.Vertices[2].Position[1], (float)cell.Vertices[2].Position[2]);

            // Create a new triangle and add it to the list
            triangles.Add(new Triangle(a, b, c));
        }

        return triangles;
    }
}

public class Vertex : IVertex
{
    public double[] Position { get; set; }

    public Vertex(double x, double y, double z)
    {
        Position = new double[] { x, y, z };
    }
}

public class Triangle
{
    public Vector3 A { get; }
    public Vector3 B { get; }
    public Vector3 C { get; }

    public Triangle(Vector3 a, Vector3 b, Vector3 c)
    {
        A = a;
        B = b;
        C = c;
    }
}