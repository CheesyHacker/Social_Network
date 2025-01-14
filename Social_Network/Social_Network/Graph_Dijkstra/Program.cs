using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics; // Add this namespace

class SocialNetwork
{
    //list of edges with weights
    LinkedList<Tuple<int, int>>[] adjacencyList;
    bool isWeighted;

    // constructor, accepts total users and graph type 
    public SocialNetwork(int totalUsers, bool weighted)
    {
        adjacencyList = new LinkedList<Tuple<int, int>>[totalUsers];
        isWeighted = weighted;

        //initialize adjacency list for each user
        for (int i = 0; i < adjacencyList.Length; ++i)
        {
            adjacencyList[i] = new LinkedList<Tuple<int, int>>();
        }
    }

    //add connection
    public void AddConnection(int user1, int user2, int? weight = null) //weight is nullable
    {
        if (isWeighted)
        {
            //if weighted, add the weight
            adjacencyList[user1].AddLast(new Tuple<int, int>(user2, weight.Value));
            adjacencyList[user2].AddLast(new Tuple<int, int>(user1, weight.Value));
        }
        else
        {
            // if unweighted, add without weight
            adjacencyList[user1].AddLast(new Tuple<int, int>(user2, 1));
            adjacencyList[user2].AddLast(new Tuple<int, int>(user1, 1));
        }
    }

    //get total number of users 
    public int GetTotalUsers()
    {
        return adjacencyList.Length;
    }

    // print the network structure
    public void PrintNetwork()
    {
        int userIndex = 0;
        foreach (LinkedList<Tuple<int, int>> connections in adjacencyList)
        {
            Console.Write("User[" + userIndex + "] -> ");
            foreach (var connection in connections)
                Console.Write(connection.Item1 + "(" + connection.Item2 + ") -> ");
            ++userIndex;
            Console.WriteLine("Null");
        }
    }

    //compute shortest paths
    public int[] ComputeShortestPaths(int startUser)
    {
        int totalUsers = GetTotalUsers();
        int[] shortestPathDistances = new int[totalUsers];

        //initialize all distances to infinity except for the start user which is 0
        for (int i = 0; i < totalUsers; i++)
            shortestPathDistances[i] = int.MaxValue;

        shortestPathDistances[startUser] = 0;

        //priority queue to store the users, sorted by distance 
        SortedSet<Tuple<int, int>> pq = new SortedSet<Tuple<int, int>>();

        pq.Add(new Tuple<int, int>(0, startUser));

        // Loop until the priority queue is empty
        while (pq.Count > 0)
        {
            //get user with the smallest known distance
            var current = pq.Min;

            // Remove the user
            pq.Remove(current);

            //extract user ID and its current distance 
            int currentUser = current.Item2;
            int currentDistance = current.Item1;

            // skip if this distance is larger than the previously found
            if (currentDistance > shortestPathDistances[currentUser]) continue;

            //explore each neighbor
            foreach (var neighbor in adjacencyList[currentUser])
            {
                //extract the neighbor's user ID and the weight between currentUser and neighbor
                int neighborUser = neighbor.Item1;
                int edgeWeight = neighbor.Item2;

                // Calculate the new distance to the neighbor via the current user
                int newDist = currentDistance + edgeWeight;

                //if shorter, update it and add the neighbor to queue
                if (newDist < shortestPathDistances[neighborUser])
                {
                    pq.Remove(new Tuple<int, int>(shortestPathDistances[neighborUser], neighborUser));
                    shortestPathDistances[neighborUser] = newDist;
                    pq.Add(new Tuple<int, int>(newDist, neighborUser));
                }
            }
        }

        return shortestPathDistances;
    }

    //calculate the influence score based on the shortest paths
    public double CalculateInfluenceScore(int[] distances)
    {
        int totalUsers = GetTotalUsers();
        int totalDistance = 0;
        int reachableUsers = 0;

        foreach (int distance in distances)
        {
            if (distance < int.MaxValue && distance > 0) //ignore unreachable users and self-loops
            {
                totalDistance += distance;
                reachableUsers++;
            }
        }

        if (reachableUsers == 0)
        {
            return 0.0;
        }

        return (double)(totalUsers - 1) / totalDistance;
    }
}

public class SocialNetworkMain
{
    public static void Main()
    {
        string fileName = "C:\\Users\\nurgi\\Downloads\\road-euroroad (1)\\road-euroroad.edges";

        if (!File.Exists(fileName))
        {
            Console.WriteLine("File not found.");
            return;
        }

        StreamReader reader = new StreamReader(File.OpenRead(fileName));
        List<Tuple<int, int, int?>> edges = new List<Tuple<int, int, int?>>(); // list of edges with weights
        int maxUserId = -1;
        bool isWeighted = false;

        //read metadata
        string firstLine = reader.ReadLine();
        string[] firstLineParts = firstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (firstLineParts.Length == 3)
        {
            isWeighted = firstLineParts[2].Equals("weighted", StringComparison.OrdinalIgnoreCase);
        }

        //read the connections 
        while (!reader.EndOfStream)
        {
            string line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) continue; // Skip empty lines
            string[] connection = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (connection.Length != 2 && connection.Length != 3) continue; // Skip malformed lines

            int user1 = Convert.ToInt32(connection[0]);
            int user2 = Convert.ToInt32(connection[1]);
            int? weight = null;

            if (connection.Length == 3 && isWeighted)
            {
                weight = Convert.ToInt32(connection[2]);
            }

            // track total users
            maxUserId = Math.Max(maxUserId, user1);
            maxUserId = Math.Max(maxUserId, user2);

            edges.Add(new Tuple<int, int, int?>(user1, user2, weight));
        }
        reader.Close();

        // Create the social network
        Console.WriteLine($"Loading graph with {maxUserId + 1} users...");
        string graphType = isWeighted ? "weighted" : "unweighted";
        Console.WriteLine($"Graph type: {graphType} influence score.");

        SocialNetwork network = new SocialNetwork(maxUserId + 1, isWeighted);

        foreach (var edge in edges)
        {
            if (edge.Item3.HasValue)
            {
                network.AddConnection(edge.Item1, edge.Item2, edge.Item3.Value);
            }
            else
            {
                network.AddConnection(edge.Item1, edge.Item2);
            }
        }

        network.PrintNetwork();
        Console.WriteLine("Total number of users in the network = " + network.GetTotalUsers());

        // starting user ID for influence score calculation
        Console.WriteLine("Enter the starting user ID for influence score calculation:");
        int user = Convert.ToInt32(Console.ReadLine());

        //measure time using ticks
        Stopwatch stopwatch = Stopwatch.StartNew(); 
        int[] distances = network.ComputeShortestPaths(user);
        stopwatch.Stop(); 
        Console.WriteLine($"Time taken to compute shortest paths: {stopwatch.ElapsedTicks} ticks");

        Console.WriteLine("Shortest distances from user " + user + ":");
        for (int i = 0; i < distances.Length; i++)
        {
            if (distances[i] == int.MaxValue)
            {
                Console.WriteLine($"User {i} -> Distance: Unreachable");
            }
            else
            {
                Console.WriteLine($"User {i} -> Distance: {distances[i]}");
            }
        }

        // measure time 
        stopwatch.Restart(); 
        double influenceScore = network.CalculateInfluenceScore(distances);
        stopwatch.Stop(); 
        Console.WriteLine($"Time taken to calculate influence score: {stopwatch.ElapsedTicks} ticks");

        Console.WriteLine($"\nInfluence score for user {user} = {influenceScore:F2}");
    }
}
