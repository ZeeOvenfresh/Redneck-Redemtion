using UnityEngine;
using System.Collections;

public class ChaseCamera : MonoBehaviour 
{
	public Transform car;
	public float distance;
	public float height;
	public float rotationDamping = 3f;
	public float heightDamping = 2f;
	private float desiredAngle = 0;
	bool cameraToggle = false;
	// LateUpdate is called once per frame after Update() has been called
	void LateUpdate ()
	{
		if (Input.GetButton ("Fire1")) 
		{
			cameraToggle = true;
			float currentAngle = transform.eulerAngles.y;
			float currentHeight = transform.position.y;
			//Determine where we want to be.
			desiredAngle = car.eulerAngles.y;
			float desiredHeight = car.position.y + height;
			//Now move towards our goals.
			currentAngle = Mathf.LerpAngle (currentAngle, desiredAngle, rotationDamping * Time.deltaTime);
			currentHeight = Mathf.Lerp (currentHeight, desiredHeight, heightDamping * Time.deltaTime);
			Quaternion currentRotation = Quaternion.Euler (0, currentAngle, 0);
			//Set our new positions.
			Vector3 finalPosition = car.position - (currentRotation * Vector3.forward * distance);
			finalPosition.y = currentHeight;
			transform.position = finalPosition;
			transform.LookAt (car);
		}
		else
		{
			cameraToggle = false;
		}
	}
	void FixedUpdate()
	{
		desiredAngle = car.eulerAngles.y; //NOTE: Removed from LateUpdate() and added here.
		//if the car is going backwards add 180 to the wanted rotation.
		Vector3 localVelocity = car.InverseTransformDirection(car.rigidbody.velocity);
		if(localVelocity.z < -0.5f)
		{
			desiredAngle += 180;
		}
	}
}