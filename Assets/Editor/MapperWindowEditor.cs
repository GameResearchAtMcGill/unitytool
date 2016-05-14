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
using Vectrosity;

namespace EditorArea {
	public class MapperWindowEditor : EditorWindow {


		#region variables

		// Data holders
		private static Cell[][][] fullMap, original;
		public static List<Path> paths = new List<Path> (), deaths = new List<Path>();

		// Parameters with default values
		public static int timeSamples = 2000, attemps = 2500, iterations = 1, gridSize = 60, ticksBehind = 0;
		private static bool drawMap = true, drawNeverSeen = false, drawHeatMap = false, drawHeatMap3d = false, drawDeathHeatMap = false, drawDeathHeatMap3d = false, drawCombatHeatMap = false, drawPath = true, smoothPath = false, drawFoVOnly = false, drawCombatLines = false, simulateCombat = false;
		private static float stepSize = 1 / 10f, crazySeconds = 5f, playerDPS = 10;
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

		//GEONEW
		public static Triangulation triangles;
		public static List<PathGeo> pathsgeo = new List<PathGeo> ();
		public static bool drawPathGeo = true;
		private static Dictionary<PathGeo, bool> toggleStatusGeo = new Dictionary<PathGeo, bool> ();
		private static GameObject[] enemygeoobjs = null;
		private static List<EnemyGeo> enemygeos = null;
		private static bool playingGeo = false;
		private static int curFrame = 0, realFrame = 0, totalFrames = 0;
		private static bool ignoreFrameLimit = true;
		private static bool useDist = false;
		private static bool useDists = false;
		private static int distTime = 0;

		public static int attemps2 = 1;
		private static int preCompWidth = 100, preCompHeight = 100;
		private static int colorCodeIndex = 0;

		// Helping stuff
		private static Vector2 scrollPos = new Vector2 ();
		private static GameObject playerNode;
		private List<Tuple<Vector3, string>> textDraw = new List<Tuple<Vector3, string>>();
		private int lastTime = timeSlice;
		private long stepInTicks = 0L, playTime = 0L;
		private static bool simulated = false, playing = false;
		private Mapper mapper;
		private RRTKDTreeCombat rrt = new RRTKDTreeCombat ();
		private RRTKDTree oldRrt = new RRTKDTree ();
		private MapperEditorDrawer drawer;
		private DateTime previous = DateTime.Now;
		private long accL = 0L;


		//Material lineMaterial = Resources.Load ("Arrow", typeof(Material)) as Material;
		//Texture2D backTex = Resources.Load ("arrowStart", typeof(Texture2D)) as Texture2D;

		#endregion variables

		[MenuItem("Window/Mapper")]
		static void Init () {
			MapperWindowEditor window = (MapperWindowEditor)EditorWindow.GetWindow (typeof(MapperWindowEditor));
			//window.title = "Mapper";
            window.titleContent.text = "Mapper";
			window.ShowTab ();
		}
		
		void OnGUI () {

			if (GUILayout.Button ("Test Stuff")) {
                Debug.Log("Button PRessed");

                triangles = GameObject.Find("Triangulation").GetComponent<Triangulation>();

                triangles.TriangulationSpace();
                List<Triangle> tris = triangles.triangles;
                Triangle startTri = null;
                //Vector3 startPoint = new Vector3(-45, 1, 45);
                Vector3 startPoint = new Vector3(-20, 1, -20);
                foreach (Triangle t in tris) {
                    if (t.containsPoint(startPoint)) {
                        startTri = t;
                    }
                }

                Triangulation.computeDistanceTree(startTri);
                
                GameObject trianglesDraw = new GameObject("Triangles");
                GameObject visitedTris = new GameObject("VisitedTris");
                int triIndI = 1;
                string triIndS = triIndI.ToString();

                foreach (Triangle tri in tris)
                {
                    //Debug.Log("NEW TRI");
                    //Debug.Log(tri.GetCenterTriangle());
                    //Debug.Log(tri.distance);
                    if(tri.distance < float.MaxValue / 2) {
                        GameObject triObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        Vector3 pos = tri.GetCenterTriangle();
                        pos.y = tri.distance;
                        triObj.transform.position = pos;
                        triObj.transform.parent = visitedTris.transform;
                    }

                    GameObject triangle = new GameObject("triangle" + triIndS);
                    triIndI++;
                    triIndS = triIndI.ToString();
                    triangle.transform.parent = trianglesDraw.transform;
                    //Debug.Log(tri);
                    //Debug.Log(tri.vertex[0] + "," + tri.vertex[1] + "," + tri.vertex[2]);
                    Line[] lins = tri.getLines();
                    foreach (Line l in lins)
                    {
                        //Debug.Log(l.vertex[0] + "," + l.vertex[1]);

                        GameObject lin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        lin.GetComponent<Renderer>().sharedMaterial.color = Color.red;
                        lin.transform.parent = triangle.transform;
                        lin.transform.position = (l.vertex[0] + l.vertex[1]) / 2.0f;
                        lin.transform.position = new Vector3(lin.transform.position.x, 0.05f * lin.transform.position.y, lin.transform.position.z);
                        Vector3 dists = (l.vertex[1] - l.vertex[0]);
                        dists.y = 0.05f * dists.y;

                        Vector3 from = Vector3.right;
                        Vector3 to = dists / dists.magnitude;

                        Vector3 axis = Vector3.Cross(from, to);
                        float angle = Mathf.Rad2Deg * Mathf.Acos(Vector3.Dot(from, to));
                        lin.transform.RotateAround(lin.transform.position, axis, angle);


                        Vector3 scale = Vector3.one;
                        scale.x = Vector3.Magnitude(dists);
                        scale.z = 0.2f;
                        scale.y = 0.2f;

                        lin.transform.localScale = scale;


                    }
                    float l1 = lins[0].Magnitude();
                    float l2 = lins[1].Magnitude();
                    float l3 = lins[2].Magnitude();
                    float s = 0.5f * (l1 + l2 + l3);
                    float area = Mathf.Sqrt(s * (s - l1) * (s - l2) * (s - l3));
                    //Debug.Log(area);
                }



                /*
                List<float> areas = new List<float>();
                float areaSum = 0;
                for(int i =0; i < tris.Count; i++) {
                    Triangle tri = tris[i];
                    Line[] lins = tri.getLines();
                    float l1 = lins[0].Magnitude();
                    float l2 = lins[1].Magnitude();
                    float l3 = lins[2].Magnitude();
                    float s = 0.5f * (l1 + l2 + l3);
                    float area = Mathf.Sqrt(s * (s - l1) * (s - l2) * (s - l3));
                    areas.Add(area);
                    areaSum = areaSum + area;
                }
                List<float> standardizedAreas = new List<float>();
                foreach(float a in areas) {
                    standardizedAreas.Add(a / areaSum);
                }
                */
                //List<Vector3> pointList = new List<Vector3>();
                //pointList.Add(new Vector3(-5, 0, -5));
                //pointList.Add(new Vector3(5, 0, 5));
                //float linewidth = 5.0f;
                //Color c = Color.red;

                //VectorLine line = new VectorLine("Line",pointList, linewidth);
                //line.color = c;
                //line.Draw3D ();
                //VectorLine line2 = new VectorLine("Line2", new List<Vector3>(pointList), 1.0f);

                //line2.Draw3D ();

                //List<Vector3> pointList = new List<Vector3>();
                //pointList.Add(new Vector3(-5, 0, 5));
                //pointList.Add(new Vector3(5, 0, 5));
                //float linewidth = 5.0f;
                //VectorLine line = new VectorLine("line", pointList, linewidth);
                //line.Draw3D();
                //line2.Draw ();
                //line2.Draw3DAuto();

                //line = new VectorObject3D();

                //line.drawTransform = new GameObject().transform;
                //Debug.Log (line.drawTransform);
                //Debug.Log (line.drawTransform.parent);
                //Debug.Log(parent);
                //Debug.Log (parent.transform);

                //line.drawTransform.parent = parent.transform;






            }

            if (GUILayout.Button("Test Stuff2")) {
                Debug.Log("Button PRessed");

                triangles = GameObject.Find("Triangulation").GetComponent<Triangulation>();

                triangles.TriangulationSpace();
                List<Triangle> tris = triangles.triangles;
                Triangle startTri = null;
                //Vector3 startPoint = new Vector3(-45, 1, 45);
                //Vector3 startPoint = new Vector3(-20, 1, 20);
                //Vector3 endPoint = new Vector3(43, 1, -42.7f);
                Vector3 startPoint = new Vector3(-20, 1, -20);
                //Vector3 endPoint = new Vector3(47, 1, -46);
                //Vector3 endPoint = new Vector3(20, 1, -20);
                Vector3 endPoint = new Vector3(20, 1, 20);
                Triangle endTri = null;
                foreach (Triangle t in tris) {
                    if (t.containsPoint(startPoint)) {
                        startTri = t;
                    }
                    if (t.containsPoint(endPoint)) {
                        endTri = t;
                    }
                    t.treeDepth = -1;
                }


                /*foreach(Triangle t in triangles.triangles) {
                    Debug.Log(t.GetCenterTriangle());
                    Debug.Log(t.parents.Count);
                }*/
                //Triangulation.simpTreeStruct(startTri);


                //Triangulation.computeDistanceTreeE(startTri, endTri);
                //Debug.Log(startTri.distance);
                //Triangulation.drawTreeStruct(startTri);


                List<List<int>> paths = Triangulation.findAllSimpleEndPaths(startTri, endTri);

                /*
                Debug.Log("Number of Paths Found = " + paths.Count);
                foreach (List<int> path in paths) {
                    string toPrint = "Path:";
                    foreach (int choice in path) {
                        toPrint = toPrint + choice + ",";
                    }
                    Debug.Log(toPrint);
                }

                Debug.Log("CREATING MIDPOINT OBJECTS");
                List<Triangle> midTris = new List<Triangle>();
                foreach (List<int> pth in paths) {
                    Triangle t = Triangulation.findMidTriangleAlongPath(startTri, endTri, pth);
                    GameObject midPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    midPoint.name = "A MIDPOINT";
                    midPoint.transform.position = t.GetCenterTriangle();
                    Debug.Log(t.GetCenterTriangle());
                    if (!midTris.Contains(t)) {
                        midTris.Add(t);
                    }
                }
                Debug.Log("UNIQUE MIDPOINTS FOUND");
                foreach(Triangle midTri in midTris) {
                    GameObject midPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    midPoint.name = "A UNique MIDPOINT";
                    midPoint.transform.position = midTri.GetCenterTriangle();
                    Debug.Log(midTri.GetCenterTriangle());
                }
                /**/
                Triangulation.computeDistanceTreeE(startTri, endTri);
                Triangulation.drawTreeStruct(startTri);
                Triangulation.drawTreeStructSimp(startTri);







                //Triangulation.computeDistanceTree(startTri);
            }

            if (GUILayout.Button("Test Stuff3")) {
                Debug.Log("Button PRessed");

                triangles = GameObject.Find("Triangulation").GetComponent<Triangulation>();

                RRTKDTreeGEO rrt = new RRTKDTreeGEO();
                List<Geometry> obstacles = triangles.TriangulationSpace();
                List<Triangle> tris = triangles.triangles;

                if (start == null) {
                    start = GameObject.Find("Start");
                }
                if (end == null) {
                    end = GameObject.Find("End");
                }
                float startX = start.transform.position.x;
                float startY = start.transform.position.z;
                float endX = end.transform.position.x;
                float endY = end.transform.position.z;

                rrt.generateMidPoint(startX, startY, endX, endY, tris, false);
                rrt.generateMidPoint(startX, startY, endX, endY,tris, true);
                rrt.generateMidPoints(startX, startY, endX, endY, tris);

            }


            if (GUILayout.Button("Test Stuff4")) {
                Debug.Log("Button PRessed");

                triangles = GameObject.Find("Triangulation").GetComponent<Triangulation>();

                RRTKDTreeGEO rrt = new RRTKDTreeGEO();
                /*List<Geometry> obstacles = triangles.TriangulationSpace();
                List<Triangle> tris = triangles.triangles;

                if (start == null) {
                    start = GameObject.Find("Start");
                }
                if (end == null) {
                    end = GameObject.Find("End");
                }
                float startX = start.transform.position.x;
                float startY = start.transform.position.z;
                float endX = end.transform.position.x;
                float endY = end.transform.position.z;

                rrt.generateMidPoint(startX, startY, endX, endY, tris, false);
                rrt.generateMidPoint(startX, startY, endX, endY, tris, true);
                rrt.generateMidPoints(startX, startY, endX, endY, tris);*/


                EnemyGeo e = GameObject.Find("EnemyGeo Bot").GetComponent<EnemyGeo>();
                
                Debug.Log(rrt.checkCollE(e, 0, 100, -9, new List<int>(), new List<int>()));

                Debug.Log(e.getPosition(100));





            }



            #region Pre-Init

            // Wait for the floor to be set and initialize the drawer and the mapper
            if (floor != null) {
				if (floor.GetComponent<Collider>() == null) {
					Debug.LogWarning ("Floor has no valid collider, game object ignored.");
					floor = null;
				} else {
					drawer = floor.gameObject.GetComponent<MapperEditorDrawer> ();
					if (drawer == null) {
						drawer = floor.gameObject.AddComponent<MapperEditorDrawer> ();
						//drawer.hideFlags = HideFlags.HideInInspector;
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
			/*
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
			
			if (GUILayout.Button ("Precompute Maps")) 
			{
				ComputeMap();
			} 
			EditorGUILayout.LabelField ("");
			
			#endregion
			
			// ----------------------------------
			*/
			#region 4. Path

			#region not mine

			//EditorGUILayout.LabelField ("4. Path");
			
			start = (GameObject)EditorGUILayout.ObjectField ("Start", start, typeof(GameObject), true);
			end = (GameObject)EditorGUILayout.ObjectField ("End", end, typeof(GameObject), true);
			attemps = EditorGUILayout.IntSlider ("Attempts", attemps, 10, 100000);
			attemps2 = EditorGUILayout.IntSlider ("Attempts2", attemps2, 1, 100);
			iterations = EditorGUILayout.IntSlider ("Iterations", iterations, 1, 1500);
			randomSeed = EditorGUILayout.IntSlider("Random Seed", randomSeed, -1, 10000);
			/*smoothPath = EditorGUILayout.Toggle ("Smooth path", smoothPath);
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

				startX = (int)((start.transform.position.x - floor.GetComponent<Collider>().bounds.min.x) / SpaceState.Editor.tileSize.x);
				startY = (int)((start.transform.position.z - floor.GetComponent<Collider>().bounds.min.z) / SpaceState.Editor.tileSize.y);
				endX = (int)((end.transform.position.x - floor.GetComponent<Collider>().bounds.min.x) / SpaceState.Editor.tileSize.x);
				endY = (int)((end.transform.position.z - floor.GetComponent<Collider>().bounds.min.z) / SpaceState.Editor.tileSize.y);

				paths.Clear ();
				deaths.Clear ();
				ClearPathsRepresentation ();
				arrangedByCrazy = arrangedByDanger = arrangedByDanger3 = arrangedByDanger3Norm = arrangedByLength = arrangedByLoS = arrangedByLoS3 = arrangedByLoS3Norm = arrangedByTime = arrangedByVelocity = null;

				Exploration.DavAStar3d astar3d = new DavAStar3d();
				List<Node> nodes = astar3d.Compute(startX, startY, endX, endY, original, playerSpeed);

				if (nodes.Count > 0) {
					paths.Add (new Path (nodes));
					toggleStatus.Add (paths.Last (), true);
					paths.Last ().color = new Color (UnityEngine.Random.Range (0.0f, 1.0f), UnityEngine.Random.Range (0.0f, 1.0f), UnityEngine.Random.Range (0.0f, 1.0f));
				}
			}

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
				startX = (int)((start.transform.position.x - floor.GetComponent<Collider>().bounds.min.x) / SpaceState.Editor.tileSize.x);
				startY = (int)((start.transform.position.z - floor.GetComponent<Collider>().bounds.min.z) / SpaceState.Editor.tileSize.y);
				endX = (int)((end.transform.position.x - floor.GetComponent<Collider>().bounds.min.x) / SpaceState.Editor.tileSize.x);
				endY = (int)((end.transform.position.z - floor.GetComponent<Collider>().bounds.min.z) / SpaceState.Editor.tileSize.y);

				GameObject[] hps = GameObject.FindGameObjectsWithTag("HealthPack");
				HealthPack[] packs = new HealthPack[hps.Length];
				for (int i = 0; i < hps.Length; i++) {
					packs[i] = hps[i].GetComponent<HealthPack>();
					packs[i].posX = (int)((packs[i].transform.position.x - floor.GetComponent<Collider>().bounds.min.x) / SpaceState.Editor.tileSize.x);
					packs[i].posZ = (int)((packs[i].transform.position.z - floor.GetComponent<Collider>().bounds.min.z) / SpaceState.Editor.tileSize.y);
				}

				// Update the parameters on the RRT class
				rrt.min = floor.GetComponent<Collider>().bounds.min;
				rrt.tileSizeX = SpaceState.Editor.tileSize.x;
				rrt.tileSizeZ = SpaceState.Editor.tileSize.y;
				rrt.enemies = SpaceState.Editor.enemies;
				rrt.packs = packs;
				rrt.simulateCombat = simulateCombat;

				int seed = randomSeed;
				if (randomSeed != -1)
					UnityEngine.Random.seed = randomSeed;
				else {
					DateTime now = DateTime.Now;
					seed = now.Millisecond + now.Second + now.Minute + now.Hour + now.Day + now.Month+ now.Year;
					UnityEngine.Random.seed = seed;
				}

				List<Node> nodes = null;
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
						nodes = rrt.Compute (startX, startY, endX, endY, attemps, stepSize, playerMaxHp, playerSpeed, playerDPS, fullMap, smoothPath);
						// Did we found a path?
						if (nodes.Count > 0) {
							paths.Add (new Path (nodes));
							toggleStatus.Add (paths.Last (), true);
							paths.Last ().color = new Color (UnityEngine.Random.Range (0.0f, 1.0f), UnityEngine.Random.Range (0.0f, 1.0f), UnityEngine.Random.Range (0.0f, 1.0f));
						}
						// Grab the death list
						foreach (List<Node> deathNodes in rrt.deathPaths) {
							deaths.Add(new Path(deathNodes));
						}
					} catch (Exception e) {
						Debug.LogWarning("Skip errored calculated path");
						Debug.LogException(e);
						// This can happen in two different cases:
						// In line 376 by having a duplicate node being picked (coincidence picking the EndNode as visiting but the check is later on)
						// We also cant just bring the check earlier since there's data to be copied (really really rare cases)
						// The other case is yet unkown, but it's a conicidence by trying to insert a node in the tree that already exists (really rare cases)
					}
				}
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
			*/
			#endregion not mine

			#region experimental
			EditorGUILayout.LabelField ("");
			EditorGUILayout.LabelField ("");

			RRTKDTreeGEO rrtgeo = new RRTKDTreeGEO();

			if(GUILayout.Button("Clear Paths"))
			{
				GameObject g = GameObject.Find("temp");
				DestroyImmediate(g);
				g = new GameObject("temp");
			}

			EditorGUILayout.LabelField ("");

			useDist = EditorGUILayout.Toggle ("Use Distraction", useDist);
			useDists = EditorGUILayout.Toggle ("Use Distractions", useDists);
			distTime = EditorGUILayout.IntField("Distraction Time", distTime);

			if (GUILayout.Button (playingGeo ? "StopGeo" : "PlayGeo")) {
				playingGeo = !playingGeo;
			}

			if (GUILayout.Button ("Reset playtime")){
				playingGeo = false;
				curFrame = 0;
				realFrame = 0;
				goToFrame(0);
			}
			EditorGUILayout.IntField(curFrame);

			EditorGUILayout.LabelField ("");

			if(GUILayout.Button ("Print latest path")){
				printLatestPath();
			}

			if(GUILayout.Button ("Refind Enemies")) {
				enemygeoobjs  = GameObject.FindGameObjectsWithTag("EnemyGeo");
				enemygeos = new List<EnemyGeo>();
				foreach(GameObject g in enemygeoobjs){
					enemygeos.Add(g.GetComponent<EnemyGeo>());
					
				}
			}

			if (GUILayout.Button ("Compute Path Geo")) {
				if(enemygeoobjs  == null){
					enemygeoobjs  = GameObject.FindGameObjectsWithTag("EnemyGeo");
					enemygeos = new List<EnemyGeo>();
					foreach(GameObject g in enemygeoobjs){
						enemygeos.Add(g.GetComponent<EnemyGeo>());

					}

				}
				rrtgeo.enemies = enemygeos;


				rrtgeo.casts = casts;
				triangles = GameObject.Find ("Triangulation").GetComponent<Triangulation>();


                List<Geometry> obstacles = triangles.TriangulationSpace();
                //triangles.TriangulationSpace();
                List<Triangle> tris = triangles.triangles;


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

				/*

				// Prepare start and end position
				startX = (int)((start.transform.position.x - floor.collider.bounds.min.x) / SpaceState.Editor.tileSize.x);
				startY = (int)((start.transform.position.z - floor.collider.bounds.min.z) / SpaceState.Editor.tileSize.y);
				endX = (int)((end.transform.position.x - floor.collider.bounds.min.x) / SpaceState.Editor.tileSize.x);
				endY = (int)((end.transform.position.z - floor.collider.bounds.min.z) / SpaceState.Editor.tileSize.y);
				
				GameObject[] hps = GameObject.FindGameObjectsWithTag("HealthPack");
				HealthPack[] packs = new HealthPack[hps.Length];
				for (int i = 0; i < hps.Length; i++) {
					packs[i] = hps[i].GetComponent<HealthPack>();
					packs[i].posX = (int)((packs[i].transform.position.x - floor.collider.bounds.min.x) / SpaceState.Editor.tileSize.x);
					packs[i].posZ = (int)((packs[i].transform.position.z - floor.collider.bounds.min.z) / SpaceState.Editor.tileSize.y);
				}
				
				// Update the parameters on the RRT class
				rrt.min = floor.collider.bounds.min;
				rrt.tileSizeX = SpaceState.Editor.tileSize.x;
				rrt.tileSizeZ = SpaceState.Editor.tileSize.y;
				rrt.enemies = SpaceState.Editor.enemies;
				rrt.packs = packs;
				rrt.simulateCombat = simulateCombat;
				*/
				float startX = start.transform.position.x;
				float startY = start.transform.position.z;
				float endX = end.transform.position.x;
				float endY = end.transform.position.z;

				GameObject floora = GameObject.Find ("Floor");
				
				float minX = floora.GetComponent<Collider>().bounds.min.x;
				float maxX = floora.GetComponent<Collider>().bounds.max.x;
				float minY = floora.GetComponent<Collider>().bounds.min.z;
				float maxY = floora.GetComponent<Collider>().bounds.max.z;




				int seed = randomSeed;
				if (randomSeed != -1)
					UnityEngine.Random.seed = randomSeed;
				else {
					DateTime now = DateTime.Now;
					seed = now.Millisecond + now.Second + now.Minute + now.Hour + now.Day + now.Month+ now.Year;
					UnityEngine.Random.seed = seed;
				}
				
				List<NodeGeo> nodes = null;

				Vector3 distractPos = GameObject.Find ("DistractPoint").transform.position;
				Vector2 distractPos2 = new Vector2(distractPos.x, distractPos.z);

				Vector3 distract2Pos = GameObject.Find ("DistractPoint2").transform.position;
				Vector2 distract2Pos2 = new Vector2(distract2Pos.x, distract2Pos.z);

                

                List<RRTResult> results = new List<RRTResult>();


                for (int it = 0; it < iterations; it++) {

                    RRTResult toReturn = new RRTResult();
                    toReturn.startTime = System.DateTime.Now;
                    int index = 0;
                    toReturn.rrtsUsedMM.Add(0);
                    toReturn.rrtsUsed2MM.Add(0);
                    toReturn.nodesUsedMM.Add(0);
                    toReturn.nodesUsed2MM.Add(0);
                    toReturn.nodesRejectedMM.Add(0);
                    toReturn.nodesRejected2MM.Add(0);
                    toReturn.type = "normal";

                    bool found = false;
                    for (int it2 = 0; it2 < attemps2; it2++){
                        toReturn.rrtsUsedMM[index]++;

                        // We have this try/catch block here to account for the issue that we don't solve when we find a path when t is near the limit
                        try {

                            //nodes= rrtgeo.ComputeGeo (startX, startY, endX, endY, minX, maxX, minY, maxY, 1000, attemps, playerSpeed, distractPos2, distract2Pos2);
                            toReturn = rrtgeo.ComputeGeo(startX, startY, endX, endY, minX, maxX, minY, maxY, 1000, attemps, playerSpeed, distractPos2, distract2Pos2, tris, toReturn, index, obstacles, false);
                            nodes = toReturn.Nodes;
                            toReturn.endTime = DateTime.Now;
                            
                            //nodes = rrt.Compute (startX, startY, endX, endY, attemps, stepSize, playerMaxHp, playerSpeed, playerDPS, fullMap, smoothPath);

                            //Debug.Log (nodes.Count);
                            if (nodes.Count <= 0){
								Debug.Log ("RRT Search Failed");
							}

							// Did we found a path?
							if (nodes.Count > 0) {
                                found = true;
                                results.Add(toReturn);

                                pathsgeo.Add (new PathGeo(nodes));
								toggleStatusGeo.Add(pathsgeo.Last(), true);
								//Debug.Log ("Count1 is : " + toggleStatusGeo.Count);
								pathsgeo.Last ().color = new Color (UnityEngine.Random.Range (0.0f, 1.0f), UnityEngine.Random.Range (0.0f, 1.0f), UnityEngine.Random.Range (0.0f, 1.0f));
								//paths.Add (new Path (nodes));
								//toggleStatus.Add (paths.Last (), true);
								//paths.Last ().color = new Color (UnityEngine.Random.Range (0.0f, 1.0f), UnityEngine.Random.Range (0.0f, 1.0f), UnityEngine.Random.Range (0.0f, 1.0f));



								//Create path lines using the line objects 
								List<Line> path = new List<Line>(); 
								for(int i =0; i<nodes.Count-1;i++)
								{
									path.Add(new Line(nodes[i].GetVector3Draw(),nodes[i+1].GetVector3Draw()));
								}

								GameObject temp = GameObject.Find("temp");
								Color c = new Color(UnityEngine.Random.Range(0.0f,1.0f),
			                           UnityEngine.Random.Range(0.0f,1.0f),
			                           UnityEngine.Random.Range(0.0f,1.0f)) ;

								/*foreach(Line l in path)
								{
									l.DrawVector(temp,c);
								}*/

								if(nodes.Count > 0){
									Debug.Log ("Succeeded in " + it2 + " attempts");
									break;
								}
							}


							/*
							// Grab the death list
							foreach (List<Node> deathNodes in rrt.deathPaths) {
								deaths.Add(new Path(deathNodes));
							}
							*/

						} catch (Exception e) {
							Debug.LogWarning("Skip errored calculated path");
							Debug.LogException(e);
							// This can happen in two different cases:
							// In line 376 by having a duplicate node being picked (coincidence picking the EndNode as visiting but the check is later on)
							// We also cant just bring the check earlier since there's data to be copied (really really rare cases)
							// The other case is yet unkown, but it's a conicidence by trying to insert a node in the tree that already exists (really rare cases)
						}
					}
                    if (!found) {
                        results.Add(toReturn);
                    }


                }

                printResultsToFile(results);
                
                // Set the map to be drawn
				//drawer.fullMap = fullMap;
				//ComputeHeatMap (paths, deaths);
				
				// Compute the summary about the paths and print it
				String summary = "Summary:\n";
				summary += "Seed used:" + seed;
				summary += "\nSuccessful paths found: " + paths.Count;
				summary += "\nDead paths: " + deaths.Count;

				/*

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

				*/

				 Debug.Log(summary);
				
			}
			
            if (GUILayout.Button ("Compute Path Multi-Part Geo")) {
                if (enemygeoobjs == null) {
                    enemygeoobjs = GameObject.FindGameObjectsWithTag("EnemyGeo");
                    enemygeos = new List<EnemyGeo>();
                    foreach (GameObject g in enemygeoobjs) {
                        enemygeos.Add(g.GetComponent<EnemyGeo>());

                    }

                }
                rrtgeo.enemies = enemygeos;


                rrtgeo.casts = casts;
                triangles = GameObject.Find("Triangulation").GetComponent<Triangulation>();

                triangles.TriangulationSpace();
                List<Triangle> tris = triangles.triangles;


                float playerSpeed = GameObject.FindGameObjectWithTag("AI").GetComponent<Player>().speed;

                //Check the start and the end and get them from the editor. 
                if (start == null) {
                    start = GameObject.Find("Start");
                }
                if (end == null) {
                    end = GameObject.Find("End");
                }

                paths.Clear();

                float startX = start.transform.position.x;
                float startY = start.transform.position.z;
                float endX = end.transform.position.x;
                float endY = end.transform.position.z;

                GameObject floora = GameObject.Find("Floor");

                float minX = floora.GetComponent<Collider>().bounds.min.x;
                float maxX = floora.GetComponent<Collider>().bounds.max.x;
                float minY = floora.GetComponent<Collider>().bounds.min.z;
                float maxY = floora.GetComponent<Collider>().bounds.max.z;




                int seed = randomSeed;
                if (randomSeed != -1)
                    UnityEngine.Random.seed = randomSeed;
                else {
                    DateTime now = DateTime.Now;
                    seed = now.Millisecond + now.Second + now.Minute + now.Hour + now.Day + now.Month + now.Year;
                    UnityEngine.Random.seed = seed;
                }

                RRTResult returned = null;
                List<NodeGeo> nodes = null;

                Vector3 distractPos = GameObject.Find("DistractPoint").transform.position;
                Vector2 distractPos2 = new Vector2(distractPos.x, distractPos.z);

                Vector3 distract2Pos = GameObject.Find("DistractPoint2").transform.position;
                Vector2 distract2Pos2 = new Vector2(distract2Pos.x, distract2Pos.z);

                List<RRTResult> results = new List<RRTResult>();
                for (int it = 0; it < iterations; it++) {

                    

                        // We have this try/catch block here to account for the issue that we don't solve when we find a path when t is near the limit
                        try {

                            returned = rrtgeo.ComputeGeoFromPartials(startX, startY, endX, endY, minX, maxX, minY, maxY, 1000, attemps, attemps2, playerSpeed, distractPos2, distract2Pos2, tris);
                            returned.endTime = System.DateTime.Now;
                            nodes = returned.Nodes;
                            results.Add(returned);

                            // Did we found a path?
                            if (nodes.Count > 0) {
                                pathsgeo.Add(new PathGeo(nodes));
                                toggleStatusGeo.Add(pathsgeo.Last(), true);
                                pathsgeo.Last().color = new Color(UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f));

                                //Create path lines using the line objects 
                                List<Line> path = new List<Line>();
                                for (int i = 0; i < nodes.Count - 1; i++) {
                                    path.Add(new Line(nodes[i].GetVector3Draw(), nodes[i + 1].GetVector3Draw()));
                                }

                                GameObject temp = GameObject.Find("temp");
                                Color c = new Color(UnityEngine.Random.Range(0.0f, 1.0f),
                                       UnityEngine.Random.Range(0.0f, 1.0f),
                                       UnityEngine.Random.Range(0.0f, 1.0f));

                                /*foreach(Line l in path)
								{
									l.DrawVector(temp,c);
								}*/
                            }




                        }
                        catch (Exception e) {
                            Debug.LogWarning("Skip errored calculated path");
                            Debug.LogException(e);
                            // This can happen in two different cases:
                            // In line 376 by having a duplicate node being picked (coincidence picking the EndNode as visiting but the check is later on)
                            // We also cant just bring the check earlier since there's data to be copied (really really rare cases)
                            // The other case is yet unkown, but it's a conicidence by trying to insert a node in the tree that already exists (really rare cases)
                        }



                }
                printResultsToFile(results);
                

                // Compute the summary about the paths and print it
                String summary = "Summary:\n";
                summary += "Seed used:" + seed;
                summary += "\nSuccessful paths found: " + paths.Count;
                summary += "\nDead paths: " + deaths.Count;


                Debug.Log(summary);
            }


            if (GUILayout.Button("Compute Path Multi-Multi-Part Geo")) {
                if (enemygeoobjs == null) {
                    enemygeoobjs = GameObject.FindGameObjectsWithTag("EnemyGeo");
                    enemygeos = new List<EnemyGeo>();
                    foreach (GameObject g in enemygeoobjs) {
                        enemygeos.Add(g.GetComponent<EnemyGeo>());

                    }

                }
                rrtgeo.enemies = enemygeos;


                rrtgeo.casts = casts;
                triangles = GameObject.Find("Triangulation").GetComponent<Triangulation>();

                triangles.TriangulationSpace();
                List<Triangle> tris = triangles.triangles;


                float playerSpeed = GameObject.FindGameObjectWithTag("AI").GetComponent<Player>().speed;

                //Check the start and the end and get them from the editor. 
                if (start == null) {
                    start = GameObject.Find("Start");
                }
                if (end == null) {
                    end = GameObject.Find("End");
                }

                paths.Clear();

                float startX = start.transform.position.x;
                float startY = start.transform.position.z;
                float endX = end.transform.position.x;
                float endY = end.transform.position.z;

                GameObject floora = GameObject.Find("Floor");

                float minX = floora.GetComponent<Collider>().bounds.min.x;
                float maxX = floora.GetComponent<Collider>().bounds.max.x;
                float minY = floora.GetComponent<Collider>().bounds.min.z;
                float maxY = floora.GetComponent<Collider>().bounds.max.z;




                int seed = randomSeed;
                if (randomSeed != -1)
                    UnityEngine.Random.seed = randomSeed;
                else {
                    DateTime now = DateTime.Now;
                    seed = now.Millisecond + now.Second + now.Minute + now.Hour + now.Day + now.Month + now.Year;
                    UnityEngine.Random.seed = seed;
                }

                List<NodeGeo> nodes = null;
                RRTResult returned = null;

                Vector3 distractPos = GameObject.Find("DistractPoint").transform.position;
                Vector2 distractPos2 = new Vector2(distractPos.x, distractPos.z);

                Vector3 distract2Pos = GameObject.Find("DistractPoint2").transform.position;
                Vector2 distract2Pos2 = new Vector2(distract2Pos.x, distract2Pos.z);




                List<RRTResult> results = new List<RRTResult>();

                for (int it = 0; it < iterations; it++) {



                    // We have this try/catch block here to account for the issue that we don't solve when we find a path when t is near the limit
                    try {

                        //nodes= rrtgeo.ComputeGeo (startX, startY, endX, endY, minX, maxX, minY, maxY, 1000, attemps, playerSpeed, distractPos2, distract2Pos2);
                        returned = rrtgeo.ComputeGeosFromPartials(startX, startY, endX, endY, minX, maxX, minY, maxY, 1000, attemps, attemps2, playerSpeed, distractPos2, distract2Pos2, tris);
                        returned.endTime = System.DateTime.Now;
                        nodes = returned.Nodes;
                        results.Add(returned);
                        //nodes = rrt.Compute (startX, startY, endX, endY, attemps, stepSize, playerMaxHp, playerSpeed, playerDPS, fullMap, smoothPath);

                        //Debug.Log (nodes.Count);
                        if (nodes.Count <= 0) {
                            Debug.Log("RRT Search Failed");
                        }

                        // Did we found a path?
                        if (nodes.Count > 0) {
                            pathsgeo.Add(new PathGeo(nodes));
                            toggleStatusGeo.Add(pathsgeo.Last(), true);
                            //Debug.Log ("Count1 is : " + toggleStatusGeo.Count);
                            pathsgeo.Last().color = new Color(UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f));
                            //paths.Add (new Path (nodes));
                            //toggleStatus.Add (paths.Last (), true);
                            //paths.Last ().color = new Color (UnityEngine.Random.Range (0.0f, 1.0f), UnityEngine.Random.Range (0.0f, 1.0f), UnityEngine.Random.Range (0.0f, 1.0f));



                            //Create path lines using the line objects 
                            List<Line> path = new List<Line>();
                            for (int i = 0; i < nodes.Count - 1; i++) {
                                path.Add(new Line(nodes[i].GetVector3Draw(), nodes[i + 1].GetVector3Draw()));
                            }

                            GameObject temp = GameObject.Find("temp");
                            Color c = new Color(UnityEngine.Random.Range(0.0f, 1.0f),
                                   UnityEngine.Random.Range(0.0f, 1.0f),
                                   UnityEngine.Random.Range(0.0f, 1.0f));

                            /*foreach(Line l in path)
                            {
                                l.DrawVector(temp,c);
                            }*/

                        }




                    }
                    catch (Exception e) {
                        Debug.LogWarning("Skip errored calculated path");
                        Debug.LogException(e);
                        // This can happen in two different cases:
                        // In line 376 by having a duplicate node being picked (coincidence picking the EndNode as visiting but the check is later on)
                        // We also cant just bring the check earlier since there's data to be copied (really really rare cases)
                        // The other case is yet unkown, but it's a conicidence by trying to insert a node in the tree that already exists (really rare cases)
                    }



                }

                printResultsToFile(results);
                // Compute the summary about the paths and print it
                String summary = "Summary:\n";
                summary += "Seed used:" + seed;
                summary += "\nSuccessful paths found: " + paths.Count;
                summary += "\nDead paths: " + deaths.Count;


                Debug.Log(summary);
            }


            EditorGUILayout.LabelField ("");


			preCompWidth = EditorGUILayout.IntField(preCompWidth);
			preCompHeight = EditorGUILayout.IntField(preCompHeight);


			if (GUILayout.Button ("PreCompute Casts")){
				GameObject floora = GameObject.Find ("Floor");


				float minX = floora.GetComponent<Collider>().bounds.min.x;
				float maxX = floora.GetComponent<Collider>().bounds.max.x;
				float minY = floora.GetComponent<Collider>().bounds.min.z;
				float maxY = floora.GetComponent<Collider>().bounds.max.z;
				int width = preCompWidth;
				int height = preCompHeight;
			
				preComputeCasts(minX, maxX, minY, maxY, width, height);
			}

			colorCodeIndex = EditorGUILayout.IntField (colorCodeIndex);

			/*
			if (GUILayout.Button ("Color Code Waypoints")){


				GameObject waypoints = GameObject.Find ("Waypoints");
				List<Line> normals = new List<Line>();
				List<Line> toDistract = new List<Line>();
				List<Line> fromDistract = new List<Line>();
				Vector3 curPos = Vector3.zero;
				Vector3 nexPos = Vector3.zero;
				Vector3 disPos = Vector3.zero;


				foreach (WaypointGeo wp in waypoints.GetComponentsInChildren<WaypointGeo>()){
					curPos = wp.transform.position;
					nexPos = wp.next.transform.position;
					disPos = wp.distractPoint.transform.position;
					if(wp.type != "distract"){
						normals.Add(new Line(curPos, nexPos));
					}
					else{
						List<WaypointGeo> visited = new List<WaypointGeo>();

						fromDistract.Add (new Line(curPos, nexPos));
						WaypointGeo current = wp.next;
						WaypointGeo next = current.next;						
						curPos = wp.transform.position;
						nexPos = current.transform.position;
						fromDistract.Add (new Line(curPos, nexPos));
						bool check = false;
						while(!check){
							visited.Add (current);
							curPos = current.transform.position;
							nexPos = next.transform.position;
							fromDistract.Add ( new Line(curPos, nexPos));
							current = next;
							next = next.next;
							foreach(WaypointGeo way in visited){
								if(way == current){
									check = true;
								}
							}
						}


					}
					toDistract.Add (new Line(curPos, disPos));
				}

				//foreach (WaypointGeo waypoint in waypoints.GetComponentsInChildren<WaypointGeo>()){
				/*WaypointGeo waypoint = waypoints.GetComponentInChildren<WaypointGeo>();
				WaypointGeo current = waypoint.next;
				WaypointGeo next = current.next;

				curPos = waypoint.transform.position;
				nexPos = current.transform.position;
				normals.Add (new Line(curPos, nexPos));
				while(current != waypoint){
					curPos = current.transform.position;
					nexPos = next.transform.position;
					normals.Add ( new Line(curPos, nexPos));
					current = next;
					next = next.next;
				}
				//}



				GameObject temp = GameObject.Find ("temp");
				foreach(Line l in normals){
					l.DrawManArrow (temp, Color.blue, 2.0f);
				}
				foreach(Line l in toDistract){
					l.DrawManArrow (temp, Color.red, 4.0f);
				}
				foreach(Line l in fromDistract){
					l.DrawManArrow(temp, Color.green, 6.0f);
				}
				foreach(Line l in normals){
					l.DrawManArrow (temp, Color.blue, 2.0f);
				}
			}



			if (GUILayout.Button ("Color Code Waypoints Set")){

				
				GameObject waypoints = GameObject.Find ("Waypoints");
				List<Line> normals = new List<Line>();
				List<Line> toDistract = new List<Line>();
				List<Line> fromDistract = new List<Line>();
				Vector3 curPos = Vector3.zero;
				Vector3 nexPos = Vector3.zero;
				Vector3 disPos = Vector3.zero;
				
				
				foreach (WaypointGeo wp in waypoints.GetComponentsInChildren<WaypointGeo>()){
					curPos = wp.transform.position;
					nexPos = wp.next.transform.position;
					disPos = wp.distractPoints[colorCodeIndex].transform.position;
					toDistract.Add (new Line(curPos, disPos));
					if(wp.type != "distract"){
						normals.Add(new Line(curPos, nexPos));
					}
					else{
						List<WaypointGeo> visited = new List<WaypointGeo>();
						
						fromDistract.Add (new Line(curPos, nexPos));
						WaypointGeo current = wp.next;
						WaypointGeo next = current.next;						
						curPos = wp.transform.position;
						nexPos = current.transform.position;
						fromDistract.Add (new Line(curPos, nexPos));
						bool check = false;
						while(!check){
							visited.Add (current);
							curPos = current.transform.position;
							nexPos = next.transform.position;
							fromDistract.Add ( new Line(curPos, nexPos));
							current = next;
							next = next.next;
							foreach(WaypointGeo way in visited){
								if(way == current){
									check = true;
								}
							}
						}
						
						
					}

				}

				GameObject temp = GameObject.Find ("temp" + colorCodeIndex);
				foreach(Line l in normals){
					l.DrawManArrow (temp, Color.blue, 2.0f);
				}
				foreach(Line l in toDistract){
					l.DrawManArrow (temp, Color.red, 4.0f);
				}
				foreach(Line l in fromDistract){
					l.DrawManArrow(temp, Color.green, 6.0f);
				}
				foreach(Line l in normals){
					l.DrawManArrow (temp, Color.blue, 2.0f);
				}
			}*/

			if (GUILayout.Button ("Color Code Waypoints Set Via Guards")){
				
				
				GameObject waypoints = GameObject.Find ("Waypoints");
				GameObject guards = GameObject.Find ("Enemies");
				List<Line> normals = new List<Line>();
				List<Line> toDistract = new List<Line>();
				List<Line> fromDistract = new List<Line>();
				Vector3 curPos = Vector3.zero;
				Vector3 nexPos = Vector3.zero;
				Vector3 disPos = Vector3.zero;

                foreach (EnemyGeo eg in guards.GetComponentsInChildren<EnemyGeo>()) {
                    WaypointGeo current = eg.initialTarget;
                    WaypointGeo next = current.next;
                    curPos = eg.transform.position;
                    nexPos = current.transform.position;
                    normals.Add(new Line(curPos, nexPos));
                    List<WaypointGeo> visited = new List<WaypointGeo>();
                    bool check = false;
                    while (!check) {
                        visited.Add(current);
                        curPos = current.transform.position;
                        nexPos = current.next.transform.position;
                        disPos = current.distractPoints[colorCodeIndex].transform.position;
                        toDistract.Add(new Line(curPos, disPos));
                        normals.Add(new Line(curPos, nexPos));
                        current = next;
                        next = next.next;
                        foreach (WaypointGeo way in visited) {
                            if (way == current) {
                                check = true;
                            }
                        }
                    }


                    foreach (WaypointGeo wp in waypoints.GetComponentsInChildren<WaypointGeo>()) {
                        if (wp.type == "distract") {
                            visited = new List<WaypointGeo>();
                            current = wp;
                            next = current.next;
                            check = false;
                            while (!check) {
                                visited.Add(current);
                                curPos = current.transform.position;
                                nexPos = next.transform.position;
                                fromDistract.Add(new Line(curPos, nexPos));
                                current = next;
                                next = next.next;
                                foreach (WaypointGeo way in visited) {
                                    if (way == current) {
                                        check = true;
                                    }
                                }
                            }
                        }
                    }
                    normals = returnUniqueLineSet(normals);
                    toDistract = returnUniqueLineSet(toDistract);
                    fromDistract = returnUniqueLineSet(fromDistract);
                    GameObject temp = GameObject.Find("temp" + colorCodeIndex);
                    foreach (Line l in normals) {
                        DebugDrawLineUsingObjectsArrow(temp, l, Color.blue, 1.0f);


                        //l.DrawManArrow (temp, Color.blue, 2.0f);
                    }
                    foreach (Line l in toDistract) {
                        DebugDrawLineUsingObjectsArrow(temp, l, Color.red, 2.0f);
                        //l.DrawManArrow (temp, Color.red, 4.0f);
                    }
                    foreach (Line l in fromDistract) {
                        DebugDrawLineUsingObjectsArrow(temp, l, Color.green, 3.0f);
                        //l.DrawManArrow(temp, Color.green, 6.0f);
                    }
                    //foreach(Line l in normals){
                    //         DebugDrawLineUsingObjectsLine(temp, l, Color.blue, 2.0f);
                    //l.DrawManArrow (temp, Color.blue, 2.0f);
                    //}
                }
			}

			if (GUILayout.Button ("Clear Color Code Waypoints")){
				GameObject g = GameObject.Find ("temp" + colorCodeIndex);
				DestroyImmediate(g);
				g = new GameObject("temp" + colorCodeIndex);
			}

			EditorGUILayout.LabelField ("");
			EditorGUILayout.LabelField ("");





            #endregion experimental

            #endregion 4. Path

            /*
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
			
			if (GUILayout.Button ("Metric Road Map")) 
			{
				//Get the road map. 
				List<Line> roadMap; 

				Triangulation triangulation = GameObject.Find("Triangulation").GetComponent<Triangulation>();
				
				//Get the triangulation
				if(triangulation.roadMap.Count == 0)
				{
					triangulation.TriangulationSpace();   
				}
				roadMap = triangulation.roadMap;


				//Check if the map is precomputed
				if (playerPrefab == null) 
				{
					//timeSamples = 1; //TODO change back to original value, only there for debugging purposes.
					ComputeMap(); 
					fullMap = drawer.fullMap; 

				}
				
				//Compute eachLine. 
				foreach(Line l in roadMap)
				{
					Vector2 v1;
					Vector2 v2; 

					v1.x = (int)((l.vertex[0].x - floor.GetComponent<Collider>().bounds.min.x) / SpaceState.Editor.tileSize.x);
					v1.y = (int)((l.vertex[0].z - floor.GetComponent<Collider>().bounds.min.z) / SpaceState.Editor.tileSize.y);	

					v2.x = (int)((l.vertex[1].x - floor.GetComponent<Collider>().bounds.min.x) / SpaceState.Editor.tileSize.x);
					v2.y = (int)((l.vertex[1].z - floor.GetComponent<Collider>().bounds.min.z) / SpaceState.Editor.tileSize.y);
 					
 					

 					//Draw the line on the 2d map
					l.listGrid = ComputeLine(v1,v2);
				}
				
				//Get Absolute seen value. 
				foreach(Line l in roadMap)
				{

					foreach(Vector2 v in l.listGrid)
					{
						int count=0; 
						
						for(int t = 0; t<fullMap.Length; t++)
						{
							if(fullMap[t][(int)v.x][(int)v.y].seen)
							{
								count++; 
							}
						}
						l.valueGrid.Add(count); 
					}

				}
				//Find the most expansive segment
				int max = -1;
				foreach(Line l in roadMap)
				{
					if(l.valueGrid.Max()>max)
						max = l.valueGrid.Max(); 
				} 

				//Set there color and draw them
				GameObject g = GameObject.Find("Lines");
				if(g)
					GameObject.DestroyImmediate(g);
				g = new GameObject("Lines");	


				foreach(Line l in roadMap)
				{
					
					l.DrawVector(g,l.valueGrid.Max()/(float)max); 
				}

			}
			EditorGUILayout.LabelField ("");

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

			//GEO
			drawPathGeo = EditorGUILayout.Toggle ("Draw path Geo", drawPathGeo);
			
			if (drawer != null) 
			{
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
			}
			
			EditorGUILayout.LabelField ("");
			
			if (GUILayout.Button (playing ? "Stop" : "Play")) {
				playing = !playing;
			}
			
			EditorGUILayout.LabelField ("");
			
			/*if (GUILayout.Button ("Batch computation")) {
				BatchComputing ();
			}*/
            /*
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
				Analyzer.ComputePathsDangerValues (paths, SpaceState.Editor.enemies, floor.GetComponent<Collider>().bounds.min, SpaceState.Editor.tileSize.x, SpaceState.Editor.tileSize.y, original, drawer.seenNeverSeen, drawer.seenNeverSeenMax);
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
							Material m = new Material (player.GetComponent<Renderer>().sharedMaterial);
							m.color = p.Key.color;
							player.GetComponent<Renderer>().material = m;
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
			*/
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

				drawer.pathsGeo = toggleStatusGeo;
				//Debug.Log ("The count is:" + drawer.pathsGeo.Count);
				drawer.drawPathGeo = drawPathGeo;
				
			}
			
			if (original != null && lastTime != timeSlice) {
				lastTime = timeSlice;
				UpdatePositions (timeSlice, mapper);
			}
			
			SceneView.RepaintAll ();

			}


        private void printResultsToFile(List<RRTResult> rs) {
            DateTime now = DateTime.Now;
            string dateString = "" + now.Month + "" + now.Day + "" + now.Hour + "" + now.Minute + "" + now.Second;
            StreamWriter sw = File.CreateText("Result" + dateString + ".txt");//new StreamWriter("Result" + System.DateTime.Now + ".txt");
            sw.WriteLine("Text is being written");
            printSummaryResultsToFile(rs, sw);
            sw.WriteLine("--------------------------");
            sw.WriteLine("--------------------------");
            printIndividResultsToFile(rs, sw);
            sw.Close();
        }


        private void printSummaryResultsToFile(List<RRTResult> rs, StreamWriter sw) {
            int n = rs.Count;
            sw.WriteLine(n + " search attempts were made, on average the following data:");
            if (rs[0].type.Equals("normal")) {
                sw.WriteLine("The search type was normal");
                int sumRRTs = 0;
                int sumNodes = 0;
                int sumNodesRej = 0;
                int sumNodesWasted = 0;
                double sumTimes = 0.0;

                int sumRRTsF = 0;
                int sumNodesF = 0;
                int sumNodesRejF = 0;
                int sumNodesWastedF = 0;
                double sumTimesF = 0.0;

                int successes = 0;
                foreach (RRTResult r in rs) {
                    if (r.success) {
                        sumNodesWasted += r.nodesWasted;
                        sumRRTs += r.rrtsUsedMM[0];
                        sumNodes += r.nodesUsedMM[0];
                        sumNodesRej += r.nodesRejectedMM[0];
                        sumTimes += (r.endTime - r.startTime).TotalSeconds;
                        successes++;
                    }
                    else {
                        sumNodesWastedF += r.nodesWasted;
                        sumRRTsF += r.rrtsUsedMM[0];
                        sumNodesF += r.nodesUsedMM[0];
                        sumNodesRejF += r.nodesRejectedMM[0];
                        sumTimesF += (r.endTime - r.startTime).TotalSeconds;
                    }
                }
                double avNodesWasted = (double)sumNodesWasted / (double)successes;
                double avRRTs = (double)sumRRTs / (double)successes;
                double avNodes = (double)sumNodes / (double)successes;
                double avNodesRej = (double)sumNodesRej / (double)successes;
                double avTime =  sumTimes / (double)successes;
                double avSucc = (double)successes / (double)n;
                double f = (n - successes);
                double avNodesWastedF = (double)sumNodesWastedF / (double)f;
                double avRRTsF = (double)sumRRTsF / (double)f;
                double avNodesF = (double)sumNodesF / (double)f;
                double avNodesRejF = (double)sumNodesRejF / (double)f;
                double avTimeF = sumTimesF / (double)f;
                sw.WriteLine("Success,RRTs,Nodes,NodesRej,NodesWasted,Time,RRTsF,NodesF,NodesRejF,NodesWatedF,TimeF");
                sw.WriteLine(avSucc + "," + avRRTs + "," + avNodes + "," + avNodesRej + "," + avNodesWasted + "," + avTime + "," + avRRTsF + "," + avNodesF + "," + avNodesRejF + "," + avNodesWastedF + "," + avTimeF);
            }
            else if (rs[0].type.Equals("2-1")) {
                sw.WriteLine("The search type was single midpoint");
                int sumRRTs = 0;
                int sumNodes = 0;
                int sumNodesRej = 0;
                int sumRRTs2 = 0;
                int sumNodes2 = 0;
                int sumNodesRej2 = 0;
                double sumTimes = 0.0;
                int successes = 0;

                int sumRRTsF = 0;
                int sumNodesF = 0;
                int sumNodesRejF = 0;
                double sumTimesF = 0.0;
                int sumRRTs2F = 0;
                int sumNodes2F = 0;
                int sumNodesRej2F = 0;

                foreach (RRTResult r in rs) { 
                    if (r.success) {                        
                        sumRRTs += r.rrtsUsedMM[0];
                        sumNodes += r.nodesUsedMM[0];
                        sumNodesRej += r.nodesRejectedMM[0];
                        sumRRTs2 += r.rrtsUsed2MM[0];
                        sumNodes2 += r.nodesUsed2MM[0];
                        sumNodesRej2 += r.nodesRejected2MM[0];
                        sumTimes += (r.endTime - r.startTime).TotalSeconds;                    
                        successes++;
                    }
                    else {
                        sumRRTsF += r.rrtsUsedMM[0];
                        sumNodesF += r.nodesUsedMM[0];
                        sumNodesRejF += r.nodesRejectedMM[0];
                        sumRRTs2F += r.rrtsUsed2MM[0];
                        sumNodes2F += r.nodesUsed2MM[0];
                        sumNodesRej2F += r.nodesRejected2MM[0];
                        sumTimesF += (r.endTime - r.startTime).TotalSeconds;
                    }
                }
                double avRRTs = (double)sumRRTs / (double)successes;
                double avNodes = (double)sumNodes / (double)successes;
                double avNodesRej = (double)sumNodesRej / (double)successes;
                double avRRTs2 = (double)sumRRTs2 / (double)successes;
                double avNodes2 = (double)sumNodes2 / (double)successes;
                double avNodesRej2 = (double)sumNodesRej2 / (double)successes;
                double avTime = sumTimes / (double)successes;
                double avSucc = (double)successes / (double)n;
                double f = (n - successes);
                double avRRTsF = (double)sumRRTsF / (double)f;
                double avNodesF = (double)sumNodesF / (double)f;
                double avNodesRejF = (double)sumNodesRejF / (double)f;
                double avTimeF = sumTimesF / (double)f;
                double avRRTs2F = (double)sumRRTs2F / (double)f;
                double avNodes2F = (double)sumNodes2F / (double)f;
                double avNodesRej2F = (double)sumNodesRej2F / (double)f;

                sw.WriteLine("Success,RRTs,Nodes,NodesRej,RRTs2,Nodes2,NodesRej2,Time,RRTsF,NodesF,NodesRejF,RRTs2F,Nodes2F,NodesRej2F,TimeF,");
                sw.WriteLine(avSucc + "," + avRRTs + "," + avNodes + "," + avNodesRej + "," + avRRTs2 + "," + avNodes2 + "," + avNodesRej2 + "," + avTime + "," + avRRTsF + "," + avNodesF + "," + avNodesRejF + "," + avRRTs2F + "," + avNodes2F + "," + avNodesRej2F + "," + avTimeF);
            }
            else if (rs[0].type.Equals("2-2")) {
                sw.WriteLine("The search type was multi-midpoint");
                int sumRRTs = 0;
                int sumNodes = 0;
                int sumNodesRej = 0;
                int sumMidPoints = 0;
                int sumRRTs2 = 0;
                int sumNodes2 = 0;
                int sumNodesRej2 = 0;
                double sumTimes = 0.0;
                int successes = 0;

                int sumRRTsF = 0;
                int sumNodesF = 0;
                int sumNodesRejF = 0;
                int sumMidPointsF = 0;
                double sumTimesF = 0.0;
                int sumRRTs2F = 0;
                int sumNodes2F = 0;
                int sumNodesRej2F = 0;


                foreach (RRTResult r in rs) {
                    if (r.success) {
                        successes++;
                        sumMidPoints += r.rrtsUsedMM.Count;
                        for(int i = 0; i < r.rrtsUsedMM.Count; i++) { 
                            sumRRTs += r.rrtsUsedMM[i];
                            sumNodes += r.nodesUsedMM[i];
                            sumNodesRej += r.nodesRejectedMM[i];
                            sumRRTs2 += r.rrtsUsed2MM[i];
                            sumNodes2 += r.nodesUsed2MM[i];
                            sumNodesRej2 += r.nodesRejected2MM[i];
                        }

                        sumTimes += (r.endTime - r.startTime).TotalSeconds;
                    
                    }
                    else {
                        sumMidPointsF += r.rrtsUsedMM.Count;
                        for (int i = 0; i < r.rrtsUsedMM.Count; i++) {
                            sumRRTsF += r.rrtsUsedMM[i];
                            sumNodesF += r.nodesUsedMM[i];
                            sumNodesRejF += r.nodesRejectedMM[i];
                            sumRRTs2F += r.rrtsUsed2MM[i];
                            sumNodes2F += r.nodesUsed2MM[i];
                            sumNodesRej2F += r.nodesRejected2MM[i];
                        }

                        sumTimesF += (r.endTime - r.startTime).TotalSeconds;
                    }
                }
                double avMidPoints = (double)sumMidPoints / (double)successes;
                double avRRTs = (double)sumRRTs / (double)successes;
                double avNodes = (double)sumNodes / (double)successes;
                double avNodesRej = (double)sumNodesRej / (double)successes;
                double avRRTs2 = (double)sumRRTs2 / (double)successes;
                double avNodes2 = (double)sumNodes2 / (double)successes;
                double avNodesRej2 = (double)sumNodesRej2 / (double)successes;
                double avTime = sumTimes / (double)successes;
                double avSucc = (double)successes / (double)n;
                double f = n - successes;
                double avMidPointsF = (double)sumMidPointsF / (double)f;
                double avRRTsF = (double)sumRRTsF / (double)f;
                double avNodesF = (double)sumNodesF / (double)f;
                double avNodesRejF = (double)sumNodesRejF / (double)f;
                double avTimeF = sumTimesF / (double)f;
                double avRRTs2F = (double)sumRRTs2F / (double)f;
                double avNodes2F = (double)sumNodes2F / (double)f;
                double avNodesRej2F = (double)sumNodesRej2F / (double)f;

                sw.WriteLine("Success,RRTs,Nodes,NodesRej,RRTs2,Nodes2,NodesRej2,Mids,Time,RRTsF,NodesF,NodesRejF,RRTs2F,Nodes2F,NodesRej2F,MidsF,TimeF,");
                sw.WriteLine(avSucc + "," + avRRTs + "," + avNodes + "," + avNodesRej + "," + avRRTs2 + "," + avNodes2 + "," + avNodesRej2 + ","  + avMidPoints + "," + avTime + "," + avRRTsF + "," + avNodesF + "," + avNodesRejF + "," + avRRTs2F + "," + avNodes2F + "," + avNodesRej2F + "," + avMidPointsF + "," + avTimeF);
            }

            
        }

        private void printIndividResultsToFile(List<RRTResult> rs, StreamWriter sw) {
            foreach (RRTResult r in rs) {
                if (r.type.Equals("normal")) {
                    
                    //sw.WriteLine("There were " + r.rrtsUsedMM[0] + " tree searches used attempts made");
                    //sw.WriteLine(r.nodesUsedMM[0] + " nodes were used");
                    //sw.WriteLine(r.nodesRejectedMM[0] + " nodes were rejected for already being other nodes");
                    TimeSpan timedif = r.endTime - r.startTime;
                    //sw.WriteLine("The search took " + timedif);
                    //sw.WriteLine("The success of the search was " + r.success);
                    string sults = "";
                    sults = sults + r.rrtsUsedMM[0] + "," + r.nodesUsedMM[0] + "," + r.nodesWasted + "," + timedif.TotalSeconds;
                    if (r.success) {
                        sults = sults + "," + 1;
                    }
                    else {
                        sults = sults + "," + 0;
                    }
                    sw.WriteLine(sults);
                }
                else if (r.type.Equals("2-1")) {
                    
                    /*sw.WriteLine(r.rrtsUsedMM[0] + "tree searches were made for the first half");
                    sw.WriteLine(r.nodesUsedMM[0] + " nodes were used for the first half");
                    sw.WriteLine(r.nodesRejectedMM[0] + " nodes were rejected for already being other nodes in the first half");

                    sw.WriteLine(r.rrtsUsed2MM[0] + "tree searches were made for the first half");
                    sw.WriteLine(r.nodesUsed2MM[0] + " nodes were used for the first half");
                    sw.WriteLine(r.nodesRejected2MM[0] + " nodes were rejected for already being other nodes in the first half");
                    */
                    TimeSpan timedif = r.endTime - r.startTime;
                    /*
                    sw.WriteLine("The search took " + timedif);
                    sw.WriteLine("The success of the search was " + r.success);
                    */
                    string sults = "";
                    sults = sults + r.rrtsUsedMM[0] + "," + r.nodesUsedMM[0] + "," + r.rrtsUsed2MM[0] + "," + r.nodesUsed2MM[0] + "," + timedif.TotalSeconds;
                    if (r.success) {
                        sults = sults + "," + 1;
                    }
                    else {
                        sults = sults + "," + 0;
                    }
                    sw.WriteLine(sults);


                }
                else if (r.type.Equals("2-2")) {
                    
                   /* sw.WriteLine("There were " + r.rrtsUsedMM.Count + " midPoints used");
                    for (int i = 0; i < r.rrtsUsedMM.Count; i++) {
                        sw.WriteLine("For midpoint  " + i + ":");
                        sw.WriteLine(r.rrtsUsedMM[i] + "tree searches were made for the first half");
                        sw.WriteLine(r.nodesUsedMM[i] + " nodes were used for the first half");
                        sw.WriteLine(r.nodesRejectedMM[i] + " nodes were rejected for already being other nodes in the first half");

                        sw.WriteLine(r.rrtsUsed2MM[i] + "tree searches were made for the first half");
                        sw.WriteLine(r.nodesUsed2MM[i] + " nodes were used for the first half");
                        sw.WriteLine(r.nodesRejected2MM[i] + " nodes were rejected for already being other nodes in the first half");
                    }*/
                    TimeSpan timedif = r.endTime - r.startTime;
                    /*sw.WriteLine("The search took " + timedif);
                    sw.WriteLine("The success of the search was " + r.success);*/

                    string sults = "";

                    int rrtsUsed = 0;
                    int rrtsUsed2 = 0;
                    int nodesUsed = 0;
                    int nodesUsed2 = 0;
                    for(int i = 0; i< r.rrtsUsedMM.Count; i++) {
                        rrtsUsed = rrtsUsed + r.rrtsUsedMM[i];
                        rrtsUsed2 = rrtsUsed2 + r.rrtsUsed2MM[i];
                        nodesUsed = nodesUsed + r.nodesUsedMM[i];
                        nodesUsed2 = nodesUsed2 + r.nodesUsed2MM[i];
                    }

                   
                    sults = sults + rrtsUsed + "," +nodesUsed + "," +rrtsUsed2 + "," + nodesUsed2 + "," + timedif.TotalSeconds;
                    if (r.success) {
                        sults = sults + "," + 1;
                    }
                    else {
                        sults = sults + "," + 0;
                    }
                    sw.WriteLine(sults);



                }
                //sw.WriteLine("--------------------------------");






            }
        }




        private void DebugDrawLineUsingObjectsArrow(GameObject parent, Line l, Color c, float thickness) {

            float fractionSizeArrow = 0.1f;
            float angleArrow = 15f;


            DebugDrawLineUsingObjectsLine(parent, l, c, thickness);

            Vector3 startPoint = l.vertex[0];
            Vector3 endPoint = l.vertex[1];
            //float dist = Vector2.Distance(startPoint, endPoint);
            //float size = fractionSizeArrow * dist;


            Vector3 linePoint1 = endPoint + ((startPoint - endPoint) * fractionSizeArrow);
            Vector3 linePoint2 = linePoint1;

            linePoint1.x = endPoint.x + ((linePoint1.x - endPoint.x) * Mathf.Cos(Mathf.Deg2Rad * angleArrow)) - ((linePoint1.z - endPoint.z) * Mathf.Sin(Mathf.Deg2Rad * angleArrow));
            linePoint1.z = endPoint.z + ((linePoint1.x - endPoint.x) * Mathf.Sin(Mathf.Deg2Rad * angleArrow)) + ((linePoint1.z - endPoint.z) * Mathf.Cos(Mathf.Deg2Rad * angleArrow));

            linePoint2.x = endPoint.x + ((linePoint2.x - endPoint.x) * Mathf.Cos(Mathf.Deg2Rad * -angleArrow)) - ((linePoint2.z - endPoint.z) * Mathf.Sin(Mathf.Deg2Rad * -angleArrow));
            linePoint2.z = endPoint.z + ((linePoint2.x - endPoint.x) * Mathf.Sin(Mathf.Deg2Rad * -angleArrow)) + ((linePoint2.z - endPoint.z) * Mathf.Cos(Mathf.Deg2Rad * -angleArrow));

            Line l1 = new Line(linePoint1, endPoint);
            Line l2 = new Line(linePoint2, endPoint);

            DebugDrawLineUsingObjectsLine(parent, l1, c, thickness);
            DebugDrawLineUsingObjectsLine(parent, l2, c, thickness);



        }

        private void DebugDrawLineUsingObjectsLine(GameObject parent, Line l, Color c, float thickness) {
            Vector3 l1 = l.vertex[0];
            Vector3 l2 = l.vertex[1];
            GameObject lin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lin.GetComponent<Renderer>().sharedMaterial.color = Color.red;
            lin.transform.parent = parent.transform;
            lin.transform.position = (l1 + l2) / 2.0f;
            lin.transform.position = new Vector3(lin.transform.position.x, 0.05f * lin.transform.position.y, lin.transform.position.z);
            Vector3 dists = (l1 - l2);
            dists.y = 0.05f * dists.y;

            Vector3 from = Vector3.right;
            Vector3 to = dists / dists.magnitude;

            Vector3 axis = Vector3.Cross(from, to);
            float angle = Mathf.Rad2Deg * Mathf.Acos(Vector3.Dot(from, to));
            lin.transform.RotateAround(lin.transform.position, axis, angle);


            Vector3 scale = Vector3.one;
            scale.x = Vector3.Magnitude(dists);
            scale.z = 0.2f*thickness;
            scale.y = 0.2f;

            lin.transform.localScale = scale;

            lin.GetComponent<Renderer>().material.color = c;
        }

        private void DebugDrawLineUsingObjects(GameObject parent, Vector3 l1, Vector3 l2) {
            GameObject lin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lin.GetComponent<Renderer>().sharedMaterial.color = Color.red;
            lin.transform.parent = parent.transform;
            lin.transform.position = (l1 + l2) / 2.0f;
            lin.transform.position = new Vector3(lin.transform.position.x, 0.05f * lin.transform.position.y, lin.transform.position.z);
            Vector3 dists = (l1 - l2);
            dists.y = 0.05f * dists.y;

            Vector3 from = Vector3.right;
            Vector3 to = dists / dists.magnitude;

            Vector3 axis = Vector3.Cross(from, to);
            float angle = Mathf.Rad2Deg * Mathf.Acos(Vector3.Dot(from, to));
            lin.transform.RotateAround(lin.transform.position, axis, angle);


            Vector3 scale = Vector3.one;
            scale.x = Vector3.Magnitude(dists);
            scale.z = 0.2f;
            scale.y = 0.2f;

            lin.transform.localScale = scale;
        }

		private List<Line> returnUniqueLineSet(List<Line> lines){

			List<Line> toReturn = new List<Line>();
			foreach(Line l in lines){
				Vector3 start = l.vertex[0];
				Vector3 end = l.vertex[1];
				if(Vector3.Equals(start, end)){
				}
				else if(toReturn.Count < 1){
					toReturn.Add (l);
				}
				else{
					bool notEqual = true;
					foreach(Line l2 in toReturn){
						if(Vector3.Equals (l.vertex[0], l2.vertex[0]) && Vector3.Equals (l.vertex[1], l2.vertex[1])){

							notEqual = false;
							break;
						}
						else{

						}
					}
					if(notEqual){
						toReturn.Add (l);
					}
				}
			}
			return toReturn;
		}

		public preCast casts;

		private void preComputeCasts(float minX, float maxX, float minY, float maxY, int width, int height){

            DateTime now = DateTime.Now;

			float startX = Mathf.Floor(minX);
			float startY = Mathf.Floor(minY);
			float stepX = ((maxX - minX) / width);
			float stepY = ((maxY - minY) / height);
			float curX1 = startX;
			float curY1 = startY;
			float curX2 = startX;
			float curY2 = startY;
			bool[,,,] castsA = new bool[width,height,width,height];

			Debug.Log (stepX);
			Debug.Log (stepY);

			Debug.Log (curX1);
			Debug.Log(curY1);
			Debug.Log (curX2);
			Debug.Log (curY2);

			for (int i = 0; i < width; i++){
				curY1 = startY;
				for (int j = 0; j < height; j++){
					curX2 = startX;
					for (int k = 0; k < width; k++){
						curY2 = startY;
						for(int l = 0; l < height; l++){

							Vector3 start = new Vector3(curX1, 0, curY1);
							Vector3 end = new Vector3(curX2, 0, curY2);
							int layerMask = 1 << 8;
							castsA[i,j,k,l] = Physics.Linecast (start, end, layerMask);
							curY2 = curY2 + stepY;
						}
						curX2 = curX2 + stepX;
					}
					curY1 = curY1 + stepY;
				}
				curX1 = curX1 + stepX;
			}

			Debug.Log (curX1);
			Debug.Log(curY1);
			Debug.Log (curX2);
			Debug.Log (curY2);

			casts = new preCast(minX, maxX, minY, maxY, width, height, stepX, stepY, castsA);
            Debug.Log((DateTime.Now - now).TotalSeconds);

		}


		private void printLatestPath(){
			List<NodeGeo> path = pathsgeo.Last().points;
			Debug.Log ("This is the latest Path");
			NodeGeo current = path.First();
			for(int i = 0; i < path.Count; i++){
				current = path[i];
				Debug.Log (current.ToString());
			}
			NodeGeo last = path.Last();
			Debug.Log ("distractTimes");
			foreach(int j in last.distractTimes){
				Debug.Log (j);
			}
			Debug.Log ("distractNums");
			foreach(int j in last.distractNums){
				Debug.Log (j);
			}
		}

		private void goToFrame(int t){
			if(useDist){
				//Debug.Log (distTime);
				foreach(EnemyGeo e in enemygeos){
					e.goToFrameDist(t, distTime);
				}
			}
			else if(useDists){
				List<NodeGeo> path;
				try{
					path = pathsgeo.Last().points;
				}
				catch(Exception e){
					return;
				}
				NodeGeo last = path.Last ();
				List<int> distTimes = last.distractTimes;
				List<int> distNums = last.distractNums;



				foreach(EnemyGeo e in enemygeos){
					e.goToFrameDistsN(t, distTimes, distNums);
				}
			}
			else{
				foreach(EnemyGeo e in enemygeos){
					e.goToFrame(t);
				}
			}
			goToFrameP(t);
		}

		private void goToFrameP(int t){
			List<NodeGeo> path;
			try{
				path = pathsgeo.Last().points;
			}
			catch(Exception e){
				return;
			}
			NodeGeo current = path.First();
			Vector2 position = Vector2.zero;
			bool lerped = false;
			if(t > path.Last().t){
				return;
			}
			for(int i = 0; i < path.Count; i++){
				current = path[i];
				if(current.t > t){
					lerped = true;
					position = Vector2.Lerp(current.parent.GetVector2(), current.GetVector2(), (((float)t) - ((float)current.parent.t)) / (((float)current.t) - ((float)current.parent.t)));
					break;
				}
			}




			GameObject P = GameObject.Find ("Player");
			Vector3 position3 = P.transform.position;
			position3.x = position.x;
			position3.z = position.y;
			if(lerped){
				P.transform.position = position3;
			}
			NodeGeo last = path.Last();
			if(last.t == t){
				position3.x = last.x;
				position3.z = last.y;
				P.transform.position = position3;
			}
		}

		public void Update(){
			/*if(!clean){
				cleanUp();
				clean = true;
			}
			
			if(prevDrawPaths != drawPaths){
				DestroyImmediate(paths);
				paths = new GameObject("paths");
				paths.transform.parent = players.transform;
				if(drawPaths){
					foreach(movementModel model in mModels){
						if(model != null){
							model.drawPath(paths);
						}
					}
				}
			}
			prevDrawPaths = drawPaths;*/
			
			

			if(playingGeo){
				ignoreFrameLimit = true;
				if(realFrame != curFrame){
					
					goToFrame(curFrame);
					realFrame = curFrame;
				}
				else if(curFrame <= totalFrames || ignoreFrameLimit){
					curFrame++;
					realFrame = curFrame;
					goToFrame(curFrame);
					//Debug.Log (curFrame);
					//MOVE STUFF!

					//if(!isOne){
					//	goToFrame(curFrame);
					//}
					
				}
				
			}
			else{
				if(realFrame != curFrame){
					goToFrame(curFrame);
					realFrame = curFrame;
				}
			}
		}
	


		#region notmineL

		public List<Vector2> ComputeLine(Vector2 v1, Vector2 v2)
		{
			List<Vector2> cells = new List<Vector2>();  
			int x0 = (int)v1.x; int y0 = (int)v1.y;
			int x1 = (int)v2.x; int y1 = (int)v2.y;
			
			int sx, sy;
    
	        if(x0<x1)
	            sx = 1;
	        else
	            sx = -1;
	        
	        if(y0<y1)
	            sy = 1;
	        else
	            sy = -1;

	        int dx =  Math.Abs(x1-x0);
	        int dy = -Math.Abs(y1-y0);
	        int err = dx + dy ;
	        int e2 = 0; 
	        
	        while(true)
	        {
	            //fullMap[0][x0][y0].seen = true;
	            cells.Add(new Vector2(x0, y0));
	            if (x0==x1 && y0==y1) 
	                break;

	            e2 = 2 * err;

	            if (e2 >= dy) 
	            { 
	                err += dy; 
	                x0 += sx; 
	            }
	            if (e2 <= dx) 
	            { 
	                err += dx; 
	                y0 += sy; 
	            }
	        }
			return cells; 
		}

		public void ComputeMap()
		{
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
					//drawer.hideFlags = HideFlags.HideInInspector;
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
			
			original = mapper.PrecomputeMaps (SpaceState.Editor, floor.GetComponent<Collider>().bounds.min, floor.GetComponent<Collider>().bounds.max, gridSize, gridSize, timeSamples, stepSize, ticksBehind, baseMap);

			drawer.fullMap = original;
			float maxSeenGrid;
			drawer.seenNeverSeen = Analyzer.ComputeSeenValuesGrid (original, out maxSeenGrid);
			drawer.seenNeverSeenMax = maxSeenGrid;
			drawer.tileSize = SpaceState.Editor.tileSize;
			drawer.zero.Set (floor.GetComponent<Collider>().bounds.min.x, floor.GetComponent<Collider>().bounds.min.z);
			
			ResetAI ();
			previous = DateTime.Now;

		}
		/*public void Update () {
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
		}*/
		
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
								
					fullMap = mapper.PrecomputeMaps (SpaceState.Editor, floor.GetComponent<Collider>().bounds.min, floor.GetComponent<Collider>().bounds.max, gridSize, gridsize, timesamples, stepSize);
								
					startX = (int)((start.transform.position.x - floor.GetComponent<Collider>().bounds.min.x) / SpaceState.Editor.tileSize.x);
					startY = (int)((start.transform.position.z - floor.GetComponent<Collider>().bounds.min.z) / SpaceState.Editor.tileSize.y);
					endX = (int)((end.transform.position.x - floor.GetComponent<Collider>().bounds.min.x) / SpaceState.Editor.tileSize.x);
					endY = (int)((end.transform.position.z - floor.GetComponent<Collider>().bounds.min.z) / SpaceState.Editor.tileSize.y);
	
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
						
					fullMap = mapper.PrecomputeMaps (SpaceState.Editor, floor.GetComponent<Collider>().bounds.min, floor.GetComponent<Collider>().bounds.max, gridSize, gridSize, timesamples, stepSize);
						
					startX = (int)((start.transform.position.x - floor.GetComponent<Collider>().bounds.min.x) / SpaceState.Editor.tileSize.x);
					startY = (int)((start.transform.position.z - floor.GetComponent<Collider>().bounds.min.z) / SpaceState.Editor.tileSize.y);
					endX = (int)((end.transform.position.x - floor.GetComponent<Collider>().bounds.min.x) / SpaceState.Editor.tileSize.x);
					endY = (int)((end.transform.position.z - floor.GetComponent<Collider>().bounds.min.z) / SpaceState.Editor.tileSize.y);
						
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
	
				fullMap = mapper.PrecomputeMaps (SpaceState.Editor, floor.GetComponent<Collider>().bounds.min, floor.GetComponent<Collider>().bounds.max, gridSize, gridSize, timesamples, stepSize);
					
				startX = (int)((start.transform.position.x - floor.GetComponent<Collider>().bounds.min.x) / SpaceState.Editor.tileSize.x);
				startY = (int)((start.transform.position.z - floor.GetComponent<Collider>().bounds.min.z) / SpaceState.Editor.tileSize.y);
				endX = (int)((end.transform.position.x - floor.GetComponent<Collider>().bounds.min.x) / SpaceState.Editor.tileSize.x);
				endY = (int)((end.transform.position.z - floor.GetComponent<Collider>().bounds.min.z) / SpaceState.Editor.tileSize.y);
					
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
						pos.x += floor.GetComponent<Collider>().bounds.min.x;
						pos.z += floor.GetComponent<Collider>().bounds.min.z;
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
		
		#endregion notmineL
	
	}
}