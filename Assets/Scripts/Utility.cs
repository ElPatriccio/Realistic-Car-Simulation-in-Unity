using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

public static class Utility
{
	public static Dictionary<int, int> LoadXmlData(string fileName)
	{
		Dictionary<int, int> torqueCurve = new Dictionary<int, int>();
		XDocument xml = XDocument.Load(Application.streamingAssetsPath + "/torqueCurves/" + fileName + ".xml");
		foreach (XElement element in xml.Root.Descendants())
		{
			torqueCurve.Add(int.Parse(element.Attribute("x").Value), int.Parse(element.Attribute("y").Value));
		}
		return torqueCurve;
	}
}
