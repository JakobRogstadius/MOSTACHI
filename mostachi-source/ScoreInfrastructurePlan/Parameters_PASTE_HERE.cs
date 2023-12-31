﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScoreInfrastructurePlan
{
    public partial class Parameters
    {
        /* REPLACE CODE STARTING HERE */
        public class HGV60Internal : VehicleType
        {
            public override ParameterTimeSeries<KilometersPerYear> Common_Annual_distance_km { get; } = new ParameterTimeSeries<KilometersPerYear> { 90000f, 90000f, 90000f, 90000f, 90000f, 90000f, 90000f };
            public override ParameterTimeSeries<Dimensionless> Common_Utilization_calendar_days_per_year { get; } = new ParameterTimeSeries<Dimensionless> { 230f, 230f, 230f, 230f, 230f, 230f, 230f };
            public override ParameterTimeSeries<Hours> Common_Max_time_in_transit_h_per_day { get; } = new ParameterTimeSeries<Hours> { 9f, 9f, 9f, 9f, 9f, 9f, 9f };
            public override ParameterTimeSeries<Hours> Common_Max_time_in_use_h_per_day { get; } = new ParameterTimeSeries<Hours> { 12f, 12f, 12f, 12f, 12f, 12f, 12f };
            public override ParameterTimeSeries<KiloWatts> Common_Power_peak_kW { get; } = new ParameterTimeSeries<KiloWatts> { 750f, 750f, 750f, 750f, 750f, 750f, 750f };
            public override ParameterTimeSeries<EuroPerKilometer> Common_Tyres_euro_per_km { get; } = new ParameterTimeSeries<EuroPerKilometer> { 0.11328f, 0.11328f, 0.11328f, 0.11328f, 0.11328f, 0.11328f, 0.11328f };
            public override ParameterTimeSeries<EuroPerTonKilometer> Common_Cargo_capacity_value_euro_per_ton_km { get; } = new ParameterTimeSeries<EuroPerTonKilometer> { 0.025f, 0.025f, 0.025f, 0.025f, 0.025f, 0.025f, 0.025f };
            public override ParameterTimeSeries<EuroPerCubicMeter> Common_Cargo_capacity_value_euro_per_m3_km { get; } = new ParameterTimeSeries<EuroPerCubicMeter> { 0.007f, 0.007f, 0.007f, 0.007f, 0.007f, 0.007f, 0.007f };
            public override ParameterTimeSeries<Kilogram> Common_Total_weight_limit_kg { get; } = new ParameterTimeSeries<Kilogram> { 60000f, 60000f, 60000f, 60000f, 60000f, 60000f, 60000f };
            public override ParameterTimeSeries<Hours> Common_Depot_stop_h { get; } = new ParameterTimeSeries<Hours> { 12f, 12f, 12f, 12f, 12f, 12f, 12f };
            public override ParameterTimeSeries<Hours> Common_Destination_stop_h { get; } = new ParameterTimeSeries<Hours> { 1f, 1f, 1f, 1f, 1f, 1f, 1f };
            public override ParameterTimeSeries<Hours> Common_Rest_stop_h { get; } = new ParameterTimeSeries<Hours> { 0.75f, 0.75f, 0.75f, 0.75f, 0.75f, 0.75f, 0.75f };
            public override ParameterTimeSeries<Hours> Common_Drive_session_h { get; } = new ParameterTimeSeries<Hours> { 4.5f, 4.5f, 4.5f, 4.5f, 4.5f, 4.5f, 4.5f };
            public override ParameterTimeSeries<EuroPerHour> Common_Driver_cost_euro_per_h { get; } = new ParameterTimeSeries<EuroPerHour> { 33.6f, 33.6f, 33.6f, 33.6f, 33.6f, 33.6f, 33.6f };
            public override ParameterTimeSeries<Kilogram> ICEV_Chassis_weight_kg { get; } = new ParameterTimeSeries<Kilogram> { 23000f, 23000f, 23000f, 23000f, 23000f, 23000f, 23000f };
            public override ParameterTimeSeries<Euro> ICEV_Chassis_cost_2020_euro { get; } = new ParameterTimeSeries<Euro> { 259200f, 259200f, 259200f, 259200f, 259200f, 259200f, 259200f };
            public override ParameterTimeSeries<Years> ICEV_Lifetime_years { get; } = new ParameterTimeSeries<Years> { 10f, 10f, 10f, 10f, 10f, 10f, 10f };
            public override ParameterTimeSeries<EuroPerKilometer> ICEV_Maintenance_cost_euro_per_km { get; } = new ParameterTimeSeries<EuroPerKilometer> { 0.09984f, 0.09984f, 0.09984f, 0.09984f, 0.09984f, 0.09984f, 0.09984f };
            public override ParameterTimeSeries<Dimensionless> ICEV_Residual_value_at_end_of_life_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.09f, 0.09f, 0.09f, 0.09f, 0.09f, 0.09f, 0.09f };
            public override ParameterTimeSeries<LiterPerKilometer> ICEV_Fuel_consumption_liter_per_km { get; } = new ParameterTimeSeries<LiterPerKilometer> { 0.35f, 0.341337063592656f, 0.33288854566302f, 0.324649139086415f, 0.316613668096116f, 0.308777085032081f, 0.301134467170144f };
            public override ParameterTimeSeries<EuroPerKilometer> BEV_Maintenance_cost_euro_per_km { get; } = new ParameterTimeSeries<EuroPerKilometer> { 0.039936f, 0.039936f, 0.039936f, 0.039936f, 0.039936f, 0.039936f, 0.039936f };
            public override ParameterTimeSeries<Dimensionless> BEV_Residual_value_at_end_of_life_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.09f, 0.09f, 0.09f, 0.09f, 0.09f, 0.09f, 0.09f };
            public override ParameterTimeSeries<KiloWattHoursPerKilometer> BEV_Energy_consumption_kWh_per_km { get; } = new ParameterTimeSeries<KiloWattHoursPerKilometer> { 1.45730611764706f, 1.38588361754075f, 1.31796153060067f, 1.25336830175221f, 1.19194078382642f, 1.13352382548893f, 1.07796987936455f };
            public override ParameterTimeSeries<Kilometers> BEV_Min_range_buffer_km { get; } = new ParameterTimeSeries<Kilometers> { 50f, 50f, 50f, 50f, 50f, 50f, 50f };
            public override ParameterTimeSeries<Years> BEV_Lifetime_years { get; } = new ParameterTimeSeries<Years> { 10f, 11.6666666666667f, 13.3333333333333f, 15f, 15f, 15f, 15f };
            public override ParameterTimeSeries<Euro> ICEV_Chassis_cost_euro { get; } = new ParameterTimeSeries<Euro> { 259200f, 262736.783864086f, 266453.979250271f, 270360.788959337f, 274466.885227394f, 278782.4336718f, 283318.118458564f };
            public override ParameterTimeSeries<Kilogram> BEV_Chassis_weight_excl_battery_kg { get; } = new ParameterTimeSeries<Kilogram> { 17780f, 17780f, 17780f, 17780f, 17780f, 17780f, 17780f };
            public override ParameterTimeSeries<Euro> BEV_Chassis_cost_excl_battery_euro { get; } = new ParameterTimeSeries<Euro> { 215794.56f, 213303.264692705f, 211051.331053471f, 209015.761403954f, 207175.767664421f, 205512.559057275f, 204009.150207859f };
            public override ParameterTimeSeries<KiloWattHours> BEV_Min_net_battery_capacity_kWh { get; } = new ParameterTimeSeries<KiloWattHours> { 216.666666666667f, 187.1682907583f, 161.686011071002f, 139.673050761632f, 120.657074658702f, 104.230054300458f, 90.0395128110531f };
        }
        public class HGV40Internal : VehicleType
        {
            public override ParameterTimeSeries<KilometersPerYear> Common_Annual_distance_km { get; } = new ParameterTimeSeries<KilometersPerYear> { 115000f, 115000f, 115000f, 115000f, 115000f, 115000f, 115000f };
            public override ParameterTimeSeries<Dimensionless> Common_Utilization_calendar_days_per_year { get; } = new ParameterTimeSeries<Dimensionless> { 230f, 230f, 230f, 230f, 230f, 230f, 230f };
            public override ParameterTimeSeries<Hours> Common_Max_time_in_transit_h_per_day { get; } = new ParameterTimeSeries<Hours> { 11f, 11f, 11f, 11f, 11f, 11f, 11f };
            public override ParameterTimeSeries<Hours> Common_Max_time_in_use_h_per_day { get; } = new ParameterTimeSeries<Hours> { 15f, 15f, 15f, 15f, 15f, 15f, 15f };
            public override ParameterTimeSeries<KiloWatts> Common_Power_peak_kW { get; } = new ParameterTimeSeries<KiloWatts> { 550f, 550f, 550f, 550f, 550f, 550f, 550f };
            public override ParameterTimeSeries<EuroPerKilometer> Common_Tyres_euro_per_km { get; } = new ParameterTimeSeries<EuroPerKilometer> { 0.09024f, 0.09024f, 0.09024f, 0.09024f, 0.09024f, 0.09024f, 0.09024f };
            public override ParameterTimeSeries<EuroPerTonKilometer> Common_Cargo_capacity_value_euro_per_ton_km { get; } = new ParameterTimeSeries<EuroPerTonKilometer> { 0.025f, 0.025f, 0.025f, 0.025f, 0.025f, 0.025f, 0.025f };
            public override ParameterTimeSeries<EuroPerCubicMeter> Common_Cargo_capacity_value_euro_per_m3_km { get; } = new ParameterTimeSeries<EuroPerCubicMeter> { 0.007f, 0.007f, 0.007f, 0.007f, 0.007f, 0.007f, 0.007f };
            public override ParameterTimeSeries<Kilogram> Common_Total_weight_limit_kg { get; } = new ParameterTimeSeries<Kilogram> { 40000f, 40000f, 40000f, 40000f, 40000f, 40000f, 40000f };
            public override ParameterTimeSeries<Hours> Common_Depot_stop_h { get; } = new ParameterTimeSeries<Hours> { 9f, 9f, 9f, 9f, 9f, 9f, 9f };
            public override ParameterTimeSeries<Hours> Common_Destination_stop_h { get; } = new ParameterTimeSeries<Hours> { 1f, 1f, 1f, 1f, 1f, 1f, 1f };
            public override ParameterTimeSeries<Hours> Common_Rest_stop_h { get; } = new ParameterTimeSeries<Hours> { 0.75f, 0.75f, 0.75f, 0.75f, 0.75f, 0.75f, 0.75f };
            public override ParameterTimeSeries<Hours> Common_Drive_session_h { get; } = new ParameterTimeSeries<Hours> { 4.5f, 4.5f, 4.5f, 4.5f, 4.5f, 4.5f, 4.5f };
            public override ParameterTimeSeries<EuroPerHour> Common_Driver_cost_euro_per_h { get; } = new ParameterTimeSeries<EuroPerHour> { 33.6f, 33.6f, 33.6f, 33.6f, 33.6f, 33.6f, 33.6f };
            public override ParameterTimeSeries<Kilogram> ICEV_Chassis_weight_kg { get; } = new ParameterTimeSeries<Kilogram> { 20000f, 20000f, 20000f, 20000f, 20000f, 20000f, 20000f };
            public override ParameterTimeSeries<Euro> ICEV_Chassis_cost_2020_euro { get; } = new ParameterTimeSeries<Euro> { 235200f, 235200f, 235200f, 235200f, 235200f, 235200f, 235200f };
            public override ParameterTimeSeries<Years> ICEV_Lifetime_years { get; } = new ParameterTimeSeries<Years> { 10f, 10f, 10f, 10f, 10f, 10f, 10f };
            public override ParameterTimeSeries<EuroPerKilometer> ICEV_Maintenance_cost_euro_per_km { get; } = new ParameterTimeSeries<EuroPerKilometer> { 0.11424f, 0.11424f, 0.11424f, 0.11424f, 0.11424f, 0.11424f, 0.11424f };
            public override ParameterTimeSeries<Dimensionless> ICEV_Residual_value_at_end_of_life_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.09f, 0.09f, 0.09f, 0.09f, 0.09f, 0.09f, 0.09f };
            public override ParameterTimeSeries<LiterPerKilometer> ICEV_Fuel_consumption_liter_per_km { get; } = new ParameterTimeSeries<LiterPerKilometer> { 0.27f, 0.263317163342906f, 0.256799735225758f, 0.250443621580949f, 0.244244829674147f, 0.238199465596176f, 0.232303731816968f };
            public override ParameterTimeSeries<EuroPerKilometer> BEV_Maintenance_cost_euro_per_km { get; } = new ParameterTimeSeries<EuroPerKilometer> { 0.045696f, 0.045696f, 0.045696f, 0.045696f, 0.045696f, 0.045696f, 0.045696f };
            public override ParameterTimeSeries<Dimensionless> BEV_Residual_value_at_end_of_life_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.09f, 0.09f, 0.09f, 0.09f, 0.09f, 0.09f, 0.09f };
            public override ParameterTimeSeries<KiloWattHoursPerKilometer> BEV_Energy_consumption_kWh_per_km { get; } = new ParameterTimeSeries<KiloWattHoursPerKilometer> { 1.12420757647059f, 1.06911021924572f, 1.01671318074909f, 0.966884118494564f, 0.919497176094663f, 0.874432665377173f, 0.831576764081227f };
            public override ParameterTimeSeries<Kilometers> BEV_Min_range_buffer_km { get; } = new ParameterTimeSeries<Kilometers> { 50f, 50f, 50f, 50f, 50f, 50f, 50f };
            public override ParameterTimeSeries<Years> BEV_Lifetime_years { get; } = new ParameterTimeSeries<Years> { 10f, 11.6666666666667f, 13.3333333333333f, 15f, 15f, 15f, 15f };
            public override ParameterTimeSeries<Euro> ICEV_Chassis_cost_euro { get; } = new ParameterTimeSeries<Euro> { 235200f, 237794.607834718f, 240521.566745076f, 243387.627966071f, 246399.887113539f, 249565.801751033f, 252893.209852799f };
            public override ParameterTimeSeries<Kilogram> BEV_Chassis_weight_excl_battery_kg { get; } = new ParameterTimeSeries<Kilogram> { 16172f, 16172f, 16172f, 16172f, 16172f, 16172f, 16172f };
            public override ParameterTimeSeries<Euro> BEV_Chassis_cost_excl_battery_euro { get; } = new ParameterTimeSeries<Euro> { 204865.92f, 202893.36015395f, 201110.322286173f, 199498.597276008f, 198041.725520596f, 196724.828842609f, 195534.45854814f };
            public override ParameterTimeSeries<KiloWattHours> BEV_Min_net_battery_capacity_kWh { get; } = new ParameterTimeSeries<KiloWattHours> { 158.888888888889f, 137.256746556087f, 118.569741452068f, 102.426903891864f, 88.4818547497146f, 76.435373153669f, 66.0289760614389f };
        }
        public class MGV24Internal : VehicleType
        {
            public override ParameterTimeSeries<KilometersPerYear> Common_Annual_distance_km { get; } = new ParameterTimeSeries<KilometersPerYear> { 80000f, 80000f, 80000f, 80000f, 80000f, 80000f, 80000f };
            public override ParameterTimeSeries<Dimensionless> Common_Utilization_calendar_days_per_year { get; } = new ParameterTimeSeries<Dimensionless> { 230f, 230f, 230f, 230f, 230f, 230f, 230f };
            public override ParameterTimeSeries<Hours> Common_Max_time_in_transit_h_per_day { get; } = new ParameterTimeSeries<Hours> { 9f, 9f, 9f, 9f, 9f, 9f, 9f };
            public override ParameterTimeSeries<Hours> Common_Max_time_in_use_h_per_day { get; } = new ParameterTimeSeries<Hours> { 12f, 12f, 12f, 12f, 12f, 12f, 12f };
            public override ParameterTimeSeries<KiloWatts> Common_Power_peak_kW { get; } = new ParameterTimeSeries<KiloWatts> { 300f, 300f, 300f, 300f, 300f, 300f, 300f };
            public override ParameterTimeSeries<EuroPerKilometer> Common_Tyres_euro_per_km { get; } = new ParameterTimeSeries<EuroPerKilometer> { 0.06144f, 0.06144f, 0.06144f, 0.06144f, 0.06144f, 0.06144f, 0.06144f };
            public override ParameterTimeSeries<EuroPerTonKilometer> Common_Cargo_capacity_value_euro_per_ton_km { get; } = new ParameterTimeSeries<EuroPerTonKilometer> { 0.035f, 0.035f, 0.035f, 0.035f, 0.035f, 0.035f, 0.035f };
            public override ParameterTimeSeries<EuroPerCubicMeter> Common_Cargo_capacity_value_euro_per_m3_km { get; } = new ParameterTimeSeries<EuroPerCubicMeter> { 0.0098f, 0.0098f, 0.0098f, 0.0098f, 0.0098f, 0.0098f, 0.0098f };
            public override ParameterTimeSeries<Kilogram> Common_Total_weight_limit_kg { get; } = new ParameterTimeSeries<Kilogram> { 24000f, 24000f, 24000f, 24000f, 24000f, 24000f, 24000f };
            public override ParameterTimeSeries<Hours> Common_Depot_stop_h { get; } = new ParameterTimeSeries<Hours> { 12f, 12f, 12f, 12f, 12f, 12f, 12f };
            public override ParameterTimeSeries<Hours> Common_Destination_stop_h { get; } = new ParameterTimeSeries<Hours> { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f };
            public override ParameterTimeSeries<Hours> Common_Rest_stop_h { get; } = new ParameterTimeSeries<Hours> { 0.75f, 0.75f, 0.75f, 0.75f, 0.75f, 0.75f, 0.75f };
            public override ParameterTimeSeries<Hours> Common_Drive_session_h { get; } = new ParameterTimeSeries<Hours> { 4.5f, 4.5f, 4.5f, 4.5f, 4.5f, 4.5f, 4.5f };
            public override ParameterTimeSeries<EuroPerHour> Common_Driver_cost_euro_per_h { get; } = new ParameterTimeSeries<EuroPerHour> { 30f, 30f, 30f, 30f, 30f, 30f, 30f };
            public override ParameterTimeSeries<Kilogram> ICEV_Chassis_weight_kg { get; } = new ParameterTimeSeries<Kilogram> { 10000f, 10000f, 10000f, 10000f, 10000f, 10000f, 10000f };
            public override ParameterTimeSeries<Euro> ICEV_Chassis_cost_2020_euro { get; } = new ParameterTimeSeries<Euro> { 148800f, 148800f, 148800f, 148800f, 148800f, 148800f, 148800f };
            public override ParameterTimeSeries<Years> ICEV_Lifetime_years { get; } = new ParameterTimeSeries<Years> { 7f, 7f, 7f, 7f, 7f, 7f, 7f };
            public override ParameterTimeSeries<EuroPerKilometer> ICEV_Maintenance_cost_euro_per_km { get; } = new ParameterTimeSeries<EuroPerKilometer> { 0.12096f, 0.12096f, 0.12096f, 0.12096f, 0.12096f, 0.12096f, 0.12096f };
            public override ParameterTimeSeries<Dimensionless> ICEV_Residual_value_at_end_of_life_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.09f, 0.09f, 0.09f, 0.09f, 0.09f, 0.09f, 0.09f };
            public override ParameterTimeSeries<LiterPerKilometer> ICEV_Fuel_consumption_liter_per_km { get; } = new ParameterTimeSeries<LiterPerKilometer> { 0.22f, 0.214554725686813f, 0.20924422870247f, 0.204065173140032f, 0.199014305660416f, 0.194088453448736f, 0.189284522221233f };
            public override ParameterTimeSeries<EuroPerKilometer> BEV_Maintenance_cost_euro_per_km { get; } = new ParameterTimeSeries<EuroPerKilometer> { 0.048384f, 0.048384f, 0.048384f, 0.048384f, 0.048384f, 0.048384f, 0.048384f };
            public override ParameterTimeSeries<Dimensionless> BEV_Residual_value_at_end_of_life_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.09f, 0.09f, 0.09f, 0.09f, 0.09f, 0.09f, 0.09f };
            public override ParameterTimeSeries<KiloWattHoursPerKilometer> BEV_Energy_consumption_kWh_per_km { get; } = new ParameterTimeSeries<KiloWattHoursPerKilometer> { 0.641214691764706f, 0.609788791717931f, 0.579903073464295f, 0.551482052770974f, 0.524453944883623f, 0.498750483215128f, 0.474306746920404f };
            public override ParameterTimeSeries<Kilometers> BEV_Min_range_buffer_km { get; } = new ParameterTimeSeries<Kilometers> { 30f, 30f, 30f, 30f, 30f, 30f, 30f };
            public override ParameterTimeSeries<Years> BEV_Lifetime_years { get; } = new ParameterTimeSeries<Years> { 7f, 8.66666666666667f, 10.3333333333333f, 12f, 12f, 12f, 12f };
            public override ParameterTimeSeries<Euro> ICEV_Chassis_cost_euro { get; } = new ParameterTimeSeries<Euro> { 148800f, 150216.88779801f, 151706.051113582f, 153271.176724488f, 154916.13947122f, 156645.011850076f, 158462.074095593f };
            public override ParameterTimeSeries<Kilogram> BEV_Chassis_weight_excl_battery_kg { get; } = new ParameterTimeSeries<Kilogram> { 7912f, 7912f, 7912f, 7912f, 7912f, 7912f, 7912f };
            public override ParameterTimeSeries<Euro> BEV_Chassis_cost_excl_battery_euro { get; } = new ParameterTimeSeries<Euro> { 134805.12f, 133480.979480506f, 132284.061327051f, 131202.142116074f, 130224.172840815f, 129340.166074278f, 128541.093973492f };
            public override ParameterTimeSeries<KiloWattHours> BEV_Min_net_battery_capacity_kWh { get; } = new ParameterTimeSeries<KiloWattHours> { 86.6666666666667f, 74.8673163033202f, 64.6744044284006f, 55.8692203046529f, 48.2628298634807f, 41.6920217201831f, 36.0158051244212f };
        }
        public class MGV16Internal : VehicleType
        {
            public override ParameterTimeSeries<KilometersPerYear> Common_Annual_distance_km { get; } = new ParameterTimeSeries<KilometersPerYear> { 42000f, 42000f, 42000f, 42000f, 42000f, 42000f, 42000f };
            public override ParameterTimeSeries<Dimensionless> Common_Utilization_calendar_days_per_year { get; } = new ParameterTimeSeries<Dimensionless> { 230f, 230f, 230f, 230f, 230f, 230f, 230f };
            public override ParameterTimeSeries<Hours> Common_Max_time_in_transit_h_per_day { get; } = new ParameterTimeSeries<Hours> { 9f, 9f, 9f, 9f, 9f, 9f, 9f };
            public override ParameterTimeSeries<Hours> Common_Max_time_in_use_h_per_day { get; } = new ParameterTimeSeries<Hours> { 12f, 12f, 12f, 12f, 12f, 12f, 12f };
            public override ParameterTimeSeries<KiloWatts> Common_Power_peak_kW { get; } = new ParameterTimeSeries<KiloWatts> { 160f, 160f, 160f, 160f, 160f, 160f, 160f };
            public override ParameterTimeSeries<EuroPerKilometer> Common_Tyres_euro_per_km { get; } = new ParameterTimeSeries<EuroPerKilometer> { 0.03072f, 0.03072f, 0.03072f, 0.03072f, 0.03072f, 0.03072f, 0.03072f };
            public override ParameterTimeSeries<EuroPerTonKilometer> Common_Cargo_capacity_value_euro_per_ton_km { get; } = new ParameterTimeSeries<EuroPerTonKilometer> { 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f };
            public override ParameterTimeSeries<EuroPerCubicMeter> Common_Cargo_capacity_value_euro_per_m3_km { get; } = new ParameterTimeSeries<EuroPerCubicMeter> { 0.014f, 0.014f, 0.014f, 0.014f, 0.014f, 0.014f, 0.014f };
            public override ParameterTimeSeries<Kilogram> Common_Total_weight_limit_kg { get; } = new ParameterTimeSeries<Kilogram> { 16000f, 16000f, 16000f, 16000f, 16000f, 16000f, 16000f };
            public override ParameterTimeSeries<Hours> Common_Depot_stop_h { get; } = new ParameterTimeSeries<Hours> { 12f, 12f, 12f, 12f, 12f, 12f, 12f };
            public override ParameterTimeSeries<Hours> Common_Destination_stop_h { get; } = new ParameterTimeSeries<Hours> { 0.2f, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f };
            public override ParameterTimeSeries<Hours> Common_Rest_stop_h { get; } = new ParameterTimeSeries<Hours> { 0.75f, 0.75f, 0.75f, 0.75f, 0.75f, 0.75f, 0.75f };
            public override ParameterTimeSeries<Hours> Common_Drive_session_h { get; } = new ParameterTimeSeries<Hours> { 4.5f, 4.5f, 4.5f, 4.5f, 4.5f, 4.5f, 4.5f };
            public override ParameterTimeSeries<EuroPerHour> Common_Driver_cost_euro_per_h { get; } = new ParameterTimeSeries<EuroPerHour> { 30f, 30f, 30f, 30f, 30f, 30f, 30f };
            public override ParameterTimeSeries<Kilogram> ICEV_Chassis_weight_kg { get; } = new ParameterTimeSeries<Kilogram> { 5000f, 5000f, 5000f, 5000f, 5000f, 5000f, 5000f };
            public override ParameterTimeSeries<Euro> ICEV_Chassis_cost_2020_euro { get; } = new ParameterTimeSeries<Euro> { 91200f, 91200f, 91200f, 91200f, 91200f, 91200f, 91200f };
            public override ParameterTimeSeries<Years> ICEV_Lifetime_years { get; } = new ParameterTimeSeries<Years> { 7f, 7f, 7f, 7f, 7f, 7f, 7f };
            public override ParameterTimeSeries<EuroPerKilometer> ICEV_Maintenance_cost_euro_per_km { get; } = new ParameterTimeSeries<EuroPerKilometer> { 0.10176f, 0.10176f, 0.10176f, 0.10176f, 0.10176f, 0.10176f, 0.10176f };
            public override ParameterTimeSeries<Dimensionless> ICEV_Residual_value_at_end_of_life_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.09f, 0.09f, 0.09f, 0.09f, 0.09f, 0.09f, 0.09f };
            public override ParameterTimeSeries<LiterPerKilometer> ICEV_Fuel_consumption_liter_per_km { get; } = new ParameterTimeSeries<LiterPerKilometer> { 0.16f, 0.1560398004995f, 0.152177620874524f, 0.148411035010933f, 0.144737676843939f, 0.141155238871808f, 0.137661470706351f };
            public override ParameterTimeSeries<EuroPerKilometer> BEV_Maintenance_cost_euro_per_km { get; } = new ParameterTimeSeries<EuroPerKilometer> { 0.040704f, 0.040704f, 0.040704f, 0.040704f, 0.040704f, 0.040704f, 0.040704f };
            public override ParameterTimeSeries<Dimensionless> BEV_Residual_value_at_end_of_life_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.09f, 0.09f, 0.09f, 0.09f, 0.09f, 0.09f, 0.09f };
            public override ParameterTimeSeries<KiloWattHoursPerKilometer> BEV_Energy_consumption_kWh_per_km { get; } = new ParameterTimeSeries<KiloWattHoursPerKilometer> { 0.466337957647059f, 0.44348275761304f, 0.421747689792215f, 0.401077856560708f, 0.381421050824453f, 0.362727624156457f, 0.344950361396657f };
            public override ParameterTimeSeries<Kilometers> BEV_Min_range_buffer_km { get; } = new ParameterTimeSeries<Kilometers> { 20f, 20f, 20f, 20f, 20f, 20f, 20f };
            public override ParameterTimeSeries<Years> BEV_Lifetime_years { get; } = new ParameterTimeSeries<Years> { 7f, 8.66666666666667f, 10.3333333333333f, 12f, 12f, 12f, 12f };
            public override ParameterTimeSeries<Euro> ICEV_Chassis_cost_euro { get; } = new ParameterTimeSeries<Euro> { 91200f, 91957.3645774527f, 92753.3623599453f, 93589.9640292023f, 94469.2407915219f, 95393.3695055391f, 96364.6380715572f };
            public override ParameterTimeSeries<Kilogram> BEV_Chassis_weight_excl_battery_kg { get; } = new ParameterTimeSeries<Kilogram> { 3886.4f, 3886.4f, 3886.4f, 3886.4f, 3886.4f, 3886.4f, 3886.4f };
            public override ParameterTimeSeries<Euro> BEV_Chassis_cost_excl_battery_euro { get; } = new ParameterTimeSeries<Euro> { 86355.072f, 85394.0463033778f, 84525.3551899418f, 83740.1272265117f, 83030.3433401382f, 82388.7549240118f, 81808.8098116891f };
            public override ParameterTimeSeries<KiloWattHours> BEV_Min_net_battery_capacity_kWh { get; } = new ParameterTimeSeries<KiloWattHours> { 46.2222222222222f, 39.9292353617708f, 34.493015695147f, 29.7969174958149f, 25.7401759271897f, 22.235744917431f, 19.2084293996913f };
        }
        public class WorldInternal
        {
            public ParameterTimeSeries<OtherUnit> Economy_Public_sector_interest_rate_percent { get; } = new ParameterTimeSeries<OtherUnit> { 0.03f, 0.03f, 0.03f, 0.03f, 0.03f, 0.03f, 0.03f };
            public ParameterTimeSeries<OtherUnit> Economy_Private_sector_interest_public_charging_and_trucks_percent { get; } = new ParameterTimeSeries<OtherUnit> { 0.08f, 0.08f, 0.08f, 0.08f, 0.08f, 0.08f, 0.08f };
            public ParameterTimeSeries<OtherUnit> Economy_Private_sector_interest_depot_charging_percent { get; } = new ParameterTimeSeries<OtherUnit> { 0.12f, 0.12f, 0.12f, 0.12f, 0.12f, 0.12f, 0.12f };
            public ParameterTimeSeries<Dimensionless> Economy_Heavy_traffic_volume_vs_2020_percent { get; } = new ParameterTimeSeries<Dimensionless> { 1f, 1.1040808032f, 1.21899441999476f, 1.34586833832413f, 1.48594739597835f, 1.64060599446473f, 1.81136158410335f };
            public ParameterTimeSeries<Hours> Charging_Extra_stop_overhead_h { get; } = new ParameterTimeSeries<Hours> { 0.25f, 0.214683506425f, 0.184356031723732f, 0.158312797284197f, 0.135948585731687f, 0.116743676313593f, 0.100251767135789f };
            public ParameterTimeSeries<EuroPerKilogram> CO2_SCC_euro_per_kg { get; } = new ParameterTimeSeries<EuroPerKilogram> { 0.2f, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f };
            public ParameterTimeSeries<Dimensionless> CO2_Tax_ratio_of_SCC { get; } = new ParameterTimeSeries<Dimensionless> { 0.5f, 0.55f, 0.6f, 0.65f, 0.7f, 0.75f, 0.8f };
            public ParameterTimeSeries<EuroPerLiter> FossilDiesel_Price_euro_per_liter { get; } = new ParameterTimeSeries<EuroPerLiter> { 1f, 1.0510100501f, 1.1046221254112f, 1.16096895537f, 1.22019003994797f, 1.28243199501723f, 1.34784891533291f };
            public ParameterTimeSeries<EuroPerLiter> RenewableDiesel_Price_euro_per_liter { get; } = new ParameterTimeSeries<EuroPerLiter> { 2f, 2f, 1.9019800998f, 1.80876415001761f, 1.72011670928258f, 1.63581387519446f, 1.55564271879829f };
            public ParameterTimeSeries<LiterPerYear> RenewableDiesel_Supply_cap_liter_per_year { get; } = new ParameterTimeSeries<LiterPerYear> { 82000000f, 90534625.8624f, 99957542.4395701f, 110361203.742579f, 121847686.470225f, 134529691.546108f, 148531649.896475f };
            public ParameterTimeSeries<Dimensionless> RenewableDiesel_Blend_guess_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.05f, 0.1f, 0.5f, 0.77814734375f, 0.901562797829639f, 0.956322890449374f, 0.980620234457743f };
            public ParameterTimeSeries<KilogramPerLiter> FossilDiesel_Emissions_kg_CO2_per_liter { get; } = new ParameterTimeSeries<KilogramPerLiter> { 3.23f, 3.23f, 3.23f, 3.23f, 3.23f, 3.23f, 3.23f };
            public ParameterTimeSeries<KilogramPerLiter> RenewableDiesel_Emissions_kg_CO2_per_liter { get; } = new ParameterTimeSeries<KilogramPerLiter> { 0.67f, 0.518433228125f, 0.401153749289714f, 0.310405124207035f, 0.240185568013723f, 0.185851013991629f, 0.143807971841768f };
            public ParameterTimeSeries<EuroPerLiter> Diesel_Price_euro_per_liter { get; } = new ParameterTimeSeries<EuroPerLiter> { 1.05f, 1.14590904509f, 1.5033011126056f, 1.66504906537905f, 1.67090532666293f, 1.62037917610075f, 1.551615723606f };
            public ParameterTimeSeries<KilogramPerLiter> Diesel_Emissions_kg_CO2_per_liter { get; } = new ParameterTimeSeries<KilogramPerLiter> { 3.102f, 2.9588433228125f, 1.81557687464486f, 0.958125002575593f, 0.53449453570702f, 0.318810642741943f, 0.203617649765858f };
            public ParameterTimeSeries<EuroPerLiter> Diesel_CO2_tax_euro_per_liter { get; } = new ParameterTimeSeries<EuroPerLiter> { 0.3102f, 0.325472765509375f, 0.217869224957383f, 0.124556250334827f, 0.0748292349989828f, 0.0478215964112914f, 0.0325788239625373f };
            public ParameterTimeSeries<EuroPerLiter> Diesel_Pollution_tax_euro_per_liter { get; } = new ParameterTimeSeries<EuroPerLiter> { 0.02f, 0.0322102f, 0.051874849202f, 0.0835449633883131f, 0.134549998986512f, 0.216694118867768f, 0.348988045377729f };
            public ParameterTimeSeries<EuroPerLiter> Diesel_Road_tax_euro_per_liter { get; } = new ParameterTimeSeries<EuroPerLiter> { 0.4f, 0.4f, 0.4f, 0.4f, 0.4f, 0.4f, 0.4f };
            public ParameterTimeSeries<KiloWattHoursPerLiter> Electricity_Conversion_ratio_kWh_per_liter { get; } = new ParameterTimeSeries<KiloWattHoursPerLiter> { 4.16373176470588f, 4.16373176470588f, 4.16373176470588f, 4.16373176470588f, 4.16373176470588f, 4.16373176470588f, 4.16373176470588f };
            public ParameterTimeSeries<EuroPerKiloWattHour> Electricity_Road_tax_euro_per_kWh { get; } = new ParameterTimeSeries<EuroPerKiloWattHour> { 0.096067667804786f, 0.096067667804786f, 0.096067667804786f, 0.096067667804786f, 0.096067667804786f, 0.096067667804786f, 0.096067667804786f };
            public ParameterTimeSeries<EuroPerKiloWattHour> SE12_Price_min_euro_per_kWh { get; } = new ParameterTimeSeries<EuroPerKiloWattHour> { 0.0712952876712329f, 0.0712952876712329f, 0.0712952876712329f, 0.0712952876712329f, 0.0712952876712329f, 0.0712952876712329f, 0.0712952876712329f };
            public ParameterTimeSeries<EuroPerKiloWattHour> SE12_Price_max_euro_per_kWh { get; } = new ParameterTimeSeries<EuroPerKiloWattHour> { 0.125255397260274f, 0.125255397260274f, 0.125255397260274f, 0.125255397260274f, 0.125255397260274f, 0.125255397260274f, 0.125255397260274f };
            public ParameterTimeSeries<KilogramPerKiloWattHour> SE12_CO2_emissions_kg_per_kWh { get; } = new ParameterTimeSeries<KilogramPerKiloWattHour> { 0.265f, 0.174656603648f, 0.115112940369263f, 0.0758688120786044f, 0.0500037322281411f, 0.0329565360026082f, 0.0217210439479946f };
            public ParameterTimeSeries<EuroPerKiloWattHour> SE12_CO2_tax_euro_per_kWh { get; } = new ParameterTimeSeries<EuroPerKiloWattHour> { 0.0265f, 0.01921222640128f, 0.0138135528443115f, 0.00986294557021857f, 0.00700052251193976f, 0.00494348040039123f, 0.00347536703167915f };
            public ParameterTimeSeries<EuroPerKiloWattHour> SE34_Price_min_euro_per_kWh { get; } = new ParameterTimeSeries<EuroPerKiloWattHour> { 0.0712952876712329f, 0.0712952876712329f, 0.0712952876712329f, 0.0712952876712329f, 0.0712952876712329f, 0.0712952876712329f, 0.0712952876712329f };
            public ParameterTimeSeries<EuroPerKiloWattHour> SE34_Price_max_euro_per_kWh { get; } = new ParameterTimeSeries<EuroPerKiloWattHour> { 0.125255397260274f, 0.125255397260274f, 0.125255397260274f, 0.125255397260274f, 0.125255397260274f, 0.125255397260274f, 0.125255397260274f };
            public ParameterTimeSeries<KilogramPerKiloWattHour> SE34_CO2_emissions_kg_per_kWh { get; } = new ParameterTimeSeries<KilogramPerKiloWattHour> { 0.265f, 0.174656603648f, 0.115112940369263f, 0.0758688120786044f, 0.0500037322281411f, 0.0329565360026082f, 0.0217210439479946f };
            public ParameterTimeSeries<EuroPerKiloWattHour> SE34_CO2_tax_euro_per_kWh { get; } = new ParameterTimeSeries<EuroPerKiloWattHour> { 0.0265f, 0.01921222640128f, 0.0138135528443115f, 0.00986294557021857f, 0.00700052251193976f, 0.00494348040039123f, 0.00347536703167915f };
            public ParameterTimeSeries<EuroPerKiloWattHour> OtherRegion_Price_min_euro_per_kWh { get; } = new ParameterTimeSeries<EuroPerKiloWattHour> { 0.0712952876712329f, 0.0712952876712329f, 0.0712952876712329f, 0.0712952876712329f, 0.0712952876712329f, 0.0712952876712329f, 0.0712952876712329f };
            public ParameterTimeSeries<EuroPerKiloWattHour> OtherRegion_Price_max_euro_per_kWh { get; } = new ParameterTimeSeries<EuroPerKiloWattHour> { 0.125255397260274f, 0.125255397260274f, 0.125255397260274f, 0.125255397260274f, 0.125255397260274f, 0.125255397260274f, 0.125255397260274f };
            public ParameterTimeSeries<KilogramPerKiloWattHour> OtherRegion_CO2_emissions_kg_per_kWh { get; } = new ParameterTimeSeries<KilogramPerKiloWattHour> { 0.265f, 0.174656603648f, 0.115112940369263f, 0.0758688120786044f, 0.0500037322281411f, 0.0329565360026082f, 0.0217210439479946f };
            public ParameterTimeSeries<EuroPerKiloWattHour> OtherRegion_CO2_tax_euro_per_kWh { get; } = new ParameterTimeSeries<EuroPerKiloWattHour> { 0.0265f, 0.01921222640128f, 0.0138135528443115f, 0.00986294557021857f, 0.00700052251193976f, 0.00494348040039123f, 0.00347536703167915f };
        }
        public class InfrastructureInternal
        {
            public ParameterTimeSeries<EuroPerKilometer> ERS_base_cost_euro_per_km { get; } = new ParameterTimeSeries<EuroPerKilometer> { 1200000f, 1141188.05988f, 1085258.49001057f, 1032070.02556955f, 981488.325116677f, 933385.631278976f, 887640.448065936f };
            public ParameterTimeSeries<EuroPerKiloWattKilometer> ERS_power_cost_euro_per_kW_km { get; } = new ParameterTimeSeries<EuroPerKiloWattKilometer> { 150f, 142.648507485f, 135.657311251321f, 129.008753196193f, 122.686040639585f, 116.673203909872f, 110.955056008242f };
            public ParameterTimeSeries<OtherUnit> ERS_maintenance_cost_ratio_per_year { get; } = new ParameterTimeSeries<OtherUnit> { 0.02f, 0.02f, 0.02f, 0.02f, 0.02f, 0.02f, 0.02f };
            public ParameterTimeSeries<Years> ERS_write_off_period_years { get; } = new ParameterTimeSeries<Years> { 25f, 25f, 25f, 25f, 25f, 25f, 25f };
            public ParameterTimeSeries<Dimensionless> ERS_profit_margin_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f };
            public ParameterTimeSeries<Euro> ERS_pick_up_cost_base_light_euro { get; } = new ParameterTimeSeries<Euro> { 500f, 451.9603984f, 408.536403443773f, 369.284551322702f, 333.803985877547f, 301.732364889448f, 272.742159691218f };
            public ParameterTimeSeries<Euro> ERS_pick_up_cost_base_heavy_euro { get; } = new ParameterTimeSeries<Euro> { 2000f, 1807.8415936f, 1634.14561377509f, 1477.13820529081f, 1335.21594351019f, 1206.92945955779f, 1090.96863876487f };
            public ParameterTimeSeries<EuroPerKiloWatt> ERS_pick_up_cost_euro_per_kW { get; } = new ParameterTimeSeries<EuroPerKiloWatt> { 50f, 45.19603984f, 40.8536403443773f, 36.9284551322702f, 33.3803985877547f, 30.1732364889448f, 27.2742159691218f };
            public ParameterTimeSeries<Years> ERS_pick_up_lifespan_years { get; } = new ParameterTimeSeries<Years> { 10f, 10f, 10f, 10f, 10f, 10f, 10f };
            public ParameterTimeSeries<Kilogram> ERS_pick_up_weight_light_kg { get; } = new ParameterTimeSeries<Kilogram> { 50f, 47.549502495f, 45.2191037504402f, 43.0029177320644f, 40.8953468798615f, 38.8910679699573f, 36.985018669414f };
            public ParameterTimeSeries<Kilogram> ERS_pick_up_weight_heavy_kg { get; } = new ParameterTimeSeries<Kilogram> { 300f, 285.29701497f, 271.314622502641f, 258.017506392387f, 245.372081279169f, 233.346407819744f, 221.910112016484f };
            public ParameterTimeSeries<Dimensionless> ERS_standardization_risk_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.5f, 0.16384f, 0.0536870912000001f, 0.017592186044416f, 0.00576460752303425f, 0.00188894659314786f, 0.000618970019642692f };
            public ParameterTimeSeries<Dimensionless> ERS_cost_of_standard_change_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.3f, 0.3f, 0.3f, 0.3f, 0.3f, 0.3f, 0.3f };
            public ParameterTimeSeries<Dimensionless> ERS_utilization_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.431518849206349f, 0.431518849206349f, 0.431518849206349f, 0.431518849206349f, 0.431518849206349f, 0.431518849206349f, 0.431518849206349f };
            public ParameterTimeSeries<Dimensionless> ERS_electricity_price_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.690518572862225f, 0.690518572862225f, 0.690518572862225f, 0.690518572862225f, 0.690518572862225f, 0.690518572862225f, 0.690518572862225f };
            public ParameterTimeSeries<Euro> ERS_grid_connection_cable_euro { get; } = new ParameterTimeSeries<Euro> { 10000f, 10000f, 10000f, 10000f, 10000f, 10000f, 10000f };
            public ParameterTimeSeries<Kilometers> ERS_grid_connection_interval_km { get; } = new ParameterTimeSeries<Kilometers> { 20f, 20f, 20f, 20f, 20f, 20f, 20f };
            public ParameterTimeSeries<OtherUnit> ERS_reference_aadt { get; } = new ParameterTimeSeries<OtherUnit> { 2000f, 2000f, 2000f, 2000f, 2000f, 2000f, 2000f };
            public ParameterTimeSeries<Dimensionless> ERS_range_gain_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f };
            public ParameterTimeSeries<EuroPerKiloWatt> Station_hardware_cost_euro_per_kW { get; } = new ParameterTimeSeries<EuroPerKiloWatt> { 500f, 475.49502495f, 452.191037504402f, 430.029177320644f, 408.953468798615f, 388.910679699573f, 369.85018669414f };
            public ParameterTimeSeries<OtherUnit> Station_hardware_maintenance_ratio_per_year { get; } = new ParameterTimeSeries<OtherUnit> { 0.1f, 0.0975248753121875f, 0.0951110130465772f, 0.0927568968818328f, 0.0904610480274618f, 0.0882220242948802f, 0.0860384191914697f };
            public ParameterTimeSeries<Euro> Station_grid_connection_cable_euro { get; } = new ParameterTimeSeries<Euro> { 15000f, 15000f, 15000f, 15000f, 15000f, 15000f, 15000f };
            public ParameterTimeSeries<Years> Station_write_off_period_years { get; } = new ParameterTimeSeries<Years> { 15f, 15f, 15f, 15f, 15f, 15f, 15f };
            public ParameterTimeSeries<Dimensionless> Station_profit_margin_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f };
            public ParameterTimeSeries<Dimensionless> Station_utilization_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.431518849206349f, 0.431518849206349f, 0.431518849206349f, 0.431518849206349f, 0.431518849206349f, 0.431518849206349f, 0.431518849206349f };
            public ParameterTimeSeries<Dimensionless> Station_electricity_price_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.690518572862225f, 0.690518572862225f, 0.690518572862225f, 0.690518572862225f, 0.690518572862225f, 0.690518572862225f, 0.690518572862225f };
            public ParameterTimeSeries<OtherUnit> Station_reference_vehicles_per_day { get; } = new ParameterTimeSeries<OtherUnit> { 100f, 100f, 100f, 100f, 100f, 100f, 100f };
            public ParameterTimeSeries<EuroPerKiloWatt> Depot_hardware_cost_euro_per_kW { get; } = new ParameterTimeSeries<EuroPerKiloWatt> { 300f, 285.29701497f, 271.314622502641f, 258.017506392387f, 245.372081279169f, 233.346407819744f, 221.910112016484f };
            public ParameterTimeSeries<OtherUnit> Depot_hardware_maintenance_ratio_per_year { get; } = new ParameterTimeSeries<OtherUnit> { 0.1f, 0.0975248753121875f, 0.0951110130465772f, 0.0927568968818328f, 0.0904610480274618f, 0.0882220242948802f, 0.0860384191914697f };
            public ParameterTimeSeries<Euro> Depot_grid_connection_cable_euro { get; } = new ParameterTimeSeries<Euro> { 5000f, 5000f, 5000f, 5000f, 5000f, 5000f, 5000f };
            public ParameterTimeSeries<Years> Depot_write_off_period_years { get; } = new ParameterTimeSeries<Years> { 7f, 7f, 7f, 7f, 7f, 7f, 7f };
            public ParameterTimeSeries<Dimensionless> Depot_profit_margin_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f };
            public ParameterTimeSeries<Dimensionless> Depot_utilization_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.443717261904762f, 0.443717261904762f, 0.443717261904762f, 0.443717261904762f, 0.443717261904762f, 0.443717261904762f, 0.443717261904762f };
            public ParameterTimeSeries<Dimensionless> Depot_electricity_price_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.376429247057363f, 0.376429247057363f, 0.376429247057363f, 0.376429247057363f, 0.376429247057363f, 0.376429247057363f, 0.376429247057363f };
            public ParameterTimeSeries<OtherUnit> Depot_reference_vehicles_per_day { get; } = new ParameterTimeSeries<OtherUnit> { 20f, 20f, 20f, 20f, 20f, 20f, 20f };
            public ParameterTimeSeries<EuroPerKiloWatt> Destination_hardware_cost_euro_per_kW { get; } = new ParameterTimeSeries<EuroPerKiloWatt> { 500f, 475.49502495f, 452.191037504402f, 430.029177320644f, 408.953468798615f, 388.910679699573f, 369.85018669414f };
            public ParameterTimeSeries<OtherUnit> Destination_hardware_maintenance_ratio_per_year { get; } = new ParameterTimeSeries<OtherUnit> { 0.1f, 0.0975248753121875f, 0.0951110130465772f, 0.0927568968818328f, 0.0904610480274618f, 0.0882220242948802f, 0.0860384191914697f };
            public ParameterTimeSeries<Euro> Destination_grid_connection_cable_euro { get; } = new ParameterTimeSeries<Euro> { 5000f, 5000f, 5000f, 5000f, 5000f, 5000f, 5000f };
            public ParameterTimeSeries<Years> Destination_write_off_period_years { get; } = new ParameterTimeSeries<Years> { 10f, 10f, 10f, 10f, 10f, 10f, 10f };
            public ParameterTimeSeries<Dimensionless> Destination_profit_margin_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f };
            public ParameterTimeSeries<Dimensionless> Destination_utilization_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.268365079365079f, 0.268365079365079f, 0.268365079365079f, 0.268365079365079f, 0.268365079365079f, 0.268365079365079f, 0.268365079365079f };
            public ParameterTimeSeries<Dimensionless> Destination_electricity_price_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.824517819047154f, 0.824517819047154f, 0.824517819047154f, 0.824517819047154f, 0.824517819047154f, 0.824517819047154f, 0.824517819047154f };
            public ParameterTimeSeries<OtherUnit> Destination_reference_vehicles_per_day { get; } = new ParameterTimeSeries<OtherUnit> { 50f, 50f, 50f, 50f, 50f, 50f, 50f };
            public ParameterTimeSeries<Euro> Grid_initial_acccess_base_euro { get; } = new ParameterTimeSeries<Euro> { 5000f, 5000f, 5000f, 5000f, 5000f, 5000f, 5000f };
            public ParameterTimeSeries<EuroPerKiloWatt> Grid_initial_acccess_euro_per_kW { get; } = new ParameterTimeSeries<EuroPerKiloWatt> { 100f, 100f, 100f, 100f, 100f, 100f, 100f };
            public ParameterTimeSeries<EuroPerKiloWattHour> Grid_fee_at_100_percent_utilization_euro_per_kWh { get; } = new ParameterTimeSeries<EuroPerKiloWattHour> { 0.006f, 0.006f, 0.006f, 0.006f, 0.006f, 0.006f, 0.006f };
        }
        public class BatteryInternal
        {
            public ParameterTimeSeries<Dimensionless> Gross_SoC_window_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.65f, 0.683156532565f, 0.718004381517283f, 0.754629820990499f, 0.793123525966178f, 0.833580796761202f, 0.876101794966388f };
            public ParameterTimeSeries<Years> Gross_Calendar_lifetime_years { get; } = new ParameterTimeSeries<Years> { 15f, 16.5f, 18f, 19.5f, 21f, 22.5f, 24f };
            public ParameterTimeSeries<Dimensionless> Gross_Cycle_lifetime_cycles { get; } = new ParameterTimeSeries<Dimensionless> { 1500f, 1914.42234375f, 2443.34194016616f, 3118.39226911705f, 3979.94655771663f, 5079.53241134908f, 6482.91356272599f };
            public ParameterTimeSeries<CRate> Gross_Reference_charging_rate_c { get; } = new ParameterTimeSeries<CRate> { 1f, 1.2166529024f, 1.48024428491834f, 1.80094350550692f, 2.19112314303342f, 2.66583633148742f, 3.24339751002754f };
            public ParameterTimeSeries<CRate> Gross_Reference_discharging_rate_c { get; } = new ParameterTimeSeries<CRate> { 1.5f, 1.8249793536f, 2.22036642737752f, 2.70141525826038f, 3.28668471455013f, 3.99875449723114f, 4.86509626504132f };
            public ParameterTimeSeries<CRate> Gross_Max_permitted_charging_rate_c { get; } = new ParameterTimeSeries<CRate> { 1.5f, 1.8249793536f, 2.22036642737752f, 2.70141525826038f, 3.28668471455013f, 3.99875449723114f, 4.86509626504132f };
            public ParameterTimeSeries<CRate> Gross_Max_permitted_discharging_rate_c { get; } = new ParameterTimeSeries<CRate> { 2.25f, 2.7374690304f, 3.33054964106628f, 4.05212288739056f, 4.9300270718252f, 5.9981317458467f, 7.29764439756197f };
            public ParameterTimeSeries<KiloWattHoursPerKilogram> Gross_Specific_energy_kWh_per_kg { get; } = new ParameterTimeSeries<KiloWattHoursPerKilogram> { 0.2f, 0.23185481486f, 0.268783275868824f, 0.311593483320153f, 0.361222246933883f, 0.418755585930843f, 0.485452494237932f };
            public ParameterTimeSeries<KiloWattHoursPerLiter> Gross_Energy_density_kWh_per_liter { get; } = new ParameterTimeSeries<KiloWattHoursPerLiter> { 0.5f, 0.57963703715f, 0.671958189672061f, 0.778983708300382f, 0.903055617334706f, 1.04688896482711f, 1.21363123559483f };
            public ParameterTimeSeries<EuroPerKiloWattHour> Gross_Pack_cost_euro_per_kWh { get; } = new ParameterTimeSeries<EuroPerKiloWattHour> { 160f, 130.459631616f, 106.37322175864f, 86.7338207777454f, 70.7203894207052f, 57.6634746972829f, 47.0172229169129f };
            public ParameterTimeSeries<Dimensionless> Gross_Residual_value_if_recycled_this_year_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.00329608278917795f, 0.01422776195327f, 0.0547276571419069f, 0.15f, 0.245272342858093f, 0.28577223804673f, 0.296703917210822f };
            public ParameterTimeSeries<KilogramPerKiloWattHour> Gross_Production_emissions_kg_CO2_per_kWh { get; } = new ParameterTimeSeries<KilogramPerKiloWattHour> { 150f, 98.86222848f, 65.1582681335448f, 42.9446106105308f, 28.3039993744195f, 18.6546430203443f, 12.2949305366007f };
            public ParameterTimeSeries<Dimensionless> Gross_End_of_life_definition_ratio_of_gross { get; } = new ParameterTimeSeries<Dimensionless> { 0.8f, 0.8f, 0.8f, 0.8f, 0.8f, 0.8f, 0.8f };
            public ParameterTimeSeries<Dimensionless> Net_End_of_life_definition_ratio_of_net { get; } = new ParameterTimeSeries<Dimensionless> { 0.8f, 0.8f, 0.8f, 0.8f, 0.8f, 0.8f, 0.8f };
            public ParameterTimeSeries<Years> Net_Calendar_lifetime_years { get; } = new ParameterTimeSeries<Years> { 20.7692307692308f, 21.7373314784003f, 22.5625364092713f, 23.2564358203662f, 23.8298315221152f, 24.2927861086525f, 24.6546692680029f };
            public ParameterTimeSeries<Dimensionless> Net_Cycle_lifetime_cycles { get; } = new ParameterTimeSeries<Dimensionless> { 2307.69230769231f, 2802.31872563972f, 3402.96243736411f, 4132.34698971208f, 5018.0664517148f, 6093.62935312944f, 7399.72637879907f };
            public ParameterTimeSeries<CRate> Net_Reference_charging_rate_c { get; } = new ParameterTimeSeries<CRate> { 1.53846153846154f, 1.78092844670887f, 2.06160898599296f, 2.38652575794456f, 2.76265054723249f, 3.1980539161234f, 3.70207837566635f };
            public ParameterTimeSeries<CRate> Net_Reference_discharging_rate_c { get; } = new ParameterTimeSeries<CRate> { 2.30769230769231f, 2.6713926700633f, 3.09241347898943f, 3.57978863691684f, 4.14397582084873f, 4.7970808741851f, 5.55311756349953f };
            public ParameterTimeSeries<CRate> Net_Max_permitted_charging_rate_c { get; } = new ParameterTimeSeries<CRate> { 2.30769230769231f, 2.6713926700633f, 3.09241347898943f, 3.57978863691684f, 4.14397582084873f, 4.7970808741851f, 5.55311756349953f };
            public ParameterTimeSeries<CRate> Net_Max_permitted_discharging_rate_c { get; } = new ParameterTimeSeries<CRate> { 3.46153846153846f, 4.00708900509495f, 4.63862021848415f, 5.36968295537526f, 6.2159637312731f, 7.19562131127765f, 8.3296763452493f };
            public ParameterTimeSeries<KiloWattHoursPerKilogram> Net_Specific_energy_kWh_per_kg { get; } = new ParameterTimeSeries<KiloWattHoursPerKilogram> { 0.13f, 0.158393131378258f, 0.192987569752384f, 0.235137734539693f, 0.286493862145626f, 0.349066614968436f, 0.425305801572762f };
            public ParameterTimeSeries<KiloWattHoursPerLiter> Net_Energy_density_kWh_per_liter { get; } = new ParameterTimeSeries<KiloWattHoursPerLiter> { 0.325f, 0.395982828445644f, 0.482468924380961f, 0.587844336349232f, 0.716234655364066f, 0.872666537421089f, 1.06326450393191f };
            public ParameterTimeSeries<EuroPerKiloWattHour> Net_Pack_cost_euro_per_kWh { get; } = new ParameterTimeSeries<EuroPerKiloWattHour> { 246.153846153846f, 190.965943231447f, 148.151215364247f, 114.935586118107f, 89.1669293689833f, 69.1756275112488f, 53.6663926350211f };
            public ParameterTimeSeries<Dimensionless> Net_Residual_value_if_recycled_this_year_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.00507089659873531f, 0.0208265035538049f, 0.0762218985715055f, 0.198772955729626f, 0.309248603563113f, 0.342824881711612f, 0.338663747655265f };
            public ParameterTimeSeries<KilogramPerKiloWattHour> Net_Production_emissions_kg_CO2_per_kWh { get; } = new ParameterTimeSeries<KilogramPerKiloWattHour> { 230.769230769231f, 144.713874152398f, 90.7491232795164f, 56.9081812247537f, 35.6867479626704f, 22.3789260655057f, 14.0336780580074f };
        }
        /* REPLACE CODE UNTIL HERE */
    }
}
