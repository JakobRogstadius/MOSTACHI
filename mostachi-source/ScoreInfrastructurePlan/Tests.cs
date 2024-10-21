using Commons;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ScoreInfrastructurePlan
{
    class Tests
    {
        public static void RunTests()
        {
            var h = new CoordinateHash(57.773394, 14.182761);
            var a = h.To_Sweref99TMRaster_1km();
            var b = CoordinateHash.Sweref99TMIndex_toCoordinate(a.cell_id);

            RenderErsNetwork();

            //Räkna total körsträcka och energiförbrukning per fordonstyp

            //(var allSegments, var clusterToRestArea) = Experiments.GetRouteSegments();
            //var movements = Experiments.GetMovements_AndCreateDepotAndDestinationSegments(allSegments, clusterToRestArea, new ExperimentScenario() { MovementSampleRatio = new Dimensionless(0.02f) });
            //Euro driver = new Euro(0), vehicle = new Euro(0), fuel = new Euro(0);
            //ModelYear yr = ModelYear.Y2020;
            //foreach (var movement in movements)
            //{
            //    var km = movement.Route.Length_km * new Dimensionless(movement.TotalTripCountPerYear.Val);
            //    driver += movement.VehicleType.Common_Driver_cost_euro_per_h[yr] * (movement.Route.Length_h_excl_breaks + movement.VehicleType.Common_Destination_stop_h[yr]) * new Dimensionless(movement.TotalTripCountPerYear.Val);
            //    vehicle += movement.VehicleType.ICEV_ChassisAndMaintenance_cost_euro_per_km(yr) * km;
            //    fuel += Parameters.World.Diesel_Price_euro_per_liter[yr] * movement.VehicleType.ICEV_Fuel_consumption_liter_per_km[yr] * km;
            //}


            //ConcurrentDictionary<VehicleType, (float km, float kWh)> sums = new ConcurrentDictionary<VehicleType, (float km, float kWh)>();
            //sums.AddOrUpdate(Parameters.MGV16, (0, 0), (key, old) => old);
            //sums.AddOrUpdate(Parameters.MGV24, (0, 0), (key, old) => old);
            //sums.AddOrUpdate(Parameters.HGV40, (0, 0), (key, old) => old);
            //sums.AddOrUpdate(Parameters.HGV60, (0, 0), (key, old) => old);
            //Parallel.ForEach(movements, movement =>
            //{
            //    float km = movement.TotalTripCountPerYear * movement.Route.Length_km;
            //    float kWh = km * movement.VehicleType.BEV_Energy_consumption_kWh_per_km[ModelYear.Y2035];
            //    sums.AddOrUpdate(movement.VehicleType, (km, kWh), (key, old) => (old.km + km, old.kWh + kWh));
            //});

            ModelYear y = ModelYear.Y2030;
            //for (int i = 0; i < 10; i++)
            //{
            //    var values = new object[] {
            //        Parameters.HGV40.GetSampled_Utilization_calendar_days_per_year(y, i),
            //        Parameters.HGV40.GetSampled_Utilization_h_per_day(y, i),
            //        Parameters.HGV40.GetSampled_Annual_distance_km(y, i),
            //        Parameters.HGV40.GetSampled_Depot_stop_h(y, i),
            //        Parameters.HGV40.GetSampled_ICEV_lifetime_years(y, i),
            //        Parameters.HGV40.GetSampled_BEV_lifetime_years(y, i)
            //    };

            //    foreach (object val in values)
            //        Console.WriteLine(val);

            //    Console.WriteLine();
            //}


            var inf = Parameters.Infrastructure;
            foreach (ModelYear yr in ModelYear.Y2020.GetSequence(ModelYear.Y2050))
            {
                Console.WriteLine(
                    string.Join('\t', new EuroPerKiloWattHour[]
                    {
                        InfrastructureCost.GetSiteUserFee_CachedOrEstimated_excl_energy(new RouteSegment() { Type = RouteSegmentType.Depot, ID = (new Random()).Next() }, yr, new KiloWatts(100)),
                        InfrastructureCost.GetSiteUserFee_CachedOrEstimated_excl_energy(new RouteSegment() { Type = RouteSegmentType.Destination }, yr, new KiloWatts(500)),
                        InfrastructureCost.GetSiteUserFee_CachedOrEstimated_excl_energy(new RouteSegment() { Type = RouteSegmentType.RestStop }, yr, new KiloWatts(750)),
                        InfrastructureCost.Calculate_ERS_euro_per_kWh(yr, new KiloWatts(300), new KiloWattsPerKilometer(0), 85, new OtherUnit(2000))
                    }.Select(n => Math.Round(1000 * n.Val) / 1000f)));
            }

            ModelYear year = ModelYear.Y2030;

            Route route = new Route()
            {
                ID = 0,
                SegmentSequence = GetRoute()
            };
            Console.WriteLine("Total route length is " + route.Length_km_total + " km, " + route.Length_h_excl_breaks + " h.");
            Console.WriteLine(route.ToString(year));

            Route_VehicleType rvpair = new Route_VehicleType()
            {
                VehicleType = Parameters.VehicleTypes[104],
                Route = route,
                EmptyTripCountPerYear2020 = new OtherUnit(400),
                LoadedTripCountPerYear2020 = new OtherUnit(1000),
                CargoTonnesPerYear2020 = new OtherUnit(1000 * 20)
            };

            BatteryOffers availableBatteryCapacities_kW = new BatteryOffers { { Parameters.VehicleTypes[104], new UnitList<KiloWattHours>() { 500 }.ToArray() } }; //250, 500, 750, 1000 } } };
            //var scaleFactors = new float[] { 0.5f, 0.75f, 1f, 1.5f, 2f };
            var scaleFactors = new float[] { 2f };

            foreach (var s in scaleFactors)
            {
                PrintCondition(rvpair, availableBatteryCapacities_kW, year, s);
            }

            /*
             * Observationer:
             * Fordonskostnader blir mycket högre för dieselbilar än för 1MWh-BEV. Är det rätt?
             * Hög effekt på publik laddning resulterar i att fordon som inte behöver ladda till fullt där köper mer el och får högre batterislitage än nödvändigt.
             * Olika strategier kan ge olika total energiförbrukning, eftersom de hinner ladda olika mycket. Total energiförbrukning ska vara konstant.
             * Kan ett fordon minimera sina egna kostnader? Ange önskad batterinivå vid ankomst till depå? Ladda bara upp till X% publikt (X är platsberoende).
             * Att erbjuda höga laddeffekter i förhållande till motoreffekt tvingar upp slitage.
             * Kostnad för tid och vikt syns inte nu.
             * På korta rutter blir det väldigt viktigt att total energi per fordon blir rätt.
             * Kan jag normera vid slutet av rutten? Ja, det verkar gå. Nu blir kostnaderna stabila över olika laddeffekter.
             */
        }

        private static RouteSegment[] GetRoute()
        {
            List<RouteSegment> route = new List<RouteSegment>();
            int sid = 2;
            route.Add(new RouteSegment() { ID = sid++, Type = RouteSegmentType.Depot, PlaceHash = new CoordinateHash(57.6 + route.Count * 0.1, 14.1) });
            for (int i = 0; i < 1; i++)
            {
                route.Add(new RouteSegment() { ID = sid++, Type = RouteSegmentType.Road, LengthToTraverseOneWay_km = new Kilometers(50), Speed_Kmph = new KilometersPerHour(50), PlaceHash = new CoordinateHash(57.6 + route.Count * 0.1, 14.1) });
                route.Add(new RouteSegment() { ID = sid++, Type = RouteSegmentType.Road, LengthToTraverseOneWay_km = new Kilometers(50), Speed_Kmph = new KilometersPerHour(50), PlaceHash = new CoordinateHash(57.6 + route.Count * 0.1, 14.1) });
                route.Add(new RouteSegment() { ID = sid++, Type = RouteSegmentType.RestStop, PlaceHash = new CoordinateHash(57.6 + route.Count * 0.1, 14.1) });
                route.Add(new RouteSegment() { ID = sid++, Type = RouteSegmentType.Road, LengthToTraverseOneWay_km = new Kilometers(50), Speed_Kmph = new KilometersPerHour(50), PlaceHash = new CoordinateHash(57.6 + route.Count * 0.1, 14.1) });
            }
            route.Add(new RouteSegment() { ID = sid++, Type = RouteSegmentType.Destination, PlaceHash = new CoordinateHash(57.6 + route.Count * 0.1, 14.1) });
            foreach (var segment in route.Where(n => n.Type == RouteSegmentType.Depot)) // || n.Type == RouteSegmentType.Destination || n.Type == RouteSegmentType.RestStop))
            {
                segment.ChargingOfferedFromYear = ModelYear.Y2020;
            }
            return route.ToArray();
        }

        private static void PrintCondition(Route_VehicleType rvpair, BatteryOffers availableBatteryCapacities_kW, ModelYear year, float s)
        {
            Console.WriteLine();
            Dimensionless ersRatio = new Dimensionless(0.5f);
            InfraOffers infraOffer = new InfraOffers()
            {
                AvailablePowerPerUser_kW = new Dictionary<RouteSegmentType, KiloWatts>()
                    {
                        { RouteSegmentType.Depot, new KiloWatts(200 * s) },
                        { RouteSegmentType.Destination, new KiloWatts(350 * s) },
                        { RouteSegmentType.RestStop, new KiloWatts(400 * s) },
                        { RouteSegmentType.Road, new KiloWatts(200 * s) }
                    },
                ErsReferenceSpeed_kmph = new KilometersPerHour(80),
                ErsCoverageRatio = ersRatio
            };

            var referenceCosts = new Dictionary<RouteSegmentType, EuroPerKiloWattHour>();
            foreach (var rstype in new RouteSegmentType[] { RouteSegmentType.Depot, RouteSegmentType.Destination })
            {
                var kW = infraOffer.AvailablePowerPerUser_kW[rstype];
                referenceCosts.Add(rstype, InfrastructureCost.GetSiteUserFee_CachedOrEstimated_excl_energy(new RouteSegment() { Type = rstype }, year, kW));
            }
            referenceCosts.Add(RouteSegmentType.Road, InfrastructureCost.Calculate_ERS_euro_per_kWh(year, infraOffer.AvailablePowerPerUser_kW[RouteSegmentType.Road], new KiloWattsPerKilometer(0), infraOffer.ErsReferenceSpeed_kmph.Val));

            //SingleYearScorer.ScoreYear(year, new List<Route_VehicleType>() { rvpair }, availableBatteryCapacities_kW, infraOffer, referenceCosts, new Dictionary<int, float>());

            Scenario scenario = new Scenario()
            {
                InfraOffers = infraOffer,
                BatteryOffers = availableBatteryCapacities_kW,
                ChargingStrategies = new List<ChargingStrategy>() { ChargingStrategy.AllPlannedStopsAndErs } //(Enum.GetValues(typeof(ChargingStrategy)).OfType<ChargingStrategy>())
            };

            var score = SingleYearScorer.FindCheapestWayToTraverseRoute(year, rvpair, scenario, new Dictionary<RouteSegment, (KiloWatts kW_segment, KiloWattsPerKilometer kW_perLaneKm)>());
            var c = score.costPerKm;
            var vals1 = new object[] { s, score.netBatteryCapacity_kWh, score.chargingStrategy };
            Console.WriteLine(string.Join("\t", vals1));
            Console.WriteLine(score.costPerKm.ToString());
            Console.WriteLine(string.Join(", ", score.costPerKm.InfraUsePerTraversal.OrderBy(n => n.Key).Select(n => n.Key + ":" + Math.Round(n.Value.kWh.Val))));
        }

        public static void RenderErsNetwork(IEnumerable<RouteSegment> segments = null)
        {
            List<int> clusterIDs;
            if (segments == null)
                clusterIDs = File.ReadAllLines(Paths.ErsBuildOrder).Select(n => int.Parse(n)).Take(1000).ToList();
            else
                clusterIDs = segments.Select(n => n.ID).ToList();
            var clusterToBins = DataReader.ReadClusterToWeightedGridCell(Paths.ClusterToWeightedGridCells);
            var bins = clusterIDs
                .SelectMany(n => clusterToBins[n])
                .GroupBy(n => n.Index)
                .ToDictionary(g => g.Key, g => (CoordinateHash.GetHashCoordinate(g.Key), g.Sum(m => m.Meters)));
            
            File.WriteAllLines("ers_raster.csv", bins.Select(n => string.Join('\t', n.Key, n.Value.Item1.lat, n.Value.Item1.lon, n.Value.Item2)));


        }
    }
}
