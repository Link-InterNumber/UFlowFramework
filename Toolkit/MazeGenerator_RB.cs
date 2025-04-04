using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public struct MazeCell
{
    public int X;
    public int Y;
    public bool Visited;
    public bool E, S, W, N;

    public MazeCell(bool visited, bool e, bool s, bool w, bool n)
    {
        Visited = visited;
        E = e;
        S = s;
        W = w;
        N = n;
        X = 0;
        Y = 0;
    }
}

public class MazeGenerator_RB : MonoBehaviour
{
    // Rows and columns being public so that clients may fill in the value they prefer
    public int row, column, wallWidth;
    public Transform wallTemp, Tile_regular, Tile_current;
    public List<Transform> currents; // Wall and cells


    private MazeCell[] maze;

    // Visited cells as well as the visit history to be recorded
    private int visitedCells;
    private Stack<Tuple<int, int>> stack = new Stack<Tuple<int, int>>();
    private List<Transform> previousCurrent = new List<Transform>();
    private List<Transform> walls = new List<Transform>();
    private List<Transform> tiles = new List<Transform>();

    System.Random rnd = new System.Random();

    void Start()
    {
        // Initialize the maze array with the preferred number of cells
        maze = new MazeCell[row * column];
        for (int i = 0; i < maze.Length; i++)
        {
            maze[i] = new MazeCell(false, false, false, false, false);
        }

        // We start from a random position and set up whatever needed
        int x = rnd.Next(row);
        int y = rnd.Next(column);
        stack.Push(Tuple.Create(x, y));
        maze[y * row + x].Visited = true;
        visitedCells = 1;

        InitializeMazeStructure();
    }

    void Update()
    {
        RB_Algorithm();
    }

    void RB_Algorithm()
    {
        List<int> neighbours = new List<int>();
        Func<int, int, uint> lookAt = (px, py) => (uint)((stack.Peek().Item1 + px) + (stack.Peek().Item2 + py) * row);

        if (visitedCells < row * column)
        {
            // North neighbour
            if (stack.Peek().Item2 > 0 && maze[lookAt(0, -1)].Visited == false)
                neighbours.Add(0); // meaning the north neighbour exists and unvisited
            // East neighbour
            if (stack.Peek().Item1 < row - 1 && maze[lookAt(+1, 0)].Visited == false)
                neighbours.Add(1);
            // South neighbour
            if (stack.Peek().Item2 < column - 1 && maze[lookAt(0, +1)].Visited == false)
                neighbours.Add(2);
            // West neighbour
            if (stack.Peek().Item1 > 0 && maze[lookAt(-1, 0)].Visited == false)
                neighbours.Add(3);

            if (neighbours.Count <= 0)
            {
                // No available neighbours so backtrack!
                stack.Pop();
            }
            else // Are there any neighbours available?
            {
                // Choose one available neighbour at random
                int nextCellDir = neighbours[rnd.Next(neighbours.Count)];

                // Create a path between the neighbour and the current cell
                switch (nextCellDir)
                {
                    case 0: // North
                        maze[lookAt(0, -1)].Visited = true;
                        maze[lookAt(0, -1)].S = true;
                        maze[lookAt(0, 0)].N = true;
                        stack.Push(Tuple.Create((stack.Peek().Item1 + 0), (stack.Peek().Item2 - 1)));
                        break;

                    case 1: // East
                        maze[lookAt(+1, 0)].Visited = true;
                        maze[lookAt(+1, 0)].W = true;
                        maze[lookAt(0, 0)].E = true;
                        stack.Push(Tuple.Create((stack.Peek().Item1 + 1), (stack.Peek().Item2 + 0)));
                        break;

                    case 2: // South
                        maze[lookAt(0, +1)].Visited = true;
                        maze[lookAt(0, +1)].N = true;
                        maze[lookAt(0, 0)].S = true;
                        stack.Push(Tuple.Create((stack.Peek().Item1 + 0), (stack.Peek().Item2 + 1)));
                        break;

                    case 3: // West
                        maze[lookAt(-1, 0)].Visited = true;
                        maze[lookAt(-1, 0)].E = true;
                        maze[lookAt(0, 0)].W = true;
                        stack.Push(Tuple.Create((stack.Peek().Item1 - 1), (stack.Peek().Item2 + 0)));
                        break;
                }

                visitedCells++; // Update the visited cells' number
            }

            DrawEverything();
        }
    }

    void DrawEverything()
    {
        // Draw Maze
        // for (int x = 0; x < row; x++)
        // {
        //     for (int y = 0; y < column; y++)
        //     {
        // Knock down the wall pieces we put before
        for (int p = 0; p < wallWidth; p++)
        {
            Vector3 v3 = new Vector3(stack.Peek().Item1 * (wallWidth + 1) + p, 0,
                stack.Peek().Item2 * (wallWidth + 1) + wallWidth);
            Vector3 v3_2 = new Vector3(stack.Peek().Item1 * (wallWidth + 1) + wallWidth, 0,
                stack.Peek().Item2 * (wallWidth + 1) + p);
            Vector3 v3_3 = new Vector3(stack.Peek().Item1 * (wallWidth + 1) + p, 0,
                stack.Peek().Item2 * (wallWidth + 1) - 1);
            Vector3 v3_4 = new Vector3(stack.Peek().Item1 * (wallWidth + 1) - 1, 0,
                stack.Peek().Item2 * (wallWidth + 1) + p);
            // Vector3 v3 = new Vector3(x * (wallWidth + 1) + p, 0, y * (wallWidth + 1) + wallWidth);
            // Vector3 v3_2 = new Vector3(x * (wallWidth + 1) + wallWidth, 0, y * (wallWidth + 1) + p);
            if (maze[stack.Peek().Item2 * row + stack.Peek().Item1].S && checkIfWallPosEmpty(v3))
                Instantiate(Tile_regular, v3, Quaternion.identity);
            if (maze[stack.Peek().Item2 * row + stack.Peek().Item1].E && checkIfWallPosEmpty(v3_2))
                Instantiate(Tile_regular, v3_2, Quaternion.identity);
            if (maze[stack.Peek().Item2 * row + stack.Peek().Item1].N && checkIfWallPosEmpty(v3_3))
                Instantiate(Tile_regular, v3_3, Quaternion.identity);
            if (maze[stack.Peek().Item2 * row + stack.Peek().Item1].W && checkIfWallPosEmpty(v3_4))
                Instantiate(Tile_regular, v3_4, Quaternion.identity);
        }

        //     }
        // }

        // Get rid of the previous stack peek first
        // foreach (Transform t in previousCurrent)
        // {
        //     if (t != null) // because it can be destroyed already
        //         Instantiate(Tile_regular, t.position, Quaternion.identity);
        // }


        // Then we draw the new stack peek
        var index = 0;
        for (int py = 0; py < wallWidth; py++)
        {
            for (int px = 0; px < wallWidth; px++)
            {
                Vector3 v3 = new Vector3(stack.Peek().Item1 * (wallWidth + 1) + px, 0,
                    stack.Peek().Item2 * (wallWidth + 1) + py);
                currents[index].position = v3;
                // previousCurrent.Add(Instantiate(Tile_current, v3, Quaternion.identity));
                index++;
            }
        }
    }

    private void InitializeMazeStructure()
    {
        for (int x = 0; x < row; x++)
        {
            for (int y = 0; y < column; y++)
            {
                // Each cell is inflated by wallWidth, so fill it in
                for (int py = 0; py < wallWidth; py++)
                {
                    for (int px = 0; px < wallWidth; px++)
                    {
                        Vector3 v3 = new Vector3(x * (wallWidth + 1) + px, 0, y * (wallWidth + 1) + py);
                        tiles.Add(Instantiate(Tile_regular, v3, Quaternion.identity));
                        if (px != py) continue;
                        Vector3 v3_1 = new Vector3(x * (wallWidth + 1) + px, 0, y * (wallWidth + 1) + wallWidth); //右边
                        Vector3 v3_2 = new Vector3(x * (wallWidth + 1) + wallWidth, 0, y * (wallWidth + 1) + px); //下面
                        walls.Add(Instantiate(wallTemp, v3_1, Quaternion.identity));
                        walls.Add(Instantiate(wallTemp, v3_2, Quaternion.identity));
                    }
                }

                // Fill in the corner cell
                walls.Add(Instantiate(wallTemp,
                    new Vector3((x + 1) * (wallWidth + 1) - 1, 0, (y + 1) * (wallWidth + 1) - 1),
                    Quaternion.identity));
            }
        }

        // Fill in rest of the cells
        for (int i = 0; i < row * (wallWidth + 1); i++)
        {
            var wall = Instantiate(wallTemp, new Vector3(i, 0, (column - column) * (wallWidth + 1) - 1),
                Quaternion.identity);
            walls.Add(wall);
        }

        for (int j = -1; j < column * (wallWidth + 1); j++)
        {
            var wall = Instantiate(wallTemp, new Vector3((row - row) * (wallWidth + 1) - 1, 0, j), Quaternion.identity);
            walls.Add(wall);
        }

        for (int i = 0; i < wallWidth * wallWidth; i++)
        {
            currents.Add(Instantiate(Tile_current));
        }
    }

    private bool checkIfWallPosEmpty(Vector3 targetPos)
    {
        var toDestroy = walls.FirstOrDefault(o => o.position == targetPos);
        if (toDestroy)
        {
            walls.Remove(toDestroy);
            GameObject.Destroy(toDestroy.gameObject);
            return true;
        }
        // foreach (GameObject t in allTilings)
        // {
        //     if (t.transform.position == targetPos)
        //     {
        //         Destroy(t);
        //         break;
        //     }
        // }

        return false;
    }

    private bool checkIfTilePosEmpty(Vector3 targetPos)
    {
        // GameObject[] allTilings = GameObject.FindGameObjectsWithTag("Tile");
        var toDestroy = tiles.FirstOrDefault(o => o.position == targetPos);
        if (toDestroy)
        {
            tiles.Remove(toDestroy);
            GameObject.Destroy(toDestroy.gameObject);
            return false;
        }
        // foreach (GameObject t in allTilings)
        // {
        //     if (t.transform.position == targetPos)
        //     {
        //         Destroy(t);
        //         break;
        //     }
        // }

        return true;
    }
}