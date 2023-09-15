using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Commons;

namespace CalculateRoutes
{
    class Program
    {
        class Coordinate
        {
            public double Longitude { get; set; }
            public double Latitude { get; set; }
        }

        class CoordinateGenerator
        {
            private class Cell
            {
                public Coordinate Corner;
                public double WidthLon { get; set; }
                public double HeightLat { get; set; }
            }

            readonly Random _rand = new Random();
            readonly Dictionary<int, List<(double cumProb, Cell cell)>> _densityMap = new Dictionary<int, List<(double cumProb, Cell cell)>>();
            Dictionary<int, Coordinate> _placeCoordinates;

            public CoordinateGenerator(string densityMapPath, Dictionary<int, Coordinate> placeCoordinates)
            {
                _placeCoordinates = placeCoordinates;

                Dictionary<int, double> cumProb = new Dictionary<int, double>();

                bool skipHeader = true;
                foreach (string line in File.ReadLines(densityMapPath))
                {
                    if (skipHeader)
                    {
                        skipHeader = false;
                        continue;
                    }
                    string[] parts = line.Split('\t');

                    Coordinate latLon = new Coordinate() { Latitude = double.Parse(parts[1]), Longitude = double.Parse(parts[2]) };
                    Cell cell = new Cell() { Corner = latLon, HeightLat = double.Parse(parts[10]), WidthLon = double.Parse(parts[11]) };
                    double ratioOfRegionPopInCell = double.Parse(parts[8]) / double.Parse(parts[9]);

                    int municipality = int.Parse(parts[3]);

                    if (!cumProb.ContainsKey(municipality))
                        cumProb.Add(municipality, 0);
                    cumProb[municipality] += ratioOfRegionPopInCell;

                    if (!_densityMap.ContainsKey(municipality))
                        _densityMap.Add(municipality, new List<(double cumProb, Cell cell)>());

                    _densityMap[municipality].Add((cumProb[municipality], cell));
                }
            }

            public (bool isVariant, Coordinate coordinate) GetCoordinate(int placeID)
            {
                int municipalityID = PlaceIdToMunicipalityCode(placeID);
                List<(double cumProb, Cell cell)> cells;
                //if it is a specific place in Sweden or outside of Sweden (missing in the density map)
                if ((placeID % 10 != 0) || !_densityMap.TryGetValue(municipalityID, out cells))
                {
                    return (false, _placeCoordinates[placeID]);
                }
                double x = _rand.NextDouble();
                double cumSum = 0;
                for (int i = 0; i < cells.Count; i++)
                {
                    cumSum = cells[i].cumProb;
                    if (cumSum >= x)
                        return (true, SamplePositionOffset(cells[i].cell));
                }
                throw new Exception("Not sure how this could happen");
            }

            public static int PlaceIdToMunicipalityCode(int place_id)
            {
                //this is a "Samgods-rule" for how to find what municipality a certain node belongs to
                return (int)Math.Floor((place_id - 700000) / 100.0);
            }

            public static int MunicipalityCodeToPlaceID(int municipality_code)
            {
                //this is a "Samgods-rule" for how to find what municipality a certain node belongs to
                return municipality_code * 100 + 700000;
            }

            private Coordinate SamplePositionOffset(Cell cell)
            {
                return new Coordinate()
                {
                    //Latitude = position.Latitude + ((_rand.NextDouble() - 0.5) / _earthRadius_km) * _toDegrees,
                    //Longitude = position.Longitude + ((_rand.NextDouble() - 0.5) / _earthRadius_km) * _toDegrees / Math.Cos(position.Latitude * Math.PI / 180)
                    Latitude = cell.Corner.Latitude + _rand.NextDouble() * cell.HeightLat,
                    Longitude = cell.Corner.Longitude + _rand.NextDouble() * cell.WidthLon
                };
            }
        }

        class ConcurrentHashSet<T>
        {
            public HashSet<T> _set = new HashSet<T>();

            public bool Add(T Val)
            {
                lock (_set)
                    return _set.Add(Val);
            }
        }

        class NodeSequence
        {
            public long[] NodeIDs { get; set; }
            public Coordinate[] Coordinates { get; set; }
            public float[] Distances_m { get; set; }
            public float[] Speeds_kmph { get; set; }
            public IEnumerable<(long NodeID, Coordinate Coordinate)> Nodes
            {
                get
                {
                    for (int i = 0; i < NodeIDs.Length; i++)
                    {
                        yield return (NodeIDs[i], Coordinates[i]);
                    }
                }
            }

            public IEnumerable<(long NodeID1, long NodeID2, float Distance_m, float Speed_kmph)> NodePairs
            {
                get
                {
                    for (int i = 0; i < NodeIDs.Length - 1; i++)
                    {
                        yield return (NodeIDs[i], NodeIDs[i + 1], Distances_m[i], Speeds_kmph[i]);
                    }
                }
            }
        }

        static readonly HttpClient _httpClient = new HttpClient();

        static IEnumerable<(int routeID, int fromPlaceID, int toPlaceID)> GetRoutes(string routesPath)
        {
            bool isFirstLine = true;
            foreach (string line in File.ReadLines(routesPath))
            {
                if (isFirstLine)
                {
                    isFirstLine = false;
                    continue;
                }
                string[] parts = line.Split('\t');
                int routeID = int.Parse(parts[0]);
                int fromPlaceID = int.Parse(parts[1]);
                int toPlaceID = int.Parse(parts[2]);
                yield return (routeID, fromPlaceID, toPlaceID);
            }
        }

        static void Benchmark()
        {
            string response = "{\"code\":\"Ok\",\"routes\":[{\"geometry\":{\"coordinates\":[[17.94271,59.518442],[17.94243,59.518534],[17.941403,59.518861],[17.941117,59.518952],[17.940875,59.519033],[17.940227,59.519253],[17.939934,59.519389],[17.939465,59.519736],[17.938317,59.51934],[17.936332,59.51866],[17.934752,59.518183],[17.933754,59.51791],[17.933211,59.517802],[17.932577,59.517751],[17.931837,59.517729],[17.931289,59.517751],[17.930988,59.517789],[17.930556,59.517841],[17.930087,59.517937],[17.928546,59.518171],[17.928306,59.518207],[17.928395,59.518334],[17.928681,59.518891],[17.928774,59.519295],[17.928812,59.519713],[17.928862,59.520186],[17.928832,59.5209],[17.928563,59.522018],[17.927891,59.52422],[17.927855,59.524331],[17.927391,59.525794],[17.926855,59.526702],[17.926461,59.527245],[17.925896,59.528001],[17.925564,59.528621],[17.925497,59.528809],[17.925355,59.529202],[17.925303,59.529346],[17.925081,59.530052],[17.924767,59.531655],[17.924575,59.532498],[17.924412,59.533098],[17.924274,59.53346],[17.924051,59.533877],[17.923863,59.534127],[17.923612,59.534406],[17.923182,59.534831],[17.922941,59.535032],[17.92269,59.535216],[17.922488,59.535356],[17.922179,59.535541],[17.921923,59.535714],[17.921606,59.535893],[17.921295,59.53605],[17.920941,59.536216],[17.92048,59.536417],[17.919455,59.536853],[17.918837,59.537114],[17.918341,59.537324],[17.914166,59.539124],[17.913174,59.539534],[17.910534,59.540763],[17.909472,59.541246],[17.906367,59.542738],[17.905937,59.542986],[17.905628,59.5432],[17.905399,59.543405],[17.90522,59.543642],[17.904757,59.544319],[17.904477,59.544673],[17.904207,59.544919],[17.9039,59.545137],[17.903565,59.545343],[17.903176,59.545525],[17.902821,59.545674],[17.902324,59.545842],[17.901944,59.545932],[17.901293,59.546069],[17.900154,59.54631],[17.899722,59.546412],[17.899227,59.546563],[17.898846,59.546717],[17.898332,59.546974],[17.897909,59.547232],[17.897544,59.547528],[17.897273,59.547811],[17.897117,59.548004],[17.896755,59.548525],[17.896618,59.548775],[17.896476,59.549092],[17.896365,59.54941],[17.896299,59.549766],[17.896205,59.550379],[17.896099,59.551697],[17.895999,59.552888],[17.895877,59.553473],[17.895781,59.554272],[17.895788,59.554781],[17.895688,59.555831]],\"type\":\"LineString\"},\"legs\":[{\"annotation\":{\"metadata\":{\"datasource_names\":[\"lua profile\"]},\"nodes\":[5092895828,254841298,5393893127,5393893131,5393893133,254841300,254841301,56489287,56489286,56489285,56487874,56489284,56489283,56489282,56489281,243854261,56489280,402542908,56489279,8094893585,56488003,5096051803,10784163,2333683010,10784164,10784165,303600294,10784166,306553396,5096051805,10784167,10784168,1432108667,10784169,10784170,7995378260,1500351948,29981549,10784171,10784172,5288351794,5288351798,10784173,5288351799,5288351800,10784174,5288351793,2427333476,5288351802,5288351792,2661604846,5288351791,454150079,5288351790,10784177,5288351789,32900879,5107454046,454150144,302697174,10784179,10784180,454163232,6818354009,10784181,6818353995,1500351927,10784182,6818354007,10784184,2427333487,1500351964,1500351993,10784186,1500351918,1500352072,10784188,31305822,10784187,32900377,1500351951,10784189,5182267223,10784190,454220800,10784191,316267742,1500336074,10784192,1500336042,1500336066,10784193,454217169,2668783691,5182267262,5182267261,5182267260,10784195,7779228330],\"datasources\":[0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0],\"speed\":[7,6.9,7.1,6.8,7,7,7,11.2,11.1,11.2,11,11,11,11,11.1,10.9,10.9,11,11.1,10.9,11.5,11,13.3,13.3,13.2,13.2,13.3,13.3,13.9,13.3,13.3,13.4,13.4,13.2,13.3,11.1,10.9,11,13.4,13.3,13.2,13.2,13.3,13.5,13.1,13.3,13.1,13.1,13.8,13.5,13.4,13.4,13,13.6,13.2,13.2,13.3,13.5,13.3,13.4,13.3,13.4,13.3,13.1,13.4,13.1,13.4,11.1,11.2,11.2,11,11,11.1,11.3,11.2,11.3,11,11,11.2,11.2,11,11,15.5,15.5,15.9,15.5,15.8,15.2,15.7,15.6,15.3,15.5,15.6,15.6,15.6,15.6,15.7,15.4],\"weight\":[2.7,9.9,2.7,2.4,6.3,3.2,6.7,7,12.2,9.3,5.8,3,3.3,3.8,2.8,1.6,2.3,2.6,8.2,1.3,1.3,5.8,3.4,3.5,4,6,9.4,18.6,0.9,12.4,7.9,4.8,6.7,5.4,1.6,4,1.5,7.2,13.4,7.1,5.1,3.1,3.6,2.2,2.6,4,2,1.9,1.4,2,1.8,2,1.9,2,2.6,5.7,3.4,2.7,23.2,5.4,15.2,6,18.1,2.8,2.2,2,2.1,7.2,3.8,2.8,2.7,2.7,2.7,2.3,3,2.1,3.6,6.3,2.4,2.9,2.5,3.7,2.4,2.5,2.2,1.5,3.9,1.9,2.3,2.3,2.6,4.4,9.4,8.5,4.2,5.7,3.6,7.6],\"duration\":[2.7,9.9,2.7,2.4,6.3,3.2,6.7,7,12.2,9.3,5.8,3,3.3,3.8,2.8,1.6,2.3,2.6,8.2,1.3,1.3,5.8,3.4,3.5,4,6,9.4,18.6,0.9,12.4,7.9,4.8,6.7,5.4,1.6,4,1.5,7.2,13.4,7.1,5.1,3.1,3.6,2.2,2.6,4,2,1.9,1.4,2,1.8,2,1.9,2,2.6,5.7,3.4,2.7,23.2,5.4,15.2,6,18.1,2.8,2.2,2,2.1,7.2,3.8,2.8,2.7,2.7,2.7,2.3,3,2.1,3.6,6.3,2.4,2.9,2.5,3.7,2.4,2.5,2.2,1.5,3.9,1.9,2.3,2.3,2.6,4.4,9.4,8.5,4.2,5.7,3.6,7.6],\"distance\":[18.82235,68.412867,19.047858,16.358144,43.99314,22.407331,46.794984,78.326437,135.14066,103.738294,63.974029,32.907825,36.218446,41.823878,31.015899,17.50103,25.051042,28.534877,90.757394,14.1207,14.991722,64.01999,45.240731,46.541999,52.685613,79.433612,125.273612,247.83682,12.512026,164.81568,105.422056,64.355074,89.923712,71.457644,21.249283,44.439636,16.282911,79.51778,179.173122,94.386965,67.365937,41.009179,48.056121,29.759242,34.108014,53.128075,26.163354,24.883634,19.293497,26.963787,24.055533,26.757169,24.748982,27.191976,34.28723,75.449204,45.356231,36.439613,309.038593,72.168608,202.098728,80.445956,241.21926,36.72362,29.496716,26.202919,28.226362,79.69623,42.420584,31.310719,29.790659,29.692871,29.84487,25.984318,33.67786,23.645896,39.737631,69.580949,26.866489,32.569458,27.472102,40.702714,37.310854,38.823844,34.988342,23.198097,61.436882,28.859072,36.155921,35.919133,39.770952,68.387295,146.717971,132.590379,65.429713,89.034372,56.615561,116.923524]},\"steps\":[],\"distance\":5820.3,\"duration\":476.4,\"summary\":\"\",\"weight\":476.4}],\"distance\":5820.3,\"duration\":476.4,\"weight_name\":\"routability\",\"weight\":476.4}],\"waypoints\":[{\"hint\":\"0NW0hQLWtIUBAAAAGwAAAB8AAABjAAAAI_Z3P_38lkG8u61BfzOJQgEAAAAbAAAAHwAAAGMAAACLkgAAtsgRAeotjAMUyhEB-y6MAwEA_xIYGtNM\",\"distance\":36.296622,\"name\":\"Hästhagsvägen\",\"location\":[17.94271,59.518442]},{\"hint\":\"I7W0hSu1tIV4AAAATAAAAPcCAACZAgAAfEY8Q1w66kJAj41EZ3d5RHgAAABMAAAA9wIAAJkCAACLkgAACBERAfe_jANDBxEBur-MAwsAHwsYGtNM\",\"distance\":141.510604,\"name\":\"Stockholmsvägen\",\"location\":[17.895688,59.555831]}]}";

            int sum = 0;
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < 10000; i++)
            {
                dynamic responseObj = JObject.Parse(response);
                dynamic r = responseObj.routes[0];
                long[] nodeOsmIDs = r.legs[0].annotation.nodes.ToObject<long[]>();
                double[][] nodePositions = r.geometry.coordinates.ToObject<double[][]>();
                float[] nodePairDistance = r.legs[0].annotation.distance.ToObject<float[]>();
                float[] nodePairSpeed = r.legs[0].annotation.speed.ToObject<float[]>();

                sum += nodeOsmIDs.Length + nodePositions.Length + nodePairDistance.Length + nodePairSpeed.Length;
            }
            watch.Stop();
            Console.WriteLine(sum + ", " + watch.ElapsedMilliseconds);

            sum = 0;
            Regex pattern = new Regex("(?<=(nodes|speed|nates|tance)\":\\[)[^\"]+(?=\\])", RegexOptions.Compiled);
            watch.Reset();
            watch.Start();
            for (int i = 0; i < 10000; i++)
            {
                long[] nodeOsmIDs = null;
                double[][] nodePositions = null;
                float[] nodePairDistance = null;
                float[] nodePairSpeed = null;
                var matches = pattern.Matches(response);
                foreach (Match match in matches)
                {
                    string variable = match.Groups[1].Value;
                    switch (variable)
                    {
                        case "nates": //coordinates
                            nodePositions = match.Value[1..^1].Split("],[").Select(n => n.Split(",").Select(m => double.Parse(m, CultureInfo.InvariantCulture)).ToArray()).ToArray();
                            break;
                        case "nodes": //nodes
                            nodeOsmIDs = match.Value.Split('\t').Select(n => long.Parse(n)).ToArray();
                            break;
                        case "speed": //speed
                            nodePairSpeed = match.Value.Split('\t').Select(n => float.Parse(n, CultureInfo.InvariantCulture)).ToArray();
                            break;
                        case "tance": //distance
                            nodePairDistance = match.Value.Split('\t').Select(n => float.Parse(n, CultureInfo.InvariantCulture)).ToArray();
                            break;
                        default:
                            break;
                    }
                }

                sum += nodeOsmIDs.Length + nodePositions.Length + nodePairDistance.Length + nodePairSpeed.Length;
            }
            watch.Stop();
            Console.WriteLine(sum + ", " + watch.ElapsedMilliseconds);
        }

        static readonly Regex _pattern = new Regex("(?<=(nodes|speed|nates|tance)\":\\[)[^\"]+(?=\\])", RegexOptions.Compiled);
        static NodeSequence GetRouteFromOsrm(string server, double fromLat, double fromLon, double toLat, double toLon)
        {
            string response = "", url = "";
            try
            {
                url = String.Format(server + "/route/v1/driving/{0},{1};{2},{3}?geometries=geojson&annotations=true&overview=full", fromLon, fromLat, toLon, toLat);
                for (int i = 0; i < 10 && response == ""; i++)
                {
                    Task<string> task = null;
                    try
                    {
                        task = _httpClient.GetStringAsync(url);
                        task.Wait();
                    }
                    catch (Exception)
                    {
                        Console.Write(i);
                    }
                    finally
                    {
                        if (task.IsCompletedSuccessfully)
                            response = task.Result;
                        else
                            Thread.Sleep(50 + (int)Math.Pow(3, i));
                    }
                }

                //This is more robust, but around three times slower
                //dynamic responseObj = JObject.Parse(response);
                //dynamic r = responseObj.routes[0];
                //long[] nodeOsmIDs = r.legs[0].annotation.nodes.ToObject<long[]>();
                //double[][] nodePositions = r.geometry.coordinates.ToObject<double[][]>();
                //float[] nodePairDistance = r.legs[0].annotation.distance.ToObject<float[]>();
                //float[] nodePairSpeed = r.legs[0].annotation.speed.ToObject<float[]>();

                long[] nodeOsmIDs = null;
                double[][] nodePositions = null;
                float[] nodePairDistance = null;
                float[] nodePairSpeed = null;
                var matches = _pattern.Matches(response);
                foreach (Match match in matches)
                {
                    string variable = match.Groups[1].Value;
                    switch (variable)
                    {
                        case "nates": //coordinates
                            nodePositions = match.Value.Substring(1, match.Value.Length - 2).Split("],[").Select(n => n.Split(",").Select(m => double.Parse(m, CultureInfo.InvariantCulture)).ToArray()).ToArray();
                            break;
                        case "nodes": //nodes
                            nodeOsmIDs = match.Value.Split('\t').Select(n => long.Parse(n)).ToArray();
                            break;
                        case "speed": //speed
                            nodePairSpeed = match.Value.Split('\t').Select(n => float.Parse(n, CultureInfo.InvariantCulture)).ToArray();
                            break;
                        case "tance": //distance
                            nodePairDistance = match.Value.Split('\t').Select(n => float.Parse(n, CultureInfo.InvariantCulture)).ToArray();
                            break;
                        default:
                            break;
                    }
                }

                var seq = new NodeSequence()
                {
                    NodeIDs = nodeOsmIDs,
                    Coordinates = nodePositions.Select(n => new Coordinate() { Latitude = n[1], Longitude = n[0] }).ToArray(),
                    Distances_m = nodePairDistance,
                    Speeds_kmph = nodePairSpeed.Select(n => n * 3.6f).ToArray() //m/s => km/h
                };

                return seq;
            }
            catch (Exception e)
            {
                Console.WriteLine(url);
                Console.WriteLine(response);
                Console.WriteLine(e);
            }

            return null;
        }

        private static void ProcessRoute(
            (int routeID, int fromPlaceID, int toPlaceID) route,
            ConcurrentHashSet<long> nodes,
            ConcurrentHashSet<(long, long)> nodePairs,
            int variantsPerRoute,
            CoordinateGenerator coordGen,
            IAsyncWriter nodeWriter,
            IAsyncWriter nodePairWriter,
            IAsyncWriter nodeSeqWriter,
            string osrmServer,
            string errorPath)
        {
            for (int i = 0; i < variantsPerRoute; i++)
            {
                (bool p1IsVariant, Coordinate p1) = coordGen.GetCoordinate(route.fromPlaceID);
                (bool p2IsVariant, Coordinate p2) = coordGen.GetCoordinate(route.toPlaceID);
                
                NodeSequence nodeSequence = GetRouteFromOsrm(osrmServer, p1.Latitude, p1.Longitude, p2.Latitude, p2.Longitude);

                if (nodeSequence == null)
                {
                    File.AppendAllText(errorPath, 
                        String.Format("Could not get route: routeID={0}, fromPlaceID={1}, toPlaceID={2}, variantNo={3}, from=({4}, {5}), to=({6}, {7})",
                        route.routeID, route.fromPlaceID, route.toPlaceID, i, p1.Latitude, p1.Longitude, p2.Latitude, p2.Longitude));
                    continue;
                }
                else if (nodeSequence.Distances_m.Sum() > 10000*1000)
                {
                    File.AppendAllText(errorPath,
                        String.Format("Route is >10000km: routeID={0}, fromPlaceID={1}, toPlaceID={2}, variantNo={3}, from=({4}, {5}), to=({6}, {7})",
                        route.routeID, route.fromPlaceID, route.toPlaceID, i, p1.Latitude, p1.Longitude, p2.Latitude, p2.Longitude));
                    continue;
                }
                else if (nodeSequence.NodeIDs.Length > 10000*100)
                {
                    File.AppendAllText(errorPath,
                        String.Format("Route is >1000000 nodes: routeID={0}, fromPlaceID={1}, toPlaceID={2}, variantNo={3}, from=({4}, {5}), to=({6}, {7})",
                        route.routeID, route.fromPlaceID, route.toPlaceID, i, p1.Latitude, p1.Longitude, p2.Latitude, p2.Longitude));
                    continue;
                }

                foreach (var node in nodeSequence.Nodes)
                {
                    if (nodes.Add(node.NodeID))
                        nodeWriter.Push(node.NodeID, node.Coordinate.Latitude, node.Coordinate.Longitude);
                }

                foreach (var pair in nodeSequence.NodePairs)
                {
                    if (nodePairs.Add((pair.NodeID1, pair.NodeID2)))
                        nodePairWriter.Push(pair.NodeID1, pair.NodeID2, pair.Distance_m, pair.Speed_kmph);
                }

                nodeSeqWriter.Push(route.routeID, i, nodeSequence.NodeIDs);

                if (!(p1IsVariant || p2IsVariant))
                    break;
            }
        }

        static void Main()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            int variantsPerRoute = 1;

            ConcurrentHashSet<long> nodes = new ConcurrentHashSet<long>();
            ConcurrentHashSet<(long, long)> nodePairs = new ConcurrentHashSet<(long, long)>();

            Dictionary<int, Coordinate> places = LoadPlaces();

            //Initialize coordinate sampling function
            CoordinateGenerator coordGen = new CoordinateGenerator(Paths.OriginDestinationDensity, places);

            IAsyncWriter nodeWriter = new AsyncBinaryWriter(Paths.Nodes);
            IAsyncWriter nodePairWriter = new AsyncBinaryWriter(Paths.NodePairs);
            IAsyncWriter[] nodeSequenceWriters = new AsyncBinaryWriter[20];
            List<Task> writerHandles = new List<Task>();
            writerHandles.Add(Task.Run(nodeWriter.WriteAsync));
            writerHandles.Add(Task.Run(nodePairWriter.WriteAsync));
            for (int i = 0; i < 20; i++)
            {
                nodeSequenceWriters[i] = new AsyncBinaryWriter(Paths.NodeSequences + ".part_" + i);
                writerHandles.Add(Task.Run(nodeSequenceWriters[i].WriteAsync));
                nodeSequenceWriters[i].Push("route_id\tvariant_no\tnode_id_sequence");
            }

            nodeWriter.Push("node_id\tlatitude\tlongitude");
            nodePairWriter.Push("from_node_id\tto_node_id\tdistance_m\tspeed_kmph");

            var routes = GetRoutes(Paths.Routes);

            int counter = 0;
            Parallel.ForEach(routes, new ParallelOptions() { MaxDegreeOfParallelism = 8 }, route =>
            {
                var nodeSeqWriter = nodeSequenceWriters[route.routeID % nodeSequenceWriters.Length];
                ProcessRoute(route, nodes, nodePairs, variantsPerRoute, coordGen, nodeWriter, nodePairWriter, nodeSeqWriter, Paths.RootPaths.Osrm, Paths.Error);

                Interlocked.Increment(ref counter);
                if (counter % 100 == 0)
                    Console.Write(", " + counter);
            });

            nodeWriter.Stop();
            nodePairWriter.Stop();
            foreach (var writer in nodeSequenceWriters)
                writer.Stop();

            Console.WriteLine("Waiting to finish writing to files...");
            Task.WaitAll(writerHandles.ToArray());
        }

        private static Dictionary<int, Coordinate> LoadPlaces()
        {
            //Load places
            Dictionary<int, Coordinate> places = new Dictionary<int, Coordinate>();
            bool isFirstLine = true;
            foreach (string line in File.ReadLines(Paths.Places))
            {
                if (isFirstLine)
                {
                    isFirstLine = false;
                    continue;
                }
                string[] parts = line.Split('\t');
                int placeID = int.Parse(parts[0]);
                double lat = double.Parse(parts[1]);
                double lon = double.Parse(parts[2]);
                places.Add(placeID, new Coordinate() { Latitude = lat, Longitude = lon });
            }

            return places;
        }

        public static void Test_SamplePointsInStockholm()
        {
            int stockholm = CoordinateGenerator.MunicipalityCodeToPlaceID(180);

            var places = LoadPlaces();
            CoordinateGenerator gen = new CoordinateGenerator(Paths.OriginDestinationDensity, places);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("latitude\tlongitude");
            for (int i = 0; i < 100000; i++)
            {
                var p = gen.GetCoordinate(stockholm);
                sb.AppendLine(p.coordinate.Latitude + "\t" + p.coordinate.Longitude);
            }
            File.WriteAllText("points_in_stockholm.tsv", sb.ToString());
        }
    }
}
