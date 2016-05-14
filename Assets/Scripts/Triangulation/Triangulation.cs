using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System; 
using Vectrosity;

[ExecuteInEditMode]
public class Triangulation : MonoBehaviour 
{
	//Data holder to display and save
	[HideInInspector]
	public List<Vector3> points = new List<Vector3>();
	[HideInInspector]
	public List<Color> colours = new List<Color>();
	// Use this for initialization
	[HideInInspector]

	public List<Triangle> triangles = new List<Triangle>(); 
	[HideInInspector]
	public List<Line> lines = new List<Line>(); 
	[HideInInspector]

	public List<Line> linesMinSpanTree = new List<Line>(); 
	[HideInInspector]

	public List<Geometry> obsGeos = new List<Geometry> (); 
	//Contains Map
	[HideInInspector]
	public Geometry mapBG = new Geometry ();

	public bool drawTriangles = false; 
	public bool drawRoadMap = false; 
	public bool drawMinSpanTree = false;
	public bool stopAll = false;
	[HideInInspector]

	public List<int>[] G = new List<int>[110];
	[HideInInspector]

	public int[] colorG = new int[110];
	[HideInInspector]

	public bool[] visitedG = new bool[110];
	public const int red = 1;
	public const int green = 2;
	public const int blue = 3;
	[HideInInspector]

	public List<Line> roadMap = new List<Line>();

	//Stuff for alex
	[HideInInspector]
	List<Geometry> toReturn = new List<Geometry> ();


    private float threshold = 0.00001f;

	public void Start(){
	
	}

	public void Clear()
	{
		linesMinSpanTree.Clear(); 
		triangles.Clear(); 
		lines.Clear(); 
		points.Clear(); 
		colours.Clear();
		obsGeos.Clear ();
		roadMap.Clear(); 
		toReturn.Clear(); 

		GameObject temp = GameObject.Find("temp"); 
		DestroyImmediate(temp); 


		stopAll = true;
	}
	void OnDrawGizmosSelected( ) 
	{
		//return; 
		//Debug.Log(colours.Count);
		//Debug.Log(points.Count);
		//var i = 0;
		foreach(Vector3 v in points)
		{

			//Gizmos.color = colours[i];
			//Gizmos.color = Color.red;
			Gizmos.DrawSphere (v, 0.25f);
			//i++; 
		}

		//Gizmos.color = Color.red;
		//Gizmos.DrawSphere (new Vector3(0,2,0), 1);
	}
	public void Update()
	{
		//TODO: move to vectrocity for the drawing. 
		return;

        /*
		//return; 
		//points.Clear(); 
		//colours.Clear();  
		if ( stopAll )
			return;


		if(drawMinSpanTree)
		{
			GameObject temp = GameObject.Find("temp"); 
			foreach(Line l in linesMinSpanTree)
			{
				//l.DrawLine(Color.blue); 
				//l.DrawVector( temp );
				Debug.DrawLine(l.vertex[0], l.vertex[1],Color.red);
			}
		}

		foreach(Triangle tt in triangles)
		{
			//triangulation.points.Add(tt.GetCenterTriangle());
			//triangulation.colours.Add(Color.cyan); 
			if(drawTriangles)
			{	

				tt.DrawDebug();
				foreach(Vector3 v in tt.getVertexMiddle())
				{
				//	points.Add(v);
				}

				foreach(Color v in tt.colourVertex)
				{
				//	colours.Add(v);
				}
			}

			if(drawRoadMap)
			{
				Line[] ll = tt.GetSharedLines(); 
			

				if(ll.Length == 1)
				{
					
					Debug.DrawLine(ll[0].MidPoint(), tt.GetCenterTriangle(),Color.red);
					//Debug.Log("Drawing Red Line at: " + ll[0].MidPoint() + " " + tt.GetCenterTriangle());
				}
				else if(ll.Length > 2)
				{
					for(int i = 0; i<ll.Length; i++)
					{
						Debug.DrawLine(ll[i].MidPoint(), tt.GetCenterTriangle(),Color.red);
						//Debug.Log("Drawing Red Line at: " + ll[i].MidPoint() + " " + tt.GetCenterTriangle());
					}

				}
				
				else
				{
					for(int i = 0; i<ll.Length; i++)
					{
						Debug.DrawLine(ll[i].MidPoint(), ll[(i+1) % ll.Length].MidPoint(),Color.red);
					}
				}
			}
		}*/

	}

	public void AddPoint(Vector3 v)
	{
		points.Add(v); 
		colours.Add(Color.cyan); 
	}

	public void AddPoint(Vector3 v,Color c)
	{
		points.Add(v); 
		colours.Add(c); 
	}

    private int infiniLoopStop = 1000;


	public List<Geometry> TriangulationSpace ()
	{

		if (toReturn.Count>0)
			return toReturn; 

		//Compute one step of the discritzation
		//Find this is the view
		GameObject floor = (GameObject)GameObject.Find ("Floor");
			
		Vector3 [] vertex = new Vector3[4]; 
		
		//First geometry is the outer one
		List<Geometry> geos = new List<Geometry> ();

		
		//Drawing lines
		//VectorLine.SetCamera3D(Camera.current); 

		//Floor
		//Vector3[] f = new Vector3[4];
		MeshFilter mesh = (MeshFilter)(floor.GetComponent ("MeshFilter"));
		Vector3[] t = mesh.sharedMesh.vertices; 
		
		Geometry tempGeometry = new Geometry (); 

		
		vertex [0] = mesh.transform.TransformPoint (t [0]);
		vertex [2] = mesh.transform.TransformPoint (t [120]);
		vertex [1] = mesh.transform.TransformPoint (t [110]);
		vertex [3] = mesh.transform.TransformPoint (t [10]);
		
		vertex [0].y = 1; 
		vertex [1].y = 1; 
		vertex [2].y = 1; 
		vertex [3].y = 1; 
		//these were in tempGeometry previously

		//Disabled Temporarily - Find a way to avoid floor when checking for obstacle collision
		//geos.Add (tempGeometry);

		Vector3 [] mapBoundary = new Vector3[4]; //the map's four corners

		for (int i = 0; i < 4; i++) {
			mapBoundary [i] = vertex [i];
		}

		Geometry mapBG = new Geometry (); 
		for (int i = 0; i < 4; i++)
			mapBG.edges.Add( new Line( mapBoundary[i], mapBoundary[(i + 1) % 4]) );
		//mapBG.DrawVertex (GameObject.Find ("temp"));
		//mapBG.DrawGeometry(GameObject.find);

		GameObject[] obs = GameObject.FindGameObjectsWithTag ("Obs");
		if(obs == null)
		{
			Debug.Log("Add tag geos to the geometries"); 
			return null; 
		}
		//data holder
		Triangulation triangulation = GameObject.Find ("Triangulation").GetComponent<Triangulation> (); 
		triangulation.points.Clear ();
		triangulation.colours.Clear (); 
		
		//Only one geometry for now
		
		foreach (GameObject o in obs) {
			mesh = (MeshFilter)(o.GetComponent ("MeshFilter"));
			t = mesh.sharedMesh.vertices; 
			tempGeometry = new Geometry();

			vertex [0] = mesh.transform.TransformPoint (t [6]);
			vertex [1] = mesh.transform.TransformPoint (t [8]);
			vertex [3] = mesh.transform.TransformPoint (t [7]);
			vertex [2] = mesh.transform.TransformPoint (t [9]);
			
			vertex [0].y = 1;
			vertex [2].y = 1;
			vertex [1].y = 1;
			vertex [3].y = 1;
			for (int i = 0; i< vertex.Length; i+=1) {
				if (i < vertex.Length - 1)
				    tempGeometry.edges.Add (new Line (vertex [i], vertex [i + 1]));
				else 	       
					tempGeometry.edges.Add (new Line (vertex [0], vertex [i]));
			}	
			geos.Add (tempGeometry); 
		}
		
		//lines are defined by all the points in  obs
		lines = new List<Line> ();

		obsGeos.Clear ();
		foreach (Geometry g in geos) {
			obsGeos.Add(g);
		}


		//Create empty GameObject
		GameObject temp = GameObject.Find("temp");
		DestroyImmediate(temp);
		temp = new GameObject("temp");

        /* GameObject parent1 = new GameObject("DebugParent1");
         foreach (Geometry aki in obsGeos)
         { 
             foreach (Line l in aki.edges)
             {
                 //Debug.Log(l.vertex[0] + "," + l.vertex[1]);

                 GameObject lin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                 lin.GetComponent<Renderer>().sharedMaterial.color = Color.red;
                 lin.transform.parent = parent1.transform;
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
         }
         */



        //CODESPACE
        //Merging Polygons
        /*  for (int i = 0; i < obsGeos.Count; i++) {
              for (int j = i + 1; j < obsGeos.Count; j++) {
                  //check all line intersections
                  if( obsGeos[i].GeometryIntersect( obsGeos[j] ) ){
                      Debug.Log(obsGeos.Count);
                      //Debug.Log("Geometries Intersect: " + i + " " + j);
                      Geometry tmpG = obsGeos[i].GeometryMerge( obsGeos[j] ); 
                      //remove item at position i, decrement i since it will be increment in the next step, break
                      obsGeos.RemoveAt(j);
                      obsGeos.RemoveAt(i);
                      obsGeos.Add(tmpG);
                      i--;
                      Debug.Log(obsGeos.Count);
                      break;
                  }
              }
          }*/


        /*GameObject parentUO = new GameObject("UnOrderedObs");
        foreach (Geometry G in obsGeos) {
            GameObject geo = new GameObject("geo");
            geo.transform.parent = parentUO.transform;
            foreach (Line l in G.edges) {
                //foreach( Line l in mapBG.edges){
                GameObject lin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lin.GetComponent<Renderer>().sharedMaterial.color = Color.red;
                lin.transform.parent = geo.transform;
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
        }*/


        obsGeos.Sort();
        /*GameObject parentO = new GameObject("OrderedObs");
        foreach (Geometry G in obsGeos) {
            GameObject geo = new GameObject("geo");
            geo.transform.parent = parentO.transform;
            foreach (Line l in G.edges) {
                //foreach( Line l in mapBG.edges){
                GameObject lin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lin.GetComponent<Renderer>().sharedMaterial.color = Color.red;
                lin.transform.parent = geo.transform;
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
        }*/



        bool done = false;
        bool difFound = false;
        string preS = "1";
        int preI = 1;
        //GameObject mergeys = new GameObject("Mergeys");
        int whileIndex = 1;
        int numMerge = 0;
        while (!done)
        {
            obsGeos.Sort();
           for(int i = 0; i < obsGeos.Count; i++) {
                if (difFound)
                {
                    break;
                }
                for(int j = i+1; j < obsGeos.Count; j++) {
                    if (obsGeos[i].GeometryIntersect(obsGeos[j]))
                    {
                        //if(preI == 33) { 
                        //if (preI == 28) {
                        //    Debug.Log("DEBUGGERATION ACTIVATED");
                        //    obsGeos[i].debuggery = true;
                       //     obsGeos[j].debuggery = true;
                            //Debug.Log(obsGeos[i].debuggery);
                            //Debug.Log(obsGeos[j].debuggery);
                        //}
                        numMerge++;
                        //Debug.Log("NEW MERGE NUMBER:" + numMerge);
                        Geometry tmpG = obsGeos[i].GeometryMerge(obsGeos[j]);
                        //Debug.Log("MERGE COMPLETE OF NUMBER:" + numMerge);
                        obsGeos[i].debuggery = false;
                        obsGeos[j].debuggery = false;
                        preS = preI.ToString();
                        /*GameObject merger = new GameObject(preS);
                        GameObject geo1 = new GameObject(preS + "geo1");
                        GameObject geo2 = new GameObject(preS + "geo2");
                        GameObject geoM = new GameObject(preS + "geoM");
                        merger.transform.parent = mergeys.transform;
                        geo1.transform.parent = merger.transform;
                        geo2.transform.parent = merger.transform;
                        geoM.transform.parent = merger.transform;
                        preI++;/**/
                        
                       /*
                       foreach (Line l in obsGeos[i].edges)
                        {
                            GameObject lin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            lin.GetComponent<Renderer>().sharedMaterial.color = Color.red;
                            lin.transform.parent = geo1.transform;
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
                        foreach (Line l in obsGeos[j].edges)
                        {
                            GameObject lin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            lin.GetComponent<Renderer>().sharedMaterial.color = Color.red;
                            lin.transform.parent = geo2.transform;
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
                        foreach (Line l in tmpG.edges)
                        {
                            GameObject lin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            lin.GetComponent<Renderer>().sharedMaterial.color = Color.red;
                            lin.transform.parent = geoM.transform;
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
                        }/**/
                        

                        obsGeos.RemoveAt(j);
                        obsGeos.RemoveAt(i);
                        obsGeos.Add(tmpG);
                        difFound = true;
                        break;
                    }
                }
            }
            if (difFound)
            {
                difFound = false;
            }
            else
            {
                done = true;
            }
            whileIndex++;
            if (whileIndex > infiniLoopStop)
            {
                Debug.Log("INFINILOOP1");
                break;
            }

       }



















        //mapBG.DrawGeometry (temp);
        //Debug.Log(obsGeos);
        //Debug.Log(obsGeos.Count);
        //TODO REMOVE DEBUG
        //GameObject pparent = new GameObject("DebugParent");
        /*
        GameObject parent;
        Debug.Log(obsGeos.Count);
        foreach(Geometry ggg in obsGeos) {
            parent = new GameObject("Geo");
            parent.transform.parent = pparent.transform;
            foreach (Line l in ggg.edges/*obsGeos[0].edges/**//*) {
                //Debug.Log(l.vertex[0] + "," + l.vertex[1]);

                GameObject lin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lin.GetComponent<Renderer>().sharedMaterial.color = Color.red;
                lin.transform.parent = parent.transform;
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
        }/**/
        //TODO END OF DEBUG TO REMOVE



        /*
        parent = new GameObject("MapBG1");
        foreach (Line l in mapBG.edges) {
            //Debug.Log(l.vertex[0] + "," + l.vertex[1]);

            GameObject lin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lin.GetComponent<Renderer>().sharedMaterial.color = Color.red;
            lin.transform.parent = parent.transform;
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
        */








        List<Geometry> finalPoly = new List<Geometry> ();//Contains all polygons that are fully insde the map
		foreach ( Geometry g in obsGeos ) {
			if( mapBG.GeometryIntersect( g ) && !mapBG.GeometryInside( g ) ){
                //mapBG.debuggery = true;
                //g.debuggery = true;
 
				mapBG = mapBG.GeometryMergeInner( g );

                //mapBG.debuggery = false;
                //g.debuggery = false;
                mapBG.BoundGeometry( mapBoundary );
            }
			else
				finalPoly.Add(g);
		}
       // Debug.Log(finalPoly.Count);

		foreach(Geometry g in finalPoly){
            //DRAWING GEOMETRY NO LONGER WORKS CUZ VECTROSITY
			//g.DrawGeometry( temp);
			toReturn.Add (g);
		}

		List<Vector3> allVertex = new List<Vector3>();
		List<Vector3> tempVertex = new List<Vector3>();
		Geometry totalGeo = new Geometry ();

		//Getting all vertices
		foreach (Geometry g in finalPoly) {
			tempVertex = g.GetVertex();
			foreach( Vector3 v in tempVertex )
				allVertex.Add(v);
			foreach( Line l in g.edges )
				totalGeo.edges.Add(l);
		}

		tempVertex = mapBG.GetVertex();
		foreach( Vector3 v in tempVertex )
			allVertex.Add(v);
		foreach( Line l in mapBG.edges )
			totalGeo.edges.Add(l);
		lines.Clear ();


        








        //-----------START MST CODE------------------//
        //We will use "mapBG" and "finalPoly"
        //finalPoly contains the "quadrilaters"
        //get all lines from quadrilaters/finalPoly and put them in "lines" || We use "obsLines"
        List<Line> obsLines = new List<Line> ();
		List<Geometry> toCheck = new List<Geometry> ();
		foreach (Geometry g in finalPoly) {
			foreach( Line l in g.edges )
				obsLines.Add( l );
			toCheck.Add(g);
		}
		//set links with neighbors for each quadrilater (send list of all obstacles as a paramter)
		foreach (Geometry g in toCheck) {
			g.SetVoisins( toCheck );		
		}
		//keep a list of the edges (graph where obstaceles are the nodes) in a list of lines called "linesLinking"
		List<Vector3> mapVertices = mapBG.GetVertex();

		//Possible redundancy here
		Geometry start = mapBG.findClosestQuad (mapVertices[0], toCheck, new List<Geometry> ());


		List<Line> linesLinking = new List<Line> ();
        if (start != null)
        {
            linesLinking.Add(mapBG.GetClosestLine(start, toCheck));
        	start.visited = true;
        
            List<Geometry> toCheckNode = new List<Geometry> (); 
		    toCheckNode.Add (start);
            //Debug.Log(start);
            //Debug.Log(start.voisinsLine);
            //Debug.Log(start.voisinsLine.Count);
            Line LinetoAdd = null;
            if (start.voisinsLine.Count > 0) { 
		        LinetoAdd= start.voisinsLine [0];

            }
        
            //DRAW GEOMETRY NEEDS VECTROSITY WHICH DOES NOT WORK RIGHT NOW
            //mapBG.DrawGeometry (temp);
		    toReturn.Add (mapBG);

            //Straight Porting//
            int whileIndex2 = 1;
		    while (LinetoAdd != null) {
			    LinetoAdd = null; 
			    Geometry qToAdd = null; 
			
			    //Check all 
			    foreach (Geometry q in toCheckNode) {
				
				    for (int i = 0; i<q.voisins.Count; i++) {
					    if (! q.voisins [i].visited) {
						    if (LinetoAdd != null) {
							    //get the shortest line
							    if (LinetoAdd.Magnitude () >= q.voisinsLine [i].Magnitude ()) {
								    LinetoAdd = q.voisinsLine [i];
								    qToAdd = q.voisins [i]; 
								
							    }
						    } else {
							    qToAdd = q.voisins [i]; 
							    LinetoAdd = q.voisinsLine [i];
						    }
					    } else {
						    continue; 
					    }
				    }
			    }
			    if (LinetoAdd != null) {
				    linesLinking.Add (LinetoAdd); 
				    qToAdd.visited = true; 
				    toCheckNode.Add (qToAdd); 
			    }

                whileIndex2++;
                if(whileIndex2 > infiniLoopStop)
                {
                    Debug.Log("INFINILOOP2");
                    break;
                }
		    }

        }

        foreach (Line l in linesLinking) {
			triangulation.linesMinSpanTree.Add (l); 
		}
        //END porting

        //-----------END MST CODE--------------------//

        //DEBUG SECTION -- OBSTACLES
        /*GameObject parentO = new GameObject("DebugParentObstacles");
        foreach (Geometry G in obsGeos){
            foreach (Line l in G.edges){
           //foreach( Line l in mapBG.edges){
                GameObject lin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lin.GetComponent<Renderer>().sharedMaterial.color = Color.red;
                lin.transform.parent = parentO.transform;
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
        }
        *//*
        GameObject parentMap = new GameObject("ParentMap");
        foreach (Line l in mapBG.edges) {
            GameObject lin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lin.GetComponent<Renderer>().sharedMaterial.color = Color.red;
            lin.transform.parent = parentMap.transform;
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
        }/**/


        /*

        //DEBUG SECTION -- MST
        GameObject parent = new GameObject("DebugParentMinSpanTree");
        foreach (Line l in linesMinSpanTree)
        {
            GameObject lin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lin.GetComponent<Renderer>().sharedMaterial.color = Color.red;
            lin.transform.parent = parent.transform;
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
        }*/

        //




        //int vlcnt = 0;
        lines.Clear ();
        //Constructing "lines" for triangulation
        //First add lines that are in MST


        foreach (Line l in linesMinSpanTree){
            lines.Add(l);
        }

        //		Debug.Log (allVertex.Count);
        //		int iv = -1, jv;
        //		vlcnt = 0;


        //finalPoly -> contained polygons
        //MapBG -> outer edge including polys
        //totalPoly -> all of the above edges


        foreach(Line l in mapBG.edges) {
            lines.Add(l);
        }




        //GameObject vertRent = new GameObject("vertRent");
        //GameObject vert;
        bool doIt = false;
        Vector3 vA = new Vector3(-32.5f, 1f, 39f);
        Vector3 vB = new Vector3(-49f, 1f, 42.5f);
        for(int i = 0; i < allVertex.Count; i++) {

            /*vert = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            vert.transform.position = allVertex[i];
            vert.transform.parent = vertRent.transform;*/

            for (int j = i+1; j < allVertex.Count; j++) {
                /*if((allVertex[i] - vA).magnitude < 0.05) {
                    if((allVertex[j] - vB).magnitude < 0.05) {
                        doIt = true;
                    }
                    else {
                        doIt = false;
                    }
                }
                else {
                    doIt = false;
                }*/

                Line l = new Line(allVertex[i], allVertex[j]);
                Vector3 mid = l.MidPoint();
                bool collision = false;
                if ((allVertex[i] - allVertex[j]).magnitude < threshold) {
                    collision = true;
                }

                foreach( Geometry g in obsGeos) {
                    if (g.PointInside(mid)) {
                        collision = true;
                        if (doIt) {
                            Debug.Log("INSIDE G" + g);
                        }
                        break;
                    }
                }
                if (!collision) {
                    foreach (Geometry g in obsGeos) {
                        if (collision) {
                            break;
                        }
                        foreach (Line l2 in g.edges) {
                            int intersect = l.LineIntersectMuntac(l2);
                            if (intersect == 1) {
                                collision = true;
                                if (doIt) {
                                    Debug.Log("COLLIDES WITH EDGE OF" + g + "SPECIFICALLY LINE" + l2);
                                }
                                break;
                            }
                        }
                    }
                }
                /*if (!collision) {
                    if (!(mapBG.PointInside(allVertex[i]) || mapBG.PointBelongsRough(allVertex[i]))) {
                        if (doIt) { 
                            Debug.Log("POINT1 NOT INSIDE OR BELONGS");
                        }
                        collision = true;
                    }
                    else if (!(mapBG.PointInside(allVertex[j]) || mapBG.PointBelongsRough(allVertex[j]))) {
                        if (doIt) {
                            Debug.Log("POINT2 NOT INSIDE OR BELONGS");
                        }
                        collision = true;
                    }

                }*/
                if (!collision) {
                    foreach(Line l2 in lines) {
                        int intersect = l.LineIntersectMuntac(l2);
                        if (intersect > 0) {
                            if (doIt) { 
                                Debug.Log("INTERSECTS PREVIOUS LINE LOCATED AT" + l2);
                            }
                            collision = true;
                            break;
                        }
                    }
                }
                if (!collision) {
                    lines.Add(l);
                }
                
            }
        }


        /*
		foreach (Vector3 Va in allVertex) {
            vert = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            vert.transform.position = Va;
            vert.transform.parent = vertRent.transform;
//			++iv;
//			jv = -1;
			foreach(Vector3 Vb in allVertex){
//				++jv;
				if( Va != Vb ){
                    bool collides = false;
                    //bool essential = false;
					Line tempLine = new Line(Va, Vb);
					//A-Collision with final polygon
					foreach( Line l in totalGeo.edges ){
						if( l.LineIntersectMuntacEndPt( tempLine ) == 1 ){
//							if( iv == 16 && jv == 18 ){	
//								++vlcnt;
//								l.name = vlcnt.ToString();
//								l.DrawVector(temp);
//								tempLine.name = l.name + "DUP";
//								tempLine.DrawVector(temp);
//								Debug.Log("Here 1");
//							}
							collides = true;
							break;
						}
					}

					if( collides ) continue;

					//B-Collision with existing lines
					foreach( Line l in lines ){
						if( l.LineIntersectMuntacEndPt( tempLine ) == 1 || l.Equals(tempLine) ){
							collides = true;
							break;
						}
					}

					if( collides ) continue;
					//C-To avoid diagonals
					foreach( Geometry g in geos ){
						if( g.PointInside( tempLine.MidPoint() ) ){
							collides = true;
							break;
						}
					}
					if( collides ) continue;
					//Add Line
					lines.Add( tempLine );
				}
			}
		}
        */
//		foreach (Line L in lines)
//			L.DrawVector(temp);
//		Debug.Log ("Total Lines" + lines.Count);
				
		//Find the centers 
		List<Triangle> triangles = new List<Triangle> ();
        //Well why be efficient when you can be not efficient
        //GameObject linesPar = new GameObject("linesPar");


        Vector3 v1;
        Vector3 v2;
        Vector3 v3;
        bool foundLine2 = false;

        foreach (Line l in lines) {
                /*//DEBUG PRINTING
                GameObject lin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lin.GetComponent<Renderer>().sharedMaterial.color = Color.red;
                lin.transform.parent = linesPar.transform;
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
                //END DEBUG PRINTING*/
            v1 = l.vertex [0]; 
			v2 = l.vertex [1];
			foreach (Line l2 in lines) {
				if (l == l2)
					continue;
								
				if ((l2.vertex[0] - v2).magnitude < threshold) {
					v3 = l2.vertex [1];
                    foundLine2 = true;

				} else if ((l2.vertex[1] - v2).magnitude < threshold) {
					v3 = l2.vertex [0];
                    foundLine2 = true;
				}
                else {
                    v3 = Vector3.zero;
                    foundLine2 = false;
                }
				
				if (foundLine2) {
					foreach (Line l3 in lines) {
                        if (l3 == l2 || l3 == l) {
                            continue;
                        }
						if (  (((l3.vertex [0] -v1).magnitude < threshold)   && ((l3.vertex[1] - v3).magnitude < threshold))
						    || (((l3.vertex[1] - v1).magnitude < threshold) && ((l3.vertex[0] - v3).magnitude < threshold))) {
							//Debug.DrawLine(v1,v2,Color.red); 
							//Debug.DrawLine(v2,v3,Color.red); 
							//Debug.DrawLine(v3,v1,Color.red); 
							
							//Add the traingle
							Triangle toAddTriangle = new Triangle (
								v1, triangulation.points.IndexOf (v1),
								v2, triangulation.points.IndexOf (v2),
								v3, triangulation.points.IndexOf (v3));
							
							
							bool isAlready = false; 
							foreach (Triangle tt in triangles) {
								if (tt.Equals (toAddTriangle)) {
									//Debug.Log(toAddTriangle.refPoints[0]+", "+
									//          toAddTriangle.refPoints[1]+", "+
									//          toAddTriangle.refPoints[2]+", "); 
									isAlready = true; 
									break; 
								}
								
							}
							if (!isAlready) {
								triangles.Add (toAddTriangle);
							}
							
						}
					}
				}
			}
		}
		
		
		//Find shared edge and triangle structure
		
		foreach (Triangle tt in triangles) {
			foreach (Triangle ttt in triangles) {
				if (tt == ttt)
					continue; 
				tt.ShareEdged (ttt);
				
			}
			
		}
        
        //GameObject triParent = new GameObject("triParent");
        //drawTris(triangles, triParent.transform);


        triangles = delaunayIfy(triangles);
        //GameObject triDParent = new GameObject("triDParent");
        //drawTris(triangles, triDParent.transform);

        triangulation.triangles = triangles;

		//Create the road map
		roadMap.Clear();


        makeRoadMap(triangles);

        /*
		foreach(Triangle tt in triangles)
		{
			Line[] ll = tt.GetSharedLines(); 
			if(ll.Length == 1)
			{
				
				roadMap.Add(new Line(ll[0].MidPoint(), tt.GetCenterTriangle()));
			}
			else if(ll.Length > 2)
			{
				for(int i = 0; i<ll.Length; i++)
				{
					roadMap.Add(new Line(ll[i].MidPoint(), tt.GetCenterTriangle()));
				}
			}
			else
			{
				for(int i = 0; i<ll.Length; i++)
				{
					roadMap.Add(new Line(ll[i].MidPoint(), ll[(i+1) % ll.Length].MidPoint()));
				}
			}
		}*/
        /*
        GameObject roadPar = new GameObject("roadPar");
        foreach (Line l in roadMap) {
            GameObject lin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lin.GetComponent<Renderer>().sharedMaterial.color = Color.red;
            lin.transform.parent = roadPar.transform;
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
        }/**/

        return obsGeos;
		//return toReturn;
	}

    
    private List<Triangle> delaunayIfy(List<Triangle> tris) {
        bool done = false;
        bool edgeFlipped = false;
        bool foundSharedEdge = false;
        int whileIndex = 0;
        //GameObject trisPar = new GameObject("tris");
        while (!done) {
            whileIndex++;
            if(whileIndex > 100) {
                Debug.Log(whileIndex);
                break;
            }
            foreach(Triangle t in tris) {
                Line[] l1s = t.getLines();
                foreach( Triangle tt in t.voisins) {
                    Line[] l2s = tt.getLines();
                    int i = 0;
                    int j = 0;

                    for(i = 0; i < 3; i++) {
                        for(j = 0; j < 3; j++) {
                            if (l1s[i].Equals(l2s[j])) {
                                foundSharedEdge = true;
                                break;
                            }
                        }
                        if (foundSharedEdge) {
                            foundSharedEdge = false;
                            break;
                        }
                    }

                    Line l11 = l1s[((i + 1) % 3)];
                    Line l12 = l1s[((i + 2) % 3)];

                    Vector3 start1 = Vector3.zero;
                    Vector3 v11 = Vector3.zero;
                    Vector3 v12 = Vector3.zero;

                    if ((l12.vertex[0] - l11.vertex[0]).magnitude < 0.00025f) {
                        start1 = l12.vertex[0];
                        v11 = l11.vertex[1] - l11.vertex[0];
                        v12 = l12.vertex[1] - l12.vertex[0];
                    }
                    else if((l12.vertex[1] - l11.vertex[1]).magnitude < 0.00025f) {
                        start1 = l12.vertex[1];
                        v11 = l11.vertex[0] - l11.vertex[1];
                        v12 = l12.vertex[0] - l12.vertex[1];

                    }
                    else if((l12.vertex[0] - l11.vertex[1]).magnitude < 0.00025f) {
                        start1 = l12.vertex[0];
                        v11 = l11.vertex[0] - l11.vertex[1];
                        v12 = l12.vertex[1] - l12.vertex[0];

                    }
                    else {
                        start1 = l12.vertex[1];
                        v11 = l11.vertex[1] - l11.vertex[0];
                        v12 = l12.vertex[0] - l12.vertex[1];
                    }

                    float angle1 = Vector3.Angle(v11, v12);


                    Line l21 = l2s[((j + 1) % 3)];
                    Line l22 = l2s[((j + 2) % 3)];
                    Vector3 start2 = Vector3.zero;
                    Vector3 v21 = Vector3.zero;
                    Vector3 v22 = Vector3.zero;

                    if ((l22.vertex[0] - l21.vertex[0]).magnitude < 0.00025f) {
                        start2 = l22.vertex[0];
                        v21 = l21.vertex[1] - l21.vertex[0];
                        v22 = l22.vertex[1] - l22.vertex[0];
                    }
                    else if ((l22.vertex[1] - l21.vertex[1]).magnitude < 0.00025f) {
                        start2 = l22.vertex[1];
                        v21 = l21.vertex[0] - l21.vertex[1];
                        v22 = l22.vertex[0] - l22.vertex[1];

                    }
                    else if ((l22.vertex[0] - l21.vertex[1]).magnitude < 0.00025f) {
                        start2 = l22.vertex[0];
                        v21 = l21.vertex[0] - l21.vertex[1];
                        v22 = l22.vertex[1] - l22.vertex[0];

                    }
                    else {
                        start2 = l22.vertex[1];
                        v21 = l21.vertex[1] - l21.vertex[0];
                        v22 = l22.vertex[0] - l22.vertex[1];
                    }
                    float angle2 = Vector3.Angle(v21, v22);

                    if(angle1 + angle2 > 181f) {
                        if (false) {

                            Debug.Log("PART1");
                            Debug.Log(l1s[i]);
                            Debug.Log(l2s[j]);
                            Debug.Log("PART2");
                            Debug.Log(l11);
                            Debug.Log(l12);
                            Debug.Log(start1);
                            Debug.Log(v11);
                            Debug.Log(v12);
                            Debug.Log(angle1);
                            Debug.Log("PART3");
                            Debug.Log(l21);
                            Debug.Log(l22);
                            Debug.Log(start2);
                            Debug.Log(v21);
                            Debug.Log(v22);
                            Debug.Log(angle2);
                            Debug.Log("DONE");
                        }
                        tris.Remove(t);
                        tris.Remove(tt);
                        Triangle t3 = new Triangle(start1, start2, start1 + v11);
                        Triangle t4 = new Triangle(start1, start2, start1 + v12);

                        /*foreach (Triangle ttt in tris) {
                            t3.ShareEdged(ttt);
                            t4.ShareEdged(ttt);
                        }*/
                        foreach(Triangle ttt in t.voisins) {
                            ttt.voisins.Remove(t);
                            ttt.ShareEdged(t3);
                            ttt.ShareEdged(t4);
                            t3.ShareEdged(ttt);
                            t4.ShareEdged(ttt);
                        }
                        foreach(Triangle ttt in tt.voisins) {
                            ttt.voisins.Remove(tt);
                            ttt.ShareEdged(t3);
                            ttt.ShareEdged(t4);
                            t3.ShareEdged(ttt);
                            t4.ShareEdged(ttt);
                        }
                        tris.Add(t3);
                        tris.Add(t4);

                        edgeFlipped = true;
                        break;
                    }
                }
                if (edgeFlipped) {
                    break;
                }
            }
            if (edgeFlipped) {
               /*GameObject tri = new GameObject("tri" + whileIndex);
                tri.transform.parent = trisPar.transform;
                drawTris(tris, tri.transform);*/
                edgeFlipped = false;
            }
            else {
                done = true;
            }
        }

        return tris;
    }

    private void drawTris(List<Triangle> tris, Transform parent) {
        foreach(Triangle t in tris) {
            GameObject tp = new GameObject("t");
            tp.transform.parent = parent;

            foreach(Line l in t.getLines()) {
                GameObject lin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lin.GetComponent<Renderer>().sharedMaterial.color = Color.red;
                lin.transform.parent = tp.transform;
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
        }
    }

    private void makeRoadMap(List<Triangle> tris) {
        Triangle t = tris[0];
        List<Triangle> visited = new List<Triangle>();
        addLines(t, visited);
    }


    private List<Triangle> addLines(Triangle t, List<Triangle> visited) {
        visited.Add(t);
        foreach(Triangle tt in t.voisins) {
            if (!visited.Contains(tt)) {
                Vector3 midPoint = t.ShareEdged(tt).MidPoint();
                Line l1 = new Line(t.GetCenterTriangle(), midPoint);
                Line l2 = new Line(midPoint, tt.GetCenterTriangle());
                roadMap.Add(l1);
                roadMap.Add(l2);
            }
        }
        foreach(Triangle tt in t.voisins) {
            if (!visited.Contains(tt)) {
                visited = addLines(tt, visited);
            }
        }
        return visited;
    }




    public static List<List<int>> findAllSimpleEndPaths(Triangle startT, Triangle endT) {
        //Debug.Log(startT.GetCenterTriangle());
        //Debug.Log(endT.GetCenterTriangle());

        genTreeStructE(startT,endT);
        simpTreeStruct(startT);

        List<List<int>> paths = new List<List<int>>();
        List<int> path = new List<int>();
        paths.Add(path);
        int numPaths = 1;
        int curPath = 0;

        if (startT.simpTreeKids.Count == 1) {
            //path.Add(0);
            paths = findAllSimpEndPathHelp(startT.simpTreeKids[0], numPaths, curPath, paths, endT);
        }
        else if (startT.simpTreeKids.Count > 1) {
            for (int i = 0; i < startT.simpTreeKids.Count; i++) {
                if (i < numPaths) {
                    paths[i].Add(i);
                }
                else {
                    numPaths++;
                    paths.Add(new List<int>());
                    paths[i].Add(i);
                }
            }
            for (int i = 0; i < startT.simpTreeKids.Count; i++) {
                paths = findAllSimpEndPathHelp(startT.simpTreeKids[i], numPaths, i, paths, endT);
                numPaths = paths.Count;
            }
        }
        List<List<int>> endPaths = new List<List<int>>();
        foreach(List<int> p in paths) {
            if (p[p.Count - 1] != -1) {
                endPaths.Add(p);
            }
                
        }

        return endPaths;

    }

    private static List<List<int>> findAllSimpEndPathHelp(Triangle curT, int numPaths, int curPath, List<List<int>> paths, Triangle endT) {
        if (curT.simpTreeKids.Count == 1) {

            //paths[curPath].Add(0);
            paths = findAllSimpEndPathHelp(curT.simpTreeKids[0], numPaths, curPath, paths, endT);
            return paths;
        }
        else if (curT.simpTreeKids.Count > 1) {
            int oldNumPaths = numPaths;
            for (int i = 0; i < curT.simpTreeKids.Count; i++) {
                if (i < 1) {
                    paths[curPath].Add(i);
                }
                else {
                    numPaths++;
                    List<int> toAdd = new List<int>();
                    toAdd.AddRange(paths[curPath]);
                    toAdd.RemoveAt(toAdd.Count - 1);
                    toAdd.Add(i);
                    paths.Add(toAdd);

                }
            }

            paths = findAllSimpEndPathHelp(curT.simpTreeKids[0], numPaths, curPath, paths, endT);
            numPaths = paths.Count;

            for (int i = 1; i < curT.simpTreeKids.Count; i++) {
                paths = findAllSimpEndPathHelp(curT.simpTreeKids[i], numPaths, (oldNumPaths - 1 + i), paths, endT);
                numPaths = paths.Count;
            }
            return paths;
        }
        else {
            if (curT.Equals(endT)) {
                return paths;
            }
            else {
                paths[curPath].Add(-1);
                return paths;
            }
            
        }
    }

    public static List<List<int>> findAllSimplePaths(Triangle startT) {
        genTreeStruct(startT);
        simpTreeStruct(startT);

        List<List<int>> paths = new List<List<int>>();
        List<int> path = new List<int>();
        paths.Add(path);
        int numPaths = 1;
        int curPath = 0;

        if(startT.simpTreeKids.Count == 1) {
            path.Add(0);
            paths = findAllSimpPathHelp(startT.simpTreeKids[0], numPaths, curPath, paths);
        }
        else if(startT.simpTreeKids.Count > 1) {
            for(int i  =0; i < startT.simpTreeKids.Count; i++) {
                if(i < numPaths) {
                    paths[i].Add(i);
                }
                else {
                    numPaths++;
                    paths.Add(new List<int>());
                    paths[i].Add(i);
                }
            }
            for (int i = 0; i < startT.simpTreeKids.Count; i++) {
                paths = findAllSimpPathHelp(startT.simpTreeKids[i], numPaths, i, paths);
                numPaths = paths.Count;
            }
        }
        return paths;

    }

    private static List<List<int>> findAllSimpPathHelp(Triangle curT, int numPaths, int curPath, List<List<int>> paths) {
        if (curT.simpTreeKids.Count == 1) {

            paths[curPath].Add(0);
            paths = findAllSimpPathHelp(curT.simpTreeKids[0], numPaths, curPath, paths);
            return paths;
        }
        else if(curT.simpTreeKids.Count > 1) {
            int oldNumPaths = numPaths;
            for(int i=0; i < curT.simpTreeKids.Count; i++) {
                if(i < 1) {
                    paths[curPath].Add(i);
                }
                else {
                    numPaths++;
                    List<int> toAdd = new List<int>();
                    toAdd.AddRange(paths[curPath]);
                    toAdd.RemoveAt(toAdd.Count - 1);
                    toAdd.Add(i);
                    paths.Add(toAdd);

                }
            }

            paths = findAllSimpPathHelp(curT.simpTreeKids[0], numPaths, curPath, paths);
            numPaths = paths.Count;

            for (int i = 1; i < curT.simpTreeKids.Count; i++) {
                paths = findAllSimpPathHelp(curT.simpTreeKids[i], numPaths, (oldNumPaths - 1 + i), paths);
                numPaths = paths.Count;
            }
            return paths;
        }
        else {
            return paths;
        }
    }

    public static float findDistanceAlongPath(Triangle startT, Triangle endT, List<int> path) {
        computeDistanceTree(startT);
        Triangle t = startT;
        int index = 0;
        float distance = 0;
        while (!t.Equals(endT)) {
            Triangle tt;
            if(t.treeKids.Count > 1) {
                tt = t.treeKids[path[index]];
                index++;
            }
            else {
                if(t.treeKids.Count == 1){
                    tt = t.treeKids[0];
                }
                else {
                    Debug.Log("THERE ARE PROBLEMS");
                    Debug.Log(t.treeKids.Count);
                    Debug.Log(t.GetCenterTriangle());
                    break;
                }
               
            }
            Vector3 midPoint = t.ShareEdged(tt).MidPoint();
            Line l1 = new Line(t.GetCenterTriangle(), midPoint);
            Line l2 = new Line(midPoint, tt.GetCenterTriangle());
            distance = distance + l1.Magnitude() + l2.Magnitude();
            t = tt;
        }
        return distance;
    }

    public static Triangle findMidTriangleAlongPath(Triangle startT, Triangle endT, List<int> path) {
        //computeDistanceTree(startT);
        float TotDistance = findDistanceAlongPath(startT, endT, path);
        //Debug.Log("TotDistance:" + TotDistance);
        float halfDist = TotDistance / 2f - 5f;
        //Debug.Log("HalfDistance:" + TotDistance);
        float distance = 0;
        Triangle t = startT;
        int index = 0;
        while (!t.Equals(endT)) {
            Triangle tt;
            if (t.treeKids.Count > 1) {
                tt = t.treeKids[path[index]];
                index++;
            }
            else {
                tt = t.treeKids[0];
            }
            Vector3 midPoint = t.ShareEdged(tt).MidPoint();
            Line l1 = new Line(t.GetCenterTriangle(), midPoint);
            Line l2 = new Line(midPoint, tt.GetCenterTriangle());
            distance = distance + l1.Magnitude() + l2.Magnitude();
            if (distance > halfDist) {
                if((distance - halfDist) > (halfDist - (distance - l1.Magnitude() - l2.Magnitude()))) {
                    //Debug.Log("PDist:" + (distance - l1.Magnitude() - l2.Magnitude()));
                    //Debug.Log("RealdDist:" + t.distance);
                    return t;
                }
                else {
                    //Debug.Log("PDist:" + distance);
                    //Debug.Log("RealdDist:" + tt.distance);
                    return tt;
                }
            }
            t = tt;
        }
        Debug.Log("THIS FAILED");
        return t;
    }


    private static int antiInfiniLoopTreeDrawSimp = 0;

    public static void drawTreeStructSimp(Triangle t) {
        antiInfiniLoopTreeDrawSimp = 0;
        GameObject triTree = new GameObject("TriTreeSimp");
        GameObject node = new GameObject("rootNode");
        node.transform.parent = triTree.transform;
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.transform.position = t.GetCenterTriangle();
        root.transform.parent = node.transform;
        root.name = "root";
        foreach (Triangle tt in t.simpTreeKids) {
            DebugDrawLineUsingObjectsArrowVec(node, t.GetCenterTriangle(), tt.GetCenterTriangle());
            drawTreeStructHelpSimp(tt, node);
        }
    }

    private static void drawTreeStructHelpSimp(Triangle t, GameObject tObj) {
        antiInfiniLoopTreeDrawSimp++;
        //Debug.Log(t.GetCenterTriangle());
        if (antiInfiniLoopTreeDrawSimp > 50) {
            //Debug.Log(antiInfiniLoopTreeDrawSimp);
            Debug.Log("INFINILOOPTREEDRAWSIMP");
            //Debug.Log(t.GetCenterTriangle());
            return;

        }
        GameObject node = new GameObject("node");
        node.transform.parent = tObj.transform;
        GameObject ctObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ctObj.transform.position = t.GetCenterTriangle();
        ctObj.transform.parent = node.transform;
        ctObj.name = "tri";
        /*string toDebug = "This triangle calls on its children:";
        foreach (Triangle tt in t.simpTreeKids) {
            toDebug = toDebug + tt.GetCenterTriangle() + ",";
        }
        Debug.Log(toDebug);*/
        foreach (Triangle tt in t.simpTreeKids) {
            DebugDrawLineUsingObjectsArrowVec(node, t.GetCenterTriangle(), tt.GetCenterTriangle());
            drawTreeStructHelpSimp(tt, node);
        }
    }

    public static void simpTreeStruct(Triangle t) {
        t.simpTreeKids.Clear();

        foreach(Triangle tt in t.treeKids) {
            t.simpTreeKids.Add(simpTreeStructHelp(tt));
        }
    }

    private static Triangle simpTreeStructHelp(Triangle t) {
        if(t.treeKids.Count == 0) {
            return t;
        }
        else if(t.treeKids.Count == 1) {
            if (t.parents.Count == 1) {
                return simpTreeStructHelp(t.treeKids[0]);
            }
            else {
                Triangle tkid = simpTreeStructHelp(t.treeKids[0]);
                if (!t.simpTreeKids.Contains(tkid)) {
                    t.simpTreeKids.Add(tkid);
                }
                return t;
            }
        }
        else {
            
            foreach(Triangle tt in t.treeKids) {
                Triangle tkid = simpTreeStructHelp(tt);
                if (!t.simpTreeKids.Contains(tkid)) {
                    t.simpTreeKids.Add(simpTreeStructHelp(tt));
                }
            }
            return t;
        }
    }

    private static int antiInfiniLoopTreeDraw = 0;

    public static void drawTreeStruct(Triangle t) {
        antiInfiniLoopTreeDraw = 0;

        drawn = new List<Triangle>();
        drawn.Add(t);
        //Set up nested objects
        GameObject triTree = new GameObject("TriTree");
        GameObject node = new GameObject("rootNode");
        node.transform.parent = triTree.transform;
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.transform.position = t.GetCenterTriangle();
        root.transform.position = t.GetCenterTriangle() + new Vector3(0f, t.distance - 1f, 0f);
        root.transform.parent = node.transform;
        root.name = "root";

        /*
        string toDebug = "Triangle at " + t.GetCenterTriangle() + ". With kids:";
        foreach(Triangle tt in t.treeKids) {
            toDebug = toDebug + tt.GetCenterTriangle() + ",";
        }
        Debug.Log(toDebug);
        */

        foreach (Triangle tt in t.treeKids) {
            //Draw Line from original node to each treeKid
            DebugDrawLineUsingObjectsArrowVec(node, t.GetCenterTriangle(), tt.GetCenterTriangle());

            //Recursively draw each tree Node
            drawTreeStructHelp(tt, node);
        }
    }

    private static List<Triangle> drawn;

    private static void drawTreeStructHelp(Triangle t, GameObject tObj) {
        if (drawn.Contains(t)) {
            return;
        }
        else {
            drawn.Add(t);
        }

        /*
        string toDebug = "Triangle at " + t.GetCenterTriangle() + ". With kids:";
        foreach (Triangle tt in t.treeKids) {
            toDebug = toDebug + tt.GetCenterTriangle() + ",";
        }
        Debug.Log(toDebug);
        */

        antiInfiniLoopTreeDraw++;
        if(antiInfiniLoopTreeDraw > 250) {
            Debug.Log("INFINILOOPTREEDRAW");
            Debug.Log(t.GetCenterTriangle());
            return;

        }

        //Set up nested objects
        GameObject node = new GameObject("node");
        node.transform.parent = tObj.transform;
        GameObject ctObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ctObj.transform.position = t.GetCenterTriangle();
        ctObj.transform.position = t.GetCenterTriangle() + new Vector3(0f, t.distance - 1f, 0f);
        ctObj.transform.parent = node.transform;
        ctObj.name = "tri";


        foreach (Triangle tt in t.treeKids) {
            //Draw Line between each treeKid an this parent
            DebugDrawLineUsingObjectsArrowVec(node, t.GetCenterTriangle(), tt.GetCenterTriangle());

            //Call recursively on each child
            drawTreeStructHelp(tt, node);
        }
    }

    public static void genTreeStructE(Triangle t, Triangle end) {
        antiInfiniLoopTree = 0;
        //t.treeKids = new List<Triangle>();
        t.treeKids.Clear();
        t.treeKids.AddRange(t.voisins);
        t.treeDepth = 0;
        foreach (Triangle tt in t.treeKids) {
            if (!tt.parents.Contains(t)) {
                tt.parents.Add(t);
            }
            tt.treeDepth = 1;
            genTreeStructHelpE(tt,end);
        }
        foreach (Triangle tt in t.treeKids) {
            setParents(tt);
        }
    }


    private static void genTreeStructHelpE(Triangle t, Triangle end) {
        //Debug.Log(t.GetCenterTriangle());
        antiInfiniLoopTree++;
        if (antiInfiniLoopTree > 250) {
            Debug.Log("INFINILOOPTREE");
            Debug.Log(t.GetCenterTriangle());

            return;
        }
        //t.treeKids = new List<Triangle>();
        t.treeKids.Clear();
        /*foreach (Triangle tt in t.voisins) {
            if(tt.treeDepth < 0) {
                t.treeKids.Add(tt);
            }
            else if(tt.treeDepth > t.treeDepth) {
                t.treeKids.Add(tt);
            }
        }*/
        bool testering = false;
        /*if ((t.GetCenterTriangle() - new Vector3(-14f, 1, 23.3333f)).magnitude < 0.0001f) {
            testering = true;
            Debug.Log("TESTERING");
            Debug.Log(t.GetCenterTriangle());
        }*/
        //if ((t.GetCenterTriangle() - new Vector3(47.7f, 1f, -32.3f)).magnitude < 0.5f) {
        //    Debug.Log("WE HAVE REACHED WEIRDO");
        //}

        foreach (Triangle tt in t.voisins) {
            //if((t.GetCenterTriangle() - new Vector3(47.7f, 1f, -32.3f)).magnitude < 0.5f) {
           //     if (tt.Equals(end)) {
           //         Debug.Log("INFO!");
           //         Debug.Log("weirdo" + t.treeDepth);
            //        Debug.Log("end" + tt.treeDepth);
           //         Debug.Log("ENDINFO!");
            //    }
            //}



            if (t.Equals(end)) {
                Debug.Log("FOUND END");
                Debug.Log(t.GetCenterTriangle());
                continue;
            }

            if (testering) {
                Debug.Log("New Voisin" + tt.GetCenterTriangle());
            }
            if (tt.treeDepth < 0) {            
                if (testering) {
                    Debug.Log("CASE1");
                }
                tt.treeDepth = t.treeDepth + 1;
                if (!t.treeKids.Contains(tt)) {

                    t.treeKids.Add(tt);
                }
                else {
                    Debug.Log("NOT ADDED, YAY1");
                }
                if (!tt.Equals(end)) {
                    
                   genTreeStructHelpE(tt, end);
                    
                }
            }
            else if (tt.treeDepth > t.treeDepth + 1) {
                if (testering) {
                    Debug.Log("CASE2");
                }
                tt.treeDepth = t.treeDepth + 1;
                if (!t.treeKids.Contains(tt)) {

                    t.treeKids.Add(tt);
                }
                else {
                    Debug.Log("NOT ADDED, YAY1");
                }
                if (!tt.Equals(end)) {
                    
                        genTreeStructHelpE(tt, end);
                    
                }
            }
            else if (tt.treeDepth == t.treeDepth + 1) {

                //TODO: ADD STUFF HERE TO FIX THE THINGS! maybe, or change the distance function...?
                if (testering) {
                    Debug.Log("CASE3");
                }
                if (!t.treeKids.Contains(tt)) {
                    t.treeKids.Add(tt);
                }
                else {
                    Debug.Log("NOT ADDED, YAY1");
                }
                tt.treeKids.Remove(t);
            }
            else if(tt.treeDepth == t.treeDepth) {
                if (tt.Equals(end)) {

                    t.treeKids.Add(tt);
                }
               

            }
        }
    }







    public static void genTreeStruct(Triangle t) {
        antiInfiniLoopTree = 0;
        //t.treeKids = new List<Triangle>();
        t.treeKids.Clear();
        t.treeKids.AddRange(t.voisins);
        t.treeDepth = 0;
        foreach(Triangle tt in t.treeKids) {
            if (!tt.parents.Contains(t)) { 
                tt.parents.Add(t);
            }
            tt.treeDepth = 1;
            genTreeStructHelp(tt);
        }
        foreach(Triangle tt in t.treeKids) {
            setParents(tt);
        }
    }

    private static int antiInfiniLoopTree = 0;

    private static void setParents(Triangle t) {
        foreach(Triangle tt in t.treeKids) {
            if (!tt.parents.Contains(t)) {
                tt.parents.Add(t);
                setParents(tt);
            }
        }
    }

    private static void genTreeStructHelp(Triangle t) {
        //Debug.Log(t.GetCenterTriangle());
        antiInfiniLoopTree++;
        if(antiInfiniLoopTree > 250) {
            Debug.Log("INFINILOOPTREE");
            Debug.Log(t.GetCenterTriangle());

            return;
        }
        //t.treeKids = new List<Triangle>();
        t.treeKids.Clear();
        /*foreach (Triangle tt in t.voisins) {
            if(tt.treeDepth < 0) {
                t.treeKids.Add(tt);
            }
            else if(tt.treeDepth > t.treeDepth) {
                t.treeKids.Add(tt);
            }
        }*/
        bool testering = false;
        /*if ((t.GetCenterTriangle() - new Vector3(-14f, 1, 23.3333f)).magnitude < 0.0001f) {
            testering = true;
            Debug.Log("TESTERING");
            Debug.Log(t.GetCenterTriangle());
        }*/

        foreach(Triangle tt in t.voisins) {
            if (testering) {
                Debug.Log("New Voisin" + tt.GetCenterTriangle());
            }
            if(tt.treeDepth < 0) {
                if (testering) {
                    Debug.Log("CASE1");
                }
                tt.treeDepth = t.treeDepth + 1;
                if (!t.treeKids.Contains(tt)) {

                    t.treeKids.Add(tt);
                }
                else {
                    Debug.Log("NOT ADDED, YAY1");
                }
                genTreeStructHelp(tt);
            }
            else if(tt.treeDepth > t.treeDepth + 1) {
                if (testering) {
                    Debug.Log("CASE2");
                }
                tt.treeDepth = t.treeDepth + 1;
                if (!t.treeKids.Contains(tt)) {
                    
                    t.treeKids.Add(tt);
                }
                else {
                    Debug.Log("NOT ADDED, YAY1");
                }
                genTreeStructHelp(tt);
            }
            else if(tt.treeDepth == t.treeDepth + 1) {

                //TODO: ADD STUFF HERE TO FIX THE THINGS! maybe, or change the distance function...?
                if (testering) {
                    Debug.Log("CASE3");
                }
                if (!t.treeKids.Contains(tt)) {
                    t.treeKids.Add(tt);
                }
                else {
                    Debug.Log("NOT ADDED, YAY1");
                }
                tt.treeKids.Remove(t);
            }
        }
    }

    public static void computeDistanceTreeE(Triangle t, Triangle end) {
        genTreeStructE(t, end);
        t.distance = 0;
        computeCloseHelpTree(t);
        Debug.Log("Max Dist is" + maxDist);
        antiInfiniLoop2 = 0;
    }

    public static void computeDistanceTree(Triangle t) {
        genTreeStruct(t);
        t.distance = 0;
        computeCloseHelpTree(t);
        //Debug.Log("Max Dist is" + maxDist);
        antiInfiniLoop2 = 0;
    }

    private static int antiInfiniLoop2 = 0;

    private static float maxDist = 0;

    private static void computeCloseHelpTree(Triangle t) {
        antiInfiniLoop2++;
        if (antiInfiniLoop2 > 100) {
            Debug.Log("INFINITE LOOP");
        }
        else if (antiInfiniLoop2 > 90) {
            Debug.Log(t.GetCenterTriangle());
        }
        foreach (Triangle tt in t.treeKids) {
            if (tt.visited) {
                tt.distance = 0;
            }
            else {
                Vector3 midPoint = t.ShareEdged(tt).MidPoint();
                Line l1 = new Line(t.GetCenterTriangle(), midPoint);
                Line l2 = new Line(midPoint, tt.GetCenterTriangle());
                maxDist = Mathf.Max(maxDist, l1.Magnitude() + l2.Magnitude());
                if (l1.Magnitude() + l2.Magnitude() + t.distance < tt.distance + 0.0001f) {
                    tt.distance = l1.Magnitude() + l2.Magnitude() + t.distance;
                    tt.changed = true;
                }
                //tt.distance = Mathf.Min(l1.Magnitude() + l2.Magnitude() + t.distance, tt.distance);
                //t.distance = Mathf.Min(l1.Magnitude() + l2.Magnitude() + tt.distance, t.distance);
            }
        }
        foreach (Triangle tt in t.treeKids) {
            computeCloseHelpTree(tt);
        }
    }


    private static void DebugDrawLineUsingObjectsArrowVec(GameObject parent, Vector3 l1, Vector3 l2) {
        DebugDrawLineUsingObjectsArrow(parent, new Line(l1, l2), Color.blue, 1f);
    }

    private static void DebugDrawLineUsingObjectsArrow(GameObject parent, Line l, Color c, float thickness) {

        float fractionSizeArrow = 0.2f;
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

    private static void DebugDrawLineUsingObjectsLine(GameObject parent, Line l, Color c, float thickness) {
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
        scale.z = 0.2f * thickness;
        scale.y = 0.2f;

        lin.transform.localScale = scale;

        lin.GetComponent<Renderer>().material.color = c;
    }



    private static void DebugDrawLineUsingObjects(GameObject parent, Vector3 l1, Vector3 l2) {
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


    //HOPEFULLY DEPRECATED
    public static void computeDistance(Triangle t) {
        t.distance = 0;
        List<Triangle> visited = new List<Triangle>();
        computeCloseHelp(t, visited);
        antiInfiniLoop = 0;
    }

    private static int antiInfiniLoop = 0;

    private static List<Triangle> computeCloseHelp(Triangle t, List<Triangle> visited) {
        antiInfiniLoop++;
        if (antiInfiniLoop > 100) {
            Debug.Log("INFINITE LOOP");
            return visited;
        }
        else if (antiInfiniLoop > 90) {
            Debug.Log(t.GetCenterTriangle());
        }
        visited.Add(t);
        foreach (Triangle tt in t.voisins) {
            if (tt.visited) {
                tt.distance = 0;
            }
            else {
                Vector3 midPoint = t.ShareEdged(tt).MidPoint();
                Line l1 = new Line(t.GetCenterTriangle(), midPoint);
                Line l2 = new Line(midPoint, tt.GetCenterTriangle());
                if (l1.Magnitude() + l2.Magnitude() + t.distance < tt.distance + 0.0001f) {
                    tt.distance = l1.Magnitude() + l2.Magnitude() + t.distance;
                    tt.changed = true;
                }
                //tt.distance = Mathf.Min(l1.Magnitude() + l2.Magnitude() + t.distance, tt.distance);
                //t.distance = Mathf.Min(l1.Magnitude() + l2.Magnitude() + tt.distance, t.distance);
            }
        }
        foreach (Triangle tt in t.voisins) {
            if (!visited.Contains(tt)) {
                tt.changed = false;
                visited = computeCloseHelp(tt, visited);
            }
            else if (tt.changed) {
                tt.changed = false;
                visited = computeCloseHelp(tt, visited);
            }
        }
        return visited;
    }
}



