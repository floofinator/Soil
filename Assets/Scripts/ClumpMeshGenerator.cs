using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class ClumpMeshGenerator : MonoBehaviour
{
	public static float SQUARESIZE = 0.5f;
	public SquareGrid squareGrid;
	Rigidbody2D rb;
	List<Vector3> vertices;
	List<int> triangles;
	List<Color> colors;
	Dictionary<int,List<Triangle>> triangleDictionary = new Dictionary<int, List<Triangle>> ();
	List<List<int>> outlines = new List<List<int>> ();
	HashSet<int> checkedVertices = new HashSet<int>();
	public void GenerateMesh(float[,] chunk) {
		//Creates or Recreates a mesh and colliders for the clump.
		triangleDictionary.Clear ();
		outlines.Clear ();
		checkedVertices.Clear ();

		squareGrid = new SquareGrid(chunk, SQUARESIZE);

		vertices = new List<Vector3>();
		triangles = new List<int>();
		colors = new List<Color>();

		for (int x = 0; x < squareGrid.squares.GetLength(0); x ++) {
			for (int y = 0; y < squareGrid.squares.GetLength(1); y ++) {
				TriangulateSquare(squareGrid.squares[x,y]);
			}
		}

		Mesh mesh = new Mesh();
		GetComponent<MeshFilter>().mesh = mesh;

		CalculateMeshOutlines ();
		mesh.vertices = vertices.ToArray();
		mesh.colors = colors.ToArray();
		mesh.triangles = triangles.ToArray();
		mesh.RecalculateNormals();

		Generate2DColliders();
	}
	void Generate2DColliders() {
		//removes any colliders already on the object.
		PolygonCollider2D[] currentColliders = gameObject.GetComponents<PolygonCollider2D> ();
		for (int i = 0; i < currentColliders.Length; i++) {
			Destroy(currentColliders[i]);
		}

		//creates a polygon collider for each mesh outline.
		foreach (List<int> outline in outlines) {
			PolygonCollider2D edgeCollider = gameObject.AddComponent<PolygonCollider2D>();
			Vector2[] edgePoints = new Vector2[outline.Count];

			for (int i =0; i < outline.Count; i ++) {
				edgePoints[i] = new Vector2(vertices[outline[i]].x,vertices[outline[i]].y);
			}
			edgeCollider.points = edgePoints;
		}
	}
	void TriangulateSquare(Square square) {
		//finds the configuration of the square and creates a mesh based on those points.
		switch (square.configuration) {
		case 0:
			break;

		// 1 nodes:
		case 1:
			MeshFromPoints(square.centreBottom, square.bottomLeft, square.centreLeft);
			break;
		case 2:
			MeshFromPoints(square.centreRight, square.bottomRight, square.centreBottom);
			break;
		case 4:
			MeshFromPoints(square.centreTop, square.topRight, square.centreRight);
			break;
		case 8:
			MeshFromPoints(square.topLeft, square.centreTop, square.centreLeft);
			break;

		// 2 nodes:
		case 3:
			MeshFromPoints(square.centreRight, square.bottomRight, square.bottomLeft, square.centreLeft);
			break;
		case 6:
			MeshFromPoints(square.centreTop, square.topRight, square.bottomRight, square.centreBottom);
			break;
		case 9:
			MeshFromPoints(square.topLeft, square.centreTop, square.centreBottom, square.bottomLeft);
			break;
		case 12:
			MeshFromPoints(square.topLeft, square.topRight, square.centreRight, square.centreLeft);
			break;
		case 5:
			MeshFromPoints(square.centreTop, square.topRight, square.centreRight, square.centreBottom, square.bottomLeft, square.centreLeft);
			break;
		case 10:
			MeshFromPoints(square.topLeft, square.centreTop, square.centreLeft, square.centreRight, square.bottomRight, square.centreBottom);
			break;

		// 3 point:
		case 7:
			MeshFromPoints(square.centreTop, square.topRight, square.bottomRight, square.bottomLeft, square.centreLeft);
			break;
		case 11:
			MeshFromPoints(square.topLeft, square.centreTop, square.centreRight, square.bottomRight, square.bottomLeft);
			break;
		case 13:
			MeshFromPoints(square.topLeft, square.topRight, square.centreRight, square.centreBottom, square.bottomLeft);
			break;
		case 14:
			MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.centreBottom, square.centreLeft);
			break;

		// 4 point:
		case 15:
			MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);
			break;
		}
	}
	void MeshFromPoints(params Node[] nodes) {
		//Creates a mesh based on given nodes.
		AssignVertices(nodes);

		if (nodes.Length >=6){
			CreateTriangle(nodes[0], nodes[1], nodes[2]);
			CreateTriangle(nodes[3], nodes[4], nodes[5]);
			return;
		}
		if (nodes.Length >= 3)
			CreateTriangle(nodes[0], nodes[1], nodes[2]);
		if (nodes.Length >= 4)
			CreateTriangle(nodes[0], nodes[2], nodes[3]);
		if (nodes.Length >= 5) 
			CreateTriangle(nodes[0], nodes[3], nodes[4]);

	}
	void AssignVertices(Node[] nodes) {
		//adds vertices based on the given nodes.
		for (int i = 0; i < nodes.Length; i ++) {
			if (nodes[i].vertexIndex == -1) {
				nodes[i].vertexIndex = vertices.Count;
				vertices.Add(nodes[i].position);
				colors.Add(Color.white*(nodes[i].weight/ClumpGenerator.MAXWEIGHT));
			}
		}
	}
	void CreateTriangle(Node a, Node b, Node c) {
		//Creates a triangle and adds it to the dictionary
		triangles.Add(a.vertexIndex);
		triangles.Add(b.vertexIndex);
		triangles.Add(c.vertexIndex);

		Triangle triangle = new Triangle (a.vertexIndex, b.vertexIndex, c.vertexIndex);
		AddTriangleToDictionary (triangle.vertexIndexA, triangle);
		AddTriangleToDictionary (triangle.vertexIndexB, triangle);
		AddTriangleToDictionary (triangle.vertexIndexC, triangle);
	}
	void AddTriangleToDictionary(int vertexIndexKey, Triangle triangle) {
		//adds the triangle to the dictionary, grouping it with other triangles with the same vertex.
		if (triangleDictionary.ContainsKey (vertexIndexKey)) {
			triangleDictionary [vertexIndexKey].Add (triangle);
		} else {
			List<Triangle> triangleList = new List<Triangle>();
			triangleList.Add(triangle);
			triangleDictionary.Add(vertexIndexKey, triangleList);
		}
	}
	void CalculateMeshOutlines() {
		//Creates a list of outlines the mesh has.
		for (int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex ++) {
			if (!checkedVertices.Contains(vertexIndex)) {
				int newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);
				if (newOutlineVertex != -1) {
					checkedVertices.Add(vertexIndex);

					List<int> newOutline = new List<int>();
					newOutline.Add(vertexIndex);
					outlines.Add(newOutline);
					FollowOutline(newOutlineVertex, outlines.Count-1);
					outlines[outlines.Count-1].Add(vertexIndex);
				}
			}
		}
	}
	void FollowOutline(int vertexIndex, int outlineIndex) {
		//stores all the vertices that are a part of the same outline.
		outlines [outlineIndex].Add (vertexIndex);
		checkedVertices.Add (vertexIndex);
		int nextVertexIndex = GetConnectedOutlineVertex (vertexIndex);

		if (nextVertexIndex != -1) {
			FollowOutline(nextVertexIndex, outlineIndex);
		}
	}
	int GetConnectedOutlineVertex(int vertexIndex) {
		//Finds the vertex that is on the outline.
		List<Triangle> trianglesContainingVertex = triangleDictionary [vertexIndex];

		for (int i = 0; i < trianglesContainingVertex.Count; i ++) {
			Triangle triangle = trianglesContainingVertex[i];

			for (int j = 0; j < 3; j ++) {
				int vertexB = triangle[j];
				if (vertexB != vertexIndex && !checkedVertices.Contains(vertexB)) {
					if (IsOutlineEdge(vertexIndex, vertexB)) {
						return vertexB;
					}
				}
			}
		}
		return -1;
	}
	bool IsOutlineEdge(int vertexA, int vertexB) {
		//Checks if two vertices make an edge of an outline.
		List<Triangle> trianglesContainingVertexA = triangleDictionary [vertexA];
		int sharedTriangleCount = 0;

		for (int i = 0; i < trianglesContainingVertexA.Count; i ++) {
			if (trianglesContainingVertexA[i].Contains(vertexB)) {
				sharedTriangleCount ++;
				if (sharedTriangleCount > 1) {
					break;
				}
			}
		}
		return sharedTriangleCount == 1;
	}
	struct Triangle {
		//Class for a Triangle containing vertex indicies.
		public int vertexIndexA;
		public int vertexIndexB;
		public int vertexIndexC;
		int[] vertices;
		public Triangle (int a, int b, int c) {
			vertexIndexA = a;
			vertexIndexB = b;
			vertexIndexC = c;

			vertices = new int[3];
			vertices[0] = a;
			vertices[1] = b;
			vertices[2] = c;
		}
		public int this[int i] {
			get {
				return vertices[i];
			}
		}
		public bool Contains(int vertexIndex) {
			return vertexIndex == vertexIndexA || vertexIndex == vertexIndexB || vertexIndex == vertexIndexC;
		}
	}

	public class SquareGrid {
		//Class for a grid of squares made of control nodes.
		public Square[,] squares;

		public SquareGrid(float[,] map, float squareSize) {
			int size = map.GetLength(0);
			float mapWidth = size * squareSize;
			float mapHeight = size * squareSize;

			ControlNode[,] controlNodes = new ControlNode[size,size];

			for (int x = 0; x < size; x ++) {
				for (int y = 0; y < size; y ++) {
					Vector3 pos = new Vector3(x * squareSize,y * squareSize);
					float rightWeight = (x+1<size)?map[x+1,y]:ClumpGenerator.SURFACE;
					float aboveWeight = (y+1<size)?map[x,y+1]:ClumpGenerator.SURFACE;
					controlNodes[x,y] = new ControlNode(pos,map[x,y], aboveWeight, rightWeight, squareSize);
				}
			}

			squares = new Square[size-1,size-1];
			for (int x = 0; x < size-1; x ++) {
				for (int y = 0; y < size-1; y ++) {
					squares[x,y] = new Square(controlNodes[x,y+1], controlNodes[x+1,y+1], controlNodes[x+1,y], controlNodes[x,y]);
				}
			}

		}
	}
	
	public class Square {
		//Class for a Square made of four control nodes and four other edge nodes.
		public ControlNode topLeft, topRight, bottomRight, bottomLeft;
		public Node centreTop, centreRight, centreBottom, centreLeft;
		public int configuration;

		public Square (ControlNode _topLeft, ControlNode _topRight, ControlNode _bottomRight, ControlNode _bottomLeft) {
			topLeft = _topLeft;
			topRight = _topRight;
			bottomRight = _bottomRight;
			bottomLeft = _bottomLeft;

			centreTop = topLeft.right;
			centreRight = bottomRight.above;
			centreBottom = bottomLeft.right;
			centreLeft = bottomLeft.above;

			if (topLeft.weight>ClumpGenerator.SURFACE)
				configuration += 8;
			if (topRight.weight>ClumpGenerator.SURFACE)
				configuration += 4;
			if (bottomRight.weight>ClumpGenerator.SURFACE)
				configuration += 2;
			if (bottomLeft.weight>ClumpGenerator.SURFACE)
				configuration += 1;
		}

	}

	public class Node {
		//Basic Node class
		public Vector3 position;
		public float weight;
		public int vertexIndex = -1;

		public Node(Vector3 _pos) {
			position = _pos;
		}
	}

	public class ControlNode : Node {
		//Nodes with a weight and nodes above and to the right.
		public Node above, right;

		public ControlNode(Vector3 _pos, float _weight, float _aboveWeight, float _rightWeight, float squareSize) : base(_pos) {
			weight = _weight;
			
			if(Mathf.Abs(_weight-_aboveWeight)> 0.000001f){
				above = new Node(_pos + (Vector3.up*squareSize*Interp(_weight,_aboveWeight)));
				above.weight = (_weight+_aboveWeight)/2;
			}else{
				above = new Node(_pos + (Vector3.up*squareSize*0.5f));
				above.weight = (_weight+_aboveWeight)/2;
			}

			if(Mathf.Abs(_weight-_rightWeight) > 0.000001f){
				right = new Node(_pos + (Vector3.right*squareSize*Interp(_weight,_rightWeight)));
				right.weight = (_weight+_rightWeight)/2;
			}else{
				right = new Node(_pos + (Vector3.right*squareSize*0.5f));
				right.weight = (_weight+_rightWeight)/2;
			}
		}

		float Interp(float a, float b){
			return Mathf.Abs((ClumpGenerator.SURFACE-a) / (b - a));
		}
	}
}
