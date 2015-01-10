using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;
using System.Linq;
using Common;
using Exploration;
using Path = Common.Path;
using Extra;
using Objects;
using Learning;

namespace EditorArea {
	public class MapperWindowEditor : EditorWindow {

		// Data holders
		private Cell[][][] fullMap, original;
		private static int[][][][][][] Q; 
		private List<List<Node>> learnedResult = new List<List<Node>>();


		public static List<Path> paths = new List<Path> (), deaths = new List<Path>();

		// Parameters with default values
		public static int timeSamples = 1000, episodes = 3600, iterations4Learning = 50, attemps = 25000, iterations = 1, gridSize = 60, ticksBehind = 0;
		private static bool drawMap = true, drawNeverSeen = false, drawHeatMap = false, drawHeatMap3d = false, drawDeathHeatMap = false, drawDeathHeatMap3d = false, drawCombatHeatMap = false, drawPath = true, smoothPath = false, drawFoVOnly = false, drawCombatLines = false, simulateCombat = false, learnedData = true, allBranches = true, drawByTimeSlice = true;
		private static float stepSize = 1 / 10f, crazySeconds = 5f, playerDPS = 10, GAMMA_DiscountFactor = 0.8f, ALPHA_LearningRate = 0.5f, Epsilon_for_E_Greedy = 0.5f ;
		private static int randomSeed = -1;

		// Computed parameters
		private static int[,] heatMap, deathHeatMap, combatHeatMap;
		private static int[][,] heatMap3d, deathHeatMap3d;
		private static GameObject start = null, end = null, floor = null, playerPrefab = null;
		private static Dictionary<Path, bool> toggleStatus = new Dictionary<Path, bool> ();
		private static Dictionary<Path, GameObject> players = new Dictionary<Path, GameObject> ();
		private static int startX, startY, endX, endY, maxHeatMap, timeSlice, imported = 0;
		private static bool seeByTime, seeByLength, seeByDanger, seeByLoS, seeByDanger3, seeByLoS3, seeByDanger3Norm, seeByLoS3Norm, seeByCrazy, seeByVelocity;
		private static List<Path> arrangedByTime, arrangedByLength, arrangedByDanger, arrangedByLoS, arrangedByDanger3, arrangedByLoS3, arrangedByDanger3Norm, arrangedByLoS3Norm, arrangedByCrazy, arrangedByVelocity;

		// Helping stuff
		private static Vector2 scrollPos = new Vector2 ();
		private static GameObject playerNode;
		private List<Tuple<Vector3, string>> textDraw = new List<Tuple<Vector3, string>>();
		private int lastTime = timeSlice;
		private long stepInTicks = 0L, playTime = 0L;
		private static bool simulated = false, playing = false;
		private Mapper mapper;
		//private RRTKDTreeCombat rrt = new RRTKDTreeCombat ();
	//	private RRTKDTree rrt = new RRTKDTree ();
		private RRTKDTreeConnect rrt = new RRTKDTreeConnect ();

		private RRTKDTree oldRrt = new RRTKDTree ();
		private MapperEditorDrawer drawer;
		private DateTime previous = DateTime.Now;
		private long accL = 0L;
		
		[MenuItem("Window/Mapper")]
		static void Init () {
			MapperWindowEditor window = (MapperWindowEditor)EditorWindow.GetWindow (typeof(MapperWindowEditor));
			window.title = "Mapper";
			window.ShowTab ();
		}
		
		void OnGUI () {
			#region Pre-Init
			
			// Wait for the floor to be set and initialize the drawer and the mapper
			if (floor != null) {
				if (floor.collider == null) {
					Debug.LogWarning ("Floor has no valid collider, game object ignored.");
					floor = null;
				} else {
					drawer = floor.gameObject.GetComponent<MapperEditorDrawer> ();
					if (drawer == null) {
						drawer = floor.gameObject.AddComponent<MapperEditorDrawer> ();
						drawer.hideFlags = HideFlags.HideInInspector;
					}
				}
				if (mapper == null) {
					mapper = floor.GetComponent<Mapper> ();

					if (mapper == null) 
						mapper = floor.AddComponent<Mapper> ();
					
					mapper.hideFlags = HideFlags.None;
				}
			} 
			
			#endregion
			
			// ----------------------------------
			
			scrollPos = EditorGUILayout.BeginScrollView (scrollPos);
			
			#region 1. Map
			
			EditorGUILayout.LabelField ("1. Map");
			playerPrefab = (GameObject)EditorGUILayout.ObjectField ("Player Prefab", playerPrefab, typeof(GameObject), false);
			
			floor = (GameObject)EditorGUILayout.ObjectField ("Floor", floor, typeof(GameObject), true);
			gridSize = EditorGUILayout.IntSlider ("Grid size", gridSize, 10, 300);

			if (GUILayout.Button ((MapperEditor.editGrid ? "Finish Editing" : "Edit Grid"))) {
				if (floor != null) {
					MapperEditor.editGrid = !MapperEditor.editGrid;
					Selection.activeGameObject = mapper.gameObject;
				}
			}
	
			EditorGUILayout.LabelField ("");
			
			#endregion
			
			// ----------------------------------
			
			#region 2. Units
			
			EditorGUILayout.LabelField ("2. Units");

			playerDPS = EditorGUILayout.Slider("Player DPS", playerDPS, 0.1f, 100f);
			if (GUILayout.Button ("Store Positions")) {
				StorePositions ();
			}
			EditorGUILayout.LabelField ("");
			
			#endregion
			
			// ----------------------------------
			
			#region 3. Map Computation
			
			EditorGUILayout.LabelField ("3. Map Computation");
			timeSamples = EditorGUILayout.IntSlider ("Time samples", timeSamples, 1, 10000);
			stepSize = EditorGUILayout.Slider ("Step size", stepSize, 0.01f, 1f);
			stepInTicks = ((long)(stepSize * 10000000L));
			ticksBehind = EditorGUILayout.IntSlider (new GUIContent ("Ticks behind", "Number of ticks that the FoV will remain seen after the enemy has no visibility on that cell (prevents noise/jitter like behaviours)"), ticksBehind, 0, 100);
			
			if (GUILayout.Button ("Precompute Maps")) {
				
				//Find this is the view
				if (playerPrefab == null) {
					playerPrefab = GameObject.Find ("Player"); 
				}
				
				if (floor == null) {
					floor = (GameObject)GameObject.Find ("Floor");
					
					if (mapper == null) {
						mapper = floor.GetComponent<Mapper> ();
						
						if (mapper == null)
							mapper = floor.AddComponent<Mapper> ();

						mapper.hideFlags = HideFlags.None;
					
					}
					drawer = floor.gameObject.GetComponent<MapperEditorDrawer> ();
					if (drawer == null) {
						drawer = floor.gameObject.AddComponent<MapperEditorDrawer> ();
						drawer.hideFlags = HideFlags.HideInInspector;
					}
				}
				
				if (!simulated) {
					StorePositions ();
					simulated = true;
				}
				
				Cell[][] baseMap = null;
				if (MapperEditor.grid != null) {
					Cell[][] obstacles = mapper.ComputeObstacles ();
					baseMap = new Cell[gridSize][];
					for (int x = 0; x < gridSize; x++) {
						baseMap [x] = new Cell[gridSize];
						for (int y = 0; y < gridSize; y++) {
							baseMap [x] [y] = MapperEditor.grid [x] [y] == null ? obstacles [x] [y] : MapperEditor.grid [x] [y];
						}
					}
				}
				
				original = mapper.PrecomputeMaps (SpaceState.Editor, floor.collider.bounds.min, floor.collider.bounds.max, gridSize, gridSize, timeSamples, stepSize, ticksBehind, baseMap);

				drawer.fullMap = original;
				float maxSeenGrid;
				drawer.seenNeverSeen = Analyzer.ComputeSeenValuesGrid (original, out maxSeenGrid);
				drawer.seenNeverSeenMax = maxSeenGrid;
				drawer.tileSize = SpaceState.Editor.tileSize;
				drawer.zero.Set (floor.collider.bounds.min.x, floor.collider.bounds.min.z);
				
				ResetAI ();
				previous = DateTime.Now;
				
				Debug.Log(  System.GC.GetTotalMemory(true) );
			} 
			EditorGUILayout.LabelField ("");
			
			#endregion

			// ----------------------------------
			#region 3.5 Trianing
			EditorGUILayout.LabelField ("3.5 Training");
			
			start = (GameObject)EditorGUILayout.ObjectField ("Start", start, typeof(GameObject), true);
			end = (GameObject)EditorGUILayout.ObjectField ("End", end, typeof(GameObject), true);

			GAMMA_DiscountFactor = EditorGUILayout.Slider ("Discount Factor", GAMMA_DiscountFactor, 0.01f, 1f);
			ALPHA_LearningRate = EditorGUILayout.Slider ("Learning Rate", ALPHA_LearningRate, 0.01f, 1f);
			Epsilon_for_E_Greedy = EditorGUILayout.Slider ("Epsilon for E-Greedy", Epsilon_for_E_Greedy, 0.01f, 1f);;
			episodes = EditorGUILayout.IntSlider ("Episodes", episodes, 1000, 10 * gridSize * gridSize );
			iterations4Learning = EditorGUILayout.IntSlider ("Iterations", iterations4Learning, 1, 1500);
			

			if (GUILayout.Button ("Try Reinforcement Learning")) {
			
				//Check the start and the end and get them from the editor. 
				if (start == null) {
					start = GameObject.Find ("Start");
				}
				if (end == null) {
					end = GameObject.Find ("End");	
				}

				startX = (int)((start.transform.position.x - floor.collider.bounds.min.x) / SpaceState.Editor.tileSize.x);
				startY = (int)((start.transform.position.z - floor.collider.bounds.min.z) / SpaceState.Editor.tileSize.y);
				endX = (int)((end.transform.position.x - floor.collider.bounds.min.x) / SpaceState.Editor.tileSize.x);
				endY = (int)((end.transform.position.z - floor.collider.bounds.min.z) / SpaceState.Editor.tileSize.y);

				//Debug.Log( "startX : " + startX + " startY " + startY );

				System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
				sw.Start();

				int actionNumber = 0, nextX = 0, nextY = 0, currX, currY, t;
				float maxQ = 0;
				int reward, distance;
				Boolean stop = false;
//				float GAMMA_DiscountFactor = 0.8f, TEMPERATURE, ALPHA_LearningRate = 0.5f;

				//Boolean decision = false;
				//int []directionChange = new int[5] { -1, 1, 0, 0, 0};
				//int yi;
				Cell currCell = null, nextCell = null;
//				Node currNode = null, nextNode = null;

				//initialize Q values, if out of the map, the lowest value
				for( int w = 0; w < original.Length; w++ ){
					for( int ww = 0; ww < gridSize; ww++ ){
						original[w][0][ww].qValues[ original[0][0][0].LEFT ] = float.MinValue;
						original[w][ww][0].qValues[ original[0][0][0].DOWNWARD ] = float.MinValue;
						original[w][gridSize-1][ww].qValues[ original[0][0][0].RIGHT ] = float.MinValue;
						original[w][ww][gridSize-1].qValues[ original[0][0][0].UPWARD ] = float.MinValue;						
					}					   
				}

//				Cell[][] testCells = original[0];
//				learnedResult.Clear(); // a space for containing result paths
				List<Node> lastNodes = new List<Node>();
				for( int iteration = 0; iteration < iterations4Learning; iteration++ ){
				
//					for( int zz = 0; zz < original[0][0][0].qValues.Length; zz++ )
//						Debug.Log ( iteration + " " + zz + " : " + original[100][54][24].qValues[zz] );
					for( int episode = 0; episode < episodes; episode++ ){
	
						currX = startX;
						currY = startY; // set the initial state 
						//currY = episode % (gridSize - 1);
						//currX = episode / (gridSize - 1); // visit all states
						t = 0; // initialize time
						
						currCell = original[t][currX][currY];
						//							currCell = testCells[currX][currY];
//						currNode = new Node();
//						currNode.x = currX;
//						currNode.y = currY;
//						currNode.t = t;
//						currNode.cell = currCell;
//						currNode.parent = null;
						stop = false;
						do{
	
							// get an action according to my policy, E-greedy
//							List<int> actions = currCell.Select1ActionOnEGreedy( currX, currY, Epsilon_for_E_Greedy );
							List<int> actions = currCell.Select1ActionOnGreedy( currX, currY );
							
							nextX = actions[0];
							nextY = actions[1];
							actionNumber = actions[2];
							
							if( !(nextX >= 0 && nextX <= gridSize - 1 && nextY >= 0 && nextY <= gridSize - 1 ) ){
								reward = -200;
								maxQ = float.MinValue;
								stop = true;
							}
							else{
							//	Debug.Log ( "x : " + nextX + " y : " + nextY + " t : " + t );
								nextCell = original[t+1][nextX][nextY];
								//							nextCell = testCells[nextX][nextY];
//								nextNode = new Node();
//								nextNode.t = t+1;
//								nextNode.x = nextX;
//								nextNode.y = nextY;
//								nextNode.cell = nextCell;
//								nextNode.parent = currNode;
								
								// get the reward value for Reinforcement
								if( nextX == endX && nextY == endY ) // reched the goal
									reward = 10000;
								else if( nextCell.blocked ){ //collision
									reward = -50;
									stop = true;
								}
								else if( nextCell.seen ){ // collision
									reward = -50;
									stop = true;
								}
								else //simple movement
									reward = -1;
								maxQ = nextCell.getMaxQ();						
							}
							// update the Q-Function
							if( actionNumber == currCell.LEFT ) //left
								currCell.qValues[currCell.LEFT] += ALPHA_LearningRate * ( reward + GAMMA_DiscountFactor * maxQ - currCell.qValues[currCell.LEFT] ); 
							else if( actionNumber == currCell.RIGHT ) //right
								currCell.qValues[currCell.RIGHT] += ALPHA_LearningRate * ( reward + GAMMA_DiscountFactor * maxQ - currCell.qValues[currCell.RIGHT ] ); 
							else if( actionNumber == currCell.DOWNWARD ) //downward
								currCell.qValues[currCell.DOWNWARD] += ALPHA_LearningRate * ( reward + GAMMA_DiscountFactor * maxQ - currCell.qValues[currCell.DOWNWARD ] );
							else if( actionNumber == currCell.UPWARD ) //upward
								currCell.qValues[currCell.UPWARD] += ALPHA_LearningRate * ( reward + GAMMA_DiscountFactor * maxQ - currCell.qValues[currCell.UPWARD ] ); 
							else if( actionNumber == currCell.HERE )
								currCell.qValues[currCell.HERE] +=  ALPHA_LearningRate * ( reward + GAMMA_DiscountFactor * maxQ - currCell.qValues[currCell.HERE ] ); 
	
							if( stop ){ //collision -> the next episode
								//lastNodes.Add ( nextNode );
								break;
							}
	
							t++;
							currX = nextX;
							currY = nextY;
							currCell = nextCell;
//							currNode = nextNode;
						}while( t+1 <= original.Length-1 );
//						if( episode % 5 == 0 )
//							lastNodes.Add( currNode ); // 모든 경우의 학습된 경로를 뽑기
						
					}//episode
				}//iteration

				sw.Stop();
				Debug.Log ( "Elapsed time for RL : " + System.Math.Truncate( (double)sw.ElapsedMilliseconds/60000 ) + "min " 
				           + System.Math.Truncate( ( (double)sw.ElapsedMilliseconds % 60000 )/1000 ) + "sec" );
				
				sw.Reset();
				sw.Start();
				//make a path learned above
				currX = startX;
				currY = startY;
				Node currNode = new Node();
				currNode.x = currX;
				currNode.y = currY;
				currNode.t = 0;
				currNode.cell = original[0][currX][currY];
//				currNode.cell = testCells[currX][currY];
				System.Collections.Queue nodeQueue = new System.Collections.Queue();
				nodeQueue.Enqueue( currNode );
				//Node nextNode = null;
				learnedResult.Clear(); // a space for containing result paths
				int time = 0;
				List<int> nextXYList;

				while( true ){
				//while( nodeQueue.Count != 0 ){
					currNode = (Node)nodeQueue.Dequeue();
					
					// time over or one that has not been to near ones
					if( currNode.t >= original.Length - 1 || currNode.cell.isFirstVisit() ){
						lastNodes.Add( currNode );
						break;
					}
					
					nextXYList = currNode.cell.Select1ActionOnGreedy( currNode.x, currNode.y );
					if( nextXYList == null ){ // the last node
						lastNodes.Add( currNode );
						//continue;
						break;
					}
					for( int k = 0; k < nextXYList.Count/2; k++ ){
						Node nextNode = new Node();
						nextNode.x = nextXYList[k*2]; 
						nextNode.y = nextXYList[k*2+1];
						nextNode.t = currNode.t + 1;
						//Debug.Log( "t: " + nextNode.t + " x: " + nextNode.x + " y: " + nextNode.y );
						nextNode.cell = original[nextNode.t][nextNode.x][nextNode.y];
//						nextNode.cell = testCells[nextNode.x][nextNode.y];
						nextNode.parent = currNode;
						nodeQueue.Enqueue( nextNode ); 
					}//k
				} //while
				
				foreach( Node n in lastNodes )
					learnedResult.Add( rrt.ReturnPath( n, false ) ); // 모든 경우의 학습된 경로를 뽑기

				sw.Stop();
				Debug.Log ( "Elapsed time for making paths : " + System.Math.Truncate( (double)sw.ElapsedMilliseconds/60000 ) + "min " 
				           + System.Math.Truncate( ( (double)sw.ElapsedMilliseconds % 60000 )/1000 ) + "sec" );
			}

			#endregion

			// ----------------------------------
			
			#region 4. Path
			
			EditorGUILayout.LabelField ("4. Path");
			
			//start = (GameObject)EditorGUILayout.ObjectField ("Start", start, typeof(GameObject), true);
			//end = (GameObject)EditorGUILayout.ObjectField ("End", end, typeof(GameObject), true);
			attemps = EditorGUILayout.IntSlider ("Attempts", attemps, 1000, 100000);
			iterations = EditorGUILayout.IntSlider ("Iterations", iterations, 1, 1500);
			randomSeed = EditorGUILayout.IntSlider("Random Seed", randomSeed, -1, 10000);
			smoothPath = EditorGUILayout.Toggle ("Smooth path", smoothPath);
			simulateCombat = EditorGUILayout.Toggle ("Simulate combat", simulateCombat);

			if (GUILayout.Button ("(WIP) Compute 3D A* Path")) {
				float playerSpeed = GameObject.FindGameObjectWithTag ("AI").GetComponent<Player> ().speed;
				
				//Check the start and the end and get them from the editor. 
				if (start == null) {
					start = GameObject.Find ("Start");
				}
				if (end == null) {
					end = GameObject.Find ("End");	
				}

				startX = (int)((start.transform.position.x - floor.collider.bounds.min.x) / SpaceState.Editor.tileSize.x);
				startY = (int)((start.transform.position.z - floor.collider.bounds.min.z) / SpaceState.Editor.tileSize.y);
				endX = (int)((end.transform.position.x - floor.collider.bounds.min.x) / SpaceState.Editor.tileSize.x);
				endY = (int)((end.transform.position.z - floor.collider.bounds.min.z) / SpaceState.Editor.tileSize.y);

				paths.Clear ();
				deaths.Clear ();
				ClearPathsRepresentation ();
				arrangedByCrazy = arrangedByDanger = arrangedByDanger3 = arrangedByDanger3Norm = arrangedByLength = arrangedByLoS = arrangedByLoS3 = arrangedByLoS3Norm = arrangedByTime = arrangedByVelocity = null;

				//Exploration.DavAStar3d astar3d = new DavAStar3d();
				//List<Node> nodes = astar3d.Compute(startX, startY, endX, endY, original, playerSpeed);

				Exploration.AStarTY astarTY = new AStarTY();
				List<Node> nodes = astarTY.Compute(startX, startY, endX, endY, original, playerSpeed);
				
				if (nodes.Count > 0) {
					paths.Add (new Path (nodes));
					toggleStatus.Add (paths.Last (), true);
					paths.Last ().color = new Color (UnityEngine.Random.Range (0.0f, 1.0f), UnityEngine.Random.Range (0.0f, 1.0f), UnityEngine.Random.Range (0.0f, 1.0f));
				}
			}

			allBranches = EditorGUILayout.Toggle ("all branches", allBranches);
			learnedData = EditorGUILayout.Toggle ("show learned paths", learnedData);


			if (GUILayout.Button ("Compute Path")) {
				float playerSpeed = GameObject.FindGameObjectWithTag ("AI").GetComponent<Player> ().speed;
				float playerMaxHp = GameObject.FindGameObjectWithTag ("AI").GetComponent<Player> ().maxHp;
				
				//Check the start and the end and get them from the editor. 
				if (start == null) {
					start = GameObject.Find ("Start");
				}
				if (end == null) {
					end = GameObject.Find ("End");	
				}
				
				paths.Clear ();
				deaths.Clear ();
				ClearPathsRepresentation ();
				arrangedByCrazy = arrangedByDanger = arrangedByDanger3 = arrangedByDanger3Norm = arrangedByLength = arrangedByLoS = arrangedByLoS3 = arrangedByLoS3Norm = arrangedByTime = arrangedByVelocity = null;

				// Prepare start and end position
				startX = (int)((start.transform.position.x - floor.collider.bounds.min.x) / SpaceState.Editor.tileSize.x);
				startY = (int)((start.transform.position.z - floor.collider.bounds.min.z) / SpaceState.Editor.tileSize.y);
				endX = (int)((end.transform.position.x - floor.collider.bounds.min.x) / SpaceState.Editor.tileSize.x);
				endY = (int)((end.transform.position.z - floor.collider.bounds.min.z) / SpaceState.Editor.tileSize.y);

				GameObject[] hps = GameObject.FindGameObjectsWithTag("HealthPack");
				HealthPack[] packs = new HealthPack[hps.Length];
				for (int i = 0; i < hps.Length; i++) {
					packs[i] = GameObject.Find("Visit"+i).GetComponent<HealthPack>(); //태영 
					//packs[i] = hps[i].GetComponent<HealthPack>();
					packs[i].posX = (int)((packs[i].transform.position.x - floor.collider.bounds.min.x) / SpaceState.Editor.tileSize.x);
					packs[i].posZ = (int)((packs[i].transform.position.z - floor.collider.bounds.min.z) / SpaceState.Editor.tileSize.y);
				}

				// Update the parameters on the RRT class
				rrt.min = floor.collider.bounds.min;
				rrt.tileSizeX = SpaceState.Editor.tileSize.x;
				rrt.tileSizeZ = SpaceState.Editor.tileSize.y;
				rrt.enemies = SpaceState.Editor.enemies;
				rrt.packs = packs;
			//	rrt.simulateCombat = simulateCombat;

				int seed = randomSeed;
				if (randomSeed != -1)
					UnityEngine.Random.seed = randomSeed;
				else {
					DateTime now = DateTime.Now;
					seed = now.Millisecond + now.Second + now.Minute + now.Hour + now.Day + now.Month+ now.Year;
					UnityEngine.Random.seed = seed;
				}

				//List<Node> nodes = null;

				System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
				sw.Start();

				for (int it = 0; it < iterations; it++) {

					// Make a copy of the original map
					fullMap = new Cell[original.Length][][];
					for (int t = 0; t < original.Length; t++) {
						fullMap[t] = new Cell[original[0].Length][];
						for (int x = 0; x < original[0].Length; x++) {
							fullMap[t][x] = new Cell[original[0][0].Length];
							for (int y = 0; y < original[0][0].Length; y++)
								fullMap[t][x][y] = original[t][x][y].Copy();
						}
					}
					
					// Use the copied map so the RRT can modify it
					foreach (Enemy e in SpaceState.Editor.enemies) {
						for (int t = 0; t < original.Length; t++)
							for (int x = 0; x < original[0].Length; x++)
								for (int y = 0; y < original[0][0].Length; y++)
									if (e.seenCells[t][x][y] != null)
										e.seenCells[t][x][y] = fullMap[t][x][y];

						// TODO: Need to make a backup of the enemies positions, rotations and forwards
						
					}
					// We have this try/catch block here to account for the issue that we don't solve when we find a path when t is near the limit
					try {
						List<List<Node>> computedPaths;
						if( learnedData )
							computedPaths = learnedResult;

						else
							computedPaths = rrt.Compute (startX, startY, endX, endY, attemps, playerSpeed, fullMap, smoothPath, allBranches );

							
						// Did we found a path?
						if (computedPaths.Count > 0) {
							Color c =	new Color (UnityEngine.Random.Range (0.0f, 1.0f), UnityEngine.Random.Range (0.0f, 1.0f), UnityEngine.Random.Range (0.0f, 1.0f));
							foreach( List<Node> nodes in computedPaths ){
								paths.Add (new Path (nodes));
								toggleStatus.Add (paths.Last (), true);
								paths.Last ().color = c;
							}
	
						}
						// Grab the death list
					//	foreach (List<Node> deathNodes in rrt.deathPaths) {
					//		deaths.Add(new Path(deathNodes));
					//	}
					} catch (Exception e) {
						Debug.LogWarning("Skip errored calculated path");
						Debug.LogException(e);
						// This can happen in two different cases:
						// In line 376 by having a duplicate node being picked (coincidence picking the EndNode as visiting but the check is later on)
						// We also cant just bring the check earlier since there's data to be copied (really really rare cases)
						// The other case is yet unkown, but it's a conicidence by trying to insert a node in the tree that already exists (really rare cases)
					}
				}

				sw.Stop();
				Debug.Log ( "Elapsed time : " + System.Math.Truncate( (double)sw.ElapsedMilliseconds/60000 ) + "min " 
				           + System.Math.Truncate( ( (double)sw.ElapsedMilliseconds % 60000 )/1000 ) + "sec" );

				// Set the map to be drawn
				drawer.fullMap = fullMap;
				ComputeHeatMap (paths, deaths);

				// Compute the summary about the paths and print it
				String summary = "Summary:\n";
				summary += "Seed used:" + seed;
				summary += "\nSuccessful paths found: " + paths.Count;
				summary += "\nDead paths: " + deaths.Count;

				// How many paths killed how many enemies
				Dictionary<int, int> map = new Dictionary<int, int>();
				for (int i = 0; i <= SpaceState.Editor.enemies.Length; i++)
					map.Add(i, 0);
				foreach (Path p in paths) {
					int killed = 0;
					foreach (Node n in p.points)
						if (n.died != null)
							killed++;

					if (map.ContainsKey(killed))
						map[killed]++;
					else
						map.Add(killed, 1);
				}

				foreach (int k in map.Keys) {
					summary += "\n" + map[k] + " paths killed " + k + " enemies";
				}

				// Debug.Log(summary);
			}

			//태영 코드 
			EditorGUILayout.LabelField ("set by Ty");
			if (GUILayout.Button ("Export Coordinates X,Y,T")) {
				List<Path> all = new List<Path>();
				all.AddRange(paths);
				all.AddRange(deaths);
				if( SpaceState.Editor.enemies.Length > 0 ){
				PathBulk.SaveXYT2File ("ms" + SpaceState.Editor.enemies[0].moveSpeed 
				                       + "rs" + SpaceState.Editor.enemies[0].rotationSpeed 
				                       + "fd" + SpaceState.Editor.enemies[0].fovDistance + "_xyt.txt", all); // 플레이어 이동 좌표들 출력!
				}
				else
					PathBulk.SaveXYT2File ("_xyt.txt", all); // 플레이어 이동 좌표들 출력!

			}
			EditorGUILayout.LabelField ("");
			/////
			
			if (GUILayout.Button ("(DEBUG) Export Paths")) {
				List<Path> all = new List<Path>();
				all.AddRange(paths);
				all.AddRange(deaths);
				PathBulk.SavePathsToFile ("pathtest.xml", all);
			}
			
			if (GUILayout.Button ("(DEBUG) Import Paths")) {
				paths.Clear ();
				ClearPathsRepresentation ();
				List<Path> pathsImported = PathBulk.LoadPathsFromFile ("pathtest.xml");
				foreach (Path p in pathsImported) {
					if (p.points.Last().playerhp <= 0) {
						deaths.Add(p);
					} else {
						p.name = "Imported " + (++imported);
						p.color = new Color (UnityEngine.Random.Range (0.0f, 1.0f), UnityEngine.Random.Range (0.0f, 1.0f), UnityEngine.Random.Range (0.0f, 1.0f));
						toggleStatus.Add (p, true);
					}
				}
				ComputeHeatMap (paths, deaths);
				SetupArrangedPaths (paths);
			}
			
			EditorGUILayout.LabelField ("");
			
			#endregion
			
			// ----------------------------------
			
			#region 5. Visualization
			
			EditorGUILayout.LabelField ("5. Visualization");
			
			timeSlice = EditorGUILayout.IntSlider ("Time", timeSlice, 0, timeSamples - 1);
			drawMap = EditorGUILayout.Toggle ("Draw map", drawMap);
			drawNeverSeen = EditorGUILayout.Toggle ("- Draw safe places", drawNeverSeen);
			drawFoVOnly = EditorGUILayout.Toggle ("- Draw only fields of view", drawFoVOnly);
			drawHeatMap = EditorGUILayout.Toggle ("- Draw heat map", drawHeatMap);
			drawCombatHeatMap = EditorGUILayout.Toggle ("-> Draw combat heat map", drawCombatHeatMap);
			drawHeatMap3d = EditorGUILayout.Toggle ("-> Draw heat map 3d", drawHeatMap3d);
			drawDeathHeatMap = EditorGUILayout.Toggle ("-> Draw death heat map", drawDeathHeatMap);
			drawDeathHeatMap3d = EditorGUILayout.Toggle ("--> Draw 3d death heat map", drawDeathHeatMap3d);
			drawPath = EditorGUILayout.Toggle ("Draw path", drawPath);
			drawCombatLines = EditorGUILayout.Toggle ("Draw combat lines", drawCombatLines);
			drawByTimeSlice = EditorGUILayout.Toggle ("Draw by Time Slice", drawByTimeSlice);


			if (drawer != null) {
				drawer.heatMap = null;
				drawer.heatMap3d = null;
				drawer.deathHeatMap = null;
				drawer.deathHeatMap3d = null;
				drawer.combatHeatMap = null;

				if (drawHeatMap) {
					if (drawCombatHeatMap)
						drawer.combatHeatMap = combatHeatMap;

					else if (drawHeatMap3d)
						drawer.heatMap3d = heatMap3d;

					else if (drawDeathHeatMap) {
						if (drawDeathHeatMap3d)
							drawer.deathHeatMap3d = deathHeatMap3d;
						else
							drawer.deathHeatMap = deathHeatMap;
					}

					else
						drawer.heatMap = heatMap;
				}

				drawer.drawByTimeSlice = drawByTimeSlice;
			}
			
			EditorGUILayout.LabelField ("");
			
			if (GUILayout.Button (playing ? "Stop" : "Play")) {
				playing = !playing;
			}
			
			EditorGUILayout.LabelField ("");
			
			/*if (GUILayout.Button ("Batch computation")) {
				BatchComputing ();
			}*/
			
			#endregion
			
			// ----------------------------------
			
			#region 6. Paths
			
			EditorGUILayout.LabelField ("6. Paths");
			
			EditorGUILayout.LabelField ("");
			
			crazySeconds = EditorGUILayout.Slider ("Crazy seconds window", crazySeconds, 0f, 10f);
			
			if (GUILayout.Button ("Analyze paths")) {		
				ClearPathsRepresentation ();
				
				// Setup paths names
				int i = 1;
				foreach (Path path in paths) {
					path.name = "Path " + (i++);
					path.color = new Color (UnityEngine.Random.Range (0.0f, 1.0f), UnityEngine.Random.Range (0.0f, 1.0f), UnityEngine.Random.Range (0.0f, 1.0f));
					toggleStatus.Add (path, true);
					path.ZeroValues ();
				}
				
				// Analyze paths
				Analyzer.PreparePaths (paths);
				//Analyzer.ComputePathsTimeValues (paths);
				//Analyzer.ComputePathsLengthValues (paths);
				//Analyzer.ComputePathsVelocityValues (paths);
				//Analyzer.ComputePathsLoSValues (paths, SpaceState.Editor.enemies, floor.collider.bounds.min, SpaceState.Editor.tileSize.x, SpaceState.Editor.tileSize.y, original, drawer.seenNeverSeen, drawer.seenNeverSeenMax);
				Analyzer.ComputePathsDangerValues (paths, SpaceState.Editor.enemies, floor.collider.bounds.min, SpaceState.Editor.tileSize.x, SpaceState.Editor.tileSize.y, original, drawer.seenNeverSeen, drawer.seenNeverSeenMax);
				//Analyzer.ComputeCrazyness (paths, original, Mathf.FloorToInt (crazySeconds / stepSize));
				//Analyzer.ComputePathsVelocityValues (paths);
				
				SetupArrangedPaths (paths);
			}
			
			if (GUILayout.Button ("(DEBUG) Export Analysis")) {
				XmlSerializer ser = new XmlSerializer (typeof(MetricsRoot), new Type[] {
					typeof(PathResults),
					typeof(PathValue),
					typeof(Value)
				});
				
				MetricsRoot root = new MetricsRoot ();
				
				foreach (Path path in paths) {
					root.everything.Add (new PathResults (path, Analyzer.pathMap [path]));
				}
				using (FileStream stream = new FileStream ("pathresults.xml", FileMode.Create)) {
					ser.Serialize (stream, root);
					stream.Flush ();
					stream.Close ();
				}
			}
			
			if (GUILayout.Button ("(DEBUG) Compute clusters")) {
				ComputeClusters ();
			}
			
			#region Paths values display
			
			seeByTime = EditorGUILayout.Foldout (seeByTime, "Paths by Time");
			if (seeByTime && arrangedByTime != null) {
				for (int i = 0; i < arrangedByTime.Count; i++) {
					EditorGUILayout.BeginHorizontal ();
	
					EditorGUILayout.FloatField (arrangedByTime [i].name, arrangedByTime [i].time);
					toggleStatus [arrangedByTime [i]] = EditorGUILayout.Toggle ("", toggleStatus [arrangedByTime [i]], GUILayout.MaxWidth (20f));
					EditorGUILayout.ColorField (arrangedByTime [i].color, GUILayout.MaxWidth (40f));
					
					EditorGUILayout.EndHorizontal ();
				}
			}
			
			seeByLength = EditorGUILayout.Foldout (seeByLength, "Paths by Length");
			if (seeByLength && arrangedByLength != null) {
				for (int i = 0; i < arrangedByLength.Count; i++) {
					EditorGUILayout.BeginHorizontal ();
	
					EditorGUILayout.FloatField (arrangedByLength [i].name, arrangedByLength [i].length2d);
					toggleStatus [arrangedByLength [i]] = EditorGUILayout.Toggle ("", toggleStatus [arrangedByLength [i]], GUILayout.MaxWidth (20f));
					EditorGUILayout.ColorField (arrangedByLength [i].color, GUILayout.MaxWidth (40f));
					
					EditorGUILayout.EndHorizontal ();
				}
			}
			
			seeByDanger = EditorGUILayout.Foldout (seeByDanger, "Paths by Danger (A*)");
			if (seeByDanger && arrangedByDanger != null) {
				for (int i = 0; i < arrangedByDanger.Count; i++) {
					EditorGUILayout.BeginHorizontal ();
	
					EditorGUILayout.FloatField (arrangedByDanger [i].name, arrangedByDanger [i].danger);
					toggleStatus [arrangedByDanger [i]] = EditorGUILayout.Toggle ("", toggleStatus [arrangedByDanger [i]], GUILayout.MaxWidth (20f));
					EditorGUILayout.ColorField (arrangedByDanger [i].color, GUILayout.MaxWidth (40f));
					
					EditorGUILayout.EndHorizontal ();
				}
			}
			
			seeByDanger3 = EditorGUILayout.Foldout (seeByDanger3, "Paths by Danger 3 (A*)");
			if (seeByDanger3 && arrangedByDanger3 != null) {
				for (int i = 0; i < arrangedByDanger3.Count; i++) {
					EditorGUILayout.BeginHorizontal ();
	
					EditorGUILayout.FloatField (arrangedByDanger3 [i].name, arrangedByDanger3 [i].danger3);
					toggleStatus [arrangedByDanger3 [i]] = EditorGUILayout.Toggle ("", toggleStatus [arrangedByDanger3 [i]], GUILayout.MaxWidth (20f));
					EditorGUILayout.ColorField (arrangedByDanger3 [i].color, GUILayout.MaxWidth (40f));
					
					EditorGUILayout.EndHorizontal ();
				}
			}
			
			seeByDanger3Norm = EditorGUILayout.Foldout (seeByDanger3Norm, "Paths by Danger 3 (A*) (normalized)");
			if (seeByDanger3Norm && arrangedByDanger3Norm != null) {
				for (int i = 0; i < arrangedByDanger3Norm.Count; i++) {
					EditorGUILayout.BeginHorizontal ();
	
					EditorGUILayout.FloatField (arrangedByDanger3Norm [i].name, arrangedByDanger3Norm [i].danger3Norm);
					toggleStatus [arrangedByDanger3Norm [i]] = EditorGUILayout.Toggle ("", toggleStatus [arrangedByDanger3Norm [i]], GUILayout.MaxWidth (20f));
					EditorGUILayout.ColorField (arrangedByDanger3Norm [i].color, GUILayout.MaxWidth (40f));
					
					EditorGUILayout.EndHorizontal ();
				}
			}
			
			seeByLoS = EditorGUILayout.Foldout (seeByLoS, "Paths by Line of Sight (FoV)");
			if (seeByLoS && arrangedByLoS != null) {
				for (int i = 0; i < arrangedByLoS.Count; i++) {
					EditorGUILayout.BeginHorizontal ();
	
					EditorGUILayout.FloatField (arrangedByLoS [i].name, arrangedByLoS [i].los);
					toggleStatus [arrangedByLoS [i]] = EditorGUILayout.Toggle ("", toggleStatus [arrangedByLoS [i]], GUILayout.MaxWidth (20f));
					EditorGUILayout.ColorField (arrangedByLoS [i].color, GUILayout.MaxWidth (40f));
					
					EditorGUILayout.EndHorizontal ();
				}
			}
			
			seeByLoS3 = EditorGUILayout.Foldout (seeByLoS3, "Paths by Line of Sight 3 (FoV)");
			if (seeByLoS3 && arrangedByLoS3 != null) {
				for (int i = 0; i < arrangedByLoS3.Count; i++) {
					EditorGUILayout.BeginHorizontal ();
	
					EditorGUILayout.FloatField (arrangedByLoS3 [i].name, arrangedByLoS3 [i].los3);
					toggleStatus [arrangedByLoS3 [i]] = EditorGUILayout.Toggle ("", toggleStatus [arrangedByLoS3 [i]], GUILayout.MaxWidth (20f));
					EditorGUILayout.ColorField (arrangedByLoS3 [i].color, GUILayout.MaxWidth (40f));
					
					EditorGUILayout.EndHorizontal ();
				}
			}
			
			seeByLoS3Norm = EditorGUILayout.Foldout (seeByLoS3Norm, "Paths by Line of Sight 3 (FoV) (normalized)");
			if (seeByLoS3Norm && arrangedByLoS3Norm != null) {
				for (int i = 0; i < arrangedByLoS3Norm.Count; i++) {
					EditorGUILayout.BeginHorizontal ();
	
					EditorGUILayout.FloatField (arrangedByLoS3Norm [i].name, arrangedByLoS3Norm [i].los3Norm);
					toggleStatus [arrangedByLoS3Norm [i]] = EditorGUILayout.Toggle ("", toggleStatus [arrangedByLoS3Norm [i]], GUILayout.MaxWidth (20f));
					EditorGUILayout.ColorField (arrangedByLoS3Norm [i].color, GUILayout.MaxWidth (40f));
					
					EditorGUILayout.EndHorizontal ();
				}
			}
			
			seeByCrazy = EditorGUILayout.Foldout (seeByCrazy, "Paths by Crazyness");
			if (seeByCrazy && arrangedByCrazy != null) {
				for (int i = 0; i < arrangedByCrazy.Count; i++) {
					EditorGUILayout.BeginHorizontal ();
	
					EditorGUILayout.FloatField (arrangedByCrazy [i].name, arrangedByCrazy [i].crazy);
					toggleStatus [arrangedByCrazy [i]] = EditorGUILayout.Toggle ("", toggleStatus [arrangedByCrazy [i]], GUILayout.MaxWidth (20f));
					EditorGUILayout.ColorField (arrangedByCrazy [i].color, GUILayout.MaxWidth (40f));
					
					EditorGUILayout.EndHorizontal ();
				}
			}
			
			seeByVelocity = EditorGUILayout.Foldout (seeByVelocity, "Paths by Velocity Changes");
			if (seeByVelocity && arrangedByVelocity != null) {
				for (int i = 0; i < arrangedByVelocity.Count; i++) {
					EditorGUILayout.BeginHorizontal ();
	
					EditorGUILayout.FloatField (arrangedByVelocity [i].name, arrangedByVelocity [i].velocity);
					toggleStatus [arrangedByVelocity [i]] = EditorGUILayout.Toggle ("", toggleStatus [arrangedByVelocity [i]], GUILayout.MaxWidth (20f));
					EditorGUILayout.ColorField (arrangedByVelocity [i].color, GUILayout.MaxWidth (40f));
					
					EditorGUILayout.EndHorizontal ();
				}
			}
			
			#endregion
			
			
			#endregion
			
			// ----------------------------------
			
			#region Temp Player setup
			
			if (playerNode == null) {
				playerNode = GameObject.Find ("TempPlayerNode");
				if (playerNode == null) {
					playerNode = new GameObject ("TempPlayerNode");
					playerNode.hideFlags = HideFlags.HideAndDontSave;
				}
			}
			if (playerPrefab != null) {
				foreach (KeyValuePair<Path, bool> p in toggleStatus) {
					if (p.Value) {
						if (!players.ContainsKey (p.Key)) {
							GameObject player = GameObject.Instantiate (playerPrefab) as GameObject;
							player.transform.position.Set (p.Key.points [0].x, 0f, p.Key.points [0].y);
							player.transform.parent = playerNode.transform;
							players.Add (p.Key, player);
							Material m = new Material (player.renderer.sharedMaterial);
							m.color = p.Key.color;
							player.renderer.material = m;
							player.hideFlags = HideFlags.HideAndDontSave;
						} else {
							players [p.Key].SetActive (true);
						}
					} else {
						if (players.ContainsKey (p.Key)) {
							players [p.Key].SetActive (false);
						}
					}
				}
			}
			
			#endregion
			
			EditorGUILayout.EndScrollView ();
			
			// ----------------------------------
			
			if (drawer != null) {
				drawer.timeSlice = timeSlice;
				drawer.drawHeatMap = drawHeatMap;
				drawer.drawMap = drawMap;
				drawer.drawFoVOnly = drawFoVOnly;
				drawer.drawNeverSeen = drawNeverSeen;
				drawer.drawPath = drawPath;
				drawer.drawCombatLines = drawCombatLines;
				drawer.paths = toggleStatus;
				drawer.textDraw = textDraw;
				
			}
			
			if (original != null && lastTime != timeSlice) {
				lastTime = timeSlice;
				UpdatePositions (timeSlice, mapper);
			}
			
			SceneView.RepaintAll ();

		}
			
		public void Update () {
			textDraw.Clear();
			if (playing) {
				long l = DateTime.Now.Ticks - previous.Ticks;
				playTime += l;
				accL += l;
				if (playTime > stepInTicks) {
					playTime -= stepInTicks;
					timeSlice++;
					if (timeSlice >= timeSamples) {
						timeSlice = 0;
					}
					drawer.timeSlice = timeSlice;
					SpaceState.Editor.timeSlice = timeSlice;
					UpdatePositions (timeSlice, mapper, 0f);
					accL += playTime;
				} else {
					UpdatePositions (timeSlice, mapper, (float)accL / (float)stepInTicks);
					accL = 0L;
				}
			}
				
			previous = DateTime.Now;
		}
		
		private void ClearPathsRepresentation () {
			toggleStatus.Clear ();
			
			foreach (GameObject obj in players.Values)
				GameObject.DestroyImmediate (obj);
				
			players.Clear ();

			GameObject.DestroyImmediate(GameObject.Find("TempPlayerNode"));

			Resources.UnloadUnusedAssets ();
		}
		
		private void SetupArrangedPaths (List<Path> paths) {
			arrangedByTime = new List<Path> ();
			arrangedByTime.AddRange (paths);
			arrangedByTime.Sort (new Analyzer.TimeComparer ());
				
			arrangedByLength = new List<Path> ();
			arrangedByLength.AddRange (paths);
			arrangedByLength.Sort (new Analyzer.Length2dComparer ());
				
			arrangedByDanger = new List<Path> ();
			arrangedByDanger.AddRange (paths);
			arrangedByDanger.Sort (new Analyzer.DangerComparer ());
				
			arrangedByDanger3 = new List<Path> ();
			arrangedByDanger3.AddRange (paths);
			arrangedByDanger3.Sort (new Analyzer.Danger3Comparer ());
				
			arrangedByDanger3Norm = new List<Path> ();
			arrangedByDanger3Norm.AddRange (paths);
			arrangedByDanger3Norm.Sort (new Analyzer.Danger3NormComparer ());
				
			arrangedByLoS = new List<Path> ();
			arrangedByLoS.AddRange (paths);
			arrangedByLoS.Sort (new Analyzer.LoSComparer ());
				
			arrangedByLoS3 = new List<Path> ();
			arrangedByLoS3.AddRange (paths);
			arrangedByLoS3.Sort (new Analyzer.LoS3Comparer ());
				
			arrangedByLoS3Norm = new List<Path> ();
			arrangedByLoS3Norm.AddRange (paths);
			arrangedByLoS3Norm.Sort (new Analyzer.LoS3NormComparer ());
				
			arrangedByCrazy = new List<Path> ();
			arrangedByCrazy.AddRange (paths);
			arrangedByCrazy.Sort (new Analyzer.CrazyComparer ());
				
			arrangedByVelocity = new List<Path> ();
			arrangedByVelocity.AddRange (paths);
			arrangedByVelocity.Sort (new Analyzer.VelocityComparer ());
		}
		
		private void ComputeHeatMap (List<Path> paths, List<Path> deaths = null) {
			heatMap = Analyzer.Compute2DHeatMap (paths, gridSize, gridSize, out maxHeatMap);
			drawer.heatMapMax = maxHeatMap;
			drawer.heatMap = heatMap;

			deathHeatMap = Analyzer.ComputeDeathHeatMap (deaths, gridSize, gridSize, out maxHeatMap);
			drawer.deathHeatMapMax = maxHeatMap;

			int[] maxHeatMap3d;
			heatMap3d = Analyzer.Compute3DHeatMap (paths, gridSize, gridSize, timeSamples, out maxHeatMap3d);
			drawer.heatMapMax3d = maxHeatMap3d;

			deathHeatMap3d = Analyzer.Compute3dDeathHeatMap(deaths, gridSize, gridSize, timeSamples, out maxHeatMap3d);
			drawer.deathHeatMapMax3d = maxHeatMap3d;

			combatHeatMap = Analyzer.Compute2DCombatHeatMap(paths, deaths, gridSize, gridSize, out maxHeatMap);
			drawer.combatHeatMap2dMax = maxHeatMap;

			drawer.tileSize.Set (SpaceState.Editor.tileSize.x, SpaceState.Editor.tileSize.y);
		}

		private void ComputeClusters () {
			if (MapperEditor.grid != null) {
				Dictionary<int, List<Path>> clusterMap = new Dictionary<int, List<Path>> ();
				foreach (Path currentPath in paths) {
					
					Node cur = currentPath.points [currentPath.points.Count - 1];
					Node par = cur.parent;
					while (cur.parent != null) {
						
						Vector3 p1 = cur.GetVector3 ();
						Vector3 p2 = par.GetVector3 ();
						Vector3 pd = p1 - p2;
						
						float pt = (cur.t - par.t);
						
						// Navigate through time to find the right cells to start from
						for (int t = 0; t < pt; t++) {
							
							float delta = ((float)t) / pt;
							
							Vector3 pos = p2 + pd * delta;
							int pX = Mathf.FloorToInt (pos.x);
							int pY = Mathf.FloorToInt (pos.z);
							
							short i = 1;
							if (fullMap [par.t + t] [pX] [pY].cluster > 0) {
								
								while (i <= 256) {
									if ((fullMap [par.t + t] [pX] [pY].cluster & i) > 0) {
										List<Path> inside;
										clusterMap.TryGetValue (i, out inside);
										
										if (inside == null) {
											inside = new List<Path> ();
											clusterMap.Add (i, inside);
										}
										
										if (!inside.Contains (currentPath))
											inside.Add (currentPath);
									}
									
									
									i *= 2;
								}
							}
						}
						
						cur = par;
						par = par.parent;
					}
				}
				
				ClustersRoot root = new ClustersRoot ();
				int j = 0;//for colours
				foreach (int n in clusterMap.Keys) {
					MetricsRoot cluster = new MetricsRoot ();
					cluster.number = n + "";
					
					
					foreach (Path path in clusterMap[n]) {
						cluster.everything.Add (new PathResults (path, null));
						switch (j) {
						case 0:
							path.color = Color.red;
							break; 
						case 1:
							path.color = Color.blue;
							break; 
						case 2:
							path.color = Color.green;
							break; 
						case 3:
							path.color = Color.magenta;
							break; 
						}
					}
					j++; 
					root.everything.Add (cluster);
				}
				
				XmlSerializer ser = new XmlSerializer (typeof(ClustersRoot), new Type[] {
					typeof(MetricsRoot),
					typeof(PathResults),
					typeof(PathValue),
					typeof(Value)
				});
				
				using (FileStream stream = new FileStream ("clusterresults.xml", FileMode.Create)) {
					ser.Serialize (stream, root);
					stream.Flush ();
					stream.Close ();
				}
			}
		}
	
		private void BatchComputing () {
			ResultsRoot root = new ResultsRoot ();
				
			float speed = GameObject.FindGameObjectWithTag ("AI").GetComponent<Player> ().speed;
				
			int gridsize, timesamples, rrtattemps, att = 1;
				
			//for (int att = 1; att <= 31; att++) {
				
			// Grid varying batch tests
				
			using (FileStream stream = new FileStream ("gridvary" + att + ".xml", FileMode.Create)) {
					
				rrtattemps = 30000;
				timesamples = 1200;
					
				for (gridsize = 60; gridsize <= 160; gridsize += 5) {
						
					Debug.Log ("Gridsize attemps " + gridsize + " Memory: " + GC.GetTotalMemory (true) + " Date: " + System.DateTime.Now.ToString ());
								
					fullMap = mapper.PrecomputeMaps (SpaceState.Editor, floor.collider.bounds.min, floor.collider.bounds.max, gridSize, gridsize, timesamples, stepSize);
								
					startX = (int)((start.transform.position.x - floor.collider.bounds.min.x) / SpaceState.Editor.tileSize.x);
					startY = (int)((start.transform.position.z - floor.collider.bounds.min.z) / SpaceState.Editor.tileSize.y);
					endX = (int)((end.transform.position.x - floor.collider.bounds.min.x) / SpaceState.Editor.tileSize.x);
					endY = (int)((end.transform.position.z - floor.collider.bounds.min.z) / SpaceState.Editor.tileSize.y);
	
					ResultBatch job = new ResultBatch ();
					job.gridSize = gridsize;
					job.timeSamples = timesamples;
					job.rrtAttemps = rrtattemps;
								
					TimeSpan average = new TimeSpan (0, 0, 0, 0, 0);
					List<Node> path = null;
								
					for (int it = 0; it < 155;) {
						Result single = new Result ();
									
						DateTime before = System.DateTime.Now;
						path = oldRrt.Compute (startX, startY, endX, endY, rrtattemps, speed, fullMap, false);
						oldRrt.tree = null;
						DateTime after = System.DateTime.Now;
						TimeSpan delta = after - before;
									
						average += delta;
						single.timeSpent = delta.TotalMilliseconds;
									
						if (path != null && path.Count > 0) {
							it++;
							job.results.Add (single);
						}
						job.totalTries++;
						if (job.totalTries > 100 && job.results.Count == 0)
							it = 200;
					}
						
					// Force the garbage collector to pick this guy before instantiating the next map to avoid memory leak in the Large Object Heap
					fullMap = null;
								
					job.averageTime = (double)average.TotalMilliseconds / (double)job.totalTries;
								
					root.everything.Add (job);
						
				}
					
				Debug.Log ("Serializing 1");
					
				XmlSerializer ser = new XmlSerializer (typeof(ResultsRoot), new Type[] {
					typeof(ResultBatch),
					typeof(Result)
				});
				ser.Serialize (stream, root);
				stream.Flush ();
				stream.Close ();
			}
				
			// Time varying batch tests
				
			using (FileStream stream = new FileStream ("timevary" + att + ".xml", FileMode.Create)) {
				
				root.everything.Clear ();
					
				gridsize = 60;
				rrtattemps = 30000;
					
				for (timesamples = 500; timesamples <= 3500; timesamples += 100) {
						
					Debug.Log ("Timesamples attemps " + timesamples + " Memory: " + GC.GetTotalMemory (true) + " Date: " + System.DateTime.Now.ToString ());
						
					fullMap = mapper.PrecomputeMaps (SpaceState.Editor, floor.collider.bounds.min, floor.collider.bounds.max, gridSize, gridSize, timesamples, stepSize);
						
					startX = (int)((start.transform.position.x - floor.collider.bounds.min.x) / SpaceState.Editor.tileSize.x);
					startY = (int)((start.transform.position.z - floor.collider.bounds.min.z) / SpaceState.Editor.tileSize.y);
					endX = (int)((end.transform.position.x - floor.collider.bounds.min.x) / SpaceState.Editor.tileSize.x);
					endY = (int)((end.transform.position.z - floor.collider.bounds.min.z) / SpaceState.Editor.tileSize.y);
						
					ResultBatch job = new ResultBatch ();
					job.gridSize = gridsize;
					job.timeSamples = timesamples;
					job.rrtAttemps = rrtattemps;
						
					TimeSpan average = new TimeSpan (0, 0, 0, 0, 0);
					List<Node> path = null;
						
					for (int it = 0; it < 155;) {
						Result single = new Result ();
							
						DateTime before = System.DateTime.Now;
						path = oldRrt.Compute (startX, startY, endX, endY, rrtattemps, speed, fullMap, false);
						DateTime after = System.DateTime.Now;
						TimeSpan delta = after - before;
							
						average += delta;
						single.timeSpent = delta.TotalMilliseconds;
							
						if (path.Count > 0) {
							it++;
							job.results.Add (single);
						}
						job.totalTries++;
						if (job.totalTries > 100 && job.results.Count == 0)
							it = 200;
					}
					fullMap = null;
						
					job.averageTime = (double)average.TotalMilliseconds / (double)job.totalTries;
						
					root.everything.Add (job);
						
				}
					
				Debug.Log ("Serializing 2");
					
				XmlSerializer ser = new XmlSerializer (typeof(ResultsRoot), new Type[] {
					typeof(ResultBatch),
					typeof(Result)
				});
				ser.Serialize (stream, root);
				stream.Flush ();
				stream.Close ();
			}
				
			// Attemp varying batch tests
				
			using (FileStream stream = new FileStream ("attempvary" + att + ".xml", FileMode.Create)) {
				
				root.everything.Clear ();
					
				gridsize = 60;
				timesamples = 1200;
	
				fullMap = mapper.PrecomputeMaps (SpaceState.Editor, floor.collider.bounds.min, floor.collider.bounds.max, gridSize, gridSize, timesamples, stepSize);
					
				startX = (int)((start.transform.position.x - floor.collider.bounds.min.x) / SpaceState.Editor.tileSize.x);
				startY = (int)((start.transform.position.z - floor.collider.bounds.min.z) / SpaceState.Editor.tileSize.y);
				endX = (int)((end.transform.position.x - floor.collider.bounds.min.x) / SpaceState.Editor.tileSize.x);
				endY = (int)((end.transform.position.z - floor.collider.bounds.min.z) / SpaceState.Editor.tileSize.y);
					
				for (rrtattemps = 5000; rrtattemps <= 81000; rrtattemps += 3000) {
						
					Debug.Log ("RRT attemps " + rrtattemps + " Memory: " + GC.GetTotalMemory (true) + " Date: " + System.DateTime.Now.ToString ());
	
					ResultBatch job = new ResultBatch ();
					job.gridSize = gridsize;
					job.timeSamples = timesamples;
					job.rrtAttemps = rrtattemps;
	
					TimeSpan average = new TimeSpan (0, 0, 0, 0, 0);
					List<Node> path = null;
	
					for (int it = 0; it < 155;) {
						Result single = new Result ();
							
						DateTime before = System.DateTime.Now;
						path = oldRrt.Compute (startX, startY, endX, endY, rrtattemps, speed, fullMap, false);
						DateTime after = System.DateTime.Now;
						TimeSpan delta = after - before;
							
						average += delta;
						single.timeSpent = delta.TotalMilliseconds;
							
						if (path.Count > 0) {
							it++;
							job.results.Add (single);
						}
						job.totalTries++;
						if (job.totalTries > 100 && job.results.Count == 0)
							it = 200;
					}
						
					job.averageTime = (double)average.TotalMilliseconds / (double)job.totalTries;
							
					root.everything.Add (job);
						
				}
					
				fullMap = null;
					
				XmlSerializer ser = new XmlSerializer (typeof(ResultsRoot), new Type[] {
					typeof(ResultBatch),
					typeof(Result)
				});
				ser.Serialize (stream, root);
				stream.Flush ();
				stream.Close ();
			}
				
			//}
		}
		
		// Resets the AI back to it's original position
		private void ResetAI () {
			GameObject[] objs = GameObject.FindGameObjectsWithTag ("AI") as GameObject[];
			foreach (GameObject ob in objs)
				ob.GetComponent<Player> ().ResetSimulation ();
			
			objs = GameObject.FindGameObjectsWithTag ("Enemy") as GameObject[];
			foreach (GameObject ob in objs) 
				ob.GetComponent<Enemy> ().ResetSimulation ();
			
			timeSlice = 0;
			
			
		}
		
		// Updates everyone's position to the current timeslice
		private void UpdatePositions (int t, Mapper mapper, float diff = 0f) {
			for (int i = 0; i < SpaceState.Editor.enemies.Length; i++) {
				if (SpaceState.Editor.enemies [i] == null)
					continue;
				
				Vector3 pos;
				Quaternion rot;
				
				if (t == 0 || diff == 0) {
					pos = SpaceState.Editor.enemies [i].positions [t];
					rot = SpaceState.Editor.enemies [i].rotations [t];	
				} else {
					pos = SpaceState.Editor.enemies [i].transform.position;
					rot = SpaceState.Editor.enemies [i].transform.rotation;
				}
				
				if (diff > 0 && t + 1 < SpaceState.Editor.enemies [i].positions.Length) {
					pos += (SpaceState.Editor.enemies [i].positions [t + 1] - SpaceState.Editor.enemies [i].positions [t]) * diff;
					//rot = Quaternion.Lerp(rot, SpaceState.Editor.enemies[i].rotations[t+1], diff);
				}
				
				SpaceState.Editor.enemies [i].transform.position = pos;
				SpaceState.Editor.enemies [i].transform.rotation = rot;
			}

			foreach (KeyValuePair<Path, GameObject> each in players) {
				bool used = false;
				toggleStatus.TryGetValue (each.Key, out used);
				if (used) {
					Node p = null;
					foreach (Node n in each.Key.points) {
						if (n.t > t) {
							p = n;
							break;
						}
					}
					if (p != null) {
						Vector3 n2 = p.GetVector3 ();
						Vector3 n1 = p.parent.GetVector3 ();
						Vector3 pos = n1 * (1 - (float)(t - p.parent.t) / (float)(p.t - p.parent.t)) + n2 * ((float)(t - p.parent.t) / (float)(p.t - p.parent.t));
						
						pos.x *= SpaceState.Editor.tileSize.x;
						pos.z *= SpaceState.Editor.tileSize.y;
						pos.x += floor.collider.bounds.min.x;
						pos.z += floor.collider.bounds.min.z;
						pos.y = 0f;

						each.Value.transform.position = pos;
						textDraw.Add(Tuple.New<Vector3, string>(new Vector3(pos.x, pos.y + 1f, pos.z), "H:" + p.playerhp));
					}
				}
			}
		}
		
		private void StorePositions () {
			GameObject[] objs = GameObject.FindGameObjectsWithTag ("Enemy") as GameObject[];
			for (int i = 0; i < objs.Length; i++) {
				objs [i].GetComponent<Enemy> ().SetInitialPosition ();
			}
			objs = GameObject.FindGameObjectsWithTag ("AI") as GameObject[];
			for (int i = 0; i < objs.Length; i++) {
				objs [i].GetComponent<Player> ().SetInitialPosition ();
			}
		}
		
	}
}