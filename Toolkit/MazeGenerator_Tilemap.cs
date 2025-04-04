using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;
// using DanielLochner.Assets;
// using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine.Tilemaps;

public class MazeGenerator_Tilemap : MonoBehaviour
{
    // Rows and columns being public so that clients may fill in the value they prefer
    public bool DrawTileFirst;
    public int row, column, wallWidth;
    public Tile walls, Tile_regular, Tile_current; // Wall and cells
    public Tilemap Root;

    uint NORTH = 0b_0000_0001;
    uint EAST = 0b_0000_0010;
    uint SOUTH = 0b_0000_0100;
    uint WEST = 0b_0000_1000;
    uint VISITED = 0b_0001_0000;

    private uint[] _maze;
    // Visited cells as well as the visit history to be recorded
    private int _visitedCells;
    private Stack<Vector2Int> _stack = new Stack<Vector2Int>();
    // private List<Transform> _previousCurrent = new List<Transform>();
    System.Random _rnd = new System.Random();

    // void Start()
    // {
    //     InitializeMazeStructure();
    // }

    // void Update()
    // {
    //     if (RB_Algorithm())
    //         DrawEverything();
    // }

    private Vector2Int startPos;
    // [Button(ButtonSizes.Large)]
    public void BuildMaze()
    {
        if (isBuilding) return;
        CoroutineRunner.instance.StartCoroutine(FullMaze());
    }

    private bool isBuilding = false;
    private IEnumerator FullMaze()
    {
        while (!clearTile)
        {
            yield return null;
        }
        
        _maze = new uint[row * column];
        // We start from a random position and set up whatever needed
        int x = _rnd.Next(row); 
        int y = _rnd.Next(column);
        startPos.x = x;
        startPos.y = y;
        _stack.Push(new Vector2Int(x, y));
        _maze[y * row + x] = VISITED;
        _visitedCells = 1;

        isBuilding = true;
        List<int> neighbours = new List<int>();
        Func<int, int, uint> lookAt = (px, py) => (uint)((_stack.Peek().x + px) + (_stack.Peek().y + py) * row);
        while (_visitedCells < row * column)
        {
            neighbours.Clear();
            // North neighbour
            if (_stack.Peek().y > 0 && (_maze[lookAt(0, -1)] & VISITED) == 0)
                neighbours.Add(0); // meaning the north neighbour exists and unvisited
            // East neighbour
            if (_stack.Peek().x < row - 1 && (_maze[lookAt(+1, 0)] & VISITED) == 0)
                neighbours.Add(1);
            // South neighbour
            if (_stack.Peek().y < column - 1 && (_maze[lookAt(0, +1)] & VISITED) == 0)
                neighbours.Add(2);
            // West neighbour
            if (_stack.Peek().x > 0 && (_maze[lookAt(-1, 0)] & VISITED) == 0)
                neighbours.Add(3);

            // Are there any neighbours available?
            if (neighbours.Count > 0)
            {
                // Choose one available neighbour at random
                int nextCellDir = neighbours[_rnd.Next(neighbours.Count)];

                // Create a path between the neighbour and the current cell
                switch (nextCellDir)
                {
                    case 0: // North
                        _maze[lookAt(0, -1)] |= VISITED | SOUTH;
                        _maze[lookAt(0, 0)] |= NORTH;
                        _stack.Push(new Vector2Int((_stack.Peek().x + 0), (_stack.Peek().y - 1)));
                        break;

                    case 1: // East
                        _maze[lookAt(+1, 0)] |= VISITED | WEST;
                        _maze[lookAt(0, 0)] |= EAST;
                        _stack.Push(new Vector2Int((_stack.Peek().x + 1), (_stack.Peek().y + 0)));
                        break;

                    case 2: // South
                        _maze[lookAt(0, +1)] |= VISITED | NORTH;
                        _maze[lookAt(0, 0)] |= SOUTH;
                        _stack.Push(new Vector2Int((_stack.Peek().x + 0), (_stack.Peek().y + 1)));
                        break;

                    case 3: // West
                        _maze[lookAt(-1, 0)] |= VISITED | EAST;
                        _maze[lookAt(0, 0)] |= WEST;
                        _stack.Push(new Vector2Int((_stack.Peek().x - 1), (_stack.Peek().y + 0)));
                        break;

                }
                _visitedCells++; // Update the visited cells' number
                if (_visitedCells % 20 == 0) yield return null;
            }
            else
            {
                // No available neighbours so backtrack!
                _stack.Pop();
            }
        }
        if(DrawTileFirst) yield return InitializeMazeStructure();
        yield return BuildTileNWall();
        isBuilding = false;
    }

    // [Button(ButtonSizes.Large)]
    public void DestroyMaze()
    {
        if (!clearTile || isBuilding) return;
        CoroutineRunner.instance.StartCoroutine(DestroyMazeHandle());
    }

    private bool clearTile = true;
    private IEnumerator DestroyMazeHandle()
    {
        clearTile = false;
        Root.ClearAllTiles();
        yield return null;
        // Root.gameObject.SetActive(false);
        // var loopNum = Root.childCount;
        // for (int i = 0; i < loopNum; i++)
        // {
        //     var obj = Root.GetChild(0);
        //     obj.SetParent(null);
        //     GameObject.Destroy(obj.gameObject);
        //     if (i % 50 == 0) yield return null;
        // }
        // Root.gameObject.SetActive(true);
        clearTile = true;
    }
    
    private IEnumerator InitializeMazeStructure()
    {
        // for (int x = 0; x < row; x++)
        // {
        var loopNum = (Mathf.Max(column, row) - 1) * 2 + 1;
            for (int y = 0; y < loopNum; y++)
            {
                // Each cell is inflated by wallWidth, so fill it in
                for (int i = 0; i < y + 1; i++)
                {
                    if(i >= row || (y - i) >= column) continue;
                    for (int py = 0; py < wallWidth; py++)
                    {
                        for (int px = 0; px < wallWidth; px++)
                        {
                            Vector3Int v3 = new Vector3Int(
                                i * (wallWidth + 1) + px, 
                                (y - i) * (wallWidth + 1) + py,
                                0);
                            Root.SetTile(v3, Tile_regular);
                            // Instantiate(Tile_regular, v3, Quaternion.identity, Root);
                            // Vector3 v3_1 = new Vector3(x * (wallWidth + 1) + px, 0, y * (wallWidth + 1) + wallWidth);
                            // Vector3 v3_2 = new Vector3(x * (wallWidth + 1) + wallWidth, 0, y * (wallWidth + 1) + px);
                            // Instantiate(walls, v3_1, Quaternion.identity, Root);
                            // Instantiate(walls, v3_2, Quaternion.identity, Root);
                        }
                    }
                }
                yield return null;

                // Fill in the corner cell
                // Instantiate(walls, new Vector3((x+1) * (wallWidth + 1) - 1, 0, (y+1) * (wallWidth + 1) - 1), Quaternion.identity, Root);
            }

        // }
        // Fill in rest of the cells
        for (int i = 0; i < row * (wallWidth+1); i++)
        {
            Root.SetTile(new Vector3Int(i,  (column - column)* (wallWidth + 1) - 1), walls);
        }
        for (int j = -1; j < column * (wallWidth + 1); j++)
        {
            Root.SetTile(new Vector3Int((row - row) * (wallWidth + 1) - 1, j), walls);
        }
    }

    private IEnumerator BuildTileNWall()
    {
        List<Vector2Int> curPos = new List<Vector2Int>();
        List<Vector2Int> tempPos = new List<Vector2Int>();

        curPos.Add(startPos);
        var drawNum = 0;
        uint unView = 0b_0000_1111;
        while (drawNum < _visitedCells)
        {
            tempPos.AddRange(curPos);
            curPos.Clear();
            foreach (var vector2Int in tempPos)
            {
                if(_maze[vector2Int.y * row + vector2Int.x] < 16) continue; 
                _maze[vector2Int.y * row + vector2Int.x] &= unView;

                if ((_maze[vector2Int.y * row + vector2Int.x] & SOUTH) == 4 && _maze[(vector2Int.y + 1) * row + vector2Int.x] >= 16)
                    curPos.Add(new Vector2Int(vector2Int.x, vector2Int.y + 1));

                if ((_maze[vector2Int.y * row + vector2Int.x] & EAST) == 2 && _maze[vector2Int.y  * row + vector2Int.x + 1] >= 16)
                    curPos.Add(new Vector2Int(vector2Int.x + 1, vector2Int.y));

                if ((_maze[vector2Int.y * row + vector2Int.x] & NORTH) == 1 && _maze[(vector2Int.y - 1) * row + vector2Int.x] >= 16)
                    curPos.Add(new Vector2Int(vector2Int.x, vector2Int.y - 1));
                
                if ((_maze[vector2Int.y * row + vector2Int.x] & WEST) == 8 && _maze[vector2Int.y * row + vector2Int.x - 1] >= 16)
                    curPos.Add(new Vector2Int(vector2Int.x - 1, vector2Int.y));
                
                for (int py = 0; py < wallWidth; py++)
                {
                    for (int px = 0; px < wallWidth; px++)
                    {
                        drawNum++;
                        if (!DrawTileFirst)
                        {
                            Vector3Int v3 = new Vector3Int(vector2Int.x * (wallWidth + 1) + px, vector2Int.y * (wallWidth + 1) + py);
                            Root.SetTile(v3, Tile_regular);// (Tile_regular, v3, Quaternion.identity, Root);
                        }

                        if(px != py) continue;
                        Vector3Int v3_1 = new Vector3Int(vector2Int.x * (wallWidth + 1) + px, vector2Int.y * (wallWidth + 1) + wallWidth );
                        Vector3Int v3_2 = new Vector3Int(vector2Int.x * (wallWidth + 1) +  wallWidth, vector2Int.y * (wallWidth + 1) + px);
                        // Vector3 v3_3 = new Vector3(vector2Int.x * (wallWidth + 1) + px, 0, vector2Int.y * (wallWidth + 1) - 1);
                        // Vector3 v3_4 = new Vector3(vector2Int.x * (wallWidth + 1) - 1, 0, vector2Int.y * (wallWidth + 1) + px);
                        
                        // Instantiate(walls, v3_1, Quaternion.identity), Root;
                        // Instantiate(walls, v3_2, Quaternion.identity), Root;
                        
                        if ((_maze[vector2Int.y * row + vector2Int.x] & SOUTH) == 4)
                            Root.SetTile(v3_1, Tile_regular);
                        else Root.SetTile(v3_1, walls);
                        
                        if ((_maze[vector2Int.y * row + vector2Int.x] & EAST) == 2)
                            Root.SetTile(v3_2, Tile_regular);
                        else Root.SetTile(v3_2, walls);
                        // if ((maze[vector2Int.y * row + vector2Int.x] & NORTH) == 1)
                        //     Instantiate(Tile_regular, v3_3, Quaternion.identity), Root;
                        // if ((maze[vector2Int.y * row + vector2Int.x] & WEST) == 8)
                        //     Instantiate(Tile_regular, v3_4, Quaternion.identity), Root;
                    }
                }
                // Fill in the corner cell
                Root.SetTile(new Vector3Int((vector2Int.x+1) * (wallWidth + 1) - 1, (vector2Int.y+1) * (wallWidth + 1) - 1), walls);
            }
            tempPos.Clear();
            yield return null;
        }
        if(DrawTileFirst) yield break;
        for (int i = 0; i < row * (wallWidth+1); i++)
        {
            Root.SetTile(new Vector3Int(i, (column - column)* (wallWidth + 1) - 1), walls);
        }
        for (int j = -1; j < column * (wallWidth + 1); j++)
        {
            Root.SetTile(new Vector3Int((row - row) * (wallWidth + 1) - 1, j), walls);
        }
    }

}