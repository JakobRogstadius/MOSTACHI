using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ScoreInfrastructurePlan
{
    class RouteVehicleTypeBehavior : ConcurrentDictionary<Route_VehicleType, (ChargingStrategy chargingStrategy, KiloWattHours netBatteryCapacity_kWh, RouteCost costs)> { }

    class SingleYearScorer
    {
        public static (RouteVehicleTypeBehavior, Dictionary<RouteSegment, ChargingSiteCost>) SimulateUntilConvergence(
            ModelYear year,
            IEnumerable<Route_VehicleType> routes,
            Scenario scenario,
            Dictionary<RouteSegment, (KiloWatts kW_segment, KiloWattsPerKilometer kW_perLaneKm)> inheritedSitePeakPower_kW,
            bool onlyPrecomputeCostsUntilConvergence = false)
        {
            //	Initiera med gissad brukarkostnad:
            //		Simulera (undvik att ladda vid svartlistade platser) > Dimensionera infra efter användning och ärvd effekt, vilket ger brukarkostnader
            //			Om total energi från någon infratyp ändrades mer än 10 %:
            //				> Lägg till riktigt dyra platser (men inte ERS med ärvd effekt) i svartlista > Räkna om ers-brukarkostnad > Simulera igen
            //			Om simuleringen var stabil:
            //				> Returnera laddbeteende, mängd byggd laddinfrastruktur (inkl. ärvd effekt)

            float infraCostSubSampleFq = 0.2f;

            Dictionary<RouteSegmentType, KiloWattHoursPerYear> energyPerTypePerYear_prev = new Dictionary<RouteSegmentType, KiloWattHoursPerYear>();
            foreach (RouteSegmentType t in Enum.GetValues(typeof(RouteSegmentType)))
                energyPerTypePerYear_prev.Add(t, new KiloWattHoursPerYear(0));

            bool hasConverged = !scenario.AnyInfraAvailable(year); //It's already converged if only diesel is available
            int iterationCount = 0;
            while (true)
            {
                ConcurrentDictionary<RouteSegment, (KiloWatts meankW, KiloWatts maxSingleVehiclePeakKW, KiloWattHoursPerYear kWhPerYear, Dimensionless socOnArrival, OtherUnit aadt)> infraUsePerSegment
                    = new ConcurrentDictionary<RouteSegment, (KiloWatts, KiloWatts, KiloWattHoursPerYear, Dimensionless, OtherUnit)>();
                RouteVehicleTypeBehavior routeTraversals = new RouteVehicleTypeBehavior();

                float subsampleFrequency = hasConverged & !onlyPrecomputeCostsUntilConvergence ? 1 : infraCostSubSampleFq;

                Console.WriteLine("\nStarting a new iteration");
                int routeCounter = 0;

                var parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount - 1 }; 
                Parallel.ForEach(routes, parallelOptions, route_vt =>
                {
                    if (Interlocked.Increment(ref routeCounter) % 1000 == 0)
                        Console.Write('.');
                    if (Hashes.UniformHash((uint)route_vt.Route.ID) > subsampleFrequency)
                        return;

                    var routeResult = FindCheapestWayToTraverseRoute(year, route_vt, scenario);
                    routeTraversals[route_vt] = routeResult;
                    IncrementInfraUse(infraUsePerSegment, routeResult.costPerKm.InfraUsePerTraversal, route_vt.GetTotalTripCountPerYear(year) * (routeResult.costPerKm.TripCountMultiplier / new Dimensionless(subsampleFrequency)));
                });
                Console.WriteLine();

                var energyPerTypePerYear_current = infraUsePerSegment.GroupBy(n => n.Key.Type).ToDictionary(g => g.Key, g => new KiloWattHoursPerYear(g.Sum(n => n.Value.kWhPerYear.Val)));
                Console.WriteLine(string.Join(", ", energyPerTypePerYear_current.Select(n => n.Key + ": " + n.Value.Val.ToString("E1"))) + " kWh/year");
                float changeRatio = energyPerTypePerYear_prev.Sum(n => Math.Abs(energyPerTypePerYear_current.GetValueOrDefault(n.Key, new KiloWattHoursPerYear(0)).Val - n.Value.Val)) / energyPerTypePerYear_prev.Sum(n => n.Value.Val);
                energyPerTypePerYear_prev = energyPerTypePerYear_current;

                (var siteUserCosts, var ersSegmentUserCosts, var segmentsWithVeryHighCostPerkWh) = CalculateCostToChargePerSiteBasedOnSimulatedUse_EuroPerKWh(year, infraUsePerSegment, inheritedSitePeakPower_kW);

                if (hasConverged)
                {
                    if (!onlyPrecomputeCostsUntilConvergence)
                        InfrastructureCost.ResetCache();

                    if (iterationCount == 10)
                        Console.WriteLine("SIMULATION DID NOT CONVERGE!!!");

                    Console.WriteLine("Built ERS length (lane km): " + Math.Round(ersSegmentUserCosts.Sum(n => n.Key.LaneLengthToElectrifyOneOrBothWays_km.Val)) + " km");

                    var finalInfraCosts = siteUserCosts.Union(ersSegmentUserCosts).ToDictionary(n => n.Key, n => n.Value);

                    return (routeTraversals, finalInfraCosts);
                }
                else
                {
                    var euroPerkWh_sites = siteUserCosts.ToDictionary(n => n.Key, n => n.Value.Cost_EuroPerkWh);
                    var builtErs = ersSegmentUserCosts.Where(n => inheritedSitePeakPower_kW.ContainsKey(n.Key) || !segmentsWithVeryHighCostPerkWh.Contains(n.Key));
                    var euroPerkWh_ers = new EuroPerKiloWattHour(builtErs.Sum(n => n.Value.Cost_EuroPerYear.Val) / builtErs.Sum(n => n.Value.EnergyDelivered_kWhPerYear.Val));
                    Console.WriteLine("Built ERS length (lane km): " + Math.Round(builtErs.Sum(n => n.Key.LaneLengthToElectrifyOneOrBothWays_km.Val)) + " km");

                    var blacklist = segmentsWithVeryHighCostPerkWh.Where(n => n.Type != RouteSegmentType.Road || !inheritedSitePeakPower_kW.ContainsKey(n));
                    InfrastructureCost.SetCache(year, euroPerkWh_sites, euroPerkWh_ers, blacklist);
                }

                if (changeRatio < 0.1 || siteUserCosts.Count + ersSegmentUserCosts.Count == 0 || iterationCount == 10)
                {
                    hasConverged = true;
                    Console.WriteLine("Simulation converged after " + (iterationCount + 1) + " iterations.");
                }
                iterationCount++;
            }
        }

        private static (Dictionary<RouteSegment, ChargingSiteCost> siteUserCosts, Dictionary<RouteSegment, ChargingSiteCost> ersSegmentUserCosts, HashSet<RouteSegment> segmentsWithVeryHighCostPerkWh) CalculateCostToChargePerSiteBasedOnSimulatedUse_EuroPerKWh(
            ModelYear year,
            ConcurrentDictionary<RouteSegment, (KiloWatts meankW, KiloWatts maxSingleVehicleKW, KiloWattHoursPerYear kWhPerYear, Dimensionless meanSocOnArrival, OtherUnit aadt)> infraUsePerSegment,
            Dictionary<RouteSegment, (KiloWatts kW, KiloWattsPerKilometer kWPerKm)> inheritedSegmentPeakPower_kW)
        {
            //This method calculates the user cost at each charging location, given its observed use

            var meanToPeak = new Dictionary<RouteSegmentType, Dimensionless>()
            {
                { RouteSegmentType.Depot, 1 / Parameters.Infrastructure.Depot_utilization_ratio[year] },
                { RouteSegmentType.Destination, 1 / Parameters.Infrastructure.Destination_utilization_ratio[year] },
                { RouteSegmentType.RestStop, 1 / Parameters.Infrastructure.Rest_Stop_utilization_ratio[year] },
                { RouteSegmentType.Road, 1 / Parameters.Infrastructure.ERS_utilization_ratio[year] }
            };

            //DEBUG NOTE: I believe the information that reaches this function is correct in terms of power per ERS segment

            #region Sites

            var infraUse_peakkW_perSite = infraUsePerSegment
                .Where(n => n.Value.maxSingleVehicleKW > 0 && n.Key.Type != RouteSegmentType.Road)
                .ToDictionary(n => n.Key, n => UnitMath.Max(n.Value.maxSingleVehicleKW, n.Value.meankW * meanToPeak[n.Key.Type]));

            var builtStaticCharging = infraUse_peakkW_perSite
                .Where(n => n.Value > 0 && n.Key.Type != RouteSegmentType.Road)
                .ToDictionary(
                    place => place.Key,
                    place =>
                    {
                        var segment = place.Key;
                        var kW = place.Value;
                        var segmentUse = infraUsePerSegment[segment];
                        (KiloWatts inherited_kW, KiloWattsPerKilometer dummy) = inheritedSegmentPeakPower_kW.GetValueOrDefault(segment, (new KiloWatts(0), new KiloWattsPerKilometer(0)));
                        return InfrastructureCost.CalculateSiteUsageAndCost_excl_energy(segment, year, kW, inherited_kW, segmentUse.meanSocOnArrival, segmentUse.kWhPerYear);
                    });
            //Some sites may no longer be used. Build those too.
            foreach (var item in inheritedSegmentPeakPower_kW.Where(n => n.Key.Type != RouteSegmentType.Road && !builtStaticCharging.ContainsKey(n.Key)))
                builtStaticCharging.Add(item.Key, InfrastructureCost.CalculateSiteUsageAndCost_excl_energy(item.Key, year, new KiloWatts(0), item.Value.kW, new Dimensionless(0), new KiloWattHoursPerYear(0)));

            #endregion

            #region ERS

            //Note: Inherited ERS segments can become unused (e.g. due to system collapse). These will not be in infraUsePerSegment.
            var ersUsePerSegmentExpanded = infraUsePerSegment
                .Where(n => n.Key.Type == RouteSegmentType.Road && n.Key.ChargingOfferedFromYear <= year).ToDictionary(n => n.Key, n => n.Value);
            foreach (var segment in inheritedSegmentPeakPower_kW
                .Where(n => n.Key.Type == RouteSegmentType.Road && !ersUsePerSegmentExpanded.ContainsKey(n.Key)))
            {
                ersUsePerSegmentExpanded.Add(segment.Key, (new(0), new(0), new(0), new(0), new(0)));
            }

            var builtErs = ersUsePerSegmentExpanded
                .ToDictionary(n => n.Key, n =>
                {
                    (KiloWatts inherited_kW_segment, KiloWattsPerKilometer inherited_kWPerLaneKm) = inheritedSegmentPeakPower_kW.GetValueOrDefault(n.Key, (new KiloWatts(0), new KiloWattsPerKilometer(0)));
                    KiloWatts max_kWPerVehicle = n.Value.maxSingleVehicleKW;
                    KiloWatts mean_kW = n.Value.meankW;

                    KiloWattsPerKilometer used_kWPerLaneKm = UnitMath.Max(max_kWPerVehicle / Parameters.Infrastructure.ERS_grid_connection_interval_km[year], mean_kW * meanToPeak[n.Key.Type] / n.Key.LaneLengthToElectrifyOneOrBothWays_km);
                    KiloWattsPerKilometer installed_kWPerLaneKm = UnitMath.Max(used_kWPerLaneKm, inherited_kWPerLaneKm);

                    KiloWatts used_kW_segment = UnitMath.Max(max_kWPerVehicle, used_kWPerLaneKm * n.Key.LaneLengthToElectrifyOneOrBothWays_km);
                    KiloWatts installed_kW_segment = UnitMath.Max(max_kWPerVehicle, installed_kWPerLaneKm * n.Key.LaneLengthToElectrifyOneOrBothWays_km);

                    if (installed_kWPerLaneKm == 0) //This segment is not electrified
                        return new ChargingSiteCost();

                    KiloWattHoursPerYear energy_kWhPerYear = n.Value.kWhPerYear;
                    KiloWattHoursPerKilometerYear energyPerLaneKmYear = energy_kWhPerYear / n.Key.LaneLengthToElectrifyOneOrBothWays_km;

                    var euroPerKmYear_bidirectional = InfrastructureCost.Calculate_ERS_euro_per_kmYear(year, installed_kWPerLaneKm * new Dimensionless(2), energyPerLaneKmYear * new Dimensionless(2));

                    var euroPerYear_segment = euroPerKmYear_bidirectional * new Dimensionless(0.5f) * n.Key.LaneLengthToElectrifyOneOrBothWays_km;

                    return new ChargingSiteCost()
                    {
                        Cost_EuroPerYear = euroPerYear_segment,
                        EnergyDelivered_kWhPerYear = energy_kWhPerYear,
                        InstalledPower_kWPeak_PerLaneKm = installed_kWPerLaneKm,
                        InstalledPower_kWPeak_Segment = installed_kW_segment,
                        MeanSocOnArrival = n.Value.meanSocOnArrival,
                        UsedPower_kWPeak_Segment = used_kW_segment,
                        UsedPower_kWPeak_PerLaneKm = used_kWPerLaneKm,
                        PeakPowerIsGreaterThanInherited = installed_kW_segment > inherited_kW_segment
                    };
                });

            #endregion

            #region Outlier removal

            //Look for segments with exceptionally high cost per kWh

            var OUTLIER_THRESHOLD = new Dimensionless(4);
            HashSet<RouteSegment> outliers = new HashSet<RouteSegment>();
            foreach (RouteSegmentType t in Enum.GetValues(typeof(RouteSegmentType)))
            {
                var segments = t == RouteSegmentType.Road ? builtErs : builtStaticCharging.Where(n => n.Key.Type == t);

                EuroPerYear totalInfraCost = new EuroPerYear(segments.Where(n => n.Key.Type == t).Sum(n => n.Value.Cost_EuroPerYear.Val));
                KiloWattHoursPerYear totalEnergy = new KiloWattHoursPerYear(segments.Where(n => n.Key.Type == t).Sum(n => n.Value.EnergyDelivered_kWhPerYear.Val));
                EuroPerKiloWattHour costThreshold = (totalInfraCost / totalEnergy) * OUTLIER_THRESHOLD;

                outliers.UnionWith(segments.Where(n => n.Value.Cost_EuroPerkWh > costThreshold).Select(n => n.Key));
            }

            #endregion

            return (builtStaticCharging, builtErs, outliers);
        }

        public static (ChargingStrategy chargingStrategy, KiloWattHours netBatteryCapacity_kWh, RouteCost costPerKm) FindCheapestWayToTraverseRoute(
            ModelYear year,
            Route_VehicleType route_vt,
            Scenario scenario)
        {
            //Calculate costs associated with this route-vehicle type combination, in EURO PER YEAR
            // Held constant:
            //     Cargo (ton-km) per year
            //     Operating time per vehicle
            // Not held constant:
            //     Time to traverse route (delays) => driver costs
            //     Distance per vehicle per year (delays)
            //     Annual distancece-related maintenance costs
            //     Cargo per vehicle (vehicle weight)
            //     Trips per route per year (cargo per trip)
            //     Number of vehicles (total operating time)

            //Diesel
            //Per battery size
            //Modes with ERS, with pickup
            //Modes without ERS, without pickup

            (ChargingStrategy strategy, KiloWattHours netBatteryCapacity_kWh, EuroPerYear haulierCostPerRouteYear, RouteCost routeCost)
                bestStrategy = (ChargingStrategy.NA_Diesel, new KiloWattHours(float.MaxValue), new EuroPerYear(float.MaxValue), new RouteCost());

            if (Parameters.VERBOSE) Console.WriteLine("Strat\tBat\t" + RouteCost.ToStringHeader);

            List<(ChargingStrategy strat, KiloWattHours bat, RouteCost cost, EuroPerKilometer perkm)> costs = new List<(ChargingStrategy strat, KiloWattHours bat, RouteCost cost, EuroPerKilometer perkm)>();
            var allOptions = new List<(ChargingStrategy cs, KiloWattHours kWh, EuroPerYear haulierCost, RouteCost RouteCost)>();
            foreach (ChargingStrategy chargingStrategy in scenario.ChargingStrategies)
            {
                foreach (var netBatteryCapacity_kWh in scenario.BatteryOffers[route_vt.VehicleType].OrderByDescending(n => n))
                {
                    RouteCost routeCost = GetRouteTraversalCost_perKm(year, route_vt, scenario, chargingStrategy, netBatteryCapacity_kWh);

                    costs.Add((chargingStrategy, netBatteryCapacity_kWh, routeCost, routeCost is null ? null : routeCost.SystemCost_euroPerKm - routeCost.Driver_euroPerKm));

                    if (routeCost is null) //Couldn't traverse route, thus all smaller battery capacities will also fail
                        break;

                    if (Parameters.VERBOSE) Console.WriteLine(chargingStrategy + "\t" + netBatteryCapacity_kWh + "\t" + routeCost.ToString());

                    var haulierCost = routeCost.HaulierCost_euroPerYear;

                    allOptions.Add((chargingStrategy, netBatteryCapacity_kWh, haulierCost, routeCost));

                    if (haulierCost < bestStrategy.haulierCostPerRouteYear)
                    {
                        if (chargingStrategy.UsesDepot() && !routeCost.UsedChargerTypes.Contains(RouteSegmentType.Depot))
                            bestStrategy = (chargingStrategy.GetWithoutDepot(), netBatteryCapacity_kWh, haulierCost, routeCost);
                        else
                            bestStrategy = (chargingStrategy, netBatteryCapacity_kWh, haulierCost, routeCost);
                    }

                    //No need to try different battery sizes with diesel
                    if (chargingStrategy == ChargingStrategy.NA_Diesel)
                        break;
                }
            }

            //if (bestStrategy.strategy == ChargingStrategy.NA_Diesel)
            //    Console.Write(allOptions.Count);

            return (bestStrategy.strategy, bestStrategy.netBatteryCapacity_kWh, bestStrategy.routeCost);
        }

        public static RouteCost GetRouteTraversalCost_perKm(ModelYear year,
            Route_VehicleType route_vt,
            Scenario scenario,
            ChargingStrategy chargingStrategy, 
            KiloWattHours netBatteryCapacity_kWh)
        {
            //Can't use a battery that cannot age a bit without needing replacement
            if (chargingStrategy != ChargingStrategy.NA_Diesel && netBatteryCapacity_kWh * Parameters.Battery.Net_Max_permitted_discharging_rate_c[year] < route_vt.VehicleType.Common_Power_peak_kW[year] * new Dimensionless(1.1f))
                return null;

            //TODO: Replace initial SoC with mean of incoming routes?
            //If SoC=1, vehicles start with free energy (before charging at depot)
            //If SoC=0, vehicles lose bought energy (after charging at destination)
            var traversalCost = GetRouteTraversalCost_perTraversal(year, route_vt, scenario.InfraOffers, chargingStrategy, netBatteryCapacity_kWh, new Dimensionless(0.5f));
            if (!traversalCost.success)
                return null;

            var route_trips_per_year = route_vt.GetTotalTripCountPerYear(year);
            var tcost = traversalCost.cost;//perTraversalCost;
            var vt = route_vt.VehicleType;
            var driverTime_h = tcost.Traversal_h_Total - vt.GetNormalizedDepotTime_h(year, route_vt.Route.Length_h_excl_breaks);
            var route_length_km = route_vt.Route.Length_km;
            var route_km_per_year = new KilometersPerYear(route_length_km.Val * route_trips_per_year.Val);
            var single_vehicle_km_per_year = vt.Common_Annual_distance_km[year];
            var meanCargoWeightPerTrip = route_trips_per_year > 0 ? new Tonnes(route_vt.GetCargoTonnesPerYear(year).Val / route_trips_per_year.Val) : new Tonnes(0);

            RouteCost costPerKm;
            if (chargingStrategy == ChargingStrategy.NA_Diesel)
            {
                costPerKm = new RouteCost()
                {
                    RouteIsElectrified = false,
                    RatioOfOperationInSweden = tcost.Traversal_h_InSweden / (tcost.Traversal_h_Total + new Hours(0.0001f)), //Avoid division by zero
                    TotalAnnualRouteKm = route_km_per_year,
                    TotalAnnualRouteTonKm = meanCargoWeightPerTrip * route_km_per_year,
                    BatteryAgeing_euroPerKm = new EuroPerKilometer(0),
                    BatteryAgeing_kWhPerKm = new KiloWattHoursPerKilometer(0),
                    ErsPickupAgeing_euroPerKm = new EuroPerKilometer(0),
                    Driver_euroPerKm = vt.Common_Driver_cost_euro_per_h[year] * driverTime_h / route_length_km,
                    Energy_euroPerKm = Parameters.World.Diesel_Price_euro_per_liter[year] * tcost.Diesel_liter / route_length_km,
                    Road_and_pollution_tax_euroPerKm = (Parameters.World.Diesel_Road_tax_euro_per_liter[year] + Parameters.World.Diesel_Pollution_tax_euro_per_liter[year]) * vt.ICEV_Fuel_consumption_liter_per_km[year],
                    CO2_euroPerKm_total = Parameters.World.CO2_SCC_euro_per_kg[year] * (Parameters.World.Diesel_Emissions_kg_CO2_per_liter[year] * tcost.Diesel_liter) / route_length_km,
                    CO2_euroPerKm_internalized = Parameters.World.Diesel_CO2_tax_euro_per_liter[year] * tcost.Diesel_liter / route_length_km,
                    VehicleAgeing_euroPerKm = vt.ICEV_ChassisAndMaintenance_cost_euro_per_km(year),
                    Interest_euroPerKm = GetMeanAnnualInterestPayment(Parameters.World.Economy_Private_sector_interest_public_charging_and_trucks_percent[year], vt.ICEV_Chassis_cost_euro[year]) / single_vehicle_km_per_year,
                    TripCountMultiplier = new Dimensionless(1),
                    InfrastructureFee_approximate_euroPerKm = new EuroPerKilometer(0),
                };
            }
            else
            {
                //Calculate the cost of traversing the route as the sum of several components

                //Distance induced costs 
                //battery ageing (total = cycling + calendar)
                //How long will a battery last? The battery can become insufficient due to loss of max power or loss of range

                //Account for that longer time per route reduces annual mileage per truck (holding truck operating time constant)
                //BUGFIX: I believe this should be distance on route, not per vehicle
                //Dimensionless tripTimeMultiplier = tcost.OfWhichIsDelay_h / tcost.Traversal_h_Total + 1;
                //KilometersPerYear route_km_per_year_adjusted = route_km_per_year * (1 / tripTimeMultiplier);

                bool hasPickup = chargingStrategy.HasErsPickup() &&
                    traversalCost.infraUse_perTraversal.Any(n => n.Key.Type == RouteSegmentType.Road); //remove the pickup if there was no opportunity to use it

                //Calculate how long it takes for the battery to reach the reference EoL
                //TODO: I think there should be some special handling here of days without driving
                var annualCalendarAgeing = new OtherUnit(365 * 24 * tcost.BatteryAgeing_Calendar_RatioOfReferenceLifetime.Val / tcost.Traversal_h_Total.Val);
                var annualCycleAgeing = new OtherUnit(vt.Common_Annual_distance_km[year].Val * tcost.BatteryAgeing_Cycling_RatioOfLifetime.Val / route_length_km.Val);
                var yearsToRefEolThreshold = new Years(1 / (annualCalendarAgeing.Val + annualCycleAgeing.Val));

                var refLossAtEoL = (1 - Parameters.Battery.Net_End_of_life_definition_ratio_of_net[year]);
                var annualLoss_ratioOfNet = new OtherUnit(refLossAtEoL.Val * (annualCalendarAgeing.Val + annualCycleAgeing.Val));
                var tripsPerYear = new OtherUnit(vt.Common_Annual_distance_km[year].Val / route_length_km.Val);

                //Calculate how long the battery can be used if the lifetime is limited by minimum output power and minimum range
                KiloWatts initialPower = netBatteryCapacity_kWh * Parameters.Battery.Net_Max_permitted_discharging_rate_c[year];
                Dimensionless eolPower_lossRatioThreshold = vt.Common_Power_peak_kW[year] / initialPower;
                Dimensionless eolRange_lossRatioThreshold = 1 - traversalCost.minChargeAlongRoute_kWh / netBatteryCapacity_kWh;
                //The reference end of life was 80% or some other parameter value. But it may be different for this vehicle on this route.
                Dimensionless lossWhenUnusableByVehicle_ratioOfNet = UnitMath.Min(new(0.95f), UnitMath.Max(eolPower_lossRatioThreshold, eolRange_lossRatioThreshold));
                //Even if this route isn't too demanding, assume that only 20% (ref) of initial range can be lost. The battery size was motivated by some other route.
                lossWhenUnusableByVehicle_ratioOfNet = UnitMath.Min(lossWhenUnusableByVehicle_ratioOfNet, Parameters.Battery.Net_End_of_life_definition_ratio_of_net[year]);

                var maxUsableLifetimeInVehicle_years = yearsToRefEolThreshold * (lossWhenUnusableByVehicle_ratioOfNet / refLossAtEoL);

                var vehicleLifetime_years = vt.BEV_Lifetime_years[year];
                var lifetimeInVehicle_years = UnitMath.Min(vehicleLifetime_years, maxUsableLifetimeInVehicle_years);
                var lossAtEoLInVehicle_ratioOfNet = new Dimensionless(lifetimeInVehicle_years.Val * annualLoss_ratioOfNet.Val);

                //var lossPerTrip_ratioOfNet = new OtherUnit(annualLoss_ratioOfNet.Val / tripsPerYear.Val);

                //Calculate the residual value at EoL in the vehicle
                Dimensionless residualValue_ratio = GetResidualBatteryValue_RatioOfNetPurchasePrice(year, lifetimeInVehicle_years, 1-lossAtEoLInVehicle_ratioOfNet);

                Kilometers batteryLifetimeInVehicle_km = vt.Common_Annual_distance_km[year] * lifetimeInVehicle_years;

                //TODO: This line effectively assumes that all future battery replacements have the same cost as the initial battery. In reality, replacement batteries may cost less.
                EuroPerKilometer batteryAgeing_euroPerKm =  Parameters.Battery.Net_Pack_cost_euro_per_kWh[year] * netBatteryCapacity_kWh * (1 - residualValue_ratio) / batteryLifetimeInVehicle_km;

                //The goal is to calculate the ratio at which we need to replentish vehicle batteries, so the consumption rate of virgin batteries. The entire battery pack is spent as a vehicle battery at the EoL in the truck.
                var grossBatteryCapacity_kWh = netBatteryCapacity_kWh / Parameters.Battery.Gross_SoC_window_ratio[year];
                KiloWattHoursPerKilometer grossConsumptionOfVehicleQualityBatteries_kwhPerKm = route_vt.Route.Length_km > 0 ? grossBatteryCapacity_kWh / batteryLifetimeInVehicle_km : new(0);

                //energy
                EuroPerKilometer energy_euroPerKm = tcost.ElectricityPurchased_Euro / route_length_km;
                //emissions
                float batteryScrapSoH = Math.Min(0.7f, lossWhenUnusableByVehicle_ratioOfNet.Val);
                Dimensionless emissionsAllocatedToThisTruck_ratio = new Dimensionless(Math.Min(1, lossWhenUnusableByVehicle_ratioOfNet.Val / batteryScrapSoH));
                KilogramPerKilometer co2_battery_kgPerKm = (Parameters.Battery.Net_Production_emissions_kg_CO2_per_kWh[year] * netBatteryCapacity_kWh * emissionsAllocatedToThisTruck_ratio) / batteryLifetimeInVehicle_km;
                EuroPerKilometer co2_euroPerKm_total = (tcost.CO2_kg / route_length_km + co2_battery_kgPerKm) * Parameters.World.CO2_SCC_euro_per_kg[year];
                EuroPerKilometer co2_euroPerKm_internalized = co2_euroPerKm_total * Parameters.World.CO2_Tax_ratio_of_SCC[year];
                
                //distance ageing of vehicle and pick-up
                EuroPerKilometer vehicle_euroPerKm = vt.BEV_ChassisAndMaintenance_cost_euro_per_km(year);
                EuroPerKilometer ersPickup_euroPerKm = new EuroPerKilometer(0);
                if (hasPickup && route_km_per_year > 0)
                {
                    Euro pickupBase_euro = Parameters.Infrastructure.ERS_pick_up_cost_base_heavy_euro[year];
                    KiloWatts ers_kWMax = scenario.InfraOffers.AvailablePowerPerUser_kW[RouteSegmentType.Road];
                    KiloWatts vehicle_kW_prop = vt.GetPowerConsumption_kW(year, new KilometersPerHour(90), netBatteryCapacity_kWh, hasPickup);
                    KiloWatts vehicle_kWMax = vehicle_kW_prop + netBatteryCapacity_kWh * Parameters.Battery.Net_Max_permitted_charging_rate_c[year];
                    Euro pickupPower_euro = Parameters.Infrastructure.ERS_pick_up_cost_euro_per_kW[year] * UnitMath.Min(ers_kWMax, vehicle_kWMax);
                    Kilometers pickupLifespan_km = single_vehicle_km_per_year * Parameters.Infrastructure.ERS_pick_up_lifespan_years[year];
                    ersPickup_euroPerKm = (pickupBase_euro + pickupPower_euro) / pickupLifespan_km;
                }
                //infrastructure fees
                EuroPerKilometer infraFees_euroPerKm_approximate = tcost.InfraFees_euro_approximate / route_length_km; //FIXED BUG: Was annual distance

                //Time induced costs
                //These costs are per hour. If the time to traverse the route increases, so do the per-km costs.
                EuroPerKilometer driver_euroPerKm = vt.Common_Driver_cost_euro_per_h[year] * driverTime_h / route_length_km;
                Euro bevPrice_total = vt.BEV_Chassis_cost_excl_battery_euro[year] + (hasPickup ? Parameters.Infrastructure.ERS_pick_up_cost_base_heavy_euro[year] : new Euro(0)) + Parameters.Battery.Net_Pack_cost_euro_per_kWh[year] * netBatteryCapacity_kWh;
                EuroPerKilometer interest_euroPerKm = GetMeanAnnualInterestPayment(Parameters.World.Economy_Private_sector_interest_public_charging_and_trucks_percent[year], bevPrice_total) / single_vehicle_km_per_year;

                //Account for change in cargo carrying capacity per vehicle (holding annual tonnes per route constant)
                Dimensionless bevCargoCapacity_ratio = vt.BEV_Carrying_capacity_ratio_of_ICEV(year, netBatteryCapacity_kWh, hasPickup);

                Dimensionless tripCountMultiplier = 1 / bevCargoCapacity_ratio;

                costPerKm = new RouteCost()
                {
                    RouteIsElectrified = true,
                    RatioOfOperationInSweden = tcost.Traversal_h_InSweden / (tcost.Traversal_h_Total + new Hours(0.0001f)), //Avoid division by zero
                    TotalAnnualRouteKm = route_km_per_year * tripCountMultiplier,
                    TotalAnnualRouteTonKm = meanCargoWeightPerTrip * route_km_per_year * tripCountMultiplier * bevCargoCapacity_ratio,
                    BatteryAgeing_euroPerKm = batteryAgeing_euroPerKm,
                    BatteryAgeing_kWhPerKm = grossConsumptionOfVehicleQualityBatteries_kwhPerKm,
                    ErsPickupAgeing_euroPerKm = ersPickup_euroPerKm,
                    Energy_euroPerKm = energy_euroPerKm,
                    Road_and_pollution_tax_euroPerKm = Parameters.World.Electricity_Road_tax_euro_per_kWh[year] * vt.GetEnergyConsumption_kWh_per_km(year, netBatteryCapacity_kWh, hasPickup),
                    CO2_euroPerKm_total = co2_euroPerKm_total,
                    CO2_euroPerKm_internalized = co2_euroPerKm_internalized,
                    VehicleAgeing_euroPerKm = vehicle_euroPerKm,
                    Driver_euroPerKm = driver_euroPerKm,
                    Interest_euroPerKm = interest_euroPerKm,
                    TripCountMultiplier = tripCountMultiplier,
                    InfrastructureFee_approximate_euroPerKm = infraFees_euroPerKm_approximate
                };
                foreach (var item in traversalCost.infraUse_perTraversal)
                {
                    costPerKm.InfraUsePerTraversal.Add(item.Key, item.Value);
                }
                costPerKm.UsedChargerTypes.UnionWith(tcost.UsedChargerTypes);
            }
            return costPerKm;
        }

        public static (bool success, KiloWattHours minChargeAlongRoute_kWh, CostPerRouteTraversal cost, Dictionary<RouteSegment, (KiloWatts kW, Hours h, KiloWattHours kWh, Kilometers km, Dimensionless soc)> infraUse_perTraversal) GetRouteTraversalCost_perTraversal(
            ModelYear year,
            Route_VehicleType route,
            InfraOffers infraOffers,
            //Dictionary<RouteSegmentType, float> referenceUserCost_euroPerKWh,
            ChargingStrategy chargingStrat,
            KiloWattHours netBatteryCapacity_kWh,
            Dimensionless initial_SoC)
        {
            (HashSet<int> plannedStops, bool includesRestArea) = FindPlannedStops(year, route, chargingStrat);

            //If we don't stop at rest areas and can't charge at the destination, a strategy relying on public static charging can't be best
            //if (!includesRestArea && chargingStrat.IsMultistopStrategy() && route.Route.SegmentSequence.Last().ChargingOfferedFromYear > year)
            //    return (false, 0, null, null);

            var infraUse_perTraversal = new Dictionary<RouteSegment, (KiloWatts kW, Hours h, KiloWattHours kWh, Kilometers km, Dimensionless SoC)>();

            //If the vehicle runs on diesel, return diesel consumption and default traversal time
            if (chargingStrat == ChargingStrategy.NA_Diesel)
            {
                LiterPerKilometer fuelLiterPerKm = route.VehicleType.ICEV_Fuel_consumption_liter_per_km[year];

                Liter liter = new Liter(0);
                Hours h_inSweden = new Hours(0), h_abroad = new Hours(0);
                foreach (var s in route.Route.SegmentSequence)
                {
                    Hours h = s.Type switch
                    {
                        RouteSegmentType.Road => plannedStops.Contains(s.ID) ? route.VehicleType.Common_Rest_stop_h[year] : s.LengthToTraverse_h,
                        RouteSegmentType.Depot => route.VehicleType.GetNormalizedDepotTime_h(year, route.Route.Length_h_excl_breaks),
                        RouteSegmentType.Destination => route.VehicleType.Common_Destination_stop_h[year],
                        RouteSegmentType.RestStop => plannedStops.Contains(s.ID) ? route.VehicleType.Common_Rest_stop_h[year] : new Hours(0),
                        _ => throw new NotImplementedException()
                    };
                    liter += fuelLiterPerKm * s.LengthToTraverseOneWay_km;
                    if (s.PlaceHash.IsInSweden)
                        h_inSweden += h;
                    else
                        h_abroad += h;
                }
                
                CostPerRouteTraversal cost = new CostPerRouteTraversal()
                {
                    Diesel_liter = liter,
                    CO2_kg = Parameters.World.Diesel_Emissions_kg_CO2_per_liter[year] * liter,
                    Traversal_h_InSweden = h_inSweden,
                    Traversal_h_Abroad = h_abroad
                };
                return (true, null, cost, infraUse_perTraversal);
            }

            //Else, the vehicle runs on electricity. Compute the full TraversalCost

            var cumulativeCost = new CostPerRouteTraversal();
            KiloWattHours currentCharge_kWh = netBatteryCapacity_kWh * initial_SoC;
            KiloWattHours minChargeAlongRoute_kWh = currentCharge_kWh;
            if (Parameters.VERBOSE) Console.Write(chargingStrat + " \t");
            foreach (var segment in route.Route.SegmentSequence)
            {
                var step = GetSegmentTraversalCost(currentCharge_kWh, plannedStops.Contains(segment.ID), year, route.Route.Length_h_excl_breaks, segment, infraOffers, route.VehicleType, chargingStrat, netBatteryCapacity_kWh);
                cumulativeCost.Add(step.cost);

                if (!float.IsNormal(step.delta_kWh.Val) && step.delta_kWh != 0)
                {
                    Console.WriteLine("Problem with route " + route.Route.ID + ", segment " + segment.ID + ":" + segment.ToString());
                    Console.WriteLine(step.delta_kWh);
                    continue;
                }
                currentCharge_kWh += step.delta_kWh;

                //If everything still works as intended but the battery still goes below the minimum acceptable range buffer, reject this solution
                if (currentCharge_kWh < route.VehicleType.GetEnergyConsumption_kWh_per_km(year, netBatteryCapacity_kWh, chargingStrat.HasErsPickup()) * route.VehicleType.BEV_Min_range_buffer_km[year])
                {
                    if (Parameters.VERBOSE) Console.WriteLine("-X");
                    return (false, new KiloWattHours(0), null, null);
                }

                minChargeAlongRoute_kWh = UnitMath.Min(minChargeAlongRoute_kWh, currentCharge_kWh);

                if (step.cost.ElectricityPurchased_kWh > 0)
                {
                    //TODO: Figure out how one route can traverse the same segment more than once.
                    Dimensionless SoCOnArrival = (currentCharge_kWh - step.delta_kWh) / netBatteryCapacity_kWh;
                    infraUse_perTraversal.TryAdd(segment, (step.infra_kW, step.cost.Traversal_h_Total, step.cost.ElectricityPurchased_kWh, segment.LengthToTraverseOneWay_km, SoCOnArrival));
                }
            }

            if (Parameters.VERBOSE) Console.WriteLine("-O");

            KiloWattHoursPerKilometer kWhPerKm = route.VehicleType.GetEnergyConsumption_kWh_per_km(year, netBatteryCapacity_kWh, chargingStrat.HasErsPickup());
            KiloWattHours kWhSpentPerTraversal = kWhPerKm * route.Route.Length_km;
            KiloWattHours bufferKWh = kWhPerKm * route.VehicleType.BEV_Min_range_buffer_km[year];
            bool tripNeedsMoreThanHalfBattery = kWhSpentPerTraversal * new Dimensionless(2) > (netBatteryCapacity_kWh - bufferKWh);
            Dimensionless gainToSpendRatio = cumulativeCost.ElectricityPurchased_kWh / kWhSpentPerTraversal;
            //TODO: For short routes without depot or destination charging available and which don't travel much on ERS, there is no way to charge. 
            //      It seems realistic to assume that the vehicle would charge on another route. 
            if (gainToSpendRatio < 0.75f || (tripNeedsMoreThanHalfBattery && gainToSpendRatio < 1)) //TODO: Is this threshold of 0.75 good? 
            {
                return (false, new KiloWattHours(0), null, null); //this route does not provide sufficient charging
            }

            var normCost = cumulativeCost.GetNormalized(kWhSpentPerTraversal);

            foreach (var key in infraUse_perTraversal.Keys.ToArray())
            {
                var item = infraUse_perTraversal[key];
                //Normalization (to make the sums match) can result in power draw greater than what is supplied by the infrastructure. So be it.
                infraUse_perTraversal[key] = (item.kW * normCost.scaleFactor, item.h, item.kWh * normCost.scaleFactor, item.km, item.SoC);
            }

            return (true, minChargeAlongRoute_kWh, normCost.cost, infraUse_perTraversal);
        }

        public static (KiloWattHours delta_kWh, CostPerRouteTraversal cost, KiloWatts infra_kW) GetSegmentTraversalCost(
            KiloWattHours batterySoC_kWh,
            bool segmentIsPlannedStop,
            ModelYear year,
            Hours routeLength_h,
            RouteSegment segment,
            InfraOffers infraOffers,
            //Dictionary<RouteSegmentType, float> referenceUserCost_euroPerKWh,
            VehicleType vehicleType,
            ChargingStrategy chargingStrat,
            KiloWattHours netBatteryCapacity_kWh)
        {
            //flows
            var flows = GetEnergyFlows(batterySoC_kWh, segmentIsPlannedStop, year, routeLength_h, segment, infraOffers, vehicleType, chargingStrat, netBatteryCapacity_kWh);
            var delta_kWh = flows.charging_kWh - flows.discharging_kWh;

            //ageing (state of health reaches 0 at end of use, or 80% capacity loss)
            (Dimensionless cycleAgeing_ratio, Dimensionless calendarAgeing_ratio) = Parameters.Battery.GetStateOfHealthLoss_ratioOfLifetime(year, flows.dwellTime_h, flows.charging_kWh, flows.netChargingCRate, flows.discharging_kWh, flows.netDischargingCRate, netBatteryCapacity_kWh);

            bool inSweden = segment.PlaceHash.IsInSweden;
            var cost = new CostPerRouteTraversal()
            {
                BatteryAgeing_Calendar_RatioOfReferenceLifetime = calendarAgeing_ratio,
                BatteryAgeing_Cycling_RatioOfLifetime = cycleAgeing_ratio,
                Traversal_h_Abroad = inSweden ? new Hours(0) : flows.dwellTime_h,
                Traversal_h_InSweden = inSweden ? flows.dwellTime_h : new Hours(0),
                OfWhichIsDelay_h = flows.ofWhichIsDelay_h,
                ElectricityPurchased_Euro = Parameters.GetMeanElectricityPrice(year, segment.Type, segment.Region) * flows.energyPurchased_kWh,
                ElectricityPurchased_kWh = flows.energyPurchased_kWh,
                CO2_kg = Parameters.GetCO2_kgPerkWh(year, segment.Region) * flows.energyPurchased_kWh,
                InfraFees_euro_approximate = flows.infraFees_euro,
            };
            if (flows.energyPurchased_kWh > 0)
                cost.UsedChargerTypes.Add(segment.Type);
            return (delta_kWh, cost, flows.totalPowerDraw_kW);
        }

        public static Dimensionless GetResidualBatteryValue_RatioOfNetPurchasePrice(
            ModelYear yearOfManufacture,
            Years lifeLength_Years,
            Dimensionless stateOfHealthAtEndOfLife_ratio)
        {
            //The battery residual value:
            //  declines linearly with state of health
            //  declines with the purchase price of new batteries
            //  increases with a sigmoid representing a maturing recycling market

            //Early retirement example:
            //(0.9 - 0.8) / (1 - 0.8) = 0.5
            //default value at eol = 0.3
            //actual value = 1 - (1 - 0.3) * 0.5 = 0.65
            //assume the default residual value is the scrap value, i.e. the lower bound
            float eolYear = yearOfManufacture.AsInteger() + lifeLength_Years.Val;
            var valueLossAtReferenceEoLSoH_ratio = new Dimensionless(1 - 0.3f / (1f + (float)Math.Exp(-(eolYear - 2035) * 0.3)));
            var newPriceAtEol_ratio = Parameters.Battery.Net_Pack_cost_euro_per_kWh.GetInterpolatedValue(eolYear) / Parameters.Battery.Net_Pack_cost_euro_per_kWh[yearOfManufacture];

            var ageingAtEol_ratio = 1 - stateOfHealthAtEndOfLife_ratio;
            var refAgeingAtEol_ratio = 1 - Parameters.Battery.Gross_End_of_life_definition_ratio_of_gross[yearOfManufacture];
            var ageingRelativeToRefEoL = ageingAtEol_ratio / refAgeingAtEol_ratio;
            var valueRemainingAtEol_ratioOfPurchasePrice = 1 - UnitMath.Min(new Dimensionless(1), ageingRelativeToRefEoL * valueLossAtReferenceEoLSoH_ratio);

            return valueRemainingAtEol_ratioOfPurchasePrice * newPriceAtEol_ratio;
            //return (1 - valueLossAtReferenceEoLSoH_ratio * spentUsefulLife_ratio) * newPriceAtEol_ratio;
        }

        public static (Hours dwellTime_h, Hours chargingTime_h, Hours ofWhichIsDelay_h, KiloWatts discharging_kW, KiloWatts charging_kW, KiloWattHours discharging_kWh, KiloWattHours charging_kWh, CRate netDischargingCRate, CRate netChargingCRate, KiloWatts totalPowerDraw_kW, KiloWattHours energyPurchased_kWh, Euro infraFees_euro)
            GetEnergyFlows(
            KiloWattHours netBatterySoc_Kwh,
            bool segmentIsPlannedStop,
            ModelYear year,
            Hours routeLength_h,
            RouteSegment segment,
            InfraOffers infra,
            VehicleType vehicleType,
            ChargingStrategy chargingStrat,
            KiloWattHours netBatteryCapacity_kWh)
        {
            //What user fees do we need to pay? Not treated as a system cost, but an approximate value is needed to compare charging strategies.

            KiloWatts powerConsumption_kW = vehicleType.GetPowerConsumption_kW(year, segment.Speed_Kmph, netBatteryCapacity_kWh, chargingStrat.HasErsPickup()); //Assumes speed is ~0 if this segment represents a stop
            (Hours minDwellTime_h, bool waitToCharge) = chargingStrat.DwellTime_h(year, segment, vehicleType, routeLength_h, segmentIsPlannedStop);

            //Do we want to charge here?
            Dimensionless soc = netBatterySoc_Kwh / netBatteryCapacity_kWh;
            ChargingMode wantToChargeHere = chargingStrat.ChargeIfPossible(segment.Type, segmentIsPlannedStop, soc);
            bool canChargeHere = !InfrastructureCost.SegmentIsBlacklisted(year, segment)
                && segment.ChargingOfferedFromYear <= year 
                && !(segmentIsPlannedStop && segment.Type == RouteSegmentType.Road); //Some strategies allow rest stops along roads
            //No, we only discharge
            if (minDwellTime_h == 0 || !(wantToChargeHere != ChargingMode.None && canChargeHere))
                return (minDwellTime_h, new Hours(0), new Hours(0), powerConsumption_kW, new KiloWatts(0), powerConsumption_kW * minDwellTime_h, new KiloWattHours(0), powerConsumption_kW / netBatteryCapacity_kWh, new CRate(0), new KiloWatts(0), new KiloWattHours(0), new Euro(0));

            //Yes, we want to and can charge here
            KiloWatts availablePowerFromInfra_kW = infra.AvailablePowerPerUser_kW[segment.Type];
            KiloWatts infraToPropulsion_kW = UnitMath.Min(availablePowerFromInfra_kW, powerConsumption_kW);
            KiloWatts availableForCharging_kW = wantToChargeHere == ChargingMode.Charging ? UnitMath.Max(new KiloWatts(0), availablePowerFromInfra_kW - powerConsumption_kW) : new KiloWatts(0);

            KiloWattHours chargingCap_kWh = netBatteryCapacity_kWh - netBatterySoc_Kwh;
            KiloWatts chargingCap_kW = netBatteryCapacity_kWh * Parameters.Battery.Net_Max_permitted_charging_rate_c[year];
            KiloWatts charging_kW = UnitMath.Min(UnitMath.Min(chargingCap_kW, availableForCharging_kW), chargingCap_kWh / minDwellTime_h);
            KiloWatts discharging_kW = UnitMath.Max(new KiloWatts(0), powerConsumption_kW - infraToPropulsion_kW);

            Hours actualDwellTime_h = minDwellTime_h;
            if (waitToCharge && chargingCap_kW > 0 && charging_kW > 0)
                actualDwellTime_h = UnitMath.Max(minDwellTime_h, chargingCap_kWh / charging_kW);
            Hours chargingTime_h = actualDwellTime_h;
            //Is this an extra stop only for charging?
            if (segment.Type == RouteSegmentType.RestStop && !segmentIsPlannedStop)
                actualDwellTime_h += Parameters.World.Charging_Extra_stop_overhead_h[year];
            Hours ofWhichIsDelay_h = (segmentIsPlannedStop || segment.Type == RouteSegmentType.Road) ? actualDwellTime_h - minDwellTime_h : actualDwellTime_h;

            KiloWattHours discharging_kWh = UnitMath.Min(netBatterySoc_Kwh, discharging_kW * actualDwellTime_h);
            KiloWattHours charging_kWh = charging_kW * chargingTime_h;
            CRate netDischargingCRate = discharging_kW / netBatteryCapacity_kWh;
            CRate netChargingCRate = charging_kW / netBatteryCapacity_kWh;
            KiloWattHours energyPurchased_kWh = charging_kWh + infraToPropulsion_kW * actualDwellTime_h;
            KiloWatts totalPowerDraw_kW = charging_kW + infraToPropulsion_kW;

            Euro infraFees_euro = new(0);
            if (energyPurchased_kWh > 0)
            {
                EuroPerKiloWattHour feePerkWh;
                if (segment.Type == RouteSegmentType.Road)
                    feePerkWh = InfrastructureCost.GetErsUserFee_CachedOrEstimated_excl_energy(year, infra);
                else
                    feePerkWh = InfrastructureCost.GetSiteUserFee_CachedOrEstimated_excl_energy(segment, year, infra.AvailablePowerPerUser_kW[segment.Type]);
                infraFees_euro = feePerkWh * energyPurchased_kWh;
            }

            return (actualDwellTime_h, chargingTime_h, ofWhichIsDelay_h, discharging_kW, charging_kW, discharging_kWh, charging_kWh, netDischargingCRate, netChargingCRate, totalPowerDraw_kW, energyPurchased_kWh, infraFees_euro);
        }

        private static (HashSet<int>, bool includesRestArea) FindPlannedStops(ModelYear year, Route_VehicleType route, ChargingStrategy chargingStrat)
        {
            //This method figures out where the vehicle would prefer to stop, given its transport mission.

            bool includesRestArea = false;
            HashSet<int> plannedStops = new HashSet<int>();
            Hours shift_h = new Hours(0);
            (int i, Hours h) i_lastRestStop = (int.MinValue, new Hours(0));
            (int i, Hours h) i_lastRestStopWithCharger = (int.MinValue, new Hours(0));
            for (int i = 0; i < route.Route.SegmentSequence.Length; i++)
            {
                var segment = route.Route.SegmentSequence[i];
                switch (segment.Type)
                {
                    case RouteSegmentType.Depot:
                    case RouteSegmentType.Destination:
                        plannedStops.Add(segment.ID);
                        shift_h = new Hours(0);
                        break;
                    case RouteSegmentType.Road:
                        shift_h += segment.LengthToTraverse_h;
                        break;
                    case RouteSegmentType.RestStop:
                        //Do nothing here. This check comes last.
                        break;
                    default:
                        throw new NotImplementedException();
                }

                //Is it time to rest?
                if (shift_h >= route.VehicleType.Common_Drive_session_h[year])
                {
                    bool canBacktrackToCharge = i_lastRestStopWithCharger.i != int.MinValue;
                    bool canBacktrackToRest = i_lastRestStop.i != int.MinValue;

                    //This was the logic in the 2022 report. Rest stops without charging were ignored.
                    //bool takeExtraChargingStops = chargingStrat.TakeExtraChargingStops();
                    //bool wantToBacktrack = chargingStrat.RestEarlierToCharge();
                    //Hours backtrackThreshold = route.VehicleType.Common_Drive_session_h[year] * new Dimensionless(takeExtraChargingStops ? 0.8f : 0.5f); //Bug? Numbers in wrong order?
                    //bool wontBacktrackTooFar = i_lastRestStopWithCharger.h > backtrackThreshold;
                    //if (wantToBacktrack && canBacktrack && wontBacktrackTooFar)

                    float restStopBacktrackThreshold = 0.9f;
                    float chargingBacktrackThreshold = chargingStrat.TakeExtraChargingStops() ? 0.5f : chargingStrat.RestEarlierToCharge() ? 0.75f : 1f;
                    bool backtrackToCharge = i_lastRestStopWithCharger.h > chargingBacktrackThreshold;
                    bool backtrackToRest = i_lastRestStop.h > restStopBacktrackThreshold;
                    if (canBacktrackToCharge && backtrackToCharge)
                    {
                        //We return to stop earlier at a rest area where we didn't charge
                        RouteSegment stopSegment = route.Route.SegmentSequence[i_lastRestStopWithCharger.i];
                        plannedStops.Add(stopSegment.ID);
                        includesRestArea = includesRestArea || (stopSegment.Type == RouteSegmentType.RestStop);
                        i = i_lastRestStopWithCharger.i;
                        shift_h = new Hours(0);
                    }
                    else if (canBacktrackToRest && backtrackToRest)
                    {
                        //We return to stop earlier at a rest area
                        RouteSegment stopSegment = route.Route.SegmentSequence[i_lastRestStop.i];
                        plannedStops.Add(stopSegment.ID);
                        includesRestArea = includesRestArea || (stopSegment.Type == RouteSegmentType.RestStop);
                        i = i_lastRestStop.i;
                        shift_h = new Hours(0);
                    }
                    else
                    {
                        //We are forced to take a break along the road
                        plannedStops.Add(segment.ID);
                        includesRestArea = includesRestArea || (segment.Type == RouteSegmentType.RestStop);
                        shift_h = new Hours(0);
                    }
                    i_lastRestStop = (int.MinValue, new Hours(0));
                    i_lastRestStopWithCharger = (int.MinValue, new Hours(0));
                }

                if (segment.Type == RouteSegmentType.RestStop)
                {
                    i_lastRestStop = (i, shift_h);
                    if (segment.ChargingOfferedFromYear <= year)
                        i_lastRestStopWithCharger = (i, shift_h);
                }
            }
            return (plannedStops, includesRestArea);
        }

        public static EuroPerYear GetMeanAnnualInterestPayment(OtherUnit interestRate, Euro initialBorrowedCapital)
        {
            //Assume that the loan is paid off over time.
            return new EuroPerYear(interestRate.Val * initialBorrowedCapital.Val / 2);
        }

        private static void IncrementInfraUse(
            ConcurrentDictionary<RouteSegment, (KiloWatts meankW, KiloWatts maxSingleVehiclePeakKW, KiloWattHoursPerYear kWhPerYear, Dimensionless socOnArrival, OtherUnit aadt)> infraUsePerSegment,
            Dictionary<RouteSegment, (KiloWatts kW, Hours h, KiloWattHours kWh, Kilometers km, Dimensionless socOnArrival)> infraUsePerTraversal,
            OtherUnit totalTripCountPerYear)
        {
            if (infraUsePerTraversal is null)
                return;

            foreach (var infra in infraUsePerTraversal)
            {
                (var kW, var h, var kWh, var km, var socOnArrival) = infra.Value;
                var mean_kW = kW * new Dimensionless(h.Val * totalTripCountPerYear.Val / (24 * 365)); //For ERS, this is mean kW per segment (h_per_segment * vehicles_per_year / hours_per_year => vehicles_per_segment)
                var kWhPerYear = new KiloWattHoursPerYear(kWh.Val * totalTripCountPerYear.Val);
                OtherUnit route_aadt = new OtherUnit(totalTripCountPerYear.Val / 365);
                infraUsePerSegment.AddOrUpdate(
                    infra.Key,
                    (mean_kW, kW, kWhPerYear, socOnArrival, route_aadt),
                    (key, old) => (old.meankW + mean_kW,
                                   UnitMath.Max(old.maxSingleVehiclePeakKW, infra.Value.kW),
                                   old.kWhPerYear + kWhPerYear,
                                   old.socOnArrival * 0.95f + 0.05f * socOnArrival, //Won't be a mean, but at least an approximation
                                   new OtherUnit(old.aadt.Val + route_aadt.Val))); 
            }
        }
    }
}
