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
	public class RRTKDTree : NodeProvider {
		private Cell[][][] nodeMatrix;
		private float angle;
		public KDTree tree;
		public List<Node> explored;
		// Only do noisy calculations if enemies is different from null
		public Enemy[] enemies;
		public Vector3 min;
		public float tileSizeX, tileSizeZ;

		public HealthPack[] packs; //태영 
		
		// Gets the node at specified position from the NodeMap, or create the Node based on the Cell position for that Node
		public Node GetNode (int t, int x, int y) {
			object o = tree.search (new double[] {x, t, y});
			if (o == null) {
				Node n = new Node ();
				n.x = x;
				n.y = y;
				n.t = t;
				try {
					n.cell = nodeMatrix [t] [x] [y];
				} catch {
					Debug.Log (t + "-" + x + "-" + y);
					Debug.Log (n);
					Debug.Log (nodeMatrix [t]);
					Debug.Log (nodeMatrix.Length);
					Debug.Log (nodeMatrix [t].Length);
					Debug.Log (nodeMatrix [t] [x].Length);
					n.cell = nodeMatrix [t] [x] [y];
				}
				o = n;
			}
			return (Node)o;
		}
	
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
		
			int visit = 0; //태영 
			bool visitTurn = true;
			Debug.Log ("packs len" + packs.Length );

			//RRT algo
			for (int i = 0; i <= attemps; i++) {
	
				int rt = Random.Range (1, nodeMatrix.Length);
				int rx;
				int ry;

				if( visitTurn && visit < packs.Length ){
					rx = packs[ visit ].posX;
					ry = packs[ visit ].posZ;
					Debug.Log ("sampled a visit " + i);

				}
				//Get random point
	//			int rt = Random.Range (1, nodeMatrix.Length);
				//Distribution.Pair p = rd.NextRandom();
				else{
					rx = Random.Range (0, nodeMatrix [rt].Length);
					ry = Random.Range (0, nodeMatrix [rt] [rx].Length);
				}
					//int rx = p.x, ry = p.y;
				nodeVisiting = GetNode (rt, rx, ry);
				if (nodeVisiting.visited || nodeVisiting.cell.blocked) {
					i--;
					if( visitTurn )
					{
						Debug.Log( "visited" + i);
						visitTurn = !visitTurn;
					}
					continue;
				}

				explored.Add (nodeVisiting);
				
				nodeTheClosestTo = (Node)tree.nearest (new double[] {rx, rt, ry});
				
				// Skip downwards movement
				if (nodeTheClosestTo.t > nodeVisiting.t){
					if( visitTurn )// off visit turn
					{
						Debug.Log( "downwards" + i);
						visitTurn = !visitTurn;
					}
						continue;
				}
				
				// Only add if we are going in ANGLE degrees or higher
				Vector3 p1 = nodeVisiting.GetVector3 ();
				Vector3 p2 = nodeTheClosestTo.GetVector3 ();
				Vector3 pd = p1 - p2;
				if (Vector3.Angle (pd, new Vector3 (pd.x, 0f, pd.z)) < angle) {
					if( visitTurn ) // off visit turn
					{
						Debug.Log( "over speed" + i);
						visitTurn = !visitTurn;
					}

					continue;
				}
				
				// And we have line of sight
				if ((nodeVisiting.cell.seen && !nodeVisiting.cell.safe) || Extra.Collision.CheckCollision (nodeVisiting, nodeTheClosestTo, this, SpaceState.Editor, true)){
					if( visitTurn )// off visit turn
					{
						Debug.Log( "seen" + i);
						visitTurn = !visitTurn;
					}

					
					continue;
				}
				
				try {
					tree.insert (nodeVisiting.GetArray (), nodeVisiting);

				} catch (KeyDuplicateException) {
				}
				
				nodeVisiting.parent = nodeTheClosestTo;
				nodeVisiting.visited = true;

				Debug.Log( i+ " closest time : " + nodeTheClosestTo.t + " visiting time : " + nodeVisiting.t 
				+ "visit turn : " + visitTurn +" visit : " + visit );
				if( visitTurn ) // success to have a visit -> the next visit
					visit++;
				visitTurn = true;

				// Attemp to connect to the end node
				if (Random.Range (0, 1000) > 0) {
					p1 = nodeVisiting.GetVector3 ();
					p2 = end.GetVector3 ();
					p2.y = p1.y;
					float dist = Vector3.Distance (p1, p2);
					
					float t = dist * Mathf.Tan (angle*Mathf.Deg2Rad);
					pd = p2;
					pd.y += t;
					
					if (pd.y <= nodeMatrix.GetLength (0)) {
						//Debug.Log ("Done2");

						Node endNode = GetNode ((int)pd.y, (int)pd.x, (int)pd.z);
						if (!Extra.Collision.CheckCollision (nodeVisiting, endNode, this, SpaceState.Editor, true)) {
							//Debug.Log ("Done3");
							endNode.parent = nodeVisiting;
							return ReturnPath (endNode, smooth);
						}
					}
				}

				/*
				// Attemp to connect to the visit nodes
				if ( visit < packs.Length && Random.Range (0, 1000) > 0) {

					p1 = nodeVisiting.GetVector3 ();
					Node nNode = GetNode( 0, packs[ visit ].posX, packs[ visit ].posZ ); 
					p2 = nNode.GetVector3 ();
					p2.y = p1.y;
					float dist = Vector3.Distance (p1, p2);
					
					float t = dist * Mathf.Tan (angle*Mathf.Deg2Rad);
					pd = p2;
					pd.y += t;
					
					if (pd.y <= nodeMatrix.GetLength (0)) {
						//Debug.Log ("Done2");
						
						Node visitNode = GetNode ((int)pd.y, (int)pd.x, (int)pd.z);
						if (!Extra.Collision.CheckCollision (nodeVisiting, visitNode, this, SpaceState.Editor, true)) {
							//Debug.Log ("Done3");
							explored.Add( visitNode );
							visitNode.visited = true;
							visitNode.parent = nodeVisiting;

							try {
								tree.insert (visitNode.GetArray (), visitNode);
							} catch (KeyDuplicateException) {
							}

							Debug.Log( "visit : " + visit );
							Debug.Log( "visitX : " + packs[ visit ].posX );
							Debug.Log( "visitZ : " + packs[ visit ].posZ );

							//if( visit == 1 )
							//	return ReturnPath (visitNode, smooth);

								visit++;



						}
					}
				}
				*/
					
				//Might be adding the neighboor as a the goal
				if (nodeVisiting.x == end.x & nodeVisiting.y == end.y) {
					//Debug.Log ("Done2");
					return ReturnPath (nodeVisiting, smooth);
						
				}
			}
					
			return new List<Node> ();
		}
		
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
		

	}
}