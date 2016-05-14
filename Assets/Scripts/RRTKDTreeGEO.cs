using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using KDTreeDLL;
using Common;
using Objects;
using Extra;

namespace Exploration {
	public class RRTKDTreeGEO : NodeProvider {

		//private Cell[][][] nodeMatrix;
		private float angle;
		public KDTree tree;
		public List<NodeGeo> explored;
		// Only do noisy calculations if enemies is different from null
		public List<EnemyGeo> enemies;

		private int interval = 3;
		//Not used anymore
		private int depth = 5;
		//public Vector3 min;
		//public float tileSizeX, tileSizeZ;
		private int maxDist = 2;

		public preCast casts;

        private bool debugging  = false;
		private bool preventDistractStacks = true;
        private bool useTriangulation = true;
        private bool checkWaste = false;
        private bool useDist = true;
        //First attempt was 50% distractions.
        private int distractables = 80;
		private int numNears = 3;

        private float rangeDist = 5f;
        private int rangeTime = 100;
        private float triRangeDist = 30f;

        private float triMaxRRTDist = 80f;


		//Geo version of GetNode
		public NodeGeo GetNodeGeo (int t, float x, float y) {
			object o = tree.search (new double[] {x, t, y});
			if (o == null) {
				NodeGeo n = new NodeGeo ();
				n.x = x;
				n.y = y;
				n.t = t;
				o = n;
			}
			return (NodeGeo)o;
		}

        public NodeGeo ComputePartialGeo(NodeGeo start, float endX, float endY, float minX, float maxX, float minY, float maxY,int minT, int maxT, int attemps, float speed, Vector2 distractPos, Vector2 distractPos2, List<Triangle> tris, bool part2, RRTResult toReturn, int index) {
            //Debug.Log ("COMPUTEGEO");

            

            // Initialization
            tree = new KDTree(3);
            explored = new List<NodeGeo>();

            // Prepare start and end node
            NodeGeo end = GetNodeGeo(0, endX, endY);
            tree.insert(start.GetArray(), start);
            explored.Add(start);
            float startX = start.x;
            float startY = start.y;

            // Prepare the variables		
            NodeGeo nodeVisiting = null;
            NodeGeo nodeTheClosestTo = null;

            float tan = speed / 1.0f;
            angle = 90 - Mathf.Atan(tan) * Mathf.Rad2Deg;

            float curMaxX = Mathf.Min(startX + rangeDist, maxX);
            float curMinX = Mathf.Max(startX - rangeDist, minX);
            float curMaxY = Mathf.Min(startY + rangeDist, maxY);
            float curMinY = Mathf.Max(startY - rangeDist, minY);
            int curMaxT = Mathf.Min(rangeTime + minT, maxT);

            int startTriIndex = -1;

            List<float> areas = new List<float>();
            float areaSum = 0;
            for (int i = 0; i < tris.Count; i++) {
                Triangle tri = tris[i];
                tri.resetTriProps();
                /*Line[] lins = tri.getLines();
                float l1 = lins[0].Magnitude();
                float l2 = lins[1].Magnitude();
                float l3 = lins[2].Magnitude();
                float s = 0.5f * (l1 + l2 + l3);
                float area = Mathf.Sqrt(s * (s - l1) * (s - l2) * (s - l3));
                tri.area = area;
                areas.Add(area);
                areaSum = areaSum + area;*/
                if (tri.containsPoint(new Vector3(startX, 1, startY))) {
                    if (startTriIndex >= 0) {
                        Debug.Log("PROBLEMO 1");
                    }
                    tri.visited = true;
                    startTriIndex = i;
                }
            }
            if (startTriIndex < 0) {
                Debug.Log("PROBLEMO 2");
            }
            Triangulation.computeDistanceTree(tris[startTriIndex]);
            /*GameObject triDists = new GameObject("triDists");
            foreach(Triangle tri in tris) {
                Debug.Log(tri.GetCenterTriangle());
                GameObject lin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                lin.GetComponent<Renderer>().sharedMaterial.color = Color.red;
                lin.transform.parent = triDists.transform;
                lin.transform.position = tri.GetCenterTriangle();
                Debug.Log(tri.distance);
            }*/







            List<float> standardizedAreas = new List<float>();
            List<Triangle> reachable = new List<Triangle>();
            float sumSoFar = 0;
            /*foreach (float a in areas) {
                standardizedAreas.Add((a + sumSoFar) / areaSum);
                sumSoFar = sumSoFar + a;
            }*/

            bool triAdded = true;

            //RRT algo
            for (int i = 0; i <= attemps; i++) {
                if (part2) {
                    toReturn.nodesUsed2MM[index]++;
                }
                else {
                    toReturn.nodesUsedMM[index]++;
                }
                if (triAdded) {
                    Triangulation.computeDistanceTree(tris[startTriIndex]);
                    reachable = new List<Triangle>();
                    foreach (Triangle tri in tris) {
                        if (tri.distance < triRangeDist) {
                            reachable.Add(tri);
                        }
                    }
                    areas = new List<float>();
                    areaSum = 0;
                    for (int j = 0; j < reachable.Count; j++) {
                        Triangle tri = reachable[j];
                        Line[] lins = tri.getLines();
                        float l1 = lins[0].Magnitude();
                        float l2 = lins[1].Magnitude();
                        float l3 = lins[2].Magnitude();
                        float s = 0.5f * (l1 + l2 + l3);
                        float area = Mathf.Sqrt(s * (s - l1) * (s - l2) * (s - l3));
                        tri.area = area;
                        areas.Add(area);
                        areaSum = areaSum + area;
                    }
                    standardizedAreas = new List<float>();
                    sumSoFar = 0;
                    foreach (float a in areas) {
                        standardizedAreas.Add((a + sumSoFar) / areaSum);
                        sumSoFar = sumSoFar + a;
                    }

                }


                //Then pick random x and y values
                float rx;
                float ry;
                float trisInd;
                int k = 0;
                float rl1;
                float rl2;

                bool distractPick = false;
                int distractNum = -1;


                if (Random.Range(0, 100) > distractables) {
                    //TODO
                    k = -1;
                    if (Random.Range(0, 100) > 50) {
                        rx = distractPos.x;
                        ry = distractPos.y;
                        distractPick = true;
                        distractNum = 0;
                    }
                    else {
                        rx = distractPos2.x;
                        ry = distractPos2.y;
                        distractPick = true;
                        distractNum = 1;
                    }
                }
                else {
                    /*
					rx = Random.Range (curMinX, curMaxX);
					ry = Random.Range (curMinY, curMaxY);
                    */
                    trisInd = Random.Range(0f, 1f);
                    for (k = 0; k < standardizedAreas.Count; k++) {
                        if (trisInd <= standardizedAreas[k]) {
                            break;
                        }
                    }
                    Triangle tri = reachable[k];
                    rl1 = 1.0f;
                    rl2 = 2.0f;
                    while (rl2 > rl1) {
                        rl1 = Random.Range(0f, 1f);
                        rl2 = Random.Range(0f, 1f);
                    }
                    Vector3 point = tri.vertex[0] + rl1 * (tri.vertex[1] - tri.vertex[0]) + rl2 * (tri.vertex[2] - tri.vertex[1]);
                    rx = point.x;
                    ry = point.z;
                }

                Vector3 pdO = new Vector3(rx - startX, 0, ry - startY);
                float pdd = pdO.magnitude;
                float fminT = pdd * Mathf.Tan(angle * Mathf.Deg2Rad);


                int curMinT = Mathf.Max(minT,Mathf.FloorToInt(fminT));

                //Pick a random time
                //int rt = Random.Range (1,maxT);
                int rt = Random.Range(minT, curMaxT);




                //int rx = p.x, ry = p.y;
                nodeVisiting = GetNodeGeo(rt, rx, ry);
                //if this node has already been visited continue
                if (nodeVisiting.visited) {
                    if (part2) {
                        toReturn.nodesRejected2MM[index]++;
                    }
                    else {
                        toReturn.nodesRejectedMM[index]++;
                    }
                    i--;
                    
                    //Consider checking if point is valid earlier, for i--.
                    continue;
                }

                explored.Add(nodeVisiting);


                Vector3 p1 = new Vector3();
                Vector3 p2 = new Vector3();
                Vector3 pd = new Vector3();

                if (preventDistractStacks) {
                    try {
                        object[] closestNodes = tree.nearest(new double[] { rx, rt, ry }, numNears);
                        bool viableFound = false;

                        for (int ind = 0; ind < numNears; ind++) {
                            nodeTheClosestTo = (NodeGeo)closestNodes[ind];

                            // cannot go back in time, so skip if t is decreasing
                            if (nodeTheClosestTo.t > nodeVisiting.t) {
                                viableFound = false;
                                continue;
                            }

                            // Only add if we are going in ANGLE degrees or higher.As there is a fixed max speed
                            p1 = nodeVisiting.GetVector3();
                            p2 = nodeTheClosestTo.GetVector3();
                            pd = p1 - p2;
                            if (Vector3.Angle(pd, new Vector3(pd.x, 0f, pd.z)) < angle) {
                                viableFound = false;
                                continue;
                            }

                            if (distractPick) {
                                if (Mathf.Approximately(p1.x, p2.x) && Mathf.Approximately(p1.z, p2.z)) {
                                    viableFound = false;
                                    continue;
                                }
                            }
                            viableFound = true;

                            nodeVisiting.distractTimes = new List<int>();
                            nodeVisiting.distractNums = new List<int>();


                            //Experimental New distract
                            if (nodeTheClosestTo.distractTimes.Count == maxDist) {
                                //Debug.Log ("maxDist");
                                nodeVisiting.distractTimes.AddRange(nodeTheClosestTo.distractTimes);
                                nodeVisiting.distractNums.AddRange(nodeTheClosestTo.distractNums);
                            }
                            else if (nodeTheClosestTo.distractTimes.Count > 0) {
                                if (distractPick) {
                                    //Debug.Log ("distractPick");
                                    nodeVisiting.distractTimes.AddRange(nodeTheClosestTo.distractTimes);
                                    nodeVisiting.distractNums.AddRange(nodeTheClosestTo.distractNums);
                                    nodeVisiting.distractTimes.Add(nodeVisiting.t);
                                    nodeVisiting.distractNums.Add(distractNum);
                                }
                                else {
                                    //Debug.Log ("Non-distractPick");
                                    nodeVisiting.distractTimes.AddRange(nodeTheClosestTo.distractTimes);
                                    nodeVisiting.distractNums.AddRange(nodeTheClosestTo.distractNums);
                                }
                            }
                            else if (distractPick) {
                                nodeVisiting.distractTimes.Add(nodeVisiting.t);
                                nodeVisiting.distractNums.Add(distractNum);
                            }
                        }
                        if (!viableFound) {
                            continue;
                        }
                    }
                    //DO SAME THING AS ELSE BRANCH
                    catch (System.ArgumentException e) {
                        nodeTheClosestTo = (NodeGeo)tree.nearest(new double[] { rx, rt, ry });

                        // cannot go back in time, so skip if t is decreasing
                        if (nodeTheClosestTo.t > nodeVisiting.t) {
                            continue;
                        }



                        // Only add if we are going in ANGLE degrees or higher.As there is a fixed max speed
                        p1 = nodeVisiting.GetVector3();
                        p2 = nodeTheClosestTo.GetVector3();
                        pd = p1 - p2;
                        if (Vector3.Angle(pd, new Vector3(pd.x, 0f, pd.z)) < angle) {
                            continue;
                        }

                        //Experimental New distract
                        if (nodeTheClosestTo.distractTimes.Count == maxDist) {
                            //Debug.Log ("maxDist");
                            nodeVisiting.distractTimes = nodeTheClosestTo.distractTimes;
                            nodeVisiting.distractNums = nodeTheClosestTo.distractNums;
                        }
                        else if (nodeTheClosestTo.distractTimes.Count > 0) {
                            if (distractPick) {
                                //Debug.Log ("distractPick");
                                nodeVisiting.distractTimes = nodeTheClosestTo.distractTimes;
                                nodeVisiting.distractNums = nodeTheClosestTo.distractNums;
                                nodeVisiting.distractTimes.Add(nodeVisiting.t);
                                nodeVisiting.distractNums.Add(distractNum);
                            }
                            else {
                                //Debug.Log ("Non-distractPick");
                                nodeVisiting.distractTimes = nodeTheClosestTo.distractTimes;
                                nodeVisiting.distractNums = nodeTheClosestTo.distractNums;
                            }
                        }
                        else if (distractPick) {
                            nodeVisiting.distractTimes.Add(nodeVisiting.t);
                            nodeVisiting.distractNums.Add(distractNum);
                        }
                    }

                }
                else {
                    nodeTheClosestTo = (NodeGeo)tree.nearest(new double[] { rx, rt, ry });

                    // cannot go back in time, so skip if t is decreasing
                    if (nodeTheClosestTo.t > nodeVisiting.t) {
                        continue;
                    }



                    // Only add if we are going in ANGLE degrees or higher.As there is a fixed max speed
                    p1 = nodeVisiting.GetVector3();
                    p2 = nodeTheClosestTo.GetVector3();
                    pd = p1 - p2;
                    if (Vector3.Angle(pd, new Vector3(pd.x, 0f, pd.z)) < angle) {
                        continue;
                    }

                    //Experimental New distract
                    if (nodeTheClosestTo.distractTimes.Count == maxDist) {
                        //Debug.Log ("maxDist");
                        nodeVisiting.distractTimes = nodeTheClosestTo.distractTimes;
                        nodeVisiting.distractNums = nodeTheClosestTo.distractNums;
                    }
                    else if (nodeTheClosestTo.distractTimes.Count > 0) {
                        if (distractPick) {
                            //Debug.Log ("distractPick");
                            nodeVisiting.distractTimes = nodeTheClosestTo.distractTimes;
                            nodeVisiting.distractNums = nodeTheClosestTo.distractNums;
                            nodeVisiting.distractTimes.Add(nodeVisiting.t);
                            nodeVisiting.distractNums.Add(distractNum);
                        }
                        else {
                            //Debug.Log ("Non-distractPick");
                            nodeVisiting.distractTimes = nodeTheClosestTo.distractTimes;
                            nodeVisiting.distractNums = nodeTheClosestTo.distractNums;
                        }
                    }
                    else if (distractPick) {
                        nodeVisiting.distractTimes.Add(nodeVisiting.t);
                        nodeVisiting.distractNums.Add(distractNum);
                    }
                }

                //Debug.Log(nodeVisiting.ToString());
                //Debug.Log(nodeTheClosestTo.ToString());

                //Old Backup Distract
                //if(nodeTheClosestTo.distractTime > 0){
                //	nodeVisiting.distractTime = nodeTheClosestTo.distractTime;
                //}
                //else if(distractPick){
                //	nodeVisiting.distractTime = nodeVisiting.t;
                //}




                //Check for collision with obstacles
                if (checkCollObs(p2.x, p2.z, p1.x, p1.z)) {

                    continue;
                }

                //Check for collision with guard line of sight -- OLD WAY
                //if(checkCollEs(p2.x, p2.z, (int)p2.y, p1.x, p1.z, (int)p1.y, enemies, 1, depth, nodeVisiting.distractTime)){
                //		continue;
                //}

                if (checkCollEs(p2.x, p2.z, (int)p2.y, p1.x, p1.z, (int)p1.y, enemies, 1, depth, nodeVisiting.distractTimes, nodeVisiting.distractNums)) {
                    continue;
                }




                try {
                    tree.insert(nodeVisiting.GetArray(), nodeVisiting);
                }
                catch (KeyDuplicateException) {
                }

                nodeVisiting.parent = nodeTheClosestTo;
                nodeVisiting.visited = true;

                curMaxX = Mathf.Max(Mathf.Min(nodeVisiting.x + rangeDist, maxX), curMaxX);
                curMinX = Mathf.Min(Mathf.Max(nodeVisiting.x - rangeDist, minX), curMinX);
                curMaxY = Mathf.Max(Mathf.Min(nodeVisiting.y + rangeDist, maxY), curMaxY);
                curMinY = Mathf.Min(Mathf.Max(nodeVisiting.y - rangeDist, minY), curMinY);
                curMaxT = Mathf.Max(Mathf.Min(nodeVisiting.t + rangeTime, maxT), curMaxT);
                if (k >= 0) {
                    if (!reachable[k].visited) {
                        reachable[k].visited = true;
                        triAdded = true;
                    }
                }


                if (nodeVisiting.t < nodeVisiting.parent.t) {
                    Debug.LogError("T-Failure Node Added");
                }


                // Attempt to connect to the end node
                if (Random.Range(0, 1000) > 0) {
                    p1 = nodeVisiting.GetVector3();
                    p2 = end.GetVector3();
                    p2.y = p1.y;
                    float dist = Vector3.Distance(p1, p2);
                    float t = dist * Mathf.Tan(angle * Mathf.Deg2Rad);
                    pd = p2;
                    pd.y += t;



                    NodeGeo endNode = GetNodeGeo((int)pd.y, pd.x, pd.z);


                    if (!checkCollObs(p1.x, p1.z, p2.x, p2.z) && !checkCollEs(p1.x, p1.z, (int)p1.y, pd.x, pd.z, (int)pd.y, enemies, 1, depth, nodeVisiting.distractTimes, nodeVisiting.distractNums)) {
                        //Debug.Log ("Done3");
                        endNode.parent = nodeVisiting;
                        endNode.distractTimes = nodeVisiting.distractTimes;
                        endNode.distractNums = nodeVisiting.distractNums;
                        if (debugging) {
                            DrawTree(start, minX, maxX, minY, maxY, maxT);
                            DrawSamples();
                        }
                        return endNode;
                    }
                }

                //Might be adding the neighboor as a the goal
                if (Mathf.Approximately(nodeVisiting.x, end.x) & Mathf.Approximately(nodeVisiting.y, end.y)) {
                    //Debug.Log ("Done2");
                    if (debugging) {
                        DrawTree(start, minX, maxX, minY, maxY, maxT);
                        DrawSamples();
                    }
                    return nodeVisiting;

                }
            }

            //End RRT algo

            if (debugging) {
                DrawTree(start, minX, maxX, minY, maxY, maxT);
                DrawSamples();
            }

            return null;
        }

        public RRTResult ComputeGeoFromPartials(float startX, float startY, float endX, float endY, float minX, float maxX, float minY, float maxY, int maxT, int attemps, int attemps2, float speed, Vector2 distractPos, Vector2 distractPos2, List<Triangle> tris) {
            RRTResult toReturn = new RRTResult();
            toReturn.startTime = System.DateTime.Now;
            toReturn.rrtsUsedMM.Add(0);
            toReturn.rrtsUsed2MM.Add(0);
            toReturn.nodesUsedMM.Add(0);
            toReturn.nodesUsed2MM.Add(0);
            toReturn.nodesRejectedMM.Add(0);
            toReturn.nodesRejected2MM.Add(0);
            int index = 0;
            toReturn.type = "2-1";

            tree = new KDTree(3);
            NodeGeo start = GetNodeGeo(0, startX, startY);
            int startTriIndex = -1;
            int endTriIndex = -1;

            foreach (Triangle tri in tris) {
                tri.resetTriProps();
            }

            for (int i = 0; i < tris.Count; i++) {
                Triangle tri = tris[i];
                if (tri.containsPoint(new Vector3(startX, 1, startY))) {
                    if (startTriIndex >= 0) {
                        Debug.Log("PROBLEMO 1");
                    }
                    tri.visited = true;
                    startTriIndex = i;
                }
                if (tri.containsPoint(new Vector3(endX, 1, endY))) {
                    endTriIndex = i;
                }
            }
            tris[startTriIndex].visited = true;
            Triangulation.computeDistanceTree(tris[startTriIndex]);
            float distToEnd = tris[endTriIndex].distance;
            //GameObject bobo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //bobo.transform.position = tris[endTriIndex].GetCenterTriangle();
            //bobo.name = "EndTri";


            float halfDist = distToEnd / 2;
            halfDist = halfDist - 5f;
            Triangle halfway = tris[endTriIndex];
            float distDiff = halfDist + 10f;
            //Debug.Log(distToEnd);
            if (distToEnd > triMaxRRTDist) {
                tris.Reverse();
                foreach (Triangle t in tris) {
                    if (Mathf.Abs(t.distance - halfDist) < distDiff + 0.05f) {
                        if (Mathf.Abs(t.distance - halfDist) > distDiff - 0.05f) {
                            if (Random.Range(0f, 1f) > 0.5f) {
                                halfway = t;
                                distDiff = Mathf.Abs(t.distance - halfDist);
                            }
                        }
                        else {
                            halfway = t;
                            distDiff = Mathf.Abs(t.distance - halfDist);
                        }
                    }
                }

                //Debug.Log(distDiff);
                //Debug.Log(halfway.distance);
                //GameObject bob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                //bob.transform.position = halfway.GetCenterTriangle();
                //bob.name = "HALFWAYPOINT";

                float partEndX = halfway.GetCenterTriangle().x;
                float partEndY = halfway.GetCenterTriangle().z;

                NodeGeo partNode = null;
                for (int i = 0; i < attemps2; i++) {
                    toReturn.rrtsUsedMM[index]++;
                    partNode = ComputePartialGeo(start, partEndX, partEndY, minX, maxX, minY, maxY, 0, maxT, attemps, speed, distractPos, distractPos2, tris, false, toReturn, index);
                    if (partNode == null) {
                        foreach (Triangle t in tris) {
                            t.resetTriProps();
                            tris[startTriIndex].visited = true;
                            Triangulation.computeDistanceTree(tris[startTriIndex]);
                        }
                    }
                    else {
                        break;
                    }
                }
                if (partNode == null) {

                    return toReturn;
                }
                //Debug.Log("REACHED PARTWAY TO" + partNode);
                NodeGeo endNode = null;
                for (int i = 0; i < attemps2; i++) {
                    toReturn.rrtsUsed2MM[index]++;
                    endNode = ComputePartialGeo(partNode, endX, endY, minX, maxX, minY, maxY, partNode.t, maxT * 2, attemps, speed, distractPos, distractPos2, tris, true, toReturn, index);
                    if (endNode == null) {
                        foreach (Triangle t in tris) {
                            t.resetTriProps();
                            //tris[startTriIndex].visited = true;
                            //Triangulation.computeDistanceTree(tris[startTriIndex]);
                        }
                    }
                    else {
                        toReturn.success = true;
                        break;
                    }

                }
                toReturn.Nodes = ReturnPathGeo(endNode, false);
                if (toReturn.Nodes.Count > 0) {
                    toReturn.success = true;
                }
                return toReturn;
            }
            else {
                NodeGeo endNode = null;
                for (int i = 0; i < attemps2; i++) {
                    toReturn.rrtsUsedMM[index]++;
                    endNode = ComputePartialGeo(start, endX, endY, minX, maxX, minY, maxY, 0, maxT, attemps, speed, distractPos, distractPos2, tris, false, toReturn, index);
                    if (endNode == null) {
                        foreach (Triangle t in tris) {
                            t.resetTriProps();
                           // tris[startTriIndex].visited = true;
                            //Triangulation.computeDistanceTree(tris[startTriIndex]);
                        }
                    }
                    else {
                        toReturn.success = true;
                        break;
                    }
                }
                toReturn.Nodes = ReturnPathGeo(endNode, false);
                if (toReturn.Nodes.Count  > 0) {
                    toReturn.success = true;
                }
                return toReturn;
            }
        }

        public RRTResult ComputeGeosFromPartials(float startX, float startY, float endX, float endY, float minX, float maxX, float minY, float maxY, int maxT, int attemps, int attemps2, float speed, Vector2 distractPos, Vector2 distractPos2, List<Triangle> tris) {
            //Debug.Log("1");
            tree = new KDTree(3);
            NodeGeo start = GetNodeGeo(0, startX, startY);
            int startTriIndex = -1;
            int endTriIndex = -1;

                
            RRTResult toReturn = new RRTResult();
            toReturn.startTime = System.DateTime.Now;
            toReturn.type = "2-2";
            int index = -1;
            //Debug.Log("2");

            foreach (Triangle tri in tris) {
                tri.resetTriProps();
            }

            for (int i = 0; i < tris.Count; i++) {
                Triangle tri = tris[i];
                if (tri.containsPoint(new Vector3(startX, 1, startY))) {
                    if (startTriIndex >= 0) {
                        Debug.Log("PROBLEMO 1");
                    }
                    tri.visited = true;
                    startTriIndex = i;
                }
                if (tri.containsPoint(new Vector3(endX, 1, endY))) {
                    endTriIndex = i;
                }
            }
            tris[startTriIndex].visited = true;
            //Debug.Log("3");

            Triangulation.computeDistanceTreeE(tris[startTriIndex], tris[endTriIndex]);
            float distToEnd = tris[endTriIndex].distance;


            float halfDist = distToEnd / 2;
            halfDist = halfDist - 5f;
            float distDiff = halfDist + 10f;
            //Debug.Log(distToEnd);
            //Debug.Log("4");
            if (distToEnd > triMaxRRTDist) {
                //Debug.Log("5");
                List<List<int>> paths = Triangulation.findAllSimpleEndPaths(tris[startTriIndex], tris[endTriIndex]);
                //Debug.Log(paths);
                //Debug.Log(paths.Count);
                //foreach (List<int> p in paths) {
                //    Debug.Log(p);
                 //   foreach(int i in p) {
                //        Debug.Log(i);
                //    }
                //}

                List<Triangle> midTris = new List<Triangle>();
                foreach (List<int> pth in paths) {
                    //Debug.Log("5.1");
                    Triangle t = Triangulation.findMidTriangleAlongPath(tris[startTriIndex], tris[endTriIndex], pth);
                    if (!midTris.Contains(t)) {
                        midTris.Add(t);
                    }
                }
                //Debug.Log("5.2");

                //int q = 0;
                foreach (Triangle halfway in midTris) {
                    //q++;
                    //Debug.Log("Testing for Midpoint " + q + " which is at location:" + halfway.GetCenterTriangle());
                    //Debug.Log("5.3");
                    index++;
                    toReturn.rrtsUsedMM.Add(0);
                    toReturn.rrtsUsed2MM.Add(0);
                    toReturn.nodesUsedMM.Add(0);
                    toReturn.nodesUsed2MM.Add(0);
                    toReturn.nodesRejectedMM.Add(0);
                    toReturn.nodesRejected2MM.Add(0);

                    float partEndX = halfway.GetCenterTriangle().x;
                    float partEndY = halfway.GetCenterTriangle().z;

                    NodeGeo partNode = null;
                    for (int i = 0; i < attemps2; i++) {
                        toReturn.rrtsUsedMM[index]++;
                        //Debug.Log("Attempting search part 1 -" + i);
                        partNode = ComputePartialGeo(start, partEndX, partEndY, minX, maxX, minY, maxY, 0, maxT, attemps, speed, distractPos, distractPos2, tris, false, toReturn, index);
                        //Debug.Log("7");
                        if (partNode == null) {
                            //Debug.Log("Failed at search part 1 -" + i);
                            //Debug.Log("8");
                            foreach (Triangle t in tris) {
                                t.resetTriProps();
                                tris[startTriIndex].visited = true;
                                Triangulation.computeDistanceTree(tris[startTriIndex]);
                            }
                            
                        }
                        else {
                            //Debug.Log("Succeeded at search part 1 -" + i);
                            break;
                        }
                    }
                    if (partNode == null) {
                        //Debug.Log("Failed search 1 at midpoint" + q);
                        continue;
                    }
                    //Debug.Log("REACHED PARTWAY TO" + partNode);
                    NodeGeo endNode = null;
                    for (int i = 0; i < attemps2; i++) {
                        toReturn.rrtsUsed2MM[index]++;
                        //Debug.Log("Attempting search part 2 -" + i);
                        endNode = ComputePartialGeo(partNode, endX, endY, minX, maxX, minY, maxY, partNode.t, maxT * 2, attemps, speed, distractPos, distractPos2, tris, true, toReturn, index);
                        if (endNode == null) {
                            //Debug.Log("Failed at search part 2 -" + i);
                            foreach (Triangle t in tris) {
                                t.resetTriProps();
                                //tris[startTriIndex].visited = true;
                                //Triangulation.computeDistanceTree(tris[startTriIndex]);
                            }
                        }
                        else {
                            toReturn.success = true;
                            //Debug.Log("Succeeded at search part 1 -" + i);
                            break;
                        }

                    }
                    if(endNode == null) {
                        //Debug.Log("Failed search 2 at midpoint" + q);
                        //TODO: CHANGE
                        continue;
                    }
                    else {
                        //Debug.Log("Succeeded at full search at midpoint" + q);
                        toReturn.Nodes = ReturnPathGeo(endNode, false);
                        if (toReturn.Nodes.Count > 0) {
                            toReturn.success = true;
                        }
                        return toReturn;
                    }       
                }
                //Debug.Log("Failed All Searches");
                return toReturn;
            }
            else {
                index++;
                toReturn.rrtsUsedMM.Add(0);
                toReturn.rrtsUsed2MM.Add(0);
                toReturn.nodesUsedMM.Add(0);
                toReturn.nodesUsed2MM.Add(0);
                toReturn.nodesRejectedMM.Add(0);
                toReturn.nodesRejected2MM.Add(0);
                NodeGeo endNode = null;
                for (int i = 0; i < attemps2; i++) {
                    toReturn.rrtsUsedMM[index]++;
                    endNode = ComputePartialGeo(start, endX, endY, minX, maxX, minY, maxY, 0, maxT, attemps, speed, distractPos, distractPos2, tris, false,toReturn, index);
                    if (endNode == null) {
                        foreach (Triangle t in tris) {
                            t.resetTriProps();
                            // tris[startTriIndex].visited = true;
                            //Triangulation.computeDistanceTree(tris[startTriIndex]);
                        }
                    }
                    else {
                        toReturn.success = true;
                        break;
                    }
                }
                toReturn.Nodes = ReturnPathGeo(endNode, false);
                if (toReturn.Nodes.Count > 0) {
                    toReturn.success = true;
                }
                return toReturn;
            }
        }
        /*
        public List<NodeGeo> ComputeGeoFromPartials(float startX, float startY, float endX, float endY, float minX, float maxX, float minY, float maxY, int maxT, int attemps, float speed, Vector2 distractPos, Vector2 distractPos2, List<Triangle> tris) {
            tree = new KDTree(3);
            NodeGeo start = GetNodeGeo(0, startX, startY);
            int startTriIndex = -1;
            int endTriIndex = -1;
            
            foreach(Triangle tri in tris) {
                tri.visited = false;
                tri.distance = float.MaxValue;
            }

            for (int i = 0; i < tris.Count; i++) {
                Triangle tri = tris[i];
                if (tri.containsPoint(new Vector3(startX, 1, startY))) {
                    if (startTriIndex >= 0) {
                        Debug.Log("PROBLEMO 1");
                    }
                    tri.visited = true;
                    startTriIndex = i;
                }
                if (tri.containsPoint(new Vector3(endX, 1, endY))) {
                    endTriIndex = i;
                }
            }
            tris[startTriIndex].visited = true;
            Triangulation.computeDistanceTree(tris[startTriIndex]);
            float distToEnd = tris[endTriIndex].distance;
            GameObject bobo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bobo.transform.position = tris[endTriIndex].GetCenterTriangle();
            bobo.name = "EndTri";


            float halfDist = distToEnd / 2;
            halfDist = halfDist - 5f;
            Triangle halfway = tris[endTriIndex];
            float distDiff = halfDist + 10f;
            //Debug.Log(distToEnd);
            if (distToEnd > triMaxRRTDist) {
                foreach(Triangle t in tris) {
                    if(Mathf.Abs(t.distance - halfDist) < distDiff) {
                        halfway = t;
                        distDiff = Mathf.Abs(t.distance - halfDist);
                    }
                }

                //Debug.Log(distDiff);
                //Debug.Log(halfway.distance);
                GameObject bob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bob.transform.position = halfway.GetCenterTriangle();
                bob.name = "HALFWAYPOINT";

                float partEndX = halfway.GetCenterTriangle().x;
                float partEndY = halfway.GetCenterTriangle().z;

                NodeGeo partNode = ComputePartialGeo(start, partEndX, partEndY, minX, maxX, minY, maxY,0, maxT, attemps, speed, distractPos, distractPos2, tris, false);
                if(partNode == null) {
                    return new List<NodeGeo>();
                }
                Debug.Log("REACHED PARTWAY TO" + partNode);
                NodeGeo endNode = ComputePartialGeo(partNode, endX, endY, minX, maxX, minY, maxY, partNode.t, maxT*2, attemps, speed, distractPos, distractPos2, tris, true);
                return ReturnPathGeo(endNode, false);
            }
            else {
                NodeGeo endNode = ComputePartialGeo(start, endX, endY, minX, maxX, minY, maxY,0, maxT, attemps, speed, distractPos, distractPos2, tris, false);
                return ReturnPathGeo(endNode, false);
            }
        }
        */



        public void generateMidPoint(float startX, float startY, float endX, float endY, List<Triangle> tris, bool e) {
            Debug.Log("GenMId" + e);
            int startTriIndex = -1;
            int endTriIndex = -1;

            foreach (Triangle tri in tris) {
                tri.resetTriProps();
            }

            for (int i = 0; i < tris.Count; i++) {
                Triangle tri = tris[i];
                if (tri.containsPoint(new Vector3(startX, 1, startY))) {
                    if (startTriIndex >= 0) {
                        Debug.Log("PROBLEMO 1");
                    }
                    tri.visited = true;
                    startTriIndex = i;
                }
                if (tri.containsPoint(new Vector3(endX, 1, endY))) {
                    endTriIndex = i;
                }
            }
            tris[startTriIndex].visited = true;
            if (e) {
                Triangulation.computeDistanceTreeE(tris[startTriIndex], tris[endTriIndex]);
            }
            else { 
                Triangulation.computeDistanceTree(tris[startTriIndex]);
            }
            float distToEnd = tris[endTriIndex].distance;

            float halfDist = distToEnd / 2;
            halfDist = halfDist - 5f;
            Triangle halfway = tris[endTriIndex];
            float distDiff = halfDist + 10f;
            Debug.Log("Dist to End:" + distToEnd);
            Debug.Log("HalfDist:" + halfDist);


            if (distToEnd > triMaxRRTDist) {
                foreach (Triangle t in tris) {
                    if (Mathf.Abs(t.distance - halfDist) < distDiff + 0.05f) {
                        if(Mathf.Abs(t.distance - halfDist) > distDiff - 0.05f) {
                            if(Random.Range(0f,1f) > 0.5f) {
                                halfway = t;
                                distDiff = Mathf.Abs(t.distance - halfDist);
                            }
                        }
                        else {
                            halfway = t;
                            distDiff = Mathf.Abs(t.distance - halfDist);
                        }                        
                    }
                }
                Debug.Log("TriDist:" + halfway.distance);

                //Debug.Log(distDiff);
                //Debug.Log(halfway.distance);
                GameObject bob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bob.transform.position = halfway.GetCenterTriangle();
                bob.name = "HALFWAYPOINT";
                if (e) {
                    bob.name = bob.name + "e";
                }

            }
        }


        public void generateMidPoints(float startX, float startY, float endX, float endY, List<Triangle> tris) {
            //Debug.Log("GenMids");
            int startTriIndex = -1;
            int endTriIndex = -1;

            foreach (Triangle tri in tris) {
                tri.resetTriProps();
            }

            for (int i = 0; i < tris.Count; i++) {
                Triangle tri = tris[i];
                if (tri.containsPoint(new Vector3(startX, 1, startY))) {
                    if (startTriIndex >= 0) {
                        Debug.Log("PROBLEMO 1");
                    }
                    tri.visited = true;
                    startTriIndex = i;
                }
                if (tri.containsPoint(new Vector3(endX, 1, endY))) {
                    endTriIndex = i;
                }
            }
            tris[startTriIndex].visited = true;
            Triangulation.computeDistanceTreeE(tris[startTriIndex], tris[endTriIndex]);
            float distToEnd = tris[endTriIndex].distance;

            float halfDist = distToEnd / 2;
            halfDist = halfDist - 5f;
            Triangle halfway = tris[endTriIndex];
            float distDiff = halfDist + 10f;

            //Debug.Log("Dist to End:" + distToEnd);
            //Debug.Log("HalfDist:" + halfDist);


            if (distToEnd > triMaxRRTDist) {
                List<List<int>> paths = Triangulation.findAllSimpleEndPaths(tris[startTriIndex], tris[endTriIndex]);
                List<Triangle> midTris = new List<Triangle>();

                int z = 0;
                foreach (List<int> pth in paths) {
                    //Debug.Log("5.1");
                    //Debug.Log("FInding a MId");
                    Triangle t = Triangulation.findMidTriangleAlongPath(tris[startTriIndex], tris[endTriIndex], pth);
                    if (!midTris.Contains(t)) {
                        z++;
                        midTris.Add(t);
                        GameObject bob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        bob.transform.position = t.GetCenterTriangle();
                        bob.name = "HALFWAYPOINT" + z;
                    }
                    else {
                        GameObject bob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        bob.transform.position = t.GetCenterTriangle();
                        bob.name = "OHALFWAYPOINT" + z;

                    }
                }


            }
        }


        public RRTResult ComputeGeo (float startX, float startY, float endX, float endY, float minX, float maxX, float minY, float maxY, int maxT, int attemps, float speed, Vector2 distractPos, Vector2 distractPos2, List<Triangle> tris, RRTResult toReturn, int index, List<Geometry> obs, bool smooth = false) {
            //Debug.Log ("COMPUTEGEO");
            

            toReturn.type = "normal";

			// Initialization
			tree = new KDTree (3);
			explored = new List<NodeGeo> ();
			//nodeMatrix = matrix;
			
			//Start and ending node
			NodeGeo start = GetNodeGeo (0, startX, startY);
			start.visited = true; 
			start.parent = null;
			
			// Prepare start and end node
			NodeGeo end = GetNodeGeo (0, endX, endY);
			tree.insert (start.GetArray (), start);
			explored.Add (start);
			
			// Prepare the variables		
			NodeGeo nodeVisiting = null;
			NodeGeo nodeTheClosestTo = null;
			
			float tan = speed / 1.0f;
			angle = 90 - Mathf.Atan (tan) * Mathf.Rad2Deg;



            // WHAT IS THIS??
            /*
			List<Distribution.Pair> pairs = new List<Distribution.Pair> ();
			
			for (int x = 0; x < matrix[0].Length; x++) 
				for (int y = 0; y < matrix[0].Length; y++) 
					if (((Cell)matrix [0] [x] [y]).waypoint)
						pairs.Add (new Distribution.Pair (x, y));
			
			pairs.Add (new Distribution.Pair (end.x, end.y));
			
			//Distribution rd = new Distribution(matrix[0].Length, pairs.ToArray());
			*/


            float curMaxX = Mathf.Min(startX + rangeDist, maxX);
            float curMinX = Mathf.Max(startX - rangeDist, minX);
            float curMaxY = Mathf.Min(startY + rangeDist, maxY);
            float curMinY = Mathf.Max(startY - rangeDist, minY);
            int curMaxT = Mathf.Min(rangeTime, maxT);

            
                int startTriIndex = -1;
            if (useTriangulation) {
                if (useDist) {
                    foreach (Triangle tri in tris) {
                        tri.visited = false;
                        tri.distance = float.MaxValue;
                    }
                }
                
            }

                List<float> areas = new List<float>();
                float areaSum = 0;
            if (useTriangulation) {
                for (int i = 0; i < tris.Count; i++) {
                    Triangle tri = tris[i];
                    /*Line[] lins = tri.getLines();
                    float l1 = lins[0].Magnitude();
                    float l2 = lins[1].Magnitude();
                    float l3 = lins[2].Magnitude();
                    float s = 0.5f * (l1 + l2 + l3);
                    float area = Mathf.Sqrt(s * (s - l1) * (s - l2) * (s - l3));
                    tri.area = area;
                    areas.Add(area);
                    areaSum = areaSum + area;*/
                    if (tri.containsPoint(new Vector3(startX, 1, startY))) {
                        if (startTriIndex >= 0) {
                            Debug.Log("PROBLEMO 1");
                        }
                        tri.visited = true;
                        startTriIndex = i;
                    }
                }
                if (startTriIndex < 0) {
                    Debug.Log("PROBLEMO 2");
                }
                Triangulation.computeDistanceTree(tris[startTriIndex]);
                /*GameObject triDists = new GameObject("triDists");
                foreach(Triangle tri in tris) {
                    Debug.Log(tri.GetCenterTriangle());
                    GameObject lin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    lin.GetComponent<Renderer>().sharedMaterial.color = Color.red;
                    lin.transform.parent = triDists.transform;
                    lin.transform.position = tri.GetCenterTriangle();
                    lin.transform.position = new Vector3(lin.transform.position.x, tri.distance, lin.transform.position.z);
                    Debug.Log(tri.distance);
                }*/


            }




                List<float> standardizedAreas = new List<float>();
                List<Triangle> reachable = new List<Triangle>();
                float sumSoFar = 0;
                /*foreach (float a in areas) {
                    standardizedAreas.Add((a + sumSoFar) / areaSum);
                    sumSoFar = sumSoFar + a;
                }*/

                bool triAdded = true;
            if (useTriangulation) { 
                if (!useDist) {
                    reachable = new List<Triangle>();
                    reachable.AddRange(tris);
                    areas = new List<float>();
                    areaSum = 0;
                    for (int j = 0; j < reachable.Count; j++) {
                        Triangle tri = reachable[j];
                        Line[] lins = tri.getLines();
                        float l1 = lins[0].Magnitude();
                        float l2 = lins[1].Magnitude();
                        float l3 = lins[2].Magnitude();
                        float s = 0.5f * (l1 + l2 + l3);
                        float area = Mathf.Sqrt(s * (s - l1) * (s - l2) * (s - l3));
                        tri.area = area;
                        areas.Add(area);
                        areaSum = areaSum + area;
                    }
                    standardizedAreas = new List<float>();
                    sumSoFar = 0;
                    foreach (float a in areas) {
                        standardizedAreas.Add((a + sumSoFar) / areaSum);
                        sumSoFar = sumSoFar + a;
                    }
                }
            }


            //RRT algo
            for (int i = 0; i <= attemps; i++) {
                toReturn.nodesUsedMM[index]++;
                if (useTriangulation) {
                    if (triAdded) {
                        Triangulation.computeDistanceTree(tris[startTriIndex]);
                        reachable = new List<Triangle>();
                        foreach (Triangle tri in tris) {
                            if (tri.distance < triRangeDist) {
                                reachable.Add(tri);
                            }
                        }
                        areas = new List<float>();
                        areaSum = 0;
                        for (int j = 0; j < reachable.Count; j++) {
                            Triangle tri = reachable[j];
                            Line[] lins = tri.getLines();
                            float l1 = lins[0].Magnitude();
                            float l2 = lins[1].Magnitude();
                            float l3 = lins[2].Magnitude();
                            float s = 0.5f * (l1 + l2 + l3);
                            float area = Mathf.Sqrt(s * (s - l1) * (s - l2) * (s - l3));
                            tri.area = area;
                            areas.Add(area);
                            areaSum = areaSum + area;
                        }
                        standardizedAreas = new List<float>();
                        sumSoFar = 0;
                        foreach (float a in areas) {
                            standardizedAreas.Add((a + sumSoFar) / areaSum);
                            sumSoFar = sumSoFar + a;
                        }

                    }
                }

				//Then pick random x and y values
				float rx;
				float ry;
                float trisInd;
                int k = 0;
                float rl1;
                float rl2;

				bool distractPick = false;
				int distractNum = -1;


                if (Random.Range(0, 100) > distractables) {
                    //TODO
                    k = -1;
                    if (Random.Range(0, 100) > 50) {
                        rx = distractPos.x;
                        ry = distractPos.y;
                        distractPick = true;
                        distractNum = 0;
                    }
                    else {
                        rx = distractPos2.x;
                        ry = distractPos2.y;
                        distractPick = true;
                        distractNum = 1;
                    }
                }
                else {
                    /*
					rx = Random.Range (curMinX, curMaxX);
					ry = Random.Range (curMinY, curMaxY);
                    */
                    if (useTriangulation) { 

                        trisInd = Random.Range(0f, 1f);
                        for (k = 0; k < standardizedAreas.Count; k++) {
                            if (trisInd <= standardizedAreas[k]) {
                                break;
                            }
                        }
                        Triangle tri = reachable[k];
                        rl1 = 1.0f;
                        rl2 = 2.0f;
                        while (rl2 > rl1) {
                            rl1 = Random.Range(0f, 1f);
                            rl2 = Random.Range(0f, 1f);
                        }
                        Vector3 point = tri.vertex[0] + rl1 * (tri.vertex[1] - tri.vertex[0]) + rl2 * (tri.vertex[2] - tri.vertex[1]);
                
                        rx = point.x;
                        ry = point.z;
                    }
                    else {
                        if (useDist) { 
                            rx = Random.Range(curMinX, curMaxX);
                            ry = Random.Range(curMinY, curMaxY);
                        }
                        else {
                            rx = Random.Range(minX, maxX);
                            ry = Random.Range(minY, maxY);
                        }
                    }
                }

                Vector3 pdO = new Vector3(rx - startX, 0, ry - startY);
                float pdd = pdO.magnitude;
                float fminT = pdd * Mathf.Tan(angle * Mathf.Deg2Rad);
            

                int minT = Mathf.FloorToInt(fminT);

                //Pick a random time
                //int rt = Random.Range (1,maxT);
                int rt;
                if (useDist) { 
                    rt = Random.Range(minT, curMaxT);
                }
                else {
                    rt = Random.Range(minT, maxT);
                }




                //int rx = p.x, ry = p.y;
                nodeVisiting = GetNodeGeo (rt, rx, ry);
				//if this node has already been visited continue
				if (nodeVisiting.visited) {
					i--;
                    toReturn.nodesRejectedMM[index]++;
					//Consider checking if point is valid earlier, for i--.
					continue;
				}
				
				explored.Add (nodeVisiting);
                if (checkWaste) {
                    //Debug.Log("Checking");
                    if (oldCheckInObs(rx, ry, obs)) {
                        //Debug.Log("Found1");
                        toReturn.nodesWasted++;
                        continue;
                    }
                }

                Vector3 p1 = new Vector3();
				Vector3 p2 = new Vector3();
				Vector3 pd = new Vector3();

				if(preventDistractStacks){
					try{
						object[] closestNodes = tree.nearest(new double[] {rx, rt, ry}, numNears);					
						bool viableFound = false;

						for(int ind = 0; ind < numNears; ind++){
							nodeTheClosestTo = (NodeGeo)closestNodes[ind];

                            // cannot go back in time, so skip if t is decreasing
                            if (nodeTheClosestTo.t > nodeVisiting.t)
                            {
                                viableFound = false;
                                continue;
                            }

							// Only add if we are going in ANGLE degrees or higher.As there is a fixed max speed
							p1 = nodeVisiting.GetVector3 ();
							p2 = nodeTheClosestTo.GetVector3 ();
							pd = p1 - p2;
							if (Vector3.Angle (pd, new Vector3 (pd.x, 0f, pd.z)) < angle) {
                                viableFound = false;
                                continue;
							}

							if(distractPick){
								if(Mathf.Approximately(p1.x, p2.x) && Mathf.Approximately(p1.z, p2.z)){
                                    viableFound = false;
                                    continue;
								}
							}
							viableFound = true;

                            nodeVisiting.distractTimes = new List<int>();
                            nodeVisiting.distractNums = new List<int>();
                            
                            
                            //Experimental New distract
							if(nodeTheClosestTo.distractTimes.Count == maxDist){
								//Debug.Log ("maxDist");
								nodeVisiting.distractTimes.AddRange(nodeTheClosestTo.distractTimes);
								nodeVisiting.distractNums.AddRange(nodeTheClosestTo.distractNums);
							}
							else if(nodeTheClosestTo.distractTimes.Count > 0){
								if(distractPick){
									//Debug.Log ("distractPick");
									nodeVisiting.distractTimes.AddRange(nodeTheClosestTo.distractTimes);
									nodeVisiting.distractNums.AddRange(nodeTheClosestTo.distractNums);
									nodeVisiting.distractTimes.Add (nodeVisiting.t);
									nodeVisiting.distractNums.Add (distractNum);
								}
								else{
									//Debug.Log ("Non-distractPick");
									nodeVisiting.distractTimes.AddRange(nodeTheClosestTo.distractTimes);
									nodeVisiting.distractNums.AddRange(nodeTheClosestTo.distractNums);
								}
							}
							else if(distractPick){
								nodeVisiting.distractTimes.Add (nodeVisiting.t);
								nodeVisiting.distractNums.Add (distractNum);
							}
						}
						if(!viableFound){
							continue;
						}
					}
					//DO SAME THING AS ELSE BRANCH
					catch (System.ArgumentException e){
                        nodeTheClosestTo = (NodeGeo)tree.nearest (new double[] {rx, rt, ry});
						
						// cannot go back in time, so skip if t is decreasing
						if (nodeTheClosestTo.t > nodeVisiting.t){
							continue;
						}
						
						
						
						// Only add if we are going in ANGLE degrees or higher.As there is a fixed max speed
						p1 = nodeVisiting.GetVector3 ();
						p2 = nodeTheClosestTo.GetVector3 ();
						pd = p1 - p2;
						if (Vector3.Angle (pd, new Vector3 (pd.x, 0f, pd.z)) < angle) {
							continue;
						}				
						
						//Experimental New distract
						if(nodeTheClosestTo.distractTimes.Count == maxDist){
							//Debug.Log ("maxDist");
							nodeVisiting.distractTimes = nodeTheClosestTo.distractTimes;
							nodeVisiting.distractNums = nodeTheClosestTo.distractNums;
						}
						else if(nodeTheClosestTo.distractTimes.Count > 0){
							if(distractPick){
								//Debug.Log ("distractPick");
								nodeVisiting.distractTimes = nodeTheClosestTo.distractTimes;
								nodeVisiting.distractNums = nodeTheClosestTo.distractNums;
								nodeVisiting.distractTimes.Add (nodeVisiting.t);
								nodeVisiting.distractNums.Add (distractNum);
							}
							else{
								//Debug.Log ("Non-distractPick");
								nodeVisiting.distractTimes = nodeTheClosestTo.distractTimes;
								nodeVisiting.distractNums = nodeTheClosestTo.distractNums;
							}
						}
						else if(distractPick){
							nodeVisiting.distractTimes.Add (nodeVisiting.t);
							nodeVisiting.distractNums.Add (distractNum);
						}
					}

				}
				else{
                    nodeTheClosestTo = (NodeGeo)tree.nearest (new double[] {rx, rt, ry});
					
					// cannot go back in time, so skip if t is decreasing
					if (nodeTheClosestTo.t > nodeVisiting.t){
						continue;
					}


					
					// Only add if we are going in ANGLE degrees or higher.As there is a fixed max speed
					p1 = nodeVisiting.GetVector3 ();
					p2 = nodeTheClosestTo.GetVector3 ();
					pd = p1 - p2;
					if (Vector3.Angle (pd, new Vector3 (pd.x, 0f, pd.z)) < angle) {
						continue;
					}				

					//Experimental New distract
					if(nodeTheClosestTo.distractTimes.Count == maxDist){
						//Debug.Log ("maxDist");
						nodeVisiting.distractTimes = nodeTheClosestTo.distractTimes;
						nodeVisiting.distractNums = nodeTheClosestTo.distractNums;
					}
					else if(nodeTheClosestTo.distractTimes.Count > 0){
						if(distractPick){
							//Debug.Log ("distractPick");
							nodeVisiting.distractTimes = nodeTheClosestTo.distractTimes;
							nodeVisiting.distractNums = nodeTheClosestTo.distractNums;
							nodeVisiting.distractTimes.Add (nodeVisiting.t);
							nodeVisiting.distractNums.Add (distractNum);
						}
						else{
							//Debug.Log ("Non-distractPick");
							nodeVisiting.distractTimes = nodeTheClosestTo.distractTimes;
							nodeVisiting.distractNums = nodeTheClosestTo.distractNums;
						}
					}
					else if(distractPick){
						nodeVisiting.distractTimes.Add (nodeVisiting.t);
						nodeVisiting.distractNums.Add (distractNum);
					}
				}

                //Debug.Log(nodeVisiting.ToString());
                //Debug.Log(nodeTheClosestTo.ToString());

                //Old Backup Distract
                //if(nodeTheClosestTo.distractTime > 0){
                //	nodeVisiting.distractTime = nodeTheClosestTo.distractTime;
                //}
                //else if(distractPick){
                //	nodeVisiting.distractTime = nodeVisiting.t;
                //}


                //Check for collision with obstacles
                if (checkCollObs(p2.x, p2.z, p1.x, p1.z)){
                    
					continue;
				}
				
				//Check for collision with guard line of sight -- OLD WAY
				//if(checkCollEs(p2.x, p2.z, (int)p2.y, p1.x, p1.z, (int)p1.y, enemies, 1, depth, nodeVisiting.distractTime)){
				//		continue;
				//}

				if(checkCollEs(p2.x, p2.z, (int)p2.y, p1.x, p1.z, (int)p1.y, enemies, 1, depth, nodeVisiting.distractTimes, nodeVisiting.distractNums)){
					continue;
				}



				
				try {
					tree.insert (nodeVisiting.GetArray (), nodeVisiting);
				} catch (KeyDuplicateException) {
				}

				nodeVisiting.parent = nodeTheClosestTo;
				nodeVisiting.visited = true;

                if (useDist) { 

                    curMaxX = Mathf.Max(Mathf.Min(nodeVisiting.x + rangeDist, maxX), curMaxX);
                    curMinX = Mathf.Min(Mathf.Max(nodeVisiting.x - rangeDist, minX), curMinX);
                    curMaxY = Mathf.Max(Mathf.Min(nodeVisiting.y + rangeDist, maxY), curMaxY);
                    curMinY = Mathf.Min(Mathf.Max(nodeVisiting.y - rangeDist, minY), curMinY);
                    curMaxT = Mathf.Max(Mathf.Min(nodeVisiting.t + rangeTime, maxT), curMaxT);
                }
                if (useTriangulation) {
                    if (k >= 0) {
                        if (!reachable[k].visited) {
                            reachable[k].visited = true;
                            triAdded = true;
                        }
                    }
                }


                if (nodeVisiting.t < nodeVisiting.parent.t)
                {
                    Debug.LogError("T-Failure Node Added");
                }


                // Attempt to connect to the end node
                if (Random.Range (0, 1000) > 0) {
					p1 = nodeVisiting.GetVector3 ();
					p2 = end.GetVector3 ();
					p2.y = p1.y;
					float dist = Vector3.Distance (p1, p2);
					float t = dist * Mathf.Tan (angle * Mathf.Deg2Rad);
					pd = p2;
					pd.y += t;



					NodeGeo endNode = GetNodeGeo ((int)pd.y, pd.x, pd.z);


					if (!checkCollObs(p1.x, p1.z, p2.x, p2.z) && !checkCollEs(p1.x, p1.z, (int)p1.y, pd.x, pd.z, (int)pd.y, enemies, 1, depth, nodeVisiting.distractTimes, nodeVisiting.distractNums)) {
						//Debug.Log ("Done3");
						endNode.parent = nodeVisiting;
						endNode.distractTimes = nodeVisiting.distractTimes;
						endNode.distractNums = nodeVisiting.distractNums;
						if(debugging){
							DrawTree(start, minX, maxX, minY, maxY, maxT);
                            DrawSamples();
						}
                        toReturn.Nodes = ReturnPathGeo(endNode, smooth);
                        if(toReturn.Nodes != null) {
                            toReturn.success = true;
                        }
                        return toReturn;
					}
				}
				
				//Might be adding the neighboor as a the goal
				if (Mathf.Approximately(nodeVisiting.x, end.x) & Mathf.Approximately(nodeVisiting.y,end.y)) {
					//Debug.Log ("Done2");
					if(debugging){
						DrawTree(start, minX, maxX, minY, maxY, maxT);
                        DrawSamples();
					}
                    toReturn.Nodes = ReturnPathGeo(nodeVisiting, smooth);
                    if (toReturn.Nodes != null) {
                        toReturn.success = true;
                    }
                    return toReturn;
					
				}
			}

			//End RRT algo

			if(debugging){
				DrawTree(start, minX, maxX, minY, maxY, maxT);
                DrawSamples();
			}

            return toReturn;

        }

        public bool oldCheckInObs(float x, float y, List<Geometry> obs) {
            foreach(Geometry g in obs) {
                if (g.PointInside(new Vector3(x, 0, y))) {
                    return true;
                }
            }
            return false;
        }


		//Check for collision of a path with the obstacles, x, t, y
		public bool checkCollObs(float startX, float startY, float endX, float endY){
			//Debug.Log ("checkCollObs");

			if(casts != null){
				return casts.getCast(startX, startY, endX, endY);
			}
			else{
				Vector3 start = new Vector3(startX, 0, startY);
				Vector3 end = new Vector3(endX, 0, endY);
				int layerMask = 1 << 8;
				return Physics.Linecast (start, end, layerMask);
			}

			/* OLD WAY
			Line path = new Line(start, end);
			foreach(Geometry g in obs){
				foreach(Line l in g.edges){
					if(l.LineIntersection(path)){
						return true;
					}
				}
			}

			return false;
			*/
		}

		//Check for collision of a path with the enemies
		public bool checkCollEs(float startX, float startY,int startT, float endX, float endY,  int endT, List<EnemyGeo> enems, int d, int depth, List<int> distractTimes, List<int> distractNums){
			//Debug.Log ("CheckCollEs");
			if(enems == null){
				//Debug.Log ("no enems");
				return false;
			}




			if(d == 1){
				foreach(EnemyGeo e in enems){
					if(checkCollE(e, startX, startT, startY, distractTimes, distractNums)){
							return true;
					}
					if(checkCollE(e, endX, endT, endY, distractTimes, distractNums)){
							return true;
					}
				}
			}
			//if(d < depth){
				float newX = (startX + endX)/2.0f;
				float newY = (startY + endY) / 2.0f;
				int newT = (startT + endT) / 2;
				if(newT - startT <=interval){
					if( endT - newT <=interval){
						return false;
					}
					else{
						if(checkCollEs(newX, newY, newT, endX, endY, endT, enems, d+1, depth, distractTimes, distractNums)){
							return true;
						}
					}
				}
				else if( endT - newT <=interval){
					if(checkCollEs(startX, startY, startT, newX, newY, newT, enems, d+1, depth, distractTimes, distractNums)){
						return true;
					}
				}
				else{
					foreach(EnemyGeo e in enems){
						if(checkCollE(e, newX, newT, newY, distractTimes, distractNums)){
							return true;
						}
						if(checkCollEs(startX, startY, startT, newX, newY, newT, enems, d+1, depth, distractTimes, distractNums)){
							return true;
						}
						if(checkCollEs(newX, newY, newT, endX, endY, endT, enems, d+1, depth, distractTimes, distractNums)){
							return true;
						}
					}
				}
					                                      
			//}

			/* OLD WAY CHECK EVERY FRAME
			int numSteps = endT - startT;
			float stepX = (endX - startX) / ((float) numSteps);
			float stepY = (endY - startY) / ((float) numSteps);

			float checkX = startX;
			float checkY = startY;

			for(int t = startT; t <= endT; t++){
				foreach(EnemyGeo e in enems){
					if(checkCollE(e, checkX, t, checkY, obs)){
						return true;
					}
				}

				checkX += stepX;
				checkY += stepY;
			}
			*/

			//Debug.Log ("All enmes checked");

			return false;
		}
		

		public bool checkCollE(EnemyGeo e, float x, int t, float y, List<int> distractTimes, List<int> distractNums){
			//Debug.Log ("CheckCollE");
			Vector3 posE3;
			Vector3 forw;
			if(distractTimes.Count > 0){
				//Debug.Log ("distracted movement");
				posE3 = e.getPositionDistsN(t, distractTimes, distractNums);
				forw = e.getForwardDistsN(t, distractTimes, distractNums);
			}
			else{
				posE3 = e.getPosition (t);
				forw = e.getForward(t);
			}
			Vector2 posE = new Vector2(posE3.x, posE3.z);
			Vector2 posP = new Vector2(x,y);
			if(Vector2.Distance(posE, posP) > e.fovDistance){
				//Debug.Log ("Too Far Away: " + Vector2.Distance(posE, posP) + " t: " + t );
				return false;
			}
			Vector2 toPlay = posP-posE;
			Vector2 look = new Vector2(forw.x, forw.z);

			if(Vector2.Angle(toPlay, look) > e.fovAngle*0.5){
				//Debug.Log ("Angle Too Big: " + Vector2.Angle((posP-posE), e.getForward(t))+ " t: " + t);
				//Debug.Log ("poP " + posP + " posE " + posE + " vec " + (posP-posE) + " forw " + e.getForward (t) + " angle "  + Vector2.Angle ((posP-posE), e.getForward (t)));
				return false;
			}
			bool toReturn = checkCollObs(x, y, posE.x, posE.y);
			if(toReturn){
				//Debug.Log ("Obstacle to enemy detected");
			}
			else{
				//Debug.Log ("Collision with enemy");
			}
			return !toReturn;
		}


		//Returns the computed geo path by the RRT
		private List<NodeGeo> ReturnPathGeo(NodeGeo endNode, bool smooth) {
            if(endNode == null) {
                return new List<NodeGeo>();
            }
			NodeGeo n = endNode;
			List<NodeGeo> points = new List<NodeGeo> ();
			
			while (n != null) {
				points.Add (n);
				n = n.parent;
			}
			points.Reverse ();
			
			// If we didn't find a path
			if (points.Count == 1){
				points.Clear ();
			}
			else if(smooth){
				Debug.Log ("NO SMOOTHING IMPLEMENTED CURRENTLY");
			}

			//TODO CHANGE WHEN VECTROSITY WORKS AGAIN!
			/*GameObject treePic = new GameObject("pathDrawing");
			foreach(NodeGeo node in points){
				DrawNode(node, treePic, Color.red);
			}*/

			return points;
		}

		private void DrawTree(NodeGeo start, float minX, float maxX, float minY, float maxY, int maxT){
			//Debug.Log ("DRAWTREE CALLED");
			double[] min = new double[3];
			double[] max = new double[3];
			min[0] = minX;
			min[1] = 0;
			min[2] = minY;
			max[0] = maxX;
			max[1] = maxT;
			max[2] = maxY;
			NodeGeo node;
			GameObject treePic = new GameObject("treePic");


			foreach(object obj in tree.range (min, max)){
				node = (NodeGeo)obj;
				DrawNode(node, treePic, Color.gray);
			}
			

		}

        private void DrawSamples()
        {

            GameObject sampsPic = new GameObject("treePic");

            foreach(NodeGeo nd in explored)
            {
                DrawNode(nd, sampsPic, Color.gray, false);
            }

        }

        private void DrawNode(NodeGeo node, GameObject parent, Color c, bool lines = true){
			GameObject nod = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			nod.GetComponent<Renderer>().sharedMaterial.color = c;
			nod.transform.parent = parent.transform;
			nod.transform.position = node.GetVector3();
			nod.transform.position = new Vector3(nod.transform.position.x, 0.05f * nod.transform.position.y, nod.transform.position.z);
			nod.transform.localScale = new Vector3(0.3f, 0.3f,0.3f);
			/*nod.transform.position.x = node.x;
			nod.transform.position.z = node.y;
			nod.transform.position.y = node.t;*/
			if(node.parent != null && lines){
				DrawLine(node.parent, node, parent, c);
			}
		}

		private void DrawLine(NodeGeo node1, NodeGeo node2, GameObject parent, Color c){
			//Debug.Log ("node1 : " + node1.x + " , " + node1.y + " , " + (0.05f * node1.t));
			//Debug.Log ("node2 : " + node2.x + " , " + node2.y + " , " + (0.05f * node2.t));


			//Have vector going towards (1, 0, 0)
			//Want vector going towards node2 from position.



			GameObject lin = GameObject.CreatePrimitive(PrimitiveType.Cube);
			lin.GetComponent<Renderer>().sharedMaterial.color = c;
			lin.transform.parent = parent.transform;
			lin.transform.position = (node1.GetVector3() + node2.GetVector3()) / 2.0f;
			lin.transform.position = new Vector3(lin.transform.position.x, 0.05f * lin.transform.position.y, lin.transform.position.z);
			Vector3 dists = (node2.GetVector3() - node1.GetVector3());
			dists.y = 0.05f * dists.y;

			/*
			float rotationY = -1.0f * Mathf.Rad2Deg * Mathf.Atan(dists.z / dists.x);          
			float rotationZ = Mathf.Rad2Deg * Mathf.Atan (dists.y / dists.x);
			Vector3 rot = Vector3.zero;
			if(!float.IsNaN (rotationY)){
				rot.y = rotationY;
			}*/
			Vector3 from = Vector3.right;
			Vector3 to = dists / dists.magnitude;

			Vector3 axis = Vector3.Cross(from, to);
			float angle = Mathf.Rad2Deg * Mathf.Acos(Vector3.Dot (from, to));
			lin.transform.RotateAround (lin.transform.position, axis, angle);


			Vector3 scale = Vector3.one;
			//scale.x = Mathf.Sqrt (Mathf.Pow((node2.x - node1.x),2) + Mathf.Pow ((node2.y - node1.y), 2));
			scale.x = Vector3.Magnitude(dists);
			scale.z = 0.2f;
			scale.y = 0.2f;

			lin.transform.localScale = scale;
			//Debug.Log (rot);
			/*lin.transform.eulerAngles = rot;
			if(!float.IsNaN (rotationZ)){
				lin.transform.RotateAround(lin.transform.position, new Vector3(0, 0, 1), rotationZ);
			}*/


			//Debug.Log ("Line");
			//Debug.Log (lin.transform.position);
			//Debug.Log (lin.transform.eulerAngles);
			//Debug.Log (lin.transform.localScale);



			/*lin.transform.position.x = (node1.x + node2.x) / 2.0f;
			lin.transform.position.z = (node1.y + node2.y) / 2.0f;
			lin.transform.position.y = (node1.t + node2.t) / 2.0f;
			lin.transform.localScale.x = Mathf.Abs (node2.x - node1.x);
			lin.transform.localScale.z = Mathf.Abs (node2.y - node1.y);*/

		}

		#region oldcode
		//To prevent errors temporarily

		public Node GetNode (int t, int x, int y) {
			return null;
		}


		//OLD CODE FOLLOWS


		/*



		// Returns the computed path by the RRT, and smooth it if that's the case
		private List<Node> ReturnPath (Node endNode, bool smooth) {
			Node n = endNode;
			List<Node> points = new List<Node> ();
			
			while (n != null) {
				points.Add (n);
				n = n.parent;
			}
			points.Reverse ();
			
			// If we didn't find a path
			if (points.Count == 1)
				points.Clear ();
			else if (smooth) {
				// Smooth out the path
				Node final = null;
				foreach (Node each in points) {
					final = each;
					while (Extra.Collision.SmoothNode(final, this, SpaceState.Editor, true)) {
					}
				}
				
				points.Clear ();
				
				while (final != null) {
					points.Add (final);
					final = final.parent;
				}
				points.Reverse ();
			}
			
			return points;
		}


		// Gets the node at specified position from the NodeMap, or create the Node based on the Cell position for that Node

		
		public List<Node> Compute (int startX, int startY, int endX, int endY, int attemps, float speed, Cell[][][] matrix, bool smooth = false) {
			// Initialization
			tree = new KDTree (3);
			explored = new List<Node> ();
			nodeMatrix = matrix;
			
			//Start and ending node
			Node start = GetNode (0, startX, startY);
			start.visited = true; 
			start.parent = null;
			
			// Prepare start and end node
			Node end = GetNode (0, endX, endY);
			tree.insert (start.GetArray (), start);
			explored.Add (start);
			
			// Prepare the variables		
			Node nodeVisiting = null;
			Node nodeTheClosestTo = null;
			
			float tan = speed / 1;
			angle = 90f - Mathf.Atan (tan) * Mathf.Rad2Deg;
			
			List<Distribution.Pair> pairs = new List<Distribution.Pair> ();
			
			for (int x = 0; x < matrix[0].Length; x++) 
				for (int y = 0; y < matrix[0].Length; y++) 
					if (((Cell)matrix [0] [x] [y]).waypoint)
						pairs.Add (new Distribution.Pair (x, y));
			
			pairs.Add (new Distribution.Pair (end.x, end.y));
			
			//Distribution rd = new Distribution(matrix[0].Length, pairs.ToArray());
			
			//RRT algo
			for (int i = 0; i <= attemps; i++) {
				
				//Get random point
				int rt = Random.Range (1, nodeMatrix.Length);
				//Distribution.Pair p = rd.NextRandom();
				int rx = Random.Range (0, nodeMatrix [rt].Length);
				int ry = Random.Range (0, nodeMatrix [rt] [rx].Length);
				//int rx = p.x, ry = p.y;
				nodeVisiting = GetNode (rt, rx, ry);
				if (nodeVisiting.visited || nodeVisiting.cell.blocked) {
					i--;
					continue;
				}
				
				explored.Add (nodeVisiting);
				
				nodeTheClosestTo = (Node)tree.nearest (new double[] {rx, rt, ry});
				
				// Skip downwards movement
				if (nodeTheClosestTo.t > nodeVisiting.t)
					continue;
				
				// Only add if we are going in ANGLE degrees or higher
				Vector3 p1 = nodeVisiting.GetVector3 ();
				Vector3 p2 = nodeTheClosestTo.GetVector3 ();
				Vector3 pd = p1 - p2;
				if (Vector3.Angle (pd, new Vector3 (pd.x, 0f, pd.z)) < angle) {
					continue;
				}
				
				// And we have line of sight
				if ((nodeVisiting.cell.seen && !nodeVisiting.cell.safe) || Extra.Collision.CheckCollision (nodeVisiting, nodeTheClosestTo, this, SpaceState.Editor, true))
					continue;
				
				try {
					tree.insert (nodeVisiting.GetArray (), nodeVisiting);
				} catch (KeyDuplicateException) {
				}
				
				nodeVisiting.parent = nodeTheClosestTo;
				nodeVisiting.visited = true;
				
				// Attemp to connect to the end node
				if (Random.Range (0, 1000) > 0) {
					p1 = nodeVisiting.GetVector3 ();
					p2 = end.GetVector3 ();
					p2.y = p1.y;
					float dist = Vector3.Distance (p1, p2);
					
					float t = dist * Mathf.Tan (angle);
					pd = p2;
					pd.y += t;
					
					if (pd.y <= nodeMatrix.GetLength (0)) {
						Node endNode = GetNode ((int)pd.y, (int)pd.x, (int)pd.z);
						if (!Extra.Collision.CheckCollision (nodeVisiting, endNode, this, SpaceState.Editor, true)) {
							//Debug.Log ("Done3");
							endNode.parent = nodeVisiting;
							return ReturnPath (endNode, smooth);
						}
					}
				}
				
				//Might be adding the neighboor as a the goal
				if (nodeVisiting.x == end.x & nodeVisiting.y == end.y) {
					//Debug.Log ("Done2");
					return ReturnPath (nodeVisiting, smooth);
					
				}
			}
			
			return new List<Node> ();
		}

		*/

		#endregion oldcode
	}
}
