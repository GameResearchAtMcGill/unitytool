﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System; 
using Vectrosity; 


//Changes Made:
//Used my implementation of line intersection function called: LineIntersectionMuntac (will change name later)
//Another function called GetIntersectionPoint
[Serializable]
public class Line 
{

	public Vector3[] vertex = new Vector3[2];
	public Color[] colours = new Color[2]; 


	public Line(Vector3 v1, Vector3 v2)
	{
		vertex[0] = v1; 
		vertex[1] = v2; 
		colours[0] = Color.cyan;
		colours[1] = Color.cyan; 
	}

	public  bool Equals(Line l)
	{
		return this.MidPoint().Equals(l.MidPoint());
		//return vertex[0].Equals(l.vertex[0]) && vertex[1].Equals(l.vertex[1]); 
		
	}
	public static Line Zero
	{
		//get the person name 
		get { return new Line(Vector3.zero,Vector3.zero); }
	}
	public Vector3 MidPoint()
	{
		return new Vector3( (vertex[0].x + vertex[1].x)/2,
		                   (vertex[0].y + vertex[1].y)/2,
		                   (vertex[0].z + vertex[1].z)/2);
	}

	public Vector3 GetOther(Vector3 v)
	{
		if(vertex[0]==v)
			return vertex[1];
		return vertex[0];
	}

	public void DrawLine(Color c)
	{
		Debug.DrawLine(this.vertex[0],this.vertex[1],c); 
	}

	public void DrawLine()
	{
		Color c = new Color(UnityEngine.Random.Range(0.0f,1.0f),
		                           UnityEngine.Random.Range(0.0f,1.0f),
		                           UnityEngine.Random.Range(0.0f,1.0f)) ;

		Debug.DrawLine(this.vertex[0],this.vertex[1],c); 
	}
	public void DrawVector(GameObject parent)
	{
		Color c = new Color(UnityEngine.Random.Range(0.0f,1.0f),
		                           UnityEngine.Random.Range(0.0f,1.0f),
		                           UnityEngine.Random.Range(0.0f,1.0f)) ;

		VectorLine line = new VectorLine("Line",vertex,c,null,2.0f);
		line.vectorObject.transform.parent = parent.transform;
		line.Draw3D();
	}
	public void DrawVector(GameObject parent,Color c)
	{
	
		VectorLine line = new VectorLine("Line",vertex,c,null,2.0f);
		line.vectorObject.transform.parent = parent.transform;
		line.Draw3D();
	}
	public bool ShareVertex(Line l)
	{
		foreach(Vector3 v in vertex)
		{
			foreach(Vector3 w in l.vertex)
			{
				if(v.Equals(w))
					return true; 
			}
		}
		return false; 
	}


	public bool LineIntersection(Line l)
	{
		Vector3 a = l.vertex[0]; 
		Vector3 b = l.vertex[1];
		Vector3 c = vertex[0];
		Vector3 d = vertex[1];
		
		
		// a-b
		// c-d
		//if the same lines
		
		//When share a point use the other algo
		if(a.Equals(c) || a.Equals(d) || b.Equals(c) || b.Equals(d))
			return LineIntersect(a,b,c,d); 
		
		
		
		
		return CounterClockWise(a,c,d) != CounterClockWise(b,c,d) && 
			CounterClockWise(a,b,c) != CounterClockWise(a,b,d);
		
		//if( CounterClockWise(a,c,d) == CounterClockWise(b,c,d))
		//	return false;
		//else if (CounterClockWise(a,b,c) == CounterClockWise(a,b,d))
		//	return false; 
		//else 
		//	return true; 
		
		
	}
	public float Magnitude()
	{
		return (vertex[0]-vertex[1]).magnitude; 
	}
	private bool LineIntersect(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
	{
		//Debug.Log(a); 
		//Debug.Log(b); 
		//Debug.Log(c); 
		//Debug.Log(d); 
		
		Vector2 u = new Vector2(b.x,b.z) - new Vector2(a.x,a.z);
		Vector2 p0 = new Vector2(a.x,a.z); Vector2 p1 = new Vector2(b.x,b.z); 
		
		Vector2 v = new Vector2(d.x,d.z) - new Vector2(c.x,c.z);
		Vector2 q0 = new Vector2(c.x,c.z); Vector2 q1 = new Vector2(d.x,d.z);
		
		Vector2 w = new Vector2(a.x,a.z) - new Vector2(d.x,d.z);
		
		
		//if (u.x * v.y - u.y*v.y == 0)
		//	return true;
		
		double s = (v.y* w.x - v.x*w.y) / (v.x*u.y - v.y*u.x);
		double t = (u.x*w.y-u.y*w.x) / (u.x*v.y- u.y*v.x); 
		//Debug.Log(s); 
		//Debug.Log(t); 
		
		if ( (s>0 && s< 1) || (t>0 && t< 1) )
			return true;
		
		return false; 
	}
	public Vector3 LineIntersectionVect(Line l)
	{
		Vector3 a = l.vertex[0]; 
		Vector3 b = l.vertex[1];
		Vector3 c = vertex[0];
		Vector3 d = vertex[1];

		return LineIntersectVect(a,b,c,d);
	}
	private Vector3 LineIntersectVect (Vector3 a, Vector3 b, Vector3 c, Vector3 d)
	{
		//Debug.Log(a); 
		//Debug.Log(b); 
		//Debug.Log(c); 
		//Debug.Log(d); 
		
		Vector2 u = new Vector2 (b.x, b.z) - new Vector2 (a.x, a.z);
		Vector2 p0 = new Vector2 (a.x, a.z);
		Vector2 p1 = new Vector2 (b.x, b.z); 
		
		Vector2 v = new Vector2 (d.x, d.z) - new Vector2 (c.x, c.z);
		Vector2 q0 = new Vector2 (c.x, c.z);
		Vector2 q1 = new Vector2 (d.x, d.z);
		
		Vector2 w = new Vector2 (a.x, a.z) - new Vector2 (d.x, d.z);
		
		
		//if (u.x * v.y - u.y*v.y == 0)
		//	return true;
		
		double s = (v.y * w.x - v.x * w.y) / (v.x * u.y - v.y * u.x);
		double t = (u.x * w.y - u.y * w.x) / (u.x * v.y - u.y * v.x); 
		//Debug.Log(s); 
		//Debug.Log(t); 
		

			//Interpolation
		Vector3 r = a + (b-a)*(float)s; 
		return r; 
		//}
		


		//return Vector3.zero; 
	}
	private bool CounterClockWise(Vector3 v1,Vector3 v2,Vector3 v3)
	{
		//v1 = a,b
		//v2 = c,d
		//v3 = e,f
		
		float a = v1.x, b = v1.z;  
		float c = v2.x, d = v2.z;  
		float e = v3.x, f = v3.z;  
		
		if((f-b)*(c-a)> (d-b)*(e-a))
			return true;
		else
			return false; 
	}

	public int LineIntersectMuntac (Line param){
		Vector3 a = this.vertex [0];
		Vector3 b = this.vertex[1];
		Vector3 c = param.vertex [0];
		Vector3 d = param.vertex [1];

		Vector2 u = new Vector2 (b.x, b.z) - new Vector2 (a.x, a.z);
		Vector2 p0 = new Vector2 (a.x, a.z);
		
		Vector2 v = new Vector2 (d.x, d.z) - new Vector2 (c.x, c.z);
		Vector2 q0 = new Vector2 (c.x, c.z);
		
		double numerator1 = CrossProduct ((q0 - p0), v);
		double numerator2 = CrossProduct ((q0 - p0), u);
		double denom = CrossProduct (u, v);
		
		//Case 1 - Colinear
		if ( denom == 0 && numerator2 == 0 ) {
			//Case 2 - Colinear and Overlapping
			if( Vector2.Dot( (q0 - p0), u ) >= 0 && Vector2.Dot( (q0 - p0), u ) <= Vector2.Dot( u, u ) )
				return 2;
			if( Vector2.Dot( (p0 - q0), v ) >= 0 && Vector2.Dot( (p0 - q0), v ) <= Vector2.Dot( v, v ) )
				return 2;
			return 0;
		}
		//Case 3 - Parallel
		if (denom == 0 && numerator2 != 0)
			return 0;
		
		//Case 4 - Intersects
		double s = numerator1 / denom;
		double t = numerator2 / denom;
		
		if ((s > 0 && s < 1) && (t > 0 && t < 1))
			return 1;
		
		return 0; 
	}

	public Vector3 GetIntersectionPoint (Line param){
		Vector3 a = this.vertex [0];
		Vector3 b = this.vertex [1];
		Vector3 c = param.vertex [0];
		Vector3 d = param.vertex [1];

		Vector2 u = new Vector2 (b.x, b.z) - new Vector2 (a.x, a.z);
		Vector2 p0 = new Vector2 (a.x, a.z);
		
		Vector2 v = new Vector2 (d.x, d.z) - new Vector2 (c.x, c.z);
		Vector2 q0 = new Vector2 (c.x, c.z);
		
		double numerator1 = CrossProduct ((q0 - p0), v);
		double numerator2 = CrossProduct ((q0 - p0), u);
		double denom = CrossProduct (u, v);
		
		double s = numerator1 / denom;
		double t = numerator2 / denom;
		
		Vector3 r = a + (b-a)*(float)s; 
		return r;
	}

	private double CrossProduct( Vector2 a, Vector2 b ){
		return (a.x * b.y) - (a.y * b.x);
	}
}
class LineEqualityComparer : IEqualityComparer<Line>
{
	
	public bool Equals(Line b1, Line b2)
	{
		return b1.Equals(b2);
	}
	
	
	public int GetHashCode(Line bx)
	{
		int hCode = (int)(bx.MidPoint().sqrMagnitude);
		return hCode.GetHashCode();
	}
	
}
