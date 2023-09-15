using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Commons;
using System.Threading.Tasks;
using System.Threading;

/*
 * TODO:
 * It appears some nodes and nodepairs that exist in the route sequences are missing in nodes.bin and nodepairs.bin. 
 * I believe this was due to bug that let the routing program close before everything was written to disk.
 * The solution would be to re-run the routing program. Do that, after I've shown that the compressor works.
 * */


namespace CompressNodeSequences
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] nodeSeqPaths = Enumerable.Range(0, 20).Select(n => Paths.RootPaths.GeneratedDataRoot + "nodesequences.bin.part_" + n).ToArray();

            Func<string, IEnumerable<(long fromNodeID, long toNodeID, float distance_m, float speed_kmph)>> getNodePairs = DataReader.ReadNodePairs;
            Func<string[], IEnumerable<(int routeID, int variantNumber, long[] nodeSequence)>> getNodeSequences = DataReader.ReadNodeSequences;

            Console.Write("Calculating degree of each node...");
            Dictionary<long, HashSet<long>> nodeNeighbors = new Dictionary<long, HashSet<long>>();
            int pairCount = 0;
            foreach (var pair in getNodePairs(Paths.NodePairs))
            {
                if (!nodeNeighbors.TryAdd(pair.fromNodeID, new HashSet<long>() { pair.toNodeID }))
                    nodeNeighbors[pair.fromNodeID].Add(pair.toNodeID);
                if (!nodeNeighbors.TryAdd(pair.toNodeID, new HashSet<long>() { pair.fromNodeID }))
                    nodeNeighbors[pair.toNodeID].Add(pair.fromNodeID);
                pairCount++;
            }
            var nodeDegree = nodeNeighbors.ToDictionary(n => n.Key, m => m.Value.Count);
            Console.WriteLine(" done!");


            Console.Write("Splitting graph...");
            HashSet<long> intersectionNodeIDs = nodeDegree.Where(n => n.Value > 2 || n.Value == 1).Select(n => n.Key).ToHashSet();
            Console.WriteLine(" done!");


            Console.Write("Assigning cluster IDs to connected components...");
            int nextClusterID = 0;
            var pairToCluster = new Dictionary<(long fromNodeID, long toNodeID), int>(pairCount);
            var clusterToPairs = new Dictionary<int, List<(long fromNodeID, long toNodeID, float distance_m, float speed_kmph)>>();
            var nodeToCluster = new Dictionary<long, int>(pairCount);
            var clusterToNodes = new Dictionary<int, List<long>>();

            //Each leg between two intersections should become one cluster
            //A cluster should consist of node pairs, not nodes
            //Both directions of a node pair may appear

            int progressCounter = 0;
            HashSet<long> nodeDirectionPositive = new HashSet<long>();
            HashSet<long> nodeDirectionNegative = new HashSet<long>();
            foreach (var pair in getNodePairs(Paths.NodePairs))
            {
                (long fromNodeID, long toNodeID, float distance_m, float speed_kmph) orderedPair = pair.fromNodeID < pair.toNodeID ? 
                    (pair.fromNodeID, pair.toNodeID, pair.distance_m, pair.speed_kmph) : (pair.toNodeID, pair.fromNodeID, pair.distance_m, pair.speed_kmph);
                if (pair.fromNodeID < pair.toNodeID)
                    nodeDirectionPositive.Add(pair.fromNodeID);
                else
                    nodeDirectionNegative.Add(pair.fromNodeID);

                if (pairToCluster.ContainsKey((orderedPair.fromNodeID, orderedPair.toNodeID)))
                    continue;

                bool fromIsInternal = !intersectionNodeIDs.Contains(pair.fromNodeID);
                bool toIsInternal = !intersectionNodeIDs.Contains(pair.toNodeID);

                if (!fromIsInternal && !toIsInternal)
                {
                    int clusterID = nextClusterID++;
                    clusterToPairs.Add(clusterID, new List<(long, long, float, float)>() { orderedPair });
                    pairToCluster[(orderedPair.fromNodeID, orderedPair.toNodeID)] = clusterID;
                }
                else
                {
                    int fromClusterID = -1, toClusterID = -1;
                    bool fromExists = fromIsInternal && nodeToCluster.TryGetValue(pair.fromNodeID, out fromClusterID);
                    bool toExists = toIsInternal && nodeToCluster.TryGetValue(pair.toNodeID, out toClusterID);

                    if (!fromExists && !toExists) //new cluster
                    {
                        int clusterID = nextClusterID++;
                        nodeToCluster[pair.fromNodeID] = clusterID;
                        nodeToCluster[pair.toNodeID] = clusterID;
                        pairToCluster[(orderedPair.fromNodeID, orderedPair.toNodeID)] = clusterID;
                        clusterToNodes.Add(clusterID, new List<long>() { pair.fromNodeID, pair.toNodeID });
                        clusterToPairs.Add(clusterID, new List<(long, long, float, float)>() { orderedPair });
                    }
                    else if (fromExists && toExists) //merge clusters
                    {
                        var nodesToReassign = clusterToNodes[toClusterID];
                        clusterToNodes.Remove(toClusterID);
                        var pairsToReassign = clusterToPairs[toClusterID];
                        clusterToPairs.Remove(toClusterID);

                        clusterToNodes[fromClusterID].AddRange(nodesToReassign);
                        clusterToPairs[fromClusterID].AddRange(pairsToReassign);
                        foreach (var node in nodesToReassign)
                            nodeToCluster[node] = fromClusterID;
                        foreach (var p in pairsToReassign)
                            pairToCluster[(p.fromNodeID, p.toNodeID)] = fromClusterID;

                        pairToCluster[(orderedPair.fromNodeID, orderedPair.toNodeID)] = fromClusterID;
                        clusterToPairs[fromClusterID].Add(orderedPair);
                    }
                    else if (fromExists) //add to from-cluster
                    {
                        clusterToPairs[fromClusterID].Add(orderedPair);
                        clusterToNodes[fromClusterID].Add(pair.toNodeID);
                        nodeToCluster[pair.toNodeID] = fromClusterID;
                        pairToCluster[(orderedPair.fromNodeID, orderedPair.toNodeID)] = fromClusterID;
                    }
                    else if (toExists) //add to from-cluster
                    {
                        clusterToPairs[toClusterID].Add(orderedPair);
                        clusterToNodes[toClusterID].Add(pair.fromNodeID);
                        nodeToCluster[pair.fromNodeID] = toClusterID;
                        pairToCluster[(orderedPair.fromNodeID, orderedPair.toNodeID)] = toClusterID;
                    }
                    else
                        throw new NotImplementedException();
                }

                progressCounter++;
                if (progressCounter % 100000 == 0)
                    Console.WriteLine(progressCounter);
            }
            Console.WriteLine(" done!");

            
            Console.Write("Writing node directionality to disk...");
            var bidirectionalNodes = nodeDirectionNegative.Intersect(nodeDirectionPositive);
            File.WriteAllText(Paths.BidirectionalNodes, string.Join("\n", bidirectionalNodes));
            Console.WriteLine(" done!");


            Console.Write("Writing clusters to disk...");
            AsyncBinaryWriter writer = new AsyncBinaryWriter(Paths.ClusterNodePairs);
            var task = Task.Run(writer.WriteAsync);
            progressCounter = 0;
            writer.Push("clusterID\tfromNodeID\ttoNodeID\tdistance_m\tspeed_kmph\tnodePairs");
            Parallel.ForEach (clusterToPairs, cluster => 
            {
                float distance_m = cluster.Value.Sum(n => n.distance_m);
                float speed_kmph = cluster.Value.Sum(n => n.distance_m) / cluster.Value.Sum(n => n.distance_m / n.speed_kmph);
                long[] endpoints = FindEndpoints(cluster.Value);

                writer.Push(
                    cluster.Key,
                    endpoints[0],
                    endpoints[1],
                    distance_m,
                    speed_kmph,
                    cluster.Value.Select(n => (n.fromNodeID, n.toNodeID)));

                Interlocked.Increment(ref progressCounter);
                if (progressCounter % 100000 == 0)
                    Console.Write(" " + progressCounter);
            });
            writer.Stop();
            task.Wait();
            Console.WriteLine(" done!");

            
            Console.Write("Writing cluster assignments to disk...");
            writer = new AsyncBinaryWriter(Paths.NodePairsClusters);
            task = Task.Run(writer.WriteAsync);
            writer.Push("fromNodeID\ttoNodeID\tclusterID");
            foreach (var pair in pairToCluster)
                writer.Push(pair.Key.fromNodeID, pair.Key.toNodeID, pair.Value);
            writer.Stop();
            task.Wait();
            Console.WriteLine(" done!");


            // compress movement sequences
            Console.Write("Reducing node sequences...");
            writer = new AsyncBinaryWriter(Paths.ClusterSequences);
            task = Task.Run(writer.WriteAsync);
            progressCounter = 0;
            Parallel.ForEach(getNodeSequences(nodeSeqPaths), seq =>
            {
                int prevClusterID = int.MaxValue;
                List<int> clusterSeq = new List<int>();
                for (int i = 1; i < seq.nodeSequence.Length; i++)
                {
                    var s = seq.nodeSequence;
                    (long, long) orderedPair = s[i] < s[i - 1] ? (s[i], s[i - 1]) : (s[i - 1], s[i]);
                    int clusterID = int.MinValue;
                    pairToCluster.TryGetValue(orderedPair, out clusterID);
                    if (clusterID != prevClusterID)
                    {
                        clusterSeq.Add(clusterID);
                        prevClusterID = clusterID;
                    }
                }
                writer.Push(seq.routeID, seq.variantNumber, clusterSeq);

                int val = Interlocked.Increment(ref progressCounter);
                if (val % 100000 == 0)
                    Console.Write(" " + progressCounter);
            });
            writer.Stop();
            task.Wait();

            GenerateRouteLengthClassCSV();

            Console.WriteLine(" done!");
        }

        static long[] FindEndpoints(List<(long fromNodeID, long toNodeID, float distance_m, float speed_kmph)> nodePairs)
        {
            //var endpoints = nodePairs.SelectMany(n => new long[] { n.toNodeID, n.fromNodeID }).Distinct().Intersect(intersectionNodeIDs).OrderBy(n => n).ToArray();
            //if (endpoints.Length == 2)
            //    return endpoints;

            var endpoints = nodePairs
                .SelectMany(n => new long[] { n.toNodeID, n.fromNodeID })
                .GroupBy(n => n)
                .Where(n => n.Count() == 1)
                .Select(n => n.Key)
                .OrderBy(n => n)
                .ToArray();

            if (endpoints.Length == 2)
                return endpoints;
            else if (endpoints.Length == 0) //A loop, connected at zero or one node with the rest of the graph. Just take two nodes.
                return new long[] { nodePairs.First().fromNodeID, nodePairs.Last().toNodeID };

            throw new ArgumentException("Road stretch cannot have " + endpoints.Length + " endpoints. " + string.Join(',', nodePairs.Select(n => (n.fromNodeID, n.toNodeID))));
        }

        private static void GenerateRouteLengthClassCSV()
        {
            Console.WriteLine("Generating RouteLengthClass CSV...");

            HashSet<long>
                bidirectionalNodes = DataReader.ReadBidirectionalNodes(Paths.BidirectionalNodes).ToHashSet();
            Dictionary<long, (double lat, double lon, bool bidirectional)>
                nodes = DataReader.ReadNodes(Paths.Nodes).ToDictionary(n => n.nodeID, m => (m.latitude, m.longitude, bidirectionalNodes.Contains(m.nodeID)));

            var bins = new int[] { 1, 10, 20, 50, 100, 200, 500, 10000 };
            string[] nodeSeqPaths = Enumerable.Range(0, 20).Select(n => Paths.RootPaths.GeneratedDataRoot + "nodesequences.bin.part_" + n).ToArray();

            using (StreamWriter file = new StreamWriter(Paths.RouteLengthClass, append: false))
            {
                file.WriteLine("route_id\tlength_class");
                HashSet<int> seenRoutes = new HashSet<int>();
                foreach (var route in DataReader.ReadNodeSequences(nodeSeqPaths))
                {
                    if (!seenRoutes.Add(route.routeID))
                        continue;

                    int binCount = route.nodeSequence.Select(nodeID =>
                    {
                        var node = nodes[nodeID];
                        return new CoordinateHash(node.lat, node.lon).Index;
                    }).Distinct().Count();

                    int routeLengthClass = bins.Where(n => n <= binCount).Max();

                    file.WriteLine(route.routeID + "\t" + routeLengthClass);
                }
            }
        }


    }
}
