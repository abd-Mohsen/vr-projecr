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
        Graphics.DrawMeshInstanced(mesh, 0, material, particles.Select(p => p.Matrix).ToList());
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
            //CheckCollisionWithSphere(particle);
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
        particle.Velocity = Quaternion.Euler(0, 180, 0) * collisionNormal;
        particle.Velocity *= 0.9f;
    }
    
}
