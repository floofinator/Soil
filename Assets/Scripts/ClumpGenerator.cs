using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClumpGenerator : MonoBehaviour
{
	public bool start = false;
	public static float MAXWEIGHT = 4f;
	public static float SURFACE = 0.5f;
	public int size = 64;
	public float noiseScale;
	public float noiseMagnitude;
	public float noiseFill = 4;
	public Vector2 noisePos = Vector2.zero;
	float[,] map;
	ClumpMeshGenerator meshGenerator;
	Rigidbody2D rb;
	List<List<Vector2Int>> regions;
	void Awake() {
		meshGenerator = GetComponent<ClumpMeshGenerator>();
		rb = GetComponent<Rigidbody2D>();
		if(start){
			GenerateMap();
		}
	}
	void OnDrawGizmos(){
		if(regions!=null){
			System.Random random = new System.Random(0);
			foreach(List<Vector2Int> region in regions){
				Color rColor = new Color((float)random.Next(0,20)/20,(float)random.Next(0,20)/20,(float)random.Next(0,20)/20);
				foreach(Vector2Int tile in region){
					float value = map[tile.x,tile.y]/ClumpGenerator.MAXWEIGHT;
					Gizmos.color = rColor*value;
					Gizmos.DrawCube(transform.TransformPoint(new Vector3(tile.x,tile.y)*ClumpMeshGenerator.SQUARESIZE),Vector3.one);
				}
			}

		}
		if(rb!=null){
			Gizmos.color = Color.blue;
			Gizmos.DrawCube(transform.TransformPoint(rb.centerOfMass),Vector3.one);
		}
		Gizmos.DrawWireCube(transform.TransformPoint(new Vector3(size/2,size/2)*ClumpMeshGenerator.SQUARESIZE),new Vector3(size,size,1));
	}

    public void Circle(Vector2 pos, float radius, float rate){
		//Call this function to add/subtract from the map tiles.
		pos = transform.InverseTransformPoint(pos);
		pos/=ClumpMeshGenerator.SQUARESIZE;
		bool cut = (radius<0);
		radius = Mathf.Abs(radius);
        int xStart = (int)pos.x - (int)radius;
        int yStart = (int)pos.y - (int)radius;
        for (int x = xStart; x < (xStart+radius*2)+1; x++)
        {
            for (int y = yStart; y < (yStart+radius*2)+1; y++)
            {
                if(IsInMapRange(x,y)){
                    map[x,y] = Falloff(x,y,pos,radius,rate,cut);
                }
            }
        }
        ProcessMap();
    }
	float Falloff(int x, int y, Vector2 pos, float radius, float rate, bool cut){
		//Calculates the falloff for adding/subtracting weight from the tiles.
		float value = map[x,y];
		float dis = Vector2.Distance(new Vector2(x,y), pos);
		float strength = Mathf.Lerp(MAXWEIGHT,0,dis/Mathf.Abs(radius));

		if(!cut){
			float min = Mathf.Lerp(MAXWEIGHT,0,dis/Mathf.Abs(radius));
			if (value<min) value += strength*rate;
		}else{
			Debug.Log("CUT!");
			float max = Mathf.Lerp(0,MAXWEIGHT,dis/Mathf.Abs(radius));
			max = Mathf.Clamp(max-2f,0,MAXWEIGHT);
			if (value>max) value -= strength*rate;
		}
		
		return Mathf.Clamp(value,0,MAXWEIGHT);
	}
	void GenerateMap() {
		//Creates a map from noise;
		map = new float[size,size];
		RandomFillMap();
		ProcessMap();
	}
	public void SetMap(float[,] _map) {
		//Sets the map to a prexisting one.
		map = _map;
		ProcessMap();
	}
	void ProcessMap() {
		//Updates map data and calls the MeshGenerator to make the mesh and collisions.
		regions = GetRegions();
		if(regions.Count == 0) Destroy(gameObject);
		if(regions.Count > 1) SeperateRegion(regions[regions.Count-1]);
		if(regions.Count > 0) Center();
		if(rb != null) CalculateMass();
		meshGenerator.GenerateMesh(map);
	}

	void Center(){
		//finds the center of the filled tiles and moves them so they are at the center of the map array.
		Vector2Int center = Vector2Int.zero;
		int tiles = 0;
		for (int x = 0; x < size; x++) {
			for (int y = 0; y < size; y++) {
				if (IsInMapRange(x,y) && map[x,y] > SURFACE) {
					tiles++;
					center += new Vector2Int(x,y);
				}
			}
		}
		if(tiles>0){
			center = center/tiles;
			Vector2Int dif = center-new Vector2Int(size/2,size/2);
			float[,] newMap = new float[size,size];
			for (int x = 0; x < size; x++) {
				for (int y = 0; y < size; y++) {
					if (IsInMapRange(x,y)&&IsInMapRange(x+dif.x,y+dif.y)) {
						newMap[x,y] = map[x+dif.x,y+dif.y];
					}
				}
			}
			map = newMap;
			transform.position = transform.TransformVector(new Vector3(dif.x,dif.y)*ClumpMeshGenerator.SQUARESIZE) + transform.position;
		}
	}
	List<List<Vector2Int>> GetRegions() {
		//Creates a list of lists of tile positions that are connected together.
		List<List<Vector2Int>> regions = new List<List<Vector2Int>> ();
		int[,] mapFlags = new int[size,size];

		for (int x = 0; x < size; x ++) {
			for (int y = 0; y < size; y ++) {
				if (mapFlags[x,y] == 0 && map[x,y] > SURFACE) {
					List<Vector2Int> newRegion = GetRegionTiles(x,y);
					regions.Add(newRegion);

					foreach (Vector2Int tile in newRegion) {
						mapFlags[tile.x, tile.y] = 1;
					}
				}
			}
		}

		return regions;
	}

	List<Vector2Int> GetRegionTiles(int startX, int startY) {
		//finds tiles connected to the start tile.
		List<Vector2Int> tiles = new List<Vector2Int> ();
		int[,] mapFlags = new int[size,size];

		Queue<Vector2Int> queue = new Queue<Vector2Int> ();
		queue.Enqueue (new Vector2Int (startX, startY));
		mapFlags [startX, startY] = 1;

		while (queue.Count > 0) {
			Vector2Int tile = queue.Dequeue();
			tiles.Add(tile);

			for (int x = tile.x - 1; x <= tile.x + 1; x++) {
				for (int y = tile.y - 1; y <= tile.y + 1; y++) {
					if (IsInMapRange(x,y) && (y == tile.y || x == tile.x)) {
						if (mapFlags[x,y] == 0 && map[x,y] > SURFACE) {
							mapFlags[x,y] = 1;
							queue.Enqueue(new Vector2Int(x,y));
						}
					}
				}
			}
		}
		return tiles;
	}

	bool IsInMapRange(int x, int y) {
		//simple check that the index is within the map array.
		return x >= 0 && x < size && y >= 0 && y < size;
	}

	void RandomFillMap() {
		//Fills the map with random noise.
		Vector2 center = new Vector2(size/2,size/2);
		for (int x = 0; x < size; x ++) {
			for (int y = 0; y < size; y ++) {
				float noise = (Mathf.PerlinNoise(((float)x/size*noiseScale)+noisePos.x,((float)y/size*noiseScale)+noisePos.y)*noiseMagnitude)+noiseFill;
				map[x,y] = Mathf.Clamp(noise,0,MAXWEIGHT);
			}
		}
	}
	void CalculateMass(){
		//Calculates the mass of all the tiles and finds the center of mass.
		float mass = 0;
		Vector2 center = Vector2.zero;
		for (int x = 0; x < size; x ++) {
			for (int y = 0; y < size; y ++) {
				if(map[x,y]>SURFACE){
					mass += map[x,y];
					center += new Vector2(x,y)*map[x,y];
				}
			}
		}
		if(mass!=0){
			rb.centerOfMass = (center/mass)*ClumpMeshGenerator.SQUARESIZE;
			rb.mass = mass;
		}
		if(mass<MAXWEIGHT){
			Destroy(gameObject);
		}
	}

	void SeperateRegion(List<Vector2Int> region){
		//takes a list of tile points and removes it from the current map while creating a new clump with the removed tile values.
		float[,] newMap = new float[size,size];
		foreach (Vector2Int tile in region)
		{
			newMap[tile.x,tile.y] = map[tile.x,tile.y];
			map[tile.x,tile.y] = 0f;
		}
		GameObject newClump = new GameObject();
		newClump.transform.position = transform.position;
		newClump.transform.rotation = transform.rotation;
		newClump.AddComponent<ClumpMeshGenerator>();
		newClump.GetComponent<MeshRenderer>().material = GetComponent<MeshRenderer>().material;
		newClump.AddComponent<Rigidbody2D>();
		ClumpGenerator newClumpGen = newClump.AddComponent<ClumpGenerator>();
		newClumpGen.size = size;
		newClumpGen.SetMap(newMap);
		newClumpGen.CalculateMass();
		ProcessMap();
		newClumpGen.ProcessMap();
	}
}