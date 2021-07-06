using UnityEngine;

public class PyramidInstantiator : MonoBehaviour
{
    public GameObject pyramidCubePrefab;
    public int pyramidHeight;

    void Start()
    {
        for (int layer = 0; layer < pyramidHeight; ++layer)
        {
            int layerSize = pyramidHeight - layer;
            for (int x = 0; x < layerSize; ++x)
            {
                for (int y = 0; y < layerSize; ++y)
                {
                    var cubeInstance = Instantiate(pyramidCubePrefab, gameObject.transform.position, gameObject.transform.rotation);

                    var cubeY = 1.2f + layer * 0.06f;
                    var cubeX = (x * 0.06f) - ((layerSize - 1) * 0.06f) / 2;
                    var cubeZ = (y * 0.06f) - ((layerSize - 1) * 0.06f) / 2;

                    cubeInstance.transform.localPosition = gameObject.transform.position + new Vector3(cubeX, cubeY, cubeZ);
                }
            }
        }
    }
}
