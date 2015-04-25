using UnityEngine;
using System.Collections;

public class Car : MonoBehaviour
{

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
		//lower center of mass for roll-over resistance
		rigidbody.centerOfMass += centerOfMassAdjustment;
		//calculate the spread of top speed over the number of gears.
		gearSpread = topSpeed / numberOfGears;
	}

	// FixedUpdate is called once per physics frame
	void FixedUpdate ()
	{
		//Determine what texture to use on our brake lights right now.
		DetermineBreakLightState();
		//front wheel steering
		wheelFL.steerAngle = Input.GetAxis("Horizontal") * maxTurnAngle;
		wheelFR.steerAngle = Input.GetAxis("Horizontal")* maxTurnAngle;
		//rear wheel drive
		wheelBL.motorTorque = Input.GetAxis("Vertical") * maxTorque;
		wheelBR.motorTorque = Input.GetAxis("Vertical") * maxTorque;

		//adjust engine sound
		EngineSound();

		float rotationThisFrame = 360*Time.deltaTime;

		wheelTransformFL.Rotate(-wheelFL.rpm/rotationThisFrame,0,0);
		wheelTransformFR.Rotate(-wheelFR.rpm/rotationThisFrame,0,0);
		wheelTransformBL.Rotate(-wheelFR.rpm/rotationThisFrame,0,0);
		wheelTransformBR.Rotate(-wheelFR.rpm/rotationThisFrame,0,0);

		//Spoilers add down pressure based on the car’s speed. (Upside-down lift)
		Vector3 localVelocity = transform.InverseTransformDirection(rigidbody.velocity);
		rigidbody.AddForce(-transform.up*(localVelocity.z*spoilerRatio),ForceMode.Impulse);

		//calculate max speed in KM/H (condensed calculation)
		currentSpeed = wheelBL.radius*wheelBL.rpm*Mathf.PI*0.12f;
		if(currentSpeed < topSpeed)
		{
			//rear wheel drive.
			wheelBL.motorTorque = Input.GetAxis("Vertical") * maxTorque;
			wheelBR.motorTorque = Input.GetAxis("Vertical") * maxTorque;
		}
		else
		{
			//can't go faster, already at top speed that engine produces.
			wheelBL.motorTorque = 0;
			wheelBR.motorTorque = 0;
		}
		//Adjust the wheels heights based on the suspension.
		UpdateWheelPositions();

		//Handbrake controls
		if(Input.GetButton("Jump"))
		{
			applyHandbrake = true;
			wheelFL.brakeTorque = maxBrakeTorque;
			wheelFR.brakeTorque = maxBrakeTorque;
			//Wheels are locked, so power slide!
			if(rigidbody.velocity.magnitude > 1)
			{
				SetSlipValues(handbrakeForwardSlip, handbrakeSidewaysSlip);
			}
			else //skid to a stop, regular friction enabled.
			{
				SetSlipValues(1f,1f);
			}
		}
		else
		{
			applyHandbrake = false;
			wheelFL.brakeTorque = 0;
			wheelFR.brakeTorque = 0;
			SetSlipValues(1f,1f);
		}
		//apply deceleration when not pressing the gas or when breaking in either direction.
		if( !applyHandbrake && ((Input.GetAxis("Vertical") <= -0.5f && localVelocity.z > 0 ) || (Input.GetAxis("Vertical") >= 0.5f && localVelocity.z < 0) ))
		{
			wheelBL.brakeTorque = decelerationTorque + maxTorque;
			wheelBR.brakeTorque = decelerationTorque + maxTorque;
		}
		else if(!applyHandbrake && Input.GetAxis("Vertical") == 0)
		{
			wheelBL.brakeTorque = decelerationTorque;
			wheelBR.brakeTorque = decelerationTorque;
		}
		else
		{
			wheelBL.brakeTorque = 0;
			wheelBR.brakeTorque = 0;
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