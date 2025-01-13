using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Commons;

public class EtisPlus
{
    public class CountryBiaser
    {
        private Dictionary<string, int> _segmentCounts = new Dictionary<string, int>();
        private Dictionary<string, double> _countryPriority = new Dictionary<string, double>();

        public static CountryBiaser Instance { get; } = new CountryBiaser();

        public void AddCountry(string country, int segmentCount)
        {
            _segmentCounts[country] = segmentCount;
            _countryPriority[country] = 1;
        }

        public void Pick(string country)
        {
            _countryPriority[country] = 1e-8;
            foreach (var cn in _segmentCounts)
            {
                _countryPriority[cn.Key] += cn.Value / 1000.0;
            }
        }

        public double GetPriority(string country)
        { 
            return _countryPriority[country]; 
        }
    }

    class Node
    {
        public Node(int id, string countryCode)
        {
            this.ID = id;
            this.CountryCode = countryCode;
            _currentValue = ID / 1000000f; //This is only to break ties while sorting later
        }

        public int ID { get; }
        public string CountryCode { get; }
        public Dictionary<Node, Edge> Edges { get; } = new Dictionary<Node, Edge>();
        public float WeightSum { get; private set; }
        float _currentValue = 0;
        public float CurrentValue { get { return (float)CountryBiaser.Instance.GetPriority(CountryCode) * _currentValue * (float)Math.Log(WeightSum); } }
        public bool IsPicked { get; private set; }

        public void AddEdge(Node other, float weight)
        {
            Node a = this, b = other;
            if (a.ID > b.ID)
                (a, b) = (b, a);
            Edge e = new Edge() { A = a, B = b, Weight = weight };
            Edges.Add(other, e);
            other.Edges.Add(this, e);
            WeightSum += weight;
        }

        public void Pick()
        {
            if (IsPicked)
                throw new Exception();
            IsPicked = true;
            foreach (var edge in Edges)
            {
                Node other = edge.Key;
                if (other.IsPicked)
                    continue;
                other._currentValue += edge.Value.Weight;
            }
            _currentValue = -ID;

            CountryBiaser.Instance.Pick(CountryCode);
        }

        public override string ToString()
        {
            return "Node" + ID;
        }
    }

    class Edge
    {
        public Node A { get; set; }
        public Node B { get; set; }
        public float Weight { get; set; }

        public override string ToString()
        {
            return "(" + A + "\t" + B + ")";
        }
    }
    static IEnumerable<(int routeID, int[] clusterSequence, float annualMovements, float annualTonnes)> ReadRoutes(double sampleRatio = 1)
    {
        Regex regex = new Regex(@"(?<=,|^)(?:\"".*?\""|[^,]+)", RegexOptions.Compiled); 
        
        using (StreamReader r = new StreamReader(File.OpenRead(@"C:\data\etisplus\01_Trucktrafficflow.csv")))
        {
            Random random = new Random(0);

            //(1) ID origin region, (2) name origin region, (3) ID destination region, (4) name destination region, (5) shortest path in the modeled E-road network,
            //(6) distance from origin region to the E-road network, (7) distance within the E-road network, (8) distance from the E-road network to the destination region,
            //(9) total distance, (10) road freight flow in tons for 2010, (11) road freight flow in tons for 2019, (12) road freight flow in tons for 2030,
            //(13) truck traffic flow in number of vehicles for 2010, (14) truck traffic flow in number of vehicles for 2019, (15) truck traffic flow in number of vehicles for 2030
            string? line = r.ReadLine();

            while ((line = r.ReadLine()) != null)
            {
                if (random.NextDouble() > sampleRatio)
                    continue;
                var matches = regex.Matches(line);
                if (matches[5].Value == "[]")
                    continue;
                yield return (
                    routeID: int.Parse(matches[0].Value),
                    clusterSequence: matches[5].Value.Trim('"').Trim('[').Trim(']').Split(',').Select(n => int.Parse(n)).ToArray(),
                    annualMovements: float.Parse(matches[15].Value),
                    annualTonnes: float.Parse(matches[12].Value)
                );
            }
        }
    }

    public static Dictionary<int, (CoordinateHash coordinate, string countryCode)> ReadNodes()
    {
        var nodes = new Dictionary<int, (CoordinateHash, string)>();
        using (StreamReader r = new StreamReader(File.OpenRead(@"C:\data\etisplus\03_network-nodes.csv")))
        {
            //(1) node ID, (2) longitude of the location, (3) latitude of the location, (4) ID of the corresponding NUTS-3 region, (5) country code
            string? line = r.ReadLine();

            while ((line = r.ReadLine()) != null)
            {
                var fields = line.Split(',');
                nodes.Add(int.Parse(fields[1]), (new CoordinateHash(double.Parse(fields[3]), double.Parse(fields[2])), fields[5]));
            }
        }

        return nodes;
    }

    public static Dictionary<int, (CoordinateHash from, CoordinateHash to, bool bidirectional, float distance_m, float speed_kmph, float annualMovements, string countryCode)> ReadClusters()
    {
        Regex regex = new Regex(@"(?<=,|^)(?:\"".*?\""|[^,]+)", RegexOptions.Compiled);

        var nodes = ReadNodes();

        var clusters = new Dictionary<int, (CoordinateHash from, CoordinateHash to, bool bidirectional, float distance_m, float speed_kmph, float annualMovements, string countryCode)>();

        using (StreamReader r = new StreamReader(File.OpenRead(@"C:\data\etisplus\04_network-edges.csv")))
        {
            //(1) edge ID, (2) information whether the edge is manually added or part of the original ETISplus dataset, (3) length of the edge,
            //(4) ID endpoint A, (5) ID endpoint B, (6) number of trucks in 2019 (both directions), (7) number of  trucks in 2030 (both directions)
            string? line = r.ReadLine();

            while ((line = r.ReadLine()) != null)
            {
                var fields = line.Split(',');
                //For some reason, the dataset contains multiple edges with the same ID. Sometimes duplicates, but sometimes linking different nodes.
                bool success = clusters.TryAdd(int.Parse(fields[1]),
                    (
                    from: nodes[int.Parse(fields[4])].coordinate,
                    to: nodes[int.Parse(fields[5])].coordinate,
                    bidirectional: true,
                    distance_m: float.Parse(fields[3]) * 1000,
                    speed_kmph: 80f,
                    annualMovements: float.Parse(fields[7]),
                    countryCode: nodes[int.Parse(fields[4])].countryCode
                ));

                if (!success)
                    Console.WriteLine(fields[1]);
            }
        }

        return clusters;
    }

    public static void CalculateErsRolloutOrder(List<int> startNodeIDs)
    {
        var routes = ReadRoutes(1);

        Console.WriteLine(1);

        var clusters = ReadClusters();

        foreach (var c in clusters.GroupBy(n => n.Value.countryCode).Select(g => (name: g.Key, clusterCount: g.Count())))
            CountryBiaser.Instance.AddCountry(c.name, c.clusterCount);

        //For each route, get the total number of annual vehicle passages
        //BUG: Route traffic counts should be distributed across variants. The importance of logistics hubs is effectively downweighted by 10 due to this bug.
        Dictionary<int, float> routeMovementCount = routes.ToDictionary(n => n.routeID, n => n.annualMovements);

        var clusterAssociations = new ConcurrentDictionary<int, ConcurrentDictionary<int, float>>();

        Console.WriteLine(2);

        int minLength = 20, minAnnualMovements = 150;

        //Build an index of all route variants that go between grid cells (origin cell to destination cell)
        var fromToIndex = new ConcurrentDictionary<CoordinateHash, ConcurrentDictionary<CoordinateHash, ConcurrentBag<(int routeID, int[] clusterSequence)>>>();
        Parallel.ForEach(routes
            .Where(n => n.clusterSequence.Length >= minLength && routeMovementCount[n.routeID] >= minAnnualMovements), seq =>
            {
                var fromCluster = clusters[seq.clusterSequence.First()];
                var toCluster = clusters[seq.clusterSequence.Last()];
                var fromHash = fromCluster.from;
                var toHash = toCluster.to;
                var fromDict = fromToIndex.GetOrAdd(fromHash, _ => new ConcurrentDictionary<CoordinateHash, ConcurrentBag<(int, int[])>>());
                var toBag = fromDict.GetOrAdd(toHash, _ => new ConcurrentBag<(int, int[])>());
                toBag.Add((seq.routeID, seq.clusterSequence));
            });

        Console.WriteLine(2);

        int counter = 0;
        //For each place of origin
        Parallel.ForEach(fromToIndex, fromItem =>
        {
            Random rand = new Random(fromItem.Key.Index);
            var fromBin = fromItem.Key;
            var toBins = fromItem.Value;
            //For each outgoing route variant, (place: route variant) tuples
            foreach (var (toBin, seq) in toBins.SelectMany(n => n.Value.Select(m => (n.Key, m))))
            {
                //Make a set of all the segments traversed by this route
                SortedList<int, int> clusterIDs = new SortedList<int, int>();
                foreach (var id in seq.clusterSequence)
                    clusterIDs.TryAdd(rand.Next(), id);

                //If possible, get one random route pointing in the opposite direction. Add those segments to the set, with repetition.
                if (fromToIndex.TryGetValue(toBin, out var toToBin))
                {
                    if (toToBin.TryGetValue(fromBin, out var toFromSeqs))
                    {
                        var oppositeSeq = toFromSeqs.ElementAt((int)(rand.NextDouble() * toFromSeqs.Count()));
                        foreach (var id in oppositeSeq.clusterSequence)
                            clusterIDs.TryAdd(rand.Next(), id);
                    }
                }

                //Take a random sample of the segments in the set. Remove repeating items from the sample.
                var clusterIDSample = clusterIDs.Values.Take(minLength).Distinct().ToArray();

                //For each pair of segments in the sample, increment the count of shared annual vehicle passages
                var movements = routeMovementCount[seq.routeID];
                for (int i = 0; i < clusterIDSample.Length - 1; i++)
                {
                    int x = clusterIDSample[i];
                    for (int j = i + 1; j < clusterIDSample.Length; j++)
                    {
                        int a = x;
                        int b = clusterIDSample[j];
                        if (a == b)
                            continue;
                        if (a > b)
                            (a, b) = (b, a);
                        var a_bins = clusterAssociations.GetOrAdd(a, _ => new ConcurrentDictionary<int, float>());
                        a_bins.AddOrUpdate(b, movements, (k, old) => old + movements);
                    }
                }

                Interlocked.Increment(ref counter);

                //After every 10k pair of bins, forget all pairwise associations that have been observed less than 1/1000th as often as the most frequent pair
                lock (clusterAssociations)
                {
                    if (counter > 10000)
                    {
                        float threshold = clusterAssociations.SelectMany(n => n.Value.Select(m => m.Value)).Max() / 1000;
                        List<int> pruneSet2 = new List<int>();
                        foreach (var a in clusterAssociations.Keys)
                        {
                            var set = clusterAssociations[a];
                            var pruneSet = clusterAssociations[a].Where(n => n.Value < threshold).Select(n => n.Key).ToArray();
                            foreach (var b in pruneSet)
                            {
                                set.Remove(b, out float _);
                            }
                            if (set.Count == 0)
                                pruneSet2.Add(a);
                        }
                        foreach (var a in pruneSet2)
                        {
                            clusterAssociations.Remove(a, out ConcurrentDictionary<int, float> _);
                        }
                        GC.Collect();

                        counter = 0;
                        Console.WriteLine("Pruned" + pruneSet2.Count);
                    }
                }
            }
        });

        Console.WriteLine(3);

        //Discard all associations with support < 2000 (common vehicle passages per year)
        //For each segment in the set of associations, select the most strongly associated 100 segments with greater segment ID
        List<(int A, int B, float Weight)> tuples = clusterAssociations.SelectMany(
            n => n.Value
                .Where(m => m.Value > 2000) //At least x annualMovements in common
                .OrderByDescending(m => m.Value)
                .Take(100) //Keep only the 100 strongest edges for each node
                .Select(m => (n.Key, m.Key, m.Value))
            ).ToList();

        Console.WriteLine(4);

        //Build a node-edge representation of the resulting graph (a graph of cluster similarities), to be colored in order of construction
        //Edges have a fixed weight, which is the strength of association between nodes
        //Nodes have two properties:
        //  1) EdgeWeightSum
        //  2) CurrentValue = the sum of EdgeWeightSum of all already colored neighbors * ln(EdgeWeightSum)
        Dictionary<int, Node> nodes = new Dictionary<int, Node>();
        foreach (var edge in tuples)
        {
            if (!nodes.TryGetValue(edge.A, out Node a))
            {
                a = new Node(edge.A, clusters[edge.A].countryCode);
                nodes.Add(edge.A, a);
            }
            if (!nodes.TryGetValue(edge.B, out Node b))
            {
                b = new Node(edge.B, clusters[edge.B].countryCode);
                nodes.Add(edge.B, b);
            }
            a.AddEdge(b, edge.Weight);
        }
        Console.WriteLine(5);

        IComparer<Node> valueComparer = Comparer<Node>.Create((Node a, Node b) => a.CurrentValue.CompareTo(b.CurrentValue)); // b.CurrentValue.CompareTo(a.CurrentValue));

        //Start with a seed of specified road segments
        //Node start = nodes[527429]; pickOrder.Add(start); start.Pick(); // Northbound south of Helsingborg
        //start = nodes[18177]; pickOrder.Add(start); start.Pick(); // Southbound south of Helsingborg
        List<Node> pickOrder = new List<Node>();
        foreach (var id in startNodeIDs.Intersect(nodes.Keys))
        {
            Node start = nodes[id];
            pickOrder.Add(start);
            start.Pick();
        }

        //Fully color the graph in descending order of CurrentValue, resorting after each colored node
        var pool = nodes.Values.OrderByDescending(n => n.WeightSum).Take(50000).Where(n => !n.IsPicked).ToList();
        pool.Sort(valueComparer);
        while (pool.Count > 0)
        {
            if (pool.Count % 10000 == 0)
                Console.WriteLine(pool.Count);

            Node n = pool.Last();
            n.Pick();
            pickOrder.Add(n);
            pool.RemoveAt(pool.Count - 1);
            pool.Sort(valueComparer);
        }

        Console.WriteLine(6);

        int i = 0;
        double cd = 0;
        File.WriteAllLines(@"C:\data\etisplus\ers_build_order_seed_in_all.csv", pickOrder.Select(n => {
            var c = clusters[n.ID];
            var d = c.distance_m * (c.bidirectional ? 1 : 0.5);
            cd += d;
            return string.Join(',', new object[] { i++, n.ID, c.from.ExactLatitude, c.from.ExactLongitude, c.to.ExactLatitude, c.to.ExactLongitude, c.annualMovements, d, cd });
        }));
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        //52.159566, 12.109096, 52.387140, 12.641034
        //49.088842, 2.509603, 49.278558, 2.739365

        //var nodes = EtisPlus.ReadNodes();

        //var seed = nodes.Where(n => n.Value.ExactLatitude > 52.159566 && n.Value.ExactLongitude > 12.109096 && n.Value.ExactLatitude < 52.387140 && n.Value.ExactLongitude < 12.641034)
        //    .Union(nodes.Where(n => n.Value.ExactLatitude > 49.088842 && n.Value.ExactLongitude > 2.509603 && n.Value.ExactLatitude < 49.278558 && n.Value.ExactLongitude < 2.739365))
        //    .Select(n => n.Key).ToList();

        var clusters = EtisPlus.ReadClusters();
        var seed = clusters.GroupBy(n => n.Value.countryCode)
            .SelectMany(group => group.OrderByDescending(n => n.Value.annualMovements).Take(10))
            .Where(n => n.Value.annualMovements > 365 * 15000)
            .Select(n => n.Key)
            .ToList();

        EtisPlus.CalculateErsRolloutOrder(seed);
    }
}