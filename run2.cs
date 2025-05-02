using System;
using System.Collections.Generic;
using System.Linq;

class Robots
{
    static readonly int[] dx = { -1, 1, 0, 0 };
    static readonly int[] dy = { 0, 0, -1, 1 };

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
        var rows = data.Count;
        var cols = data[0].Count;
        
        var robotPositions = new List<(int, int)>();
        var keyPositions = new Dictionary<char, (int, int)>();
        var totalKeys = 0;

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                char cell = data[i][j];
                if (cell == '@') robotPositions.Add((i, j));
                else if (char.IsLower(cell))
                {
                    keyPositions[cell] = (i, j);
                    totalKeys++;
                }
            }
        }

        if (totalKeys == 0) return 0;

        //состояние = (позиции, маска ключей)
        var queue = new Queue<(ValueTuple<(int, int), (int, int)>[], int, int)>();
        var visited = new Dictionary<(ValueTuple<(int, int), (int, int)>[], int), bool>();

        var initialRobots = robotPositions.Select(p => new ValueTuple<(int, int), (int, int)>(p, p)).ToArray();
        queue.Enqueue((initialRobots, 0, 0));
        visited.Add((initialRobots, 0), true);

        while (queue.Count > 0)
        {
            var (robots, keysMask, steps) = queue.Dequeue();

            if (keysMask == (1 << totalKeys) - 1)
                return steps;

            for (int robotIndex = 0; robotIndex < robots.Length; robotIndex++)
            {
                var (currentPos, _) = robots[robotIndex];

                foreach (var dir in Enumerable.Range(0, 4))
                {
                    int nx = currentPos.Item1 + dx[dir];
                    int ny = currentPos.Item2 + dy[dir];

                    if (nx < 0 || nx >= rows || ny < 0 || ny >= cols || data[nx][ny] == '#')
                        continue;

                    char cell = data[nx][ny];

                    if (char.IsUpper(cell) && (keysMask & (1 << (char.ToLower(cell) - 'a'))) == 0)
                        continue;

                    var newRobots = robots.ToArray();
                    newRobots[robotIndex] = (currentPos, (nx, ny));

                    int newKeysMask = keysMask;
                    if (char.IsLower(cell))
                        newKeysMask |= 1 << (cell - 'a');

                    var stateKey = (newRobots, newKeysMask);
                    if (!visited.ContainsKey(stateKey))
                    {
                        visited[stateKey] = true;
                        queue.Enqueue((newRobots, newKeysMask, steps + 1));
                    }
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