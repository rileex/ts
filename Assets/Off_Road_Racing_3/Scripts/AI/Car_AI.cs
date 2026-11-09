//______________________________________________
// Car AI is based on the below tutorial:
// https://youtube.com/playlist?list=PLB9LefPJI-5wH5VdLFPkWfnPjeI6OSys1&feature=shared
//______________________________________________

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ALIyerEdon;

namespace ALIyerEdon
{
	public class Car_AI : MonoBehaviour
	{

		#region Public Variables

		public float steerSensibility = 20f;

		public bool canRespawn = true;

		[Space(10)]
		[Header("Waypoint System___________________________________________")]

		[Tooltip("Find the target waypoint by its name")]
		public string pathName = "Path_1";

		[Tooltip("Remaining distance to go to the next waypoint")]
		public float remainingDistance = 22f;

		[Space(10)]
		[Header("Raycast System___________________________________________")]

		[Tooltip("Cast AI rays from here")]
		public Transform rayPosition;

		[Space(10)]
		// Corner ray settings
		public float cornerSensorLength = 2;
		public float cornerSensorAngle = 90;

		[Space(10)]
		// Center ray settings
		public float centerSensorLength = 7;
		public float centerSensorAngle = 15;

		[Space(10)]
		[Tooltip("Hit the AI raycast only to these layers")]
		public LayerMask raycastLayers;


		#endregion

		#region Internal variables
		float steerInput_Temp;
		bool raysHit;
		bool followWaypoint;

		[HideInInspector] public bool raceStarted = false;
		[HideInInspector] public bool brakeTrigger = false;
		public int currentWaypoint = 0;
		[HideInInspector] public int currentLap = 0;
		[HideInInspector] public Waypoint_System path;

		// Temp the overall steer input
		float newSteer = 0;

		// Determine that which ray has been hitted
		bool centerLeft_Hit;
		bool centerRight_Hit;
		bool cornerLeft_Hit;
		bool cornerRight_Hit;

		// New steer inputs based on hitted rays angles
		float steerInput_cornerRight;
		float steerInput_centerLeft;
		float steerInput_centerRight;
		float steerInput_cornerLeft;

		// Store waypoint points
		List<Transform> waypoints = new List<Transform>();

		//RCC_CarControllerV3 vController;
		//public RCC_Inputs newInputs = new RCC_Inputs();
		EasyCarController vController;

		#endregion

		bool checkReversing = false;
		bool isReversing = false;
		//public float respawnDelay = 7f;
		float carSpeed = 0;
		[HideInInspector] public bool canReverseCheck;

		void Start()
		{

			#region Initialize

			path = GameObject.Find(pathName).GetComponent<Waypoint_System>();

			waypoints = path.waypoints;

			currentLap = 0;

			GetComponent<Car_Position>().currentLap = currentLap;


			//GotoNextPoint();

			GetComponent<Car_Position>().nextCheckpoint = FindFirstObjectByType<Checkpoint_Manager>()
				.checkpoints[0];
			GetComponent<Car_Position>().currentCheckpoint = 0;

			vController = GetComponent<EasyCarController>();
			//vController.externalController = true;

			#endregion

		}
		void GotoNextPoint()
		{

			// Car positioning system

			if (currentWaypoint < waypoints.Count)
				GetComponent<Car_Position>().nextCheckpoint = waypoints[currentWaypoint];
			else
				GetComponent<Car_Position>().nextCheckpoint = waypoints[0];

			GetComponent<Car_Position>().currentCheckpoint = currentWaypoint;

			GetComponent<Car_Position>().currentLap = currentLap;

		}

		void Update()
		{
			if (!raceStarted)
				return;

			#region Steering

			Vector3 steerVector = transform.InverseTransformPoint(new Vector3(waypoints[currentWaypoint].position.x,
				transform.position.y, waypoints[currentWaypoint].position.z));

			if (followWaypoint)
				newSteer = (steerVector.x / steerVector.magnitude);
			else
				newSteer = 0;

			#endregion

			#region Waypoint

			// Go to the next waypoint based on the remaining distance
			if (steerVector.magnitude <= remainingDistance)
			{

				currentWaypoint++;
				//GotoNextPoint();
			}


			// Reset to the first one if needed
			if (currentWaypoint >= waypoints.Count)
			{
				currentWaypoint = 0;

				// go to the next lap
				//currentLap++;
			}

			#endregion

			#region Inputs
			// Steering input controlled by AI
			if (!isReversing)
				vController.steerInput = Mathf.Lerp(vController.steerInput,
								Mathf.Clamp(newSteer + steerInput_Temp, -1f, 1f), Time.deltaTime * steerSensibility);
			else
				vController.steerInput = Mathf.Lerp(vController.steerInput,
					Mathf.Clamp(newSteer + steerInput_Temp, -1f, 1f), Time.deltaTime * steerSensibility) * -1f;

			// Throttle input controlled by AI
			if (!brakeTrigger)
			{
				if (!isReversing)
				{
					vController.throttleInput = 1f;
				}
				else
				{
					vController.throttleInput = -1f;
				}
			}
			else
				vController.throttleInput = 0;

			#endregion

		}

		void FixedUpdate()
		{
			AI_Sensors();
		}

		void AI_Sensors()
		{

			#region Init
			Vector3 fwd = transform.TransformDirection(new Vector3(0, 0, 1));
			Vector3 pivotPos = new Vector3(rayPosition.localPosition.x, rayPosition.localPosition.y, rayPosition.localPosition.z);
			RaycastHit hit;

			carSpeed = GetComponent<Rigidbody>().linearVelocity.magnitude * 3.6f;
			#endregion

			#region Debug
			// Debug rays
			Debug.DrawRay(rayPosition.position, Quaternion.AngleAxis(cornerSensorAngle, transform.up) * fwd * cornerSensorLength, Color.green);
			Debug.DrawRay(rayPosition.position, Quaternion.AngleAxis(-cornerSensorAngle, transform.up) * fwd * cornerSensorLength, Color.green);

			Debug.DrawRay(rayPosition.position, Quaternion.AngleAxis(centerSensorAngle, transform.up) * fwd * centerSensorLength, Color.green);
			Debug.DrawRay(rayPosition.position, Quaternion.AngleAxis(-centerSensorAngle, transform.up) * fwd * centerSensorLength, Color.green);
			#endregion

			#region Sensors
			// Corner rays
			if (Physics.Raycast(rayPosition.position, Quaternion.AngleAxis(cornerSensorAngle, transform.up) * fwd, out hit, cornerSensorLength, raycastLayers) && !hit.collider.isTrigger && hit.transform.root != transform)
			{
				Debug.DrawRay(rayPosition.position, Quaternion.AngleAxis(cornerSensorAngle, transform.up) * fwd * cornerSensorLength, Color.red);
				steerInput_cornerRight = Mathf.Lerp(-.5f, 0, (hit.distance / cornerSensorLength));
				cornerRight_Hit = true;
			}

			else
			{
				steerInput_cornerRight = 0;
				cornerRight_Hit = false;
			}

			if (Physics.Raycast(rayPosition.position, Quaternion.AngleAxis(-cornerSensorAngle, transform.up) * fwd, out hit, cornerSensorLength, raycastLayers) && !hit.collider.isTrigger && hit.transform.root != transform)
			{
				Debug.DrawRay(rayPosition.position, Quaternion.AngleAxis(-cornerSensorAngle, transform.up) * fwd * cornerSensorLength, Color.red);
				steerInput_cornerLeft = Mathf.Lerp(.5f, 0, (hit.distance / cornerSensorLength));
				cornerLeft_Hit = true;
			}

			else
			{
				steerInput_cornerLeft = 0;
				cornerLeft_Hit = false;
			}

			// Center rays
			if (Physics.Raycast(rayPosition.position, Quaternion.AngleAxis(centerSensorAngle, transform.up) * fwd, out hit, centerSensorLength, raycastLayers) && !hit.collider.isTrigger && hit.transform.root != transform)
			{
				Debug.DrawRay(rayPosition.position, Quaternion.AngleAxis(centerSensorAngle, transform.up) * fwd * centerSensorLength, Color.red);
				steerInput_centerRight = Mathf.Lerp(-1, 0, (hit.distance / centerSensorLength));
				centerRight_Hit = true;
			}

			else
			{
				steerInput_centerRight = 0;
				centerRight_Hit = false;
			}

			if (Physics.Raycast(rayPosition.position, Quaternion.AngleAxis(-centerSensorAngle, transform.up) * fwd, out hit, centerSensorLength, raycastLayers) && !hit.collider.isTrigger && hit.transform.root != transform)
			{
				Debug.DrawRay(rayPosition.position, Quaternion.AngleAxis(-centerSensorAngle, transform.up) * fwd * centerSensorLength, Color.red);
				steerInput_centerLeft = Mathf.Lerp(1, 0, (hit.distance / centerSensorLength));
				centerLeft_Hit = true;
			}

			else
			{
				steerInput_centerLeft = 0;
				centerLeft_Hit = false;
			}
			#endregion

			#region Steering
			if (centerLeft_Hit && centerRight_Hit)
				steerInput_centerRight = 1f;

			if (centerLeft_Hit || centerRight_Hit || cornerRight_Hit || cornerLeft_Hit)
				raysHit = true;
			else
				raysHit = false;

			if (raysHit)
				steerInput_Temp = (steerInput_cornerRight + steerInput_centerLeft + steerInput_centerRight + steerInput_cornerLeft);
			else
				steerInput_Temp = 0;

			if (raysHit && Mathf.Abs(steerInput_Temp) > 0.5f)
				followWaypoint = false;
			else
				followWaypoint = true;
			#endregion

			// Reverse check
			if (canReverseCheck)
			{
				if (carSpeed <= 10f)
				{
					if (centerLeft_Hit || centerRight_Hit ||
						cornerLeft_Hit || cornerRight_Hit)
					{
						if (!checkReversing)
							StartCoroutine(Check_Reversing());
					}
				}
			}


			// Respawn check
			if (canRespawn)
				Respawn_Check();


		}

		IEnumerator Check_Reversing()
		{
			checkReversing = true;

			yield return new WaitForSeconds(1f);

			if (carSpeed <= 10f)
				isReversing = true;

			yield return new WaitForSeconds(3f);

			isReversing = false;

			yield return new WaitForSeconds(2);

			checkReversing = false;
		}
		//_________________________________________________________________________
		bool respawnCheck_1 = false;
		bool respawnCheck_2 = false;
		bool respawnCheck_3 = false;
		bool respawnCheck_4 = false;
		bool respawnCheking = false;

		public void Respawn_Check()
		{
			if (canReverseCheck)
			{
				if (carSpeed <= 7f)
				{
					if (!respawnCheking)
						StartCoroutine(Check_Respawn());
				}
			}
		}

		IEnumerator Check_Respawn()
		{
			respawnCheking = true;

			//_________________________________111
			yield return new WaitForSeconds(Time.timeScale);

			if (carSpeed <= 7f)
				respawnCheck_1 = true;
			else
			{
				respawnCheck_1 = false;
				respawnCheck_2 = false;
				respawnCheck_3 = false;
				respawnCheck_4 = false;

				respawnCheking = false;

				StopCoroutine(Check_Respawn());
			}
			//_________________________________222
			yield return new WaitForSeconds(Time.timeScale);

			if (carSpeed <= 7f)
				respawnCheck_2 = true;
			else
			{
				respawnCheck_1 = false;
				respawnCheck_2 = false;
				respawnCheck_3 = false;
				respawnCheck_4 = false;

				respawnCheking = false;

				StopCoroutine(Check_Respawn());
			}
			//_________________________________333
			yield return new WaitForSeconds(Time.timeScale);

			if (carSpeed <= 7f)
				respawnCheck_3 = true;
			else
			{
				respawnCheck_1 = false;
				respawnCheck_2 = false;
				respawnCheck_3 = false;
				respawnCheck_4 = false;

				respawnCheking = false;

				StopCoroutine(Check_Respawn());
			}
			//_________________________________444
			yield return new WaitForSeconds(Time.timeScale);

			if (carSpeed <= 7f)
				respawnCheck_4 = true;
			else
			{
				respawnCheck_1 = false;
				respawnCheck_2 = false;
				respawnCheck_3 = false;
				respawnCheck_4 = false;

				respawnCheking = false;

				StopCoroutine(Check_Respawn());
			}

			if (respawnCheck_1 && respawnCheck_2
				&& respawnCheck_3 & respawnCheck_4)
			{
				try
				{
					//Respawn now
					transform.position = waypoints[currentWaypoint - 1].position;

					var targetPosition = waypoints[currentWaypoint].position;
					targetPosition.y = transform.position.y;
					transform.LookAt(targetPosition);
				}
				catch { }
				isReversing = false;
				checkReversing = false;

			}

			respawnCheking = false;
			respawnCheck_1 = false;
			respawnCheck_2 = false;
			respawnCheck_3 = false;
			respawnCheck_4 = false;
		}
	}
}