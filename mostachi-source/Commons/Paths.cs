using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Xml;

namespace Commons
{
    public static class Paths
    {
        public static class RootPaths
        {
            private static bool _isInitialized = false;

            private static string _dataRoot = "";
            private static string _generatedDataRoot = "";
            private static string _logsRoot = "";
            private static string _osrm = "";

            public static string DataRoot { get { if (!_isInitialized) Initialize(); return _dataRoot; } }
            public static string GeneratedDataRoot { get { if (!_isInitialized) Initialize(); return _generatedDataRoot; } }
            public static string LogsRoot { get { if (!_isInitialized) Initialize(); return _logsRoot; } }
            public static string Osrm { get { if (!_isInitialized) Initialize(); return _osrm; } }

            // Normalizing allows 'paths.xml' containing Windows style '\' as well as Linux style '/'
            public static string Normalize(string path)
            {
              return path.Replace('\\', '/');
            }

            static void Initialize()
            {
                XmlDocument doc = new XmlDocument();
                doc.Load("paths.xml");
                _dataRoot = Normalize(doc.DocumentElement?.SelectSingleNode("/paths/data")?.InnerText ?? "");
                _generatedDataRoot = _dataRoot + "generated" + Path.DirectorySeparatorChar;
                _logsRoot = Normalize(doc.DocumentElement?.SelectSingleNode("/paths/logs")?.InnerText ?? "");
                _osrm = doc.DocumentElement?.SelectSingleNode("/paths/osrm")?.InnerText ?? "";

                Directory.CreateDirectory(_logsRoot);
                Directory.CreateDirectory(_generatedDataRoot);

                _isInitialized = true;
            }
        }

        public static readonly string Places = RootPaths.DataRoot + "places.tsv";
        public static readonly string OriginDestinationDensity = RootPaths.DataRoot + "truck_origin_destination_density_grid.tsv";
        public static readonly string Routes = RootPaths.DataRoot + "routes.tsv";
        public static readonly string RouteVehicleType = RootPaths.DataRoot + "routevehicletype.tsv";
        public static readonly string AceaLongHaulStopCoordinates = RootPaths.DataRoot + "acea_long_haul_stop_locations.tsv";
        public static readonly string AceaRegionalStopCoordinates = RootPaths.DataRoot + "acea_regional_stop_locations.tsv";

        public static readonly string ClusterSequences = RootPaths.GeneratedDataRoot + "clustersequences.bin";
        public static readonly string Nodes = RootPaths.GeneratedDataRoot + "nodes.bin";
        public static readonly string ClusterNodePairs = RootPaths.GeneratedDataRoot + "clusternodepairs.bin";
        public static readonly string BidirectionalNodes = RootPaths.GeneratedDataRoot + "bidirectionalnodes.tsv";
        public static readonly string AceaAllStopClusters = RootPaths.GeneratedDataRoot + "acea_all_stop_location_matches.tsv";
        public static readonly string NodePairs = RootPaths.GeneratedDataRoot + "nodepairs.bin";
        public static readonly string NodePairsClusters = RootPaths.GeneratedDataRoot + "nodepairclusters.bin";
        public static readonly string NodeSequences = RootPaths.GeneratedDataRoot + "nodesequences.bin";
        public static readonly string RouteLengthClass = RootPaths.GeneratedDataRoot + "route_length_class.tsv";
        public static readonly string AnnualMovementsAndLengthPerCluster = RootPaths.GeneratedDataRoot + "cluster_traffic_and_length.tsv";
        public static readonly string ErsBuildOrder = RootPaths.GeneratedDataRoot + "ers_build_order.tsv";
        public static readonly string ClusterToWeightedGridCells = RootPaths.GeneratedDataRoot + "cluster_to_weighted_grid_cell.tsv";
        public static readonly string ClusterToWeightedGridCells_Sweref99Tm = RootPaths.GeneratedDataRoot + "cluster_to_weighted_grid_cell_sweref99tm.tsv";

        public static readonly string Error = RootPaths.LogsRoot + "error.txt";


        public static string ExperimentLogsDir = RootPaths.LogsRoot;
        public static string ExperimentLog = RootPaths.LogsRoot + "experiment_log_" + DateTime.Now.ToString("yyyy-MM-dd HH-mm").Replace(' ', '_') + ".txt";
        public static string RunLog = RootPaths.LogsRoot + "run_log_" + DateTime.Now.ToString("yyyy-MM-dd HH-mm").Replace(' ', '_') + ".txt";

        public static void ResetExperimentLogPath(string experimentName = "")
        {
            string s = experimentName.Replace(' ', '_');
            Directory.CreateDirectory(RootPaths.LogsRoot + s);
            ExperimentLogsDir = RootPaths.LogsRoot + s + Path.DirectorySeparatorChar;
            ExperimentLog = RootPaths.LogsRoot + s + Path.DirectorySeparatorChar + "experiment_log_" + s + '_' + DateTime.Now.ToString("yyyy-MM-dd HH-mm").Replace(' ', '_') + ".txt";
        }
    }
}
