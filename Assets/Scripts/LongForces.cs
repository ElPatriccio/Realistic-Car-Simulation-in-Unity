using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LongForces : MonoBehaviour
{
	// Traction = Direction of car (u) * Engineforce
	// Drag = - C_drag * v * |v|
	// Rolling Resistance = - C_rr * v

	// C_rr must be approximately 30 times the value of Cdrag

	//Fmax = mu* W
	//Wf = (c/L)*W - (h/L)*M*a  => N
	//Wr = (b/L)*W + (h/L)*M*a => N

	// Torque = Force * distance

	//F_drive = u* T_engine * xgear* xdiff * nloss / Rw(radius of wheel) => N (muss kleiner sein als Wr weil sonst räder rutschen)

	//T_engine (rpm)

	// max torque = LookupTorqueCurve( rpm )
	// engine torque = throttle position* max torque

	//rpm = wheel rotation rate * gear ratio * differential ratio * 60 / 2 pi
	//inertia of a cylinder = Mass * radius2 / 2

	//load = Wr and Wf
	//traction torque = traction force * wheel radius

	[SerializeField] private TorqueCurve torqueCurve;

	public GameObject frontWheel;
	public GameObject rearWheel;
	private Dictionary<float, int> rpmToTorque = new Dictionary<float, int>();

	private Rigidbody rb;

	private Vector3 F_traction;
	private Vector3 T_brake;
	private Vector3 F_tractionPerWheel;
	private Vector3 F_drag;
	private Vector3 F_rr;
	private Vector3 F_long;
	private Vector3 F_maxFrictionFront;
	private Vector3 F_maxFrictionRear;

	private Vector3 T_drive;
	private Vector3 F_drive;

	private Vector3 T_traction = Vector3.zero;
	private float C_slipSlope = 20;

	[SerializeField] private float C_engineforce = 5000;
	[SerializeField] private float C_carWeight = 1300;
	[SerializeField] private float C_brakingForce = 5000;
	[SerializeField] private float C_wheelFriction = 1.2f;
	[SerializeField] private float C_wheelWeight = 30f;
	[SerializeField] private float C_wheelRadius = 0.35f;


	private const float C_drag = 0.4257f;
	private const float C_rr = C_drag * 30f;

	private Vector2 C_centerOfMass;
	private float C_distanceToFrontWheels;
	private float C_distanceToRearWheels;
	private float C_distanceToGround;
	private float C_wheelbase;


	[NonSerialized] public float speed = 0;
	private float brakeInput = 0;
	private float engineRpm = 1000;
	private float engineTorque;
	private float wheelRotationRate = 0;  //rad/s
	private float slipRatio = 0;
	private float tractionForce = 0;
	private Vector3 acceleration = Vector3.zero;
	private Vector3 velocity = Vector3.zero;

	private float rearWheelLoad;
	private float rearWheelMaxForce;
	private Vector3 wheelAcc = Vector3.zero;
	private Vector3 wheelVelocity = Vector3.zero;


	private bool isBreaking = false;
	private float driveInput = 0;

	private void Start()
	{

		rb = gameObject.GetComponent<Rigidbody>();
		C_centerOfMass = new Vector2(rb.centerOfMass.z, rb.centerOfMass.y);
		C_distanceToFrontWheels = new Vector2(Math.Abs(C_centerOfMass.x - frontWheel.transform.localPosition.z), 0).magnitude;
		C_distanceToRearWheels = new Vector2(Math.Abs(C_centerOfMass.x - rearWheel.transform.localPosition.z), 0).magnitude;
		C_distanceToGround = new Vector2(0, transform.position.y + rb.centerOfMass.y).magnitude;
		C_wheelbase = new Vector2(Math.Abs(frontWheel.transform.localPosition.z - rearWheel.transform.localPosition.z), 0).magnitude;
	}


	private void Update()
	{
		brakeInput = 0;
		driveInput = 0;
		if (transform.position.x < -301.025f)
		{
			transform.position = new Vector3(-1.025f - (transform.position.x + 301.025f), transform.position.y, transform.position.z);
		}

		if (Input.GetAxisRaw("Vertical") > 0)
		{
			brakeInput = 0;
			driveInput = 1;
		}
		else if (Input.GetAxisRaw("Vertical") < 0)
		{
			brakeInput = 1;
			driveInput = 0;
		}

		if (Input.GetKey(KeyCode.U))
		{
			if (engineRpm < 6000) engineRpm += 100;
		}
		if (Input.GetKey(KeyCode.J))
		{
			if (engineRpm > 1000) engineRpm -= 100;
		}
	}

	private void FixedUpdate()
	{
		int prefix = 1;
		if (acceleration.normalized + transform.forward == Vector3.zero) prefix = -1;
		rearWheelLoad = (C_distanceToFrontWheels / C_wheelbase) * C_carWeight * 9.81f + (C_distanceToGround / C_wheelbase) * C_carWeight * (prefix * acceleration.magnitude);
		rearWheelMaxForce = C_wheelFriction * rearWheelLoad;
		//Debug.Log(T_drive);
		//Debug.Log(F_traction);
		wheelAcc = (T_drive + T_traction) / ((C_wheelWeight * (C_wheelRadius * C_wheelRadius)) / 2f);
		Debug.Log(wheelAcc);
		wheelVelocity += wheelAcc * Time.fixedDeltaTime;
		wheelRotationRate = wheelVelocity.magnitude * -1;
		if (wheelVelocity.normalized + transform.forward == Vector3.zero) wheelRotationRate *= -1;

		if (speed == 0 && driveInput > 0) slipRatio = ((wheelRotationRate * C_wheelRadius - 0.01f) / 0.01f) * -1;
		else if (speed == 0) slipRatio = 0;
		else slipRatio = (wheelRotationRate * C_wheelRadius - speed) / Math.Abs(speed);

		engineRpm = (int)Math.Round(wheelRotationRate * 2.66f * 3.42f * (60 / (2 * 3.14f)), 0);
		if (engineRpm < 1000) engineRpm = 1000;
		if (engineRpm > 6000) engineRpm = 6000;
		Debug.Log(engineRpm);

		if (velocity != Vector3.zero && velocity.normalized + transform.forward != Vector3.zero) T_brake = -1 * brakeInput * C_brakingForce * transform.forward;
		else if (brakeInput == 1)
		{
			velocity = Vector3.zero;
			T_brake = Vector3.zero;
		}
		else T_brake = Vector3.zero;

		T_drive = 0.7f * 2.66f * 3.42f * driveInput * torqueCurve.GetTorque(engineRpm) * transform.forward;
		F_drive = T_drive / C_wheelRadius;
		tractionForce = slipRatio * 20 * rearWheelLoad;
		if (tractionForce > rearWheelMaxForce) tractionForce = rearWheelMaxForce;
		F_traction = transform.forward * tractionForce;
		T_traction = 1/4*F_traction * C_wheelRadius * -1;

		CalcLongForce();
		acceleration = F_long / C_carWeight;
		velocity += Time.fixedDeltaTime * acceleration;

		rb.velocity = velocity;

		speed = velocity.magnitude;
		if (velocity.normalized + transform.forward == Vector3.zero) speed *= -1;
	}

	private void CalcLongForce()
	{
		F_drag = -C_drag * velocity.magnitude * velocity;
		F_rr = -C_rr * velocity;
		F_long = F_drive + F_drag + F_rr;
	}

	private void OnGUI()
	{
		GUI.Box(new Rect(10, 20, 200, 100), "Speed: " + Math.Round(speed * 3.6, 1) + "km/h");
	}

	private void OnDrawGizmos()
	{

		Vector3 start = transform.position + transform.forward * 1.5f;
		Vector3 end = start + F_long * 0.001f;
		if (F_long.normalized + transform.forward == Vector3.zero) Gizmos.color = Color.red;
		else Gizmos.color = Color.green;
		Gizmos.DrawLine(start, end);

		Gizmos.color = Color.red;
		Gizmos.DrawLine(start, (end - F_long * 0.001f + T_brake));
	}
}




