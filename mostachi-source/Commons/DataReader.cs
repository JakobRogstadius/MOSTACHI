//#define DEBUGGING

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Commons
{
    public class DataReader
    {
        #region Debug functions
        /*
        1 2 3           9 10       
              4 5 6 7 8      11 16
           12            13
                         14
                         15

        a    c
          b     d
        e   f g
             h
        */

        public static IEnumerable<(long nodeID, double latitude, double longitude)> DummyReadNodes(string path)
        {
            return Enumerable.Range(1, 16).Select(n => ((long)n, 0.0, 0.0));
        }

        public static IEnumerable<(long fromNodeID, long toNodeID, float distance_m, float speed_kmph)> DummyReadNodePairs(string path)
        {
            var pairs = new List<(long fromNodeID, long toNodeID, float distance_m, float speed_kmph)>
            {
                (1, 2, 100, 50),
                (2, 3, 100, 50),
                (3, 4, 100, 50),
                (4, 5, 100, 50),
                (5, 6, 100, 50),
                (6, 7, 100, 50),
                (7, 8, 100, 50),
                (8, 9, 100, 50),
                (9, 10, 100, 50),
                (10, 11, 100, 50),
                (11, 16, 100, 50),
                (12, 4, 100, 50),
                (8, 13, 100, 50),
                (13, 11, 100, 50),
                (13, 14, 100, 50),
                (14, 15, 100, 50),
            };
            pairs.AddRange(pairs.Select(n => (n.toNodeID, n.fromNodeID, n.distance_m, n.speed_kmph)).ToArray());
            return pairs;
        }

        public static IEnumerable<(int routeID, int variantNumber, long[] nodeSequence)> DummyReadNodeSequences(string path)
        {
            var seq = new List<(int routeID, int variantNumber, long[] nodeSequence)>
            {
                (0, 0, new long[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 16 }),
                (1, 0, new long[] { 1, 2, 3, 4, 5, 6, 7, 8, 13, 14, 15 }),
                (2, 0, new long[] { 12, 4, 5, 6, 7, 8, 9, 10, 11, 16 }),
                (3, 0, new long[] { 12, 4, 5, 6, 7, 8 }),
                (4, 0, new long[] { 15, 14, 13, 8, 7, 6, 5, 4, 12 }),
                (5, 0, new long[] { 16, 11, 13, 14, 15 }),
                (6, 0, new long[] { 16, 11, 13, 8, 7, 6, 5, 4, 3, 2, 1 }),
                (7, 0, new long[] { 12, 4, 3, 2, 1 }),
            };
            seq.AddRange(seq.Select(n => (n.routeID + seq.Count, 0, n.nodeSequence.Reverse().ToArray())).ToArray());
            return seq;
        }
        #endregion

        public static bool USE_CACHING = true;

        public enum CountryCode
        {
            Sweden = 1,
            Finland = 501,
            Norway = 518,
            Denmark = 519,
            Scandinavia = 2,
            Other = 3
        }

        private class Cache
        {
            readonly ConcurrentDictionary<string, List<object>> _cache = new();

            public bool HasData(string file)
            {
                return _cache.ContainsKey(file);
            }

            public void Append(string file, object o)
            {
                if (_cache.TryGetValue(file, out var cachedObject))
                    cachedObject.Add(o);
                else
                    _cache[file] = new List<object>() { o };
            }

            public IEnumerable<T> Get<T>(string file)
            {
                foreach (var item in _cache[file])
                {
                    yield return (T)item;
                }
            }
        }

        private readonly static Cache _cache = new Cache();

        public static IEnumerable<(int placeID, float latitude, float longitude, string name, CountryCode country)> ReadPlaces(string path)
        {
            if (USE_CACHING && _cache.HasData(path))
            {
                foreach (var item in _cache.Get<(int placeID, float latitude, float longitude, string name, CountryCode country)>(path))
                    yield return item;
            }
            else
            {
                using (StreamReader r = new StreamReader(File.OpenRead(path)))
                {
                    string? line = r.ReadLine(); //route_id,vehicletype_id,annualmovements,annualtonnes
                    while ((line = r.ReadLine()) != null)
                    {
                        string[] parts = line.Split('\t');
                        int placeID = int.Parse(parts[0]);
                        float latitude = float.Parse(parts[1]);
                        float longitude = float.Parse(parts[2]);
                        string name = parts[3];
                        int countryID = int.Parse(parts[4]);
                        CountryCode cc = CountryCode.Other;
                        switch (countryID)
                        {
                            case (int)CountryCode.Sweden:
                                cc = CountryCode.Sweden;
                                break;
                            case (int)CountryCode.Finland:
                            case (int)CountryCode.Norway:
                            case (int)CountryCode.Denmark:
                                cc = CountryCode.Scandinavia;
                                break;
                            default:
                                break;
                        }
                        var item = (placeID, latitude, longitude, name, cc);
                        yield return item;
                        if (USE_CACHING) _cache.Append(path, item);
                    }
                }
            }
        }

        // KFF: I have not changed this function to use .tsv format since it appears not to be used and it is not clear that it will not be used with externally produced .csv files
        public static IEnumerable<(int routeID, int fromPlaceID, int toPlaceID)> ReadRoutes(string path)
        {
            if (USE_CACHING && _cache.HasData(path))
            {
                foreach (var item in _cache.Get<(int routeID, int fromPlaceID, int toPlaceID)>(path))
                    yield return item;
            }
            else
            {
                using (StreamReader r = new StreamReader(File.OpenRead(path)))
                {
                    string? line = r.ReadLine(); //route_id,vehicletype_id,annualmovements,annualtonnes
                    while ((line = r.ReadLine()) != null)
                    {
                        string[] parts = line.Split('\t');
                        int placeID = int.Parse(parts[0]);
                        int fromPlaceID = int.Parse(parts[1]);
                        int toPlaceID = int.Parse(parts[2]);
                        var item = (placeID, fromPlaceID, toPlaceID);
                        yield return item;
                        if (USE_CACHING) _cache.Append(path, item);
                    }
                }
            }
        }

        public static IEnumerable<(long nodeID, double latitude, double longitude)> ReadNodes(string path)
        {
            if (USE_CACHING && _cache.HasData(path))
            {
                foreach (var item in _cache.Get<(long nodeID, double latitude, double longitude)>(path))
                    yield return item;
            }
            else
            {
                using (BinaryReader r = new BinaryReader(File.OpenRead(path)))
                {
                    string headers = r.ReadString();
                    while (r.BaseStream.Position != r.BaseStream.Length)
                    {
                        long nodeID = r.ReadInt64();
                        double lat = r.ReadDouble();
                        double lon = r.ReadDouble();
                        var item = (nodeID, lat, lon);
                        yield return item;
                        if (USE_CACHING) _cache.Append(path, item);
                    }
                }
            }
        }

        public static IEnumerable<(long fromNodeID, long toNodeID, float distance_m, float speed_kmph)> ReadNodePairs(string path)
        {
            if (USE_CACHING && _cache.HasData(path))
            {
                foreach (var item in _cache.Get<(long fromNodeID, long toNodeID, float distance_m, float speed_kmph)>(path))
                    yield return item;
            }
            else
            {
                using (BinaryReader r = new BinaryReader(File.OpenRead(path)))
                {
                    string headers = r.ReadString();
                    while (r.BaseStream.Position != r.BaseStream.Length)
                    {
                        long fromNodeID = r.ReadInt64();
                        long toNodeID = r.ReadInt64();
                        float distance = r.ReadSingle();
                        float speed = r.ReadSingle();
                        var item = (fromNodeID, toNodeID, distance, speed);
                        yield return item;
                        if (USE_CACHING) _cache.Append(path, item);
                    }
                }
            }
        }

        public static IEnumerable<(int routeID, int variantNumber, long[] nodeSequence)> ReadNodeSequences(string[] paths)
        {
            foreach (string path in paths)
            {
                Console.WriteLine("Processing " + path + "...");
                foreach (var item in ReadNodeSequences(path))
                {
                    yield return item;
                }
            }
        }

        public static IEnumerable<(int routeID, int variantNumber, long[] nodeSequence)> ReadNodeSequences(string path)
        {
            using (BinaryReader r = new BinaryReader(File.OpenRead(path)))
            {
                string headers = r.ReadString();
                while (r.BaseStream.Position != r.BaseStream.Length)
                {
                    int routeID = r.ReadInt32();
                    int variantNumber = r.ReadInt32();
                    int arrLen = r.ReadInt32();
                    long[] arr = new long[arrLen];
                    for (int i = 0; i < arr.Length; i++)
                        arr[i] = r.ReadInt64();
                    yield return (routeID, variantNumber, arr);
                }
            }
        }

        public static readonly Dictionary<int, float> RouteClassWeights_ForBundledSwedenRouteData = new Dictionary<int, float>() { { 1, 1.35f }, { 10, 1.3f }, { 20, 0.8f }, { 50, 2f }, { 100, 1.1f }, { 200, 2f }, { 500, 0.7f }, { 10000, 0f } };

        public static IEnumerable<(int routeID, short vehicleTypeID, float annualMovementsLoad, float annualMovementsEmpty, float annualTonnes)> ReadRouteVehicleTypes(string routeVehicleTypePath, string? routeClassPath = null, Dictionary<int, float>? routeClassWeights = null)
        {
            if (routeClassWeights != null)
            {
                Console.WriteLine("NOTE: Traffic volumes are being rescaled based on custom weighting (route class weights)");
            }

            if (USE_CACHING && _cache.HasData(routeVehicleTypePath))
            {
                foreach (var item in _cache.Get<(int routeID, short vehicleTypeID, float annualMovementsLoad, float annualMovementsEmpty, float annualTonnes)>(routeVehicleTypePath))
                    yield return item;
            }
            else
            {
                Dictionary<int, int> routeClass = null;
                if (routeClassPath != null && routeClassWeights != null)
                    routeClass = DataReader.ReadClusterLengthClass(routeClassPath);

                using (StreamReader r = new StreamReader(File.OpenRead(routeVehicleTypePath)))
                {
                    string? line = r.ReadLine(); //route_id,vehicletype_id,annualmovements,annualtonnes
                    while ((line = r.ReadLine()) != null)
                    {
                        string[] parts = line.Split('\t');
                        int routeID = int.Parse(parts[0]);

                        float scaling = 1f;
                        if (routeClass != null && routeClassWeights != null)
                            scaling = routeClassWeights[routeClass[routeID]];

                        short vehicleTypeID = short.Parse(parts[1]);
                        float annualMovementsLoad = float.Parse(parts[2]) * scaling;
                        float annualMovementsEmpty = float.Parse(parts[3]) * scaling;
                        float annualTonnes = float.Parse(parts[4]) * scaling;
                        var item = (routeID, vehicleTypeID, annualMovementsLoad, annualMovementsEmpty, annualTonnes);
                        yield return item;
                        if (USE_CACHING) _cache.Append(routeVehicleTypePath, item);
                    }
                }
            }
        }

        public static IEnumerable<(int clusterID, long fromNodeID, long toNodeID, float distance_m, float speed_kmph, (long fromNodeID, long toNodeID)[] nodeSequence)> ReadClusterNodes(string path)
        {
            if (USE_CACHING && _cache.HasData(path))
            {
                foreach (var item in _cache.Get<(int clusterID, long fromNodeID, long toNodeID, float distance_m, float speed_kmph, (long fromNodeID, long toNodeID)[] nodeSequence)>(path))
                    yield return item;
            }
            else
            {
                using (BinaryReader r = new BinaryReader(File.OpenRead(path)))
                {
                    string headers = r.ReadString();
                    while (r.BaseStream.Position != r.BaseStream.Length)
                    {
                        int clusterID = r.ReadInt32();
                        long fromNodeID = r.ReadInt64();
                        long toNodeID = r.ReadInt64();
                        float distance_m = r.ReadSingle();
                        float speed_kmph = Math.Min(90, r.ReadSingle()); //Trucks are not allowed to drive faster than 90 km/h
                        int arrLen = r.ReadInt32();
                        (long, long)[] arr = new (long, long)[arrLen];
                        for (int i = 0; i < arr.Length; i++)
                            arr[i] = (r.ReadInt64(), r.ReadInt64());
                        if (float.IsNaN(speed_kmph))
                            speed_kmph = 50;
                        var item = (clusterID, fromNodeID, toNodeID, distance_m, speed_kmph, arr);
                        yield return item;
                        if (USE_CACHING) _cache.Append(path, item);
                    }
                }
            }
        }
        public static double UniformHash(uint x)
        {
            x = x + 31393;
            x = ((x >> 16) ^ x) * 0x45d9f3b;
            x = ((x >> 16) ^ x) * 0x45d9f3b;
            x = (x >> 16) ^ x;
            return (double)x / (double)uint.MaxValue;
        }
        public static IEnumerable<(int routeID, int variantNumber, int[] clusterSequence)> ReadClusterSequences(string path, double sampleRatio=1)
        {
            if (USE_CACHING && _cache.HasData(path))
            {
                foreach (var item in _cache.Get<(int routeID, int variantNumber, int[] clusterSequence)>(path))
                    yield return item;
            }
            else
            {            //Random random = new Random(0);
                using (BinaryReader r = new BinaryReader(File.OpenRead(path)))
                {
                    //string headers = r.ReadString();
                    while (r.BaseStream.Position != r.BaseStream.Length)
                    {
                        int routeID = r.ReadInt32();
                        int variantNumber = r.ReadInt32();
                        int arrLen = r.ReadInt32();
                        int[] arr = new int[arrLen];
                        for (int i = 0; i < arr.Length; i++)
                            arr[i] = r.ReadInt32();

#if DEBUGGING
                    if (routeID < 10000)
#else
                        double hash = UniformHash((uint)(routeID * 13 + variantNumber * 19));
                        if (hash < sampleRatio)
#endif
                        {
                            var item = (routeID, variantNumber, arr);
                            yield return item;
                            if (USE_CACHING) _cache.Append(path, item);
                        }
#if DEBUGGING
                    else { break; };
#endif
                    }
                }
            }
        }

        public static IEnumerable<(double latitude, double longitude)> ReadACEAChargeLocations(string path, bool swedenOnly)
        {
            using (StreamReader r = new StreamReader(File.OpenRead(path)))
            {
                string? line = r.ReadLine(); //header 1
                line = r.ReadLine(); //header 2
                line = r.ReadLine(); //latitude, longitude

                while ((line = r.ReadLine()) != null)
                {
                    string[] parts = line.Split('\t');
                    (double, double) coord;
                    try
                    {
                        coord = (double.Parse(parts[0]), double.Parse(parts[1]));
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Failed to parse \"" + line + "\" in " + path);
                        throw;
                    }
                    if (!swedenOnly || IsInSweden(coord.Item1, coord.Item2))
                        yield return coord;
                }
            }
        }

        public static IEnumerable<(int clusterID, int aceaPointID, float clusterMidLat, float clusterMidLon, float aceaPointLat, float aceaPointLon)> ReadClusterIDsWithAceaStopLocation(string path)
        {
            using (StreamReader r = new StreamReader(File.OpenRead(path)))
            {
                string? line = r.ReadLine(); //header

                while ((line = r.ReadLine()) != null)
                {
                    string[] parts = line.Split('\t');
                    yield return (
                        int.Parse(parts[0]),
                        int.Parse(parts[1]),
                        float.Parse(parts[2]),
                        float.Parse(parts[3]),
                        float.Parse(parts[4]),
                        float.Parse(parts[5]));
                }
            }
        }

        private static bool IsInSwedenBBox(double lat, double lon)
        {
            const double minLat = 55.3617373725, minLon = 11.0273686052, maxLat = 69.1062472602, maxLon = 23.9033785336;
            return lat > minLat && lon > minLon && lat < maxLat && lon < maxLon;
        }

        static PointF[] SwedenPolygon = new PointF[]
        {
            new PointF(55.35860787521675f, 12.78783708527213f),
            new PointF(56.06569526902317f, 12.628535338309211f),
            new PointF(58.829710793917876f, 10.330992045764336f),
            new PointF(59.23849705961967f, 11.531979275793333f),
            new PointF(60.280606754846836f, 12.437338892552985f),
            new PointF(63.31638056794665f, 11.855321982132375f),
            new PointF(68.132181398889f, 17.259764773975718f),
            new PointF(69.12843383853036f, 20.474715487011057f),
            new PointF(68.18373823798812f, 23.65271252785373f),
            new PointF(65.79516391587467f, 24.216252689636566f),
            new PointF(61.34216173528727f, 18.58322542534182f),
            new PointF(58.04900825789566f, 20.38619798151363f),
            new PointF(55.27910719025753f, 16.388302041698203f),
            new PointF(55.35860787521675f, 12.78783708527213f)
        };

        //static Wibci.CountryReverseGeocode.CountryReverseGeocodeService _geoService;
        public static bool IsInSweden(double latitude, double longitude)
        {
            //Elområde lat <57.22 4, <60.88 3, <64.14 2, > 1
            //_geoService ??= new Wibci.CountryReverseGeocode.CountryReverseGeocodeService();
            //var country = _geoService.FindCountry(new Wibci.CountryReverseGeocode.Models.GeoLocation() { Latitude = latitude, Longitude = longitude });
            //if (country != null)
            //    return (country.Name == "Sweden");
            //else
            //    return IsInSwedenBBox(latitude, longitude);
            return IsPointInPolygon4(SwedenPolygon, new PointF((float)latitude, (float)longitude));
        }

        /// <summary>
        /// Determines if the given point is inside the polygon
        /// </summary>
        /// <param name="polygon">the vertices of polygon</param>
        /// <param name="testPoint">the given point</param>
        /// <returns>true if the point is inside the polygon; otherwise, false</returns>
        public static bool IsPointInPolygon4(PointF[] polygon, PointF testPoint)
        {
            bool result = false;
            int j = polygon.Count() - 1;
            for (int i = 0; i < polygon.Count(); i++)
            {
                if (polygon[i].Y < testPoint.Y && polygon[j].Y >= testPoint.Y || polygon[j].Y < testPoint.Y && polygon[i].Y >= testPoint.Y)
                {
                    if (polygon[i].X + (testPoint.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) * (polygon[j].X - polygon[i].X) < testPoint.X)
                    {
                        result = !result;
                    }
                }
                j = i;
            }
            return result;
        }

        public static IEnumerable<(string routeID, float fromMeasure, float toMeasure, float 
            Total, float aadtHeavy, float shapeLength_meter, (double lat, double lon)[] geometry, float[] directions)> ReadAadtData(string path)
        {
            Regex oneValuePattern = new Regex(@"\d+\.\d+", RegexOptions.Compiled);
            Regex twoValuePattern = new Regex(@"(?'lat'\d+\.\d+),\s(?'lon'\d+\.\d+)", RegexOptions.Compiled);

            using (StreamReader r = new StreamReader(File.OpenRead(path)))
            {
                string? line = r.ReadLine(); //header 1

                while ((line = r.ReadLine()) != null)
                {
                    string[] parts = line.Split(';');

                    if (!parts.Select(n => n.Length > 0).All(n => n))
                        continue;

                    string routeID = parts[0];
                    float fromMeasure = float.Parse(parts[1]);
                    float toMeasure = float.Parse(parts[2]);
                    float aadtTotal = float.Parse(parts[3]);
                    float aadtHeavy = float.Parse(parts[4]);
                    float shapeLength_meter = float.Parse(parts[5]);
                    var geometry = twoValuePattern.Matches(parts[6]).Select(m => (double.Parse(m.Groups[1].Value), double.Parse(m.Groups[2].Value))).ToArray();
                    var directions = oneValuePattern.Matches(parts[7]).Select(m => float.Parse(m.Value)).ToArray();

                    yield return (routeID, fromMeasure, toMeasure, aadtTotal, aadtHeavy, shapeLength_meter, geometry, directions);
                }
            }
        }

        public static IEnumerable<long> ReadBidirectionalNodes(string path)
        {
            using (StreamReader r = new StreamReader(File.OpenRead(path)))
            {
                string? line;
                while ((line = r.ReadLine()) != null)
                {
                    yield return long.Parse(line);
                }
            }
        }

        public static Dictionary<int, int> ReadClusterLengthClass(string path)
        {
            Dictionary<int, int> data = new Dictionary<int, int>();
            using (StreamReader r = new StreamReader(File.OpenRead(path)))
            {
                string? line = r.ReadLine(); //skip header
                while ((line = r.ReadLine()) != null)
                {
                    string[] parts = line.Split('\t');
                    data.Add(int.Parse(parts[0]), int.Parse(parts[1]));
                }
            }
            return data;
        }

        // KFF: I have not changed this function to use .tsv format since it appears not to be used and it is not clear that it will not be used with externally produced .csv files
        public static Dictionary<int, (float annualMovements, float annualtonnes, float annualMovementsFromAadt)> ReadClustersWithTrvAadtData(string clusterSeqPath, string matchedAadtPath, string routeVehicleTypePath)
        {
            Console.WriteLine("Importing matched AADT data...");
            Dictionary<int, float> matchedAadt = new Dictionary<int, float>();
            using (StreamReader r = new StreamReader(File.OpenRead(matchedAadtPath)))
            {
                string? line = r.ReadLine();
                while ((line = r.ReadLine()) != null)
                {
                    string[] parts = line.Split('\t');
                    matchedAadt.Add(int.Parse(parts[0]), float.Parse(parts[1]));
                }
            }

            ConcurrentDictionary<int, (float annualMovements, float annualTonnes)> sumPerCluster = ReadModelTrafficPerCluster(clusterSeqPath, routeVehicleTypePath);

            return sumPerCluster.ToDictionary(n => n.Key, n =>
            {
                matchedAadt.TryGetValue(n.Key, out float aadt);
                return (n.Value.annualMovements, n.Value.annualTonnes, aadt * 365);
            });
        }

        public static Dictionary<int, (float annual_movements, float aadt, float speed_kmph, float hours_with_vehicle_per_day, float length_m, bool bidirectional, float from_lat, float from_lon, float to_lat, float to_lon, bool is_in_sweden_bbox)> ReadAnnualMovementsAndLengthPerCluster(string path)
        {
            Dictionary<int, (float annual_movements, float aadt, float speed_kmph, float hours_with_vehicle_per_day, float length_m, bool bidirectional, float from_lat, float from_lon, float to_lat, float to_lon, bool is_in_sweden_bbox)> data
                = new Dictionary<int, (float annual_movements, float aadt, float speed_kmph, float hours_with_vehicle_per_day, float length_m, bool bidirectional, float from_lat, float from_lon, float to_lat, float to_lon, bool is_in_sweden_bbox)>();
            using (StreamReader r = new StreamReader(File.OpenRead(path)))
            {
                string? line = r.ReadLine(); //skip header
                while ((line = r.ReadLine()) != null)
                {
                    string[] parts = line.Split('\t');
                    var values = (float.Parse(parts[1]),
                        float.Parse(parts[2]),
                        float.Parse(parts[3]),
                        float.Parse(parts[4]),
                        float.Parse(parts[5]),
                        bool.Parse(parts[6]),
                        float.Parse(parts[7]),
                        float.Parse(parts[8]),
                        float.Parse(parts[9]),
                        float.Parse(parts[10]),
                        bool.Parse(parts[11]));
                    if (float.IsNaN(values.Item1 + values.Item2 + values.Item3 + values.Item4 + values.Item5 + values.Item7 + values.Item8 + values.Item9))
                        Console.WriteLine("oh");
                    data.Add(int.Parse(parts[0]), values);
                }
            }
            return data;
        }

        public static ConcurrentDictionary<int, (float annualMovements, float annualTonnes)> ReadModelTrafficPerCluster(string clusterSeqPath, string routeVehicleTypePath, string? routeClassPath = null, Dictionary<int, float>? routeClassWeights = null)
        {
            Console.WriteLine("Calculating annual movements per route...");
            Dictionary<int, (float annualMovements, float annualTonnes)> routeVolume = DataReader.ReadRouteVehicleTypes(routeVehicleTypePath, routeClassPath, routeClassWeights)
                .GroupBy(n => n.routeID)
                .ToDictionary(g => g.Key, g => (g.Sum(i => i.annualMovementsLoad + i.annualMovementsEmpty), g.Sum(i => i.annualTonnes)));

            Console.WriteLine("Counting variants per route...");
            Dictionary<int, int>
                variantsPerRoute = DataReader.ReadClusterSequences(clusterSeqPath).Select(n => n.routeID).GroupBy(n => n).ToDictionary(g => g.Key, g => g.Count());

            Console.Write("Summing annualMovements per road segment...");
            ConcurrentDictionary<int, (float annualMovements, float annualTonnes)> sumPerCluster = new ConcurrentDictionary<int, (float annualMovements, float annualTonnes)>();
            Parallel.ForEach(DataReader.ReadClusterSequences(clusterSeqPath), seq =>
            {
                float scaling = 1f / variantsPerRoute[seq.routeID];

                var volume = routeVolume[seq.routeID];
                volume = (volume.annualMovements * scaling, volume.annualTonnes * scaling);
                
                foreach (int clusterID in seq.clusterSequence)
                {
                    sumPerCluster.AddOrUpdate(
                        clusterID,
                        (volume.annualMovements, volume.annualTonnes),
                        (key, old) => (old.annualMovements + volume.annualMovements, old.annualTonnes + volume.annualTonnes));
                }
            });
            Console.WriteLine("done!");
            return sumPerCluster;
        }
    
        public static Dictionary<int, List<(int Index, float Ratio, float Meters)>> ReadClusterToWeightedGridCell(string path)
        {
            var data = new Dictionary<int, List<(int, float, float)>>();
            using (StreamReader r = new StreamReader(File.OpenRead(path)))
            {
                string? line = r.ReadLine(); //skip header
                while ((line = r.ReadLine()) != null)
                {
                    string[] parts = line.Split('\t');
                    int clusterID = int.Parse(parts[0]);
                    if (!data.ContainsKey(clusterID))
                        data[clusterID] = new List<(int, float, float)>();
                    data[clusterID].Add((int.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3])));
                }
            }
            return data;
        }

        public static Dictionary<int, List<(string Index, float Ratio, float Meters)>> ReadClusterToWeightedGridCell_stringID(string path)
        {
            var data = new Dictionary<int, List<(string, float, float)>>();
            using (StreamReader r = new StreamReader(File.OpenRead(path)))
            {
                string line = r.ReadLine(); //skip header
                while ((line = r.ReadLine()) != null)
                {
                    string[] parts = line.Split('\t');
                    int clusterID = int.Parse(parts[0]);
                    if (!data.ContainsKey(clusterID))
                        data[clusterID] = new List<(string, float, float)>();
                    data[clusterID].Add((parts[1], float.Parse(parts[2]), float.Parse(parts[3])));
                }
            }
            return data;
        }
    }
}
