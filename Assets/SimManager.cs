using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class SimManager : MonoBehaviour
{
    [SerializeField] Mesh mesh;
    [SerializeField] Material material;
    [SerializeField] GameObject body;
    [SerializeField] int limit;
    [SerializeField] Vector3 particleSize;
    [SerializeField] float voxelSize = 1f;
    [SerializeField] Vector3 sphereCenter; 
    [SerializeField] float sphereRadius = 1;
    const int batchSize = 1023;

    List<Particle> particles = new(); // TODO: make it 2d to deal with more batches

    Dictionary<Vector3Int, List<Particle>> bvh = new();
    

    void Start()
    {
        // Vector3 pos = new(0,0,0);
        // Matrix4x4 matrix = Matrix4x4.TRS(pos:pos, Quaternion.Euler(0,0,0), particleSize);
        // particles.Add(new(matrix, new(1,0,0)));
        InitializeBVH();
        body.transform.position = sphereCenter;
        body.transform.localScale = new(2*sphereRadius, 2*sphereRadius, 2*sphereRadius);
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
            particles[i].velocity.y = Math.Max(0f, particles[i].Velocity.y - 1.0f);
            particles[i].velocity.z = Math.Max(0f, particles[i].Velocity.z - 1.0f);
        }
    }

    void SpawnNewParticle(){
        Vector3 pos = new(0,0,0);
        Matrix4x4 matrix = Matrix4x4.TRS(pos:pos, Quaternion.Euler(0,0,0), particleSize);
        particles.Add(new(matrix, new(1,0,0)));
    }

    public void InitializeBVH(){
        foreach (Particle particle in particles){
            AddParticleToHash(particle);
        }
    }

    public void UpdateBVH(){
        bvh.Clear();
        foreach (Particle particle in particles){
            AddParticleToHash(particle);
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
            List<Particle> inVoxel = bvh[GetVoxelCoordinate(particle.Matrix.GetPosition())]; // in the same voxel
            List<Particle> nearby = GetNearbyParticles(GetVoxelCoordinate(particle.Matrix.GetPosition()));
            
            foreach (Particle other in inVoxel.Union(nearby)){
                if (other != particle && IsParticlesColliding(particle, other)){
                    Debug.Log($"particle is colliding with other particle");
                }
            }
            CheckCollisionWithSphere(particle);
        }
    }

    //TODO optimize later by only checking particles in voxels that include the sphere
    void CheckCollisionWithSphere(Particle particle){
        Vector3 toParticle = particle.Matrix.GetPosition() - sphereCenter;
        float distance = toParticle.magnitude;
        float particleRadius = particleSize.x / 2;

        if (distance <= sphereRadius + particleRadius)
        {
            Vector3 collisionNormal = toParticle.normalized;
            // move particle outside sphere
            particle.Matrix = Matrix4x4.TRS(
                sphereCenter + collisionNormal * (sphereRadius + particleRadius),
                Quaternion.identity,
                particleSize
            );
            CollideWithBody(collisionNormal, particle);
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
        if(collisionNormal.y == 0) collisionNormal.y += 0.02f;
        particle.Velocity = (collisionNormal.y < 0 ? Quaternion.Euler(0, 0, 90) : Quaternion.Euler(0, 0, -90)) * collisionNormal;

        //particle.Velocity = Quaternion.Euler(0, 0, 90) * collisionNormal;
        //particle.Velocity = collisionNormal; 
        
        // Vector3 localY = collisionNormal; // The normal of the plane is the local Y-axis
        
        // // Define a local X-axis (perpendicular to the normal)
        // Vector3 arbitrary = Vector3.up; // Any arbitrary vector not parallel to the normal
        // Vector3 localX = Vector3.Cross(localY, arbitrary).normalized;
        
        // // Define a local Z-axis (perpendicular to both localY and localX)
        // Vector3 localZ = Vector3.Cross(localX, localY).normalized;

        // // Create a rotation matrix for 90 degrees around the local Y-axis
        // Quaternion rotation = Quaternion.Euler(0, -90, 0);

        // // Rotate the normal vector around the local Y-axis
        // Vector3 rotatedNormal = rotation * localZ;

        // // Set the particle velocity to the rotated normal
        // particle.Velocity = rotatedNormal;
    }
    
}
