using System;
using System.Collections.Generic;
using UnityEngine;

namespace Common {
	[Serializable]
	public class Cell {
		public bool blocked = false;
		public bool seen = false;
		public bool sight = false;
		public bool safe = false;
		public bool noisy = false;
		public bool waypoint = false;
		public bool goal = false;
		public short cluster = 0;

		public  int FORWARD = 0;
		public  int BACKWARD = 3;
		public  int STOP = 4;
		public  int RIGHT_TURN = 1;
		public  int LEFT_TURN = 2;
		

		public double []qValues = new double[3];
		
		//public bool visit = false;
				
		public Cell Copy () {
			Cell copy = new Cell ();
			copy.blocked = this.blocked;
			copy.seen = this.seen;
			copy.safe = this.safe;
			copy.sight = this.sight;
			copy.noisy = this.noisy;
			copy.waypoint = this.waypoint;
			copy.goal = this.goal;
			copy.cluster = this.cluster;
			return copy;
		}

		public double getMaxQ(){

			double max = double.MinValue;

			for (int i = 0; i< qValues.Length; i++) {
				if( qValues[i] > max )
					max = qValues[i];
			}
			//Debug.Log ("max : " + max);
			return max;

		}

		public List<int> allMove( int x, int y){

			List <int> result = new List<int> ();
			double max = getMaxQ ();

			if (max == 0)
								return null;

			if (max == this.qValues[FORWARD]) {
				result.Add( x-1 );
				result.Add ( y );
			}
			if (max == this.qValues[BACKWARD]) {
				result.Add ( x+1 );
				result.Add ( y );
			}
			if (max == this.qValues[LEFT_TURN]) {
				result.Add (x);
				result.Add (y+1);
			}
			if (max == this.qValues[RIGHT_TURN]) {
				result.Add( x );
				result.Add( y-1 );
			}
			if (max == this.qValues[STOP]) {
				result.Add( x );
				result.Add( y );
			}
			
			return result;
			
		}
		
		public List<int> Select1ActionOnEGreedy( int x, int y, float epsilon ){
		
			double max = getMaxQ ();
			int maxAction = 0;
			
			while( !(this.qValues[maxAction] == max) )
				maxAction = UnityEngine.Random.Range( 0, qValues.Length );	
				
			int numMaxAction = (int)Mathf.Round( epsilon * 100 );
			int numOthers = 100 - numMaxAction;
			int numEach = numOthers / this.qValues.Length - 1;
			
			List<int> actions = new List<int>();
			for( int i = 0; i < numMaxAction; i++ )
				actions.Add( maxAction );

			for( int j = 0; j < this.qValues.Length ; j++ ){
				if( j == maxAction )
					continue;
				for( int k = 0; k < numEach; k++ )
					actions.Add( j );
			}//j
			
			int randomAction = UnityEngine.Random.Range( 0, actions.Count );
			randomAction = actions[randomAction];
			
			List<int> result = new List<int>();
			if (randomAction == this.STOP ){
				result.Add (x);
				result.Add (y);
			}
			if (randomAction == this.FORWARD) {
				result.Add (x - 1);
				result.Add (y);
			} 
			else if (randomAction == this.BACKWARD) {
				result.Add (x + 1);
				result.Add (y);
			} 
			else if (randomAction == this.LEFT_TURN) {
				result.Add (x);
				result.Add (y + 1);
			} 
			else if (randomAction == this.RIGHT_TURN) {
				result.Add (x);
				result.Add (y - 1);
			}
			
			result.Add (randomAction);
			return result;
								
		}
		
		
		public List<int> Select1ActionOnGreedy( int x, int y ){ //get random x, y among the maximum q values, {x, y, action}

			double max = getMaxQ ();
			int randomAction = 0;

			while( !(qValues[randomAction] == max) )
				randomAction = UnityEngine.Random.Range( 0, qValues.Length );			

			List<int> result = new List<int>();
//			if (randomAction == this.STOP ){
//				result.Add (x);
//				result.Add (y);
//			}
			if (randomAction == this.FORWARD) {
				result.Add (x - 1);
				result.Add (y);
			} 
			else if (randomAction == this.BACKWARD) {
				result.Add (x + 1);
				result.Add (y);
			} 
			else if (randomAction == this.LEFT_TURN) {
				result.Add (x);
				result.Add (y + 1);
			} 
			else if (randomAction == this.RIGHT_TURN) {
				result.Add (x);
				result.Add (y - 1);
			}

			result.Add (randomAction);
			return result;
		}
		
		public bool isFirstVisit(){
			
			for( int i = 0; i < this.qValues.Length; i++ ){
				if( qValues[i] != 0 )
					return false;
			}
			return true;
		}
	}
}