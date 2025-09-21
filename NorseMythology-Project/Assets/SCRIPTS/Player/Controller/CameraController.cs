using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;
    public Vector3 offset;

    private Transform _transform;

    void Start()
    {
        _transform = transform;
        offset = new Vector3(0, 0, -10);

        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if(player != null)
                target = player.transform;
        }
    }

    void Update()
    {
        if(target != null)
            _transform.position = target.position + offset;
    }
}