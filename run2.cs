using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static readonly (int dx, int dy)[] Directions = {(-1, 0), (1, 0), (0, -1), (0, 1)};

    struct State : IComparable<State>
    {
        public (int X, int Y)[] RobotPositions;
        public int KeysMask;
        public int Steps;

        public State((int X, int Y)[] robots, int keysMask, int steps)
        {
            RobotPositions = robots;
            KeysMask = keysMask;
            Steps = steps;
        }

        public int CompareTo(State other) => Steps.CompareTo(other.Steps);
    }

    static List<List<char>> GetInput()
    {
        var data = new List<List<char>>();
        string line;
        while ((line = Console.ReadLine()) != null && line != "")
        {
            data.Add(line.ToCharArray().ToList());
        }
        return data;
    }

    static int Solve(List<List<char>> data)
    {
        int rows = data.Count;
        int cols = data[0].Count;

        var robotPositions = new (int, int)[4];
        var k = 0;
        var keyPositions = new Dictionary<char, (int, int)>();
        int totalKeys = 0;

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                char cell = data[i][j];
                if (cell == '@') robotPositions[k++] = (i, j);
                else if (char.IsLower(cell))
                {
                    keyPositions[cell] = (i, j);
                    totalKeys++;
                }
            }
        }

        if (totalKeys == 0) return 0;

        var initialState = new State(robotPositions, 0, 0);

        var queue = new Queue<State>();
        queue.Enqueue(initialState);
        var visited = new HashSet<(string, int)>();

        while (queue.Count > 0)
        {
            var currentState = queue.Dequeue();

            if (currentState.KeysMask == (1 << totalKeys) - 1)
                return currentState.Steps;

            var stateKey = (string.Join(",", currentState.RobotPositions.Select(p => $"{p.X},{p.Y}")), currentState.KeysMask);
            if (!visited.Add(stateKey))
                continue;

            for (int robotIndex = 0; robotIndex < currentState.RobotPositions.Length; robotIndex++)
            {
                var (x, y) = currentState.RobotPositions[robotIndex];

                foreach (var dir in Directions)
                {
                    int nx = x + dir.dx;
                    int ny = y + dir.dy;

                    if (nx < 0 || nx >= rows || ny < 0 || ny >= cols || data[nx][ny] == '#')
                        continue;

                    char cell = data[nx][ny];

                    if (char.IsUpper(cell) && (currentState.KeysMask & (1 << (char.ToLower(cell) - 'a'))) == 0)
                        continue;

                    var newRobots = currentState.RobotPositions.ToArray();
                    newRobots[robotIndex] = (nx, ny);

                    int newKeysMask = currentState.KeysMask;
                    if (char.IsLower(cell))
                        newKeysMask |= 1 << (cell - 'a');

                    var newState = new State(newRobots, newKeysMask, currentState.Steps + 1);
                    queue.Enqueue(newState);
                }
            }
        }

        return -1;
    }

    static void Main()
    {
        var data = GetInput();
        int result = Solve(data);
        Console.WriteLine(result == -1 ? "No solution found" : result.ToString());
    }
}