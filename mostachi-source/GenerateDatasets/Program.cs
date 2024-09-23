using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Commons;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using SharpKml.Base;
using SharpKml.Engine;
using SharpKml.Dom;
using RTree;
using System.Text;

namespace GenerateDatasets
{
    class Program
    {
        enum TripDirection { SwedenToSweden, SwedenToScandinavia, ScandinaviaToSweden, SwedenToOther, OtherToSweden, OtherToOther };

        class IntCoordinate
        {
            private readonly int _latRes, _lonRes, _ilat, _ilon, _hash;
            public readonly double Lat, Lon;

            public IntCoordinate(double lat, double lon, int latRes, int lonRes)
            {
                _ilat = (int)Math.Round(latRes * lat); //Using round instead of floor will generate errors at the domain boundaries, but simplifies rendering
                _ilon = (int)Math.Round(lonRes * lon);
                _latRes = latRes;
                _lonRes = lonRes;
                Lat = _ilat / (double)_latRes;
                Lon = _ilon / (double)_lonRes;
                int circumference = _lonRes * 360;
                _hash = _ilat * circumference + _ilon;
            }

            public override int GetHashCode()
            {
                return _hash;
            }

            public override bool Equals(object obj)
            {
                if (obj is IntCoordinate)
                {
                    IntCoordinate other = (IntCoordinate)obj;
                    return (other._ilat == _ilat) && (other._ilon == _ilon) && (other._latRes == _latRes) && (other._lonRes == _lonRes);
                }
                return false;
            }
        }

        static void Main(string[] args)
        {
            /* 
             * This method generates the following files
             * 
             * Filename                             Alias
             * acea_all_stop_location_matches.tsv   Paths.AceaAllStopClusters
             * route_length_class.tsv               Paths.RouteLengthClass
             * cluster_traffic_and_length.tsv       Paths.AnnualMovementsAndLengthPerCluster
             * ers_build_order.tsv                  Paths.ErsBuildOrder
             */

            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            //The bundled data for Sweden had some quality issues that we compensate for by re-scaling the amount of traffic on different route lengths.
            //Length here is measured in number of grid cells, by default 1/25 degrees, or approximately 5-ish km.
            //Set this variable to null if you don't wish to use it.
            //TODO: Improve how this is handled. It should be a global setting somewhere.
            Dictionary<int, float> routeClassWeights = null; // new Dictionary<int, float>() { { 1, 1.35f }, { 10, 1.3f }, { 20, 0.8f }, { 50, 2f }, { 100, 1.1f }, { 200, 2f }, { 500, 0.7f }, { 10000, 0f } };

            Console.WriteLine("Preprocessing data...");
            HashSet<long>
                bidirectionalNodes = DataReader.ReadBidirectionalNodes(Paths.BidirectionalNodes).ToHashSet();
            Dictionary<long, (double lat, double lon, bool bidirectional)>
                nodes = DataReader.ReadNodes(Paths.Nodes).ToDictionary(n => n.nodeID, m => (m.latitude, m.longitude, bidirectionalNodes.Contains(m.nodeID)));
            Dictionary<int, (double from_lat, double from_lon, double to_lat, double to_lon, double mid_lat, double mid_lon, bool bidirectional, float distance_m, float speed_kmph)>
                clusters = DataReader.ReadClusterNodes(Paths.ClusterNodePairs).ToDictionary(n => n.clusterID, m =>
                {
                    var c = GetCoordinates(m.fromNodeID, m.toNodeID, nodes);
                    return (c.from_lat, c.from_lon, c.to_lat, c.to_lon, c.mid_lat, c.mid_lon, c.bidirectional, m.distance_m, m.speed_kmph);
                });

            //Start deploying ERS between Malmö and Göteborg, in both directions
            var ersSeedSegments = clusters.Where(n => n.Value.mid_lat > 55.937114 && n.Value.mid_lat < 55.952384 && n.Value.mid_lon > 12.803299 && n.Value.mid_lon < 12.820696).Select(n => (int)n.Key).ToList();
            CalculateErsRolloutOrder(clusters, ersSeedSegments);

            GenerateAnnualMovementsAndLengthPerClusterCSV(routeClassWeights, clusters);

            MatchAceaChargePointsWithClusters(nodes, clusters);

            CalculateClusterToGridMapping();
        }

        private static void GenerateAnnualMovementsAndLengthPerClusterCSV(Dictionary<int, float> routeClassWeight,
            Dictionary<int, (double from_lat, double from_lon, double to_lat, double to_lon, double mid_lat, double mid_lon, bool bidirectional, float distance_m, float speed_kmph)> clusters)
        {
            float minSpeedKmph = 30;
            var trafficPerCluster = DataReader.ReadModelTrafficPerCluster(Paths.ClusterSequences, Paths.RouteVehicleType, Paths.RouteLengthClass, routeClassWeight);
            using (StreamWriter file = new StreamWriter(Paths.AnnualMovementsAndLengthPerCluster, append: false))
            {
                file.WriteLine("cluster_id\tannual_movements\taadt\tspeed_kmph\thours_with_vehicle_per_day\tlength_m\tbidirectional\tfrom_lat\tfrom_lon\tto_lat\tto_lon\tis_in_sweden_bbox");
                foreach (var item in trafficPerCluster.OrderByDescending(n => n.Value.annualMovements / Math.Max(clusters[n.Key].speed_kmph, minSpeedKmph)))
                {
                    var c = clusters[item.Key];
                    float hoursPerKm = 1f / Math.Max(minSpeedKmph, c.speed_kmph);
                    float kmPerVehicle = 15f / 1000f;
                    float vehiclesPerDay = item.Value.annualMovements / 365f;
                    float hoursWithVehiclePerDay = vehiclesPerDay * kmPerVehicle * hoursPerKm;
                    if (double.IsNaN(item.Value.annualMovements + vehiclesPerDay + c.speed_kmph + hoursWithVehiclePerDay + c.distance_m + c.from_lat + c.from_lon + c.to_lat + c.to_lon))
                        Console.WriteLine("NaN");
                    file.WriteLine(string.Join('\t', item.Key, item.Value.annualMovements, vehiclesPerDay, c.speed_kmph, hoursWithVehiclePerDay, c.distance_m, c.bidirectional, c.from_lat, c.from_lon, c.to_lat, c.to_lon, DataReader.IsInSweden(c.mid_lat, c.mid_lon)));
                }
            }
        }

        private static void CalculateHistogramOfInflowOutflowDiscrepancy(Dictionary<int, (float annualMovements, float annualTonnes)> routeVolume)
        {
            var outDegree = DataReader.ReadRoutes(Paths.Routes).GroupBy(n => n.fromPlaceID).ToDictionary(g => g.Key, g => g.Sum(n => routeVolume[n.routeID].annualMovements));
            var inDegree = DataReader.ReadRoutes(Paths.Routes).GroupBy(n => n.toPlaceID).ToDictionary(g => g.Key, g => g.Sum(n => routeVolume[n.routeID].annualMovements));
            var ratios = outDegree.Select(n =>
            {
                if (!inDegree.TryGetValue(n.Key, out float denominator))
                    denominator = n.Value;
                var bin = Math.Round(20 * n.Value / denominator) / 20.0;
                return bin;
            }).ToList();
            var histogram = ratios.GroupBy(n => n).Select(g => new { bin = g.Key, count = g.Count() }).OrderBy(n => n.bin);
            var histStr = string.Join('\n', histogram.Select(n => n.bin + "\t" + n.count));
        }

        private static void MatchAadtWithClusters(
            string clustersPath,
            string clusterSeqPath,
            string routeVehicleTypePath,
            string aadtInputPath,
            string aadtOutputPath,
            Dictionary<long, (double lat, double lon, bool bidirectional)> nodes,
            Dictionary<int, (double from_lat, double from_lon, double to_lat, double to_lon, double mid_lat, double mid_lon, bool bidirectional, float distance_m, float speed_kmph)> clusters)
        {
            Console.WriteLine("Identifying the main road network...");
            var trafficPerCluster = DataReader.ReadModelTrafficPerCluster(clusterSeqPath, routeVehicleTypePath);
            float movementsThreshold = trafficPerCluster.Values.Select(n => n.annualMovements).OrderByDescending(n => n).Skip((int)(trafficPerCluster.Count * 0.2)).First();
            var mainClusters = trafficPerCluster.Where(n => n.Value.annualMovements > movementsThreshold).Select(n => n.Key).ToHashSet();
            trafficPerCluster = null;

            Console.WriteLine("Indexing clusters in an RTree...");
            var rtree = new RTree.RTree<(int clusterID, bool bidirectional)>();
            const float oneKmLongitude = 0.015f; //0.008f is one km latitude

            //Add the bounding box of each road segment ("cluster") to an index
            foreach (var cluster in DataReader.ReadClusterNodes(clustersPath))
            {
                if (!mainClusters.Contains(cluster.clusterID))
                    continue;
                var p1 = nodes[cluster.fromNodeID];
                var p2 = nodes[cluster.toNodeID];
                bool bidirectional = p1.bidirectional && p2.bidirectional;
                var min = new float[] { (float)Math.Min(p1.lat, p2.lat), (float)Math.Min(p1.lon, p2.lon), 0 };
                var max = new float[] { (float)Math.Max(p1.lat, p2.lat), (float)Math.Max(p1.lon, p2.lon), 0 };
                rtree.Add(new Rectangle(min, max), (cluster.clusterID, bidirectional));
            }

            //For each AADT measurement, find road segments that are similar and join with the best candidate
            Console.WriteLine("Join AADT measurements with OSM road network...");
            ConcurrentBag<(int clusterID, float aadtHeavy, bool bidirectional)> matchedAadtData = new ConcurrentBag<(int clusterID, float aadtHeavy, bool bidirectional)>();
            ConcurrentBag<string> unmatched = new ConcurrentBag<string>();
            int rowCounter = 0;
            Parallel.ForEach(DataReader.ReadAadtData(aadtInputPath), aadt =>
            {
                Interlocked.Increment(ref rowCounter);
                if (rowCounter % 10000 == 0)
                    Console.Write(rowCounter + " ");

                if (aadt.geometry.Length == 0)
                    return;

                //Get nearest neighbors to each end point and the midpoint
                var p1 = aadt.geometry.First();
                var p2 = aadt.geometry[aadt.geometry.Length / 2];
                var p3 = aadt.geometry.Last();
                var neighbors = rtree.Nearest(new RTree.Point((float)p1.lat, (float)p1.lon, 0), oneKmLongitude * 0.2f);
                neighbors.AddRange(rtree.Nearest(new RTree.Point((float)p2.lat, (float)p2.lon, 0), oneKmLongitude * 0.2f));
                neighbors.AddRange(rtree.Nearest(new RTree.Point((float)p3.lat, (float)p3.lon, 0), oneKmLongitude * 0.2f));
                neighbors = neighbors.Distinct().ToList();

                if (neighbors.Count == 0)
                {
                    unmatched.Add("No match: " + aadt.routeID + ", " + p1.lat + ", " + p1.lon);
                    return;
                }

                //Remove all matches that are not approximately parallel
                double angleThreshold = 0.2; //ratio of pi
                var possibleNeighbors = neighbors.Select(neighbor =>
                    {
                        var c = clusters[neighbor.clusterID];
                        double clusterAngle = Math.Atan2(Math.Cos(Math.PI * c.to_lat / 180) * (c.to_lon - c.from_lon), c.to_lat - c.from_lat);
                        double aadtAngle = aadt.geometry.Skip(1).Zip(aadt.geometry.SkipLast(1)).Select(n => Math.Atan2(Math.Cos(Math.PI * n.First.lat / 180) * (n.First.lon - n.Second.lon), n.First.lat - n.Second.lat)).Average();
                        double angleDiff = Math.Abs(clusterAngle - aadtAngle);
                        if (angleDiff > Math.PI)
                            angleDiff = Math.PI * 2 - angleDiff;
                        return (neighbor, angleDiff);
                    })
                    .Where(n => n.angleDiff < angleThreshold * Math.PI || n.angleDiff > (1 - angleThreshold) * Math.PI)
                    .ToArray();

                if (possibleNeighbors.Length == 0)
                {
                    unmatched.Add("No good match: " + aadt.routeID + ", " + p1.lat + ", " + p1.lon + "; Candidates were " + string.Join(", ", neighbors.Select(c => { var cl = clusters[c.clusterID]; return "(" + cl.from_lat + "," + cl.from_lon + ")"; })));
                    return;
                }

                //Pick the match that is most parallel. If no match goes in the same direction, allow matches in the opposite direction.
                bool hasMatchesInSameDirection = possibleNeighbors.Where(m => m.angleDiff < Math.PI * 0.5).Count() > 0;
                var bestMatch = possibleNeighbors.Select(n =>
                    {
                        double angleDiff = n.angleDiff;
                        if ((n.neighbor.bidirectional || !hasMatchesInSameDirection) && n.angleDiff > Math.PI / 2)
                            angleDiff = Math.PI - angleDiff;
                        return (n.neighbor, angleDiff);
                    })
                    .OrderBy(n => n.angleDiff)
                    .First();

                matchedAadtData.Add((bestMatch.neighbor.clusterID, aadt.aadtHeavy, bestMatch.neighbor.bidirectional));
            });

            List<string> distinct = new List<string>();
            foreach (var cluster in matchedAadtData.GroupBy(n => n.clusterID))
            {
                string str = cluster.Key + "\t" + (float)cluster.Average(m => m.aadtHeavy) + "\t" + cluster.First().bidirectional;
                distinct.Add(str);
            }

            File.WriteAllText(aadtOutputPath, "cluster_id\taadt_heavy\n" + string.Join('\n', distinct));
            File.WriteAllText(aadtOutputPath + ".unmatched", string.Join('\n', unmatched));
        }

        private static void MatchAceaChargePointsWithClusters(
            Dictionary<long, (double lat, double lon, bool bidirectional)> nodes,
            Dictionary<int, (double from_lat, double from_lon, double to_lat, double to_lon, double mid_lat, double mid_lon, bool bidirectional, float distance_m, float speed_kmph)> clusters)
        {
            //Based on manual inspection, this matches all points in Sweden to the road network, except a handful of points used exclusively by Volvo or Scania.

            Console.WriteLine("Index clusters in an RTree...");
            var rtree = new RTree.RTree<int>();
            const float oneKmLon = 0.015f, oneKmLat = 0.008f; //0.008f is one km latitude

            foreach (var cluster in DataReader.ReadClusterNodes(Paths.ClusterNodePairs))
            {
                var p1 = nodes[cluster.fromNodeID];
                var p2 = nodes[cluster.toNodeID];
                var min = new float[] { (float)Math.Min(p1.lat, p2.lat), (float)Math.Min(p1.lon, p2.lon), 0 };
                var max = new float[] { (float)Math.Max(p1.lat, p2.lat), (float)Math.Max(p1.lon, p2.lon), 0 };
                rtree.Add(new Rectangle(min, max), cluster.clusterID);
            }

            Console.WriteLine("Join ACEA long haul points with OSM road network...");
            Dictionary<int, List<int>> clustersWithAceaChargeLocation = new Dictionary<int, List<int>>();
            Dictionary<int, (double lat, double lon)> aceaPoints = new Dictionary<int, (double lat, double lon)>();
            int nextAceaPointID = 0;
            foreach (var point in DataReader.ReadACEAChargeLocations(Paths.AceaLongHaulStopCoordinates, swedenOnly: true)
                .Union(DataReader.ReadACEAChargeLocations(Paths.AceaRegionalStopCoordinates, swedenOnly: true)))
            {
                int pointID = nextAceaPointID++;
                aceaPoints.Add(pointID, point);

                //var neighbors = rtree.Nearest(new RTree.Point((float)point.latitude, (float)point.longitude, 0), oneKmLongitude * 3f);
                var neighbors = rtree.Intersects(new RTree.Rectangle((float)point.latitude - oneKmLat, (float)point.longitude - oneKmLon, (float)point.latitude + oneKmLat, (float)point.longitude + oneKmLon, 0, 0));
                //Figure out to which clusters this charging location belongs.

                if (neighbors.Count == 0)
                    Console.WriteLine("No match: " + point.latitude + ", " + point.longitude);

                foreach (var neighbor in neighbors)
                {
                    var p1 = clusters[neighbor];
                    if (CoordinateHash.HaversineDistance_meters(p1.mid_lat, point.latitude, p1.mid_lon, point.longitude) > 1500)
                        continue;

                    if (!clustersWithAceaChargeLocation.TryAdd(neighbor, new List<int>() { pointID }))
                        clustersWithAceaChargeLocation[neighbor].Add(pointID);
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("cluster_id\tacea_point_id\tcluster_mid_lat\tcluster_mid_lon\tacea_point_lat\tacea_point_lon");
            foreach (var cluster in clustersWithAceaChargeLocation)
            {
                foreach (var aceaPoint in cluster.Value)
                {
                    var p1 = clusters[cluster.Key];
                    var p2 = aceaPoints[aceaPoint];
                    sb.AppendLine(string.Join('\t', new object[] { cluster.Key, aceaPoint, p1.mid_lat, p1.mid_lon, p2.lat, p2.lon }));
                }
            }
            File.WriteAllText(Paths.AceaAllStopClusters, sb.ToString());

            Console.WriteLine("done");
        }

        private static void GenerateAnnualMovementsVsAADTRasterCsv_RouteLength(
            string clustersPath,
            string clusterSeqPath,
            string aadtPath,
            string routeVehicleTypePath,
            //string routeLengthPath,
            string binnedTrafficPath,
            Dictionary<long, (double lat, double lon, bool bidirectional)> nodes)
        {
            /*
             * In a raster per destination group, for each route, for each traversed cell, update max annualMovements.
             * Add another layer for AADT. For each AADT measurement, update max AADT in the corresponding cell.
             */

            //route => length,
            //         variant count,
            //         annualMovements per cluster
            //length => sum of traffic per cluster
            //       => max(traffic per cluster) per bin

            Console.WriteLine("Counting variants per route...");
            Dictionary<int, int>
                variantsPerRoute = DataReader.ReadClusterSequences(clusterSeqPath).Select(n => n.routeID).GroupBy(n => n).ToDictionary(g => g.Key, g => g.Count());

            Console.WriteLine("Mapping clusters to raster bins...");
            int latRes = 40, lonRes = 20;
            var clusters = DataReader.ReadClusterNodes(clustersPath)
                .ToDictionary(
                    c => c.clusterID,
                    c => c.nodeSequence.Select(n =>
                    {
                        var node = nodes[n.fromNodeID];
                        return new IntCoordinate(node.lat, node.lon, latRes, lonRes);
                    })
                    .Distinct().ToArray());

            Console.WriteLine("Calculating total annualMovements per route...");
            var movementsPerRoute = DataReader.ReadRouteVehicleTypes(routeVehicleTypePath)
                .GroupBy(n => n.routeID)
                .ToDictionary(g => g.Key, g => g.Sum(n => n.annualMovementsLoad + n.annualMovementsEmpty));

            var annualMovementsPerDistancePerCluster = new ConcurrentDictionary<int, ConcurrentDictionary<int, float>>();

            //For each route length, sum the traffic from all routes per cluster (road segment)
            ConcurrentDictionary<int, int> routeLengthDict = new ConcurrentDictionary<int, int>();

            Parallel.ForEach(DataReader.ReadClusterSequences(clusterSeqPath), seq =>
            {
                int routeLength = seq.clusterSequence.SelectMany(c => clusters[c]).Distinct().Count();
                int variantCount = variantsPerRoute[seq.routeID];
                routeLength =
                    variantCount == 1 ? 1 :
                    routeLength < 10 ? 10 :
                    routeLength < 20 ? 20 :
                    routeLength < 50 ? 50 :
                    routeLength < 100 ? 100 :
                    routeLength < 200 ? 200 :
                    routeLength < 500 ? 500 : 10000;
                float annualMovementsIncrement = movementsPerRoute[seq.routeID] / variantCount;

                var layer = annualMovementsPerDistancePerCluster.GetOrAdd(routeLength, new ConcurrentDictionary<int, float>());
                foreach (var clusterID in seq.clusterSequence)
                {
                    layer.AddOrUpdate(clusterID, annualMovementsIncrement, (key, old) => old + annualMovementsIncrement);
                }

                routeLengthDict.TryAdd(seq.routeID, routeLength);
            });

            //File.WriteAllText(routeLengthPath, "route_id,length_class" + string.Join('\n', routeLengthDict.Select(n => n.Key + "," + n.Value)));

            //For each raster bin, take the maximum value from all contained clusters
            var raster = annualMovementsPerDistancePerCluster
                .ToDictionary(g => g.Key, g => g.Value.SelectMany(c => clusters[c.Key].Select(bin => (bin, c.Value)))
                    .GroupBy(n => n.bin)
                    .ToDictionary(g => g.Key, g => g.Max(n => n.Value)));

            Console.WriteLine("Calculating max AADT per raster bin...");
            IEnumerable<(IntCoordinate bin, float maxAadtHeavy)> maxAadtPerCell = DataReader.ReadAadtData(aadtPath)
                .SelectMany(n => n.geometry.Select(p => new IntCoordinate(p.lat, p.lon, latRes, lonRes)).Distinct().Select(bin => (bin, n.aadtHeavy)))
                .GroupBy(n => n.bin)
                .Select(g => (g.Key, g.Max(n => n.aadtHeavy)));

            Console.WriteLine("Writing to disk...");
            string header = "layer\tlatitude\tlongitude\taadt_max\n";
            string part1 = string.Join('\n', raster.SelectMany(layer => layer.Value.Select(bin => string.Join('\t', layer.Key, bin.Key.Lat, bin.Key.Lon, bin.Value / 365f))));
            string part2 = string.Join('\n', maxAadtPerCell.Select(bin => string.Join('\t', "aadt", bin.bin.Lat, bin.bin.Lon, bin.maxAadtHeavy)));
            File.WriteAllText(binnedTrafficPath, header + part1 + part2);
            Console.WriteLine("Done!");
        }

        private static void GenerateAnnualMovementsVsAADTRasterCsv_VehicleType(
            string clustersPath,
            string clusterSeqPath,
            string aadtPath,
            string routeVehicleTypePath,
            string placesPath,
            string routesPath,
            string binnedTrafficPath,
            Dictionary<long, (double lat, double lon, bool bidirectional)> nodes)
        {
            /*
             * In a raster per destination group, for each route, for each traversed cell, update max annualMovements.
             * Add another layer for AADT. For each AADT measurement, update max AADT in the corresponding cell.
             */

            //type =>
            //      route => variant count,
            //               annualMovements per cluster
            //type => sum of traffic per cluster
            //     => max(traffic per cluster) per bin

            Console.WriteLine("Counting variants per route...");
            Dictionary<int, int>
                variantsPerRoute = DataReader.ReadClusterSequences(clusterSeqPath).Select(n => n.routeID).GroupBy(n => n).ToDictionary(g => g.Key, g => g.Count());

            Console.WriteLine("Mapping clusters to raster bins...");
            int latRes = 40, lonRes = 20;
            var clusters = DataReader.ReadClusterNodes(clustersPath)
                .ToDictionary(
                    c => c.clusterID,
                    c => c.nodeSequence.Select(n =>
                    {
                        var node = nodes[n.fromNodeID];
                        return new IntCoordinate(node.lat, node.lon, latRes, lonRes);
                    })
                    .Distinct().ToArray());

            Console.WriteLine("Calculating total annualMovements per route...");
            var typeToRouteToMovements = DataReader.ReadRouteVehicleTypes(routeVehicleTypePath)
                .GroupBy(n => n.vehicleTypeID)
                .ToDictionary(g => g.Key, g => g.ToDictionary(n => n.routeID, n => n.annualMovementsLoad + n.annualMovementsEmpty));

            var maxAnnualMovementsPerTypePerBin = new Dictionary<short, Dictionary<IntCoordinate, float>>();

            foreach (short vehicleType in typeToRouteToMovements.Keys)
            {
                Dictionary<int, float> annualMovementsPerRoute = typeToRouteToMovements[vehicleType];

                Console.WriteLine("Calculating the total annualMovements per (route,cluster) as the sum of all route variants...");
                var annualMovementSumPerCluster = new ConcurrentDictionary<int, float>();

                //For this vehicle type, sum the traffic from all routes per cluster (road segment)
                Parallel.ForEach(DataReader.ReadClusterSequences(clusterSeqPath), seq =>
                {
                    annualMovementsPerRoute.TryGetValue(seq.routeID, out float annualMovements);
                    if (annualMovements > 0)
                    {
                        float annualMovementsIncrement = annualMovements / variantsPerRoute[seq.routeID];
                        foreach (var clusterID in seq.clusterSequence)
                        {
                            annualMovementSumPerCluster.AddOrUpdate(clusterID, annualMovementsIncrement, (key, old) => old + annualMovementsIncrement);
                        }
                    }
                });

                //For each raster bin, take the maximum value from all contained clusters
                var raster = annualMovementSumPerCluster.SelectMany(c => clusters[c.Key].Select(bin => (bin, c.Value)))
                    .GroupBy(n => n.bin)
                    .ToDictionary(g => g.Key, g => g.Max(n => n.Value));

                maxAnnualMovementsPerTypePerBin.Add(vehicleType, raster);
            }

            Console.WriteLine("Calculating max AADT per raster bin...");
            IEnumerable<(IntCoordinate bin, float maxAadtHeavy)> maxAadtPerCell = DataReader.ReadAadtData(aadtPath)
                .SelectMany(n => n.geometry.Select(p => new IntCoordinate(p.lat, p.lon, latRes, lonRes)).Distinct().Select(bin => (bin, n.aadtHeavy)))
                .GroupBy(n => n.bin)
                .Select(g => (g.Key, g.Max(n => n.aadtHeavy)));

            Console.WriteLine("Writing to disk...");
            string header = "layer\tlatitude\tlongitude\taadt_max\n";
            string part1 = string.Join('\n', maxAnnualMovementsPerTypePerBin.SelectMany(layer => layer.Value.Select(bin => string.Join('\t', layer.Key, bin.Key.Lat, bin.Key.Lon, bin.Value / 365f))));
            string part2 = string.Join('\n', maxAadtPerCell.Select(bin => string.Join('\t', "aadt", bin.bin.Lat, bin.bin.Lon, bin.maxAadtHeavy)));
            File.WriteAllText(binnedTrafficPath, header + part1 + part2);
            Console.WriteLine("Done!");
        }

        private static void GenerateAnnualMovementsVsAADTRasterCsv(
            string clusterNodesPath,
            string clusterSeqPath,
            string aadtPath,
            string routeVehicleTypePath,
            string placesPath,
            string routesPath,
            string binnedTrafficPath,
            Dictionary<long, (double lat, double lon, bool bidirectional)> nodes)
        {
            /*
             * In a raster per destination group, for each route, for each traversed cell, update max annualMovements.
             * Add another layer for AADT. For each AADT measurement, update max AADT in the corresponding cell.
             */

            Console.WriteLine("Counting variants per route...");
            Dictionary<int, int>
                variantsPerRoute = DataReader.ReadClusterSequences(clusterSeqPath).Select(n => n.routeID).GroupBy(n => n).ToDictionary(g => g.Key, g => g.Count());

            Console.WriteLine("Mapping clusters to raster bins...");
            int latRes = 40, lonRes = 20;
            var clusters = DataReader.ReadClusterNodes(clusterNodesPath)
                .ToDictionary(
                    c => c.clusterID,
                    c => c.nodeSequence.Select(n =>
                        {
                            var node = nodes[n.fromNodeID];
                            return new IntCoordinate(node.lat, node.lon, latRes, lonRes);
                        })
                        .Distinct().ToArray());

            Console.WriteLine("Calculating total annualMovements per route...");
            var routeToMovements = DataReader.ReadRouteVehicleTypes(routeVehicleTypePath)
                .GroupBy(n => n.routeID)
                .ToDictionary(g => g.Key, g => g.Sum(n => n.annualMovementsLoad + n.annualMovementsEmpty));

            Console.WriteLine("Calculating trip direction per route...");
            var placeToCountryCode = DataReader.ReadPlaces(placesPath)
                .ToDictionary(n => n.placeID, n => n.country);
            var routeToTripDirection = DataReader.ReadRoutes(routesPath)
                .ToDictionary(n => n.routeID, n =>
                {
                    var c1 = placeToCountryCode[n.fromPlaceID];
                    var c2 = placeToCountryCode[n.toPlaceID];
                    switch ((c1, c2))
                    {
                        case (DataReader.CountryCode.Sweden, DataReader.CountryCode.Sweden):
                            return TripDirection.SwedenToSweden;
                        case (DataReader.CountryCode.Sweden, DataReader.CountryCode.Scandinavia):
                        case (DataReader.CountryCode.Scandinavia, DataReader.CountryCode.Sweden):
                            return TripDirection.SwedenToScandinavia;
                        case (DataReader.CountryCode.Sweden, DataReader.CountryCode.Other):
                        case (DataReader.CountryCode.Other, DataReader.CountryCode.Sweden):
                            return TripDirection.SwedenToOther;
                        default:
                            return TripDirection.OtherToOther;
                    }
                });
            placeToCountryCode = null;
            GC.Collect();

            //route => layer,
            //         variant count,
            //         distribution over clusters
            //layer => sum of traffic per cluster
            //      => max(traffic per cluster) per bin

            Console.WriteLine("Calculating the total annualMovements per (route,cluster) as the sum of all route variants...");
            var seenVariantsPerRoute = new ConcurrentDictionary<int, int>();
            var routeToAnnualMovementPerCluster = new ConcurrentDictionary<int, ConcurrentDictionary<int, float>>();
            var layerToAnnualMovementPerCluster = new ConcurrentDictionary<TripDirection, ConcurrentDictionary<int, float>>();

            Parallel.ForEach(DataReader.ReadClusterSequences(clusterSeqPath), seq =>
            {
                var clusterIDs = seq.clusterSequence.Distinct();
                float annualMovementsIncrement = routeToMovements[seq.routeID] / variantsPerRoute[seq.routeID];
                var routeClusters = routeToAnnualMovementPerCluster.GetOrAdd(seq.routeID, new ConcurrentDictionary<int, float>());
                foreach (var clusterID in clusterIDs)
                {
                    routeClusters.AddOrUpdate(clusterID, annualMovementsIncrement, (oldKey, oldVal) => oldVal + annualMovementsIncrement);
                }

                int seenVariantCount = seenVariantsPerRoute.AddOrUpdate(seq.routeID, 1, (key, old) => old + 1);
                if (seenVariantCount == variantsPerRoute[seq.routeID])
                {
                    //add to sum of traffic per cluster
                    TripDirection dir = routeToTripDirection[seq.routeID];
                    var layer = layerToAnnualMovementPerCluster.GetOrAdd(dir, new ConcurrentDictionary<int, float>());
                    foreach (var cluster in routeClusters)
                    {
                        layer.AddOrUpdate(cluster.Key, cluster.Value, (key, old) => old + cluster.Value);
                    }
                    routeToAnnualMovementPerCluster.TryRemove(seq.routeID, out var dummy1);
                    seenVariantsPerRoute.Remove(seq.routeID, out int dummy2);
                }
            });
            if (routeToAnnualMovementPerCluster.Count != 0)
                throw new Exception("Something didn't complete.");
            seenVariantsPerRoute = null;
            routeToAnnualMovementPerCluster = null;
            routeToMovements = null;
            variantsPerRoute = null;
            GC.Collect();

            Console.WriteLine("Calculating the maximum annualMovement per bin per route direction...");
            ConcurrentDictionary<TripDirection, ConcurrentDictionary<IntCoordinate, float>>
                maxAnnualMovementsPerDirectionPerBin = new ConcurrentDictionary<TripDirection, ConcurrentDictionary<IntCoordinate, float>>();
            Parallel.ForEach(layerToAnnualMovementPerCluster, layer =>
            {
                var tripDirection = layer.Key;
                var raster = maxAnnualMovementsPerDirectionPerBin.GetOrAdd(tripDirection, new ConcurrentDictionary<IntCoordinate, float>());
                Parallel.ForEach(layer.Value, cluster =>
                {
                    var bins = clusters[cluster.Key];
                    foreach (var bin in bins)
                    {
                        raster.AddOrUpdate(bin, cluster.Value, (key, old) => Math.Max(old, cluster.Value));
                    }
                });
            });
            layerToAnnualMovementPerCluster = null;
            GC.Collect();

            Console.WriteLine("Calculating max AADT per raster bin...");
            IEnumerable<(IntCoordinate bin, float maxAadtHeavy)> maxAadtPerCell = DataReader.ReadAadtData(aadtPath)
                .SelectMany(n => n.geometry.Select(p => new IntCoordinate(p.lat, p.lon, latRes, lonRes)).Distinct().Select(bin => (bin, n.aadtHeavy)))
                .GroupBy(n => n.bin)
                .Select(g => (g.Key, g.Max(n => n.aadtHeavy)));

            Console.WriteLine("Writing to disk...");
            string header = "layer\tlatitude\tlongitude\taadt_max\n";
            string part1 = string.Join('\n', maxAnnualMovementsPerDirectionPerBin.SelectMany(layer => layer.Value.Select(bin => string.Join('\t', layer.Key, bin.Key.Lat, bin.Key.Lon, bin.Value / 365f))));
            string part2 = string.Join('\n', maxAadtPerCell.Select(bin => string.Join('\t', "aadt", bin.bin.Lat, bin.bin.Lon, bin.maxAadtHeavy)));
            File.WriteAllText(binnedTrafficPath, header + part1 + part2);
            Console.WriteLine("Done!");
        }

        private static void GenerateAADTPerNodeCsv(
            string clustersPath,
            string bidirectionalNodesPath,
            string aadtPerNodePath,
            ConcurrentDictionary<int, (float annualMovements, float annualTonnes)> sumPerCluster,
            Dictionary<long, (double lat, double lon)> nodes)
        {
            (long nodeID, float aadtHeavy)[] aadtPerNode = DataReader.ReadClusterNodes(clustersPath)
                .SelectMany(n => n.nodeSequence.Select(m => (m.fromNodeID, sumPerCluster[n.clusterID])))
                .GroupBy(n => n.fromNodeID)
                .Select(n => (n.Key, n.Average(m => m.Item2.annualMovements) / 365))
                .ToArray();

            var bidirectionalNodes = File.ReadAllText(bidirectionalNodesPath).Split('\n').Select(n => long.Parse(n)).ToHashSet();

            Console.WriteLine("Writing AADT per node...");
            using (StreamWriter writer = new StreamWriter(aadtPerNodePath, append: false))
            {
                writer.WriteLine("node_id\taadt_heavy\tis_bidirectional\tlongitude\tlatitude\tdirection");

                Parallel.ForEach(DataReader.ReadClusterNodes(clustersPath), cluster =>
                {
                    double aadt = sumPerCluster[cluster.clusterID].annualMovements / 365;
                    var s = cluster.nodeSequence;
                    foreach ((long n0, long n1) in s)
                    {
                        var p0 = nodes[n0];
                        var p1 = nodes[n1];
                        double dLat = p1.lat - p0.lat;
                        double dLon = p1.lon - p0.lon;
                        double angle = Math.Atan2(dLat, dLon);
                        int isBidirectional = bidirectionalNodes.Contains(n0) ? 1 : 0;
                        lock (writer)
                        {
                            writer.WriteLine(string.Join("\t", n0, aadt, isBidirectional, p0.lat, p0.lon, angle));
                        }
                    }
                });
            }
            Console.WriteLine(" done!");
        }

        private static void GenerateTrafficPerRoadCsv(
            string clusterTrafficPath,
            ConcurrentDictionary<int, (float annualMovements, float annualTonnes)> sumPerCluster,
            Dictionary<int, (double from_lat, double from_lon, double to_lat, double to_lon, double mid_lat, double mid_lon)> clusters)
        {
            Console.WriteLine("Writing traffic per road segment (cluster)...");
            using (StreamWriter writer = new StreamWriter(clusterTrafficPath, append: false))
            {
                writer.WriteLine("cluster_id\tannual_movements\tannual_tonnes\tfrom_latitude\tfrom_longitude\tto_latitude\tto_longitude\tmid_latitude\tmid_longitude");

                foreach (var c in sumPerCluster)
                {
                    var coords = clusters[c.Key];
                    writer.WriteLine(string.Join("\t",
                        c.Key,
                        c.Value.annualMovements,
                        c.Value.annualTonnes,
                        coords.from_lat, coords.from_lon,
                        coords.to_lat, coords.to_lon,
                        coords.mid_lat, coords.mid_lon));
                }
            }
        }

        private static void GenerateTrafficPerRoadKml(
            string clusterTrafficKmlPath,
            string clusterSeqPath,
            string routeVehicleTypePath,
            Dictionary<int, (double from_lat, double from_lon, double to_lat, double to_lon, double mid_lat, double mid_lon, bool bidirectional, float distance_m, float speed_kmph)> clusters)
        {
            Console.WriteLine("Writing traffic per road segment (cluster) to kml file...");
            double altitudeScale = 1 / 365.0; // 10000.0 / sumPerCluster.Max(n => n.Value.annualMovements);
            Placemark placemark = new Placemark();
            MultipleGeometry multiGeo = new MultipleGeometry();
            placemark.Geometry = multiGeo;

            var clusterTraffic = DataReader.ReadModelTrafficPerCluster(clusterSeqPath, routeVehicleTypePath);

            float threshold = clusterTraffic.Values.Select(n => n.annualMovements).OrderByDescending(n => n).Take(clusters.Count() / 10).Last();
            foreach (var s in clusterTraffic.Where(n => n.Value.annualMovements > threshold))
            {
                var c = clusters[s.Key];
                CoordinateCollection coords = new CoordinateCollection(new Vector[] { new Vector(c.from_lat, c.from_lon, s.Value.annualMovements * altitudeScale), new Vector(c.to_lat, c.to_lon, s.Value.annualMovements * altitudeScale) });
                multiGeo.AddGeometry(new LineString() { Coordinates = coords, Extrude = true, AltitudeMode = AltitudeMode.RelativeToGround });
            }
            KmlFile kmlFile = KmlFile.Create(placemark, false);
            using (var file = File.OpenWrite(clusterTrafficKmlPath))
            {
                kmlFile.Save(file);
            }
        }

        private static void GenerateTrafficOnEssingebronKml(
            string clusterTrafficKmlPath,
            string clusterSeqPath,
            string routeVehicleTypePath,
            Dictionary<int, (double from_lat, double from_lon, double to_lat, double to_lon, double mid_lat, double mid_lon, bool bidirectional, float distance_m, float speed_kmph)> clusters)
        {
            //var bbox = new { minLat = 59.31711625503913, minLon = 17.996241068930054, maxLat = 59.32005330068608, maxLon = 18.00098655671295 };
            //var bboxClusters = clusters.Where(n => n.Value.mid_lat > bbox.minLat && n.Value.mid_lat < bbox.maxLat && n.Value.mid_lon > bbox.minLon && n.Value.mid_lon < bbox.maxLon).ToArray();
            //1202, 108141

            Console.WriteLine("Writing traffic per road segment (cluster) to kml file...");
            double altitudeScale = 1 / 365.0; // 10000.0 / sumPerCluster.Max(n => n.Value.annualMovements);
            Placemark placemark = new Placemark();
            MultipleGeometry multiGeo = new MultipleGeometry();
            placemark.Geometry = multiGeo;

            var clusterTraffic = DataReader.ReadModelTrafficPerCluster(clusterSeqPath, routeVehicleTypePath);

            foreach (var s in clusterTraffic)
            {
                var c = clusters[s.Key];
                CoordinateCollection coords = new CoordinateCollection(new Vector[] { new Vector(c.from_lat, c.from_lon, s.Value.annualMovements * altitudeScale), new Vector(c.to_lat, c.to_lon, s.Value.annualMovements * altitudeScale) });
                multiGeo.AddGeometry(new LineString() { Coordinates = coords, Extrude = true, AltitudeMode = AltitudeMode.RelativeToGround });
            }
            KmlFile kmlFile = KmlFile.Create(placemark, false);
            using (var file = File.OpenWrite(clusterTrafficKmlPath))
            {
                kmlFile.Save(file);
            }
        }

        static (double from_lat, double from_lon, double to_lat, double to_lon, double mid_lat, double mid_lon, bool bidirectional) GetCoordinates(
            long fromNodeID, long toNodeID, Dictionary<long, (double lat, double lon, bool bidirectional)> nodes)
        {
            var p1 = nodes[fromNodeID];
            var p2 = nodes[toNodeID];
            (double lat, double lon) pMid = (0.5 * (p1.lat + p2.lat), 0.5 * (p1.lon + p2.lon));
            return (p1.lat, p1.lon, p2.lat, p2.lon, pMid.lat, pMid.lon, p1.bidirectional && p2.bidirectional);
        }

        static void CalculateErsRolloutOrder(
            Dictionary<int, (double from_lat, double from_lon, double to_lat, double to_lon, double mid_lat, double mid_lon, bool bidirectional, float distance_m, float speed_kmph)> clusters,
            List<int> startNodeIDs)
        {
            //var routeClassWeights = new Dictionary<int, float>() { { 1, 1.35f }, { 10, 1.3f }, { 20, 0.8f }, { 50, 2f }, { 100, 1.1f }, { 200, 2f }, { 500, 0.7f }, { 10000, 0f } };
            Dictionary<int, float> routeClassWeights = null;

            Console.WriteLine(1);
            //For each route, get the total number of annual vehicle passages
            //BUG: Route traffic counts should be distributed across variants. The importance of logistics hubs is effectively downweighted by 10 due to this bug.
            Dictionary<int, float> routeMovementCount = DataReader.ReadRouteVehicleTypes(Paths.RouteVehicleType, Paths.RouteLengthClass, routeClassWeights)
                .GroupBy(n => n.routeID)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.annualMovementsLoad + i.annualMovementsEmpty));

            var clusterAssociations = new ConcurrentDictionary<int, ConcurrentDictionary<int, float>>();

            Console.WriteLine(2);

            int minLength = 100, minAnnualMovements = 150;

            //Build an index of all route variants that go between grid cells (origin cell to destination cell)
            var fromToIndex = new ConcurrentDictionary<CoordinateHash, ConcurrentDictionary<CoordinateHash, ConcurrentBag<(int routeID, int variantNumber, int[] clusterSequence)>>>();
            Parallel.ForEach(DataReader.ReadClusterSequences(Paths.ClusterSequences, 1)
                .Where(n => n.clusterSequence.Length >= minLength && routeMovementCount[n.routeID] >= minAnnualMovements), seq =>
            {
                var fromCluster = clusters[seq.clusterSequence.First()];
                var toCluster = clusters[seq.clusterSequence.Last()];
                var fromHash = new CoordinateHash(fromCluster.from_lat, fromCluster.from_lon, 10);
                var toHash = new CoordinateHash(toCluster.to_lat, toCluster.to_lon, 10);
                var fromDict = fromToIndex.GetOrAdd(fromHash, _ => new ConcurrentDictionary<CoordinateHash, ConcurrentBag<(int, int, int[])>>());
                var toBag = fromDict.GetOrAdd(toHash, _ => new ConcurrentBag<(int, int, int[])>());
                toBag.Add(seq);
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

            //Build a node-edge representation of the resulting graph, to be colored in order of construction
            //Edges have a fixed weight, which is the strength of association between nodes
            //Nodes have two properties:
            //  1) EdgeWeightSum
            //  2) CurrentValue = the sum of EdgeWeightSum of all already colored neighbors * ln(EdgeWeightSum)
            Dictionary<int, Node> nodes = new Dictionary<int, Node>();
            foreach (var edge in tuples)
            {
                if (!nodes.TryGetValue(edge.A, out Node a))
                {
                    a = new Node(edge.A);
                    nodes.Add(edge.A, a);
                }
                if (!nodes.TryGetValue(edge.B, out Node b))
                {
                    b = new Node(edge.B);
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

            File.WriteAllLines(Paths.ErsBuildOrder, pickOrder.Select(n => n.ID.ToString()));
        }

        static void CalculateClusterToGridMapping()
        {
            var nodes = DataReader.ReadNodes(Paths.Nodes).ToDictionary(n => n.nodeID, n => new CoordinateHash(n.latitude, n.longitude));
            var clusterToWeightedGridCells = DataReader.ReadClusterNodes(Paths.ClusterNodePairs).ToDictionary(n => n, n =>
            {
                var latLonBinAndMeters = n.nodeSequence
                    .SelectMany(m => GetInterpolatedPoints(nodes[m.fromNodeID], nodes[m.toNodeID], 200))
                    .GroupBy(m => m.point.Index)
                    .ToDictionary(g => g.Key, g => g.Sum(m => m.intervalMeters));

                var swerefBinAndMeters = n.nodeSequence
                    .SelectMany(m => GetInterpolatedPoints(nodes[m.fromNodeID], nodes[m.toNodeID], 100))
                    .GroupBy(m => m.point.To_Sweref99TMRaster_1km().cell_id)
                    .ToDictionary(g => g.Key, g => g.Sum(m => m.intervalMeters));

                return (latLonBinAndMeters, swerefBinAndMeters);
            });

            using (StreamWriter writer = new StreamWriter(Paths.ClusterToWeightedGridCells, append: false))
            {
                writer.WriteLine("cluster_id\tbin_index\tratio\tmeters");

                foreach (var cluster in clusterToWeightedGridCells)
                {
                    double latlon_totMeters = cluster.Value.latLonBinAndMeters.Values.Sum();
                    foreach (var bin in cluster.Value.latLonBinAndMeters)
                        writer.WriteLine(cluster.Key.clusterID + "\t" + bin.Key + "\t" + (float)(bin.Value / latlon_totMeters) + "\t" + (float)bin.Value);
                }
            }

            using (StreamWriter writer = new StreamWriter(Paths.ClusterToWeightedGridCells_Sweref99Tm, append: false))
            {
                writer.WriteLine("cluster_id,bin_index,ratio,meters");

                foreach (var cluster in clusterToWeightedGridCells)
                {
                    double sweref_totMeters = cluster.Value.swerefBinAndMeters.Values.Sum();
                    foreach (var bin in cluster.Value.swerefBinAndMeters)
                        writer.WriteLine(cluster.Key.clusterID + "\t" + bin.Key + "\t" + (float)(bin.Value / sweref_totMeters) + "\t" + (float)bin.Value);
                }
            }
        }

        static List<(CoordinateHash point, double intervalMeters)> GetInterpolatedPoints(CoordinateHash from, CoordinateHash to, double sampleDistanceMeters, int binsPerDegree = CoordinateHash.DEFAULT_BINS_PER_DEGREE)
        {
            double totDistance = from.MetersTo(to);
            int steps = (int)(totDistance / sampleDistanceMeters);
            double stepLength = totDistance / (steps + 1);
            double dLat = (to.ExactLatitude - from.ExactLatitude) / steps;
            double dLon = (to.ExactLongitude - from.ExactLongitude) / steps;

            var points = new List<(CoordinateHash point, double intervalMeters)>() { (from, stepLength) };
            for (int i = 0; i < steps; i++)
            {
                var p = points.Last().point;
                points.Add((new CoordinateHash(p.ExactLatitude + dLat, p.ExactLongitude + dLon, binsPerDegree), stepLength));
            }
            return points;
        }

        static ThreadSafeRandom _rand = new ThreadSafeRandom();
        public static void Shuffle<T>(IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = _rand.Instance.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    class Node
    {
        public Node(int id)
        {
            this.ID = id;
            _currentValue = ID / 1000000f;
        }

        public int ID { get; }
        public Dictionary<Node, Edge> Edges { get; } = new Dictionary<Node, Edge>();
        public float WeightSum { get; private set; }
        float _currentValue = 0;
        public float CurrentValue { get { return _currentValue * (float)Math.Log(WeightSum); } }
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
}
