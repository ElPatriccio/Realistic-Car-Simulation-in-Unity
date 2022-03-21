using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CarSimulator : MonoBehaviour
{
	private Text speedText;
	private Text throttleText;
	private Text rpmText;
	private Text wheelRotText;
	private Text currentGearText;
	private Text adWheelRotText;

	private Rigidbody rb;

	[SerializeField] private Transform frontLeftWheel;
	[SerializeField] private Transform frontRightWheel;
	[SerializeField] private Transform rearLeftWheel;
	[SerializeField] private Transform rearRightWheel;

	[SerializeField] private WheelCollider frontLeftWheelC;
	[SerializeField] private WheelCollider frontRightWheelC;
	[SerializeField] private WheelCollider rearLeftWheelC;
	[SerializeField] private WheelCollider rearRightWheelC;

	#region Car Specs

	[SerializeField] private CarData carData;
	[SerializeField] private WheelData wheelData;

	private Dictionary<int, int> rpmTorqueCurve;

	#endregion

	#region Pedals

	private float verticalInput;
	private float horizontalInput;
	private float driveInput = 0;
	private float brakeInput = 0;
	private float pedalPosition = 10f;

	#endregion

	#region Forces opposing Drive

	private Vector3 F_drag = Vector3.zero;
	private Vector3 F_rr = Vector3.zero;

	private const float C_drag = 0.4257f;
	private const float C_rr = C_drag * 30f;

	#endregion

	#region Wheel Forces

	private float T_drive = 0;
	private float T_brake = 0;
	private float T_engineBrake = 0;

	#endregion

	#region Car Info

	private float engineRpm = 1000f;
	private bool isIdling = true;
	private float gearIndex = 1;
	private float currentGear;

	#endregion

	private bool isCounting = false;
	private bool isFinished = false;
	float startTime = 0f;

	private void Start()
	{
		rb = GetComponent<Rigidbody>();
		rb.mass = carData.weight;

		WheelCollider[] wheelColliders = { frontLeftWheelC, frontRightWheelC, rearLeftWheelC, rearRightWheelC };

		foreach (WheelCollider wheel in wheelColliders)
		{
			wheel.radius = wheelData.radius;
			wheel.mass = wheelData.weight;
		}

		rpmTorqueCurve = Utility.LoadXmlData(carData.torqueCurveFileName);

		currentGear = carData.g1;

		#region Text Getters

		speedText = GameObject.Find("Speed").GetComponent<Text>();
		throttleText = GameObject.Find("Throttle").GetComponent<Text>();
		rpmText = GameObject.Find("RPM").GetComponent<Text>();
		wheelRotText = GameObject.Find("WheelRotRate").GetComponent<Text>();
		currentGearText = GameObject.Find("CurrentGear").GetComponent<Text>();
		adWheelRotText = GameObject.Find("AdWheelRot").GetComponent<Text>();

		#endregion
	}

	private void Update()
	{
		driveInput = 0;
		brakeInput = 0;
		verticalInput = Input.GetAxisRaw("Vertical");
		horizontalInput = Input.GetAxisRaw("Horizontal");

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

		#region Gear Control

		if (Input.GetKeyDown(KeyCode.Keypad0))
		{
			gearIndex = 0;
			currentGear = carData.gR;
		}

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
		if (Input.GetKeyDown(KeyCode.Keypad6))
		{
			gearIndex = 6;
			currentGear = carData.g6;
		}

		#endregion

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

		if (driveInput > 0 && !isCounting)
		{
			isCounting = true;
			startTime = Time.fixedTime;
		}

	}

	private void FixedUpdate()
	{
		if (rb.velocity.magnitude * 3.6f >= 100 && !isFinished)
		{
			isFinished = true;
			Debug.Log(Time.fixedTime - startTime);
		}
		if(rb.velocity.magnitude * 3.6f <= 0.0001f && isFinished)
		{
			isFinished = false;
			isCounting = false;
		}

		#region Engine RPM

		engineRpm = Mathf.Abs(rearLeftWheelC.rpm) * 0.104719755f * Mathf.Abs(currentGear) * carData.diff * 60 / (2 * 3.14f);
		if (engineRpm < 1000)
		{
			engineRpm = 1000;
			isIdling = true;
		}
		else isIdling = false;

		#endregion

		#region Torques

		if (engineRpm > carData.maxRpm) T_drive = 0;
		else T_drive = rpmTorqueCurve[(int)engineRpm] * driveInput * currentGear * carData.diff * 0.7f;
		T_brake = brakeInput * carData.brakeForce;
		T_engineBrake = driveInput == 0 && !isIdling ? Mathf.Abs(currentGear) * 5f * (engineRpm / 60f) : 0f;
		#endregion

		#region Applying Torques on Wheel

		rearLeftWheelC.motorTorque = T_drive;
		rearRightWheelC.motorTorque = T_drive;

		frontLeftWheelC.brakeTorque = T_brake;
		frontRightWheelC.brakeTorque = T_brake;
		rearLeftWheelC.brakeTorque = T_brake * 0.2f + T_engineBrake;
		rearRightWheelC.brakeTorque = T_brake * 0.2f + T_engineBrake;

		frontLeftWheelC.steerAngle = Mathf.Lerp(frontLeftWheelC.steerAngle, horizontalInput * 30f, Time.fixedDeltaTime * 5);
		frontRightWheelC.steerAngle = Mathf.Lerp(frontRightWheelC.steerAngle, horizontalInput * 30f, Time.fixedDeltaTime * 5);

		#endregion

		#region Air & Rolling Resistance

		F_drag = -C_drag * rb.velocity.magnitude * rb.velocity;
		F_rr = -C_rr * rb.velocity;

		#endregion

		rb.AddForce(F_rr, ForceMode.Force);
		rb.AddForce(F_drag, ForceMode.Force);
		rb.AddForceAtPosition(350 * rb.velocity.magnitude * -transform.up, transform.position, ForceMode.Force);

		SetWheelTransforms();

	}

	private void SetWheelTransforms()
	{
		Vector3 _pos;
		Quaternion _rot;

		frontLeftWheelC.GetWorldPose(out _pos, out _rot);
		frontLeftWheel.position = _pos;
		frontLeftWheel.rotation = _rot;

		frontRightWheelC.GetWorldPose(out _pos, out _rot);
		frontRightWheel.position = _pos;
		frontRightWheel.rotation = _rot;

		rearLeftWheelC.GetWorldPose(out _pos, out _rot);
		rearLeftWheel.position = _pos;
		rearLeftWheel.rotation = _rot;

		rearRightWheelC.GetWorldPose(out _pos, out _rot);
		rearRightWheel.position = _pos;
		rearRightWheel.rotation = _rot;
	}


	private void OnGUI()
	{
		speedText.text = "Speed: " + (int)(rb.velocity.magnitude * 3.6f) + "km/h";
		throttleText.text = "Throttle: " + pedalPosition * 10 + "%";
		rpmText.text = "RPM: " + (int)engineRpm + "rpm";
		wheelRotText.text = "Rear Rot: " + (int)(rearLeftWheelC.rpm * 0.104719755f) + "rad/s";
		currentGearText.text = "Current Gear: " + gearIndex;
		adWheelRotText.text = "Front Rot: " + (int)(frontLeftWheelC.rpm * 0.104719755f) + "rad/s";
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
		end = start + (F_drag + F_rr) * 0.01f;
		Gizmos.color = Color.green;
		Gizmos.DrawLine(start, end);


	}
}
