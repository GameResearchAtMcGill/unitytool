//------------------------------------------------------------------------------
// <auto-generated>
//     이 코드는 도구를 사용하여 생성되었습니다.
//     런타임 버전:4.0.30319.34014
//
//     파일 내용을 변경하면 잘못된 동작이 발생할 수 있으며, 코드를 다시 생성하면
//     이러한 변경 내용이 손실됩니다.
// </auto-generated>
//------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Common;
	
namespace Learning
{
		public class StateManager
		{
			List<State> stList = null;
			Cell[][][] original;
			int endX, endY, gridSize, sight;
			public double[] sensor4Block;
			public double[] sensor4Goal;
			public double[] goalXDirectionSensor; // +, -, 0
			public double[] goalYDirectionSensor;
			public	double[] playerDirectionSensor; // 0, 1, 2, 3		
			public int allSensorInputSize;
			public int sensor4BlockSize;
			public int sensor4GoalSize;
			public int goalXDirectionSensorSize;
			public int goalYDirectionSensorSize;
			public int playerDirectionSensorSize;
			double[] allSensors;
					
		public StateManager ( Cell[][][] _original, int _endX, int _endY, int _gridSize, int _sight )
				{
					stList = new List<State>();
					original = _original;
					endX = _endX;
					endY = _endY;
					gridSize = _gridSize;
					sight = _sight;
					sensor4BlockSize = sight * sight + 1;
					sensor4GoalSize = sensor4BlockSize;
					goalXDirectionSensorSize = 3;
					goalYDirectionSensorSize = 3;
					playerDirectionSensorSize = 4;
										
					allSensorInputSize = sensor4BlockSize + //sensor4GoalSize +  //test
											goalXDirectionSensorSize + goalYDirectionSensorSize + playerDirectionSensorSize;
					sensor4Block = new double[ sensor4BlockSize ];
					sensor4Goal = new double[ sensor4GoalSize ];
					goalXDirectionSensor = new double[ goalXDirectionSensorSize ];
					goalYDirectionSensor = new double[ goalYDirectionSensorSize ];
					playerDirectionSensor = new double[ playerDirectionSensorSize ];
					allSensors = new double[ allSensorInputSize ];	
				}
				
				public State getState( double[] _inputs ){
					int i;
					foreach( State s in stList ){
						i = 0;
						for( ; i < s.sensors.Length; i ++ ){
							if( s.sensors[i] != _inputs[i] )
								break;
						}
						if( i == s.sensors.Length )
							return s;
					}
					return null;
				}
				
				public State getState( int id ){
					if( stList.Count > id )
						return stList[id];
					else
						return null; 
				}
				
				//add an State in the list, and return it, 
				//if it exists in the list, then just return it.
				public State addState(  int time, int currX, int currY, int direction  ){
					
					
					
					Array.Clear( sensor4Block, 0, sensor4Block.Length );
					Array.Clear( sensor4Goal, 0, sensor4Goal.Length );
					Array.Clear( goalXDirectionSensor, 0, goalXDirectionSensor.Length );
					Array.Clear( goalYDirectionSensor, 0, goalYDirectionSensor.Length );
					Array.Clear( playerDirectionSensor, 0, playerDirectionSensor.Length );
					Array.Clear( allSensors, 0, allSensors.Length );
			
					int focusX, focusY;
					int tempX, tempY;
					bool block = false, goal = false;
					
					
					for( int j = 0; j < sensor4Block.Length - 1; j++ ){
						focusX = -1 * (sight - 1) / 2 + (j % sight);
						focusY = -1 * (sight - 1) / 2 + (j / sight); // relative X and Y
						
						for( int i = 0; i < direction; i++ ){ //change focus X and focus Y according to player's direction
							tempX = focusX;
							tempY = focusY;
							focusX = tempY;
							focusY = -1 * tempX;
						}
						focusX += currX;
						focusY += currY; // real X and Y to be checked
						
						if( focusX >= 0 && focusX <= gridSize-1 && focusY >= 0 && focusY <= gridSize-1 ){
							if( original[time][focusX][focusY].blocked || original[time][focusX][focusY].seen ){
								sensor4Block[j] = 1; // an obstacle
								sensor4Goal[j] = 0;
								block = true;
							}
							else if ( focusX == endX && focusY == endY ){
								sensor4Goal[j] = 1; // the goal
								sensor4Block[j] = 0;
								goal = true;
							}
//							else if ( Math.Sqrt( Math.Pow( endX - focusX, 2 ) + Math.Pow( endY - focusY, 2 ) ) < 10  ){
//								sensor4Goal[j] = 1; // the goal area
//								sensor4Block[j] = 0;
//								goal = true;
//							}
							else{
								sensor4Block[j] = 0; // can go
								sensor4Goal[j] = 0;
							}
						}
						else{
							sensor4Block[j] = 1; // out of the map
							sensor4Goal[j] = 0;
							block = true;
						}
					} // j
						
					
					if( block == false ) // no obstacle
						sensor4Block[ sensor4Block.Length - 1 ] = 1;
//					else
//						sensor4Block[ sensor4Block.Length - 1 ] = 0;
					if( goal == false ) // no goal
						sensor4Goal[ sensor4Goal.Length - 1 ] = 1;
//					else
//						sensor4Goal[ sensor4Goal.Length - 1 ] = 0;
					
					int xSensor = 0, ySensor = 0;
					if( currX - endX > 0 )
						goalXDirectionSensor[0] = 1;
					else if( currX - endX < 0 )
						goalXDirectionSensor[1] = 1;
					else
						goalXDirectionSensor[2] = 1;
					
					if( currY - endY > 0 )
						goalYDirectionSensor[0] = 1;
					else if( currY - endY < 0 )
						goalYDirectionSensor[1] = 1;
					else
						goalYDirectionSensor[2] = 1;
						
					playerDirectionSensor[ direction ] = 1;
			
					
//					sensor4Block.CopyTo( allSensors, 0 );
//					sensor4Goal.CopyTo( allSensors, sensor4Block.Length );
//					goalXDirectionSensor.CopyTo( allSensors, sensor4Block.Length + sensor4Goal.Length );
//					goalYDirectionSensor.CopyTo( allSensors, sensor4Block.Length + sensor4Goal.Length + goalXDirectionSensor.Length );
//					playerDirectionSensor.CopyTo( allSensors, sensor4Block.Length + sensor4Goal.Length + goalXDirectionSensor.Length + goalYDirectionSensor.Length );
					
					//test
					sensor4Block.CopyTo( allSensors, 0 );
					goalXDirectionSensor.CopyTo( allSensors, sensor4BlockSize );
					goalYDirectionSensor.CopyTo( allSensors, sensor4BlockSize + goalXDirectionSensorSize );
					playerDirectionSensor.CopyTo( allSensors, sensor4BlockSize + goalXDirectionSensorSize + goalYDirectionSensorSize );
			
					
					State newState = getState( allSensors );
					if( newState == null )
						return addState( (double[])allSensors.Clone() );
					else
						return newState;
				}
				
				public State addState( double[] _inputs ){
					State newState = new State( stList.Count, _inputs );
					stList.Add( newState );
					return newState;
				}
				
				public void reset(){
					stList.Clear();
				}
				
//				public void makeADJMatrix( ){
//					adjMatrix = new double[ stList.Count, stList.Count ];
//					
//					for( int i = 0; i < stList.Count; i++ ){
//						List<int> near = getADJStates( i );
//						for( int j = 0; j < near.Count; j++ ){
//							adjMatrix[ i, near[j] ] = 1;
//							adjMatrix[ near[j], i ] = 1;
//						}
//					}
//				}
//				
//				private List<int> getADJStates( int stateNumber ){
//					List<int> near = new List<int>();
//					int x = stList[ stateNumber ].x;
//					int y = stList[ stateNumber ].y;
//					
//					int xm = x-1;
//					int xp = x+1;
//					int ym = y-1;
//					int yp = y+1;
//					
//										
//					int i = 0;
//					foreach( State s in stList ){
//						if( s.x == xm && s.y == y )
//							near.Add( s.id );
//						else if( s.x == xp && s.y == y )
//							near.Add( s.id );
//						else if( s.x == x && s.y == ym )
//							near.Add( s.id );
//						else if( s.x == x && s.y == yp )
//							near.Add( s.id );				
//					}
//					return near;
//				}
		}
}

