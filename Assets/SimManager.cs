using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEditor;
using Unity.VisualScripting;

public class SimManager : MonoBehaviour
{
    [SerializeField] Mesh mesh;
    [SerializeField] Material material;
    //[SerializeField] GameObject body;
    [SerializeField] int limit;
    [SerializeField] Vector3 particleSize;
    [SerializeField] float voxelSize = 1f;
    // [SerializeField] Vector3 sphereCenter; 
    // [SerializeField] float sphereRadius = 1;
    [SerializeField] Vector3 v1;
    [SerializeField] Vector3 v2;
    [SerializeField] Vector3 v3;
    const int batchSize = 1023;

    List<Particle> particles = new(); // TODO: make it 2d to deal with more batches
    List<List<Vector3>> triangles = new(); // TODO

    Dictionary<Vector3Int, List<Particle>> bvh = new();
    

    void Start()
    {
        // Vector3 pos = new(0,0,0);
        // Matrix4x4 matrix = Matrix4x4.TRS(pos:pos, Quaternion.Euler(0,0,0), particleSize);
        // particles.Add(new(matrix, new(1,0,0)));
        triangles.Add(new List<Vector3>{v1,v2,v3});
        InitializeBVH();
        // body.transform.position = sphereCenter;
        // body.transform.localScale = new(2*sphereRadius, 2*sphereRadius, 2*sphereRadius);
    }

    void Update()
    {
        if(particles.Count < limit) SpawnNewParticle();
        // TODO: test if this below is working
        for (int i = 0; i < particles.Count; i += batchSize)
        {
            int count = Math.Min(batchSize, particles.Count - i);
            Graphics.DrawMeshInstanced(mesh, 0, material, particles.Select(p => p.Matrix).ToList().GetRange(i, count));
        }
        // TODO: mapping particle to its matrix might be cpu intensive 
        //Graphics.DrawMeshInstanced(mesh, 0, material, particles.Select(p => p.Matrix).ToList());
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
            // particles[i].velocity.y = Math.Max(0f, particles[i].Velocity.y - 1.0f);
            // particles[i].velocity.z = Math.Max(0f, particles[i].Velocity.z - 1.0f);
        }
    }

    void SpawnNewParticle(){
        Vector3 pos = new(0,0,0);
        Matrix4x4 matrix = Matrix4x4.TRS(pos:pos, Quaternion.Euler(0,0,0), particleSize);
        particles.Add(new(matrix, new(0.3f,0,0f)));
    }

    public void InitializeBVH(){
        foreach (Particle particle in particles){
            AddParticleToHash(particle);
        }
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
            
            foreach (Particle other in inVoxel.Union(nearby)){
                if (other != particle && IsParticlesColliding(particle, other)){
                    //Debug.Log($"particle is colliding with other particle");
                }
            }
            CheckCollisionWithTriangle(particle, triangles[0]);
        }
    }

    //TODO optimize later by only checking particles in voxels that include the sphere
    void CheckCollisionWithSphere(Particle particle){
        // Vector3 toParticle = particle.Matrix.GetPosition() - sphereCenter;
        // float distance = toParticle.magnitude;
        // float particleRadius = particleSize.x / 2;

        // if (distance <= sphereRadius + particleRadius)
        // {
        //     Vector3 collisionNormal = toParticle.normalized;
        //     // move particle outside sphere
        //     particle.Matrix = Matrix4x4.TRS(
        //         sphereCenter + collisionNormal * (sphereRadius + particleRadius),
        //         Quaternion.identity,
        //         particleSize
        //     );
        //     CollideWithBody(collisionNormal, particle);
        // }
    }

void CheckCollisionWithTriangle(Particle particle, List<Vector3> triangle)
{
    Vector3 a = triangle[0], b = triangle[1], c = triangle[2];
    Vector3 p = particle.matrix.GetPosition();

    Vector3 pa = a - p;
    Vector3 pb = b - p;
    Vector3 pc = c - p;

    Vector3 ca = a - c;
    Vector3 cb = b - c;

    Vector3 n1 = Vector3.Cross(pa, pb).normalized;
    Vector3 n2 = Vector3.Cross(pc, pa).normalized;
    Vector3 n3 = Vector3.Cross(pb, pc).normalized;

    
    Vector3 normal = Vector3.Cross(ca, cb).normalized;

    Debug.Log($"n1:{n1}");
    Debug.Log($"n2:{n2}");
    Debug.Log($"n3:{n3}");

    bool isCollided = Vector3.Dot(n3,n2) >= 1 && Vector3.Dot(n3,n1) >= 1;
    //AreClose(n1,n2,n3,0.1f)
    if (isCollided)
    {
        // Handle collision with the triangle
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
        // TODO there is multiple cases, rotate normal according to its position (find a way to rotate on the normal's y axis)
        // TODO make collision with a plane instead of a sphere
        // TODO dont process all particles, just those near the car
        // TODO if we want the particles to slide alongside a hollow car body, decrement y,z until it hit a triangle, do that while the x is less than car's end
        // if(collisionNormal.y == 0) collisionNormal.y += 0.02f;
        // particle.Velocity = (collisionNormal.y < 0 ? Quaternion.Euler(0, 0, 90) : Quaternion.Euler(0, 0, -90)) * collisionNormal;
        //if(collisionNormal.x < 0) collisionNormal.x = Math.Abs(collisionNormal.x);
        //Debug.Log($"{collisionNormal}");
        //collisionNormal = -collisionNormal.normalized;
        if(collisionNormal.y == 0) collisionNormal.y += 0.02f;
        //particle.Velocity = (collisionNormal.y < 0 ? Quaternion.Euler(0, 0, 90) : Quaternion.Euler(0, 0, -90)) * collisionNormal;
        particle.Velocity = new(0,0,1);

        /* TODO
        why stuck when going upwards (its from gpu,)
        normal is wrong
        not touching the triangle(small gap)
        test another triangle coordinates
        */

    }

    bool AreClose(Vector3 vec1, Vector3 vec2, Vector3 vec3, float threshold)
    {
        // Compare x, y, z components of each vector with the threshold
        bool xClose = Mathf.Abs(vec1.x - vec2.x) < threshold && Mathf.Abs(vec1.x - vec3.x) < threshold && Mathf.Abs(vec2.x - vec3.x) < threshold;
        bool yClose = Mathf.Abs(vec1.y - vec2.y) < threshold && Mathf.Abs(vec1.y - vec3.y) < threshold && Mathf.Abs(vec2.y - vec3.y) < threshold;
        bool zClose = Mathf.Abs(vec1.z - vec2.z) < threshold && Mathf.Abs(vec1.z - vec3.z) < threshold && Mathf.Abs(vec2.z - vec3.z) < threshold;

        // Return true if all components are close within the threshold
        return xClose && yClose && zClose;
    }
    
}

