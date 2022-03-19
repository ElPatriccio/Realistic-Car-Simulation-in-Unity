using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Wheel Data", fileName ="wheelData")]
public class WheelData : ScriptableObject
{
	public float weight;
	public float radius;

	[Tooltip("Friction Coeficient for street tyres normally is 1.0 (racing tyres 1.5)")]
	public float frictionCoefficient;
}
