using UnityEngine;

public class DebugMovementController : MonoBehaviour
{
    float speed = 2.0f;

    private Camera desktopCamera;

    void Start()
    {
        desktopCamera = gameObject.GetComponent<Camera>();
    }

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        transform.eulerAngles += new Vector3(-mouseY * 2, mouseX * 2, 0);

        var movementVector = new Vector3(horizontal, 0, vertical);
        var flyVector = new Vector3(0, 0, 0);
        if (Input.GetKey(KeyCode.Space))
            flyVector.y += 1;
        if (Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftControl))
            flyVector.y -=1;

        transform.position += (transform.rotation * movementVector + flyVector) * Time.deltaTime * speed;
    }
}
