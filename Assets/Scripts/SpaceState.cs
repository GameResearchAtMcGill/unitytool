using System;
using UnityEngine;

public class SpaceState
{
	// Instance related
	private static SpaceState instance;
	
	private SpaceState ()
	{
	}
	
	static SpaceState ()
	{
		instance = new SpaceState ();
	}
	
	public static SpaceState Instance { 
		get { return instance; }
	}
	// Fields
	
	#region Mapper initialization
	// PrecomputeMaps
	public Enemy[] enemies;
	public Enemy2[] enemies2;
	
	// PrecomputeMaps
	public Cell[][][] fullMap;
	
	// ComputeTilesize
	public Vector3 floorMin;
	
	// ComputeTilesize
	public Vector2 tileSize;
	
	// Getters
	
	public static Enemy[] Enemies {
		get { return Instance.enemies; }
	}

	public static Enemy2[] Enemies2 {
		get { return Instance.enemies2; }
	}
	
	public static Cell[][][] FullMap {
		get { return Instance.fullMap; }
	}
	
	public static Vector2 TileSize {
		get { return Instance.tileSize; }
	}
	
	public static Vector3 FloorMin {
		get { return Instance.floorMin; }
	}
	#endregion
}