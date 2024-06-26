using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditor;
using System.Globalization;
using System.IO;
using System.Xml.Schema;
using UnityEngine.UI;
using System.Data;



//[BurstCompile]
struct RenderParticlesJob : IJobParallelFor
{
    const int batchSize = 1023;
    [ReadOnly] public NativeArray<Particle> particles;
    public Mesh mesh1;
    public Material material1;

    public void Execute(int index)
    {
        List<Matrix4x4> matrixArray = new();

        for (int i = 0; i < particles.Count(); i++){
            //matrixArray[i] = particles[i].matrix;
            matrixArray.Add(particles[i].matrix);
        }

        for (int i = 0; i < particles.Count(); i += batchSize)
        {
            int count = Math.Min(batchSize, particles.Count() - i);
            Graphics.DrawMeshInstanced(
                mesh1,
                0,
                material1,
                matrixArray.GetRange(i, count)
                //particles.Select(p => p.Matrix).ToList().GetRange(i, count)
                //_args
                //castShadows: UnityEngine.Rendering.ShadowCastingMode.Off
            );
        }
        particles.Dispose();
    }
}


struct UpdateParticlesJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<Particle> particles;
    public Vector3 particleSize1;
    public void Execute(int index)
    {
        for (int i = 0; i < particles.Count(); i++){
            Particle particle = particles[i];
            Vector3 oldPos = particles[i].Matrix.GetPosition();
            particle.Matrix = Matrix4x4.TRS(
                oldPos + 10 * Time.deltaTime * particles[i].Velocity,
                Quaternion.identity,
                particleSize1
            );
            // مشان الجزيئات تضل لازقة بالجسم
            particle.velocity.y = MathF.Max(0.0f, particles[i].velocity.y - 0.2f);
            particle.velocity.z = MathF.Max(0.0f, particles[i].velocity.z - 0.2f);
            particles[i] = particle;
        }
        particles.Dispose();
    }
}

struct UpdateBVHJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<Particle> particles;
    public Vector3 particleSize1;
    public SimManager simManager;
    public Vector3 carCentre1;
    public void Execute(int index)
    {
        simManager.bvh.Clear();
        foreach (Particle particle in particles){
            //if(particle.IsFar(carCentre1)) AddParticleToHash(particle);
        }
        particles.Dispose();
    }
}


