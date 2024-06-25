using UnityEngine;


//TODO in my oj code, dont update every particle from its script, update from manager
public class ParticleManager : MonoBehaviour
{
    public ComputeShader computeShader;
    public Material particleMaterial;
    public int particleCount = 10000;

    private ComputeBuffer particleBuffer;
    private Particle[] particles;

    struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
    }

    void Start()
    {
        particles = new Particle[particleCount];
        particleBuffer = new ComputeBuffer(particleCount, sizeof(float) * 6);

        for (int i = 0; i < particleCount; i++)
        {
            particles[i].position = Random.insideUnitSphere * 5.0f; // Spread particles out
            particles[i].velocity = Vector3.zero;
        }

        particleBuffer.SetData(particles);
        computeShader.SetBuffer(0, "particles", particleBuffer);
    }

    void Update()
    {
        computeShader.SetFloat("deltaTime", Time.deltaTime);
        computeShader.SetVector("windDirection", Vector3.forward);
        computeShader.SetFloat("windSpeed", 1.0f);
        computeShader.Dispatch(0, particleCount / 256, 1, 1);

        particleMaterial.SetBuffer("particles", particleBuffer);
        particleMaterial.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Points, particleCount);
    }

    void OnDestroy()
    {
        particleBuffer.Release();
    }
}
