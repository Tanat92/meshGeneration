using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform player, cam;
	[SerializeField] private float sensative = 1f;

    void FixedUpdate()
    {
        player.transform.localEulerAngles = new Vector3(0, player.transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensative * Time.deltaTime, 0);
        cam.transform.localEulerAngles = new Vector3(cam.transform.localEulerAngles.x - Input.GetAxis("Mouse Y") * sensative * Time.deltaTime, 0, 0);
    }
}
