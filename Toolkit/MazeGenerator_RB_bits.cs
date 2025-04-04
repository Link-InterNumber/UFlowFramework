using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;
using PowerCellStudio;
using UnityEngine.Serialization;

public class MazeGenerator_RB_bits : MonoBehaviour
{
    // Rows and columns being public so that clients may fill in the value they prefer
    public bool drawTileFirst;
    public int row, column;
    public int wallWidth = 1;
    public Transform walls, tileRegular, tileCurrent; // Wall and cells
    public Transform root;

    private const uint North = 0b_0000_0001;
    private const uint East = 0b_0000_0010;
    private const uint South = 0b_0000_0100;
    private const uint West = 0b_0000_1000;
    private const uint Visited = 0b_0001_0000;
    private int _wallWidth => Mathf.Max(1, wallWidth);

    private uint[] _maze;

    // Visited cells as well as the visit history to be recorded
    private int _visitedCells;

    private Stack<Vector2Int> _stack = new Stack<Vector2Int>();

    // private List<Transform> _previousCurrent = new List<Transform>();
    private System.Random _rnd = new System.Random();

    private Vector2Int _startPos;

    public MazeCell GetCell(int x, int y)
    {
        if (x < 0 || x >= row || y < 0 || y >= column) return new MazeCell();
        var data = _maze[y * row + x];
        var cell = new MazeCell(true,
            (data & East) == 2,
            (data & South) == 4,
            (data & West) == 8,
            (data & North) == 1);
        cell.X = x;
        cell.Y = y;
        return cell;
    }

    public void BuildMaze()
    {
        var data = PlayerDataUtils.ReadJson<PlayerSave>("save.txt");
        
        if (_isBuilding || _builded) return;
        ApplicationManager.instance.StartCoroutine(FullMaze());
    }

    private bool _isBuilding = false;
    private bool _builded = false;

    private IEnumerator FullMaze()
    {
        while (!_clearTile)
        {
            yield return null;
        }
        _maze = new uint[row * column];
        // We start from a random position and set up whatever needed
        int x = _rnd.Next(row);
        int y = _rnd.Next(column);
        _startPos.x = x;
        _startPos.y = y;
        _stack.Push(new Vector2Int(x, y));
        _maze[y * row + x] = Visited;
        _visitedCells = 1;

        _isBuilding = true;
        List<int> neighbours = new List<int>();
        Func<int, int, uint> lookAt = (px, py) => (uint)((_stack.Peek().x + px) + (_stack.Peek().y + py) * row);
        while (_visitedCells < row * column)
        {
            neighbours.Clear();
            // North neighbour
            if (_stack.Peek().y > 0 && (_maze[lookAt(0, -1)] & Visited) == 0)
                neighbours.Add(0); // meaning the north neighbour exists and unvisited
            // East neighbour
            if (_stack.Peek().x < row - 1 && (_maze[lookAt(+1, 0)] & Visited) == 0)
                neighbours.Add(1);
            // South neighbour
            if (_stack.Peek().y < column - 1 && (_maze[lookAt(0, +1)] & Visited) == 0)
                neighbours.Add(2);
            // West neighbour
            if (_stack.Peek().x > 0 && (_maze[lookAt(-1, 0)] & Visited) == 0)
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
                        _maze[lookAt(0, -1)] |= Visited | South;
                        _maze[lookAt(0, 0)] |= North;
                        _stack.Push(new Vector2Int((_stack.Peek().x + 0), (_stack.Peek().y - 1)));
                        break;

                    case 1: // East
                        _maze[lookAt(+1, 0)] |= Visited | West;
                        _maze[lookAt(0, 0)] |= East;
                        _stack.Push(new Vector2Int((_stack.Peek().x + 1), (_stack.Peek().y + 0)));
                        break;

                    case 2: // South
                        _maze[lookAt(0, +1)] |= Visited | North;
                        _maze[lookAt(0, 0)] |= South;
                        _stack.Push(new Vector2Int((_stack.Peek().x + 0), (_stack.Peek().y + 1)));
                        break;

                    case 3: // West
                        _maze[lookAt(-1, 0)] |= Visited | East;
                        _maze[lookAt(0, 0)] |= West;
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

        if (drawTileFirst) yield return InitializeMazeStructure();
        yield return BuildTileNWall();
        yield return SetEntryNExit();
        _builded = true;
        _isBuilding = false;
    }

    public void DestroyMaze()
    {
        if (!_clearTile || _isBuilding) return;
        ApplicationManager.instance.StartCoroutine(DestroyMazeHandle());
    }

    private bool _clearTile = true;

    private IEnumerator DestroyMazeHandle()
    {
        _clearTile = false;
        root.gameObject.SetActive(false);
        var loopNum = root.childCount;
        for (int i = 0; i < loopNum; i++)
        {
            var obj = root.GetChild(0);
            obj.SetParent(null);
            GameObject.Destroy(obj.gameObject);
            if (i % 50 == 0) yield return null;
        }

        root.gameObject.SetActive(true);
        _builded = false;
        _clearTile = true;
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
                if (i >= row || (y - i) >= column) continue;
                for (int py = 0; py < _wallWidth; py++)
                {
                    for (int px = 0; px < _wallWidth; px++)
                    {
                        Vector3 v3 = new Vector3(
                            i * (_wallWidth + 1) + px, 0,
                            (y - i) * (_wallWidth + 1) + py);
                        Instantiate(tileRegular, v3, Quaternion.identity, root);
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
        for (int i = 0; i < row * (_wallWidth + 1); i++)
        {
            Instantiate(walls, new Vector3(i, 0, (column - column) * (_wallWidth + 1) - 1), Quaternion.identity, root);
        }

        for (int j = -1; j < column * (_wallWidth + 1); j++)
        {
            Instantiate(walls, new Vector3((row - row) * (_wallWidth + 1) - 1, 0, j), Quaternion.identity, root);
        }
    }

    private IEnumerator BuildTileNWall()
    {
        List<Vector2Int> curPos = new List<Vector2Int>();
        List<Vector2Int> tempPos = new List<Vector2Int>();

        curPos.Add(_startPos);
        var drawNum = 0;
        uint unView = 0b_0000_1111;
        while (drawNum < _visitedCells)
        {
            tempPos.AddRange(curPos);
            curPos.Clear();
            foreach (var vector2Int in tempPos)
            {
                if (_maze[vector2Int.y * row + vector2Int.x] < 16) continue;
                _maze[vector2Int.y * row + vector2Int.x] &= unView;

                if ((_maze[vector2Int.y * row + vector2Int.x] & South) == 4 &&
                    _maze[(vector2Int.y + 1) * row + vector2Int.x] >= 16)
                    curPos.Add(new Vector2Int(vector2Int.x, vector2Int.y + 1));

                if ((_maze[vector2Int.y * row + vector2Int.x] & East) == 2 &&
                    _maze[vector2Int.y * row + vector2Int.x + 1] >= 16)
                    curPos.Add(new Vector2Int(vector2Int.x + 1, vector2Int.y));

                if ((_maze[vector2Int.y * row + vector2Int.x] & North) == 1 &&
                    _maze[(vector2Int.y - 1) * row + vector2Int.x] >= 16)
                    curPos.Add(new Vector2Int(vector2Int.x, vector2Int.y - 1));

                if ((_maze[vector2Int.y * row + vector2Int.x] & West) == 8 &&
                    _maze[vector2Int.y * row + vector2Int.x - 1] >= 16)
                    curPos.Add(new Vector2Int(vector2Int.x - 1, vector2Int.y));

                for (int py = 0; py < _wallWidth; py++)
                {
                    for (int px = 0; px < _wallWidth; px++)
                    {
                        drawNum++;
                        if (!drawTileFirst)
                        {
                            Vector3 v3 = new Vector3(vector2Int.x * (_wallWidth + 1) + px, 0,
                                vector2Int.y * (_wallWidth + 1) + py);
                            Instantiate(tileRegular, v3, Quaternion.identity, root);
                        }

                        if (px != py) continue;
                        Vector3 v3_1 = new Vector3(vector2Int.x * (_wallWidth + 1) + px, 0,
                            vector2Int.y * (_wallWidth + 1) + _wallWidth);
                        Vector3 v3_2 = new Vector3(vector2Int.x * (_wallWidth + 1) + _wallWidth, 0,
                            vector2Int.y * (_wallWidth + 1) + px);
                        // Vector3 v3_3 = new Vector3(vector2Int.x * (wallWidth + 1) + px, 0, vector2Int.y * (wallWidth + 1) - 1);
                        // Vector3 v3_4 = new Vector3(vector2Int.x * (wallWidth + 1) - 1, 0, vector2Int.y * (wallWidth + 1) + px);

                        // Instantiate(walls, v3_1, Quaternion.identity), Root;
                        // Instantiate(walls, v3_2, Quaternion.identity), Root;

                        if ((_maze[vector2Int.y * row + vector2Int.x] & South) == 4)
                            Instantiate(tileRegular, v3_1, Quaternion.identity, root);
                        else Instantiate(walls, v3_1, Quaternion.identity, root);

                        if ((_maze[vector2Int.y * row + vector2Int.x] & East) == 2)
                            Instantiate(tileRegular, v3_2, Quaternion.identity, root);
                        else Instantiate(walls, v3_2, Quaternion.identity, root);
                        // if ((maze[vector2Int.y * row + vector2Int.x] & NORTH) == 1)
                        //     Instantiate(Tile_regular, v3_3, Quaternion.identity), Root;
                        // if ((maze[vector2Int.y * row + vector2Int.x] & WEST) == 8)
                        //     Instantiate(Tile_regular, v3_4, Quaternion.identity), Root;
                    }
                }

                // Fill in the corner cell
                Instantiate(walls,
                    new Vector3((vector2Int.x + 1) * (_wallWidth + 1) - 1, 0, (vector2Int.y + 1) * (_wallWidth + 1) - 1),
                    Quaternion.identity, root);
            }

            tempPos.Clear();
            yield return null;
        }

        if (drawTileFirst) yield break;
        for (int i = 0; i < row * (_wallWidth + 1); i++)
        {
            Instantiate(walls, new Vector3(i, 0, (column - column) * (_wallWidth + 1) - 1), Quaternion.identity, root);
        }

        for (int j = -1; j < column * (_wallWidth + 1); j++)
        {
            Instantiate(walls, new Vector3((row - row) * (_wallWidth + 1) - 1, 0, j), Quaternion.identity, root);
        }
    }

    private IEnumerator SetEntryNExit()
    {
        for (int py = 0; py < _wallWidth; py++)
        {
            for (int px = 0; px < _wallWidth; px++)
            {
                Vector3 v3 = new Vector3(0 * (_wallWidth + 1) + px, 0, 0 * (_wallWidth + 1) + py);
                Instantiate(tileCurrent, v3, Quaternion.identity, root);
            }
        }

        yield return null;

        // for (int py = 0; py < wallWidth; py++)
        // {
        //     for (int px = 0; px < wallWidth; px++)
        //     {
        //         Vector3 v3 = new Vector3((row - 1) * (wallWidth + 1) + px, 0,
        //             (column - 1) * (wallWidth + 1) + py);
        //         Instantiate(tileCurrent, v3, Quaternion.identity, root);
        //     }
        // }
    }
}