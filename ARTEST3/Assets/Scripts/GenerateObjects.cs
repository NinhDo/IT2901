﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateObjects : MonoBehaviour {

	// The earth's mean radius in meters
	const float EARTH_MEAN_RADIUS = 6372.8e3f;

	// Our location. Default location is somewhere in the middle of Trondheim
	public static GPSManager.GPSLocation myLocation = new GPSManager.GPSLocation(63.430626, 10.392145);
	// The list containing the locations of each road object
	List<GPSManager.GPSLocation> roadObjectList = new List<GPSManager.GPSLocation>();
	
	// The object to instantiate (create) when placing the road objects
	public GameObject blueSign;
	public GameObject redSign;
	public GameObject redTriangle;

	// SerializeField makes the private field visible to the editor
	// Currently, no other GameObject needs the GPSManager, so this is fine
	// May want to make GPSManager static if all objects need access
	[SerializeField]
	private GPSManager gpsManager;
	[SerializeField]
	private APIWrapper apiWrapper;

	// Use this for initialization
	void Start() {
		/*
		Get the GPSManager script atteched to this same object. Gives you an error if this gameobject does not have the GPSManager script attached
		so make sure that it has it attached
		*/
		gpsManager = GetComponent<GPSManager>();
		apiWrapper = GetComponent<APIWrapper>();

		// Update position
		myLocation.latitude = gpsManager.myLatitude;
		myLocation.longitude = gpsManager.myLongitude;
		myLocation.altitude = gpsManager.myAltitude;
		StartCoroutine(FetchObjects());
	}

	// Update is called once per frame
	void Update() {
		// If we have a gpsManager
		if (gpsManager != null) {
			// Update our location
			myLocation = new GPSManager.GPSLocation(gpsManager.myLatitude, gpsManager.myLongitude, gpsManager.myAltitude);
		}
	}

	IEnumerator FetchObjects() {
		yield return new WaitForSeconds(10);
		apiWrapper.FetchObjects(myLocation, objects => {
			this.roadObjectList = objects;
			this.MakeObjectsFromLatLon();
		});
	}
	// The haversine formula calculates the distance between two gps locations by air (ignoring altitude).
	// Parameters:
	//		GPSLocation startLocation -> The location where we are
	//		GPSLocation endLocation -> The location where the object is
	// Returns the distance between the startLocation and endLocation in meters (1 Unit = 1 meter for simplicity)
	public static float Haversine(GPSManager.GPSLocation startLocation, GPSManager.GPSLocation endLocation) {
		double dLat = (endLocation.latitude - startLocation.latitude) * Mathf.Deg2Rad;
		double dLon = (endLocation.longitude - startLocation.longitude) * Mathf.Deg2Rad;
		startLocation.latitude *= Mathf.Deg2Rad;
		endLocation.latitude *= Mathf.Deg2Rad;
		// a = Sin(dLat/2)^2 + Sin(dLon/2)^2 * Cos(sLat) * Cos(eLat)
		float a = Mathf.Pow(Mathf.Sin((float) dLat / 2), 2) 
				+ Mathf.Pow(Mathf.Sin((float) dLon / 2), 2) 
				* Mathf.Cos((float) startLocation.latitude) 
				* Mathf.Cos((float) endLocation.latitude);
		float c = 2 * Mathf.Asin(Mathf.Sqrt(a));
		float d = EARTH_MEAN_RADIUS * 2 * c;
		return d;
	}

	// The formula that calculates the bearing when travelling from startLocation to endLocation
	// Parameters:
	//		GPSLocation startLocation -> The location where we are
	//		GPSLocation endLocation -> The location where the object is
	// Returns the bearing from startLocation to endLocation in radians
	public static float CalculateBearing(GPSManager.GPSLocation startLocation, GPSManager.GPSLocation endLocation) {
		float x = Mathf.Cos((float) startLocation.latitude * Mathf.Deg2Rad)
				* Mathf.Sin((float) endLocation.latitude * Mathf.Deg2Rad)
				- Mathf.Sin((float) startLocation.latitude * Mathf.Deg2Rad)
				* Mathf.Cos((float) endLocation.latitude * Mathf.Deg2Rad)
				* Mathf.Cos((float) (endLocation.longitude - startLocation.longitude) * Mathf.Deg2Rad);
		float y = Mathf.Sin((float) (endLocation.longitude - startLocation.longitude) * Mathf.Deg2Rad)
				* Mathf.Cos((float) endLocation.latitude * Mathf.Deg2Rad);
		return Mathf.Atan2(y, x) + Mathf.PI / 2;
	}

	// Uses the locations in roadObjectList and instantiates objects
	void MakeObjectsFromLatLon() {
		// For each location in the list
		foreach (GPSManager.GPSLocation location in roadObjectList) {
			// Create a new position where x, y, and z is 0
			Vector3 position = Vector3.zero;
			// Calculate the distance and bearing between us and the location
			float distance = Haversine(myLocation, location);
			float bearing = CalculateBearing(myLocation, location);
			//Debug.Log("Distance: " + distance + "\nBearing: " + bearing);
			// calculate the x and z offset between us and the location and update the x and z position
			position.x = -Mathf.Cos(bearing) * distance;
			position.z = Mathf.Sin(bearing) * distance;


			// MAKE SIGNS HERE
			// Instantiate a new GameObject on that location relative to us
			GameObject newGameObject = (GameObject) Instantiate(blueSign, position, Quaternion.identity);
			
			
			
			
			// Set the parent of the new GameObject to be us (so we dont have a huge list in root)
			newGameObject.transform.parent = gameObject.transform;
			newGameObject.GetComponent<RoadObjectManager>().roadObjectLocation = location;
			newGameObject.GetComponent<RoadObjectManager>().updateLocation();
		}
	}
}