using UnityEngine;

class Particle {
    public Matrix4x4 matrix;
    public Vector3 velocity;

    public Particle(Matrix4x4 matrix, Vector3 velocity){
        this.matrix = matrix;
        this.velocity = velocity;
    }

    public Matrix4x4 Matrix
    {
        get { return matrix; }
        set { matrix = value; }
    }

    public Vector3 Velocity
    {
        get { return velocity; }
        set { velocity = value; }
    }

}