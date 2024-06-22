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

    public bool IsFar(){
        Vector3 CarCentre = new(5,0,0);
        return (matrix.GetPosition() - CarCentre).magnitude > 15;
    }

}