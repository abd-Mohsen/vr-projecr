using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEditor;
using System.Globalization;
using System.IO;
using Unity.Jobs;
using System.Xml.Schema;
using UnityEngine.UI;
using System.Data;

public class SimManager : MonoBehaviour
{
    [SerializeField] Mesh mesh;
    [SerializeField] Material material;
    [SerializeField] int limit;
    [SerializeField] float voxelSize = 1f;
    [SerializeField] GameObject [] carBody;
    public GameObject[] game;
    [SerializeField] float particleRadius;
    [SerializeField] string modelPath;
    

    const int batchSize = 1023;
    Vector3 particleSize;
    int spawnedParticles = 0;



    List<Particle> particles = new();
    List<Triangle> triangles = new();
    Dictionary<Vector3Int, List<Particle>> bvh = new();
    Dictionary<Vector3Int, List<Triangle>> bvh2 = new();
    List<Vector3> worldVertices = new();
    
    
    public int xx=0;
    public bool startrunning=false;
   

    // private readonly uint[] _args = { 0, 0, 0, 0, 0 };
    // private ComputeBuffer _argsBuffer;
    
   //the next and showgameobject is to change model when the buttom is pressed
    public void nextt(){
        if(xx<carBody.Length){
            xx++;
        }
        if (xx==carBody.Length){
            xx=0;
        }
    }
    public void showgameobject(){
        if(xx==0){
              game[6].SetActive(false);
            game[0].SetActive(true);
           
        }
        if(xx != 0){
 game[xx].SetActive(true);
            game[xx-1].SetActive(false);
            

        }
       
           
       
       
    }

public void starttunnigg(){
    startrunning=true;
}

//change particleradius from slider
public void nextradius(){
    particleRadius+=0.1f;
}


     // يتم استدعاء قبل البدء بالمحاكاة
    void Awake(){
        if(startrunning==true){
        GetVerticesFromMesh2(); // تخزين النقاط
        Debug.Log($"extracted points {worldVertices.Count}");
        Triangulation triangulation = new(worldVertices); // تثليث
        triangles = triangulation.Delaunay();// تثليث
        Debug.Log($"finished triangulation {triangles.Count}");
        InitBVH2(); // تخزين المثلثات في هاش
        Debug.Log($"initialized bvh2 {bvh2.Count}");
        //TODO get min x,y,z and max x,y,z of the body from the world vertices
        //Application.targetFrameRate = 60;
    }}

    void Start()
    {

       if(startrunning==true){
      
        particleSize = new(2*particleRadius, 2*particleRadius, 2*particleRadius);
        particleRadius = particleSize.x/2;
        // _argsBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        // UpdateBuffers();
    }}

    void Update()
    {
        if(startrunning==true){
            Awake();
            Start();
            startrunning=false;
            
        }
       
        if(particles.Count < limit) {
            if(spawnedParticles % 50 == 0){
                SpawnNewParticle(new(-20f, 4.5f, 1.5f)); 
                //SpawnNewParticle(new(-20f, 4.5f, -2.0f)); 
            } 
            spawnedParticles++;
        }
        UpdateParticlesPosition();
        UpdateBVH();
        CheckCollisions();
        
        RenderParticles();
        //VisualizeVertices("w");
    }
    
    // عرض الجزيئات باستخدام  gpu instancing
    public void RenderParticles(){
         // TODO: test if this below is working and optimised
        //Matrix4x4[] matrixArray = new Matrix4x4[particles.Count]; 
        // TODO: mapping particle to its matrix might be cpu intensive (for loop might be faster)
        List<Matrix4x4> matrixArray = new();

        for (int i = 0; i < particles.Count; i++){
            //matrixArray[i] = particles[i].matrix;
            matrixArray.Add(particles[i].matrix);
        }

        for (int i = 0; i < particles.Count; i += batchSize)
        {
            int count = Math.Min(batchSize, particles.Count - i);
            Graphics.DrawMeshInstanced(
                mesh,
                0,
                material,
                //matrixArray[i..count]
                matrixArray.GetRange(i, count)
                //particles.Select(p => p.Matrix).ToList().GetRange(i, count)
                //_args
                //castShadows: UnityEngine.Rendering.ShadowCastingMode.Off
            );
        }
    }

    // تخزين المثلثات في هاش
    public void InitBVH2(){
        foreach(Triangle triangle in triangles){
            AddParticleToHash2(triangle, triangle.A);
            AddParticleToHash2(triangle, triangle.B);
            AddParticleToHash2(triangle, triangle.C);
        }
    }

    // عدم معالجة الجزيئات البعيدة عن الجسم
    bool IsFar(Particle p){
        Vector3 CarCentre = carBody[xx].transform.position;
        return MathF.Abs((p.matrix.GetPosition() - CarCentre).magnitude) > 20;
    }

    void AddParticleToHash2(Triangle triangle, Vector3 point){
        Vector3Int voxel = GetVoxelCoordinate(point);
        if (!bvh2.ContainsKey(voxel)){
            bvh2[voxel] = new List<Triangle>();
        }
        bvh2[voxel].Add(triangle);
    }

    public List<Vector3> ConvertToWorldCoordinates(List<Vector3> localVertices)
    {
        List<Vector3> worldVertices = new();
        foreach (Vector3 vertex in localVertices)
        {
            Vector3 worldVertex = carBody[xx].transform.TransformPoint(vertex);
            worldVertices.Add(worldVertex);
        }
        return worldVertices;
    }

    // private void UpdateBuffers()
    // {
    //     // Positions
    //     _positionBuffer1?.Release();
    //     _positionBuffer2?.Release();
    //     _positionBuffer1 = new ComputeBuffer(_count, 16);
    //     _positionBuffer2 = new ComputeBuffer(_count, 16);

    //     var positions1 = new Vector4[_count];
    //     var positions2 = new Vector4[_count];

    //     // Grouping cubes into a bunch of spheres
    //     var offset = Vector3.zero;
    //     var batchIndex = 0;
    //     var batch = 0;
    //     for (var i = 0; i < _count; i++)
    //     {
    //         var dir = Random.insideUnitSphere.normalized;
    //         positions1[i] = dir * Random.Range(10, 15) + offset;
    //         positions2[i] = dir * Random.Range(30, 50) + offset;

    //         positions1[i].w = Random.Range(-3f, 3f);
    //         positions2[i].w = batch;

    //         if (batchIndex++ == 250000)
    //         {
    //             batchIndex = 0;
    //             batch++;
    //             offset += new Vector3(90, 0, 0);
    //         }
    //     }

    //     _positionBuffer1.SetData(positions1);
    //     _positionBuffer2.SetData(positions2);
    //     _instanceMaterial.SetBuffer("position_buffer_1", _positionBuffer1);
    //     _instanceMaterial.SetBuffer("position_buffer_2", _positionBuffer2);
    //     _instanceMaterial.SetColorArray("color_buffer", SceneTools.Instance.ColorArray);

    //     // Verts
    //     _args[0] = _instanceMesh.GetIndexCount(0);
    //     _args[1] = (uint)_count;
    //     _args[2] = _instanceMesh.GetIndexStart(0);
    //     _args[3] = _instanceMesh.GetBaseVertex(0);

    //     _argsBuffer.SetData(_args);
    // }

    // تحريك الجزيئات
    void UpdateParticlesPosition(){
        //TODO edit velocity when neccessary (eg: y and z)
        for (int i = 0; i < particles.Count; i++){
            Vector3 oldPos = particles[i].Matrix.GetPosition();
            particles[i].Matrix = Matrix4x4.TRS(
                oldPos + 10 * Time.deltaTime * particles[i].Velocity,
                Quaternion.identity,
                particleSize
            );
            // مشان الجزيئات تضل لازقة بالجسم
            particles[i].velocity.y = MathF.Max(0.0f, particles[i].velocity.y - 0.2f);
            particles[i].velocity.z = MathF.Max(0.0f, particles[i].velocity.z - 0.2f);
        }
    }

    // ضخ جزيئة جديدة
    void SpawnNewParticle(Vector3 spawnPos){
        Matrix4x4 matrix = Matrix4x4.TRS(pos:spawnPos, Quaternion.Euler(0,0,0), particleSize);
        particles.Add(new(matrix, new(0.2f,-0.5f,0f)));
    }

    void VisualizeVertices(string type){
        particles.Clear();
        if(type == "world"){
            foreach(Vector3 vertex in worldVertices){
                Matrix4x4 matrix = Matrix4x4.TRS(pos:vertex, Quaternion.Euler(0,0,0), particleSize);
                particles.Add(new(matrix, new(0f,0f,0f)));
            }
        }
        else{
            for(int i=0; i<triangles.Count; i+=1){
                Vector3 a = triangles[i].A;
                Vector3 b = triangles[i].B;
                Vector3 c = triangles[i].C;
                // Vector3 b = worldVertices[i];
                // Vector3 c = worldVertices[i];
                // Debug.Log(vertex);
                Matrix4x4 matrix = Matrix4x4.TRS(pos:a, Quaternion.Euler(0,0,0), particleSize);
                particles.Add(new(matrix, new(0.5f,0f,0f)));
                Matrix4x4 matrix1 = Matrix4x4.TRS(pos:b, Quaternion.Euler(0,0,0), particleSize);
                particles.Add(new(matrix1, new(0.5f,0f,0f)));
                Matrix4x4 matrix2 = Matrix4x4.TRS(pos:c, Quaternion.Euler(0,0,0), particleSize);
                particles.Add(new(matrix2, new(0.5f,0f,0f)));
            }
        }
    }

    // تحديث الهاش 
    public void UpdateBVH(){
        bvh.Clear();
        foreach (Particle particle in particles){
            if(!IsFar(particle)) AddParticleToHash(particle);
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

    // كلشي متعلق الصدم
    public void CheckCollisions(){
        foreach (Particle particle in particles){
            if(IsFar(particle)) continue; // اذا الجزيئة بعيدة لا تكمل
            Vector3 particlePos = particle.Matrix.GetPosition();

            List<Particle> inVoxel = bvh[GetVoxelCoordinate(particlePos)]; // in the same voxel
            List<Particle> nearby = GetNearbyParticles(GetVoxelCoordinate(particlePos));// في المكعبات المحيطة
            List<Particle> newPositions = new(); // لتخزين مواقع الجزيئات الجديدة بعد الصدم
            foreach (Particle other in inVoxel.Union(nearby)){
                if (other != particle && IsParticlesColliding(particle, other)){
                    // اضافة الاحداثيات الجديدة للنقاط
                    (Particle, Particle) collided = CollideParticles(particle, other);
                    newPositions.Add(collided.Item1);
                    newPositions.Add(collided.Item2);
                }
            }
            for(int i=0; i<newPositions.Count; i++){
                AddParticleToHash(newPositions[i]);
            }
            // كشف الصم مع المثلثات التي في نفس المكعب
            if(!bvh2.ContainsKey(GetVoxelCoordinate(particlePos))) continue;
            foreach(Triangle triangle in bvh2[GetVoxelCoordinate(particlePos)]){
                CheckCollisionWithTriangle(particle, triangle);
            }
        }
    }

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
        bool isCollided = AreClose(n1,n2,n3,0.3f);
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
        float collisionDistance = 0.0f;
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
        collisionNormal = 0.2f * collisionNormal;
        //if(collisionNormal.y == 0) collisionNormal.y += 0.02f;
        particle.Velocity = (collisionNormal.y < 0 ? Quaternion.Euler(0, 0, 90) : Quaternion.Euler(0, 0, -90)) * collisionNormal;
        //particle.Velocity = new(0,1,0);
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
        particles.Remove(p1);
        particles.Remove(p2);
        
        // Push each particle away by half the overlap
        p1.Matrix = Matrix4x4.TRS(
            position1 + (2*overlap / 2 * direction),
            Quaternion.identity,
            particleSize
        );

        p2.Matrix = Matrix4x4.TRS(
            position1 - (2*overlap / 2 * direction),
            Quaternion.identity,
            particleSize
        );
        // p1.velocity = 0.9f * p1.velocity;
        // p1.velocity = 0.9f * p2.velocity;
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

    
    // public void GetVerticesFromMesh1(string modelPath){
    //     List<Vector3> localVertices = new();
    //     foreach (string line in File.ReadLines(modelPath))
    //     {
    //         if (line.StartsWith("v "))
    //         {
    //             string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    //             if (parts.Length == 4)
    //             {
    //                 float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
    //                 float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
    //                 float z = float.Parse(parts[3], CultureInfo.InvariantCulture);

    //                 localVertices.Add(new Vector3(x, y, z));
    //             }
    //         }
    //     }
    //     worldVertices = ConvertToWorldCoordinates(localVertices);
    //     worldVertices = localVertices;
    // }

    private void GetVerticesFromMesh2()
    {
        //List<Vector3> localVertices = new();
        MeshFilter[] meshFilters = carBody[xx].GetComponentsInChildren<MeshFilter>();
        //MeshFilter ogFilter = carBody.GetComponent<MeshFilter>;
        List<Vector3> res = new();
            foreach (MeshFilter meshFilter in meshFilters)
            {
                if (meshFilter != null)
                {
                    Mesh mesh = meshFilter.sharedMesh;
                    Transform meshTransform = meshFilter.transform;

                    // Get local vertices from the mesh
                    Vector3[] localVertices = mesh.vertices;

                    // Convert each local vertex to world coordinates
                    foreach (Vector3 localVertex in localVertices)
                    {
                        Vector3 worldVertex = meshTransform.TransformPoint(localVertex);
                        worldVertices.Add(worldVertex);
                    }
                }
            }
        Debug.Log($"{res.Count} vertex total (including submeshes)");
    }
    

}

