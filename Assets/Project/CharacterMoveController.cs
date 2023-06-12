using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMoveController : MonoBehaviour
{
	[SerializeField] private Transform pivotJumpCollider;
    [SerializeField] private float speed = 5;
	[SerializeField] private float jumpHeight = 1;
	[SerializeField] private float gravity = -9f;
	[SerializeField] private float distCol = 1f;
	[SerializeField] private LayerMask layerMask;
	public Vector3 velocity;
	private CharacterController cc;

	public bool isJumping;
	public bool isGrounded;
    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
		cc = GetComponent<CharacterController>();
	}

    void Start()
    {
    }

    private void Update()
    {
        isGrounded = Physics.CheckSphere(pivotJumpCollider.position, distCol, layerMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -1f;
        }

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;

        if (Input.GetKey(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Input.GetKey(KeyCode.LeftControl))
        {
            cc.height = Mathf.Lerp(cc.height, 0.7f, Time.deltaTime * 2);
        }
        else
        {
            if (cc.height != 2)
            {
                cc.height = Mathf.Lerp(cc.height, 2, Time.deltaTime * 6);
            }
        }
    }

    void FixedUpdate()
    {
        cc.Move(((Input.GetAxis("Vertical") * transform.forward) + (Input.GetAxis("Horizontal") * transform.right * .7f)) * speed * Time.deltaTime);
        cc.Move(velocity * Time.deltaTime);
    }
}
