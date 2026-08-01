using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

public class PlayerMovementAndLook : MonoBehaviour {

	[Header("Camera")]
	public Camera mainCamera;


	[Header("Movement")]
	public Rigidbody playerRigidbody;
	public float speed = 4.5f;
	private Vector3 inputDirection;
	private Vector3 movement;


	//Rotation

	private Plane playerMovementPlane;

	private RaycastHit floorRaycastHit;

	[SerializeField]
	private Vector3 playerToMouse;


	[Header("Animation")]
	public Animator playerAnimator;

	private PlayerJoystickRotate _playerJoystickRotate;

	void Awake()
	{
		CreatePlayerMovementPlane();
		_playerJoystickRotate = FindObjectOfType<PlayerJoystickRotate>();
	}

	void CreatePlayerMovementPlane()
	{
		playerMovementPlane = new Plane (transform.up, transform.position + transform.up);
	}

	void FixedUpdate()
	{

		//Arrow Key Input
		float h = CrossPlatformInputManager.GetAxis("Horizontal");
		float v = CrossPlatformInputManager.GetAxis("Vertical");

		inputDirection = new Vector3(h, 0, v);

		//Camera Direction
		var cameraForward = mainCamera.transform.forward;
		var cameraRight = mainCamera.transform.right;

		cameraForward.y = 0f;
		cameraRight.y = 0f;

		//Try not to use var for roadshows or learning code
		Vector3 desiredDirection = cameraForward * inputDirection.z + cameraRight * inputDirection.x;
		
		//Why not just pass the vector instead of breaking it up only to remake it on the other side?
		MoveThePlayer(desiredDirection);
		TurnThePlayer();
		AnimateThePlayer(desiredDirection);
		
	}

	void MoveThePlayer(Vector3 desiredDirection)
	{
		movement.Set(desiredDirection.x, 0f, desiredDirection.z);

		movement = movement.normalized * speed * Time.deltaTime;

		playerRigidbody.MovePosition(transform.position + movement);

	}

	void TurnThePlayer()
	{
#if MOBILE_INPUT
		if (_playerJoystickRotate != null)
		{
			Vector3 cursorScreenPosition = _playerJoystickRotate.MapToScreenPosition;

			Vector3 cursorWorldPosition = ScreenPointToWorldPointOnPlane(cursorScreenPosition, playerMovementPlane, mainCamera);

			playerToMouse = cursorWorldPosition - transform.position;
		}
		else
		{
			playerToMouse.x = CrossPlatformInputManager.GetAxis("Mouse X");
			playerToMouse.z = CrossPlatformInputManager.GetAxis("Mouse Y");
		}
#else
		Vector3 cursorScreenPosition = Input.mousePosition;

		Vector3 cursorWorldPosition = ScreenPointToWorldPointOnPlane(cursorScreenPosition, playerMovementPlane, mainCamera);

		playerToMouse = cursorWorldPosition - transform.position;
#endif
		playerToMouse.y = 0f;
		if (Mathf.Approximately(playerToMouse.magnitude, 0f))
			return;
		playerToMouse.Normalize();

		Quaternion newRotation = Quaternion.LookRotation(playerToMouse);

		playerRigidbody.MoveRotation(newRotation);

	}

	Vector3 PlaneRayIntersection(Plane plane, Ray ray)
	{
		float dist = 0.0f;
		plane.Raycast(ray, out dist);
		return ray.GetPoint(dist);
	}

	Vector3 ScreenPointToWorldPointOnPlane(Vector3 screenPoint, Plane plane, Camera camera)
	{	
		Ray ray = camera.ScreenPointToRay(screenPoint);
		return PlaneRayIntersection(plane, ray);
	}


	void AnimateThePlayer(Vector3 desiredDirection)
	{
		if(!playerAnimator)
			return;

		Vector3 movement = new Vector3(desiredDirection.x, 0f, desiredDirection.z);
		float forw = Vector3.Dot(movement, transform.forward);
		float stra = Vector3.Dot(movement, transform.right);

		playerAnimator.SetFloat("Forward", forw);
		playerAnimator.SetFloat("Strafe", stra);

		/*
		bool walking = h != 0f || v != 0f;

		if(walking)
		{
			Vector3 movement = new Vector3(h, 0f, v);
			float forw = Vector3.Dot(movement, transform.forward);
			float stra = Vector3.Dot(movement, transform.right);

			playerAnimator.SetFloat("Forward", forw);
			playerAnimator.SetFloat("Strafe", stra);
		}
		*/

	}
}
