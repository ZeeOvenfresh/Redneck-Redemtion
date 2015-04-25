using UnityEngine;
using System.Collections;

public class CarController : MonoBehaviour
{
	public Transform waypointContainer;
	private Transform[] waypoints;
	private int currentWaypoint=0;
	private float inputSteer;
	private float inputTorque;


	public float topSpeed = 150;
	public float maxReverseSpeed = 50;
	private float currentSpeed;
	public float maxTurnAngle = 10;
	public float maxTorque = 10;
	
	//Gear Sounds
	public int numberOfGears;
	private float gearSpread;
	
	//Brake Light Stuff
	public GameObject BrakeLights;
	public Texture2D idleLightTex;
	public Texture2D brakeLightTex;
	public Texture2D reverseLightTex;
	
	//WhellColliders For all 4 wheels
	public WheelCollider wheelFL;
	public WheelCollider wheelFR;
	public WheelCollider wheelBL;
	public WheelCollider wheelBR;
	public float spoilerRatio = 0.1f;
	
	//WhellMeshes For all 4 wheels
	public Transform wheelTransformFL;
	public Transform wheelTransformFR;
	public Transform wheelTransformBL;
	public Transform wheelTransformBR;
	public float decelerationTorque = 30;
	
	public Vector3 centerOfMassAdjustment = new Vector3(0f,-0.9f,0f);
	
	//HandBrake Controls
	public float maxBrakeTorque = 100;
	private bool applyHandbrake = false;
	public float handbrakeForwardSlip = 0.04f;
	public float handbrakeSidewaysSlip = 0.08f;
	
	void Start()
	{
		//get the waypoints from the track.
		GetWaypoints();
		//lower center of mass for roll-over resistance
		rigidbody.centerOfMass += centerOfMassAdjustment;
		//calculate the spread of top speed over the number of gears.
		gearSpread = topSpeed / numberOfGears;
	}
	
	// FixedUpdate is called once per physics frame
	void FixedUpdate ()
	{
		//calculate turn angle
		Vector3 RelativeWaypointPosition = transform.InverseTransformPoint(new Vector3( waypoints[currentWaypoint].position.x, transform.position.y, waypoints[currentWaypoint].position.z ));
		inputSteer = RelativeWaypointPosition.x / RelativeWaypointPosition.magnitude;
		//Spoilers add down pressure based on the car’s speed. (Upside-down lift)
		Vector3 localVelocity = transform.InverseTransformDirection(rigidbody.velocity);
		rigidbody.AddForce(-transform.up * (localVelocity.z * spoilerRatio),ForceMode.Impulse);
		//calculate torque.
		if ( Mathf.Abs( inputSteer ) < 0.5f )
		{
			//when making minor turning adjustments speed is based on how far to the next point.
			inputTorque = (RelativeWaypointPosition.z / RelativeWaypointPosition.magnitude);
			applyHandbrake = false;
		}
		else
		{
			//we need to make a hard turn, if moving fast apply handbrake to slide.
			if(rigidbody.velocity.magnitude > 10)
			{
				applyHandbrake = true;
			}
			//if not moving forward backup and turn opposite.
			else if(localVelocity.z < 0)
			{
				applyHandbrake = false;
				inputTorque = -1;
				inputSteer *= -1;
			}
			//let off the gas while making a hard turn.
			else
			{
				applyHandbrake = false;
				inputTorque = 0;
			}
		}
		//set slip values
		if(applyHandbrake)
		{
			SetSlipValues(handbrakeForwardSlip, handbrakeSidewaysSlip);
		}
		else
		{
			SetSlipValues(1f, 1f);
		}
		//if close enough, change waypoints.
		if ( RelativeWaypointPosition.magnitude < 25 )
		{
			currentWaypoint ++;
			if ( currentWaypoint >= waypoints.Length )
			{
				currentWaypoint = 0;
			}
		}
		//front wheel steering
		wheelFL.steerAngle = inputSteer * maxTurnAngle;
		wheelFR.steerAngle = inputSteer * maxTurnAngle;
		//calculate max speed in KM/H (optimized calc)
		currentSpeed = wheelBL.radius*wheelBL.rpm*Mathf.PI*0.12f;
		if(currentSpeed < topSpeed && currentSpeed > maxReverseSpeed)
		{
			//rear wheel drive.
			wheelBL.motorTorque = inputTorque * maxTorque;
			wheelBR.motorTorque = inputTorque * maxTorque;
		}
		else
		{
			//can't go faster, already at top speed that engine produces.
			wheelBL.motorTorque = 0;
			wheelBR.motorTorque = 0;
		}
	}

	void GetWaypoints()
	{
		//NOTE: Unity named this function poorly it also returns the parent’s component.
		Transform[] potentialWaypoints = waypointContainer.GetComponentsInChildren<Transform>();
		//initialize the waypoints array so that is has enough space to store the nodes.
		waypoints = new Transform[ (potentialWaypoints.Length - 1) ];
		//loop through the list and copy the nodes into the array.
		//start at 1 instead of 0 to skip the WaypointContainer’s transform.
		for (int i = 1; i < potentialWaypoints.Length; ++i )
		{
			waypoints[ i-1 ] = potentialWaypoints[i];
		}
	}

	void EngineSound()
	{
		//going forward calculate how far along that gear we are and the pitch sound.
		if(currentSpeed > 0)
		{
			if(currentSpeed > topSpeed)
			{
				audio.pitch = 1.75f;
			}
			else
			{
				audio.pitch = ((currentSpeed % gearSpread) / gearSpread) + 0.75f;
			}
		}
		//when reversing we have only one gear.
		else
		{
			audio.pitch = (currentSpeed / maxReverseSpeed) + 0.75f;
		}
	}
	
	void DetermineBreakLightState()
	{
		if((currentSpeed > 0 && Input.GetAxis("Vertical") < 0)
		   || (currentSpeed < 0 && Input.GetAxis("Vertical") > 0)
		   || applyHandbrake)
		{
			BrakeLights.renderer.material.mainTexture = brakeLightTex;
		}
		else if(currentSpeed < 0 && Input.GetAxis("Vertical") < 0)
		{
			BrakeLights.renderer.material.mainTexture = reverseLightTex;
		}
		else
		{
			BrakeLights.renderer.material.mainTexture = idleLightTex;
		}
	}
	void SetSlipValues(float forward, float sideways)
	{
		//Change the stiffness values of wheel friction curve and then reapply it.
		WheelFrictionCurve tempStruct = wheelBR.forwardFriction;
		tempStruct.stiffness = forward;
		wheelBR.forwardFriction = tempStruct;
		tempStruct = wheelBR.sidewaysFriction;
		tempStruct.stiffness = sideways;
		wheelBR.sidewaysFriction = tempStruct;
		tempStruct = wheelBL.forwardFriction;
		tempStruct.stiffness = forward;
		wheelBL.forwardFriction = tempStruct;
		tempStruct = wheelBL.sidewaysFriction;
		tempStruct.stiffness = sideways;
		wheelBL.sidewaysFriction = tempStruct;
	}
	//move wheels based on their suspension.
	void UpdateWheelPositions()
	{
		WheelHit contact = new WheelHit();
		if(wheelFL.GetGroundHit(out contact))
		{
			Vector3 temp = wheelFL.transform.position;
			temp.y = (contact.point + (wheelFL.transform.up*wheelFL.radius)).y;
			wheelTransformFL.position = temp;
		}
		if(wheelFR.GetGroundHit(out contact))
		{
			Vector3 temp = wheelFR.transform.position;
			temp.y = (contact.point + (wheelFR.transform.up*wheelFR.radius)).y;
			wheelTransformFR.position = temp;
		}
		if(wheelBL.GetGroundHit(out contact))
		{
			Vector3 temp = wheelBL.transform.position;
			temp.y = (contact.point + (wheelBL.transform.up*wheelBL.radius)).y;
			wheelTransformBL.position = temp;
		}
		if(wheelBR.GetGroundHit(out contact))
		{
			Vector3 temp = wheelBR.transform.position;
			temp.y = (contact.point + (wheelBR.transform.up*wheelBR.radius)).y;
			wheelTransformBR.position = temp;
		}
	}
}