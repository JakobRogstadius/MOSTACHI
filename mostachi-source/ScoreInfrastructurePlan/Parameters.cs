using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.Metadata;
using static ScoreInfrastructurePlan.Parameters;

namespace ScoreInfrastructurePlan
{
    public enum ElectricityPriceRegion { SE12, SE34, Other };
    public enum ModelYear { Y2020 = 0, Y2025 = 1, Y2030 = 2, Y2035 = 3, Y2040 = 4, Y2045 = 5, Y2050 = 6, N_A = 1000 };

    public static class ModelYearExtensions
    {
        public static int AsInteger(this ModelYear year)
        {
            return 2020 + 5 * (int)year;
        }

        public static ModelYear FromInteger(int year)
        {
            return (ModelYear)((year - 2020) / 5);
        }

        public static string AsDateString(this ModelYear year)
        {
            return year.AsInteger() + "-01-01";
        }

        public static ModelYear Next(this ModelYear year)
        {
            return (ModelYear)(year + 1);
        }

        public static ModelYear Previous(this ModelYear year)
        {
            return (ModelYear)(year - 1);
        }

        public static bool IsFirst(this ModelYear year)
        {
            return year == ModelYear.Y2020;
        }

        public static bool IsLast(this ModelYear year)
        {
            return year == ModelYear.Y2050;
        }

        public static ModelYear[] GetSequence(this ModelYear startYear, ModelYear to)
        {
            List<ModelYear> m = new List<ModelYear>();
            var y = startYear;
            while (true)
            {
                m.Add(y);
                if (y == to || y == ModelYear.Y2050)
                    break;
                y = y.Next();
            }
            return m.ToArray();
        }
    }

    public class Hashes
    {
        public static double UniformHash(uint x)
        {
            x = x + 31393;
            x = ((x >> 16) ^ x) * 0x45d9f3b;
            x = ((x >> 16) ^ x) * 0x45d9f3b;
            x = (x >> 16) ^ x;
            return (double)x / (double)uint.MaxValue;
        }

        public static int GetRandomInt(uint id, int maxValue)
        {
            return (int)Math.Floor(UniformHash(id) * maxValue);
        }
    }

    public class ParameterTimeSeries<T> : List<T> where T : HasVal<T>
    {
        static uint _instanceCounter = 0;
        uint _instanceNo = _instanceCounter++;

        private ParameterTimeSeries<T> _scaledSeries;
        public void ResetToDefault()
        {
            _scaledSeries = null;
        }
        public void SetToMultipleOfDefault(float multiple)
        {
            Dimensionless multiplier = new(multiple);
            _scaledSeries = new ParameterTimeSeries<T>();
            for (int i = 0; i < this.Count; i++)
            {
                _scaledSeries.Add(this[i] * multiplier);
            }
        }
        
        public void Set(IEnumerable<float> values)
        {
            if (values.Count() != this.Count)
                throw new ArgumentException("Wrong number of values provided");
            _scaledSeries = new ParameterTimeSeries<T>();
            foreach (float value in values)
                _scaledSeries.Add(base[0].CreateNewInstance(value));
        }

        public void SetToExponentialTrendFromFirstValue(float annualMultiple)
        {
            _scaledSeries = new ParameterTimeSeries<T>();
            for (int i = 0; i < this.Count; i++)
            {
                int years = ((ModelYear)i).AsInteger() - ((ModelYear)0).AsInteger();
                _scaledSeries.Add(base[0] * new Dimensionless((float)Math.Pow(annualMultiple, years)));
            }
        }

        public T this[ModelYear key]
        {
            get => _scaledSeries?[(int)key] ?? base[(int)key];
            set => base[(int)key] = value;
        }

        public void Add(float v) { 
            base.Add((T)Activator.CreateInstance(typeof(T), v)); 
        }

        public (T sampled, T original) Sample(ModelYear year, int id, float normStdDev = 0.05f)
        {
            T item = this[year];
            float value = item.Val;
            float stdDev = value * normStdDev;
            const double TWO_PI = 2 * Math.PI;
            //double u2 = 1.0 - _rand.NextDouble(); //uniform(0,1] random doubles
            double u1 = Hashes.UniformHash((uint)id + _instanceNo);
            double u2 = Hashes.UniformHash((uint)id + 56263 + _instanceNo);
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(TWO_PI * u2); //random normal(0,1)
            double randNormal = Math.Max(value - 4 * stdDev, value + stdDev * randStdNormal); //random normal(mean,stdDev^2)
            //_u1 = u2;
            return (item.CreateNewInstance((float)randNormal), item);
        }

        public T GetInterpolatedValue(float year)
        {
            
            float minYear = ((ModelYear)0).AsInteger();
            float yearInterval = ((ModelYear)1).AsInteger() - minYear;
            float fIndex = (year - minYear) / yearInterval;
            float di;
            T val0, val1;
            if (fIndex > this.Count - 1)
            {
                val0 = this[this.Count - 1];
                val1 = val0 + val0 - this[this.Count - 2];
                di = fIndex - (this.Count - 1);
            }
            else if (fIndex >= 0)
            {
                val0 = this[(int)fIndex];
                val1 = this[(int)Math.Ceiling(fIndex)];
                di = fIndex - (int)fIndex;
            }
            else //(fIndex < 0)
            {
                val0 = this[0];
                val1 = this[1];
                di = fIndex;
            }
            //float slope = (val1.Val - val0.Val) / yearInterval;
            //return val0.CreateNewInstance(val0.Val + slope * di * yearInterval);

            //interpolation assuming exponential change, from https://www.quora.com/How-does-one-interpolate-an-exponential
            float A = val0.Val, B = val1.Val, t = di, T = 1;
            return val0.CreateNewInstance((float)(A * Math.Pow(B / A, t / T)));
        }

        public override string ToString()
        {
            return "[" + string.Join(", ", this) + "]";
        }
    }

    public abstract class VehicleType
    {
        public abstract ParameterTimeSeries<KilometersPerYear> Common_Annual_distance_km { get; } //Sample
        public abstract ParameterTimeSeries<Dimensionless> Common_Utilization_calendar_days_per_year { get; } //Not used
        public abstract ParameterTimeSeries<Hours> Common_Max_time_in_transit_h_per_day { get; } //Sample
        public abstract ParameterTimeSeries<Hours> Common_Max_time_in_use_h_per_day { get; } //Sample
        public abstract ParameterTimeSeries<KiloWatts> Common_Power_peak_kW { get; }
        public abstract ParameterTimeSeries<EuroPerKilometer> Common_Tyres_euro_per_km { get; }
        public abstract ParameterTimeSeries<EuroPerTonKilometer> Common_Cargo_capacity_value_euro_per_ton_km { get; } //Not used
        public abstract ParameterTimeSeries<EuroPerCubicMeter> Common_Cargo_capacity_value_euro_per_m3_km { get; } //Not used
        public abstract ParameterTimeSeries<Kilogram> Common_Total_weight_limit_kg { get; }
        public abstract ParameterTimeSeries<Hours> Common_Depot_stop_h { get; } //Sample
        public abstract ParameterTimeSeries<Hours> Common_Destination_stop_h { get; }
        public abstract ParameterTimeSeries<Hours> Common_Rest_stop_h { get; }
        public abstract ParameterTimeSeries<Hours> Common_Drive_session_h { get; }
        public abstract ParameterTimeSeries<EuroPerHour> Common_Driver_cost_euro_per_h { get; }
        public abstract ParameterTimeSeries<Kilogram> ICEV_Chassis_weight_kg { get; }
        public abstract ParameterTimeSeries<Euro> ICEV_Chassis_cost_2020_euro { get; } //Not used
        public abstract ParameterTimeSeries<Years> ICEV_Lifetime_years { get; } //Sample
        public abstract ParameterTimeSeries<EuroPerKilometer> ICEV_Maintenance_cost_euro_per_km { get; }
        public abstract ParameterTimeSeries<Dimensionless> ICEV_Residual_value_at_end_of_life_ratio { get; }
        public abstract ParameterTimeSeries<LiterPerKilometer> ICEV_Fuel_consumption_liter_per_km { get; }
        public abstract ParameterTimeSeries<EuroPerKilometer> BEV_Maintenance_cost_euro_per_km { get; }
        public abstract ParameterTimeSeries<Years> BEV_Lifetime_years { get; } //Sample
        public abstract ParameterTimeSeries<Dimensionless> BEV_Residual_value_at_end_of_life_ratio { get; }
        public abstract ParameterTimeSeries<KiloWattHoursPerKilometer> BEV_Energy_consumption_kWh_per_km { get; }
        public abstract ParameterTimeSeries<Kilometers> BEV_Min_range_buffer_km { get; }
        public abstract ParameterTimeSeries<Euro> ICEV_Chassis_cost_euro { get; }
        public abstract ParameterTimeSeries<Kilogram> BEV_Chassis_weight_excl_battery_kg { get; }
        public abstract ParameterTimeSeries<Euro> BEV_Chassis_cost_excl_battery_euro { get; }
        public abstract ParameterTimeSeries<KiloWattHours> BEV_Min_net_battery_capacity_kWh { get; } //Not used

        public void ModifyDailyOperatingHoursAndAdjustDepotTimeAndAnnualDistance(float annualChange)
        {
            List<float> hoursInTransit = new List<float>();
            List<float> hoursInUse = new List<float>();
            List<float> hoursAtDepot = new List<float>();
            List<float> kmPerYear = new List<float>();
            for (int i = 0; i < 7; i++)
            {
                ModelYear y = (ModelYear)i;
                float multiple = (float)Math.Pow(annualChange, y.AsInteger() - ((ModelYear)0).AsInteger());
                hoursInTransit.Add(Math.Min(24, multiple * Common_Max_time_in_transit_h_per_day[i].Val));
                hoursInUse.Add(Math.Min(24, multiple * Common_Max_time_in_use_h_per_day[i].Val));
                hoursAtDepot.Add(24 - hoursInUse[i]);
                kmPerYear.Add(multiple * Common_Annual_distance_km[i].Val);

                if (hoursInUse[i] > 24)
                    throw new ArgumentException("Cannot set hours of use per day to more than 24 hours");
            }
            Common_Max_time_in_transit_h_per_day.Set(hoursInTransit);
            Common_Max_time_in_use_h_per_day.Set(hoursInUse);
            Common_Depot_stop_h.Set(hoursAtDepot);
            Common_Annual_distance_km.Set(kmPerYear);
        }

        public KiloWatts GetPowerConsumption_kW(ModelYear year, KilometersPerHour speed_kmph, KiloWattHours netBatteryCapacity_kWh, bool hasErsPickup)
        {
            //Adjust BEV energy consumption based on vehicle mass
            
            //From Tony Sandberg's thesis: 10% weight change => 5% fuel consumption change
            
            //From Mats Alaküla: We must add consumtion for the friction losses(conductive ERS), with a typical friction coefficienct of 0.25.
            //For trucks, with 5 pickups, 100N / pickup and 90 km / h this corresponds to about 3 kW (friction, drag unspecified), compared to 100 kW of traction power or 3 %, used 50 % of the time while on ERS meaning ~1 % of the energy consumtion.
            //There is also an effect on aerodynamics. This is VERY hard to set a good figure for.
            
            //There is going to be SOME difference in efficiency between energy transmission via ERS and via cable charging, but I cannot even figure out which has greater total losses.
            //I assume no net effect on energy consumption from ERS charging.

            Kilogram bevWeight = GetFullBevWeight(year, netBatteryCapacity_kWh, hasErsPickup);
            Kilogram icevWeight = ICEV_Chassis_weight_kg[year];
            var weightRatio = (bevWeight - icevWeight) / icevWeight;

            Dimensionless weightDiffEffect = 1 + weightRatio * 0.5f * (1 - _shareOfWeightDiffForCargo);
            
            return BEV_Energy_consumption_kWh_per_km[year] * speed_kmph * weightDiffEffect;
        }

        public EuroPerKilometer ICEV_ChassisAndMaintenance_cost_euro_per_km(ModelYear year, KilometersPerYear km_per_year = null)
        {
            km_per_year ??= Common_Annual_distance_km[year];
            var eolVal = ICEV_Chassis_cost_euro[year] * (1 - ICEV_Residual_value_at_end_of_life_ratio[year]);
            var vehCost = eolVal / (km_per_year * ICEV_Lifetime_years[year]);
            return ICEV_Maintenance_cost_euro_per_km[year]
                + Common_Tyres_euro_per_km[year]
                + vehCost;
        }

        public EuroPerKilometer BEV_ChassisAndMaintenance_cost_euro_per_km(ModelYear year, KilometersPerYear km_per_year = null)
        {
            km_per_year ??= Common_Annual_distance_km[year];
            var eolVal = BEV_Chassis_cost_excl_battery_euro[year] * (1 - BEV_Residual_value_at_end_of_life_ratio[year]);
            Years y = BEV_Lifetime_years[year];
            var vehCost = eolVal / (km_per_year * y);
            return BEV_Maintenance_cost_euro_per_km[year]
                + Common_Tyres_euro_per_km[year]
                + vehCost;
        }

        public Kilogram GetFullBevWeight(ModelYear year, KiloWattHours netBatteryCapacity_kWh, bool hasErsPickup)
        {
            Kilogram batteryWeight = new Kilogram(netBatteryCapacity_kWh.Val / Battery.Net_Specific_energy_kWh_per_kg[year].Val);
            Kilogram pickupWeight = hasErsPickup ? Infrastructure.ERS_pick_up_weight_heavy_kg[year] : _zeroWeight;
            return BEV_Chassis_weight_excl_battery_kg[year] + batteryWeight + pickupWeight;
        }

        static readonly Kilogram _zeroWeight = new (0);
        static readonly Dimensionless _shareOfWeightDiffForCargo = new (0.3f);
        public Dimensionless BEV_Carrying_capacity_ratio_of_ICEV(ModelYear year, KiloWattHours netBatteryCapacity_kWh, bool hasErsPickup)
        {
            Kilogram bevWeight = GetFullBevWeight(year, netBatteryCapacity_kWh, hasErsPickup);
            Kilogram icevWeight = ICEV_Chassis_weight_kg[year];
            Kilogram limit = Common_Total_weight_limit_kg[year];
            //Because only some transports are weight limited, only give half gain/penalty for weight change.
            return 1 - _shareOfWeightDiffForCargo + _shareOfWeightDiffForCargo * ((limit - bevWeight) / (limit - icevWeight));
        }

        public Hours GetNormalizedDepotTime_h(ModelYear year, Hours routeLength_h)
        {
            //This calculation is meant to allocate less of the day's depot time to short routes, and more to long routes.

            Hours workday_h = Common_Max_time_in_use_h_per_day[year];
            Hours rest_h = Common_Rest_stop_h[year] * (routeLength_h / Common_Drive_session_h[year]); //I'm not rounding here, because all breaks across multiple routes should add up correctly
            Hours dest_h = Common_Destination_stop_h[year];
            Hours routetime_h = routeLength_h + rest_h + dest_h;
            var route_ratio_of_workday = routetime_h / workday_h;

            Hours driveday_h = Common_Max_time_in_transit_h_per_day[year];
            var route_ratio_of_driveday = routeLength_h / driveday_h;

            Hours depot_h = Common_Depot_stop_h[year];
            return depot_h * UnitMath.Min(new Dimensionless(1), UnitMath.Max(route_ratio_of_driveday, route_ratio_of_workday));
        }

        public Hours GetStopTime_h(ModelYear year, RouteSegmentType rstype)
        {
            switch (rstype)
            {
                case RouteSegmentType.Depot:
                    return Common_Depot_stop_h[year];
                case RouteSegmentType.Destination:
                    return Common_Destination_stop_h[year];
                case RouteSegmentType.RestStop:
                    return Common_Rest_stop_h[year];
                case RouteSegmentType.Road:
                    throw new ArgumentException("Cannot get stop time for roads.");
                default:
                    throw new NotImplementedException();
            }
        }

        //Utilization_h_per_day, Utilization_calendar_days_per_year => Annual distance, Depot_stop_h, Lifetime_years*2

        //public (Hours sampled, Hours original) GetSampled_Utilization_h_per_day(ModelYear year, int id)
        //{
        //    return Common_Utilization_h_per_day.Sample(year, id, 0.25f);
        //}

        //public (Dimensionless sampled, Dimensionless original) GetSampled_Utilization_calendar_days_per_year(ModelYear year, int id)
        //{
        //    var d = Common_Utilization_calendar_days_per_year.Sample(year, id, 0.25f);
        //    d = (new Dimensionless(Math.Min(Math.Max(50, d.sampled.Val), 365)), d.original);
        //    return d;
        //}

        //public (KilometersPerYear sampled, KilometersPerYear original) GetSampled_Annual_distance_km(ModelYear year, int id)
        //{
        //    var h = GetSampled_Utilization_h_per_day(year, id);
        //    var d = GetSampled_Utilization_calendar_days_per_year(year, id);
        //    var h_ratio = h.sampled / h.original;
        //    var d_ratio = d.sampled / d.original;
        //    var x = Common_Annual_distance_km[year];
        //    return (x * h_ratio * d_ratio, x);
        //}

        //public (Hours sampled, Hours original) GetSampled_Depot_stop_h(ModelYear year, int id)
        //{
        //    var h = GetSampled_Utilization_h_per_day(year, id);
        //    return (new Hours(24 - h.sampled.Val), h.original);
        //}

        //public (Years sampled, Years original) GetSampled_ICEV_lifetime_years(ModelYear year, int id)
        //{
        //    var h = GetSampled_Utilization_h_per_day(year, id);
        //    var d = GetSampled_Utilization_calendar_days_per_year(year, id);
        //    var h_ratio = h.sampled / h.original;
        //    var d_ratio = d.sampled / d.original;
        //    var x = ICEV_Lifetime_years[year];
        //    return (x * (1f / (h_ratio * d_ratio)), x);
        //}

        //public (Years sampled, Years original) GetSampled_BEV_lifetime_years(ModelYear year, int id)
        //{
        //    var h = GetSampled_Utilization_h_per_day(year, id);
        //    var d = GetSampled_Utilization_calendar_days_per_year(year, id);
        //    var h_ratio = h.sampled / h.original;
        //    var d_ratio = d.sampled / d.original;
        //    var x = BEV_Lifetime_years[year];
        //    return (x * (1f / (h_ratio * d_ratio)), x);
        //}

        public override string ToString()
        {
            return GetType().Name.Replace("Internal", "");
        }
    }

    public static class BatteryInternalExtensions
    {
        static readonly OtherUnit HOURS_PER_YEAR = new OtherUnit(365 * 24);
        static readonly Dimensionless minAgeingRate = new Dimensionless(0.75f);

        public static (Dimensionless cyclingWear_ratio, Dimensionless calendarWear_ratio) GetStateOfHealthLoss_ratioOfLifetime(
            this BatteryInternal batteryTech,
            ModelYear year, 
            Hours calendarTime_hours, 
            KiloWattHours charging_kWh, 
            CRate charging_cRate_net, 
            KiloWattHours discharging_kWh, 
            CRate discharging_cRate_net, 
            KiloWattHours netCapacityUseable_kWh)
        {
            //cycle ageing
            KiloWattHours lifetime_kWh_bidirectional = netCapacityUseable_kWh * new Dimensionless(batteryTech.Net_Cycle_lifetime_cycles[year].Val * 2); //2 = in + out

            Dimensionless normChargingRate = charging_cRate_net / Battery.Net_Reference_charging_rate_c[year];
            Dimensionless normDischargingRate = discharging_cRate_net / Battery.Net_Reference_discharging_rate_c[year];

            //Dim: 0 per kWh
            OtherUnit chargingWearPerkWh_ratio_per_kWh = (OtherUnit)(Math.Max(minAgeingRate.Val, normChargingRate.Val * normChargingRate.Val) / lifetime_kWh_bidirectional.Val); //Slow charging helps a little, fast charging hurts a lot
            OtherUnit dischargingWearPerkWh_ratio_per_kWh = (OtherUnit)(Math.Max(minAgeingRate.Val, normDischargingRate.Val * normDischargingRate.Val) / lifetime_kWh_bidirectional.Val);

            //Ratio of battery ageing from cycling during this time period
            Dimensionless chargingWear_ratio = new Dimensionless(charging_kWh.Val * chargingWearPerkWh_ratio_per_kWh.Val);
            Dimensionless dischargingWear_ratio = new Dimensionless(discharging_kWh.Val * dischargingWearPerkWh_ratio_per_kWh.Val);

            //Ratio of battery ageing from time during this time period (dim: h / (y * h/y) => 0
            Dimensionless calendarWear_ratio = new Dimensionless(calendarTime_hours.Val / (batteryTech.Net_Calendar_lifetime_years[year].Val * HOURS_PER_YEAR.Val));

            return (chargingWear_ratio + dischargingWear_ratio, calendarWear_ratio);
        }
    }

    public partial class Parameters
    {
        public const bool VERBOSE = false;

        //This is a hack to make it easy to copy everything from Excel
        public static VehicleType HGV60 { get; } = new HGV60Internal();
        public static VehicleType HGV40 { get; } = new HGV40Internal();
        public static VehicleType MGV24 { get; } = new MGV24Internal();
        public static VehicleType MGV16 { get; } = new MGV16Internal();
        public static WorldInternal World { get; } = new WorldInternal();
        public static InfrastructureInternal Infrastructure { get; } = new InfrastructureInternal();
        public static BatteryInternal Battery { get; } = new BatteryInternal();
        public static Dictionary<short, VehicleType> VehicleTypes { get; } = new Dictionary<short, VehicleType>() { { 102, MGV16 }, { 103, MGV24 }, { 104, HGV40 }, { 105, HGV60 } };
        public static readonly VehicleType[] VehicleTypesInOrder = new VehicleType[] { MGV16, MGV24, HGV40, HGV60 };

        public static EuroPerKiloWattHour GetMeanElectricityPrice(ModelYear year, RouteSegmentType placement, ElectricityPriceRegion region)
        {
            EuroPerKiloWattHour priceLow, priceHigh;
            switch (region)
            {
                case ElectricityPriceRegion.SE12:
                    priceLow = World.SE12_Price_min_euro_per_kWh[year];
                    priceHigh = World.SE12_Price_max_euro_per_kWh[year];
                    break;
                case ElectricityPriceRegion.SE34:
                    priceLow = World.SE34_Price_min_euro_per_kWh[year];
                    priceHigh = World.SE34_Price_max_euro_per_kWh[year];
                    break;
                case ElectricityPriceRegion.Other:
                    priceLow = World.OtherRegion_Price_min_euro_per_kWh[year];
                    priceHigh = World.OtherRegion_Price_max_euro_per_kWh[year];
                    break;
                default:
                    throw new NotImplementedException();
            }

            var r = placement switch
            {
                RouteSegmentType.Depot => Infrastructure.Depot_electricity_price_ratio[year],
                RouteSegmentType.Destination => Infrastructure.Destination_electricity_price_ratio[year],
                RouteSegmentType.Road => Infrastructure.ERS_electricity_price_ratio[year],
                RouteSegmentType.RestStop => Infrastructure.Rest_Stop_electricity_price_ratio[year],
                _ => throw new NotImplementedException(),
            };
            return priceHigh * r + priceLow * (1 - r);
        }

        public static KilogramPerKiloWattHour GetCO2_kgPerkWh(ModelYear year, ElectricityPriceRegion region)
        {
            return region switch
            {
                ElectricityPriceRegion.SE12 => World.SE12_CO2_emissions_kg_per_kWh[year],
                ElectricityPriceRegion.SE34 => World.SE34_CO2_emissions_kg_per_kWh[year],
                ElectricityPriceRegion.Other => World.OtherRegion_CO2_emissions_kg_per_kWh[year],
                _ => throw new NotImplementedException(),
            };
        }

        public static void ResetAll()
        {
            foreach (var obj in new object[] { HGV60, HGV40, MGV24, MGV16, World, Infrastructure, Battery })
            {
                foreach (var prop in obj.GetType().GetProperties())
                {
                    var series = prop.GetValue(obj);
                    series.GetType().GetMethod("ResetToDefault").Invoke(series, null);
                }
            }

        }

        public static void ModifySCCAndCO2Tax(float[] SCC, float[] co2TaxRatio)
        {
            World.CO2_SCC_euro_per_kg.Set(SCC);
            World.CO2_Tax_ratio_of_SCC.Set(co2TaxRatio);
            List<float> se12 = new();
            List<float> se34 = new();
            List<float> other = new();
            List<float> diesel = new();
            for (int i = 0; i < 7; i++)
            {
                ModelYear y = (ModelYear)i;
                var taxRatio = World.CO2_SCC_euro_per_kg[y] * World.CO2_Tax_ratio_of_SCC[y];
                se12.Add((taxRatio * World.SE12_CO2_emissions_kg_per_kWh[y]).Val);
                se34.Add((taxRatio * World.SE34_CO2_emissions_kg_per_kWh[y]).Val);
                other.Add((taxRatio * World.OtherRegion_CO2_emissions_kg_per_kWh[y]).Val);
                diesel.Add((taxRatio * World.Diesel_Emissions_kg_CO2_per_liter[y]).Val);
            }
            World.SE12_CO2_tax_euro_per_kWh.Set(se12);
            World.SE34_CO2_tax_euro_per_kWh.Set(se34);
            World.OtherRegion_CO2_tax_euro_per_kWh.Set(other);
            World.Diesel_CO2_tax_euro_per_liter.Set(diesel);
        }

        public static void ModifyBatteryPackCost(float[] euroPerkWh)
        {
            Battery.Gross_Pack_cost_euro_per_kWh.Set(euroPerkWh);
            List<float> netCost = new List<float>();
            for (int i = 0; i < 7; i++)
            {
                ModelYear y = (ModelYear)i;
                netCost.Add(euroPerkWh[i] / Battery.Gross_SoC_window_ratio[y].Val);
            }
            Battery.Net_Pack_cost_euro_per_kWh.Set(netCost);
        }

        public static void ModifyRenewableFuelSupplyCap(float[] literPerYear)
        {

            float[] newBlendRatio = new float[7];
            float[] dieselPrice = new float[7];
            float[] co2Emissions = new float[7];
            float[] co2Tax = new float[7];
            for (int i = 0; i < 7; i++)
            {
                //Guesstimate the new blend ratio
                ModelYear y = (ModelYear)i;
                float oldSupplyCap = World.RenewableDiesel_Supply_cap_liter_per_year[y].Val;
                float oldBlendRatio = World.RenewableDiesel_Blend_guess_ratio[y].Val;
                float totalConsumption = oldSupplyCap / oldBlendRatio;
                newBlendRatio[i] = Math.Min(1, Math.Max(0, literPerYear[i] / totalConsumption));

                //Adjust diesel price, CO2 emissions and CO2 tax to match
                float fossilPrice = World.FossilDiesel_Price_euro_per_liter[y].Val;
                float renewablePrice = World.RenewableDiesel_Price_euro_per_liter[y].Val;
                float fossilCO2 = World.FossilDiesel_Emissions_kg_CO2_per_liter[y].Val;
                float renewableCO2 = World.RenewableDiesel_Emissions_kg_CO2_per_liter[y].Val;
                dieselPrice[i] = (1 - newBlendRatio[i]) * fossilPrice + newBlendRatio[i] * renewablePrice;
                co2Emissions[i] = (1 - newBlendRatio[i]) * fossilCO2 + newBlendRatio[i] * renewableCO2;
                float co2Price = World.CO2_SCC_euro_per_kg[y].Val;
                float taxRatio = World.CO2_Tax_ratio_of_SCC[y].Val;
                co2Tax[i] = co2Emissions[i] * co2Price * taxRatio;
            }

            World.RenewableDiesel_Supply_cap_liter_per_year.Set(literPerYear);
            World.RenewableDiesel_Blend_guess_ratio.Set(newBlendRatio);
            World.Diesel_Price_euro_per_liter.Set(dieselPrice);
            World.Diesel_Emissions_kg_CO2_per_liter.Set(co2Emissions);
            World.Diesel_CO2_tax_euro_per_liter.Set(co2Tax);
        }
    }
}