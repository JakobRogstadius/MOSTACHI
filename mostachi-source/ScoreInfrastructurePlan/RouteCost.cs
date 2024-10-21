using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScoreInfrastructurePlan
{
    class RouteCost
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

        public bool RouteIsElectrified { get; set; }
        public Dictionary<RouteSegment, (KiloWatts kW, Hours h, KiloWattHours kWh, KiloWattHours kWh_potential, Kilometers km, Dimensionless socOnArrival)> InfraUsePerTraversal { get; } = new ();
        public Dimensionless RatioOfOperatingDistanceInSweden { get; set; } = new Dimensionless(1);
        public KilometersPerYear TotalAnnualRouteKm { get; set; } = new KilometersPerYear(float.MaxValue);
        public KilometersPerYear TotalAnnualRouteKmInSweden { get { return TotalAnnualRouteKm * RatioOfOperatingDistanceInSweden; } }
        public TonKilometersPerYear TotalAnnualRouteTonKm { get; set; } = new TonKilometersPerYear(float.MaxValue);
        public TonKilometersPerYear TotalAnnualRouteTonKmInSweden { get { return TotalAnnualRouteTonKm * RatioOfOperatingDistanceInSweden; } }
        public EuroPerKilometer VehicleAgeing_euroPerKm { get; set; } = new EuroPerKilometer(float.MaxValue); //Includes maintenance
        EuroPerKilometer _wear = new EuroPerKilometer(float.MaxValue);
        public EuroPerKilometer BatteryAgeing_euroPerKm
        {
            get
            {
                return _wear;
            }
            set
            {
                if (float.IsInfinity(value.Val))
                    Console.WriteLine("Hmm");
                _wear = value;
            }
        }
        public KiloWattHoursPerKilometer BatteryAgeing_kWhPerKm { get; set; } = new KiloWattHoursPerKilometer(float.MaxValue);
        public EuroPerKilometer ErsPickupAgeing_euroPerKm { get; set; } = new EuroPerKilometer(float.MaxValue);
        public EuroPerKilometer Energy_euroPerKm { get; set; } = new EuroPerKilometer(float.MaxValue);
        public EuroPerKilometer Road_and_pollution_tax_euroPerKm { get; set; } = new EuroPerKilometer(float.MaxValue);
        public EuroPerKilometer CO2_euroPerKm_total { get; set; } = new EuroPerKilometer(float.MaxValue);
        public EuroPerKilometer CO2_euroPerKm_internalized { get; set; } = new EuroPerKilometer(float.MaxValue);
        public EuroPerKilometer Driver_euroPerKm { get; set; } = new EuroPerKilometer(float.MaxValue); //Includes stops
        public EuroPerKilometer Interest_euroPerKm { get; set; } = new EuroPerKilometer(float.MaxValue);
        public Dimensionless TripCountMultiplier { get; set; } = new Dimensionless(float.MaxValue);
        public EuroPerKilometer InfrastructureFee_approximate_euroPerKm { get; set; } = new EuroPerKilometer(float.MaxValue);
        public HashSet<RouteSegmentType> UsedChargerTypes { get; } = new HashSet<RouteSegmentType>();

        //TODO: Consider if there is a better way to split costs inside and outside Sweden
        public EuroPerYear VehicleAgeing_euroPerYear_in_SE { get { return VehicleAgeing_euroPerKm * TotalAnnualRouteKmInSweden; } }
        public EuroPerYear BatteryAgeing_euroPerYear_in_SE { get { return BatteryAgeing_euroPerKm * TotalAnnualRouteKmInSweden; } }
        public KiloWattHoursPerYear BatteryAgeing_kWhPerYear_in_SE { get { return new KiloWattHoursPerYear(BatteryAgeing_kWhPerKm.Val * TotalAnnualRouteKmInSweden.Val); } }
        public EuroPerYear ErsPickupAgeing_euroPerYear_in_SE { get { return ErsPickupAgeing_euroPerKm * TotalAnnualRouteKmInSweden; } }
        public EuroPerYear Energy_euroPerYear_in_SE { get { return Energy_euroPerKm * TotalAnnualRouteKmInSweden; } }
        public EuroPerYear CO2_euroPerYear_total_in_SE { get { return CO2_euroPerKm_total * TotalAnnualRouteKmInSweden; } }
        public EuroPerYear CO2_euroPerYear_internalized_in_SE { get { return CO2_euroPerKm_internalized * TotalAnnualRouteKmInSweden; } }
        public EuroPerYear Driver_euroPerYear_in_SE { get { return Driver_euroPerKm * TotalAnnualRouteKmInSweden; } }
        public EuroPerYear Interest_euroPerYear_in_SE { get { return Interest_euroPerKm * TotalAnnualRouteKmInSweden; } }
        public EuroPerYear RoadAndPollutionTax_euroPerYear_in_SE { get { return Road_and_pollution_tax_euroPerKm * TotalAnnualRouteKmInSweden; } }

        public KiloWattHours EnergyCostPerTraversal_kWh
        {
            get
            {
                return new KiloWattHours(InfraUsePerTraversal.Values.Sum(n => n.kWh.Val));
            }
        }

        public EuroPerKilometer SystemCost_euroPerKm
        {
            get
            {
                //Full CO2 cost, no charging infra, road tax shouldn't change
                return VehicleAgeing_euroPerKm 
                    + BatteryAgeing_euroPerKm 
                    + ErsPickupAgeing_euroPerKm 
                    + Energy_euroPerKm
                    + CO2_euroPerKm_total
                    + Driver_euroPerKm 
                    + Interest_euroPerKm;
            }
        }

        public EuroPerKilometer HaulierCost_euroPerKm
        {
            get
            {
                //Taxed CO2 cost, plus approximate charging infra
                return VehicleAgeing_euroPerKm 
                    + BatteryAgeing_euroPerKm 
                    + ErsPickupAgeing_euroPerKm 
                    + Energy_euroPerKm 
                    + Road_and_pollution_tax_euroPerKm
                    + CO2_euroPerKm_internalized 
                    + Driver_euroPerKm 
                    + Interest_euroPerKm 
                    + InfrastructureFee_approximate_euroPerKm;
            }
        }

        //The "System" is only inside Sweden
        public EuroPerYear SystemCost_euroPerYear { get { return SystemCost_euroPerKm * TotalAnnualRouteKmInSweden; } }
        
        //Haulier pays everywhere, regardless of location
        public EuroPerYear HaulierCost_euroPerYear { get { return HaulierCost_euroPerKm * TotalAnnualRouteKm; } }

        public const string ToStringHeader = "system_cost_€_per_km\ttransport_cost_€_per_km\tdriver_€_per_km\tvehicle_€_per_km\tbattery_€_per_km\ters_pickup_€_per_km\tinterest_€_per_km\tenergy_€_per_km\tcharging_infra_€_per_km\troad_and_pollution_tax_€_per_km\tco2_total_€_per_km\tco2_taxed_€_per_km\tkm_in_sweden_per_y\tdepot_kWh_per_traversal\tdestination_kWh_per_traversal\trest_stop_kWh_per_traversal\ters_kWh_per_traversal";

        public override string ToString()
        {
            return ToString("\t");
        }

        public string ToString(string separator)
        {
            StringBuilder sb = new StringBuilder();
            KilometersPerYear x = TotalAnnualRouteKmInSweden;
            var vals2 = new float[] {
                    SystemCost_euroPerYear.Val,
                    HaulierCost_euroPerYear.Val,
                    (Driver_euroPerKm * x).Val,
                    (VehicleAgeing_euroPerKm * x).Val,
                    (BatteryAgeing_euroPerKm * x).Val,
                    (ErsPickupAgeing_euroPerKm * x).Val,
                    (Interest_euroPerKm * x).Val,
                    (Energy_euroPerKm * x).Val,
                    (InfrastructureFee_approximate_euroPerKm * x).Val,
                    (Road_and_pollution_tax_euroPerKm * x).Val,
                    (CO2_euroPerKm_total * x).Val,
                    (CO2_euroPerKm_internalized * x).Val,
                    TotalAnnualRouteKmInSweden.Val,
                };
            sb.Append(string.Join(separator, vals2.Select(n => Math.Round(n))));
            return sb.ToString();
        }

        public string ToStringPerKm(string separator = "\t")
        {
            StringBuilder sb = new StringBuilder();
            var x = TotalAnnualRouteKmInSweden;
            var vals2 = new float[] {
                    SystemCost_euroPerKm.Val,
                    HaulierCost_euroPerKm.Val,
                    Driver_euroPerKm.Val,
                    VehicleAgeing_euroPerKm.Val,
                    BatteryAgeing_euroPerKm.Val,
                    ErsPickupAgeing_euroPerKm.Val,
                    Interest_euroPerKm.Val,
                    Energy_euroPerKm.Val,
                    InfrastructureFee_approximate_euroPerKm.Val,
                    Road_and_pollution_tax_euroPerKm.Val,
                    CO2_euroPerKm_total.Val,
                    CO2_euroPerKm_internalized.Val,
                    TotalAnnualRouteKmInSweden.Val,
                    UsedChargerTypes.Contains(RouteSegmentType.Depot) ? InfraUsePerTraversal.Where(n => n.Key.Type == RouteSegmentType.Depot).Sum(n => n.Value.kWh.Val) : 0f,
                    UsedChargerTypes.Contains(RouteSegmentType.Destination) ? InfraUsePerTraversal.Where(n => n.Key.Type == RouteSegmentType.Destination).Sum(n => n.Value.kWh.Val) : 0f,
                    UsedChargerTypes.Contains(RouteSegmentType.RestStop) ? InfraUsePerTraversal.Where(n => n.Key.Type == RouteSegmentType.RestStop).Sum(n => n.Value.kWh.Val) : 0f,
                    UsedChargerTypes.Contains(RouteSegmentType.Road) ? InfraUsePerTraversal.Where(n => n.Key.Type == RouteSegmentType.Road).Sum(n => n.Value.kWh.Val) : 0f,
                };
            sb.Append(string.Join(separator, vals2.Select(n => Math.Round(1000*n)/1000f)));
            return sb.ToString();
        }
    }
}
