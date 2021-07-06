using UnityEngine;

public class HandTransform 
{
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;

    public HandTransform(Vector3 _position)
    {
        position = _position;
        rotation = new Vector3(0, 0, 0);
        scale = new Vector3(1, 1, 1);
    }

    public HandTransform(Vector3 _position, Vector3 _rotation)
    {
        position = _position;
        rotation = _rotation;
        scale = new Vector3(1, 1, 1);
    }

    public HandTransform(Vector3 _position, Vector3 _rotation, Vector3 _scale)
    {
        position = _position;
        rotation = _rotation;
        scale = _scale;
    }
}
