using System;
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
            public override ParameterTimeSeries<KiloWattHoursPerKilometer> BEV_Energy_consumption_kWh_per_km { get; } = new ParameterTimeSeries<KiloWattHoursPerKilometer> { 1.45730611764706f, 1.4141084259708f, 1.37219120690325f, 1.33151650447876f, 1.29204748782823f, 1.25374841782885f, 1.21658461474236f };
            public override ParameterTimeSeries<Kilometers> BEV_Min_range_buffer_km { get; } = new ParameterTimeSeries<Kilometers> { 50f, 50f, 50f, 50f, 50f, 50f, 50f };
            public override ParameterTimeSeries<Years> BEV_Lifetime_years { get; } = new ParameterTimeSeries<Years> { 10f, 11.6666666666667f, 13.3333333333333f, 15f, 15f, 15f, 15f };
            public override ParameterTimeSeries<Euro> ICEV_Chassis_cost_euro { get; } = new ParameterTimeSeries<Euro> { 259200f, 262736.783864086f, 266453.979250271f, 270360.788959337f, 274466.885227394f, 278782.4336718f, 283318.118458564f };
            public override ParameterTimeSeries<Kilogram> BEV_Chassis_weight_excl_battery_kg { get; } = new ParameterTimeSeries<Kilogram> { 17780f, 17780f, 17780f, 17780f, 17780f, 17780f, 17780f };
            public override ParameterTimeSeries<Euro> BEV_Chassis_cost_excl_battery_euro { get; } = new ParameterTimeSeries<Euro> { 215794.56f, 213303.264692705f, 211051.331053471f, 209015.761403954f, 207175.767664421f, 205512.559057275f, 204009.150207859f };
            public override ParameterTimeSeries<KiloWattHours> BEV_Min_net_battery_capacity_kWh { get; } = new ParameterTimeSeries<KiloWattHours> { 216.666666666667f, 226.029954358822f, 205.48410135118f, 176.990560239856f, 148.80116058438f, 123.671320743104f, 102.21085008598f };
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
            public override ParameterTimeSeries<KiloWattHoursPerKilometer> BEV_Energy_consumption_kWh_per_km { get; } = new ParameterTimeSeries<KiloWattHoursPerKilometer> { 1.12420757647059f, 1.09088364289176f, 1.05854750246822f, 1.02716987488361f, 0.996722347753208f, 0.967177350896542f, 0.938508131372679f };
            public override ParameterTimeSeries<Kilometers> BEV_Min_range_buffer_km { get; } = new ParameterTimeSeries<Kilometers> { 50f, 50f, 50f, 50f, 50f, 50f, 50f };
            public override ParameterTimeSeries<Years> BEV_Lifetime_years { get; } = new ParameterTimeSeries<Years> { 10f, 11.6666666666667f, 13.3333333333333f, 15f, 15f, 15f, 15f };
            public override ParameterTimeSeries<Euro> ICEV_Chassis_cost_euro { get; } = new ParameterTimeSeries<Euro> { 235200f, 237794.607834718f, 240521.566745076f, 243387.627966071f, 246399.887113539f, 249565.801751033f, 252893.209852799f };
            public override ParameterTimeSeries<Kilogram> BEV_Chassis_weight_excl_battery_kg { get; } = new ParameterTimeSeries<Kilogram> { 16172f, 16172f, 16172f, 16172f, 16172f, 16172f, 16172f };
            public override ParameterTimeSeries<Euro> BEV_Chassis_cost_excl_battery_euro { get; } = new ParameterTimeSeries<Euro> { 204865.92f, 202893.36015395f, 201110.322286173f, 199498.597276008f, 198041.725520596f, 196724.828842609f, 195534.45854814f };
            public override ParameterTimeSeries<KiloWattHours> BEV_Min_net_battery_capacity_kWh { get; } = new ParameterTimeSeries<KiloWattHours> { 158.888888888889f, 165.755299863136f, 150.688340990866f, 129.793077509228f, 109.120851095212f, 90.692301878276f, 74.9546233963855f };
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
            public override ParameterTimeSeries<KiloWattHoursPerKilometer> BEV_Energy_consumption_kWh_per_km { get; } = new ParameterTimeSeries<KiloWattHoursPerKilometer> { 0.641214691764706f, 0.622207707427151f, 0.60376413103743f, 0.585867261970654f, 0.568500894644422f, 0.551649303844694f, 0.535297230486639f };
            public override ParameterTimeSeries<Kilometers> BEV_Min_range_buffer_km { get; } = new ParameterTimeSeries<Kilometers> { 30f, 30f, 30f, 30f, 30f, 30f, 30f };
            public override ParameterTimeSeries<Years> BEV_Lifetime_years { get; } = new ParameterTimeSeries<Years> { 7f, 8.66666666666667f, 10.3333333333333f, 12f, 12f, 12f, 12f };
            public override ParameterTimeSeries<Euro> ICEV_Chassis_cost_euro { get; } = new ParameterTimeSeries<Euro> { 148800f, 150216.88779801f, 151706.051113582f, 153271.176724488f, 154916.13947122f, 156645.011850076f, 158462.074095593f };
            public override ParameterTimeSeries<Kilogram> BEV_Chassis_weight_excl_battery_kg { get; } = new ParameterTimeSeries<Kilogram> { 7912f, 7912f, 7912f, 7912f, 7912f, 7912f, 7912f };
            public override ParameterTimeSeries<Euro> BEV_Chassis_cost_excl_battery_euro { get; } = new ParameterTimeSeries<Euro> { 134805.12f, 133480.979480506f, 132284.061327051f, 131202.142116074f, 130224.172840815f, 129340.166074278f, 128541.093973492f };
            public override ParameterTimeSeries<KiloWattHours> BEV_Min_net_battery_capacity_kWh { get; } = new ParameterTimeSeries<KiloWattHours> { 86.6666666666667f, 90.4119817435287f, 82.1936405404721f, 70.7962240959425f, 59.5204642337518f, 49.4685282972415f, 40.8843400343921f };
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
            public override ParameterTimeSeries<KiloWattHoursPerKilometer> BEV_Energy_consumption_kWh_per_km { get; } = new ParameterTimeSeries<KiloWattHoursPerKilometer> { 0.466337957647059f, 0.452514696310655f, 0.43910118620904f, 0.426085281433203f, 0.413455196105034f, 0.401199493705232f, 0.389307076717556f };
            public override ParameterTimeSeries<Kilometers> BEV_Min_range_buffer_km { get; } = new ParameterTimeSeries<Kilometers> { 20f, 20f, 20f, 20f, 20f, 20f, 20f };
            public override ParameterTimeSeries<Years> BEV_Lifetime_years { get; } = new ParameterTimeSeries<Years> { 7f, 8.66666666666667f, 10.3333333333333f, 12f, 12f, 12f, 12f };
            public override ParameterTimeSeries<Euro> ICEV_Chassis_cost_euro { get; } = new ParameterTimeSeries<Euro> { 91200f, 91957.3645774527f, 92753.3623599453f, 93589.9640292023f, 94469.2407915219f, 95393.3695055391f, 96364.6380715572f };
            public override ParameterTimeSeries<Kilogram> BEV_Chassis_weight_excl_battery_kg { get; } = new ParameterTimeSeries<Kilogram> { 3886.4f, 3886.4f, 3886.4f, 3886.4f, 3886.4f, 3886.4f, 3886.4f };
            public override ParameterTimeSeries<Euro> BEV_Chassis_cost_excl_battery_euro { get; } = new ParameterTimeSeries<Euro> { 86355.072f, 85394.0463033778f, 84525.3551899418f, 83740.1272265117f, 83030.3433401382f, 82388.7549240118f, 81808.8098116891f };
            public override ParameterTimeSeries<KiloWattHours> BEV_Min_net_battery_capacity_kWh { get; } = new ParameterTimeSeries<KiloWattHours> { 46.2222222222222f, 48.2197235965486f, 43.8366082882518f, 37.7579861845027f, 31.7442475913343f, 26.3832150918621f, 21.8049813516758f };
        }
        public class WorldInternal
        {
            public ParameterTimeSeries<OtherUnit> Economy_Public_sector_interest_rate_percent { get; } = new ParameterTimeSeries<OtherUnit> { 0.02f, 0.02f, 0.02f, 0.02f, 0.02f, 0.02f, 0.02f };
            public ParameterTimeSeries<OtherUnit> Economy_Private_sector_interest_public_charging_and_trucks_percent { get; } = new ParameterTimeSeries<OtherUnit> { 0.03f, 0.03f, 0.03f, 0.03f, 0.03f, 0.03f, 0.03f };
            public ParameterTimeSeries<OtherUnit> Economy_Private_sector_interest_depot_charging_percent { get; } = new ParameterTimeSeries<OtherUnit> { 0.07f, 0.07f, 0.07f, 0.07f, 0.07f, 0.07f, 0.07f };
            public ParameterTimeSeries<Dimensionless> Economy_Heavy_traffic_volume_vs_2020_percent { get; } = new ParameterTimeSeries<Dimensionless> { 2.5f, 2.66678028294073f, 2.84468683099258f, 3.03446190081287f, 3.23689726656904f, 3.45283752335643f, 3.68318361099394f };
            public ParameterTimeSeries<Dimensionless> Economy_Logistic_BEV_penalty_percent { get; } = new ParameterTimeSeries<Dimensionless> { 0.15f, 0.125f, 0.1f, 0.075f, 0.05f, 0.025f, 0f };
            public ParameterTimeSeries<Hours> Charging_Extra_stop_overhead_h { get; } = new ParameterTimeSeries<Hours> { 0.25f, 0.214683506425f, 0.184356031723732f, 0.158312797284197f, 0.135948585731687f, 0.116743676313593f, 0.100251767135789f };
            public ParameterTimeSeries<EuroPerKilogram> CO2_SCC_euro_per_kg { get; } = new ParameterTimeSeries<EuroPerKilogram> { 0.08f, 0.165f, 0.25f, 0.3875f, 0.525f, 0.6625f, 0.8f };
            public ParameterTimeSeries<Dimensionless> CO2_Tax_ratio_of_SCC { get; } = new ParameterTimeSeries<Dimensionless> { 0.7f, 0.75f, 0.8f, 0.85f, 0.9f, 0.95f, 1f };
            public ParameterTimeSeries<EuroPerLiter> FossilDiesel_Price_euro_per_liter { get; } = new ParameterTimeSeries<EuroPerLiter> { 0.6f, 0.636874430357299f, 0.676015066738224f, 0.71756118423643f, 0.761660617428475f, 0.808470286417248f, 0.858156755204646f };
            public ParameterTimeSeries<EuroPerLiter> RenewableDiesel_Price_euro_per_liter { get; } = new ParameterTimeSeries<EuroPerLiter> { 1.5f, 1.5f, 1.59218607589325f, 1.69003766684556f, 1.79390296059107f, 1.90415154357119f, 2.02117571604312f };
            public ParameterTimeSeries<LiterPerYear> RenewableDiesel_Supply_cap_liter_per_year { get; } = new ParameterTimeSeries<LiterPerYear> { 175000000f, 193214140.56f, 213324023.499083f, 235526959.206723f, 260040794.296212f, 287106049.031328f, 316988277.218087f };
            public ParameterTimeSeries<Dimensionless> RenewableDiesel_Blend_guess_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.05f, 0.06f, 0.08f, 0.2881215375f, 0.449162015900691f, 0.573772068253027f, 0.670192951384141f };
            public ParameterTimeSeries<KilogramPerLiter> FossilDiesel_Emissions_kg_CO2_per_liter { get; } = new ParameterTimeSeries<KilogramPerLiter> { 3.23f, 3.23f, 3.23f, 3.23f, 3.23f, 3.23f, 3.23f };
            public ParameterTimeSeries<KilogramPerLiter> RenewableDiesel_Emissions_kg_CO2_per_liter { get; } = new ParameterTimeSeries<KilogramPerLiter> { 0.67f, 0.518433228125f, 0.401153749289714f, 0.310405124207035f, 0.240185568013723f, 0.185851013991629f, 0.143807971841768f };
            public ParameterTimeSeries<EuroPerLiter> Diesel_Price_euro_per_liter { get; } = new ParameterTimeSeries<EuroPerLiter> { 0.645f, 0.688661964535861f, 0.749308747470626f, 0.997752603588364f, 1.22530466918144f, 1.43714158748054f, 1.6376038650847f };
            public ParameterTimeSeries<KilogramPerLiter> Diesel_Emissions_kg_CO2_per_liter { get; } = new ParameterTimeSeries<KilogramPerLiter> { 3.102f, 3.0673059936875f, 3.00369229994318f, 2.38880183550941f, 1.88708892256006f, 1.48335234022762f, 1.16165585611043f };
            public ParameterTimeSeries<EuroPerLiter> Diesel_CO2_tax_euro_per_liter { get; } = new ParameterTimeSeries<EuroPerLiter> { 0.173712f, 0.379579116718828f, 0.600738459988635f, 0.786811604570912f, 0.89164951590963f, 0.93358487913076f, 0.929324684888341f };
            public ParameterTimeSeries<EuroPerLiter> Diesel_Pollution_tax_euro_per_liter { get; } = new ParameterTimeSeries<EuroPerLiter> { 0.02f, 0.0322102f, 0.051874849202f, 0.0835449633883131f, 0.134549998986512f, 0.216694118867768f, 0.348988045377729f };
            public ParameterTimeSeries<EuroPerLiter> Diesel_Road_tax_euro_per_liter { get; } = new ParameterTimeSeries<EuroPerLiter> { 0.4f, 0.4f, 0.4f, 0.4f, 0.4f, 0.4f, 0.4f };
            public ParameterTimeSeries<KiloWattHoursPerLiter> Electricity_Conversion_ratio_kWh_per_liter { get; } = new ParameterTimeSeries<KiloWattHoursPerLiter> { 4.16373176470588f, 4.16373176470588f, 4.16373176470588f, 4.16373176470588f, 4.16373176470588f, 4.16373176470588f, 4.16373176470588f };
            public ParameterTimeSeries<EuroPerKiloWattHour> Electricity_Road_tax_euro_per_kWh { get; } = new ParameterTimeSeries<EuroPerKiloWattHour> { 0.096067667804786f, 0.096067667804786f, 0.096067667804786f, 0.096067667804786f, 0.096067667804786f, 0.096067667804786f, 0.096067667804786f };
            public ParameterTimeSeries<EuroPerKiloWattHour> SE12_Price_min_euro_per_kWh { get; } = new ParameterTimeSeries<EuroPerKiloWattHour> { 0.0712952876712329f, 0.0712952876712329f, 0.0712952876712329f, 0.0712952876712329f, 0.0712952876712329f, 0.0712952876712329f, 0.0712952876712329f };
            public ParameterTimeSeries<EuroPerKiloWattHour> SE12_Price_max_euro_per_kWh { get; } = new ParameterTimeSeries<EuroPerKiloWattHour> { 0.125255397260274f, 0.125255397260274f, 0.125255397260274f, 0.125255397260274f, 0.125255397260274f, 0.125255397260274f, 0.125255397260274f };
            public ParameterTimeSeries<KilogramPerKiloWattHour> SE12_CO2_emissions_kg_per_kWh { get; } = new ParameterTimeSeries<KilogramPerKiloWattHour> { 0.265f, 0.174656603648f, 0.115112940369263f, 0.0758688120786044f, 0.0500037322281411f, 0.0329565360026082f, 0.0217210439479946f };
            public ParameterTimeSeries<EuroPerKiloWattHour> SE12_CO2_tax_euro_per_kWh { get; } = new ParameterTimeSeries<EuroPerKiloWattHour> { 0.01484f, 0.02161375470144f, 0.0230225880738525f, 0.0249892899783903f, 0.0236267634777967f, 0.0207420198466415f, 0.0173768351583957f };
            public ParameterTimeSeries<EuroPerKiloWattHour> SE34_Price_min_euro_per_kWh { get; } = new ParameterTimeSeries<EuroPerKiloWattHour> { 0.0712952876712329f, 0.0712952876712329f, 0.0712952876712329f, 0.0712952876712329f, 0.0712952876712329f, 0.0712952876712329f, 0.0712952876712329f };
            public ParameterTimeSeries<EuroPerKiloWattHour> SE34_Price_max_euro_per_kWh { get; } = new ParameterTimeSeries<EuroPerKiloWattHour> { 0.125255397260274f, 0.125255397260274f, 0.125255397260274f, 0.125255397260274f, 0.125255397260274f, 0.125255397260274f, 0.125255397260274f };
            public ParameterTimeSeries<KilogramPerKiloWattHour> SE34_CO2_emissions_kg_per_kWh { get; } = new ParameterTimeSeries<KilogramPerKiloWattHour> { 0.265f, 0.174656603648f, 0.115112940369263f, 0.0758688120786044f, 0.0500037322281411f, 0.0329565360026082f, 0.0217210439479946f };
            public ParameterTimeSeries<EuroPerKiloWattHour> SE34_CO2_tax_euro_per_kWh { get; } = new ParameterTimeSeries<EuroPerKiloWattHour> { 0.01484f, 0.02161375470144f, 0.0230225880738525f, 0.0249892899783903f, 0.0236267634777967f, 0.0207420198466415f, 0.0173768351583957f };
            public ParameterTimeSeries<EuroPerKiloWattHour> OtherRegion_Price_min_euro_per_kWh { get; } = new ParameterTimeSeries<EuroPerKiloWattHour> { 0.0712952876712329f, 0.0712952876712329f, 0.0712952876712329f, 0.0712952876712329f, 0.0712952876712329f, 0.0712952876712329f, 0.0712952876712329f };
            public ParameterTimeSeries<EuroPerKiloWattHour> OtherRegion_Price_max_euro_per_kWh { get; } = new ParameterTimeSeries<EuroPerKiloWattHour> { 0.125255397260274f, 0.125255397260274f, 0.125255397260274f, 0.125255397260274f, 0.125255397260274f, 0.125255397260274f, 0.125255397260274f };
            public ParameterTimeSeries<KilogramPerKiloWattHour> OtherRegion_CO2_emissions_kg_per_kWh { get; } = new ParameterTimeSeries<KilogramPerKiloWattHour> { 0.265f, 0.174656603648f, 0.115112940369263f, 0.0758688120786044f, 0.0500037322281411f, 0.0329565360026082f, 0.0217210439479946f };
            public ParameterTimeSeries<EuroPerKiloWattHour> OtherRegion_CO2_tax_euro_per_kWh { get; } = new ParameterTimeSeries<EuroPerKiloWattHour> { 0.01484f, 0.02161375470144f, 0.0230225880738525f, 0.0249892899783903f, 0.0236267634777967f, 0.0207420198466415f, 0.0173768351583957f };
        }
        public class InfrastructureInternal
        {
            public ParameterTimeSeries<EuroPerKilometer> ERS_base_cost_euro_per_km { get; } = new ParameterTimeSeries<EuroPerKilometer> { 1500000f, 1426485.07485f, 1356573.11251321f, 1290087.53196193f, 1226860.40639585f, 1166732.03909872f, 1109550.56008242f };
            public ParameterTimeSeries<EuroPerKiloWattKilometer> ERS_power_cost_euro_per_kW_km { get; } = new ParameterTimeSeries<EuroPerKiloWattKilometer> { 700f, 665.69303493f, 633.067452506163f, 602.040848248902f, 572.534856318061f, 544.474951579403f, 517.790261371796f };
            public ParameterTimeSeries<Dimensionless> ERS_maintenance_cost_ratio_per_year { get; } = new ParameterTimeSeries<Dimensionless> { 0.02f, 0.02f, 0.02f, 0.02f, 0.02f, 0.02f, 0.02f };
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
            public ParameterTimeSeries<Dimensionless> ERS_utilization_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.265315476190476f, 0.265315476190476f, 0.265315476190476f, 0.265315476190476f, 0.265315476190476f, 0.265315476190476f, 0.265315476190476f };
            public ParameterTimeSeries<Dimensionless> ERS_electricity_price_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.726044759755607f, 0.726044759755607f, 0.726044759755607f, 0.726044759755607f, 0.726044759755607f, 0.726044759755607f, 0.726044759755607f };
            public ParameterTimeSeries<Euro> ERS_grid_connection_cable_euro { get; } = new ParameterTimeSeries<Euro> { 10000f, 10000f, 10000f, 10000f, 10000f, 10000f, 10000f };
            public ParameterTimeSeries<Kilometers> ERS_grid_connection_interval_km { get; } = new ParameterTimeSeries<Kilometers> { 10f, 10f, 10f, 10f, 10f, 10f, 10f };
            public ParameterTimeSeries<OtherUnit> ERS_reference_aadt { get; } = new ParameterTimeSeries<OtherUnit> { 2000f, 2000f, 2000f, 2000f, 2000f, 2000f, 2000f };
            public ParameterTimeSeries<EuroPerKiloWatt> Rest_Stop_hardware_cost_euro_per_kW { get; } = new ParameterTimeSeries<EuroPerKiloWatt> { 300f, 285.29701497f, 271.314622502641f, 258.017506392387f, 245.372081279169f, 233.346407819744f, 221.910112016484f };
            public ParameterTimeSeries<OtherUnit> Rest_Stop_hardware_maintenance_ratio_per_year { get; } = new ParameterTimeSeries<OtherUnit> { 0.1f, 0.0975248753121875f, 0.0951110130465772f, 0.0927568968818328f, 0.0904610480274618f, 0.0882220242948802f, 0.0860384191914697f };
            public ParameterTimeSeries<Euro> Rest_Stop_grid_connection_cable_euro { get; } = new ParameterTimeSeries<Euro> { 15000f, 15000f, 15000f, 15000f, 15000f, 15000f, 15000f };
            public ParameterTimeSeries<Years> Rest_Stop_write_off_period_years { get; } = new ParameterTimeSeries<Years> { 15f, 15f, 15f, 15f, 15f, 15f, 15f };
            public ParameterTimeSeries<Dimensionless> Rest_Stop_profit_margin_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f };
            public ParameterTimeSeries<Dimensionless> Rest_Stop_utilization_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.198986607142857f, 0.198986607142857f, 0.198986607142857f, 0.198986607142857f, 0.198986607142857f, 0.198986607142857f, 0.198986607142857f };
            public ParameterTimeSeries<Dimensionless> Rest_Stop_electricity_price_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.726044759755607f, 0.726044759755607f, 0.726044759755607f, 0.726044759755607f, 0.726044759755607f, 0.726044759755607f, 0.726044759755607f };
            public ParameterTimeSeries<OtherUnit> Rest_Stop_reference_vehicles_per_day { get; } = new ParameterTimeSeries<OtherUnit> { 100f, 100f, 100f, 100f, 100f, 100f, 100f };
            public ParameterTimeSeries<EuroPerKiloWatt> Depot_hardware_cost_euro_per_kW { get; } = new ParameterTimeSeries<EuroPerKiloWatt> { 300f, 285.29701497f, 271.314622502641f, 258.017506392387f, 245.372081279169f, 233.346407819744f, 221.910112016484f };
            public ParameterTimeSeries<OtherUnit> Depot_hardware_maintenance_ratio_per_year { get; } = new ParameterTimeSeries<OtherUnit> { 0.1f, 0.0975248753121875f, 0.0951110130465772f, 0.0927568968818328f, 0.0904610480274618f, 0.0882220242948802f, 0.0860384191914697f };
            public ParameterTimeSeries<Euro> Depot_grid_connection_cable_euro { get; } = new ParameterTimeSeries<Euro> { 5000f, 5000f, 5000f, 5000f, 5000f, 5000f, 5000f };
            public ParameterTimeSeries<Years> Depot_write_off_period_years { get; } = new ParameterTimeSeries<Years> { 7f, 7f, 7f, 7f, 7f, 7f, 7f };
            public ParameterTimeSeries<Dimensionless> Depot_profit_margin_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f };
            public ParameterTimeSeries<Dimensionless> Depot_utilization_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.341320970695971f, 0.341320970695971f, 0.341320970695971f, 0.341320970695971f, 0.341320970695971f, 0.341320970695971f, 0.341320970695971f };
            public ParameterTimeSeries<Dimensionless> Depot_electricity_price_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.376429247057363f, 0.376429247057363f, 0.376429247057363f, 0.376429247057363f, 0.376429247057363f, 0.376429247057363f, 0.376429247057363f };
            public ParameterTimeSeries<OtherUnit> Depot_reference_vehicles_per_day { get; } = new ParameterTimeSeries<OtherUnit> { 20f, 20f, 20f, 20f, 20f, 20f, 20f };
            public ParameterTimeSeries<EuroPerKiloWatt> Destination_hardware_cost_euro_per_kW { get; } = new ParameterTimeSeries<EuroPerKiloWatt> { 300f, 285.29701497f, 271.314622502641f, 258.017506392387f, 245.372081279169f, 233.346407819744f, 221.910112016484f };
            public ParameterTimeSeries<OtherUnit> Destination_hardware_maintenance_ratio_per_year { get; } = new ParameterTimeSeries<OtherUnit> { 0.1f, 0.0975248753121875f, 0.0951110130465772f, 0.0927568968818328f, 0.0904610480274618f, 0.0882220242948802f, 0.0860384191914697f };
            public ParameterTimeSeries<Euro> Destination_grid_connection_cable_euro { get; } = new ParameterTimeSeries<Euro> { 5000f, 5000f, 5000f, 5000f, 5000f, 5000f, 5000f };
            public ParameterTimeSeries<Years> Destination_write_off_period_years { get; } = new ParameterTimeSeries<Years> { 10f, 10f, 10f, 10f, 10f, 10f, 10f };
            public ParameterTimeSeries<Dimensionless> Destination_profit_margin_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f };
            public ParameterTimeSeries<Dimensionless> Destination_utilization_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.13418253968254f, 0.13418253968254f, 0.13418253968254f, 0.13418253968254f, 0.13418253968254f, 0.13418253968254f, 0.13418253968254f };
            public ParameterTimeSeries<Dimensionless> Destination_electricity_price_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.824517819047154f, 0.824517819047154f, 0.824517819047154f, 0.824517819047154f, 0.824517819047154f, 0.824517819047154f, 0.824517819047154f };
            public ParameterTimeSeries<OtherUnit> Destination_reference_vehicles_per_day { get; } = new ParameterTimeSeries<OtherUnit> { 50f, 50f, 50f, 50f, 50f, 50f, 50f };
            public ParameterTimeSeries<Euro> Grid_initial_acccess_base_euro { get; } = new ParameterTimeSeries<Euro> { 5000f, 5000f, 5000f, 5000f, 5000f, 5000f, 5000f };
            public ParameterTimeSeries<EuroPerKiloWatt> Grid_initial_acccess_euro_per_kW { get; } = new ParameterTimeSeries<EuroPerKiloWatt> { 300f, 300f, 300f, 300f, 300f, 300f, 300f };
            public ParameterTimeSeries<EuroPerKiloWattHour> Grid_fee_at_10_percent_utilization_euro_per_kWh { get; } = new ParameterTimeSeries<EuroPerKiloWattHour> { 0.06f, 0.06f, 0.06f, 0.06f, 0.06f, 0.06f, 0.06f };
        }
        public class BatteryInternal
        {
            public ParameterTimeSeries<Dimensionless> Gross_SoC_window_ratio { get; } = new ParameterTimeSeries<Dimensionless> { 0.65f, 0.825f, 0.9125f, 0.95625f, 0.978125f, 0.9890625f, 0.99453125f };
            public ParameterTimeSeries<Years> Gross_Calendar_lifetime_years { get; } = new ParameterTimeSeries<Years> { 15f, 16.5f, 18f, 19.5f, 21f, 22.5f, 24f };
            public ParameterTimeSeries<Dimensionless> Gross_Cycle_lifetime_cycles { get; } = new ParameterTimeSeries<Dimensionless> { 1500f, 2007.3383664f, 2686.27154481428f, 3594.83728964954f, 4810.70320831927f, 6437.80607961524f, 8615.23675936989f };
            public ParameterTimeSeries<CRate> Gross_Reference_charging_rate_c { get; } = new ParameterTimeSeries<CRate> { 1f, 1.2166529024f, 1.48024428491834f, 1.80094350550692f, 2.19112314303342f, 2.66583633148742f, 3.24339751002754f };
            public ParameterTimeSeries<CRate> Gross_Reference_discharging_rate_c { get; } = new ParameterTimeSeries<CRate> { 1.5f, 1.8249793536f, 2.22036642737752f, 2.70141525826038f, 3.28668471455013f, 3.99875449723114f, 4.86509626504132f };
            public ParameterTimeSeries<CRate> Gross_Max_permitted_charging_rate_c { get; } = new ParameterTimeSeries<CRate> { 1.5f, 1.8249793536f, 2.22036642737752f, 2.70141525826038f, 3.28668471455013f, 3.99875449723114f, 4.86509626504132f };
            public ParameterTimeSeries<CRate> Gross_Max_permitted_discharging_rate_c { get; } = new ParameterTimeSeries<CRate> { 2.25f, 2.7374690304f, 3.33054964106628f, 4.05212288739056f, 4.9300270718252f, 5.9981317458467f, 7.29764439756197f };
            public ParameterTimeSeries<KiloWattHoursPerKilogram> Gross_Specific_energy_kWh_per_kg { get; } = new ParameterTimeSeries<KiloWattHoursPerKilogram> { 0.2f, 0.23185481486f, 0.268783275868824f, 0.311593483320153f, 0.361222246933883f, 0.418755585930843f, 0.485452494237932f };
            public ParameterTimeSeries<KiloWattHoursPerLiter> Gross_Energy_density_kWh_per_liter { get; } = new ParameterTimeSeries<KiloWattHoursPerLiter> { 0.5f, 0.57963703715f, 0.671958189672061f, 0.778983708300382f, 0.903055617334706f, 1.04688896482711f, 1.21363123559483f };
            public ParameterTimeSeries<EuroPerKiloWattHour> Gross_Pack_cost_euro_per_kWh { get; } = new ParameterTimeSeries<EuroPerKiloWattHour> { 270f, 206.136965376f, 159.267627382776f, 124.870031702324f, 99.6254978715508f, 81.0984329495338f, 67.5013454799995f };
            public ParameterTimeSeries<KilogramPerKiloWattHour> Gross_Production_emissions_kg_CO2_per_kWh { get; } = new ParameterTimeSeries<KilogramPerKiloWattHour> { 150f, 98.86222848f, 65.1582681335448f, 42.9446106105308f, 28.3039993744195f, 18.6546430203443f, 12.2949305366007f };
            public ParameterTimeSeries<Dimensionless> Gross_End_of_life_definition_ratio_of_gross { get; } = new ParameterTimeSeries<Dimensionless> { 0.8f, 0.8f, 0.8f, 0.8f, 0.8f, 0.8f, 0.8f };
            public ParameterTimeSeries<Dimensionless> Net_End_of_life_definition_ratio_of_net { get; } = new ParameterTimeSeries<Dimensionless> { 0.8f, 0.8f, 0.8f, 0.8f, 0.8f, 0.8f, 0.8f };
            public ParameterTimeSeries<Years> Net_Calendar_lifetime_years { get; } = new ParameterTimeSeries<Years> { 20.7692307692308f, 18f, 17.7534246575342f, 18.3529411764706f, 19.3226837060703f, 20.4739336492891f, 21.7187745483111f };
            public ParameterTimeSeries<Dimensionless> Net_Cycle_lifetime_cycles { get; } = new ParameterTimeSeries<Dimensionless> { 2307.69230769231f, 2433.13741381818f, 2943.85922719373f, 3759.30696956815f, 4918.29082000693f, 6508.99824795221f, 8662.61041004985f };
            public ParameterTimeSeries<CRate> Net_Reference_charging_rate_c { get; } = new ParameterTimeSeries<CRate> { 1.53846153846154f, 1.47473079078788f, 1.62218551771873f, 1.883339613602f, 2.24012589703097f, 2.69531635411051f, 3.26123237457601f };
            public ParameterTimeSeries<CRate> Net_Reference_discharging_rate_c { get; } = new ParameterTimeSeries<CRate> { 2.30769230769231f, 2.21209618618182f, 2.4332782765781f, 2.82500942040301f, 3.36018884554646f, 4.04297453116576f, 4.89184856186401f };
            public ParameterTimeSeries<CRate> Net_Max_permitted_charging_rate_c { get; } = new ParameterTimeSeries<CRate> { 2.30769230769231f, 2.21209618618182f, 2.4332782765781f, 2.82500942040301f, 3.36018884554646f, 4.04297453116576f, 4.89184856186401f };
            public ParameterTimeSeries<CRate> Net_Max_permitted_discharging_rate_c { get; } = new ParameterTimeSeries<CRate> { 3.46153846153846f, 3.31814427927273f, 3.64991741486715f, 4.23751413060451f, 5.04028326831969f, 6.06446179674864f, 7.33777284279602f };
            public ParameterTimeSeries<KiloWattHoursPerKilogram> Net_Specific_energy_kWh_per_kg { get; } = new ParameterTimeSeries<KiloWattHoursPerKilogram> { 0.13f, 0.1912802222595f, 0.245264739230302f, 0.297961268424896f, 0.353320510282204f, 0.414175446709724f, 0.482797675910068f };
            public ParameterTimeSeries<KiloWattHoursPerLiter> Net_Energy_density_kWh_per_liter { get; } = new ParameterTimeSeries<KiloWattHoursPerLiter> { 0.325f, 0.47820055564875f, 0.613161848075755f, 0.74490317106224f, 0.88330127570551f, 1.03543861677431f, 1.20699418977517f };
            public ParameterTimeSeries<EuroPerKiloWattHour> Net_Pack_cost_euro_per_kWh { get; } = new ParameterTimeSeries<EuroPerKiloWattHour> { 415.384615384615f, 249.862988334545f, 174.53986562496f, 130.583039688704f, 101.853544149828f, 81.9952560627198f, 67.8725233420263f };
            public ParameterTimeSeries<KilogramPerKiloWattHour> Net_Production_emissions_kg_CO2_per_kWh { get; } = new ParameterTimeSeries<KilogramPerKiloWattHour> { 230.769230769231f, 119.833004218182f, 71.4063212422409f, 44.9093967168949f, 28.9369961655407f, 18.8609344913433f, 12.3625381672026f };
        }
        /* REPLACE CODE UNTIL HERE */
    }
}
