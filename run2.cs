using System;
using System.Linq;
using System.Collections.Generic;

namespace MazeSolution
{
    class Program
    {
        private static Dictionary<char, int> KeyBitIndices = new Dictionary<char, int>();
        private static List<Tuple<int, int>> StartPositions = new List<Tuple<int, int>>();
        private static List<Tuple<int, int>> KeyPositions = new List<Tuple<int, int>>();
        private static Tuple<int, int>[] AllPoints;
        private static List<PathInfo>[] Connections;
        private static int[,] Distances;
        private static readonly int[] RowDirections = { -1, 1, 0, 0 };
        private static readonly int[] ColDirections = { 0, 0, -1, 1 };
        private static int Rows, Cols;
        private static int TotalPointCount;
        private static int AllKeysMask;
        const int Infinity = 1000000;
        
        private class PriorityQueue<T>
        {
            private List<Queue<T>> Buckets;
            private int CurrentIndex;
            public int Count { get; private set; }

            public PriorityQueue(int maxPriority)
            {
                Buckets = new List<Queue<T>>(maxPriority + 1);
                for (int i = 0; i <= maxPriority; i++)
                    Buckets.Add(new Queue<T>());
                CurrentIndex = 0;
                Count = 0;
            }

            public void Enqueue(T item, int priority)
            {
                if (priority < 0 || priority >= Buckets.Count)
                    throw new ArgumentOutOfRangeException();
                Buckets[priority].Enqueue(item);
                if (priority < CurrentIndex)
                    CurrentIndex = priority;
                Count++;
            }

            public Tuple<T, int> Dequeue()
            {
                while (CurrentIndex < Buckets.Count)
                {
                    var queue = Buckets[CurrentIndex];
                    if (queue.Count > 0)
                    {
                        Count--;
                        return Tuple.Create(queue.Dequeue(), CurrentIndex);
                    }
                    CurrentIndex++;
                }
                return Tuple.Create(default(T), -1);
            }
        }
        
        private struct PathInfo
        {
            public int Destination;
            public int Steps;
            public int RequiredKeys;
        }
        
        private static ulong PackState(int robot1, int robot2, int robot3, int robot4, int keys)
        {
            return (ulong)keys |
                   ((ulong)robot1 << 26) |
                   ((ulong)robot2 << 32) |
                   ((ulong)robot3 << 38) |
                   ((ulong)robot4 << 44);
        }
        
        private static Tuple<int, int[]> UnpackState(ulong state)
        {
            int collectedKeys = (int)(state & 0x3FFFFFF);
            int[] robots = new int[4];
            robots[0] = (int)((state >> 26) & 0x3F);
            robots[1] = (int)((state >> 32) & 0x3F);
            robots[2] = (int)((state >> 38) & 0x3F);
            robots[3] = (int)((state >> 44) & 0x3F);
            return Tuple.Create(collectedKeys, robots);
        }

        private static void ComputeAllDistances()  // Флойд-Уоршелл
        {
            int n = AllPoints.Length;
            Distances = new int[n, n];

            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    Distances[i, j] = (i == j) ? 0 : Infinity;

            for (int i = 0; i < n; i++)
                foreach (var connection in Connections[i])
                    Distances[i, connection.Destination] = Math.Min(Distances[i, connection.Destination], connection.Steps);

            for (int k = 0; k < n; k++)
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < n; j++)
                        if (Distances[i, k] + Distances[k, j] < Distances[i, j])
                            Distances[i, j] = Distances[i, k] + Distances[k, j];
        }
        
        private static int FindShortestPath() // BFS + A*
        {
            var costMap = new Dictionary<ulong, int>();
            var queue = new PriorityQueue<ulong>(Rows * Cols + 1);

            ulong startState = PackState(0, 1, 2, 3, 0);
            costMap[startState] = 0;

            int initialHeuristic = EstimateHeuristic(new int[] { 0, 1, 2, 3 }, 0);
            queue.Enqueue(startState, initialHeuristic);

            while (queue.Count > 0)
            {
                var (state, priority) = queue.Dequeue();
                if (state == 0UL) break;

                var (keys, robots) = UnpackState(state);
                int currentCost = costMap[state];

                if (currentCost + EstimateHeuristic(robots, keys) != priority)
                    continue;

                if (keys == AllKeysMask)
                    return currentCost;

                for (int i = 0; i < 4; i++)
                {
                    foreach (var edge in Connections[robots[i]])
                    {
                        if ((keys & edge.RequiredKeys) != edge.RequiredKeys)
                            continue;

                        int newKeys = keys;
                        if (edge.Destination >= 4)
                            newKeys |= 1 << (edge.Destination - 4);

                        int[] newRobots = new int[4];
                        Array.Copy(robots, newRobots, 4);
                        newRobots[i] = edge.Destination;

                        ulong newState = PackState(newRobots[0], newRobots[1], newRobots[2], newRobots[3], newKeys);
                        int newCost = currentCost + edge.Steps;

                        if (costMap.TryGetValue(newState, out int existingCost) && existingCost <= newCost)
                            continue;

                        costMap[newState] = newCost;
                        int heuristic = EstimateHeuristic(newRobots, newKeys);
                        queue.Enqueue(newState, newCost + heuristic);
                    }
                }
            }

            return -1;
        }
        
        private static int EstimateHeuristic(int[] positions, int collectedKeys)
        {
            int remainingKeys = AllKeysMask & ~collectedKeys;
            int maxDistance = 0;

            for (int bit = 0; bit < 26; bit++)
            {
                if ((remainingKeys & (1 << bit)) != 0)
                {
                    int targetKeyIndex = 4 + bit;
                    int minDistToKey = Infinity;
                    foreach (int pos in positions)
                        minDistToKey = Math.Min(minDistToKey, Distances[pos, targetKeyIndex]);

                    if (minDistToKey < Infinity)
                        maxDistance = Math.Max(maxDistance, minDistToKey);
                }
            }

            return maxDistance;
        }
        
        private static void ParseGrid(List<List<char>> grid)
        {
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Cols; col++)
                {
                    char cell = grid[row][col];
                    if (cell == '@')
                        StartPositions.Add(Tuple.Create(row, col));
                    else if (cell >= 'a' && cell <= 'z' && !KeyBitIndices.ContainsKey(cell))
                    {
                        KeyBitIndices[cell] = KeyBitIndices.Count;
                        KeyPositions.Add(Tuple.Create(row, col));
                    }
                }
            }
        }
        
        private static void SetupPoints() // Инициализация всех точек (роботы и ключи)
        {
            AllPoints = new Tuple<int, int>[TotalPointCount];
            for (int i = 0; i < 4; i++)
                AllPoints[i] = StartPositions[i];
            foreach (var keyPair in KeyBitIndices)
                AllPoints[4 + keyPair.Value] = KeyPositions[keyPair.Value];
        }
        
        private static void BuildConnectionGraph(List<List<char>> grid) // Построение графа переходов между точками
        {
            Connections = new List<PathInfo>[TotalPointCount];
            for (int i = 0; i < TotalPointCount; i++)
                Connections[i] = new List<PathInfo>();

            for (int i = 0; i < TotalPointCount; i++)
            {
                var dist = new int[Rows, Cols];
                var reqKeys = new int[Rows, Cols];

                for (int r = 0; r < Rows; r++)
                    for (int c = 0; c < Cols; c++)
                        dist[r, c] = -1;

                var queue = new Queue<Tuple<int, int>>();
                var start = AllPoints[i];
                dist[start.Item1, start.Item2] = 0;
                queue.Enqueue(start);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    int currRow = current.Item1;
                    int currCol = current.Item2;
                    int currentDist = dist[currRow, currCol];
                    int currentKeys = reqKeys[currRow, currCol];

                    for (int dir = 0; dir < 4; dir++)
                    {
                        int newRow = currRow + RowDirections[dir];
                        int newCol = currCol + ColDirections[dir];

                        if (newRow < 0 || newRow >= Rows || newCol < 0 || newCol >= Cols || dist[newRow, newCol] != -1)
                            continue;

                        char cell = grid[newRow][newCol];
                        if (cell == '#') continue;

                        int newKeys = currentKeys;
                        if (cell >= 'A' && cell <= 'Z' && KeyBitIndices.TryGetValue(char.ToLower(cell), out int b))
                            newKeys |= 1 << b;

                        dist[newRow, newCol] = currentDist + 1;
                        reqKeys[newRow, newCol] = newKeys;
                        queue.Enqueue(Tuple.Create(newRow, newCol));

                        if (cell >= 'a' && cell <= 'z' && KeyBitIndices.TryGetValue(cell, out int bit))
                            Connections[i].Add(new PathInfo
                            {
                                Destination = 4 + bit,
                                Steps = dist[newRow, newCol],
                                RequiredKeys = newKeys
                            });
                    }
                }
            }
        }

        static int SolveMaze(List<List<char>> grid)
        {
            Rows = grid.Count;
            Cols = grid[0].Count;

            ParseGrid(grid);

            int keyCount = KeyBitIndices.Count;
            AllKeysMask = (1 << keyCount) - 1;
            TotalPointCount = 4 + keyCount;

            SetupPoints();
            BuildConnectionGraph(grid);
            ComputeAllDistances();

            return FindShortestPath();
        }
        
        private static List<List<char>> ReadGrid()
        {
            var grid = new List<List<char>>();
            string line;
            while ((line = Console.ReadLine()) != null && line.Trim() != "")
                grid.Add(new List<char>(line.ToCharArray()));
            return grid;
        }
        
        static void Main(string[] args)
        {
            var grid = ReadGrid();
            int result = SolveMaze(grid);
            Console.WriteLine(result == -1 ? "No solution exists" : result.ToString());
        }
    }
}