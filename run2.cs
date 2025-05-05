using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static readonly char[] keys_char = Enumerable.Range('a', 26).Select(i => (char)i).ToArray();
    static readonly char[] doors_char = keys_char.Select(char.ToUpper).ToArray();

    static int[][] directions = new int[][]
        { new int[] { -1, 0 }, new int[] { 1, 0 }, new int[] { 0, -1 }, new int[] { 0, 1 } };

    class State : IComparable<State>
    {
        public int[] positions;
        public int keyMask;
        public int g;
        public int h;
        public int f;

        public State(int[] positions, int keyMask, int g, int h)
        {
            this.positions = positions;
            this.keyMask = keyMask;
            this.g = g;
            this.h = h;
            this.f = g + h;
        }

        public int CompareTo(State other)
        {
            return f.CompareTo(other.f);
        }
    }

    static List<List<char>> GetInput()
    {
        var data = new List<List<char>>();
        string line;
        while ((line = Console.ReadLine()) != null && line.Trim() != "")
        {
            data.Add(line.ToCharArray().ToList());
        }

        return data;
    }

    static int Solve(List<List<char>> data)
    {
        int rows = data.Count;
        int cols = data[0].Count;

        List<Tuple<int, int>> points = new List<Tuple<int, int>>();
        Dictionary<char, int> keyIndex = new Dictionary<char, int>();
        List<Tuple<char, int, int>> keysRaw = new List<Tuple<char, int, int>>();

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                if (data[i][j] == '@') points.Add(Tuple.Create(i, j));
                else if (Array.IndexOf(keys_char, data[i][j]) >= 0) keysRaw.Add(Tuple.Create(data[i][j], i, j));
            }
        }

        keysRaw.Sort((a, b) => a.Item1.CompareTo(b.Item1));
        foreach (var key in keysRaw)
        {
            points.Add(Tuple.Create(key.Item2, key.Item3));
            keyIndex[key.Item1] = keyIndex.Count;
        }

        int pointCount = points.Count;
        int robotCount = 4;
        int keyCount = keyIndex.Count;
        int fullKeyMask = (1 << keyCount) - 1;

        List<Dictionary<int, Tuple<int, int>>> graph = new List<Dictionary<int, Tuple<int, int>>>();
        for (int p = 0; p < pointCount; p++)
        {
            var visited = new bool[rows, cols];
            var queue = new Queue<Tuple<int, int, int, int>>();
            var result = new Dictionary<int, Tuple<int, int>>();
            var start = points[p];
            queue.Enqueue(Tuple.Create(start.Item1, start.Item2, 0, 0));
            visited[start.Item1, start.Item2] = true;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                int r = current.Item1;
                int c = current.Item2;
                int dist = current.Item3;
                int reqKeys = current.Item4;
                char cell = data[r][c];
                if (Array.IndexOf(keys_char, cell) >= 0) result[keyIndex[cell]] = Tuple.Create(dist, reqKeys);

                foreach (var dir in directions)
                {
                    int nr = r + dir[0];
                    int nc = c + dir[1];
                    if (nr < 0 || nr >= rows || nc < 0 || nc >= cols) continue;
                    char nextCell = data[nr][nc];
                    if (nextCell == '#') continue;

                    if (Array.IndexOf(doors_char, nextCell) >= 0)
                    {
                        int doorIdx = Array.IndexOf(doors_char, nextCell);
                        char keyChar = keys_char[doorIdx];
                        int keyBit = keyIndex[keyChar];
                        queue.Enqueue(Tuple.Create(nr, nc, dist + 1, reqKeys | (1 << keyBit)));
                        continue;
                    }

                    if (!visited[nr, nc])
                    {
                        visited[nr, nc] = true;
                        queue.Enqueue(Tuple.Create(nr, nc, dist + 1, reqKeys));
                    }
                }
            }

            graph.Add(result);
        }

        int[][] minSteps = new int[pointCount][];
        for (int i = 0; i < pointCount; i++)
        {
            minSteps[i] = new int[pointCount];
            for (int j = 0; j < pointCount; j++) minSteps[i][j] = int.MaxValue;
            minSteps[i][i] = 0;
            foreach (var kvp in graph[i]) minSteps[i][kvp.Key + robotCount] = kvp.Value.Item1;
        }

        for (int k = 0; k < pointCount; k++)
        {
            for (int i = 0; i < pointCount; i++)
            {
                for (int j = 0; j < pointCount; j++)
                {
                    if (minSteps[i][k] != int.MaxValue && minSteps[k][j] != int.MaxValue &&
                        minSteps[i][j] > minSteps[i][k] + minSteps[k][j])
                    {
                        minSteps[i][j] = minSteps[i][k] + minSteps[k][j];
                    }
                }
            }
        }

        var initialState = new State(Enumerable.Range(0, robotCount).ToArray(), 0, 0, 0);
        var priorityQueue = new SortedSet<Tuple<int, int, int, string>>(Comparer<Tuple<int, int, int, string>>.Create(
            (a, b) =>
            {
                int cmp = a.Item1.CompareTo(b.Item1);
                if (cmp != 0) return cmp;
                cmp = a.Item2.CompareTo(b.Item2);
                if (cmp != 0) return cmp;
                cmp = a.Item3.CompareTo(b.Item3);
                if (cmp != 0) return cmp;
                return a.Item4.CompareTo(b.Item4);
            }));
        priorityQueue.Add(Tuple.Create(initialState.f, initialState.g, initialState.keyMask,
            GetPosKey(initialState.positions)));
        HashSet<string> visitedStates = new HashSet<string>();

        while (priorityQueue.Count > 0)
        {
            var current = priorityQueue.Min;
            priorityQueue.Remove(current);

            int[] currPositions = current.Item4.Split(',').Select(int.Parse).ToArray();
            int currKeyMask = current.Item3;
            int currG = current.Item2;

            string stateKey = currKeyMask + ":" + current.Item4;
            if (visitedStates.Contains(stateKey)) continue;
            visitedStates.Add(stateKey);

            if (currKeyMask == fullKeyMask) return currG;

            for (int robot = 0; robot < robotCount; robot++)
            {
                int currPos = currPositions[robot];
                foreach (var kvp in graph[currPos])
                {
                    int keyId = kvp.Key;
                    var value = kvp.Value;
                    int distance = value.Item1;
                    int reqKeys = value.Item2;

                    if ((currKeyMask & reqKeys) != reqKeys) continue;
                    if ((currKeyMask & (1 << keyId)) != 0) continue;

                    int[] newPositions = (int[])currPositions.Clone();
                    newPositions[robot] = keyId + robotCount;
                    int newKeyMask = currKeyMask | (1 << keyId);
                    int newH = CalculateHeuristic(graph, newPositions, newKeyMask, keyCount, minSteps);
                    int newG = currG + distance;

                    string newPosKey = GetPosKey(newPositions);
                    string newStateKey = newKeyMask + ":" + newPosKey;

                    if (!visitedStates.Contains(newStateKey))
                    {
                        priorityQueue.Add(Tuple.Create(newG + newH, newG, newKeyMask, newPosKey));
                    }
                }
            }
        }

        return -1;
    }

    static int CalculateHeuristic(
        List<Dictionary<int, Tuple<int, int>>> graph,
        int[] positions,
        int keyMask,
        int keyCount,
        int[][] minSteps)
    {
        int maxMinDistance = 0;

        for (int keyId = 0; keyId < keyCount; keyId++)
        {
            if ((keyMask & (1 << keyId)) != 0) continue;

            int minDist = int.MaxValue;
            foreach (int pos in positions)
            {
                if (minSteps[pos][keyId + 4] != int.MaxValue)
                {
                    minDist = Math.Min(minDist, minSteps[pos][keyId + 4]);
                }
            }

            if (minDist != int.MaxValue) maxMinDistance = Math.Max(maxMinDistance, minDist);
            else return int.MaxValue;
        }

        return maxMinDistance;
    }

    static string GetPosKey(int[] positions)
    {
        return string.Join(",", positions);
    }

    static void Main()
    {
        var data = GetInput();
        int result = Solve(data);
        Console.WriteLine(result == -1 ? "No solution found" : result.ToString());
    }
}