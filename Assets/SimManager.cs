using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEditor;
using System.Globalization;
using System.IO;

public class SimManager : MonoBehaviour
{
    [SerializeField] Mesh mesh;
    [SerializeField] Material material;
    //[SerializeField] GameObject body;
    [SerializeField] int limit;
    [SerializeField] Vector3 particleSize;
    [SerializeField] float voxelSize = 1f;
    [SerializeField] Vector3 v1;
    [SerializeField] Vector3 v2;
    [SerializeField] Vector3 v3;
    const int batchSize = 1023;
    float particleRadius;

    List<Particle> particles = new();
    List<Triangle> triangles = new(); // TODO

    Dictionary<Vector3Int, List<Particle>> bvh = new();

    [SerializeField] string modelPath;
    
    void Awake(){
        List<Vector3> localVertices = GetVertices(modelPath);
        List<Vector3> wordVerticies = ConvertToWorldCoordinates(localVertices);
        Triangulation triangulation = new(wordVerticies);
        triangles = triangulation.Delaunay();
    }

    public List<Vector3> ConvertToWorldCoordinates(List<Vector3> localVertices)
    {
        List<Vector3> worldVertices = new();
        foreach (Vector3 vertex in localVertices)
        {
            Vector3 worldVertex = transform.TransformPoint(vertex);
            worldVertices.Add(worldVertex);
        }
        return worldVertices;
    }

    void Start()
    {
        triangles.Add(new Triangle(v1,v2,v3));
        particleRadius = particleSize.x/2;
        //TODO create a second bvh for triangles and populate it here
        //InitializeBVH();
    }

    void Update()
    {
        if(particles.Count < limit) SpawnNewParticle();
        // TODO: test if this below is working
        for (int i = 0; i < particles.Count; i += batchSize)
        {
            int count = Math.Min(batchSize, particles.Count - i);
            Graphics.DrawMeshInstanced(
                mesh,
                0,
                material,
                particles.Select(p => p.Matrix).ToList().GetRange(i, count)
                //castShadows: UnityEngine.Rendering.ShadowCastingMode.Off
                );
        }
        // TODO: mapping particle to its matrix might be cpu intensive (for loop might be faster)
        UpdateParticlesPosition();
        UpdateBVH();
        CheckCollisions();
        //if(particles.Count > 1000) particles.RemoveAt(0);
    }

    void UpdateParticlesPosition(){
        //TODO edit velocity when neccessary (eg: y and z)
        for (int i = 0; i < particles.Count; i++){
            Vector3 oldPos = particles[i].Matrix.GetPosition();
            particles[i].Matrix = Matrix4x4.TRS(
                oldPos + 10 * Time.deltaTime * particles[i].Velocity,
                Quaternion.identity,
                particleSize
            );
            //TODO: why set matrix is not working
            // particles[i].Matrix.SetTRS(oldPos + 10 * Time.deltaTime * particles[i].Velocity,
            //     Quaternion.identity,
            //     particleSize);
            // particles[i].velocity.y = Math.Max(0f, particles[i].Velocity.y - 1.0f);
            // particles[i].velocity.z = Math.Max(0f, particles[i].Velocity.z - 1.0f);
        }
    }

    void SpawnNewParticle(){
        Vector3 pos = new(0,0,0);
        Matrix4x4 matrix = Matrix4x4.TRS(pos:pos, Quaternion.Euler(0,0,0), particleSize);
        particles.Add(new(matrix, new(0.5f,0f,0f)));
    }

    public void UpdateBVH(){
        bvh.Clear();
        foreach (Particle particle in particles){
            if(!particle.IsFar()) AddParticleToHash(particle);
        }
    }

    void AddParticleToHash(Particle particle){
        Vector3Int voxel = GetVoxelCoordinate(particle.Matrix.GetPosition());
        if (!bvh.ContainsKey(voxel)){
            bvh[voxel] = new List<Particle>();
        }
        bvh[voxel].Add(particle);
    }

    Vector3Int GetVoxelCoordinate(Vector3 position){
        int x = Mathf.FloorToInt(position.x / voxelSize);
        int y = Mathf.FloorToInt(position.y / voxelSize);
        int z = Mathf.FloorToInt(position.z / voxelSize);
        return new Vector3Int(x, y, z);
    }

    public void CheckCollisions(){
        foreach (Particle particle in particles){
            if(particle.IsFar()) continue;
            List<Particle> inVoxel = bvh[GetVoxelCoordinate(particle.Matrix.GetPosition())]; // in the same voxel
            List<Particle> nearby = GetNearbyParticles(GetVoxelCoordinate(particle.Matrix.GetPosition()));
            List<Particle> newPositions = new();
            foreach (Particle other in inVoxel.Union(nearby)){
                if (other != particle && IsParticlesColliding(particle, other)){
                    (Particle, Particle) collided = CollideParticles(particle, other);
                    newPositions.Add(collided.Item1);
                    newPositions.Add(collided.Item2);
                }
            }
            for(int i=0; i<newPositions.Count; i++){
                AddParticleToHash(newPositions[i]);
            }
            // then apply them here, to not change bvh amid
            CheckCollisionWithTriangle(particle, triangles[0]);
        }
    }

    //TODO optimize later by only checking particles in voxels that include the triangle, and multithread
    void CheckCollisionWithTriangle(Particle particle, Triangle triangle)
    {
        Vector3 a = triangle.A, b = triangle.B, c = triangle.C;
        Vector3 p = particle.matrix.GetPosition();

        Vector3 pa = a - p;
        Vector3 pb = b - p;
        Vector3 pc = c - p;

        Vector3 ca = a - c;
        Vector3 cb = b - c;

        Vector3 n1 = Vector3.Cross(pb, pa).normalized;
        Vector3 n2 = Vector3.Cross(pa, pc).normalized;
        Vector3 n3 = Vector3.Cross(pc, pb).normalized;

        
        Vector3 normal = Vector3.Cross(cb, ca).normalized;

        // Debug.Log($"n1:{n1}");
        // Debug.Log($"n2:{n2}");
        Debug.Log($"n3:{n3}"); // TODO removing this causes errors

        //bool isCollided = Vector3.Dot(n3,n2) >= 1 && Vector3.Dot(n3,n1) >= 1;
        bool isCollided = AreClose(n1,n2,n3,0.2f);
        if (isCollided)
        {
            CollideWithBody(normal, particle);
        }
    }

    List<Particle> GetNearbyParticles(Vector3Int voxel){
        List<Particle> nearbyParticles = new();

        for (int x = -1; x <= 1; x++){
            for (int y = -1; y <= 1; y++){
                for (int z = -1; z <= 1; z++){
                    Vector3Int neighborVoxel = voxel + new Vector3Int(x, y, z);
                    if (bvh.ContainsKey(neighborVoxel)){
                        nearbyParticles.AddRange(bvh[neighborVoxel]); //try to add only neighbor particles
                    }
                }
            }
        }

        return nearbyParticles;
    }

    bool IsParticlesColliding(Particle p1, Particle p2){
        float distance = Vector3.Distance(p1.Matrix.GetPosition(), p2.Matrix.GetPosition());
        float collisionDistance = 0.05f;
        return distance < collisionDistance;
    }

    void CollideWithBody(Vector3 collisionNormal, Particle particle){
        // TODO dont process all particles, just those near the car
        // TODO if we want the particles to slide alongside a hollow car body, decrement y,z until it hit a triangle, do that while the x is less than car's end

        Vector3 pushBack = particle.velocity.normalized * (particleRadius + 0.1f);
        Vector3 newPosition = particle.matrix.GetPosition() - pushBack;
        particle.Matrix = Matrix4x4.TRS(
                newPosition,
                Quaternion.identity,
                particleSize
        );
        if(collisionNormal.y == 0) collisionNormal.y += 0.02f;
        particle.Velocity = (collisionNormal.y < 0 ? Quaternion.Euler(0, 0, 90) : Quaternion.Euler(0, 0, -90)) * collisionNormal;
        //particle.Velocity = collisionNormal;

        /*
        why stuck when going upwards (its from gpu,)
        */

    }
    
    (Particle, Particle) CollideParticles(Particle p1, Particle p2)
    {
        Vector3 position1 = p1.Matrix.GetPosition();
        Vector3 position2 = p2.Matrix.GetPosition();
        Vector3 distanceVector = position1 - position2;
        
        float radiusSum = particleRadius*2;
       
        float overlap = radiusSum - distanceVector.magnitude;
        
        // Normalize the distance vector to get the direction of separation
        Vector3 direction = distanceVector.normalized;
        
        // Push each particle away by half the overlap
        p1.Matrix = Matrix4x4.TRS(
            position1 + (overlap / 2 * direction),
            Quaternion.identity,
            particleSize
        );

        p2.Matrix = Matrix4x4.TRS(
            position1 - (overlap / 2 * direction),
            Quaternion.identity,
            particleSize
        );
        p1.velocity = 0.9f * p1.velocity;
        p1.velocity = 0.9f * p2.velocity;
        return (p1,p2);
        //try to store transitions and then loopover
    }

    bool AreClose(Vector3 vec1, Vector3 vec2, Vector3 vec3, float threshold)
    {
        //TODO check if i can replace this with smth more efficient
        // Compare x, y, z components of each vector with the threshold
        bool xClose = Mathf.Abs(vec1.x - vec2.x) < threshold && Mathf.Abs(vec1.x - vec3.x) < threshold && Mathf.Abs(vec2.x - vec3.x) < threshold;
        bool yClose = Mathf.Abs(vec1.y - vec2.y) < threshold && Mathf.Abs(vec1.y - vec3.y) < threshold && Mathf.Abs(vec2.y - vec3.y) < threshold;
        bool zClose = Mathf.Abs(vec1.z - vec2.z) < threshold && Mathf.Abs(vec1.z - vec3.z) < threshold && Mathf.Abs(vec2.z - vec3.z) < threshold;

        // Return true if all components are close within the threshold
        return xClose && yClose && zClose;
    }

    
     public List<Vector3> GetVertices(string modelPath){
        //string path = "C:/Users/ABD/Desktop/car.obj";
        List<Vector3> localVertices = new();
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
        return localVertices;
    }
    

}

