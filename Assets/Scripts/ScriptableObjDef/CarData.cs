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

	#region Gear Ratios

	public float g1;
	public float g2;
	public float g3;
	public float g4;
	public float g5;
	public float g6;
	public float gR;
	public float diff;

	#endregion

	[Tooltip("cg = Center of Gravity")]
	public float cgToGround;
	[Tooltip("cg = Center of Gravity")]
	public float cgToFrontWheels;
	[Tooltip("cg = Center of Gravity")]
	public float cgToRearWheels;

}
