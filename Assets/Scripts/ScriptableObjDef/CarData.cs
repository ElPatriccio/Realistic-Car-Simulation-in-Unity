using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Car Data", fileName = "carData_CAR_NAME")]
public class CarData : ScriptableObject
{

	public string torqueCurveFileName;

	public float weight;
	public float brakeForce;
	public float maxRpm;
	public float wheelbase;

	[Tooltip("cg = Center of Gravity")]
	public float cgToGround;
	[Tooltip("cg = Center of Gravity")]
	public float cgToFrontWheels;
	[Tooltip("cg = Center of Gravity")]
	public float cgToRearWheels;

}
