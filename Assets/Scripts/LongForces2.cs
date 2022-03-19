using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LongForces2 : MonoBehaviour
{
	private Text speedText;
	private Text throttleText;
	private Text rpmText;
	private Text wheelRotText;
	private Text currentGearText;

	private Rigidbody rb;

	[SerializeField] private GameObject wheelFront;
	[SerializeField] private GameObject wheelRear;

	#region Car Specs

	[SerializeField] private CarData carData;
	[SerializeField] private WheelData wheelData;

	private Dictionary<int, int> rpmTorqueCurve;

	#endregion

	#region Pedals

	private float verticalInput;
	private float driveInput = 0;
	private float brakeInput = 0;
	private float pedalPosition = 10f;

	#endregion

	#region Drive Forces

	private Vector3 F_traction = Vector3.zero;
	private Vector3 F_drive = Vector3.zero;
	private Vector3 F_total;

	#endregion

	#region Forces opposing Drive

	private Vector3 F_brake = Vector3.zero;
	private Vector3 F_drag = Vector3.zero;
	private Vector3 F_rr = Vector3.zero;

	private const float C_drag = 0.4257f;
	private const float C_rr = C_drag * 30f;

	#endregion

	#region Wheel Forces

	private float T_drive = 0;

	#endregion

	#region Car Info

	private float speed = 0;
	private Vector3 velocity = Vector3.zero;
	private Vector3 acceleration = Vector3.zero;
	private Vector3 directionOfDrive;

	private float engineRpm = 1000f;
	private float rearWheelLoad;
	private bool isReverse = false;
	private float wheelRotationRate = 0f;

	private float gearIndex = 1;
	private float currentGear;

	#endregion

	private void Start()
	{
		rb = GetComponent<Rigidbody>();
		directionOfDrive = isReverse ? -transform.forward : transform.forward;

		rpmTorqueCurve = Utility.LoadXmlData(carData.torqueCurveFileName);
		rearWheelLoad = (carData.cgToFrontWheels / carData.wheelbase) * (carData.weight * 9.81f);

		currentGear = carData.g1;

		#region Text Getters

		speedText = GameObject.Find("Speed").GetComponent<Text>();
		throttleText = GameObject.Find("Throttle").GetComponent<Text>();
		rpmText = GameObject.Find("RPM").GetComponent<Text>();
		wheelRotText = GameObject.Find("WheelRotRate").GetComponent<Text>();
		currentGearText = GameObject.Find("CurrentGear").GetComponent<Text>();

		#endregion
	}

	private void Update()
	{
		driveInput = 0;
		brakeInput = 0;
		verticalInput = Input.GetAxisRaw("Vertical");

		#region Throttle Control

		if (Input.GetKeyDown(KeyCode.O))
		{
			if (pedalPosition >= 10f) return;
			pedalPosition += 1f;
		}

		if (Input.GetKeyDown(KeyCode.L))
		{
			if (pedalPosition <= 0f) return;
			pedalPosition -= 1f;

		}

		#endregion

		if (Input.GetKeyDown(KeyCode.Keypad1))
		{
			gearIndex = 1;
			currentGear = carData.g1;
		}
		
		if (Input.GetKeyDown(KeyCode.Keypad2))
		{
			gearIndex = 2;
			currentGear = carData.g2;
		}
		if (Input.GetKeyDown(KeyCode.Keypad3))
		{
			gearIndex = 3;
			currentGear = carData.g3;
		}
		if (Input.GetKeyDown(KeyCode.Keypad4))
		{
			gearIndex = 4;
			currentGear = carData.g4;
		}
		if (Input.GetKeyDown(KeyCode.Keypad5))
		{
			gearIndex = 5;
			currentGear = carData.g5;
		}



		if (verticalInput > 0)
		{
			driveInput = 1 * (pedalPosition / 10f);
			brakeInput = 0;
		}
		else if (verticalInput < 0)
		{
			driveInput = 0;
			brakeInput = 1 * (pedalPosition / 10f);
		}



	}

	private void FixedUpdate()
	{
		if (velocity != Vector3.zero) directionOfDrive = velocity.normalized;

		#region Temporary Wheelrotation anim
		wheelFront.transform.Rotate((velocity.magnitude / wheelData.radius) * 57.2957f * Time.fixedDeltaTime * transform.forward);
		wheelRear.transform.Rotate((velocity.magnitude / wheelData.radius) * 57.2957f * Time.fixedDeltaTime * transform.forward);
		#endregion

		#region Wheel Load

		float newRearLoad = (carData.cgToFrontWheels / carData.wheelbase) * (carData.weight * 9.81f) + ((carData.cgToGround / carData.wheelbase) * carData.weight * (acceleration.magnitude * Utility.GetPrefix(acceleration, directionOfDrive)));
		rearWheelLoad = Mathf.Lerp(rearWheelLoad, newRearLoad, 0.2f);

		#endregion

		#region Wheel Rotation

		wheelRotationRate = velocity.magnitude / wheelData.radius;

		#endregion

		engineRpm = wheelRotationRate * currentGear * carData.diff * 60 / (2 * 3.14f);
		if (engineRpm < 1000) engineRpm = 1000;
		if (engineRpm > carData.maxRpm) T_drive = 0;
		else T_drive = ((rpmTorqueCurve[(int)engineRpm] * driveInput) * currentGear * carData.diff * 0.7f);

		F_drive = (T_drive / wheelData.radius) * directionOfDrive;

		F_traction = F_drive.magnitude < rearWheelLoad ? F_drive : (rearWheelLoad * wheelData.frictionCoefficient) * directionOfDrive;

		F_drag = -C_drag * velocity.magnitude * velocity;
		F_rr = -C_rr * velocity;

		F_brake = brakeInput * carData.brakeForce * -directionOfDrive;

		F_total = F_traction + F_drag + F_rr + F_brake;

		acceleration = F_total / carData.weight;
		velocity += acceleration * Time.fixedDeltaTime;
		CheckForStandstill();

		rb.velocity = velocity;
	}

	private void CheckForStandstill()
	{
		if (velocity.magnitude < 0.1f && F_brake != Vector3.zero)
		{
			acceleration = Vector3.zero;
			velocity = Vector3.zero;
		}
	}

	private void OnGUI()
	{
		speedText.text = "Speed: " + (int)(velocity.magnitude * 3.6f) + "km/h";
		throttleText.text = "Throttle: " + pedalPosition * 10 + "%";
		rpmText.text = "RPM: " + (int)engineRpm + "rpm";
		wheelRotText.text = "Wheel Rot: " + (int)wheelRotationRate + "rad/s";
		currentGearText.text = "Current Gear: " + gearIndex;
	}

	private void OnDrawGizmos()
	{
		Vector3 start = transform.position + transform.forward * 1.5f + transform.right * -1.4f;
		Vector3 end = start + F_drag * 0.01f;
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
