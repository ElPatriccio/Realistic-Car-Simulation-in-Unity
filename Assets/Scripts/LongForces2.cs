using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LongForces2 : MonoBehaviour
{
	#region Car Specs

	[SerializeField] private CarData carData;
	[SerializeField] private WheelData wheelData;

	private Rigidbody rb;

	#endregion

	#region Pedals

	private float verticalInput;
	private float driveInput = 0;
	private float brakeInput = 0;
	private float pedalPosition = 1;

	#endregion

	#region Drive Forces

	private Vector3 F_traction = Vector3.zero; 

	#endregion

	#region Forces opposing Drive

	private Vector3 F_drag = Vector3.zero;
	private Vector3 F_rr = Vector3.zero;

	private const float C_drag = 0.4257f;
	private const float C_rr = C_drag * 30f;

	#endregion

	#region Car Info

	private float speed = 0;
	private Vector3 velocity = Vector3.zero;
	private Vector3 acceleration = Vector3.zero;

	private Vector3 directionOfDrive;

	#endregion

	private void Start()
	{
		rb = GetComponent<Rigidbody>();
		directionOfDrive = transform.forward;
		Debug.Log(Utility.LoadXmlData(carData.torqueCurveFileName)[1001]);

	}

	private void Update()
	{
		driveInput = 0;
		brakeInput = 0;
		verticalInput = Input.GetAxisRaw("Vertical");

		if (Input.GetKey(KeyCode.O))
		{
			if (pedalPosition >= 1) return;
			pedalPosition += 0.1f;
		}

		if (Input.GetKey(KeyCode.L))
		{
			if (pedalPosition <= 0) return;
			pedalPosition -= 0.1f;
		}

		if (verticalInput > 0)
		{
			driveInput = 1 * pedalPosition;
			brakeInput = 0;
		}
		else if(verticalInput < 0)
		{
			driveInput = 0;
			brakeInput = 1 * pedalPosition;
		}
	}

	private void FixedUpdate()
	{
		F_traction = 3000 * driveInput * directionOfDrive;
		F_drag = -C_drag * velocity.magnitude * velocity;
		F_rr = -C_rr * velocity;

		acceleration = (F_traction + F_drag + F_rr) / carData.weight;
		velocity += acceleration * Time.fixedDeltaTime;

		rb.velocity = velocity;

		Debug.Log(velocity.magnitude);
	}

	private void OnDrawGizmos()
	{
		Vector3 start = transform.position + transform.forward * 1.5f + transform.right * -1.4f;
		Vector3 end = start + F_drag*0.01f;
		Gizmos.color = Color.blue;
		Gizmos.DrawLine(start, end);

		start = transform.position + transform.forward * 1.5f + transform.right * -1.5f;
		end = start + F_rr * 0.01f;
		Gizmos.color = new Color(255, 140, 0);
		Gizmos.DrawLine(start, end);

		start = transform.position + transform.forward * 1.5f + transform.right * -1.6f;
		end = start + (F_traction + F_drag + F_rr) * 0.01f;
		Gizmos.color = Color.green;
		Gizmos.DrawLine(start, end);


	}
}
