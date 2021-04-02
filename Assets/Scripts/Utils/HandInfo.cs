using UnityEngine;

public class HandInfo 
{
    public Vector3 position;
    public Vector3 rotation;

    public HandInfo(Vector3 pos)
    {
        position = pos;
        rotation = new Vector3(0, 0, 0);
    }

    public HandInfo(Vector3 pos, Vector3 rot)
    {
        position = pos;
        rotation = rot;
    }
}
