using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;
    public Vector3 offset;

    void Start()
    {
        offset = new Vector3(0, 0, -10);

        if (target == null)
        {
            target = GameObject.FindGameObjectWithTag("Player").transform;
        }

    }

    void Update()
    {
        transform.position = target.position + offset;
    }
}