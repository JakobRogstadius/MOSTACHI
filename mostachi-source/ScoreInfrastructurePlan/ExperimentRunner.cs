using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Commons;
using System.IO;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Reflection.PortableExecutable;
using System.Diagnostics;
using System.Runtime;
using System.Security.Cryptography;

namespace ScoreInfrastructurePlan
{
    class ExperimentRunner
    {
        public static void Run()
        {
            (var scenarios, var sampleRatio, var logSettings) = ExperimentSetup.GetScenarios();

            //Run experiments
            CalculateScenarios(scenarios, sampleRatio, logSettings);

            /* Uncomment this to sample many routes and calculate transport cost per km for different configurations */
            //CalculateCostPerKmPerBatteryCapacityAndChargingStrategy();

            Console.WriteLine();
            Console.WriteLine("All finished!");
        }

        private static void CalculateScenarios(List<Scenario> scenarios, Dimensionless sampleRatio, LogSettings logSettings)
        {
            Console.WriteLine("Simulating " + scenarios.Count + " scenarios, in total " + scenarios.Sum(n => n.SimStartYear.GetSequence(n.SimEndYear).Length) + " 5-year periods.\n");

            Console.WriteLine("Loading data from disk into RAM...");

            Console.WriteLine("  Indexing all route segments...");
            (var routeSegments, var adjacentRestAreas) = GetRouteSegments();

            Console.WriteLine("  Finding all route-cell combinations...");
            Console.WriteLine("    Converting nodes to CoordinateHashes (1/3)");
            var nodes = DataReader.ReadNodes(Paths.Nodes).ToDictionary(n => n.nodeID, n => new CoordinateHash(n.latitude, n.longitude));
            Console.WriteLine("    Converting clusters to CoordinateHash sets (2/3)");
            var clusters = DataReader.ReadClusterNodes(Paths.ClusterNodePairs).ToDictionary(n => n.clusterID, n => n.nodeSequence.Select(m => nodes[m.fromNodeID]).Distinct().ToArray());
            Console.WriteLine("    Converting cluster sequences to CoordinateHash sets (3/3)");
            var routeVariantCellSequences = DataReader.ReadClusterSequences(Paths.ClusterSequences, sampleRatio.Val).AsParallel()
                .ToDictionary(n => (n.routeID, n.variantNumber), n => n.clusterSequence.SelectMany(m => clusters[m]).Distinct().ToArray());

            Console.WriteLine("  Counting variants per route...");
            Dictionary<int, int> variantsPerRoute
                = DataReader.ReadClusterSequences(Paths.ClusterSequences).Select(n => n.routeID).GroupBy(n => n).ToDictionary(g => g.Key, g => g.Count());

            Console.WriteLine();
            Console.WriteLine("Done initializing");

            Stopwatch watch = new Stopwatch();
            watch.Start();
            int i = 0;
            foreach (Scenario scenario in scenarios)
            {
                if (watch.Elapsed.TotalSeconds > 60)
                {
                    double progress = i / (double)scenarios.Sum(n => n.SimStartYear.GetSequence(scenario.SimEndYear).Length);
                    double totalTimeEstimate = watch.Elapsed.TotalSeconds / progress;
                    TimeSpan remaining = new TimeSpan(0, 0, (int)totalTimeEstimate);

                    Console.WriteLine();
                    Console.WriteLine("Estimated time remaining: " + remaining);
                }

                scenario.Before();

                Console.WriteLine();
                Console.WriteLine("Evaluating " + scenario.Name + "...");
                ResetAll(scenario, routeSegments);

                Console.WriteLine("  Applying roll-out plan...");
                ApplyPublicInfrastructureRolloutPlan(routeSegments, adjacentRestAreas, scenario);

                Console.WriteLine("  Scoring years...");
                var movementPatternIterator = GetMovements_AndCreateDepotAndDestinationSegments(routeSegments, adjacentRestAreas, variantsPerRoute, scenario);
                var modelYearResults = MultiYearScorer.ScoreYears(
                    movementPatternIterator,
                    scenario);

                PrintExperimentLog(scenario, modelYearResults, routeVariantCellSequences, logSettings);

                scenario.After();

                Console.WriteLine("  Finished evaluating \"" + scenario.Name + "\"");

                i += scenario.SimStartYear.GetSequence(scenario.SimEndYear).Length;
            }
        }

        /// <summary>
        /// This calculation computes the cost of traversing each route (1% sample) using each battery capacity and each charging strategy.
        /// Edit the function to adjust the charging infrastructure scenario used in the calculation.
        /// </summary>
        private static void CalculateCostPerKmPerBatteryCapacityAndChargingStrategy()
        {
            Console.WriteLine("Indexing all route segments...");
            (var routeSegments, var adjacentRestAreas) = GetRouteSegments();

            Console.WriteLine("Counting variants per route...");
            Dictionary<int, int> variantsPerRoute
                = DataReader.ReadClusterSequences(Paths.ClusterSequences).Select(n => n.routeID).GroupBy(n => n).ToDictionary(g => g.Key, g => g.Count());

            string scenarioName = "battery_and_charging_strategy_impact_";
            var sampleRatio = new Dimensionless(0.01f);
            var scenario = Experiments.Custom(sampleRatio, scenarioName, (ModelYear.Y2020, ModelYear.Y2050),
                (new Kilometers(3000), new KiloWatts(500), new Dimensionless(0.35f)),
                (ModelYear.Y2020, ModelYear.Y2020, new Dimensionless(0.8f)),
                (ModelYear.Y2020, ModelYear.Y2020, new Dimensionless(0.25f)),
                (ModelYear.Y2020, ModelYear.Y2020),
                (ModelYear.Y2020, ModelYear.Y2020, new Dimensionless(1f)));

            (scenario.ChargingStrategies).Add(ChargingStrategy.Ers);
            (scenario.ChargingStrategies).Add(ChargingStrategy.PublicStaticCharging);
            ApplyPublicInfrastructureRolloutPlan(routeSegments, adjacentRestAreas, scenario);
            var movementPatternIterator = GetMovements_AndCreateDepotAndDestinationSegments(routeSegments, adjacentRestAreas, variantsPerRoute, scenario); //This creates depot and destination segments

            foreach (ModelYear year in new ModelYear[] { ModelYear.Y2020, ModelYear.Y2035, ModelYear.Y2050 })
            {
                scenario.Name = scenarioName + year;
                Paths.ResetExperimentLogPath(scenario.Name);
                InfrastructureCost.ResetCache();

                SingleYearScorer.SimulateUntilConvergence(year, movementPatternIterator, scenario, new Dictionary<RouteSegment, (KiloWatts, KiloWattsPerKilometer)>(), onlyPrecomputeCostsUntilConvergence: true);

                StringBuilder sbLines = new StringBuilder();
                sbLines.AppendLine("route\tvariant\tvehicle_type\tcharging_strategy\tbatteryCapacity\t" + RouteCost.ToStringHeader);
                string nullStr = "\t\t\t\t\t\t\t\t\t\t";
                Parallel.ForEach(movementPatternIterator, route_vt =>
                {
                    foreach (var strategy in scenario.ChargingStrategies)
                    {
                        foreach (var netBatteryCapacity_kWh in scenario.BatteryOffers[route_vt.VehicleType])
                        {
                            var routeCost = SingleYearScorer.GetRouteTraversalCost_perKm(year, route_vt, scenario, strategy, netBatteryCapacity_kWh);
                            var costStr = routeCost != null ? routeCost.ToStringPerKm() : nullStr;
                            lock (sbLines)
                                sbLines.AppendLine(route_vt.Route.ID + "\t" + route_vt.Route.VariantNo + "\t" + route_vt.VehicleType + "\t" + strategy + "\t" + netBatteryCapacity_kWh + "\t" + costStr);
                            if (strategy == ChargingStrategy.NA_Diesel)
                                break;
                        }
                    }
                });
                File.WriteAllText(Paths.ExperimentLog, sbLines.ToString());
            }
        }

        private static void ResetAll(Scenario newScenario, Dictionary<int, RouteSegment> routeSegments)
        {
            Paths.ResetExperimentLogPath(newScenario.Name);
            InfrastructureCost.ResetCache();
            foreach (var segment in routeSegments)
                segment.Value.ChargingOfferedFromYear = ModelYear.N_A;
        }

        static readonly ConcurrentDictionary<CoordinateHash, RouteSegment> _depots = new ConcurrentDictionary<CoordinateHash, RouteSegment>();
        static readonly ConcurrentDictionary<CoordinateHash, RouteSegment> _destinations = new ConcurrentDictionary<CoordinateHash, RouteSegment>();

        static ModelYear SampleBuildYear(BuildPeriod period, int id)
        {
            var p = (float)Hashes.UniformHash((uint)id);
            if (p >= period.FinalRatio.Val)
                return ModelYear.N_A;
            var years = period.From.GetSequence(period.To);
            p /= period.FinalRatio.Val;
            return years[(int)(p * years.Length)];
        }

        public static IEnumerable<Route_VehicleType> GetMovements_AndCreateDepotAndDestinationSegments(
            Dictionary<int, RouteSegment> routeSegments,
            Dictionary<int, int> adjacentRestAreas,
            Dictionary<int, int> variantsPerRoute,
            Scenario scenario)
        {
            Dictionary<int, float> routeClassWeights = null; // new Dictionary<int, float>() { { 1, 1.35f }, { 10, 1.3f }, { 20, 0.8f }, { 50, 2f }, { 100, 1.1f }, { 200, 2f }, { 500, 0.7f }, { 10000, 0f } };

            var routeVehicleTypes = DataReader.ReadRouteVehicleTypes(Paths.RouteVehicleType, Paths.RouteLengthClass, routeClassWeights)
                .GroupBy(n => n.routeID).
                ToDictionary(g => g.Key, g => g.ToDictionary(n => n.vehicleTypeID, n => (n.annualMovementsEmpty, n.annualMovementsLoad, n.annualTonnes)));

            Console.WriteLine("Generating Route_VehicleType items...");
            int nextID = routeSegments.Keys.Max() + 1;
            foreach (var seq in DataReader.ReadClusterSequences(Paths.ClusterSequences, scenario.MovementSampleRatio.Val))
            {
                if (!routeVehicleTypes.ContainsKey(seq.routeID))
                    continue;

                var firstRoadSegment = routeSegments[seq.clusterSequence.First()];
                var lastRoadSegment = routeSegments[seq.clusterSequence.Last()];
                var depot = _depots.GetOrAdd(firstRoadSegment.PlaceHash,
                    p =>
                    {
                        int id = nextID++;
                        var s = new RouteSegment()
                        {
                            ID = id,
                            PlaceHash = p,
                            Region = firstRoadSegment.Region,
                            Type = RouteSegmentType.Depot,
                            ChargingOfferedFromYear = SampleBuildYear(scenario.DepotBuildYear, id),
                            LengthToTraverseOneWay_km = new Kilometers(0),
                            Speed_Kmph = new KilometersPerHour(0)
                        };
                        routeSegments.Add(s.ID, s);
                        return s;
                    });
                var destination = _destinations.GetOrAdd(lastRoadSegment.PlaceHash,
                    i =>
                    {
                        int id = nextID++;
                        var s = new RouteSegment()
                        {
                            ID = id,
                            PlaceHash = i,
                            Region = lastRoadSegment.Region,
                            Type = RouteSegmentType.Destination,
                            ChargingOfferedFromYear = SampleBuildYear(scenario.DestinationBuildYear, id),
                            LengthToTraverseOneWay_km = new Kilometers(0),
                            Speed_Kmph = new KilometersPerHour(0)
                        };
                        routeSegments.Add(s.ID, s);
                        return s;
                    });

                List<int> modifiedSeq = new List<int>();
                HashSet<int> passedRestAreas = new HashSet<int>();
                modifiedSeq.Add(depot.ID);
                foreach (var id in seq.clusterSequence)
                {
                    //This means we skip all parts of routes that are outside of Sweden. Only the part inside Sweden matters in the battery and cost calculations.
                    if (!routeSegments[id].PlaceHash.IsInSweden)
                        continue;

                    if (adjacentRestAreas.TryGetValue(id, out int restAreaID))
                    {
                        if (passedRestAreas.Add(restAreaID))
                            modifiedSeq.Add(restAreaID);
                    }
                    modifiedSeq.Add(id);
                }
                modifiedSeq.Add(destination.ID);

                Dimensionless scaleFactor = 1f / (variantsPerRoute[seq.routeID] * scenario.MovementSampleRatio);
                var segmentSequence = modifiedSeq.Select(n => routeSegments[n]).ToArray();
                foreach (var vtype in routeVehicleTypes[seq.routeID])
                {
                    if (vtype.Key == 101) //LGV3 & noise
                        continue;

                    var r_vt = new Route_VehicleType()
                    {
                        VehicleType = Parameters.VehicleTypes[vtype.Key],
                        CargoTonnesPerYear2020 = new OtherUnit(vtype.Value.annualTonnes * scaleFactor.Val),
                        LoadedTripCountPerYear2020 = new OtherUnit(vtype.Value.annualMovementsLoad * scaleFactor.Val),
                        EmptyTripCountPerYear2020 = new OtherUnit(vtype.Value.annualMovementsEmpty * scaleFactor.Val),
                        Route = new Route()
                        {
                            ID = seq.routeID,
                            VariantNo = seq.variantNumber,
                            SegmentSequence = segmentSequence
                        }
                    };

                    if (r_vt.IsOk())
                    {
                        yield return r_vt;
                    }
                }
            }
        }

        public static (Dictionary<int, RouteSegment> allSegments, Dictionary<int, int> clusterToRestArea) GetRouteSegments()
        {
            var bidirectionalNodes = DataReader.ReadBidirectionalNodes(Paths.BidirectionalNodes).ToHashSet();

            var nodes = DataReader.ReadNodes(Paths.Nodes).ToDictionary(n => n.nodeID, n => (n.latitude, n.longitude, isBidirectional: bidirectionalNodes.Contains(n.nodeID)));

            Dictionary<int, RouteSegment> segments = new Dictionary<int, RouteSegment>();

            //Add roads
            foreach (var cluster in DataReader.ReadClusterNodes(Paths.ClusterNodePairs))
            {
                var startNode = nodes[cluster.fromNodeID];
                segments[cluster.clusterID] = new RouteSegment()
                {
                    ID = cluster.clusterID,
                    Type = RouteSegmentType.Road,
                    LengthToTraverseOneWay_km = new Kilometers(cluster.distance_m / 1000f),
                    Speed_Kmph = new KilometersPerHour(cluster.speed_kmph),
                    Region = GetRegion((startNode.latitude, startNode.longitude)),
                    PlaceHash = new CoordinateHash(startNode.latitude, startNode.longitude),
                    IsBidirectional = startNode.isBidirectional
                };
            }

            //Add major rest stops
            Dictionary<int, int> clusterToRestArea = new Dictionary<int, int>();
            Dictionary<int, RouteSegment> restAreasByID = new Dictionary<int, RouteSegment>();
            var restStopClusters = DataReader.ReadClusterIDsWithRestStopLocation(Paths.RestStopClusters)
                .GroupBy(n => n.clusterID)
                .ToDictionary(g => g.Key, g => g.Select(n => n.restPointID).Min());
            int nextID = segments.Keys.Max() + 1;
            foreach (var restArea in restStopClusters)
            {
                if (restAreasByID.ContainsKey(restArea.Value))
                {
                    clusterToRestArea.Add(restArea.Key, restAreasByID[restArea.Value].ID);
                }
                else
                {
                    int segmentID = nextID++;
                    segments.Add(segmentID, new RouteSegment()
                    {
                        ID = segmentID,
                        Type = RouteSegmentType.RestStop,
                        Region = segments[restArea.Key].Region,
                        Speed_Kmph = new KilometersPerHour(0),
                        LengthToTraverseOneWay_km = new Kilometers(0),
                        PlaceHash = segments[restArea.Key].PlaceHash
                    });
                    clusterToRestArea.Add(restArea.Key, segmentID);
                    restAreasByID.Add(restArea.Value, segments[segmentID]);
                }
            }

            return (segments, clusterToRestArea);
        }

        static ElectricityPriceRegion GetRegion((double latitude, double longitude) point)
        {
            //Elområde lat <57.22 4, <60.88 3, <64.14 2, > 1
            if (!DataReader.IsInSweden(point.latitude, point.longitude))
                return ElectricityPriceRegion.Other;
            else if (point.latitude < 60.88)
                return ElectricityPriceRegion.SE12;
            else
                return ElectricityPriceRegion.SE34;
        }

        static void ApplyPublicInfrastructureRolloutPlan(Dictionary<int, RouteSegment> allSegments, Dictionary<int, int> clusterToRestArea, Scenario scenario)
        {
            List<ModelYear> modelYears = new List<ModelYear>();
            foreach (ModelYear year in Enum.GetValues(typeof(ModelYear)))
                modelYears.Add(year);

            Years ersBuildYearCount = new Years(modelYears.Where(n => n >= scenario.ErsBuildYear.From && n <= scenario.ErsBuildYear.To).Count());
            KilometersPerYear ersKmPerYear = scenario.InfraOffers.FinalErsNetworkScope_km / ersBuildYearCount;
            ModelYear currentErsYear = scenario.ErsBuildYear.From;
            Kilometers currentErsNetworkSize_km = new Kilometers(0);
            Kilometers currentErsNetworkWithCoverage_km = new Kilometers(0);
            Kilometers endOfYearErsLength = new Kilometers(ersKmPerYear.Val);

            //BUG: I'm not sure, but I suspect there is a bug here somewhere. When I compare BEV share with 2k and 6k km ERS,
            //the BEV share is identical after the first build year, even though network sizes should differ.

            //Load the build order. This list contains ONLY road segments.
            if (scenario.ErsBuildYear.From != ModelYear.N_A && scenario.InfraOffers.FinalErsNetworkScope_km > 0)
            {
                List<int> clusterIDsInBuildOrder = File.ReadAllLines(Paths.ErsBuildOrder).Select(n => int.Parse(n)).ToList();
                foreach (int clusterID in clusterIDsInBuildOrder)
                {
                    RouteSegment roadSegment = allSegments[clusterID];
                    if (currentErsNetworkSize_km >= scenario.InfraOffers.FinalErsNetworkScope_km)
                        break;

                    if (currentErsNetworkSize_km == 0 || currentErsNetworkWithCoverage_km / currentErsNetworkSize_km < scenario.InfraOffers.ErsCoverageRatio)
                    {
                        roadSegment.ChargingOfferedFromYear = currentErsYear;
                        currentErsNetworkWithCoverage_km += roadSegment.BidirectionalRoadLengthToElectrify_km;
                    }
                    currentErsNetworkSize_km += roadSegment.BidirectionalRoadLengthToElectrify_km;
                    if (endOfYearErsLength <= currentErsNetworkSize_km && currentErsNetworkSize_km < scenario.InfraOffers.FinalErsNetworkScope_km)
                    {
                        currentErsYear = currentErsYear.Next();
                        endOfYearErsLength += new Kilometers(ersKmPerYear.Val);
                    }
                }
            }

            if (scenario.StationBuildYear.From != ModelYear.N_A)
            {
                int sign = scenario.ReverseStationOrder ? -1 : 1;
                var restAreaIDsInAadtOrderWithDuplicates = DataReader.ReadAnnualMovementsAndLengthPerCluster(Paths.AnnualMovementsAndLengthPerCluster)
                                        .OrderByDescending(n => sign * n.Value.annual_movements)
                                        .Select(n => n.Key)
                                        .Select(id => clusterToRestArea.TryGetValue(id, out int restAreaID) ? restAreaID : -1)
                                        .OrderBy(n => n.GetHashCode()); //TODO: This shuffles the list to build fast charging stations in (fixed) random order. Keep it?
                List<int> restAreaIDsInBuildOrder = new List<int>();
                foreach (int id in restAreaIDsInAadtOrderWithDuplicates)
                {
                    if (id != -1 && !restAreaIDsInBuildOrder.Contains(id))
                        restAreaIDsInBuildOrder.Add(id);
                }
                ApplySiteRolloutPlan(allSegments, modelYears, restAreaIDsInBuildOrder, scenario.StationBuildYear);
            }

            if (scenario.DepotBuildYear.From != ModelYear.N_A)
            {
                foreach (var depot in allSegments.Values.Where(n => n.Type == RouteSegmentType.Depot))
                    depot.ChargingOfferedFromYear = SampleBuildYear(scenario.DepotBuildYear, depot.ID);
            }

            if (scenario.DestinationBuildYear.From != ModelYear.N_A)
            {
                foreach (var dest in allSegments.Values.Where(n => n.Type == RouteSegmentType.Destination))
                    dest.ChargingOfferedFromYear = SampleBuildYear(scenario.DestinationBuildYear, dest.ID);
            }
        }

        private static void ApplySiteRolloutPlan(Dictionary<int, RouteSegment> allSegments, List<ModelYear> modelYears, List<int> clusterIDsInBuildOrder, BuildPeriod period)
        {
            clusterIDsInBuildOrder = clusterIDsInBuildOrder.Take((int)(period.FinalRatio.Val * clusterIDsInBuildOrder.Count)).ToList();
            int siteBuildYearCount = modelYears.Where(n => n >= period.From && n <= period.To).Count();
            int sitesPerYear = clusterIDsInBuildOrder.Count / siteBuildYearCount;
            int finalSiteCount = sitesPerYear * siteBuildYearCount; //a few may not be built due to rounding errors
            ModelYear currentSiteYear = period.From;
            int currentSiteCount = 0, endOfYearSiteCount = sitesPerYear;

            foreach (int clusterID in clusterIDsInBuildOrder)
            {
                if (currentSiteCount >= finalSiteCount)
                    break;

                RouteSegment site = allSegments[clusterID];

                site.ChargingOfferedFromYear = currentSiteYear;
                currentSiteCount++;
                if (endOfYearSiteCount == currentSiteCount && currentSiteCount < finalSiteCount)
                {
                    currentSiteYear = currentSiteYear.Next();
                    endOfYearSiteCount += sitesPerYear;
                }
            }
        }

        private static void PrintExperimentLog(Scenario scenario,
            IEnumerable<(ModelYear year, RouteVehicleTypeBehavior routeStrategies, Dictionary<RouteSegment, ChargingSiteCost> infraSet)> modelYearResults,
            Dictionary<(int routeID, int variantNumber), CoordinateHash[]> routeVariantCellSequences,
            LogSettings logSettings
            )
        {
            StringBuilder sbStatsHeader = new StringBuilder();
            StringBuilder sbStatsDataSim = new StringBuilder();
            StringBuilder sbStatsDataAdjusted = new StringBuilder();
            StringBuilder sbInfraRaster = new StringBuilder();
            StringBuilder sbInfraVector = new StringBuilder();
            StringBuilder sbDriving = new StringBuilder();
            StringBuilder sbRoutes = new StringBuilder();

            string separator = "\t";

            sbStatsHeader.AppendLine(YearlyAggregateStats.GetHeader());
            sbInfraRaster.AppendLine(String.Join(separator, "year", "location_hash", "latitude", "longitude", "is_in_sweden", "placement", "kW_installed", "kW_used", "kW_unused", "kWPerLaneKm_installed", "kWPerRoadKm_used", "kWPerRoadKm_unused", "kWh_per_year", "euro_per_kWh", "soc_on_arrival", "lane_km", "bidirectional_ers_km"));
            sbDriving.AppendLine(String.Join(separator, "year", "location_hash", "latitude", "longitude", "is_in_sweden", "vehicle_class", "charging_strategy", "battery_capacity", "adjusted_annual_movements", "original_annual_movements", "adjusted_aadt", "annual_tonnes", "sample_count"));
            sbRoutes.AppendLine(String.Join(separator, "route_id", "route_variant_no", "year", "vehicle_type", "route_length_km", "route_length_h_excl_breaks", "annual_trips_total", "annual_trips_loaded", "charging_strategy", "net_battery_capacity_kWh") + separator + RouteCost.ToStringHeader);

            var vehicleTypeComparer = Comparer<VehicleType>.Create((a, b) => a.Common_Total_weight_limit_kg[0].CompareTo(b.Common_Total_weight_limit_kg[0]));

            Console.WriteLine("Looping through model years...");
            Euro cumulativeCost_euro = new Euro(0);
            foreach ((ModelYear year, RouteVehicleTypeBehavior routeStrategies, Dictionary<RouteSegment, ChargingSiteCost> infraSet) in modelYearResults)
            {
                Console.WriteLine("Counting ers km and stations...");
                //Total ERS km and station count this year
                Kilometers ersKm = new Kilometers(0);
                int stationCount = 0;
                foreach (var infra in infraSet)
                {
                    var segment = infra.Key;
                    //Total km of ERS
                    if (segment.Type == RouteSegmentType.Road)
                        ersKm += segment.BidirectionalRoadLengthToElectrify_km;
                    //Total charging stations
                    else if (segment.Type == RouteSegmentType.RestStop)
                        stationCount++;
                }

                Console.WriteLine("Calculating aggregate stats...");
                AppendYearlyAggregateStats(scenario, sbStatsDataSim, sbStatsDataAdjusted, separator, year, routeStrategies, infraSet);
                AppendRouteInfo(scenario, sbRoutes, separator, year, routeStrategies);


                Console.WriteLine("Calculating infra per bin...");
                //Treat ERS and other infra separately.
                //For ERS, explode all segments into their bins, then group by bin. For each bin, take the overlapping segment with the highest kW.
                //For all others, use the original data.
                var infraSegments = infraSet.Select(n => (bin: n.Key.PlaceHash, segment: n.Key, cost: n.Value));

                var binnedStaticInfra = infraSegments
                    .Where(n => n.segment.Type != RouteSegmentType.Road)
                    .GroupBy(n => (n.bin, n.segment.Type))
                    .Select(g =>
                {
                    var energyDelivered_kWhPerYear = new KiloWattHoursPerYear(g.Sum(n => n.cost.EnergyDelivered_kWhPerYear.Val));
                    var installedPower_kW = new KiloWatts(g.Sum(n => n.cost.InstalledPower_kWPeak_Segment.Val));
                    var usedPower_kW = new KiloWatts(g.Sum(n => n.cost.UsedPower_kWPeak_Segment.Val));
                    var installedPower_kWPerLaneKm = new KiloWattsPerKilometer(0);
                    var usedPower_kWPerLaneKm = new KiloWattsPerKilometer(0);
                    var cost_EuroPerYear = new EuroPerYear(g.Sum(n => n.cost.Cost_EuroPerYear.Val));
                    var cost_EuroPerKWh = new EuroPerKiloWattHour(g.Sum(n => n.cost.Cost_EuroPerYear.Val) / g.Sum(n => n.cost.EnergyDelivered_kWhPerYear.Val));
                    var meanSocOnArrival = new Dimensionless(g.Average(n => n.cost.MeanSocOnArrival.Val));

                    return (
                    binID: g.Key.bin.Index.ToString(),
                    coordinate: g.Key.bin,
                    type: g.Key.Type,
                    energyDelivered_kWhPerYear,
                    installedPower_kW,
                    installedPower_kWPerLaneKm,
                    usedPower_kW,
                    usedPower_kWPerLaneKm,
                    cost_EuroPerYear,
                    cost_EuroPerKWh,
                    meanSocOnArrival,
                    laneKm: new Kilometers(0),
                    ersCandidateKm: new Kilometers(0),
                    bidirectionalErsKm: new Kilometers(0));
                }).ToList();

                var segmentToBins = DataReader.ReadClusterToWeightedGridCell_stringID(Paths.ClusterToWeightedGridCells_Sweref99Tm);
                var binnedErs = infraSegments
                    .Where(n => n.segment.Type == RouteSegmentType.Road)
                    .SelectMany(s => segmentToBins[s.segment.ID].Select(b => (BinID: b.Index, b.Ratio, km: b.Meters / 1000, s.cost, s.segment.IsBidirectional, ersCandidate: (s.segment.ChargingOfferedFromYear <= year))))
                    .GroupBy(b => b.BinID)
                    .Select(g =>
                    {
                        var energyDelivered_kWhPerYear = new KiloWattHoursPerYear(g.Sum(n => n.cost.EnergyDelivered_kWhPerYear.Val * n.Ratio));
                        var installedPower_kWPerLaneKm = new KiloWattsPerKilometer(g.Average(n => n.cost.InstalledPower_kWPeak_PerLaneKm.Val)); //This could be weighted by ratio in the cell
                        var usedPower_kWPerLaneKm = new KiloWattsPerKilometer(g.Average(n => n.cost.UsedPower_kWPeak_PerLaneKm.Val)); //This could be weighted by ratio in the cell
                        var laneKm = new Kilometers(g.Sum(n => n.km * (n.IsBidirectional ? 2 : 1)));
                        var ersCandidateKm = new Kilometers(g.Sum(n => (n.ersCandidate ? 1 : 0) * n.km * (n.IsBidirectional ? 2 : 1)));
                        var bidirectionalErsKm = new Kilometers(g.Sum(n => (n.cost.InstalledPower_kWPeak_Segment > 0 ? 1 : 0) * n.km * (n.IsBidirectional ? 2 : 1)));
                        var installedPower_kW = installedPower_kWPerLaneKm * laneKm; //Road power needed in this cell (is this even meaningful?)
                        var usedPower_kW = usedPower_kWPerLaneKm * laneKm;
                        var cost_EuroPerYear = new EuroPerYear(g.Sum(n => n.cost.Cost_EuroPerYear.Val * n.Ratio));
                        var cost_EuroPerKWh = new EuroPerKiloWattHour(g.Sum(n => n.cost.Cost_EuroPerYear.Val) / g.Sum(n => n.cost.EnergyDelivered_kWhPerYear.Val));
                        var meanSocOnArrival = new Dimensionless(g.Average(n => n.cost.MeanSocOnArrival.Val));
                        var coord = CoordinateHash.Sweref99TMIndex_toCoordinate(g.Key);

                        return (
                            binID: g.Key,
                            coordinate: coord,
                            type: RouteSegmentType.Road,
                            energyDelivered_kWhPerYear,
                            installedPower_kW,
                            installedPower_kWPerLaneKm,
                            usedPower_kW,
                            usedPower_kWPerLaneKm,
                            cost_EuroPerYear,
                            cost_EuroPerKWh,
                            meanSocOnArrival,
                            laneKm,
                            ersCandidateKm,
                            bidirectionalErsKm);
                    }).ToList();

                Console.WriteLine("Generating infra bin csv...");
                var ersUserCost = InfrastructureCost.GetErsUserFee_CachedOrEstimated_excl_energy(year, scenario.InfraOffers);
                foreach (var infra in binnedErs.Concat(binnedStaticInfra).Where(n => n.coordinate.IsInSweden))
                {
                    sbInfraRaster.AppendLine(String.Join(separator, new object[]
                    {
                        year.AsDateString(),
                        infra.binID,
                        infra.coordinate.ExactLatitude,
                        infra.coordinate.ExactLongitude,
                        infra.coordinate.IsInSweden,
                        infra.type,
                        infra.installedPower_kW,
                        infra.usedPower_kW,
                        infra.installedPower_kW - infra.usedPower_kW,
                        infra.installedPower_kWPerLaneKm,
                        infra.usedPower_kWPerLaneKm,
                        infra.installedPower_kWPerLaneKm - infra.usedPower_kWPerLaneKm,
                        infra.energyDelivered_kWhPerYear,
                        infra.type == RouteSegmentType.Road ? ersUserCost : infra.cost_EuroPerKWh,
                        infra.meanSocOnArrival,
                        infra.laneKm,
                        infra.bidirectionalErsKm
                    }));
                }

                //TODO: Rewrite this using the pre-calculated segment-to-bin map
                Console.WriteLine("Calculating driving per bin...");
                var binnedDriving = new ConcurrentDictionary<
                    (CoordinateHash cell, VehicleType vtype, ChargingStrategy cstrat, int batteryKWh),
                    (float adjustedAnnualMovements, float originalAnnualMovements, float annualTonnesInclVehicle, int sampleCount)>(); //, float avgTripCountMultiplier, float avgSystemCost_euroPerKm, float avgHaulierCost_euroPerKm, double sum)>();

                Parallel.ForEach(routeStrategies, n =>
                {
                    var cellSeq = routeVariantCellSequences[(n.Key.Route.ID, n.Key.Route.VariantNo)];
                    foreach (var s in cellSeq)//n.Key.Route.SegmentSequence.Distinct())
                    {
                        Kilogram vehicleWeight =
                            n.Value.chargingStrategy == ChargingStrategy.NA_Diesel
                            ? n.Key.VehicleType.ICEV_Chassis_weight_kg[year]
                            : n.Key.VehicleType.BEV_Chassis_weight_excl_battery_kg[year] + n.Value.netBatteryCapacity_kWh / Parameters.Battery.Net_Specific_energy_kWh_per_kg[year];

                        var key = (
                            s,
                            n.Key.VehicleType,
                            n.Value.chargingStrategy,
                            (int)n.Value.netBatteryCapacity_kWh.Val);

                        var value = (
                            adjustedAnnualMovements: n.Key.GetTotalTripCountPerYear(year).Val * n.Value.costs.TripCountMultiplier.Val,
                            originalAnnualMovements: n.Key.GetTotalTripCountPerYear(year).Val,
                            annualTonnes: n.Key.GetCargoTonnesPerYear(year).Val + vehicleWeight.ToTonnes().Val * n.Key.GetTotalTripCountPerYear(year).Val,
                            sampleCount: 1);

                        binnedDriving.AddOrUpdate(key, value, (k, old) =>
                        {
                            return (old.adjustedAnnualMovements + value.adjustedAnnualMovements,
                                old.originalAnnualMovements + value.originalAnnualMovements, //BUG: Both values were adjustedAnnualMovements
                                old.annualTonnesInclVehicle + value.annualTonnes,
                                old.sampleCount + 1);
                        });
                    }
                });

                Console.WriteLine("Generating driving bin tsv...");
                const int batchCount = 1000000 / 7;
                const int tailCount = (int)(batchCount * 0.15);
                Parallel.ForEach(binnedDriving
                    .Where(n => n.Key.cell.IsInSweden)
                    .OrderByDescending(n => n.Value.adjustedAnnualMovements).Take(batchCount - tailCount)
                    .OrderBy(n => n.Key.cstrat).ThenBy(n => n.Key.vtype, vehicleTypeComparer).ThenBy(n => n.Key.batteryKWh)
                    .AsParallel(),
                    bin =>
                {
                    (double lat, double lon) = bin.Key.cell.GetHashCoordinate();
                    string line = String.Join(separator, new object[] {
                        year.AsDateString(),
                        bin.Key.cell.Index,
                        lat, lon, bin.Key.cell.IsInSweden,
                        bin.Key.vtype,
                        bin.Key.cstrat,
                        bin.Key.cstrat == ChargingStrategy.NA_Diesel ? 0 : bin.Key.batteryKWh,
                        bin.Value.adjustedAnnualMovements,
                        bin.Value.originalAnnualMovements,
                        bin.Value.adjustedAnnualMovements / 365,
                        bin.Value.annualTonnesInclVehicle,
                        bin.Value.sampleCount
                    }.Select(n => n.ToString()));
                    lock (sbDriving) { sbDriving.AppendLine(line); }
                });
                Parallel.ForEach(binnedDriving
                    .Where(n => n.Key.cell.IsInSweden)
                    .OrderByDescending(n => n.Value.adjustedAnnualMovements).Skip(batchCount - tailCount)
                    .GroupBy(n => n.Key.cell)
                    .OrderByDescending(n => new OtherUnit(n.Sum(m => m.Value.adjustedAnnualMovements))).Take(tailCount)
                    .AsParallel(),
                    bin =>
                {
                    (double lat, double lon) = bin.Key.GetHashCoordinate();
                    string line = String.Join(separator, new object[] {
                        year.AsDateString(),
                        bin.Key.Index,
                        lat, lon, bin.Key.IsInSweden,
                        "Tail",
                        "Tail",
                        "Tail",
                        bin.Sum(n => n.Value.adjustedAnnualMovements),
                        bin.Sum(n => n.Value.originalAnnualMovements),
                        bin.Sum(n => n.Value.adjustedAnnualMovements) / 365,
                        bin.Sum(n => n.Value.annualTonnesInclVehicle),
                        bin.Sum(n => n.Value.sampleCount)
                    }.Select(n => n.ToString()));
                    lock (sbDriving) { sbDriving.AppendLine(line); }
                });

                cumulativeCost_euro += new Euro(routeStrategies.Sum(n => n.Value.costs.SystemCost_euroPerYear.Val) + infraSegments.Sum(n => n.cost.Cost_EuroPerYear.Val));
            }
            Console.WriteLine("Writing to files...");
            var runLogPath = Paths.RunLog.Replace(".txt", ".stats.txt");
            var runLogPathAdjusted = Paths.RunLog.Replace(".txt", "_adjusted.stats.txt");

            if (!File.Exists(runLogPath))
                File.WriteAllText(runLogPath, sbStatsHeader.ToString());
            File.AppendAllText(runLogPath, sbStatsDataSim.ToString());

            if (!File.Exists(runLogPathAdjusted))
                File.WriteAllText(runLogPathAdjusted, sbStatsHeader.ToString());
            File.AppendAllText(runLogPathAdjusted, sbStatsDataAdjusted.ToString());

            File.WriteAllText(Paths.ExperimentLog.Replace(".txt", ".stats.txt"), sbStatsHeader.ToString() + sbStatsDataSim.ToString());
            File.WriteAllText(Paths.ExperimentLog.Replace(".txt", ".adjusted.stats.txt"), sbStatsHeader.ToString() + sbStatsDataAdjusted.ToString());
            if (logSettings.PrintInfraRasterLog)
                File.WriteAllText(Paths.ExperimentLog.Replace(".txt", ".infra_raster.txt"), sbInfraRaster.ToString());
            if (logSettings.PrintDrivingRasterLog)
                File.WriteAllText(Paths.ExperimentLog.Replace(".txt", ".driving_raster.txt"), sbDriving.ToString());
            if (logSettings.PrintRoutesLog)
                File.WriteAllText(Paths.ExperimentLog.Replace(".txt", ".routes.txt"), sbRoutes.ToString());

            File.AppendAllText(Paths.ExperimentLog.Replace(".txt", ".stats.txt"), Environment.NewLine + "Cumulative system cost is " + Math.Round(cumulativeCost_euro.Val / 1e6f) + " M€" + Environment.NewLine);

            //CalculateStatsPerERSStage(scenario.Name, logSettings, scenario.SimStartYear, scenario.SimEndYear);
        }

        private static void CalculateStatsPerERSStage(string scenarioName, LogSettings logSettings, ModelYear startYear, ModelYear endYear)
        {
            var infraRasterStr = File.ReadLines(Paths.ExperimentLog.Replace(".txt", ".infra_raster.txt"));
            var drivingRasterStr = File.ReadLines(Paths.ExperimentLog.Replace(".txt", ".driving_raster.txt"));

            //Traffic per bin, with charging strategy. These bins are larger than the other bins.
            var trafficPerBin = drivingRasterStr.Select(n => n.Split('\t')).Skip(1).Where(parts => parts[5] != "Tail").Select(parts => (
                year: ModelYearExtensions.FromDateString(parts[0]),
                bin: int.Parse(parts[1]),
                aadt: double.Parse(parts[10]),
                chargingStrategy: (ChargingStrategy)Enum.Parse(typeof(ChargingStrategy), parts[6])
                ))
                .GroupBy(n => (n.year, n.bin))
                .ToDictionary(
                    g => g.Key,
                    g => (aadtTotal: g.Sum(n => n.aadt), aadtErs: g.Sum(n => n.aadt * (n.chargingStrategy.UsesErs() ? 1 : 0))));

            //kWh bought per bin
            var erskWhPerBin = infraRasterStr.Select(n => n.Split('\t')).Skip(1).Select(parts => (
                year: ModelYearExtensions.FromDateString(parts[0]),
                bin: parts[1],
                segmentType: (RouteSegmentType)Enum.Parse(typeof(RouteSegmentType), parts[5]),
                kWhPerY: double.Parse(parts[12]),
                bidirectionalErsKm: double.Parse(parts[16])
                ))
                .Where(n => n.segmentType == RouteSegmentType.Road)
                .GroupBy(n => (n.year, n.bin))
                .ToDictionary(
                    g => g.Key,
                    g => (kWhPerY: g.Sum(n => n.kWhPerY), bidirectionalErsKm: g.Sum(n => n.bidirectionalErsKm)));

            //ERS lat-lon bins, with stage number
            var binToStage = File.ReadLines(Paths.RootPaths.DataRoot + @"etapper/ers_stages.tsv").Skip(1).Select(n => n.Split('\t')).Select(
                parts => (
                    bin: new CoordinateHash(double.Parse(parts[1]), double.Parse(parts[2])).To_Sweref99TMRaster_1km().cell_id,
                    stage: int.Parse(parts[0]))
                )
                .GroupBy(n => n.bin)
                .ToDictionary(
                    g => g.Key,
                    g => g.Min(n => n.stage / 2));

            var ersBinToTrafficBin = binToStage.Keys.ToDictionary(n => n, n => CoordinateHash.Sweref99TMIndex_toCoordinate(n).Index);

            var erskWhPerBinIDs = erskWhPerBin.Select(n => n.Key.bin).Distinct().ToList();
            var erskWhPerBinTrafficIDs = erskWhPerBin.Select(n => ersBinToTrafficBin[n.Key.bin]).Distinct().ToList();
            double kWhCoverage = erskWhPerBinIDs.Where(n => binToStage.ContainsKey(n)).Count() / (float)erskWhPerBinIDs.Count();
            double trafficCoverage = erskWhPerBinTrafficIDs.Where(n => trafficPerBin.ContainsKey((ModelYear.Y2040, n))).Count() / (float)erskWhPerBinTrafficIDs.Count();

            var ersUtilization = erskWhPerBin
                .Select(n => (
                    yearBin: n.Key,
                    traffic: GetAADT(trafficPerBin, n.Key.year, ersBinToTrafficBin[n.Key.bin]),
                    kWh: n.Value.kWhPerY,
                    n.Value.bidirectionalErsKm,
                    stage: binToStage[n.Key.bin]
                    ));
            var ersStageUtilization = ersUtilization
                .GroupBy(n => (n.yearBin.year, n.stage))
                .Select(g => (
                    g.Key.stage,
                    g.Key.year,
                    kWhPerYear: g.Sum(n => n.kWh),
                    vkmPerYear: 365 * g.Sum(n => n.traffic.aadtTotal * n.bidirectionalErsKm),
                    ersKm: g.Sum(n => n.bidirectionalErsKm),
                    utilization: g.Sum(n => n.traffic.aadtErs * n.bidirectionalErsKm) / g.Sum(n => n.traffic.aadtTotal * n.bidirectionalErsKm),
                    count: g.Count()
                    ))
                .OrderBy(n => n.year)
                .ThenBy(n => n.stage)
                .ToList();

            var ersTotalUtilization = ersUtilization
                .GroupBy(n => (n.yearBin.year))
                .Select(g => (
                    year: g.Key,
                    kWhPerYear: g.Sum(n => n.kWh),
                    vkmPerYear: 365 * g.Sum(n => n.traffic.aadtTotal * n.bidirectionalErsKm),
                    ersKm: g.Sum(n => n.bidirectionalErsKm),
                    utilization: g.Sum(n => n.traffic.aadtErs * n.bidirectionalErsKm) / g.Sum(n => n.traffic.aadtTotal * n.bidirectionalErsKm),
                    count: g.Count()
                    ))
                .OrderBy(n => n.year)
                .ToList();

            string header = "year\tstage\ters_km\tkm_per_year\tutilization\tkWh_per_year\n";
            string utilFilePath = Paths.ExperimentLogsDir + "ersStageUtilization.tsv";
            File.WriteAllText(utilFilePath, header);
            File.AppendAllLines(utilFilePath, ersStageUtilization.Select(n => n.year + "\t" + n.stage + "\t" + n.ersKm + "\t" + n.vkmPerYear + "\t" + n.utilization + "\t" + n.kWhPerYear));
            File.AppendAllLines(utilFilePath, ersTotalUtilization.Select(n => n.year + "\ttotal\t" + n.ersKm + "\t" + n.vkmPerYear + "\t" + n.utilization + "\t" + n.kWhPerYear));

            //Total nyttjandegrad
            //Fkm per år på ERS - väg
            //kWh per år från ERS
            int yearsWithData = ersTotalUtilization.Count();
            string padding = "";
            for (int i = 0; i < 7 - yearsWithData; i++)
                padding += "\t";
            string mergedUtilFilePath = Paths.RootPaths.LogsRoot + "ers_utilization.tsv";
            File.AppendAllText(mergedUtilFilePath, scenarioName + '\t');
            File.AppendAllText(mergedUtilFilePath, padding + string.Join('\t', ersTotalUtilization.Select(n => n.kWhPerYear)) + '\t');
            File.AppendAllText(mergedUtilFilePath, padding + string.Join('\t', ersTotalUtilization.Select(n => n.vkmPerYear)) + '\t');
            File.AppendAllText(mergedUtilFilePath, padding + string.Join('\t', ersTotalUtilization.Select(n => n.ersKm)) + '\t');
            File.AppendAllText(mergedUtilFilePath, padding + string.Join('\t', ersTotalUtilization.Select(n => n.utilization)) + '\n');

            //ERS - infra i drift
            //Brukaravgift ERS
            //ERS - utgift per år
            //ERS - intäkt per år
            //Kumulativt ERS-resultat
            //Kumulativ utjämnad systemkostnad rel.basscenario
            //Kumulativ CO₂ rel.basscenario
            //Fkm med BEV, nationellt (%)
        }

        private static (double aadtTotal, double aadtErs) GetAADT(Dictionary<(ModelYear year, int bin), (double aadtTotal, double aadtErs)> trafficPerBin, ModelYear year, int trafficBin)
        {
            if (trafficPerBin.ContainsKey((year, trafficBin)))
            {
                return trafficPerBin[(year, trafficBin)];
            }
            else
            {
                //Take the max of nearby bins
                var neighbors = CoordinateHash.GetNeighbors(trafficBin)
                    .Where(n => trafficPerBin.ContainsKey((year, n)))
                    .Select(n => trafficPerBin[(year, n)])
                    .OrderByDescending(n => n.aadtTotal);
                if (neighbors.Any())
                    return neighbors.First();
                else
                    return (0, 0);
            }
        }

        class YearlyAggregateStats
        {
            public string ScenarioName;

            public ModelYear ModelYear;

            TonnesPerYear ICEV_CO2_ton_per_y = new(0);
            TonnesPerYear BEV_CO2_ton_per_y = new(0);

            public EuroPerYear ICEV_total_epy = new(0);
            public EuroPerYear ICEV_vehicles_epy = new(0);
            public EuroPerYear ICEV_interest_epy = new(0);
            public EuroPerYear ICEV_fuel_epy = new(0);
            public EuroPerYear ICEV_driver_epy = new(0);
            public EuroPerYear ICEV_co2t_epy = new(0);
            public EuroPerYear ICEV_co2i_epy = new(0);
            public EuroPerYear ICEV_roadAndPollutionTax_epy = new(0);
            public LiterPerYear ICEV_fossil_fuel_liter_per_y = new(0);
            public LiterPerYear ICEV_renewable_fuel_liter_per_y = new(0);

            public EuroPerYear BEV_total_epy_excl_infra = new(0);
            public EuroPerYear BEV_vehicles_epy = new(0);
            public EuroPerYear BEV_batteries_epy = new(0);
            public EuroPerYear BEV_pickup_epy = new(0);
            public EuroPerYear BEV_interest_epy = new(0);
            public EuroPerYear BEV_electricity_epy = new(0);
            public EuroPerYear BEV_infrastructure_epy { get { return new EuroPerYear(ChargerPlacements.Values.Sum(n => n.EuroPerYear.Val)); } }
            public EuroPerYear BEV_driver_epy = new(0);
            public EuroPerYear BEV_co2t_epy = new(0);
            public EuroPerYear BEV_co2i_epy = new(0);
            public EuroPerYear BEV_roadAndPollutionTax_epy = new(0);

            public KiloWattHoursPerYear BatteryWear_kWhPerYear = new(0);
            //public Dimensionless BatteryWear_kWhPerKWh = new(0);
            public KiloWattHours BEV_kWh_per_year = new(0);

            public KilometersPerYear ICEV_km_per_year = new(0);
            public TonKilometersPerYear ICEV_tonkm_per_year = new(0);
            public KilometersPerYear BEV_km_per_year = new(0);
            public TonKilometersPerYear BEV_tonkm_per_year = new(0);
            public Dimensionless BEV_km_ratio { get { return BEV_km_per_year / (BEV_km_per_year + ICEV_km_per_year); } }
            public Dimensionless BEV_tonkm_ratio { get { return BEV_tonkm_per_year / (BEV_tonkm_per_year + ICEV_tonkm_per_year); } }
            public Kilometers ERS_km = new(0);

            public class YearlyChargerPlacementStats
            {
                public EuroPerYear EuroPerYear = new(0);
                public KiloWattHoursPerYear KiloWattHoursPerYear = new(0);
                public EuroPerKiloWattHour EuroPerKiloWattHour { get { return EuroPerYear / KiloWattHoursPerYear; } }
            }
            public Dictionary<RouteSegmentType, YearlyChargerPlacementStats> ChargerPlacements = new();
            public KiloWattHoursPerYear KiloWattHoursPerYear { get { return new KiloWattHoursPerYear(ChargerPlacements.Sum(n => n.Value.KiloWattHoursPerYear.Val)); } }
            public Dimensionless ChargerPlacement_kWh_ratio(RouteSegmentType s) { return ChargerPlacements[s].KiloWattHoursPerYear / UnitMath.Max(new(1), KiloWattHoursPerYear); }

            public class YearlyVehicleTypeStats
            {
                public KilometersPerYear ICEV_kmPerYear = new(0);
                public KilometersPerYear BEV_kmPerYear = new(0);
                public Dictionary<RouteSegmentType, KiloWattHoursPerYear> InfraPlacement_KWhPerYear = new();
                public KiloWattHoursPerYear KWhPerYear { get { return new KiloWattHoursPerYear(InfraPlacement_KWhPerYear.Values.Sum(n => n.Val)); } }
                public Dimensionless ICEV_km_ratio { get { return ICEV_kmPerYear / (ICEV_kmPerYear + BEV_kmPerYear); } }
                public Dimensionless InfraPlacement_km_ratio(RouteSegmentType r) { return InfraPlacement_KWhPerYear[r] / UnitMath.Max(new(1), KWhPerYear); }
                public YearlyVehicleTypeStats()
                {
                    foreach (RouteSegmentType item in Enum.GetValues(typeof(RouteSegmentType)))
                        InfraPlacement_KWhPerYear.Add(item, new(0));
                }
            }

            public Dictionary<VehicleType, YearlyVehicleTypeStats> VehicleTypes = new();

            public class YearlyChargingStrategyStats
            {
                public int RoutesCount = 0;
                public KilometersPerYear Kilometers = new(0);
                public TonKilometersPerYear TonKilometers = new(0);
            }

            public Dictionary<ChargingStrategy, YearlyChargingStrategyStats> ChargingStrategies = new();

            public float ChargingStrategy_routes_ratio(ChargingStrategy c) { return ChargingStrategies[c].RoutesCount / (float)Math.Max(1, ChargingStrategies.Sum(n => n.Value.RoutesCount)); }
            public float ChargingStrategy_km_ratio(ChargingStrategy c) { return ChargingStrategies[c].Kilometers.Val / (float)Math.Max(1, ChargingStrategies.Sum(n => n.Value.Kilometers.Val)); }
            public float ChargingStrategy_tonkm_ratio(ChargingStrategy c) { return ChargingStrategies[c].TonKilometers.Val / (float)Math.Max(1, ChargingStrategies.Sum(n => n.Value.TonKilometers.Val)); }

            public YearlyAggregateStats(string scenarioName, ModelYear modelYear)
            {
                ScenarioName = scenarioName;
                ModelYear = modelYear;

                foreach (RouteSegmentType item in Enum.GetValues(typeof(RouteSegmentType)))
                    ChargerPlacements.Add(item, new YearlyChargerPlacementStats());
                foreach (VehicleType item in Parameters.VehicleTypesInOrder)
                    VehicleTypes.Add(item, new YearlyVehicleTypeStats());
                foreach (ChargingStrategy item in Enum.GetValues(typeof(ChargingStrategy)))
                    ChargingStrategies.Add(item, new YearlyChargingStrategyStats());
            }

            public void Increment(ModelYear year, Route_VehicleType route_vehicletype, (ChargingStrategy chargingStrategy, KiloWattHours netBatteryCapacity_kWh, RouteCost costs) values)
            {
                var costs = values.costs;
                var r = costs.RatioOfOperationInSweden; //already included in the _*PerYear properties
                var v = VehicleTypes[route_vehicletype.VehicleType];

                if (values.chargingStrategy == ChargingStrategy.NA_Diesel)
                {
                    ICEV_CO2_ton_per_y += new TonnesPerYear(0.001f * costs.CO2_euroPerYear_total_in_SE.Val / Parameters.World.CO2_SCC_euro_per_kg[year].Val);

                    ICEV_total_epy += costs.SystemCost_euroPerYear;
                    ICEV_vehicles_epy += costs.VehicleAgeing_euroPerYear_in_SE;
                    ICEV_interest_epy += costs.Interest_euroPerYear_in_SE;
                    ICEV_fuel_epy += costs.Energy_euroPerYear_in_SE;
                    ICEV_driver_epy += costs.Driver_euroPerYear_in_SE;
                    ICEV_co2t_epy += costs.CO2_euroPerYear_total_in_SE;
                    ICEV_co2i_epy += costs.CO2_euroPerYear_internalized_in_SE;
                    ICEV_km_per_year += costs.TotalAnnualRouteKmInSweden;
                    ICEV_tonkm_per_year += costs.TotalAnnualRouteTonKmInSweden;
                    ICEV_roadAndPollutionTax_epy += costs.RoadAndPollutionTax_euroPerYear_in_SE;
                    ICEV_fossil_fuel_liter_per_y += (costs.Energy_euroPerYear_in_SE / Parameters.World.Diesel_Price_euro_per_liter[year]) * (1 - Parameters.World.RenewableDiesel_Blend_guess_ratio[year]);
                    ICEV_renewable_fuel_liter_per_y += (costs.Energy_euroPerYear_in_SE / Parameters.World.Diesel_Price_euro_per_liter[year]) * Parameters.World.RenewableDiesel_Blend_guess_ratio[year];
                    VehicleTypes[route_vehicletype.VehicleType].ICEV_kmPerYear += costs.TotalAnnualRouteKmInSweden;
                }
                else
                {
                    BEV_CO2_ton_per_y += new TonnesPerYear(0.001f * costs.CO2_euroPerYear_total_in_SE.Val / Parameters.World.CO2_SCC_euro_per_kg[year].Val);

                    BEV_total_epy_excl_infra += costs.SystemCost_euroPerYear;
                    BEV_vehicles_epy += costs.VehicleAgeing_euroPerYear_in_SE;
                    BEV_batteries_epy += costs.BatteryAgeing_euroPerYear_in_SE;
                    BEV_pickup_epy += costs.ErsPickupAgeing_euroPerYear_in_SE;
                    BEV_interest_epy += costs.Interest_euroPerYear_in_SE;
                    BEV_electricity_epy += costs.Energy_euroPerYear_in_SE;
                    BEV_driver_epy += costs.Driver_euroPerYear_in_SE;
                    BEV_co2t_epy += costs.CO2_euroPerYear_total_in_SE;
                    BEV_co2i_epy += costs.CO2_euroPerYear_internalized_in_SE;
                    BEV_km_per_year += costs.TotalAnnualRouteKmInSweden;
                    BEV_tonkm_per_year += costs.TotalAnnualRouteTonKmInSweden;
                    BEV_roadAndPollutionTax_epy += costs.RoadAndPollutionTax_euroPerYear_in_SE;

                    BatteryWear_kWhPerYear += costs.BatteryAgeing_kWhPerYear_in_SE;
                    var tripsPerYear = new Dimensionless(route_vehicletype.GetTotalTripCountPerYear(year).Val) * costs.TripCountMultiplier;
                    //BUG: I don't know what this value represents in the end.
                    //BatteryWear_kWhPerKWh += costs.BatteryAgeing_kWhPerYear_in_SE / new KiloWattHoursPerYear(costs.EnergyCostPerTraversal_kWh.Val * tripsPerYear);
                    BEV_kWh_per_year += costs.EnergyCostPerTraversal_kWh * tripsPerYear;

                    v.BEV_kmPerYear += costs.TotalAnnualRouteKmInSweden;
                    var kWhPerPlacementPerTrip = costs.InfraUsePerTraversal.GroupBy(n => n.Key.Type).Select(g => (Placement: g.Key, kWhPurchased: new KiloWattHoursPerYear(g.Sum(n => n.Value.kWh.Val)) * tripsPerYear));
                    foreach (var kWh in kWhPerPlacementPerTrip)
                        VehicleTypes[route_vehicletype.VehicleType].InfraPlacement_KWhPerYear[kWh.Placement] += kWh.kWhPurchased;
                }
                var csStats = ChargingStrategies[values.chargingStrategy];
                csStats.Kilometers += costs.TotalAnnualRouteKmInSweden;
                csStats.TonKilometers += costs.TotalAnnualRouteTonKmInSweden;
                csStats.RoutesCount++;
            }

            public void Increment(RouteSegment segment, ChargingSiteCost infra)
            {
                var cpStats = ChargerPlacements[segment.Type];
                cpStats.EuroPerYear += infra.Cost_EuroPerYear;
                cpStats.KiloWattHoursPerYear += infra.EnergyDelivered_kWhPerYear;
                if (segment.Type == RouteSegmentType.Road)
                    ERS_km += segment.BidirectionalRoadLengthToElectrify_km;
            }

            public static string GetHeader()
            {
                string header = "scenario_name, year, co2_ton_per_y, icev_co2_ton_per_y, bev_co2_ton_per_y, all_total_€_per_y, icev_total_€_per_y, icev_vehicles_€_per_y, icev_interest_€_per_y, " +
                    "icev_fuel_€_per_y, icev_driver_€_per_y, icev_co2t_€_per_y, icev_co2i_€_per_y, icev_roadnpoltax_€_per_y, icev_fossil_fuel_liter_per_y, icev_renewable_fuel_liter_per_y, " +
                    "bev_total_€_per_y, bev_vehicles_€_per_y, bev_batteries_€_per_y, bev_pickup_€_per_y, bev_interest_€_per_y, bev_electricity_€_per_y, bev_infrastructure_€_per_y, " +
                    "bev_driver_€_per_y, bev_co2t_€_per_y, bev_co2i_€_per_y, bev_roadnpoltax_€_per_y, bat_kWhWear_per_y, bat_kWhWear_per_kWh, ";
                header += "icev_km_per_y, bev_km_per_y, bev_km_ratio, icev_tonkm_per_y, bev_tonkm_per_y, bev_tonkm_ratio, ";
                header += "ers_km, ";
                foreach (RouteSegmentType pl in Enum.GetValues(typeof(RouteSegmentType)))
                    header += "PL_€_per_y, PL_kWh, PL_kWh_ratio, PL_€_per_kWh, ".Replace("PL", pl.ToString());
                foreach (VehicleType vt in Parameters.VehicleTypesInOrder)
                {
                    header += "VT_icev_ratio, ".Replace("VT", vt.ToString());
                    foreach (RouteSegmentType pl in Enum.GetValues(typeof(RouteSegmentType)))
                        header += "VT_PL_kWh, VT_PL_ratio, ".Replace("VT", vt.ToString()).Replace("PL", pl.ToString());
                }
                foreach (ChargingStrategy cs in Enum.GetValues(typeof(ChargingStrategy)))
                    header += "CS_km_ratio, CS_routes_ratio, ".Replace("CS", cs.ToString());
                return header.Replace(", ", "\t");
            }

            public void PerformPostHocRenewableDieselAdjustment(ModelYear year)
            {
                //This method adjusts the computed results so that the total volume of renewable diesel is unaffected by the rate of electrification.
                var w = Parameters.World;
                var simDiesel_liter_per_y = ICEV_fuel_epy / w.Diesel_Price_euro_per_liter[year];
                var newRenewable_liter_per_y = UnitMath.Min(simDiesel_liter_per_y, w.RenewableDiesel_Supply_cap_liter_per_year[year]);
                var newFossil_liter_per_y = simDiesel_liter_per_y - newRenewable_liter_per_y;

                //Update CO2, taxes, fuel cost, icev total and system total
                ICEV_total_epy -= ICEV_co2t_epy + ICEV_fuel_epy;
                ICEV_CO2_ton_per_y = (newRenewable_liter_per_y * w.RenewableDiesel_Emissions_kg_CO2_per_liter[year] + newFossil_liter_per_y * w.FossilDiesel_Emissions_kg_CO2_per_liter[year]);
                ICEV_co2t_epy = ICEV_CO2_ton_per_y * w.CO2_SCC_euro_per_kg[year];
                ICEV_co2i_epy = ICEV_co2t_epy * w.CO2_Tax_ratio_of_SCC[year];
                ICEV_fuel_epy = newRenewable_liter_per_y * w.RenewableDiesel_Price_euro_per_liter[year] + newFossil_liter_per_y * w.FossilDiesel_Price_euro_per_liter[year];
                ICEV_total_epy += ICEV_co2t_epy + ICEV_fuel_epy;

                ICEV_fossil_fuel_liter_per_y = newFossil_liter_per_y;
                ICEV_renewable_fuel_liter_per_y = newRenewable_liter_per_y;
            }

            public override string ToString()
            {
                var batteryWear_kWhPerKWh = BatteryWear_kWhPerYear.Val / BEV_kWh_per_year.Val;
                var items = new List<object> {
                    ScenarioName, ModelYear,
                    ICEV_CO2_ton_per_y + BEV_CO2_ton_per_y, ICEV_CO2_ton_per_y, BEV_CO2_ton_per_y,
                    ICEV_total_epy + BEV_total_epy_excl_infra + BEV_infrastructure_epy,
                    ICEV_total_epy, ICEV_vehicles_epy, ICEV_interest_epy, ICEV_fuel_epy, ICEV_driver_epy, ICEV_co2t_epy, ICEV_co2i_epy, ICEV_roadAndPollutionTax_epy, ICEV_fossil_fuel_liter_per_y, ICEV_renewable_fuel_liter_per_y,
                    BEV_total_epy_excl_infra + BEV_infrastructure_epy, BEV_vehicles_epy, BEV_batteries_epy, BEV_pickup_epy, BEV_interest_epy, BEV_electricity_epy, BEV_infrastructure_epy, BEV_driver_epy, BEV_co2t_epy, BEV_co2i_epy, BEV_roadAndPollutionTax_epy,
                    BatteryWear_kWhPerYear, batteryWear_kWhPerKWh,
                    ICEV_km_per_year, BEV_km_per_year, BEV_km_ratio, ICEV_tonkm_per_year, BEV_tonkm_per_year, BEV_tonkm_ratio,
                    ERS_km};
                foreach (RouteSegmentType pl in Enum.GetValues(typeof(RouteSegmentType)))
                {
                    var pls = ChargerPlacements[pl];
                    items.AddRange(new object[] { pls.EuroPerYear, pls.KiloWattHoursPerYear, ChargerPlacement_kWh_ratio(pl), pls.EuroPerKiloWattHour });
                }
                foreach (VehicleType vt in Parameters.VehicleTypesInOrder)
                {
                    var vts = VehicleTypes[vt];
                    items.Add(vts.ICEV_km_ratio);
                    foreach (RouteSegmentType pl in Enum.GetValues(typeof(RouteSegmentType)))
                        items.AddRange(new object[] { vts.InfraPlacement_KWhPerYear[pl], vts.InfraPlacement_km_ratio(pl) });
                }
                foreach (ChargingStrategy cs in Enum.GetValues(typeof(ChargingStrategy)))
                {
                    items.AddRange(new object[] { ChargingStrategy_km_ratio(cs), ChargingStrategy_routes_ratio(cs) });
                }

                return string.Join('\t', items);
            }
        }

        private static void AppendYearlyAggregateStats(Scenario scenario, StringBuilder sbSim, StringBuilder sbAdjusted, string separator, ModelYear year, RouteVehicleTypeBehavior routeStrategies, Dictionary<RouteSegment, ChargingSiteCost> infraSet)
        {
            YearlyAggregateStats yas = new YearlyAggregateStats(scenario.Name, year);
            foreach ((var a, var b) in routeStrategies)
                yas.Increment(year, a, b);
            foreach ((var rs, var cs) in infraSet)
                yas.Increment(rs, cs);
            sbSim.AppendLine(yas.ToString());
            yas.PerformPostHocRenewableDieselAdjustment(year);
            sbAdjusted.AppendLine(yas.ToString());
        }

        private static void AppendRouteInfo(Scenario scenario, StringBuilder sb, string separator, ModelYear year, RouteVehicleTypeBehavior routeStrategies)
        {
            foreach (var (route, solution) in routeStrategies)
            {
                sb.AppendLine(string.Join(separator,
                    route.Route.ID,
                    route.Route.VariantNo,
                    year.AsInteger(),
                    route.VehicleType,
                    route.Route.Length_km,
                    route.Route.Length_h_excl_breaks,
                    route.GetTotalTripCountPerYear(year),
                    route.GetLoadedTripCountPerYear(year),
                    solution.chargingStrategy,
                    solution.netBatteryCapacity_kWh,
                    solution.costs.ToStringPerKm(separator)));
            }
        }
    }
}
