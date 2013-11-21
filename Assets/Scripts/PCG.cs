using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Linq;

public class PCG : MonoBehaviour
{
	public static GameObject enemyPrefab = null, waypointPrefab = null;
	public static List<GameObject> eos = new List<GameObject> (), cos = new List<GameObject> (), pos = new List<GameObject> (), aos = new List<GameObject> ();
	public static int numOfEnemies = 0, numOfCameras = 0, numOfRegionsForEnemies = 0, numOfRegionsForCameras = 0;
	public static int[] maxAreaIndexHolderE = null, maxAreaIndexHolderC = null;
	// VoronoiDiagram instances
	public static VoronoiDiagram vEnemy = new VoronoiDiagram ();
	public static VoronoiDiagram vCamera = new VoronoiDiagram ();

	// Use this for initialization
	void Start ()
	{
	
	}
	
	// Update is called once per frame
	void Update ()
	{
	
	}
	
	public static void Initialize (GameObject enmPrefab, GameObject wpPrefab, Cell[][] obs)
	{
		enemyPrefab = enmPrefab;
		waypointPrefab = wpPrefab;
		
		// Initialize two voronoi diagrams' instances
		vEnemy.obs = new Cell[obs.Length][];
		vCamera.obs = new Cell [obs.Length][];
		for (int x = 0; x < obs.Length; x++) {
			vEnemy.obs [x] = new Cell [obs [x].Length];	
			vCamera.obs [x] = new Cell [obs [x].Length];
		}
		
		// Passing value not reference
		for (int i = 0; i < obs.Length; i++) {
			for (int j = 0; j < obs [i].Length; j++) {
				vEnemy.obs [i] [j] = obs [i] [j].Copy ();
				vCamera.obs [i] [j] = obs [i] [j].Copy ();	
			}
		}
	}
	
	public static void ResetEnemiesObs ()
	{
		for (int i = 0; i < vEnemy.obs.Length; i++) {
			for (int j = 0; j < vEnemy.obs [i].Length; j++) {
				vEnemy.obs [i] [j].nearestVoronoiCentre = -1;
			}
		}	
	}
	
	public static void ResetCamerasObs ()
	{
		for (int i = 0; i < vCamera.obs.Length; i++) {
			for (int j = 0; j < vCamera.obs [i].Length; j++) {
				vCamera.obs [i] [j].nearestVoronoiCentre = -1;
				vCamera.obs [i] [j].isNextToWall = -1;
				vCamera.obs [i] [j].isVoronoiBoundary = -1;
			}
		}	
	}
	
	public static List<GameObject> PopulateEnemies (GameObject floor)
	{
		for (int i = 0; i < numOfRegionsForEnemies; i++) {
			Vector3 position;
			do {
				position = new Vector3 (UnityEngine.Random.Range (floor.collider.bounds.min.x, floor.collider.bounds.max.x), 0.3f, UnityEngine.Random.Range (floor.collider.bounds.min.z, floor.collider.bounds.max.z));
			} while (vEnemy.obs[(int)((position.x - floor.collider.bounds.min.x) / SpaceState.TileSize.x)][(int)((position.z - floor.collider.bounds.min.z) / SpaceState.TileSize.y)].blocked == true);
			GameObject enemy = GameObject.Instantiate (enemyPrefab, position, Quaternion.identity) as GameObject;
			enemy.transform.localScale = new Vector3 (0.4f, 0.4f, 0.4f);
		}
		var intersection = GameObject.FindGameObjectsWithTag ("Enemy").OrderBy (go => go.transform.position.x).ToArray ().Except (cos);
		foreach (GameObject e in intersection) {
			eos.Add (e);
		}
		return eos;
	}
	
	public static List<GameObject> PathInVoronoiRegion (GameObject floor, Cell[][] obs, int iterations)
	{
		for (int m = 0; m < numOfEnemies; m++) {
			//Pick two random nodes in max area and be as large as possible after a certain iteration
			Vector3 randomPos1, randomPos2, dir;
			Vector3 node1 = new Vector3 (0.0f, 0.0f, 0.0f);
			Vector3 node2 = new Vector3 (0.0f, 0.0f, 0.0f);
			float maxDis = float.MinValue;
			float tempDist = 0.0f;
			// This could be another parameter
			for (int iter = 1; iter < iterations + 1; iter++) {
				do {
					do {
						randomPos1 = new Vector3 (UnityEngine.Random.Range (floor.collider.bounds.min.x, floor.collider.bounds.max.x), 0.3f, UnityEngine.Random.Range (floor.collider.bounds.min.z, floor.collider.bounds.max.z));
					} while (obs[(int)((randomPos1.x - floor.collider.bounds.min.x) / SpaceState.TileSize.x)][(int)((randomPos1.z - floor.collider.bounds.min.z) / SpaceState.TileSize.y)].blocked == true ||
						obs[(int)((randomPos1.x - floor.collider.bounds.min.x) / SpaceState.TileSize.x)][(int)((randomPos1.z - floor.collider.bounds.min.z) / SpaceState.TileSize.y)].nearestVoronoiCentre != maxAreaIndexHolderE[m]);
					do {
						randomPos2 = new Vector3 (UnityEngine.Random.Range (floor.collider.bounds.min.x, floor.collider.bounds.max.x), 0.3f, UnityEngine.Random.Range (floor.collider.bounds.min.z, floor.collider.bounds.max.z));	
					} while (obs[(int)((randomPos2.x - floor.collider.bounds.min.x) / SpaceState.TileSize.x)][(int)((randomPos2.z - floor.collider.bounds.min.z) / SpaceState.TileSize.y)].blocked == true ||
						obs[(int)((randomPos2.x - floor.collider.bounds.min.x) / SpaceState.TileSize.x)][(int)((randomPos2.z - floor.collider.bounds.min.z) / SpaceState.TileSize.y)].nearestVoronoiCentre != maxAreaIndexHolderE[m]);
					tempDist = Vector3.Distance (randomPos1, randomPos2);
					dir = new Vector3 (randomPos2.x - randomPos1.x, randomPos2.y - randomPos1.y, randomPos2.z - randomPos1.z);
				} while ((tempDist < maxDis) || (Physics.Raycast(randomPos1, dir, tempDist) == true));
				node1.x = randomPos1.x;
				node1.y = randomPos1.y;
				node1.z = randomPos1.z;
				node2.x = randomPos2.x;
				node2.y = randomPos2.y;
				node2.z = randomPos2.z;
				maxDis = tempDist;
			}
			// Debug.Log ("From: [" + (int)((node1.x - floor.collider.bounds.min.x) / SpaceState.TileSize.x) + "][" + 
			// (int)((node1.z - floor.collider.bounds.min.z) / SpaceState.TileSize.y) + "] To: [" + 
			// (int)((node2.x - floor.collider.bounds.min.x) / SpaceState.TileSize.x) + "][" + 
			// (int)((node2.z - floor.collider.bounds.min.z) / SpaceState.TileSize.y) + "]. ");
				
			// Debug.Log("The longest path is from" + node1 + "to" + node2 + ".");
			// Debug.Log ("The longest path is " + maxDis);
			
			//Move between two nodes
			Vector3 p1 = new Vector3 (node1.x, node1.y, node1.z);
			Vector3 p2 = new Vector3 (node2.x, node2.y, node2.z);
			//Waypoint wp1, wp2;
			GameObject wp1 = GameObject.Instantiate (waypointPrefab, p1, Quaternion.identity) as GameObject;
			GameObject wp2 = GameObject.Instantiate (waypointPrefab, p2, Quaternion.identity) as GameObject;
			Waypoint wpScript1, wpScript2;
			wpScript1 = wp1.GetComponent ("Waypoint") as Waypoint;
			wpScript2 = wp2.GetComponent ("Waypoint") as Waypoint;
			wpScript1.next = wpScript2;
			wpScript2.next = wpScript1;
			
			// Debug.Log ("GameObject[" + maxAreaIndexHolderE [m] + "]");
			//Set the new patrol path
			eos [maxAreaIndexHolderE [m]].GetComponent<Enemy> ().moveSpeed = 1;
			eos [maxAreaIndexHolderE [m]].GetComponent<Enemy> ().rotationSpeed = 30;
			eos [maxAreaIndexHolderE [m]].GetComponent<Enemy> ().target = wpScript2;
			eos [maxAreaIndexHolderE [m]].transform.position = new Vector3 (wp1.transform.position.x, 
				wp1.transform.position.y, wp1.transform.position.z);
		}
		var intersection = GameObject.FindGameObjectsWithTag ("Waypoint").ToArray ().Except (aos);
		foreach (GameObject p in intersection) {
			pos.Add (p);
		}
		return pos;
	}
	
	public static List<GameObject> PopulateCameras (GameObject floor)
	{
		for (int i = 0; i < numOfRegionsForCameras; i++) {
			Vector3 position;
			do {
				position = new Vector3 (UnityEngine.Random.Range (floor.collider.bounds.min.x, floor.collider.bounds.max.x), 0.3f, UnityEngine.Random.Range (floor.collider.bounds.min.z, floor.collider.bounds.max.z));
			} while (PCG.vCamera.obs[(int)((position.x - floor.collider.bounds.min.x) / SpaceState.TileSize.x)][(int)((position.z - floor.collider.bounds.min.z) / SpaceState.TileSize.y)].blocked == true);
			GameObject enemy = GameObject.Instantiate (enemyPrefab, position, Quaternion.identity) as GameObject;
			enemy.transform.localScale = new Vector3 (0.4f, 0.4f, 0.4f);
		}
		var intersection = GameObject.FindGameObjectsWithTag ("Enemy").OrderBy (go => go.transform.position.x).ToArray ().Except (eos);
		foreach (GameObject c in intersection) {
			cos.Add (c);
		}
		return cos;
	}
	
	public static List<GameObject> RotationInVoronoiRegion (GameObject floor, Cell[][] obs, int iterations)
	{
		foreach (GameObject cameraObject in cos) {
			if (cameraObject != null && cameraObject.renderer.enabled) {
				float maxSumOfDistance = 0.0f;
				int maxIndex = 0, maxIndex2 = 0;
				Vector3 finalPos = new Vector3 ();
				
				List<Vector3> lookingDirVec = new List<Vector3> ();
				lookingDirVec.Add (new Vector3 (0.0f, 0.0f, 1.0f));
				lookingDirVec.Add (new Vector3 (1.0f, 0.0f, 1.0f));
				lookingDirVec.Add (new Vector3 (1.0f, 0.0f, 0.0f));
				lookingDirVec.Add (new Vector3 (1.0f, 0.0f, -1.0f));
				lookingDirVec.Add (new Vector3 (0.0f, 0.0f, -1.0f));
				lookingDirVec.Add (new Vector3 (-1.0f, 0.0f, -1.0f));
				lookingDirVec.Add (new Vector3 (-1.0f, 0.0f, 0.0f));
				lookingDirVec.Add (new Vector3 (-1.0f, 0.0f, 1.0f));
				
				Enemy rcScript;
				rcScript = cameraObject.GetComponent ("Enemy") as Enemy;
				
				for (int iter = 1; iter < iterations + 1; iter++) {
					// Bind to walls
					// ...
					Vector3 position = new Vector3 (cameraObject.transform.position.x, cameraObject.transform.position.y, cameraObject.transform.position.z);
					Vector3 defaultPos;
					int centreBelongTo = obs [(int)((position.x - floor.collider.bounds.min.x) / SpaceState.TileSize.x)] [(int)((position.z - floor.collider.bounds.min.z) / SpaceState.TileSize.y)].nearestVoronoiCentre;
					int random = UnityEngine.Random.Range (0, vCamera.boundaryArray [centreBelongTo].Count - 1);
					do {
						random = UnityEngine.Random.Range (0, vCamera.boundaryArray [centreBelongTo].Count - 1);
						defaultPos = new Vector3 (vCamera.boundaryXArray [centreBelongTo].ElementAt (random) * SpaceState.TileSize.x + floor.collider.bounds.min.x, 
								position.y, vCamera.boundaryZArray [centreBelongTo].ElementAt (random) * SpaceState.TileSize.y + floor.collider.bounds.min.z);
					} while (vCamera.boundaryArray [centreBelongTo].ElementAt (random).isNextToWall != 1);
					
					// Find aiming position
					// rangeArray[0] = minX, rangeArray[1] = minY, rangeArray[2] = maxX, rangeArray[3] = maxY;
					float[] rangeArray = new float[4];
					
					List<float> distanceList = new List<float> ();
					
					// Foreach direction in 8 directions
					foreach (Vector3 vdir in lookingDirVec) {
						// Eliminate the possibility that a stationary guard could see the goal postion or starting postion
						rangeArray = PCG.CalculateRange (vdir, defaultPos, rcScript.fovDistance, rcScript.fovAngle);
						if (((GameObject.FindGameObjectWithTag ("End").transform.position.x < rangeArray [0] || GameObject.FindGameObjectWithTag ("End").transform.position.x > rangeArray [2]
							|| GameObject.FindGameObjectWithTag ("End").transform.position.y < rangeArray [1] || GameObject.FindGameObjectWithTag ("End").transform.position.y > rangeArray [3])
							&& (GameObject.FindGameObjectWithTag ("Start").transform.position.x < rangeArray [0] || GameObject.FindGameObjectWithTag ("Start").transform.position.x > rangeArray [2]
							|| GameObject.FindGameObjectWithTag ("Start").transform.position.y < rangeArray [1] || GameObject.FindGameObjectWithTag ("Start").transform.position.y > rangeArray [3]))
							|| (Physics.Raycast (defaultPos, new Vector3 (GameObject.FindGameObjectWithTag ("End").transform.position.x - defaultPos.x, 0.0f, GameObject.FindGameObjectWithTag ("End").transform.position.z - defaultPos.z), rcScript.fovDistance)
							|| Physics.Raycast (defaultPos, new Vector3 (GameObject.FindGameObjectWithTag ("Start").transform.position.x - defaultPos.x, 0.0f, GameObject.FindGameObjectWithTag ("Start").transform.position.z - defaultPos.z), rcScript.fovDistance))) {
							RaycastHit hit;
							float solution = 0.0f;
							if (Physics.Raycast (defaultPos, vdir, out hit)) {
								// Debug.Log ("(" + vdir.x + "," + vdir.z + ")");
								// Debug.Log (hit.distance);
								solution  = rcScript.fovDistance < hit.distance ? rcScript.fovDistance : hit.distance;
							} else {
								if (vdir.x == 0.0f && vdir.z == 1.0f) {
									// Debug.Log ("(" + vdir.x + "," + vdir.z + ")");
									// Debug.Log (defaultPos.z - floor.collider.bounds.min.z);
									solution = rcScript.fovDistance < defaultPos.z - floor.collider.bounds.min.z ? rcScript.fovDistance : defaultPos.z - floor.collider.bounds.min.z;
								} else if (vdir.x == 1.0f && vdir.z == 1.0f) {
									float a1 = Mathf.Sqrt (2.0f * Mathf.Pow (defaultPos.z - floor.collider.bounds.min.z, 2.0f));
									float a2 = Mathf.Sqrt (2.0f * Mathf.Pow (floor.collider.bounds.max.x - defaultPos.x, 2.0f));
									solution = a1 > a2 ? a2 : a1;
									solution = rcScript.fovDistance < solution ? rcScript.fovDistance : solution;
									// Debug.Log ("(" + vdir.x + "," + vdir.z + ")");
									// Debug.Log (solution);
								} else if (vdir.x == 1.0f && vdir.z == 0.0f) {
									// Debug.Log ("(" + vdir.x + "," + vdir.z + ")");
									// Debug.Log (floor.collider.bounds.max.x - defaultPos.x);
									solution = rcScript.fovDistance < floor.collider.bounds.max.x - defaultPos.x ? rcScript.fovDistance : floor.collider.bounds.max.x - defaultPos.x;
								} else if (vdir.x == 1.0f && vdir.z == -1.0f) {
									float a1 = Mathf.Sqrt (2.0f * Mathf.Pow (floor.collider.bounds.max.x - defaultPos.x, 2.0f));
									float a2 = Mathf.Sqrt (2.0f * Mathf.Pow (floor.collider.bounds.max.z - defaultPos.z, 2.0f));
									solution = a1 > a2 ? a2 : a1;
									solution = rcScript.fovDistance < solution ? rcScript.fovDistance : solution;
									// Debug.Log ("(" + vdir.x + "," + vdir.z + ")");
									// Debug.Log (solution);
								} else if (vdir.x == 0.0f && vdir.z == -1.0f) {
									// Debug.Log ("(" + vdir.x + "," + vdir.z + ")");
									// Debug.Log (floor.collider.bounds.max.z - defaultPos.z);
									solution = rcScript.fovDistance < floor.collider.bounds.max.z - defaultPos.z ? rcScript.fovDistance : floor.collider.bounds.max.z - defaultPos.z;
								} else if (vdir.x == -1.0f && vdir.z == -1.0f) {
									float a1 = Mathf.Sqrt (2.0f * Mathf.Pow (defaultPos.x - floor.collider.bounds.min.x, 2.0f));
									float a2 = Mathf.Sqrt (2.0f * Mathf.Pow (floor.collider.bounds.max.z - defaultPos.z, 2.0f));
									solution = a1 > a2 ? a2 : a1;
									solution = rcScript.fovDistance < solution ? rcScript.fovDistance : solution;
									// Debug.Log ("(" + vdir.x + "," + vdir.z + ")");
									// Debug.Log (solution);
								} else if (vdir.x == -1.0f && vdir.z == 0.0f) {
									// Debug.Log ("(" + vdir.x + "," + vdir.z + ")");
									// Debug.Log (defaultPos.x - floor.collider.bounds.min.x);
									solution = rcScript.fovDistance < defaultPos.x - floor.collider.bounds.min.x ? rcScript.fovDistance : defaultPos.x - floor.collider.bounds.min.x;
								} else if (vdir.x == -1.0f && vdir.z == 1.0f) {
									float a1 = Mathf.Sqrt (2.0f * Mathf.Pow (defaultPos.x - floor.collider.bounds.min.x, 2.0f));
									float a2 = Mathf.Sqrt (2.0f * Mathf.Pow (defaultPos.z - floor.collider.bounds.min.z, 2.0f));
									solution = a1 > a2 ? a2 : a1;
									solution = rcScript.fovDistance < solution ? rcScript.fovDistance : solution;
									// Debug.Log ("(" + vdir.x + "," + vdir.z + ")");
									// Debug.Log (solution);
								}
							}
							distanceList.Add (solution);
						} else {
							distanceList.Add (-1.0f);
						}
					}
					
					int cnt = 0;
					int maxIndexTemp = 0;
					float maxDistance = 0.0f;
					foreach (float tempDistance in distanceList) {
						if (tempDistance > maxDistance) {
							maxDistance = tempDistance;
							maxIndexTemp = cnt;
						}
						cnt++;
					}
					
					int cnt2 = 0;
					int maxIndexTemp2 = 0;
					float maxDistance2 = 0.0f;
					foreach (float tempDistance in distanceList) {
						if (tempDistance > maxDistance2 && cnt2 != maxIndexTemp) {
							maxDistance2 = tempDistance;
							maxIndexTemp2 = cnt2;
						}
						cnt2++;
					}
					
					float tempSumOfDistance = maxDistance + maxDistance2;
					if (tempSumOfDistance > maxSumOfDistance) {
						maxSumOfDistance = tempSumOfDistance;
						maxIndex = maxIndexTemp;
						maxIndex2 = maxIndexTemp2;
						finalPos = new Vector3 (defaultPos.x, defaultPos.y, defaultPos.z);
					}	
				}
				
				// Bring it to the new position
				cameraObject.transform.position = finalPos;
				// Create rotation waypoint aimed at
				Vector3 rwpPos1 = new Vector3 (finalPos.x + lookingDirVec.ElementAt (maxIndex).x * rcScript.fovDistance / Mathf.Sqrt (lookingDirVec.ElementAt (maxIndex).x * lookingDirVec.ElementAt (maxIndex).x + lookingDirVec.ElementAt (maxIndex).z * lookingDirVec.ElementAt (maxIndex).z), 0.0f,
					finalPos.z + lookingDirVec.ElementAt (maxIndex).z * rcScript.fovDistance / Mathf.Sqrt (lookingDirVec.ElementAt (maxIndex).x * lookingDirVec.ElementAt (maxIndex).x + lookingDirVec.ElementAt (maxIndex).z * lookingDirVec.ElementAt (maxIndex).z));
				Vector3 rwpPos2 = new Vector3 (finalPos.x + lookingDirVec.ElementAt (maxIndex2).x * rcScript.fovDistance / Mathf.Sqrt (lookingDirVec.ElementAt (maxIndex2).x * lookingDirVec.ElementAt (maxIndex2).x + lookingDirVec.ElementAt (maxIndex2).z * lookingDirVec.ElementAt (maxIndex2).z), 0.0f,
					finalPos.z + lookingDirVec.ElementAt (maxIndex2).z * rcScript.fovDistance / Mathf.Sqrt (lookingDirVec.ElementAt (maxIndex2).x * lookingDirVec.ElementAt (maxIndex2).x + lookingDirVec.ElementAt (maxIndex2).z * lookingDirVec.ElementAt (maxIndex2).z));
				if (CheckAcute (finalPos, lookingDirVec.ElementAt (maxIndex), lookingDirVec.ElementAt (maxIndex2))) {
					GameObject rwp1 = GameObject.Instantiate (waypointPrefab, rwpPos1, Quaternion.identity) as GameObject;
					GameObject rwp2 = GameObject.Instantiate (waypointPrefab, rwpPos2, Quaternion.identity) as GameObject;
					rwp1.AddComponent ("RotationWaypoint");
					rwp2.AddComponent ("RotationWaypoint");
					DestroyImmediate (rwp1.GetComponent ("Waypoint"));
					DestroyImmediate (rwp2.GetComponent ("Waypoint"));
					RotationWaypoint rwpScript1;
					RotationWaypoint rwpScript2;
					rwpScript1 = rwp1.GetComponent ("Waypoint") as RotationWaypoint;
					rwpScript2 = rwp2.GetComponent ("Waypoint") as RotationWaypoint;
					rwpScript1.next = rwpScript2;
					rwpScript2.next = rwpScript1;
					rwpScript1.lookDir = new Vector3 (lookingDirVec.ElementAt (maxIndex).x, lookingDirVec.ElementAt (maxIndex).y, lookingDirVec.ElementAt (maxIndex).z);
					rwpScript2.lookDir = new Vector3 (lookingDirVec.ElementAt (maxIndex2).x, lookingDirVec.ElementAt (maxIndex2).y, lookingDirVec.ElementAt (maxIndex2).z);
					rcScript.target = rwpScript1;
					rcScript.transform.LookAt (rwpPos1);
				} else {
					Vector3 rwpPos3 = new Vector3 (0.0f, 0.0f, 0.0f);
					if (CheckFlat(finalPos, lookingDirVec.ElementAt (maxIndex), lookingDirVec.ElementAt (maxIndex2), ref rwpPos3)) {
						Vector3 imVector = new Vector3(rwpPos3.x, rwpPos3.y, rwpPos3.z);
						rwpPos3 = new Vector3 (finalPos.x + rwpPos3.x * rcScript.fovDistance / Mathf.Sqrt (rwpPos3.x * rwpPos3.x + rwpPos3.z * rwpPos3.z), finalPos.z + rwpPos3.z * rcScript.fovDistance / Mathf.Sqrt (Mathf.Sqrt (rwpPos3.x * rwpPos3.x + rwpPos3.z * rwpPos3.z)));
						GameObject rwp1 = GameObject.Instantiate (waypointPrefab, rwpPos1, Quaternion.identity) as GameObject;
						GameObject rwp2 = GameObject.Instantiate (waypointPrefab, rwpPos2, Quaternion.identity) as GameObject;
						GameObject rwp3 = GameObject.Instantiate (waypointPrefab, rwpPos3, Quaternion.identity) as GameObject;
						GameObject rwp4 = GameObject.Instantiate (waypointPrefab, rwpPos3, Quaternion.identity) as GameObject;
						rwp1.AddComponent ("RotationWaypoint");
						rwp2.AddComponent ("RotationWaypoint");
						rwp3.AddComponent ("RotationWaypoint");
						rwp4.AddComponent ("RotationWaypoint");
						DestroyImmediate (rwp1.GetComponent ("Waypoint"));
						DestroyImmediate (rwp2.GetComponent ("Waypoint"));
						DestroyImmediate (rwp3.GetComponent ("Waypoint"));
						DestroyImmediate (rwp4.GetComponent ("Waypoint"));
						RotationWaypoint rwpScript1;
						RotationWaypoint rwpScript2;
						RotationWaypoint rwpScript3;
						RotationWaypoint rwpScript4;
						rwpScript1 = rwp1.GetComponent ("Waypoint") as RotationWaypoint;
						rwpScript2 = rwp2.GetComponent ("Waypoint") as RotationWaypoint;
						rwpScript3 = rwp3.GetComponent ("Waypoint") as RotationWaypoint;
						rwpScript4 = rwp4.GetComponent ("Waypoint") as RotationWaypoint;
						rwpScript1.next = rwpScript3;
						rwpScript3.next = rwpScript2;
						rwpScript2.next = rwpScript4;
						rwpScript4.next = rwpScript1;
						rwpScript1.lookDir = new Vector3 (lookingDirVec.ElementAt (maxIndex).x, lookingDirVec.ElementAt (maxIndex).y, lookingDirVec.ElementAt (maxIndex).z);
						rwpScript2.lookDir = new Vector3 (lookingDirVec.ElementAt (maxIndex2).x, lookingDirVec.ElementAt (maxIndex2).y, lookingDirVec.ElementAt (maxIndex2).z);
						rwpScript3.lookDir = new Vector3 (imVector.x, imVector.y, imVector.z);
						rwpScript4.lookDir = new Vector3 (imVector.x, imVector.y, imVector.z);
						rcScript.target = rwpScript1;
						rcScript.transform.LookAt (rwpPos1);
					} else {
						rwpPos3 = new Vector3 (finalPos.x + (- lookingDirVec.ElementAt (maxIndex).x - lookingDirVec.ElementAt (maxIndex2).x) * rcScript.fovDistance / Mathf.Sqrt ((lookingDirVec.ElementAt (maxIndex).x + lookingDirVec.ElementAt (maxIndex2).x) * (lookingDirVec.ElementAt (maxIndex).x + lookingDirVec.ElementAt (maxIndex2).x) + (lookingDirVec.ElementAt (maxIndex).z + lookingDirVec.ElementAt (maxIndex2).z) * (lookingDirVec.ElementAt (maxIndex).z + lookingDirVec.ElementAt (maxIndex2).z)), 0.0f,
						finalPos.z + (- lookingDirVec.ElementAt (maxIndex).z - lookingDirVec.ElementAt (maxIndex2).z) * rcScript.fovDistance / Mathf.Sqrt ((lookingDirVec.ElementAt (maxIndex).x + lookingDirVec.ElementAt (maxIndex2).x) * (lookingDirVec.ElementAt (maxIndex).x + lookingDirVec.ElementAt (maxIndex2).x) + (lookingDirVec.ElementAt (maxIndex).z + lookingDirVec.ElementAt (maxIndex2).z) * (lookingDirVec.ElementAt (maxIndex).z + lookingDirVec.ElementAt (maxIndex2).z)));
						GameObject rwp1 = GameObject.Instantiate (waypointPrefab, rwpPos1, Quaternion.identity) as GameObject;
						GameObject rwp2 = GameObject.Instantiate (waypointPrefab, rwpPos2, Quaternion.identity) as GameObject;
						GameObject rwp3 = GameObject.Instantiate (waypointPrefab, rwpPos3, Quaternion.identity) as GameObject;
						GameObject rwp4 = GameObject.Instantiate (waypointPrefab, rwpPos3, Quaternion.identity) as GameObject;
						rwp1.AddComponent ("RotationWaypoint");
						rwp2.AddComponent ("RotationWaypoint");
						rwp3.AddComponent ("RotationWaypoint");
						rwp4.AddComponent ("RotationWaypoint");
						DestroyImmediate (rwp1.GetComponent ("Waypoint"));
						DestroyImmediate (rwp2.GetComponent ("Waypoint"));
						DestroyImmediate (rwp3.GetComponent ("Waypoint"));
						DestroyImmediate (rwp4.GetComponent ("Waypoint"));
						RotationWaypoint rwpScript1;
						RotationWaypoint rwpScript2;
						RotationWaypoint rwpScript3;
						RotationWaypoint rwpScript4;
						rwpScript1 = rwp1.GetComponent ("Waypoint") as RotationWaypoint;
						rwpScript2 = rwp2.GetComponent ("Waypoint") as RotationWaypoint;
						rwpScript3 = rwp3.GetComponent ("Waypoint") as RotationWaypoint;
						rwpScript4 = rwp4.GetComponent ("Waypoint") as RotationWaypoint;
						rwpScript1.next = rwpScript3;
						rwpScript3.next = rwpScript2;
						rwpScript2.next = rwpScript4;
						rwpScript4.next = rwpScript1;
						rwpScript1.lookDir = new Vector3 (lookingDirVec.ElementAt (maxIndex).x, lookingDirVec.ElementAt (maxIndex).y, lookingDirVec.ElementAt (maxIndex).z);
						rwpScript2.lookDir = new Vector3 (lookingDirVec.ElementAt (maxIndex2).x, lookingDirVec.ElementAt (maxIndex2).y, lookingDirVec.ElementAt (maxIndex2).z);
						rwpScript3.lookDir = new Vector3 (- lookingDirVec.ElementAt (maxIndex).x - lookingDirVec.ElementAt (maxIndex2).x,
							- lookingDirVec.ElementAt (maxIndex).y - lookingDirVec.ElementAt (maxIndex2).y, - lookingDirVec.ElementAt (maxIndex).z - lookingDirVec.ElementAt (maxIndex2).z);
						rwpScript4.lookDir = new Vector3 (- lookingDirVec.ElementAt (maxIndex).x - lookingDirVec.ElementAt (maxIndex2).x,
							- lookingDirVec.ElementAt (maxIndex).y - lookingDirVec.ElementAt (maxIndex2).y, - lookingDirVec.ElementAt (maxIndex).z - lookingDirVec.ElementAt (maxIndex2).z);
						rcScript.target = rwpScript1;
						rcScript.transform.LookAt (rwpPos1);
					}
				}
			}
		}
		var intersection = GameObject.FindGameObjectsWithTag ("Waypoint").ToArray ().Except (pos);
		foreach (GameObject a in intersection) {
			aos.Add (a);
		}
		return aos;
	}
	
	public static void DestroyVoronoiCentreForEnemies ()
	{
		//Destroy other voronoi centres and complete the scene
		foreach (GameObject e in eos) {
			for (int n = 0; n < numOfEnemies; n++) {
				if (e == eos [maxAreaIndexHolderE [n]]) {
					break;
				}
				if (n == numOfEnemies - 1) {
					GameObject.DestroyImmediate (e);		
				}
			}
		}
	}
	
	public static void DestroyVoronoiCentreForCameras ()
	{
		//Destroy other voronoi centres and complete the scene
		foreach (GameObject c in cos) {
			for (int n = 0; n < numOfCameras; n++) {
				if (c == cos [maxAreaIndexHolderC [n]]) {
					break;
				}
				if (n == numOfCameras - 1) {
					GameObject.DestroyImmediate (c);		
				}
			}
		}
	}
	
	public static void ClearUpEnemies (GameObject[] eos)
	{
		if (eos != null) {
			foreach (GameObject e in eos) {
				DestroyImmediate (e);	
			}
		}
		PCG.eos.Clear ();
	}
	
	public static void ClearUpPaths (GameObject[] pos)
	{
		if (pos != null) {
			foreach (GameObject p in pos) {
				DestroyImmediate (p);	
			}
		}
		PCG.pos.Clear ();
	}
	
	public static void ClearUpCameras (GameObject[] cos)
	{
		if (cos != null) {
			foreach (GameObject c in cos) {
				DestroyImmediate (c);	
			}
		}
		PCG.cos.Clear ();
	}
	
	public static void ClearUpAngles (GameObject[] aos)
	{
		if (aos != null) {
			foreach (GameObject a in aos) {
				DestroyImmediate (a);	
			}
		}
		PCG.aos.Clear ();
	}
	
	// Calculate range
	private static float[] CalculateRange (Vector3 vdir, Vector3 defaultPos, float fovDistance, float fovAngle)
	{
		float[] range = new float[4];
		float minX, minY, maxX, maxY;
		float cosTheta, sinTheta, x, y, xA, yA, xB, yB;
		Vector2 pA, pB, pC, dir;
		pC = new Vector2 (defaultPos.x, defaultPos.z);
		dir = new Vector2 (vdir.x, vdir.z);
		
		cosTheta = dir.x / Mathf.Sqrt (dir.x * dir.x + dir.y * dir.y);
		sinTheta = dir.y / Mathf.Sqrt (dir.x * dir.x + dir.y * dir.y);
		x = fovDistance;
		y = fovDistance * Mathf.Tan ((fovAngle / 2) / 180.0f * Mathf.PI);
		xA = cosTheta * x - sinTheta * y + pC.x;
		yA = sinTheta * x + cosTheta * y + pC.y;
		xB = cosTheta * x + sinTheta * y + pC.x;
		yB = sinTheta * x - cosTheta * y + pC.y;
		pA = new Vector2 (xA, yA);
		pB = new Vector2 (xB, yB);
		
		// min and max
		maxX = Mathf.Max (Mathf.Max (pA.x, pB.x), pC.x);
		minX = Mathf.Min (Mathf.Min (pA.x, pB.x), pC.x);
		maxY = Mathf.Max (Mathf.Max (pA.y, pB.y), pC.y);
		minY = Mathf.Min (Mathf.Min (pA.y, pB.y), pC.y);
		
		range [0] = minX;
		range [1] = minY;
		range [2] = maxX;
		range [3] = maxY;
		
		return range;
	}
	
	private static bool CheckAcute (Vector3 pos, Vector3 v1, Vector3 v2)
	{
		Vector3 midDir1 = new Vector3 (v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
		Vector3 midDir2 = new Vector3 (-midDir1.x, -midDir1.y, -midDir1.z);
		float d1 = float.MaxValue, d2 = float.MaxValue;
		RaycastHit hit1, hit2;
		if (Physics.Raycast (pos, midDir1, out hit1)) {
			d1 = hit1.distance;
		}
		if (Physics.Raycast (pos, midDir2, out hit2)) {
			d2 = hit2.distance;	
		}
		if (d1 >= d2 && !midDir1.Equals(midDir2)) {
			return true;
		} else {
			return false;	
		}
	}
	
	private static bool CheckFlat (Vector3 pos, Vector3 v1, Vector3 v2, ref Vector3 rwpPos3) {
		Vector3 midDir1 = new Vector3 (v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
		Vector3 midDir2 = new Vector3 (-midDir1.x, -midDir1.y, -midDir1.z);
		if (midDir1 == midDir2) {
			midDir1 = new Vector3 (v1.z, 0.0f, v1.x);
			midDir2 = new Vector3 (v2.z, 0.0f, v2.x);
			if (CheckFlatDir (pos, midDir1, midDir2)) {
				rwpPos3 = new Vector3 (midDir1.x, midDir1.y, midDir1.z);
			} else {
				rwpPos3 = new Vector3 (midDir2.x, midDir2.y, midDir2.z);
			}
			return true;
		} else {
			return false;	
		}
	}
					
	private static bool CheckFlatDir (Vector3 pos, Vector3 mv1, Vector3 mv2) {
		float d1 = float.MaxValue, d2 = float.MaxValue;
		RaycastHit hit1, hit2;			
		if (Physics.Raycast (pos, mv1, out hit1)) {
			d1 = hit1.distance;
		}
		if (Physics.Raycast (pos, mv2, out hit2)) {
			d2 = hit2.distance;	
		}
		if (d1 >= d2) {	
			return true;
		} else {
			return false;	
		}
		
	}
}