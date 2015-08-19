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
	public class RRTKDTreeConnect : NodeProvider {
		private Cell[][][] nodeMatrix;
		private float angle;
		public KDTree tree;
		public List<Node> explored;
		// Only do noisy calculations if enemies is different from null
		public Enemy[] enemies;
		public Vector3 min;
		public float tileSizeX, tileSizeZ;

		public enum Status { REACHED, ADVANCED, TRAPPED };

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

		public Node GetNewConfig( Node destNode, Node nearNode, Node nearNode2D, float stepSize ){

			//get newNode
			Vector2 destVector = destNode.GetVector2();
			Vector2 nearVector = nearNode2D.GetVector2();
			Vector2 newVector = destVector - nearVector;

			newVector = newVector*(stepSize/newVector.magnitude);
			newVector = nearVector + newVector;

			Debug.Log ( " dest is visited? : " + destNode.visited 
						+" dest x : " + destNode.x + " dest y : " + destNode.y 
			           + " near x : " + nearNode.x + " near y : " + nearNode.y
			           + " near is visited? " + nearNode.visited
			           + " new x : " + newVector.x + " new y : " + newVector.y 
			           + " round x : " + System.Math.Round( newVector.x ) 
			           + " round y : " + System.Math.Round( newVector.y ));

			int newX = (int)System.Math.Round (newVector.x);
			int newY = (int)System.Math.Round (newVector.y);
			if (newX >= 60 || newY >= 60)
				return null;

			Node newNode = GetNode ( nearNode.t+1, newX, newY );
		
			//check collision 
			if ((newNode.cell.seen && !newNode.cell.safe) || Extra.Collision.CheckCollision (newNode, nearNode, this, SpaceState.Editor, true))
				return null;
			else 
				return newNode;

		}

		public Node Extend (ref KDTree tree, ref KDTree tree2D, ref Dictionary< string, double> xytTable, Node destNode, float stepSize){
			
			double[] dest2DKey = new double[]{ destNode.x, destNode.y };
			Node nearNode2D = (Node)tree2D.nearest ( dest2DKey );
			double[] nearNode2DKey = new double[]{ nearNode2D.x, nearNode2D.y };
			double nearNodeTime = (double)xytTable [nearNode2DKey [0] + "#" + nearNode2DKey [1]]; 
			double[] nearNodekey = new double[]{ nearNode2D.x, nearNodeTime, nearNode2D.y };
			Node nearNode = (Node)tree.search ( nearNodekey );
			Node newNode = null;
			double[] newNode2DKey = {0.0, 0.0};
			
			if ( ( newNode = GetNewConfig ( destNode, nearNode, nearNode2D, stepSize )) != null) {
				newNode2DKey = new double[] { newNode.x, newNode.y };
				
				if( xytTable.ContainsKey ( newNode2DKey[0] + "#" + newNode2DKey[1] ) )
					return null; // 이미 존재하는 지점 추가 .. 원래는 불가능 한 일이나 .. 정수로 반올림하면서 생기는 문제) 
				
				newNode.parent = nearNode;
				newNode.visited = true;
				
				try{
					tree.insert( newNode.GetArray(), newNode );
					tree2D.insert ( newNode2DKey, newNode );
				} catch (KeyDuplicateException) {
				}
				
				xytTable.Add ( newNode2DKey[0] + "#" + newNode2DKey[1], newNode.t );
				
				//if( newNode.equalTo( destNode ) )
				//	return newNode;
				
				return newNode;
				
			}
			return null; //trapped
		}

		public Node Connect (KDTree tree, KDTree tree2D, Dictionary< string, double> xytTable, Node destNode, float stepSize)
		{
			Node newNode = null;
			Node advancedNode = null;

				while (( newNode = Extend( ref tree, ref tree2D, ref xytTable, destNode, stepSize ) ) != null 
			       && !(newNode.x == destNode.x && newNode.y == destNode.y)) {
								advancedNode = newNode; // repeat extending to the dest node ( random node )
						}

				if ( advancedNode == null) // never made
					return null;
				else // if( newNode.x == destNode.x && newNode.y == destNode.y ) //reached
				      return advancedNode;
		}



		public List<List<Node>> Compute (int startX, int startY, int endX, int endY, int attemps, float speed, Cell[][][] matrix, bool smooth = false, bool allBranches = true ) 
			{
			// Initialization
			tree = new KDTree (3);
			KDTree tree2D = new KDTree (2);
			Dictionary< string, double > xytTable = new Dictionary< string, double >();


			explored = new List<Node> ();
			nodeMatrix = matrix;
			
			//Start and ending node
			Node start = GetNode (0, startX, startY);

			start.visited = true; 
			start.parent = null;
			
			// Prepare start and end node
			Node end = GetNode (0, endX, endY);
			Debug.Log (" start x : " + start.x + " start y : " + start.y 
								+ " end x : " + end.x + " end y : " + end.y);

			tree.insert (start.GetArray (), start);
			tree2D.insert (new double[] {start.x, start.y}, start);
			xytTable.Add ( start.x + "#" + start.y, start.t);

			explored.Add (start);
			
			// Prepare the variables		
			Node randomNode = null;
			//Node nearNode = null;
			
			//float tan = speed / 1;
			//angle = 90f - Mathf.Atan (tan) * Mathf.Rad2Deg;
			
			List<Distribution.Pair> pairs = new List<Distribution.Pair> ();
			
			for (int x = 0; x < matrix[0].Length; x++) 
				for (int y = 0; y < matrix[0].Length; y++) 
					if (((Cell)matrix [0] [x] [y]).waypoint)
						pairs.Add (new Distribution.Pair (x, y));
			
			pairs.Add (new Distribution.Pair (end.x, end.y));
			
			//Distribution rd = new Distribution(matrix[0].Length, pairs.ToArray());

			Status status;
			Node advancedNode = null;


			System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
			sw.Start();

			List<List<Node>> pathResults = new List<List<Node>> ();

			//RRT algo
			for (int i = 0; i <= attemps; i++) {
				//Debug.Log( "i : " + i );
				//Get random point
				//int rt = 1;
				int	rx = Random.Range (0, nodeMatrix [1].Length);
				int	ry = Random.Range (0, nodeMatrix [1] [1] .Length);

	//				Debug.Log (" before getting randomNode");
	//				randomNode = GetNode (rt, rx, ry);
				randomNode = new Node();
				randomNode.x = rx;
				randomNode.y = ry;

				double[] randomNode2DKey = new double[] { randomNode.x , randomNode.y  };
				if( ( xytTable.ContainsKey ( randomNode2DKey[0] + "#" + randomNode2DKey[1] ) == true ) 
					|| ( GetNode( 1, randomNode.x, randomNode.y ).cell.blocked )) { //already visited or block
					i--;
					continue;
				}

				explored.Add (randomNode);
				//Node newNode = Extend ( ref tree, ref tree2D, ref xytTable, randomNode, speed/1 );
				Node newNode = Connect ( tree, tree2D, xytTable, randomNode, speed/1 );

				if( newNode != null ){
					advancedNode = newNode;

					//add only the last node among newly found nodes
					double[] newNode2DKey = new double[] { newNode.x, newNode.y };
					if( xytTable.ContainsKey ( newNode2DKey[0] + "#" + newNode2DKey[1] ) )
						continue; // 이미 존재하는 지점 추가 .. 원래는 불가능 한 일이나 .. 정수로 반올림하면서 생기는 문제) 

					try{
						tree.insert( newNode.GetArray(), newNode );
						tree2D.insert ( newNode2DKey, newNode );
					} catch (KeyDuplicateException) {
					}
					
					xytTable.Add ( newNode2DKey[0] + "#" + newNode2DKey[1], newNode.t );

					if(endX == newNode.x && endY == newNode.y  )	{
					//Might be adding the neighboor as a the goal
					//if (randomNode.x == end.x & randomNode.y == end.y) {
						Debug.Log ( " finish!");

						sw.Stop();
						Debug.Log ( "Elapsed time : " + System.Math.Truncate( (double)sw.ElapsedMilliseconds/60000 ) + "min " 
						           + System.Math.Truncate( ( (double)sw.ElapsedMilliseconds % 60000 )/1000 ) + "sec" );

						//return ReturnPath (newNode, smooth);
						pathResults.Add( ReturnPath (newNode, smooth) );
						return pathResults;	
					}
				}
				else if ( i == attemps && advancedNode != null ){
					Debug.Log (" failed to find the solution " );

					sw.Stop();
					Debug.Log ( "Elapsed time : " + System.Math.Truncate( (double)sw.ElapsedMilliseconds/60000 ) + "min " 
					           + System.Math.Truncate( ( (double)sw.ElapsedMilliseconds % 60000 )/1000 ) + "sec" );

				//	return ReturnPath (advancedNode, smooth);
					pathResults.Add( ReturnPath (advancedNode, smooth) );
					return pathResults;
				}

				if( allBranches )
					pathResults.Add( ReturnPath (advancedNode, smooth) );

			}
				
			sw.Stop();
			Debug.Log ( "Elapsed time : " + System.Math.Truncate( (double)sw.ElapsedMilliseconds/60000 ) + "min " 
			           + System.Math.Truncate( ( (double)sw.ElapsedMilliseconds % 60000 )/1000 ) + "sec" );

			return new List<List<Node>> ();
		}
		
		// Returns the computed path by the RRT, and smooth it if that's the case
		public List<Node> ReturnPath (Node endNode, bool smooth) {
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