using UnityEngine;
using System.Collections.Generic;
using MIConvexHull;
using System.Linq;
using System;


class Triangulation{

    //List<Vector3> localVertices = new();
    List<Vector3> worldVertices;
    public List<Triangle> triangles = new();

    //string modelPath;

    public Triangulation(List<Vector3> worldVertices){
        //this.modelPath = modelPath;
        this.worldVertices = worldVertices;
        Delaunay();
    }

    //TODO find a way to remove vertices outside the model
    public List<Triangle> Delaunay()
    {
        List<Vertex> vertices = worldVertices.Select(v => new Vertex(v.x, v.y, v.z)).ToList();
        HashSet<Triangle> trianglesSet = new();

        // Perform the triangulation
        var delaunayTriangulation = DelaunayTriangulation<Vertex, DefaultTriangulationCell<Vertex>>.Create(vertices, 1e-2);

        foreach (var cell in delaunayTriangulation.Cells)
        {
            // Convert vertices back to Vector3
            Vector3 a = new ((float)cell.Vertices[0].Position[0], (float)cell.Vertices[0].Position[1], (float)cell.Vertices[0].Position[2]);
            Vector3 b = new ((float)cell.Vertices[1].Position[0], (float)cell.Vertices[1].Position[1], (float)cell.Vertices[1].Position[2]);
            Vector3 c = new ((float)cell.Vertices[2].Position[0], (float)cell.Vertices[2].Position[1], (float)cell.Vertices[2].Position[2]);
            Triangle triangle = new(a, b, c);

            // Create a new triangle and add it to the list
            trianglesSet.Add(triangle);
        }
        triangles = trianglesSet.ToList();
        return triangles;
    }
}

public class Vertex : IVertex
{
    public double[] Position { get; set; }

    public Vertex(double x, double y, double z)
    {
        x = Math.Round(x, 2);
        y = Math.Round(y, 2);
        z = Math.Round(z, 2);
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

    public override int GetHashCode()
    {
        // Order-agnostic combination of points' hash codes
        int hash = 17;

        int hashA = A.GetHashCode();
        int hashB = B.GetHashCode();
        int hashC = C.GetHashCode();

        int minHash = Math.Min(hashA, Math.Min(hashB, hashC));
        int maxHash = Math.Max(hashA, Math.Max(hashB, hashC));
        int midHash = hashA ^ hashB ^ hashC ^ minHash ^ maxHash;

        hash = hash * 31 + minHash;
        hash = hash * 31 + midHash;
        hash = hash * 31 + maxHash;

        return hash;
    }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        Triangle other = (Triangle)obj;
        return (A == other.A && B == other.B && C == other.C) ||
               (A == other.A && B == other.C && C == other.B) ||
               (A == other.B && B == other.A && C == other.C) ||
               (A == other.B && B == other.C && C == other.A) ||
               (A == other.C && B == other.A && C == other.B) ||
               (A == other.C && B == other.B && C == other.A);
    }
}