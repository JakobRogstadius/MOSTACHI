using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using static ScoreInfrastructurePlan.Parameters;
using BuiltInfrastructureSet = System.Collections.Generic.Dictionary<int, ScoreInfrastructurePlan.ChargingSiteCost>;

namespace ScoreInfrastructurePlan
{
    class ChargingSiteCost
    {
        public EuroPerYear Cost_EuroPerYear { get; set; }
        public KiloWattHoursPerYear EnergyDelivered_kWhPerYear { get; set; }
        public KiloWatts InstalledPower_kWPeak_Segment { get; set; }
        public KiloWatts UsedPower_kWPeak_Segment { get; set; }
        public KiloWattsPerKilometer InstalledPower_kWPeak_PerLaneKm { get; set; }
        public KiloWattsPerKilometer UsedPower_kWPeak_PerLaneKm { get; set; }
        public EuroPerKiloWattHour Cost_EuroPerkWh { get { return (EnergyDelivered_kWhPerYear is null) ? new(0) : new (Cost_EuroPerYear.Val / Math.Max(1, EnergyDelivered_kWhPerYear.Val)); } }
        public Dimensionless MeanSocOnArrival { get; set; }
        public bool PeakPowerIsGreaterThanInherited { get; set; }
    }

    //class BuiltInfrastructureSet : Dictionary<int, ChargingSiteCost> {    }

    class InfrastructureCost
    {
        static ConcurrentDictionary<RouteSegment, ConcurrentDictionary<ModelYear, EuroPerKiloWattHour>>
            _costCache = new ConcurrentDictionary<RouteSegment, ConcurrentDictionary<ModelYear, EuroPerKiloWattHour>>();

        static ConcurrentDictionary<ModelYear, EuroPerKiloWattHour> _ersUserCost_excl_energy = new ConcurrentDictionary<ModelYear, EuroPerKiloWattHour>();

        static Dictionary<ModelYear, HashSet<RouteSegment>> _segmentBlacklist = new Dictionary<ModelYear, HashSet<RouteSegment>>();

        public static void ResetCache()
        {
            _ersUserCost_excl_energy.Clear();
            _costCache.Clear();
            _segmentBlacklist.Clear();
        }

        public static void SetCache(ModelYear year, Dictionary<RouteSegment, EuroPerKiloWattHour> chargingCostPerSite_excl_energy, EuroPerKiloWattHour chargingCost_ers_excl_energy, IEnumerable<RouteSegment> segmentBlacklist)
        {
            if (_segmentBlacklist.ContainsKey(year))
                _segmentBlacklist[year].UnionWith(segmentBlacklist);
            else
                _segmentBlacklist[year] = new HashSet<RouteSegment>(segmentBlacklist);

            List<(RouteSegmentType Type, (EuroPerKiloWattHour estimate, EuroPerKiloWattHour calculated) Costs)> diffs = new List<(RouteSegmentType Type, (EuroPerKiloWattHour, EuroPerKiloWattHour))>();
            foreach (var site in chargingCostPerSite_excl_energy.Where(n => !_segmentBlacklist[year].Contains(n.Key)))
            {
                var siteCostPerYear = _costCache.GetOrAdd(site.Key, new ConcurrentDictionary<ModelYear, EuroPerKiloWattHour>());
                var val = siteCostPerYear.AddOrUpdate(year, site.Value, (y, cached) =>
                {
                    //Record the difference between the estimated and correct value
                    diffs.Add((site.Key.Type, (cached, site.Value)));
                    //Replace the estimated with the correct value
                    return site.Value;
                });
            }

            if (diffs.Count > 0)
            {
                var diffPerType = diffs.GroupBy(n => n.Type).ToDictionary(g => g.Key,
                    g => (
                    avgEst: g.Average(n => n.Costs.estimate.Val),
                    avgCalc: g.Average(n => n.Costs.calculated.Val),
                    p10: g.OrderBy(n => n.Costs.calculated.Val).ElementAt((int)(g.Count() * 0.1)).Costs.calculated.Val,
                    p50: g.OrderBy(n => n.Costs.calculated.Val).ElementAt((int)(g.Count() * 0.5)).Costs.calculated.Val,
                    p90: g.OrderBy(n => n.Costs.calculated.Val).ElementAt((int)(g.Count() * 0.9)).Costs.calculated.Val
                    ));

                if (_ersUserCost_excl_energy.TryGetValue(year, out EuroPerKiloWattHour currentErs))
                    diffPerType.Add(RouteSegmentType.Road, (currentErs.Val, chargingCost_ers_excl_energy.Val, 0, 0, 0));

                Console.WriteLine("\nChargingCostCache errors:");
                foreach (var item in diffPerType)
                {
                    var v = item.Value;
                    //Note: After these stats are calculated in the preprocessing step, some locations will end up with high usage fees, which will in turn result in that nobody uses them and that they are not built. That means averages don't match between runs.
                    Console.WriteLine("{0}: meanEst={1:0.000}, meanCalc={2:0.000}, p10={3:0.000}, p50={4:0.000}, p90={5:0.000}", item.Key, v.avgEst, v.avgCalc, v.p10, v.p50, v.p90);
                }
            }

            if (float.IsNormal(chargingCost_ers_excl_energy.Val))
                _ersUserCost_excl_energy[year] = chargingCost_ers_excl_energy;
            else
                _ersUserCost_excl_energy[year] = new EuroPerKiloWattHour(float.MaxValue);
        }

        public static bool SegmentIsBlacklisted(ModelYear year, RouteSegment segment)
        {
            return _segmentBlacklist.ContainsKey(year) && _segmentBlacklist[year].Contains(segment);
        }

        public static EuroPerKiloWattHour GetSiteUserFee_CachedOrEstimated_excl_energy(RouteSegment segment, ModelYear year, KiloWatts est_kWPerVehicle, OtherUnit vehiclesPerDay = null)
        {
            if (segment.Type == RouteSegmentType.Road)
                throw new ArgumentException("Input must be a charging site.");

            //If a value exists for this location, return it
            if (_costCache.TryGetValue(segment, out var costCache))
            {
                if (costCache.TryGetValue(year, out var cst))
                {
                    return cst;
                }
                //TODO: Using the previous years's cost as the initial estimate might not be great, since it becomes virtually impossible to get out of a situation of underutilization
                //else if (year > ModelYear.Y2020 && costCache.TryGetValue(year.Previous(), out cst))
                //{
                //    costCache.TryAdd(year, cst);
                //    return cst;
                //}
            }

            //No prior value existed, estimate one and cache it
            Hours stop_h;
            Dimensionless util_ratio;
            OtherUnit veh_per_day;
            switch (segment.Type)
            {
                case RouteSegmentType.Depot:
                    stop_h = HGV40.Common_Depot_stop_h[year];
                    util_ratio = Infrastructure.Depot_utilization_ratio[year];
                    veh_per_day = vehiclesPerDay ?? Infrastructure.Depot_reference_vehicles_per_day[year];
                    break;
                case RouteSegmentType.Destination:
                    stop_h = HGV40.Common_Destination_stop_h[year];
                    util_ratio = Infrastructure.Destination_utilization_ratio[year];
                    veh_per_day = vehiclesPerDay ?? Infrastructure.Destination_reference_vehicles_per_day[year];
                    break;
                case RouteSegmentType.RestStop:
                    stop_h = HGV40.Common_Rest_stop_h[year];
                    util_ratio = Infrastructure.Rest_Stop_utilization_ratio[year];
                    veh_per_day = vehiclesPerDay ?? Infrastructure.Rest_Stop_reference_vehicles_per_day[year];
                    break;
                default:
                    throw new NotImplementedException();
            }

            //dim: veh_per_day * h / (h_per_day * util_ratio) => veh / util_ratio
            OtherUnit maxSimultaneousVehicles = new OtherUnit(Math.Min(veh_per_day.Val, veh_per_day.Val * stop_h.Val / (24f * util_ratio.Val)));
            KiloWatts peakSitePower_kW = new KiloWatts(maxSimultaneousVehicles.Val * est_kWPerVehicle.Val);

            ChargingSiteCost cost = CalculateSiteUsageAndCost_excl_energy(segment, year, peakSitePower_kW, new KiloWatts(0), new Dimensionless(0.5f));
            if (peakSitePower_kW > 0)
            {
                _costCache.GetOrAdd(segment, new ConcurrentDictionary<ModelYear, EuroPerKiloWattHour>())
                    .TryAdd(year, cost.Cost_EuroPerkWh);
            }
            return cost.Cost_EuroPerkWh;
        }

        static readonly KiloWatts ZeroPower = new KiloWatts(0);
        static readonly Hours HoursPerYear = new Hours(24 * 365);
        public static ChargingSiteCost CalculateSiteUsageAndCost_excl_energy(RouteSegment site, ModelYear year, KiloWatts peakDraw_sitekW, KiloWatts alreadyInstalled_sitekW, Dimensionless meanSoCOnArrival, KiloWattHoursPerYear kWhPerYear = null)
        {
            if (peakDraw_sitekW == ZeroPower && alreadyInstalled_sitekW == ZeroPower)
                return new ChargingSiteCost();

            var inf = Infrastructure;
            Dimensionless util_ratio, profitMargin_ratio;
            OtherUnit maint_ratio_per_year, interest_ratioPerYear;
            EuroPerKiloWatt hw_euroPerKW;
            Euro gridCable_euro;
            Years writeOff_years;
            switch (site.Type)
            {
                case RouteSegmentType.Depot:
                    util_ratio = inf.Depot_utilization_ratio[year];
                    hw_euroPerKW = inf.Depot_hardware_cost_euro_per_kW[year];
                    maint_ratio_per_year = inf.Depot_hardware_maintenance_ratio_per_year[year];
                    gridCable_euro = inf.Depot_grid_connection_cable_euro[year];
                    writeOff_years = inf.Depot_write_off_period_years[year];
                    interest_ratioPerYear = World.Economy_Private_sector_interest_depot_charging_percent[year];
                    profitMargin_ratio = inf.Depot_profit_margin_ratio[year];
                    break;
                case RouteSegmentType.Destination:
                    util_ratio = inf.Destination_utilization_ratio[year];
                    hw_euroPerKW = inf.Destination_hardware_cost_euro_per_kW[year];
                    maint_ratio_per_year = inf.Destination_hardware_maintenance_ratio_per_year[year];
                    gridCable_euro = inf.Destination_grid_connection_cable_euro[year];
                    writeOff_years = inf.Destination_write_off_period_years[year];
                    interest_ratioPerYear = World.Economy_Private_sector_interest_public_charging_and_trucks_percent[year];
                    profitMargin_ratio = inf.Destination_profit_margin_ratio[year];
                    break;
                case RouteSegmentType.RestStop:
                    util_ratio = inf.Rest_Stop_utilization_ratio[year];
                    hw_euroPerKW = inf.Rest_Stop_hardware_cost_euro_per_kW[year];
                    maint_ratio_per_year = inf.Rest_Stop_hardware_maintenance_ratio_per_year[year];
                    gridCable_euro = inf.Rest_Stop_grid_connection_cable_euro[year];
                    writeOff_years = inf.Rest_Stop_write_off_period_years[year];
                    interest_ratioPerYear = World.Economy_Private_sector_interest_public_charging_and_trucks_percent[year];
                    profitMargin_ratio = inf.Rest_Stop_profit_margin_ratio[year];
                    break;
                default:
                    throw new NotImplementedException();
            }

            //There is a problem here that primarily manifests for smaller samples of the dataset. As routes are independent and their origins and destinations
            //are sampled from a distribution, some routes can end up being the sole users of a depot or destination. This results in abnormally low utilization rates
            //and abnormally high costs per kWh. I handle this by pretending that the utilization rate is greater and scaling down the cost.

            Dimensionless threshold = site.Type == RouteSegmentType.RestStop ? new Dimensionless(0.2f) : new Dimensionless(0.5f);
            Hours annualHoursOfUse = HoursPerYear * util_ratio;
            KiloWattHoursPerYear realisticKWhPerYear = new KiloWattHoursPerYear(peakDraw_sitekW.Val * annualHoursOfUse.Val);
            bool rescale = !(kWhPerYear is null) && kWhPerYear < realisticKWhPerYear * threshold;
            if (kWhPerYear is null)
                kWhPerYear = realisticKWhPerYear; //If unknown, use expected
            else if (!rescale)
                realisticKWhPerYear = kWhPerYear; //If known and ok, use known
            //If known and not ok, use expected

            KiloWatts sitekW = UnitMath.Max(peakDraw_sitekW, alreadyInstalled_sitekW); //inherited peak power does not affect the current Dimensionless of power-to-energy
            Euro hw_euro = hw_euroPerKW * sitekW;
            hw_euro *= new Dimensionless(1 + writeOff_years.Val * maint_ratio_per_year.Val);
            Euro grid_euro = inf.Grid_initial_acccess_base_euro[year]
                + inf.Grid_initial_acccess_euro_per_kW[year] * sitekW
                + gridCable_euro;
            Dimensionless interest_ratio_total = new Dimensionless(1 + interest_ratioPerYear.Val * writeOff_years.Val / 2);
            EuroPerYear capex_euroPerYear = (hw_euro + grid_euro) * interest_ratio_total / writeOff_years;
            EuroPerKiloWattHour opex_euroPerkWh = inf.Grid_fee_at_100_percent_utilization_euro_per_kWh[year] * (1 / util_ratio);
            EuroPerYear opex_euroPerYear = new EuroPerYear(opex_euroPerkWh.Val * realisticKWhPerYear.Val);

            if (rescale)
            {
                Dimensionless scaling = kWhPerYear / (realisticKWhPerYear * threshold);
                capex_euroPerYear *= scaling;
                opex_euroPerYear *= scaling;
            }

            return new ChargingSiteCost()
            {
                Cost_EuroPerYear = (opex_euroPerYear + capex_euroPerYear) * (1 + profitMargin_ratio),
                EnergyDelivered_kWhPerYear = kWhPerYear,
                InstalledPower_kWPeak_Segment = sitekW,
                UsedPower_kWPeak_Segment = peakDraw_sitekW,
                InstalledPower_kWPeak_PerLaneKm = new KiloWattsPerKilometer(0),
                UsedPower_kWPeak_PerLaneKm = new KiloWattsPerKilometer(0),
                MeanSocOnArrival = meanSoCOnArrival,
                PeakPowerIsGreaterThanInherited = sitekW > alreadyInstalled_sitekW
            };
        }

        public static EuroPerKiloWattHour GetErsUserFee_CachedOrEstimated_excl_energy(ModelYear year, InfraOffers infra)
        {
            if (_ersUserCost_excl_energy.TryGetValue(year, out var ersCost))
            {
                return ersCost;
            }
            else
            {
                ersCost = Calculate_ERS_euro_per_kWh(
                    year,
                    infra.AvailablePowerPerUser_kW[RouteSegmentType.Road],
                    new KiloWattsPerKilometer(0),
                    infra.ErsReferenceSpeed_kmph.Val,
                    infra.GetExpectedErsAadt());
                _ersUserCost_excl_energy.AddOrUpdate(year, ersCost, (_, __) => ersCost);
                return ersCost;
            }
        }

        public static EuroPerKilometerYear Calculate_ERS_euro_per_kmYear(ModelYear year, KiloWattsPerKilometer peakPower_kWPerBidirectionalKm, KiloWattHoursPerKilometerYear kWh_per_bidirectional_kmYear)
        {
            var inf = Infrastructure;
            Years writeoff_years = inf.ERS_write_off_period_years[year];
            EuroPerKilometer hw_euroPerKm = inf.ERS_base_cost_euro_per_km[year] + inf.ERS_power_cost_euro_per_kW_km[year] * peakPower_kWPerBidirectionalKm;
            hw_euroPerKm *= (1 + new Dimensionless(inf.ERS_maintenance_cost_ratio_per_year[year].Val * writeoff_years.Val));
            hw_euroPerKm *= (1 + inf.ERS_standardization_risk_ratio[year] * inf.ERS_cost_of_standard_change_ratio[year]);
            Euro grid_euroPerConnection = inf.Grid_initial_acccess_base_euro[year]
                + inf.Grid_initial_acccess_euro_per_kW[year] * peakPower_kWPerBidirectionalKm * inf.ERS_grid_connection_interval_km[year]
                + inf.ERS_grid_connection_cable_euro[year];
            EuroPerKilometer grid_euroPerKm = grid_euroPerConnection / inf.ERS_grid_connection_interval_km[year];
            EuroPerKiloWattHour grid_euroPerKWh = inf.Grid_fee_at_100_percent_utilization_euro_per_kWh[year] * (1 / inf.ERS_utilization_ratio[year]);

            Dimensionless interest_ratio_lifetime = new Dimensionless(World.Economy_Public_sector_interest_rate_percent[year].Val * writeoff_years.Val / 2); //Loan is paid off and shrinks linearly over writeoff time
            EuroPerKilometerYear capex_euroPerYear = (hw_euroPerKm + grid_euroPerKm) * (1 + interest_ratio_lifetime) / writeoff_years;
            Dimensionless margin = 1 + Parameters.Infrastructure.ERS_profit_margin_ratio[year];

            return (capex_euroPerYear + grid_euroPerKWh * kWh_per_bidirectional_kmYear) * margin;
        }

        public static EuroPerKiloWattHour Calculate_ERS_euro_per_kWh(ModelYear year, KiloWatts meanDraw_kWPerVehicle, KiloWattsPerKilometer alreadyInstalled_kWPerKm, float speed_kmph = 85f, OtherUnit aadt = null)
        {
            var inf = Infrastructure;
            aadt ??= inf.ERS_reference_aadt[year];
            var utilRatio = inf.ERS_utilization_ratio[year];
            var kWh_per_kmDay = new OtherUnit(meanDraw_kWPerVehicle.Val * aadt.Val / speed_kmph) * utilRatio;
            var kWh_per_kmYear = new KiloWattHoursPerKilometerYear(kWh_per_kmDay.Val * 365);
            //dim: veh_per_d_mean / (h_per_d * mean_per_peak * km_per_h) => veh_per_km_mean / mean_per_peak => veh_per_km_peak
            float peak_vehiclesPerKm = (float)Math.Ceiling(aadt.Val / (24 * utilRatio.Val * speed_kmph));
            KiloWattsPerKilometer peak_kW = UnitMath.Max(alreadyInstalled_kWPerKm, new KiloWattsPerKilometer(meanDraw_kWPerVehicle.Val * peak_vehiclesPerKm));

            EuroPerKilometerYear euroPerKmYear = Calculate_ERS_euro_per_kmYear(year, peak_kW, kWh_per_kmYear);

            return euroPerKmYear / kWh_per_kmYear;
        }

        public static Dimensionless GetMinimumErsCoverageDimensionless(KiloWatts ersPower_kW, KilometersPerHour vehicleSpeed_kmph, KiloWattHoursPerKilometer vehicleEnergyConsumption_kWhPerKm, Dimensionless rangeGainRatio)
        {
            //gain_ratio = charged_per_km / spent_per_km = (transferred_per_km * ers_ratio - spent_per_km) / spent_per_km
            //ers_ratio = (gain_ratio + 1) * spent_per_km / transferred_per_km

            var coverageRatio = new Dimensionless((rangeGainRatio.Val + 1) * vehicleEnergyConsumption_kWhPerKm.Val / (ersPower_kW.Val / vehicleSpeed_kmph.Val));
            return UnitMath.Min(coverageRatio, new Dimensionless(1));
        }
    }
}
