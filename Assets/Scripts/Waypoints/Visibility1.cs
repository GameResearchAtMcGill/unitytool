using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//using System;
public class Visibility1 : MonoBehaviour {
	//public List<Vector3> points = new List<Vector3>();
	//public List<Color> colours = new List<Color>();
	// Use this for initialization
	
	//public List<Triangle> triangles = new List<Triangle>(); 
	//public List<Line> lines = new List<Line>(); 
	
	//public List<Line> linesMinSpanTree = new List<Line>(); 
	public List<Geometry> obsGeos = new List<Geometry> (); 
	//Contains Map
	public Geometry mapBG = new Geometry ();
	
	//public bool drawTriangles = false; 
	//public bool drawRoadMap = false; 
	//public bool drawMinSpanTree = false;
	//public bool stopAll = false;
	//public List<int>[] G = new List<int>[110];
	//public int[] colorG = new int[110];
	//public bool[] visitedG = new bool[110];
	//public const int red = 1;
	//public const int green = 2;
	//public const int blue = 3;
	List<Geometry> globalPolygon;
	List<Vector3> pathPoints;
	float mapDiagonalLength = 0;
	GameObject floor ;
	public Camera camObj;
	//List<Vector3> globalTempArrangedPoints = new List<Vector3>();
	// Use this for initialization
	GameObject spTemp ;
	void Start () 
	{
		spTemp = (GameObject)GameObject.Find ("StartPoint");
		globalPolygon = getObstacleEdges ();

		pathPoints = definePath ();
		foreach(Vector3 vect in pathPoints)
		{
			GameObject pathObj;
			pathObj = Instantiate(pathSphere, 
			                    vect, 
			                    pathSphere.transform.rotation) as GameObject;
			//pathObj.transform.position=vect;
		}
		CalculateVisibilityForPath ();

	}


	//Vector3 first_point;
	List<Geometry> globalTempShadowPoly = new List<Geometry>();
	Geometry globalTempStarPoly;
	List<List<Vector3>> globalTempintersectionPointsPerV = new List<List<Vector3>>();
	List<Line> globalTempAllShadowLines = new List<Line>();
	public Material mat;
	public GameObject pathSphere;
	public GameObject hiddenSphere;
	public GameObject selectedBoxPrefab;
	GameObject selectedBox;
	List<GameObject> hiddenSphereList;
	//GameObject shadowObject = new GameObject();

	
	Hashtable hTable;
	Vector3 start_box,end_box;
	Rect boundbox;
	bool b_ShowBoundbox=false;
	private void makeBox() {
		//Ensures the bottom left and top right values are correct
		//regardless of how the user boxes units
		float xmin = Mathf.Min(start_box.x, end_box.x);
		float zmin = Mathf.Min(start_box.z, end_box.z);
		float width = Mathf.Max(start_box.x, end_box.x) - xmin;
		float height = Mathf.Max(start_box.z, end_box.z) - zmin;
		boundbox = new Rect(xmin, zmin, width, height);
		if(width*height>0.01)
		{
			selectedBox = Instantiate(selectedBoxPrefab) as GameObject;
			b_ShowBoundbox = true;
			float centreX=(start_box.x+end_box.x)/2;
			float centreZ=(start_box.z+end_box.z)/2;
			selectedBox.renderer.enabled=true;
			Vector3 tempVect = new Vector3(centreX,1,centreZ);
			selectedBox.transform.position=tempVect;
			tempVect.x=width;
			tempVect.z=height;
			selectedBox.transform.localScale=tempVect;
		}
		else
		{
			GameObject.Destroy(selectedBox);
			b_ShowBoundbox=false;
		}
		if(hiddenSphereList!=null)
		{
			foreach(GameObject g in hiddenSphereList)
			{
				GameObject.Destroy(g);
			}
		}
		hiddenSphereList=null;
	}

	void Update () 
	{
		if(Input.GetMouseButtonDown(0)) 
		{
			GameObject.Destroy(selectedBox);
			start_box = Input.mousePosition;
		}
		
		if(Input.GetMouseButtonUp(0)) {
			end_box = Input.mousePosition;
			start_box = camObj.ScreenToWorldPoint(start_box);
			start_box.y=1;
			end_box = camObj.ScreenToWorldPoint(end_box);
			end_box.y=1;
			//Debug.Log(start_box+","+end_box);
			makeBox();
			IdentifyGoodHidingSpots();

		}
		/*
		//Destroy old game object
		shadowObject.GetComponent("MeshFilter").mesh.Clear();
		
		//New mesh and game object
		//shadowObject = new GameObject();
		shadowObject.name = "MousePolygon";
		Mesh mesh = new Mesh();
		
		//Components
		var MF = shadowObject.AddComponent("MeshFilter");
		var MR = shadowObject.AddComponent("MeshRenderer");
		//myObject[x].AddComponent();
		
		//Create mesh
		mesh = CreateMesh(x);
		
		//Assign materials
		MR.material = myMaterial;
		
		//Assign mesh to game object
		MF.mesh = mesh;
		*/
		//foreach (Geometry geo in globalTempShadowPoly) 
		{
			//geo.DrawGeometry(GameObject.Find("Floor"));

			//Debug.Log(geo.edges.Count);
			/*foreach(Line l in geo.edges)
			{
				Debug.DrawLine (l.vertex [0], l.vertex [1], Color.blue);
			}*/
		}
		//Debug.Break ();
		//foreach (Geometry geo in globalTempStarPoly) 
		/*foreach(Line l in globalTempStarPoly.edges)
		{
			Debug.DrawLine (l.vertex [0], l.vertex [1], Color.blue);
		}*/



	}

	void IdentifyGoodHidingSpots ()
	{
		if (!b_ShowBoundbox)
			return;
		//Identify path points in box
		Geometry boundboxGeo = new Geometry ();
		boundboxGeo.edges.Add (new Line (new Vector3(boundbox.x,1,boundbox.y),new Vector3(boundbox.x+boundbox.width,1,boundbox.y)));
		boundboxGeo.edges.Add (new Line (new Vector3(boundbox.x+boundbox.width,1,boundbox.y),new Vector3(boundbox.x+boundbox.width,1,boundbox.y+boundbox.height)));
		boundboxGeo.edges.Add (new Line (new Vector3(boundbox.x+boundbox.width,1,boundbox.y+boundbox.height),new Vector3(boundbox.x,1,boundbox.y+boundbox.height)));
		boundboxGeo.edges.Add (new Line (new Vector3(boundbox.x,1,boundbox.y+boundbox.height),new Vector3(boundbox.x,1,boundbox.y)));
		int startIndex = -1;
		int endIndex = -1;
		int currIndex = 0;
		foreach(Vector3 vect in pathPoints)
		{
			if(boundboxGeo.PointInside(vect))
			{
				if(startIndex==-1)
				{
					startIndex=currIndex;
				}
			}
			else
			{
				if(startIndex!=-1)
				{
					endIndex=currIndex-1;
					break;
				}
			}
			currIndex++;
		}
		if (startIndex == -1)
			return;
		List<Line> hiddenLines = new List<Line> ();
		//ForEach first path point:
		//Identify lines behind which to hide
		List<Geometry> shadowPolyTemp = (List<Geometry>)hTable [pathPoints [startIndex]];
		foreach(Geometry geo in shadowPolyTemp)
		{
			foreach(Line l in geo.edges)
			{
				List<Vector3> pair = l.PointsOnEitherSide(0.02f);

				int ct_insideObstacle=0;
				//int ct_insideBoundary=0;
				foreach(Vector3 pt in pair)
				{
					foreach(Geometry g in globalPolygon)
					{
						if(g.PointInside(pt))
						{
							ct_insideObstacle++;
						}
					}
				}
				if(ct_insideObstacle==1)
				{
					hiddenLines.Add(l);
				}
			}
		}
		currIndex = 0;
		foreach(Line l in hiddenLines)
		{
			Vector3 midPt = l.MidPoint();
			Vector3 tempPt = Vector3.MoveTowards(midPt,pathPoints[startIndex],0.1f);
			bool b_insideObs=false;
			foreach(Geometry g in globalPolygon)
			{
				if(g.PointInside(tempPt))
				{
					b_insideObs=true;
				}
			}
			if(!b_insideObs)
			{
				hiddenLines[currIndex]=null;
			}
			
			currIndex++;
		}
		hiddenLines.RemoveAll(item=>item==null);
		float radius_hiddenSphere = ((SphereCollider)hiddenSphere.collider).radius*((SphereCollider)hiddenSphere.collider).transform.lossyScale.x;
		//Debug.Log ("radius" + radius_hiddenSphere);
		hiddenSphereList = new List<GameObject> ();
		//Foreach line:
		foreach(Line l in hiddenLines)
		{
			Vector3 towardsVect=l.vertex[0];
			while(towardsVect!=l.vertex[1])
			{
				Vector3 previous = towardsVect;
				towardsVect = Vector3.MoveTowards(previous,l.vertex[1],radius_hiddenSphere+0.01f);
				Line tempLine = new Line(previous,towardsVect);
				List<Vector3> pair = tempLine.PointsOnEitherSide(radius_hiddenSphere+0.01f);
				if(!Physics.CheckSphere(pair[0],radius_hiddenSphere))
				{
					GameObject clone1 = (GameObject)Instantiate(hiddenSphere);
					clone1.transform.position = pair[0];
					hiddenSphereList.Add(clone1);
				}
				if(!Physics.CheckSphere(pair[1],radius_hiddenSphere))
				{
					GameObject clone1 = (GameObject)Instantiate(hiddenSphere);
					clone1.transform.position = pair[1];
					hiddenSphereList.Add(clone1);
				}
			}
			////// move over line, identify point beside line inside shadow polygon
			////// , make abstract area and check if all points on hidden sphere fits in all shadow
		}
		List<Vector3> circumPoints = new List<Vector3>();
		for(int k=0;k<hiddenSphereList.Count;k++)
		{
			bool sphereFound=false;
			circumPoints.Clear();
			circumPoints.Add(new Vector3(hiddenSphereList[k].transform.position.x,1,hiddenSphereList[k].transform.position.z));
			//circumPoints.Add(new Vector3(hiddenSphereList[k].transform.position.x-radius_hiddenSphere,1,hiddenSphereList[k].transform.position.z));
			//circumPoints.Add(new Vector3(hiddenSphereList[k].transform.position.x+radius_hiddenSphere,1,hiddenSphereList[k].transform.position.z));
			//circumPoints.Add(new Vector3(hiddenSphereList[k].transform.position.x,1,hiddenSphereList[k].transform.position.z-radius_hiddenSphere));
			//circumPoints.Add(new Vector3(hiddenSphereList[k].transform.position.x,1,hiddenSphereList[k].transform.position.z+radius_hiddenSphere));
			for(int i=startIndex;i<=endIndex;i++)
			{
				shadowPolyTemp = (List<Geometry>)hTable [pathPoints [i]];
				int insideCounterTemp=0;
				foreach(Geometry geo in shadowPolyTemp)
				{
					insideCounterTemp=0;
					foreach(Vector3 vect in circumPoints)
					{
						if(geo.PointInside(vect))
						{
							insideCounterTemp++;
						}
					}
					if(insideCounterTemp>0 && insideCounterTemp<4)
						Debug.Log (insideCounterTemp);
					if(insideCounterTemp==circumPoints.Count)
					{
						sphereFound=true;
						break;
					}
				}
				if(!sphereFound)
				{
					GameObject.Destroy(hiddenSphereList[k]);
					hiddenSphereList[k]=null;
				}
			}
		}
		hiddenSphereList.RemoveAll(item=>item==null);
	}

	public void CalculateVisibilityForPath()
	{
		//globalPolygon = getObstacleEdges ();

		List<Vector3> endPoints = new List<Vector3> ();
		hTable = new Hashtable ();
		//Extract all end points

		foreach(Line l in mapBG.edges)
		{
			foreach(Vector3 vect in l.vertex)
			{
				if(!endPoints.Contains(vect))
				{
					//finding
					for(int j=0;j<endPoints.Count;j++)
					{
						float dist = (Vector3.Distance(vect,endPoints[j]));
						if(mapDiagonalLength<dist)
						{
							mapDiagonalLength=dist;
						}
					}
					endPoints.Add(vect);
				}
			}
		}
		foreach (Geometry g in globalPolygon) 
		{
			foreach(Line l in g.edges)
			{
				foreach(Vector3 vect in l.vertex)
				{
					if(!endPoints.Contains(vect))
					{
						endPoints.Add(vect);
					}
				}
			}
		}
		//
		Vector3 normalVect = new Vector3 (0, 1, 0);
		Vector3 xVect = new Vector3 (1, 0, 0);
		//Do for all path points
		foreach(Vector3 pPoint in pathPoints)
		{
			Vector3 alongX = new Vector3(pPoint.x+2,pPoint.y,pPoint.z);
			List<Geometry> starPoly = new List<Geometry>();
			List<Vector3> arrangedPoints = new List<Vector3> ();
			List<float> angles = new List<float>();

			foreach(Vector3 vect in endPoints)
			{
				float sAngle = SignedAngleBetween(pPoint-vect,alongX-pPoint,normalVect);
				//Debug.Log(pPoint+" , "+vect+" , "+sAngle);
				angles.Add(sAngle);
			}
			int numTemp = angles.Count;
			while(numTemp>0)
			{
				float minAngle = 370;
				int indexAngle = -1;
				for (int i=0;i<angles.Count;i++)
				{
					if(minAngle>angles[i])
					{
						minAngle = angles[i];
						indexAngle = i;
					}
				}
				arrangedPoints.Add(endPoints[indexAngle]);
				angles[indexAngle]=370;
				numTemp--;
			}
			//find all intersection points
			List<List<Vector3>> intersectionPointsPerV = new List<List<Vector3>>();
			foreach(Vector3 vect in arrangedPoints)
			{
				Ray rayTemp = new Ray();
				rayTemp.direction = vect - pPoint;
				rayTemp.origin = pPoint;
				Vector3 extendedPoint = rayTemp.GetPoint(mapDiagonalLength);
				//Debug.Log(pPoint+" , "+vect+" , "+extendedPoint);
				Line longRayLine = new Line(pPoint,extendedPoint);
				//Find intersection points for longRayLine
				List<Vector3> intersectionPoints = new List<Vector3>();
				//Intersection with holes
				foreach (Geometry g in globalPolygon) 
				{
					foreach(Line l in g.edges)
					{
						if(l.LineIntersectMuntacEndPt(longRayLine)!=0)
						{
							Vector3 intsctPoint = l.GetIntersectionPoint(longRayLine);//LineIntersectionVect(longRayLine);
							//intsctPoint.x
							//if(!intersectionPoints.Contains(intsctPoint))
							if(!ListContainsPoint(intersectionPoints,intsctPoint))
							{
								//Debug.Log("Adding from intersection with holes "+intsctPoint.z);
								intersectionPoints.Add(intsctPoint);
							}
						}
					}
				}
				//Intersection with boundary points
				foreach(Line l in mapBG.edges)
				{
					if(l.LineIntersectMuntacEndPt(longRayLine)!=0)
					{
						Vector3 intsctPoint = l.GetIntersectionPoint(longRayLine);
						//if(!intersectionPoints.Contains(intsctPoint))
						if(!ListContainsPoint(intersectionPoints,intsctPoint))
						{
							//Debug.Log("Adding from intersection with boundary "+intsctPoint.z);
							intersectionPoints.Add(intsctPoint);
						}
					}
				}
				//Debug.Log(ListContainsPoint(intersectionPoints,vect));
				//Debug.Log(intersectionPoints.Count+"-----------------------------------");

				intersectionPointsPerV.Add(intersectionPoints);
				//Sort Intersection Points
				foreach(List<Vector3> intersectionPts in intersectionPointsPerV)
				{
					List<float> distancesFromV = new List<float>();
					foreach(Vector3 intsctPoint in intersectionPts)
					{
						distancesFromV.Add(Vector3.Distance(pPoint,intsctPoint));
					}
					for(int j=0;j<distancesFromV.Count;j++)
					{
						float leastVal = distancesFromV[j];
						for(int i=j+1;i<distancesFromV.Count;i++)
						{
							if(leastVal>distancesFromV[i])
							{
								leastVal=distancesFromV[i];
							}
						}
						int indexToReplace = distancesFromV.IndexOf(leastVal);
						float tmpA = distancesFromV[indexToReplace];
						distancesFromV[indexToReplace] = distancesFromV[j];
						distancesFromV[j] = tmpA;
						//Interchange values for intersection points
						Vector3 tmpB = intersectionPts[indexToReplace];
						intersectionPts[indexToReplace] = intersectionPts[j];
						intersectionPts[j] = tmpB;
					}
				}
			}
			//Debug.Log(intersectionPointsPerV[0].Count);
			//Remove vertex which is not visible
			//List<int> toRemoveListIndex = new List<int>();
			foreach(List<Vector3> intersectionPts in intersectionPointsPerV)
			{
				int tmpIndex = intersectionPointsPerV.IndexOf(intersectionPts);
				if(intersectionPts[0]!=arrangedPoints[tmpIndex])
				{
					//toRemoveListIndex.Add(tmpIndex);
					intersectionPointsPerV[tmpIndex]=null;//TODO: check if will be garbage collected
				}
			}
			intersectionPointsPerV.RemoveAll(item=>item==null);
			/*foreach(int toRemoveIndex in toRemoveListIndex)
			{
				intersectionPointsPerV.RemoveAt(toRemoveIndex);
			}*/
			//Remove all hidden intersection points behind visible vertices
			//TODO Have handle special case of two vertices on same ray from V,
			//then we might have more intersection points to consider other than 2
			foreach(List<Vector3> intersectionPts in intersectionPointsPerV)
			{
				if(intersectionPts.Count<2)
					continue;
				//if second point is on same polygon, just keep the single vertex and remove all behind it
				//if(existOnSamePolygon(intersectionPts[0],intersectionPts[1]))
				if(CheckIfInsidePolygon((intersectionPts[0]+intersectionPts[1])/2))
				{
					intersectionPts.RemoveRange(1,intersectionPts.Count-1);
				}
				//else keep the first two points
				else
				{
					intersectionPts.RemoveRange(2,intersectionPts.Count-2);
				}
			}

			//Build geometries
			for(int i=0;i<intersectionPointsPerV.Count;i++)
			{
				int nextIndex = (i+1)%intersectionPointsPerV.Count;
				Geometry geoVisible = new Geometry();
				for(int j=0;j<intersectionPointsPerV[i].Count-1;j++)
				{
					geoVisible.edges.Add(new Line(intersectionPointsPerV[i][j],intersectionPointsPerV[i][j+1]));
				}
				if(intersectionPointsPerV[i].Count==1 && intersectionPointsPerV[nextIndex].Count==1)
				{
					//geoVisible.edges.Add(new Line(pPoint,intersectionPointsPerV[i][0]));
					//geoVisible.edges.Add(new Line(pPoint,intersectionPointsPerV[i+1][0]));
					geoVisible.edges.Add(new Line(intersectionPointsPerV[i][0],intersectionPointsPerV[nextIndex][0]));
				}
				//All three cases, choose points on same polygon
				else
				{
					for(int j=0;j<intersectionPointsPerV[i].Count;j++)
					{
						for(int k=0;k<intersectionPointsPerV[nextIndex].Count;k++)
						{
							//if(existOnSamePolygon(intersectionPointsPerV[i][j],intersectionPointsPerV[i+1][k]))
							if(existOnSameLineOfPolygon(intersectionPointsPerV[i][j],intersectionPointsPerV[nextIndex][k]))
							{
								//geoVisible.edges.Add(new Line(pPoint,intersectionPointsPerV[i][j]));
								//geoVisible.edges.Add(new Line(pPoint,intersectionPointsPerV[i+1][k]));
								geoVisible.edges.Add(new Line(intersectionPointsPerV[i][j],intersectionPointsPerV[nextIndex][k]));
							}
						}
					}
				}
				/*else if(intersectionPointsPerV[i].Count==1 && intersectionPointsPerV[i+1].Count==2)
				{
				}
				else if(intersectionPointsPerV[i].Count==2 && intersectionPointsPerV[i+1].Count==1)
				{
				}
				else if(intersectionPointsPerV[i].Count==2 && intersectionPointsPerV[i+1].Count==2)
				{
				}*/
				starPoly.Add(geoVisible);
			}
			//Combining all visible edges
			Geometry visiblePoly = new Geometry();
			foreach(Geometry geo in starPoly)
				visiblePoly.edges.AddRange(geo.edges);
			List<Geometry> shadowPoly = FindShadowPolygons(visiblePoly);
			//ValidatePolygons(shadowPoly);
			//globalTempArrangedPoints.AddRange(arrangedPoints);
			//globalTempStarPoly = visiblePoly;
			//globalTempShadowPoly = shadowPoly;
			//globalTempintersectionPointsPerV.AddRange(intersectionPointsPerV);
			//bArranged = true;
			arrangedPoints.Clear();
			hTable.Add(pPoint,shadowPoly);
		}//End: Do for all path points
	}

	void ValidatePolygons (List<Geometry> shadowPoly)
	{
		foreach(Geometry g in shadowPoly)
		{
		}
	}

	private List<Geometry> FindShadowPolygons(Geometry starPoly)
	{
		List<Vector3> verticesStar = new List<Vector3> ();
		//foreach(Geometry gStar in starPoly)
		{
			foreach(Line l in starPoly.edges)
			{
				if(!ListContainsPoint(verticesStar,l.vertex[0]))
				{
					verticesStar.Add(l.vertex[0]);
				}
				if(!ListContainsPoint(verticesStar,l.vertex[1]))
				{
					verticesStar.Add(l.vertex[1]);
				}
			}
		}

		List<Geometry> modObstacles = CreateModifiedObstacles(verticesStar);

		Geometry mapModBoundary = CreateModifiedBoundary(verticesStar);
		List<Geometry> allGeometries = new List<Geometry> ();
		allGeometries.AddRange (modObstacles);
		allGeometries.Add (mapModBoundary);
		allGeometries.Add (starPoly);
		List<Geometry> shadowPoly = new List<Geometry> ();
		List<Line> listEdges = new List<Line> ();
		foreach(Geometry geo in allGeometries)
		{
			foreach(Line l in geo.edges)
			{
				List<Vector3> pair = l.PointsOnEitherSide(0.02f);
				int ct_visible=0;
				int ct_insideObstacle=0;
				int ct_insideBoundary=0;
				foreach(Vector3 pt in pair)
				{
					if(starPoly.PointInside(pt))
					{
						ct_visible++;
					}
					foreach(Geometry g in globalPolygon)
					{
						if(g.PointInside(pt))
						{
							ct_insideObstacle++;
						}
					}
					if(mapBG.PointInside(pt))
					{
						ct_insideBoundary++;
					}
				}
				//if(ct_visible>1)
				{
					//GameObject clone1 = (GameObject)Instantiate(spTemp);
					//clone1.transform.position = pair[0];
					//GameObject clone2 = (GameObject)Instantiate(spTemp);
					//clone2.transform.position = pair[1];
					//Debug.Log("ct_visible="+ct_visible+" &&&&& ct_insideObstacle = "+ct_insideObstacle+"ct_insideBoundary="+ct_insideBoundary);
					//Debug.Log(pair[0].x+","+pair[0].z+" )"+pair[1].x+","+pair[1].z);
				}
				if((ct_visible==0) || (ct_visible==1 && ct_insideObstacle==0 && ct_insideBoundary==2))
				{
					//GameObject clone1 = (GameObject)Instantiate(spTemp);
					//clone1.transform.position = pair[0];
					//GameObject clone2 = (GameObject)Instantiate(spTemp);
					//clone2.transform.position = pair[1];
					listEdges.Add(l);
				}
			}
		}
		globalTempAllShadowLines.AddRange(listEdges);
		//Concatinating all lines into geometries
		//foreach(Line l in listEdges)
		/*for(int i=0;i<listEdges.Count;i++)
		{
			if(listEdges[i]==null)
				continue;
			Geometry shadow = new Geometry();
			shadow.edges.Add(listEdges[i]);
			for(int j=i;j<listEdges.Count;j++)
			{
				if(listEdges[j]==null)
					continue;
				for(int k=0;k<shadow.edges.Count;k++)
				{
					int intsct = listEdges[j].LineIntersectMuntacEndPt(shadow.edges[k]);
					if(intsct!=0)
					{
						shadow.edges.Add(listEdges[j]);
						listEdges[j]=null;
						break;
					}
				}
			}
			shadowPoly.Add(shadow);
		}*/
		///////////////////////////////////
		for(int i=0;i<listEdges.Count;i++)
		{
			if(listEdges[i]==null)
				continue;
			Geometry shadow = new Geometry();
			shadow.edges.Add(listEdges[i]);
			listEdges[i]=null;
			for(int k=0;k<shadow.edges.Count;k++)
			{
				for(int j=0;j<listEdges.Count;j++)
				{
					if(listEdges[j]==null)
						continue;
					//int intsct = listEdges[j].LineIntersectMuntacEndPt(shadow.edges[k]);
					bool intsct = listEdges[j].CommonEndPoint(shadow.edges[k]);
					if(intsct)
					{
						shadow.edges.Add(listEdges[j]);
						listEdges[j]=null;
					}
				}
			}
			shadowPoly.Add(shadow);
		}
		///////////////////////////////////
		/*foreach(Line l in listEdges)
		{
			Debug.Log(l);
		}*/
		return shadowPoly;
	}

	List<Geometry> CreateModifiedObstacles (List<Vector3> verticesStar)
	{
		List<Geometry> modObstacles = new List<Geometry> ();
		foreach(Geometry g in globalPolygon)
		{
			Geometry obstacle = CreateModifiedPolygon(g,verticesStar);
			modObstacles.Add(obstacle);
		}
		return modObstacles;
	}

	Geometry CreateModifiedBoundary (List<Vector3> verticesStar)
	{
		Geometry mapModBoundary = CreateModifiedPolygon(mapBG,verticesStar);
		return mapModBoundary;
	}
	private Geometry CreateModifiedPolygon(Geometry g,List<Vector3> verticesStar)
	{
		Geometry obstacle = new Geometry();
		//Debug.Log("************Obstacle****************");
		foreach(Line l in g.edges)
		{
			//Debug.Log("************SameLine****************");
			List<Vector3> pointsOnSameline = new List<Vector3>();
			pointsOnSameline.Add(l.vertex[0]);
			foreach(Vector3 vect in verticesStar)
			{
				if(l.PointOnLine(vect))
				{
					if(!ListContainsPoint(pointsOnSameline,vect))
					{
						//Debug.Log(vect.x+","+vect.z);
						pointsOnSameline.Add(vect);
					}
				}
			}
			//Sort points in a line
			for(int i=1;i<pointsOnSameline.Count-1;i++)
			{
				float dist = Vector3.Distance(pointsOnSameline[0],pointsOnSameline[i]);
				int indexToReplace=-1;
				for(int j=i+1;j<pointsOnSameline.Count;j++)
				{
					float dist2 = Vector3.Distance(pointsOnSameline[0],pointsOnSameline[j]);
					if(dist>dist2)
					{
						dist=dist2;
						indexToReplace=j;
					}
				}
				if(indexToReplace>0)
				{
					Vector3 tempVar = pointsOnSameline[i];
					pointsOnSameline[i] = pointsOnSameline[indexToReplace];
					pointsOnSameline[indexToReplace] = tempVar;
				}
			}
			if(!ListContainsPoint(pointsOnSameline,l.vertex[1]))
			{
				pointsOnSameline.Add(l.vertex[1]);
			}
			for(int i=0;i<pointsOnSameline.Count-1;i++)
			{
				//Debug.Log(pointsOnSameline[i].x+","+pointsOnSameline[i].z+" to "+pointsOnSameline[i+1].x+","+pointsOnSameline[i+1].z);
				obstacle.edges.Add(new Line(pointsOnSameline[i],pointsOnSameline[i+1]));
			}
		}
		return obstacle;
	}
	private bool ListContainsPoint(List<Vector3> intersectionPoints,Vector3 intsctPoint)
	{
		float limit = 0.0001f;
		foreach (Vector3 vect in intersectionPoints) 
		{
			if(Vector3.SqrMagnitude(vect-intsctPoint)<limit)
			//if(Mathf.Approximately(vect.magnitude,intsctPoint.magnitude))
				return true;
			//Debug.Log("Points not equal"+vect+" , "+intsctPoint);
		}
		return false;
	}
	private bool CheckIfInsidePolygon(Vector3 pt)
	{
		bool result = false;
		foreach (Geometry g in globalPolygon)
		{
			result = g.PointInside(pt);
			if(result)
				break;
		}
		return result;
	}
	private bool existOnSameLineOfPolygon(Vector3 pt1,Vector3 pt2)
	{
		List<Geometry> allGeometries = new List<Geometry>();
		allGeometries.Add (mapBG);
		allGeometries.AddRange(globalPolygon);
		//TODO test this shit
		bool pt1Found=false;
		bool pt2Found=false;
		foreach (Geometry g in allGeometries)
		{
			foreach (Line l in g.edges) 
			{
				pt1Found = l.PointOnLine(pt1);
				pt2Found = l.PointOnLine(pt2);
				
				if(pt1Found && pt2Found)
				{
					return true;
				}
				/*if(!pt1Found && !pt2Found)
				{
					continue;
				}
				else
				{
					Debug.Log ("Line is " + l.vertex [1] + " to " + l.vertex [0]);
					//Debug.Log("Not on same line"+pt1+" , "+pt2);
					if(pt1Found)
					{
						Debug.Log(pt1+" found");
						Debug.Log(pt2+" NOT found");
					}
					if(pt2Found)
					{
						Debug.Log(pt2+" found");
						Debug.Log(pt1+" NOT found");
					}
					//return false;
				}*/
			}
		}
		return false;
	}
	private bool existOnSamePolygon(Vector3 pt1,Vector3 pt2)
	{
		List<Geometry> allGeometries = new List<Geometry>();
		allGeometries.Add (mapBG);
		allGeometries.AddRange (globalPolygon);
		//TODO test this shit
		//Debug.Log ("In existOnSamePolygon->" + allGeometries.Count + "=mapBG+"+globalPolygon.Count);
		foreach (Geometry g in allGeometries)
		{
			bool pt1Found=false;
			bool pt2Found=false;
			foreach (Line l in g.edges) 
			{
				if(l.PointOnLine(pt1))
				{
					pt1Found = true;
				}
				if(l.PointOnLine(pt2))
				{
					pt2Found = true;
				}
			}
			if(pt1Found && pt2Found)
			{
				return true;
			}
			if(!pt1Found && !pt2Found)
			{
				continue;
			}
			else
			{
				break;
			}
		}
		return false;
	}
	//copied from stackoverflow
	float SignedAngleBetween(Vector3 a, Vector3 b, Vector3 n){
		// angle in [0,180]
		float angle = Vector3.Angle(a,b);
		float sign = Mathf.Sign(Vector3.Dot(n,Vector3.Cross(a,b)));
		
		// angle in [-179,180]
		float signed_angle = angle * sign;
		
		// angle in [0,360] (not used but included here for completeness)
		float angle360 =  (signed_angle + 180) % 360;
		
		//return signed_angle;
		return angle360;
	}

	List<Vector3> definePath()
	{
		List<Vector3> pathPts = new List<Vector3> ();
		GameObject sp = (GameObject)GameObject.Find ("StartPoint");
		GameObject ep = (GameObject)GameObject.Find ("EndPoint");
		pathPts.Add(sp.transform.position);
		//first_point = pathPts [0];
		pathPts.Add(ep.transform.position);
		findPath (pathPts);//straight Line points
		return pathPts;
	}
	private void findPath (List<Vector3> pathPts)
	{
		int iterations = 6;//increase to increase number of points on path
		for (int i=0; i<iterations; i++)
		{
			int k=0;
			int numPoints = pathPts.Count;
			for(int j=0;j<numPoints-1;j++)
			{
				Vector3 newVect = (pathPts[k]+pathPts[k+1])/2;
				//Debug.Log(newVect);
				pathPts.Insert(k+1,newVect);
				k+=2;
			}
		}
	}
	public List<Geometry> getObstacleEdges()
	{
		//Compute one step of the discritzation
		//Find this is the view
		floor = (GameObject)GameObject.Find ("Floor");
		
		Vector3 [] vertex = new Vector3[4]; 
		
		//First geometry is the outer one
		List<Geometry> geos = new List<Geometry> ();
		
		
		//Drawing lines
		//VectorLine.SetCamera3D(Camera.current); 
		
		//Floor
		Vector3[] f = new Vector3[4];
		MeshFilter mesh = (MeshFilter)(floor.GetComponent ("MeshFilter"));
		Vector3[] t = mesh.sharedMesh.vertices; 
		
		Geometry tempGeometry = new Geometry (); 
		
		vertex [0] = mesh.transform.TransformPoint (t [1]);
		vertex [1] = mesh.transform.TransformPoint (t [0]);
		vertex [2] = mesh.transform.TransformPoint (t [23]);
		vertex [3] = mesh.transform.TransformPoint (t [11]);

		
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
		
		//Geometry mapBG = new Geometry (); 
		for (int i = 0; i < 4; i++)
			mapBG.edges.Add( new Line( mapBoundary[i], mapBoundary[(i + 1) % 4]) );
		Debug.Log ("mapBg" + mapBG.edges.Count);
		//mapBG.DrawVertex (GameObject.Find ("temp"));
		//mapBG.DrawGeometry(GameObject.find);
		
		GameObject[] obs = GameObject.FindGameObjectsWithTag ("Obs");
		if(obs == null)
		{
			//Debug.Log("Add tag geos to the geometries"); 
			return null; 
		}
		//data holder
		//Triangulation triangulation = GameObject.Find ("Triangulation").GetComponent<Triangulation> (); 
		//triangulation.points.Clear ();
		//triangulation.colours.Clear (); 
		
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
		//lines = new List<Line> ();
		
		obsGeos.Clear ();
		foreach (Geometry g in geos) {
			obsGeos.Add(g);
		}
		
		
		//Create empty GameObject
		GameObject temp = GameObject.Find("temp");
		DestroyImmediate(temp);
		temp = new GameObject("temp");
		//CODESPACE
		//Merging Polygons
		for (int i = 0; i < obsGeos.Count; i++) {
			for (int j = i + 1; j < obsGeos.Count; j++) {
				//check all line intersections
				if( obsGeos[i].GeometryIntersect( obsGeos[j] ) ){
					//Debug.Log("Geometries Intersect: " + i + " " + j);
					Geometry tmpG = obsGeos[i].GeometryMerge( obsGeos[j] ); 
					//remove item at position i, decrement i since it will be increment in the next step, break
					obsGeos.RemoveAt(j);
					obsGeos.RemoveAt(i);
					obsGeos.Add(tmpG);
					i--;
					break;
				}
			}
		}
		//		mapBG.DrawGeometry (temp);
		
		List<Geometry> finalPoly = new List<Geometry> ();//Contains all polygons that are fully insde the map
		foreach ( Geometry g in obsGeos ) {
			if( mapBG.GeometryIntersect( g ) && !mapBG.GeometryInside( g ) ){
				mapBG = mapBG.GeometryMergeInner( g );
				mapBG.BoundGeometry( mapBoundary );
			}
			else
				finalPoly.Add(g);
		}
		return finalPoly;
	}
}
