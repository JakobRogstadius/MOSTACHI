using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

namespace ScoreInfrastructurePlan
{
    internal class Experiments
    {
        #region Support methods
        private static BatteryOffers GetDefaultBatteryOffers()
        {
            return new BatteryOffers {
                    { Parameters.MGV16, new float[] { 75, 150, 250, 450, 700 }.Select(n => new KiloWattHours(n)).ToArray() },
                    { Parameters.MGV24, new float[] { 150, 250, 450, 700, 1000 }.Select(n => new KiloWattHours(n)).ToArray() },
                    { Parameters.HGV40, new float[] { 150, 250, 450, 700, 1000, 1500 }.Select(n => new KiloWattHours(n)).ToArray() },
                    { Parameters.HGV60, new float[] { 250, 450, 700, 1000, 1500 }.Select(n => new KiloWattHours(n)).ToArray() }
            };
        }

        private static BatteryOffers GetLargeBatteryOffers()
        {
            return new BatteryOffers {
                    { Parameters.MGV16, new float[] { 75, 150, 250, 450, 700 }.Select(n => new KiloWattHours(n)).ToArray() },
                    { Parameters.MGV24, new float[] { 150, 250, 450, 700, 1000 }.Select(n => new KiloWattHours(n)).ToArray() },
                    { Parameters.HGV40, new float[] { 150, 250, 450, 700, 1000, 1500 }.Select(n => new KiloWattHours(n)).ToArray() },
                    { Parameters.HGV60, new float[] { 250, 450, 700, 1000, 1500 }.Select(n => new KiloWattHours(n)).ToArray() }
            };
        }

        private static Dictionary<RouteSegmentType, KiloWatts> GetDefaultInfrastructurePower()
        {
            return new Dictionary<RouteSegmentType, KiloWatts>()
            {
                { RouteSegmentType.Depot, new KiloWatts(150) },
                { RouteSegmentType.Destination, new KiloWatts(500) },
                { RouteSegmentType.RestStop, new KiloWatts(1500) },
                { RouteSegmentType.Road, new KiloWatts(375) }
            };
        }

        private static List<ChargingStrategy> GetDefaultChargingStrategies(bool forcedErsUse = false)
        {
            if (forcedErsUse)
            {
                return new List<ChargingStrategy>() {
                    ChargingStrategy.NA_Diesel,
                    ChargingStrategy.DepotAndErsCharging,
                    ChargingStrategy.AllPlannedStopsAndErs,
                    ChargingStrategy.Ers
                };
            }
            else
            {
                return new List<ChargingStrategy>() {
                    ChargingStrategy.NA_Diesel,
                    ChargingStrategy.Depot,
                    //ChargingStrategy.DepotAndErsPropulsion,
                    ChargingStrategy.DepotAndErsCharging,
                    ChargingStrategy.AllPlannedStops,
                    ChargingStrategy.AllPlannedStopsAndErs,
                };
            }
        }
        #endregion

        public static Scenario GetTestScenario(Dimensionless sampleRatio)
        {
            ModelYear y0 = ModelYear.Y2030;
            ModelYear y1 = ModelYear.Y2030;

            var scenario = new Scenario()
            {
                Name = "Test scenario",
                MovementSampleRatio = sampleRatio,
                SimStartYear = y0,
                SimEndYear = y1,
                DepotBuildYear = (y0, y1, new Dimensionless(.7f)),
                DestinationBuildYear = (y0, y1, new Dimensionless(.4f)),
                StationBuildYear = (y0, y1, new Dimensionless(1f)),
                ErsBuildYear = (y0, y1),
                BatteryOffers = GetDefaultBatteryOffers(),
                InfraOffers = new InfraOffers()
                {
                    AvailablePowerPerUser_kW = GetDefaultInfrastructurePower(),
                    ErsReferenceSpeed_kmph = new KilometersPerHour(85),
                    ErsCoverageRatio = new Dimensionless(1f),
                    FinalErsNetworkScope_km = new Kilometers(2000)
                },
                ChargingStrategies = GetDefaultChargingStrategies()
            };
            scenario.InfraOffers.AvailablePowerPerUser_kW[RouteSegmentType.Road] = new KiloWatts(200);
            return scenario;
        }

        public static Scenario Custom(Dimensionless sampleRatio, string scenarioName, (ModelYear start, ModelYear end) simulationPeriod, (Kilometers km, KiloWatts kW, Dimensionless density) ersConfig,
            BuildPeriod depot, BuildPeriod destination, (ModelYear from, ModelYear to) ers, BuildPeriod station, bool forcedErsUse = false, KiloWatts restStopkW = null)
        {
            var s = new Scenario()
            {
                Name = scenarioName,
                MovementSampleRatio = sampleRatio,
                SimStartYear = simulationPeriod.start,
                SimEndYear = simulationPeriod.end,
                DepotBuildYear = depot,
                DestinationBuildYear = destination,
                ErsBuildYear = ers,
                StationBuildYear = station,
                BatteryOffers = GetDefaultBatteryOffers(),
                InfraOffers = new InfraOffers()
                {
                    AvailablePowerPerUser_kW = GetDefaultInfrastructurePower(),
                    ErsReferenceSpeed_kmph = new KilometersPerHour(85),
                    ErsCoverageRatio = ersConfig.density,
                    FinalErsNetworkScope_km = ersConfig.km
                },
                ChargingStrategies = GetDefaultChargingStrategies(forcedErsUse: forcedErsUse)
            };
            s.InfraOffers.AvailablePowerPerUser_kW[RouteSegmentType.Road] = ersConfig.kW;
            if (!object.ReferenceEquals(null, restStopkW))
                s.InfraOffers.AvailablePowerPerUser_kW[RouteSegmentType.RestStop] = restStopkW;
            return s;
        }

        public static List<Scenario> Q1_AllDieselVsAllElectric(Dimensionless sampleRatio)
        {
            List<Scenario> scenarios = new List<Scenario>();

            scenarios.Add(new Scenario()
            {
                Name = "Q1_AllDiesel",
                MovementSampleRatio = sampleRatio,
                SimStartYear = ModelYear.Y2020,
                SimEndYear = ModelYear.Y2050,
                DepotBuildYear = (ModelYear.N_A, ModelYear.N_A),
                DestinationBuildYear = (ModelYear.N_A, ModelYear.N_A),
                ErsBuildYear = (ModelYear.N_A, ModelYear.N_A),
                StationBuildYear = (ModelYear.N_A, ModelYear.N_A),
                BatteryOffers = new BatteryOffers {
                    { Parameters.MGV16, new UnitList<KiloWattHours>() { 0 }.ToArray() },
                    { Parameters.MGV24, new UnitList<KiloWattHours>() { 0 }.ToArray() },
                    { Parameters.HGV40, new UnitList<KiloWattHours>() { 0 }.ToArray() },
                    { Parameters.HGV60, new UnitList<KiloWattHours>() { 0 }.ToArray() }
                },
                InfraOffers = new InfraOffers()
                {
                    AvailablePowerPerUser_kW = new Dictionary<RouteSegmentType, KiloWatts>()
                    {
                        { RouteSegmentType.Depot, new KiloWatts(0) },
                        { RouteSegmentType.Destination, new KiloWatts(0) },
                        { RouteSegmentType.RestStop, new KiloWatts(0) },
                        { RouteSegmentType.Road, new KiloWatts(0) }
                    },
                    ErsReferenceSpeed_kmph = new KilometersPerHour(85),
                    ErsCoverageRatio = new Dimensionless(1),
                    FinalErsNetworkScope_km = new Kilometers(0)
                },
                ChargingStrategies = new List<ChargingStrategy>() {
                    ChargingStrategy.NA_Diesel
                },
                InheritInfraPower = false
            });

            scenarios.Add(new Scenario()
            {
                Name = "Q1_AllDepot",
                MovementSampleRatio = sampleRatio,
                SimStartYear = ModelYear.Y2020,
                SimEndYear = ModelYear.Y2050,
                DepotBuildYear = (ModelYear.Y2020, ModelYear.Y2020),
                DestinationBuildYear = (ModelYear.N_A, ModelYear.N_A),
                ErsBuildYear = (ModelYear.N_A, ModelYear.N_A),
                StationBuildYear = (ModelYear.N_A, ModelYear.N_A),
                BatteryOffers = GetLargeBatteryOffers(),
                InfraOffers = new InfraOffers()
                {
                    AvailablePowerPerUser_kW = GetDefaultInfrastructurePower(),
                    ErsReferenceSpeed_kmph = new KilometersPerHour(85),
                    ErsCoverageRatio = new Dimensionless(0.4f),
                    FinalErsNetworkScope_km = new Kilometers(6000)
                },
                ChargingStrategies = GetDefaultChargingStrategies(),
                InheritInfraPower = false,
                Before = delegate () { Parameters.World.Diesel_Price_euro_per_liter.SetToMultipleOfDefault(100); },
                After = delegate () { Parameters.World.Diesel_Price_euro_per_liter.ResetToDefault(); }
            });

            scenarios.Add(new Scenario()
            {
                Name = "Q1_AllDepotAndRestStops",
                MovementSampleRatio = sampleRatio,
                SimStartYear = ModelYear.Y2020,
                SimEndYear = ModelYear.Y2050,
                DepotBuildYear = (ModelYear.Y2020, ModelYear.Y2020),
                DestinationBuildYear = (ModelYear.N_A, ModelYear.N_A),
                ErsBuildYear = (ModelYear.N_A, ModelYear.N_A),
                StationBuildYear = (ModelYear.Y2020, ModelYear.Y2020),
                BatteryOffers = GetLargeBatteryOffers(),
                InfraOffers = new InfraOffers()
                {
                    AvailablePowerPerUser_kW = GetDefaultInfrastructurePower(),
                    ErsReferenceSpeed_kmph = new KilometersPerHour(85),
                    ErsCoverageRatio = new Dimensionless(0.4f),
                    FinalErsNetworkScope_km = new Kilometers(6000)
                },
                ChargingStrategies = GetDefaultChargingStrategies(),
                InheritInfraPower = false,
                Before = delegate () { Parameters.World.Diesel_Price_euro_per_liter.SetToMultipleOfDefault(100); },
                After = delegate () { Parameters.World.Diesel_Price_euro_per_liter.ResetToDefault(); }
            });

            scenarios.Add(new Scenario()
            {
                Name = "Q1_AllInfraExceptErs",
                MovementSampleRatio = sampleRatio,
                SimStartYear = ModelYear.Y2020,
                SimEndYear = ModelYear.Y2050,
                DepotBuildYear = (ModelYear.Y2020, ModelYear.Y2020),
                DestinationBuildYear = (ModelYear.Y2020, ModelYear.Y2020),
                ErsBuildYear = (ModelYear.N_A, ModelYear.N_A),
                StationBuildYear = (ModelYear.Y2020, ModelYear.Y2020),
                BatteryOffers = GetLargeBatteryOffers(),
                InfraOffers = new InfraOffers()
                {
                    AvailablePowerPerUser_kW = GetDefaultInfrastructurePower(),
                    ErsReferenceSpeed_kmph = new KilometersPerHour(85),
                    ErsCoverageRatio = new Dimensionless(0.4f),
                    FinalErsNetworkScope_km = new Kilometers(6000)
                },
                ChargingStrategies = GetDefaultChargingStrategies(),
                InheritInfraPower = false,
                Before = delegate () { Parameters.World.Diesel_Price_euro_per_liter.SetToMultipleOfDefault(100); },
                After = delegate () { Parameters.World.Diesel_Price_euro_per_liter.ResetToDefault(); }
            });

            scenarios.Add(new Scenario()
            {
                Name = "Q1_AllInfra",
                MovementSampleRatio = sampleRatio,
                SimStartYear = ModelYear.Y2020,
                SimEndYear = ModelYear.Y2050,
                DepotBuildYear = (ModelYear.Y2020, ModelYear.Y2020),
                DestinationBuildYear = (ModelYear.Y2020, ModelYear.Y2020),
                ErsBuildYear = (ModelYear.Y2020, ModelYear.Y2020),
                StationBuildYear = (ModelYear.Y2020, ModelYear.Y2020),
                BatteryOffers = GetLargeBatteryOffers(),
                InfraOffers = new InfraOffers()
                {
                    AvailablePowerPerUser_kW = GetDefaultInfrastructurePower(),
                    ErsReferenceSpeed_kmph = new KilometersPerHour(85),
                    ErsCoverageRatio = new Dimensionless(0.4f),
                    FinalErsNetworkScope_km = new Kilometers(6000)
                },
                ChargingStrategies = GetDefaultChargingStrategies(forcedErsUse: true),
                InheritInfraPower = false,
                Before = delegate () { Parameters.World.Diesel_Price_euro_per_liter.SetToMultipleOfDefault(100); },
                After = delegate () { Parameters.World.Diesel_Price_euro_per_liter.ResetToDefault(); }
            });

            return scenarios;
        }

        public static Scenario Q2_ErsPowerTuning(Dimensionless sampleRatio, Kilometers ers_km, KiloWatts ers_kWPerUser, Dimensionless ersCoverage_ratio, (ModelYear from, ModelYear to) depotBuildYears)
        {
            var powerPerUser = GetDefaultInfrastructurePower();
            powerPerUser[RouteSegmentType.Road] = ers_kWPerUser;

            ModelYear y = ModelYear.Y2030;
            return new Scenario()
            {
                Name = "Q2_ERS_kW_cover",
                MovementSampleRatio = sampleRatio,
                SimStartYear = y,
                SimEndYear = y,
                DepotBuildYear = depotBuildYears,
                DestinationBuildYear = (y, y),
                ErsBuildYear = (y, y),
                StationBuildYear = (y, y),
                BatteryOffers = GetDefaultBatteryOffers(),
                InfraOffers = new InfraOffers()
                {
                    AvailablePowerPerUser_kW = powerPerUser,
                    ErsReferenceSpeed_kmph = new KilometersPerHour(85),
                    ErsCoverageRatio = ersCoverage_ratio,
                    FinalErsNetworkScope_km = ers_km
                },
                ChargingStrategies = GetDefaultChargingStrategies()
            };
        }

        public static List<Scenario> Q3_ErsLengthTuning(Dimensionless sampleRatio)
        {
            //This should answer how long an ERS network should be (varying lengths)
            //and if the marginal value of ERS differs by year (constant infra over time)

            var ersLengths = new UnitList<Kilometers>() { 0, 125, 250, 500, 1000, 2000, 4000, 8000 };
            var depDensity = new UnitList<Dimensionless>() { 0.1f, 0.5f, 0.9f };
            var destDensity = new UnitList<Dimensionless>() { 0.1f, 0.25f, 0.5f };
            var statDensity = new UnitList<Dimensionless>() { 0.1f, 0.5f, 0.9f };

            List<Scenario> scenarios = new List<Scenario>();

            foreach (var ersKm in ersLengths)
            {
                for (int i = 0; i < depDensity.Count; i++)
                {
                    scenarios.Add(Custom(sampleRatio,
                        "q3_ers_length_" + ersKm + "_" + i,
                        (ModelYear.Y2020, ModelYear.Y2050),
                        (ersKm, new KiloWatts(500), new Dimensionless(0.35f)),
                        (ModelYear.Y2020, ModelYear.Y2020, depDensity[i]),
                        (ModelYear.Y2020, ModelYear.Y2020, destDensity[i]),
                        (ModelYear.Y2020, ModelYear.Y2020),
                        (ModelYear.Y2020, ModelYear.Y2020, statDensity[i])));
                }
            }

            return scenarios;
        }

        public static Scenario Q4_PrivateVsPublic(Dimensionless sampleRatio, (ModelYear from, ModelYear to) privateBuildYears, (ModelYear from, ModelYear to) publicBuildYears, Kilometers ers_kmFinal)
        {
            return new Scenario()
            {
                Name = "Q4_PrivatePublic",
                MovementSampleRatio = sampleRatio,
                SimStartYear = ModelYear.Y2020,
                SimEndYear = ModelYear.Y2050,
                DepotBuildYear = privateBuildYears,
                DestinationBuildYear = privateBuildYears,
                ErsBuildYear = publicBuildYears,
                StationBuildYear = publicBuildYears,
                BatteryOffers = GetDefaultBatteryOffers(),
                InfraOffers = new InfraOffers()
                {
                    AvailablePowerPerUser_kW = GetDefaultInfrastructurePower(),
                    ErsReferenceSpeed_kmph = new KilometersPerHour(85),
                    ErsCoverageRatio = new Dimensionless(0.25f),
                    FinalErsNetworkScope_km = ers_kmFinal
                },
                ChargingStrategies = GetDefaultChargingStrategies()
            };
        }

        public static Scenario Q7_InteractionEffects(Dimensionless sampleRatio, bool depot, bool destination, bool ers, bool station)
        {
            ModelYear y = ModelYear.Y2035;
            ModelYear n = ModelYear.N_A;
            return new Scenario()
            {
                Name = "Q7_InteractionEffects",
                MovementSampleRatio = sampleRatio,
                SimStartYear = y,
                SimEndYear = y,
                DepotBuildYear = depot ? (y.Previous(), y.Next()) : (y, y.Next().Next()), //66% / 33%
                DestinationBuildYear = destination ? (y.Previous(), y.Next()) : (y, y.Next().Next()), //66% / 33%
                ErsBuildYear = ers ? (y, y) : (n, n), //100%, 0%
                StationBuildYear = station ? (y, y) : (n, n), //100%, 0%
                BatteryOffers = GetDefaultBatteryOffers(),
                InfraOffers = new InfraOffers()
                {
                    AvailablePowerPerUser_kW = GetDefaultInfrastructurePower(),
                    ErsReferenceSpeed_kmph = new KilometersPerHour(85),
                    ErsCoverageRatio = new Dimensionless(0.25f),
                    FinalErsNetworkScope_km = new Kilometers(2000)
                },
                ChargingStrategies = GetDefaultChargingStrategies()
            };
        }

        public static List<Scenario> Q6_ERSMarginalValuePerYear(Dimensionless sampleRatio)
        {
            List<Scenario> scenarios = new List<Scenario>();

            var ers = (new Kilometers(8000), new KiloWatts(300), new Dimensionless(0.5f));
            var simYears = ModelYear.Y2020.GetSequence(ModelYear.Y2050);

            foreach (var year in simYears)
            {
                var low = new BuildPeriod(year, year, new Dimensionless(0.25f));
                var high = new BuildPeriod(year, year, new Dimensionless(0.5f));
                var none = (ModelYear.N_A, ModelYear.N_A);

                scenarios.Add(Custom(sampleRatio, "ers_marginal_low_without_" + year.AsInteger(), (year, year), ers, low, low, none, low));
                scenarios.Add(Custom(sampleRatio, "ers_marginal_low_with_" + year.AsInteger(), (year, year), ers, low, low, (year, year), low));
                scenarios.Add(Custom(sampleRatio, "ers_marginal_high_without_" + year.AsInteger(), (year, year), ers, high, high, none, high));
                scenarios.Add(Custom(sampleRatio, "ers_marginal_high_with_" + year.AsInteger(), (year, year), ers, high, high, (year, year), high));
            }

            return scenarios;
        }

        public static List<Scenario> Q5_RealisticScenarios(Dimensionless sampleRatio)
        {
            List<Scenario> scenarios = new List<Scenario>();

            scenarios.Add(Custom(sampleRatio, "slow_dynamic", (ModelYear.Y2020, ModelYear.Y2050),
                (new Kilometers(2000), new KiloWatts(700), new Dimensionless(0.2f)),
                (ModelYear.Y2025, ModelYear.Y2045, new Dimensionless(0.5f)),
                (ModelYear.Y2025, ModelYear.Y2040, new Dimensionless(0.15f)),
                (ModelYear.Y2030, ModelYear.Y2040),
                (ModelYear.Y2025, ModelYear.Y2035, new Dimensionless(0.5f))));
            scenarios.Add(Custom(sampleRatio, "slow_static", (ModelYear.Y2020, ModelYear.Y2050),
                (new Kilometers(0), new KiloWatts(700), new Dimensionless(0.2f)),
                (ModelYear.Y2025, ModelYear.Y2045, new Dimensionless(0.5f)),
                (ModelYear.Y2025, ModelYear.Y2040, new Dimensionless(0.15f)),
                (ModelYear.N_A, ModelYear.N_A),
                (ModelYear.Y2025, ModelYear.Y2045, new Dimensionless(1f))));
            scenarios.Add(Custom(sampleRatio, "average_dynamic", (ModelYear.Y2020, ModelYear.Y2050),
                (new Kilometers(3000), new KiloWatts(500), new Dimensionless(0.35f)),
                (ModelYear.Y2025, ModelYear.Y2040, new Dimensionless(0.7f)),
                (ModelYear.Y2025, ModelYear.Y2035, new Dimensionless(0.2f)),
                (ModelYear.Y2030, ModelYear.Y2035),
                (ModelYear.Y2025, ModelYear.Y2030, new Dimensionless(0.3f))));
            scenarios.Add(Custom(sampleRatio, "average_static", (ModelYear.Y2020, ModelYear.Y2050),
                (new Kilometers(0), new KiloWatts(500), new Dimensionless(0.35f)),
                (ModelYear.Y2025, ModelYear.Y2040, new Dimensionless(0.9f)),
                (ModelYear.Y2025, ModelYear.Y2035, new Dimensionless(0.25f)),
                (ModelYear.N_A, ModelYear.N_A),
                (ModelYear.Y2025, ModelYear.Y2040, new Dimensionless(0.8f))));
            scenarios.Add(Custom(sampleRatio, "fast_dynamic", (ModelYear.Y2020, ModelYear.Y2050),
                (new Kilometers(4000), new KiloWatts(500), new Dimensionless(0.35f)),
                (ModelYear.Y2025, ModelYear.Y2035, new Dimensionless(0.8f)),
                (ModelYear.Y2025, ModelYear.Y2035, new Dimensionless(0.2f)),
                (ModelYear.Y2025, ModelYear.Y2035),
                (ModelYear.Y2025, ModelYear.Y2040, new Dimensionless(0.5f))));
            scenarios.Add(Custom(sampleRatio, "fast_static", (ModelYear.Y2020, ModelYear.Y2050),
                (new Kilometers(0), new KiloWatts(500), new Dimensionless(0.35f)),
                (ModelYear.Y2025, ModelYear.Y2035, new Dimensionless(1f)),
                (ModelYear.Y2025, ModelYear.Y2035, new Dimensionless(0.25f)),
                (ModelYear.N_A, ModelYear.N_A),
                (ModelYear.Y2025, ModelYear.Y2035, new Dimensionless(1f))));
            scenarios.Add(Custom(sampleRatio, "very_fast", (ModelYear.Y2020, ModelYear.Y2050),
                (new Kilometers(6000), new KiloWatts(300), new Dimensionless(0.75f)),
                (ModelYear.Y2025, ModelYear.Y2035, new Dimensionless(0.9f)),
                (ModelYear.Y2025, ModelYear.Y2035, new Dimensionless(0.05f)),
                (ModelYear.Y2025, ModelYear.Y2035),
                (ModelYear.Y2025, ModelYear.Y2040, new Dimensionless(0.8f))));

            return scenarios;
        }

        public static List<Scenario> GetScenarioMatrix(Dimensionless sampleRate, bool forcedErsUse = false)
        {
            //var simYear = (ModelYear.Y2035, ModelYear.Y2035);
            //var ers_max_km = new Kilometers(8000f);

            //var p0 = ((ModelYear.Y2050, ModelYear.Y2050), "00");
            //var p25 = ((ModelYear.Y2035, ModelYear.Y2050), "25");
            //var p75 = ((ModelYear.Y2020, ModelYear.Y2040), "75");

            var simYear = (ModelYear.Y2035, ModelYear.Y2025);
            var ers_max_km = new Kilometers(8000f);

            var p0 = ((ModelYear.Y2050, ModelYear.Y2050, new Dimensionless(0)), "00");
            var p25 = ((ModelYear.Y2020, ModelYear.Y2020, new Dimensionless(0.25f)), "25");
            var p75 = ((ModelYear.Y2020, ModelYear.Y2020, new Dimensionless(0.75f)), "75");
            
            var ratios = new ((ModelYear, ModelYear, Dimensionless) y, string name)[] { p0, p25, p75 };

            var restStop_kW = new KiloWatts(1000);
            var ers_kW = new UnitList<KiloWatts>() { 100, 300, 700 };
            var ers_cover = new UnitList<Dimensionless>() { 0.25f, 0.5f, 1.0f };

            var scenarios = new List<Scenario>();
            foreach (var depRatio in ratios)
            {
                foreach (var destRatio in ratios)
                {
                    foreach (var ersRatio in ratios)
                    {
                        var ers_km = ers_max_km * ersRatio.y.Item3;
                        foreach (var statRatio in ratios)
                        {
                            foreach (var kW in ers_kW)
                            {
                                foreach (var cover in ers_cover)
                                {
                                    string name = "matrix_" + depRatio.name + "_" + destRatio.name + "_" + ersRatio.name + "_" + statRatio.name + "_" + kW + "_" + cover;
                                    scenarios.Add(Experiments.Custom(sampleRate, name, simYear, (ers_km, kW, cover), depRatio.y, destRatio.y, (ersRatio.y.Item1, ersRatio.y.Item2), statRatio.y, forcedErsUse, restStop_kW));

                                    if (ersRatio == p0)
                                        break;
                                }
                                if (ersRatio == p0)
                                    break;
                            }
                        }
                    }
                }
            }

            return scenarios;
        }

        public static Scenario Q8_GetExpectedScenario(Dimensionless sampleRatio, string suffix, 
            Kilometers finalErsLength = null, 
            Dimensionless ersCoverageRatio = null,
            ModelYear ersStartYear = ModelYear.Y2030,
            BuildPeriod depotBuildPeriod = null,
            BuildPeriod destinationBuildPeriod = null,
            BuildPeriod stationBuildPeriod = null,
            bool forceErsUse = false)
        {
            finalErsLength ??= new(2000);
            ersCoverageRatio ??= new(0.4f);

            KiloWatts distanceAveragedErsPowerPerUser = new KiloWatts(150); //This value was identified in the 2022 technical report.
            KiloWatts ersPowerPerUser = distanceAveragedErsPowerPerUser / ersCoverageRatio;
            ModelYear ersEndYear = (ModelYear)((int)ersStartYear + (int)Math.Max(0, Math.Ceiling(ersCoverageRatio.Val * finalErsLength.Val / 1500) - 1));
            //25 30 35 40 45
            //20 40 60 80 100
            var s = new Scenario()
            {
                Name = "Q8_ExpectedScenario_" + suffix + "_" + finalErsLength.Val,
                MovementSampleRatio = sampleRatio,
                SimStartYear = ModelYear.Y2020,
                SimEndYear = ModelYear.Y2050,
                DepotBuildYear = depotBuildPeriod ?? (ModelYear.Y2025, ModelYear.Y2050, new(.95f)), //17%, 33%, 50%, 66%, 83%, 100%
                DestinationBuildYear = destinationBuildPeriod ?? (ModelYear.Y2030, ModelYear.Y2045, new(.3f)),
                ErsBuildYear = (ersStartYear, ersEndYear),
                StationBuildYear = stationBuildPeriod ?? (ModelYear.Y2025, ModelYear.Y2045, new(1f)),
                BatteryOffers = GetDefaultBatteryOffers(),
                InfraOffers = new InfraOffers()
                {
                    AvailablePowerPerUser_kW = GetDefaultInfrastructurePower(),
                    ErsReferenceSpeed_kmph = new KilometersPerHour(85),
                    ErsCoverageRatio = ersCoverageRatio,
                    FinalErsNetworkScope_km = finalErsLength
                },
                ChargingStrategies = GetDefaultChargingStrategies()
            };
            s.InfraOffers.AvailablePowerPerUser_kW[RouteSegmentType.Road] = ersPowerPerUser;

            if (forceErsUse)
            {
                s.ChargingStrategies.Remove(ChargingStrategy.PublicStaticCharging);
                s.ChargingStrategies.Remove(ChargingStrategy.Depot);
                s.ChargingStrategies.Remove(ChargingStrategy.AllPlannedStops);
            }

            return s;
        }

        public static List<Scenario> Q9_ParameterAndPolicyScenarios(Dimensionless sampleRatio)
        {
            List<Scenario> scenarios = new();

            var paramScenarios = GetParameterVariations();

            Dimensionless ersCoverageRatio = new(.4f);
            foreach (var ers_km in new float[] { 0, 2000, 6000 })
            {
                foreach (bool forceErsUse in new bool[] { false, true })
                {
                    if (ers_km == 0 && forceErsUse)
                        continue;

                    foreach ((string suffix, Action parameterConfig) in new List<(string, Action)>() {
                        ("neutral", paramScenarios.neutral),
                        ("pro-dynamic", paramScenarios.favorDynamicCharging),
                        ("pro-static", paramScenarios.favorStaticCharging),
                        //("high-SCC", paramScenarios.highCostOfCarbon),
                        //("triple-traffic", paramScenarios.tripleTraffic),
                    })
                    {
                        string suffix2 = suffix + (forceErsUse ? "_forced_ers" : "_optional_ers");
                        var s = Q8_GetExpectedScenario(sampleRatio, suffix2, finalErsLength: new(ers_km), ersCoverageRatio: ersCoverageRatio, forceErsUse: forceErsUse);
                        s.SimStartYear = ModelYear.Y2020;
                        s.Before = parameterConfig;
                        s.After = paramScenarios.resetParameters;
                        scenarios.Add(s);
                    }

                    //var denseErs = Q8_GetExpectedScenario(
                    //    sampleRatio,
                    //    "dense-ers" + (forceErsUse ? "_forced_ers" : "_optional_ers"),
                    //    finalErsLength: new(ers_km),
                    //    ersCoverageRatio: new(1f),
                    //    forceErsUse: forceErsUse);
                    //denseErs.After = paramScenarios.resetParameters;
                    //scenarios.Add(denseErs);

                    var fastStatic = Q8_GetExpectedScenario(
                        sampleRatio,
                        "rapid-static" + (forceErsUse ? "_forced_ers" : "_optional_ers"),
                        depotBuildPeriod: (ModelYear.Y2025, ModelYear.Y2050, new(.95f)),
                        destinationBuildPeriod: (ModelYear.Y2030, ModelYear.Y2035, new(.3f)),
                        stationBuildPeriod: (ModelYear.Y2025, ModelYear.Y2035, new(1f)),
                        ersStartYear: ModelYear.Y2040,
                        finalErsLength: new(ers_km),
                        ersCoverageRatio: ersCoverageRatio,
                        forceErsUse: forceErsUse);
                    fastStatic.After = paramScenarios.resetParameters;
                    scenarios.Add(fastStatic);

                    var cappedStatic = Q8_GetExpectedScenario(
                        sampleRatio,
                        "capped-static" + (forceErsUse ? "_forced_ers" : "_optional_ers"),
                        depotBuildPeriod: (ModelYear.Y2025, ModelYear.Y2050, new(.65f)),
                        destinationBuildPeriod: (ModelYear.Y2030, ModelYear.Y2045, new(.15f)),
                        stationBuildPeriod: (ModelYear.Y2025, ModelYear.Y2045, new(0.65f)),
                        ersStartYear: ModelYear.Y2030,
                        finalErsLength: new(ers_km),
                        ersCoverageRatio: ersCoverageRatio,
                        forceErsUse: forceErsUse);
                    fastStatic.After = paramScenarios.resetParameters;
                    scenarios.Add(cappedStatic);
                }
            }

            return scenarios;
        }

        public static List<Scenario> Q10_RestStopFrequency(Dimensionless sampleRatio)
        {
            var scenarios = Q9_ParameterAndPolicyScenarios(sampleRatio).Where(n => n.Name.Contains("pro-static"));
            var scenarios2 = Q9_ParameterAndPolicyScenarios(sampleRatio).Where(n => n.Name.Contains("pro-static"));
            foreach (var scenario in scenarios2) {
                scenario.Name += "_shortstops";
                var init = scenario.Before;
                scenario.Before = () =>
                {
                    init();
                    Parameters.MGV16.Common_Rest_stop_h.SetToMultipleOfDefault(0.5f);
                    Parameters.MGV24.Common_Rest_stop_h.SetToMultipleOfDefault(0.5f);
                    Parameters.HGV40.Common_Rest_stop_h.SetToMultipleOfDefault(0.5f);
                    Parameters.HGV60.Common_Rest_stop_h.SetToMultipleOfDefault(0.5f);

                    Parameters.MGV16.Common_Drive_session_h.SetToMultipleOfDefault(0.5f);
                    Parameters.MGV24.Common_Drive_session_h.SetToMultipleOfDefault(0.5f);
                    Parameters.HGV40.Common_Drive_session_h.SetToMultipleOfDefault(0.5f);
                    Parameters.HGV60.Common_Drive_session_h.SetToMultipleOfDefault(0.5f);
                };

                scenario.After = Parameters.ResetAll;
            }
            
            return scenarios.Union(scenarios2).ToList();
        }

        private static (Action neutral, Action favorStaticCharging, Action favorDynamicCharging, Action expensiveBatteries, Action tripleRenewables, Action highCostOfCarbon, Action resetParameters) GetParameterVariations()
        {
            //neutral, favor dynamic (low day-time electricity price), favor static (higher day-time electricity price), high CO2 tax, favor diesel, expensive batteries

            Action neutral = delegate () { };
            
            Action favorStaticCharging = delegate ()
            {
                //Higher ERS cost
                Parameters.Infrastructure.ERS_base_cost_euro_per_km.SetToMultipleOfDefault(2f);
                Parameters.Infrastructure.ERS_power_cost_euro_per_kW_km.SetToMultipleOfDefault(2f);
                //Higher utilization for depot/station combo, due to merging
                Parameters.Infrastructure.Depot_utilization_ratio.SetToMultipleOfDefault(1.45f);
                Parameters.Infrastructure.Rest_Stop_utilization_ratio.SetToMultipleOfDefault(1.45f);
                //High day-time electricity price
                Parameters.World.SE12_Price_max_euro_per_kWh.SetToMultipleOfDefault(3f);
                Parameters.World.SE34_Price_max_euro_per_kWh.SetToMultipleOfDefault(3f);
                Parameters.World.OtherRegion_Price_max_euro_per_kWh.SetToMultipleOfDefault(3f);
                //No growth in traffic volume
                Parameters.World.Economy_Heavy_traffic_volume_vs_2020_percent.SetToExponentialTrendFromFirstValue(1.00f);
            };
            Action favorDynamicCharging = delegate ()
            {
                //Longer operating hours, annual distance per truck. Vehicle lifespan is kept constant but maintenance cost increases. Residual value is already low.
                foreach (var v in Parameters.VehicleTypesInOrder)
                    v.ModifyDailyOperatingHoursAndAdjustDepotTimeAndAnnualDistance(annualChange: 1.02f);

                //Greater utilization of trucks results in changed utilization of day-time and nighttime charging infrastructure
                Parameters.Infrastructure.Rest_Stop_utilization_ratio.SetToExponentialTrendFromFirstValue(1.01f);
                Parameters.Infrastructure.Destination_utilization_ratio.SetToExponentialTrendFromFirstValue(1.01f);
                Parameters.Infrastructure.ERS_utilization_ratio.SetToExponentialTrendFromFirstValue(1.01f);
                Parameters.Infrastructure.Depot_utilization_ratio.SetToExponentialTrendFromFirstValue(0.99f);

                //Declining cost of day-time electricity
                //Parameters.World.SE12_Price_max_euro_per_kWh.SetToExponentialTrendFromFirstValue(0.97f); //60% decrease 2020-2050
                //Parameters.World.SE34_Price_max_euro_per_kWh.SetToExponentialTrendFromFirstValue(0.97f);
                //Parameters.World.OtherRegion_Price_max_euro_per_kWh.SetToExponentialTrendFromFirstValue(0.97f);
            };
            Action expensiveBatteries = delegate ()
            {
                float v = Parameters.Battery.Gross_Pack_cost_euro_per_kWh.First().Val;
                Parameters.ModifyBatteryPackCost(new float[] { v, v, v, v, v, v, v });
            };
            Action tripleRenewables = delegate ()
            {
                var cap = Parameters.World.RenewableDiesel_Supply_cap_liter_per_year.Select(n => n.Val * 3f).ToArray();
                Parameters.ModifyRenewableFuelSupplyCap(cap);
            };
            Action highCostOfCarbon = delegate ()
            {
                Parameters.ModifySCCAndCO2Tax(
                    Parameters.World.CO2_SCC_euro_per_kg.Select(n => n.Val * 5).ToArray(),
                    Parameters.World.CO2_Tax_ratio_of_SCC.Select(n => n.Val).ToArray());
            };
            Action resetParameters = delegate ()
            {
                Parameters.ResetAll();
            };

            return (neutral, favorStaticCharging, favorDynamicCharging, expensiveBatteries, tripleRenewables, highCostOfCarbon, resetParameters);
        }

        public static List<Scenario> GetRandomScenarios(Dimensionless sampleRate, int scenarioCount)
        {
            var simYear = (ModelYear.Y2035, ModelYear.Y2035);
            var ers_max_km = new Kilometers(8000f);

            List<((ModelYear begin, ModelYear end) y, float ratio)> years = new List<((ModelYear begin, ModelYear end) y, float ratio)>();
            foreach (var y1 in ModelYear.Y2020.GetSequence(ModelYear.Y2045))
            { 
                foreach (var y2 in y1.Next().GetSequence(ModelYear.Y2050))
                {
                    float ratio = (ModelYear.Y2035.AsInteger() - y1.AsInteger()) / (y2.AsInteger() - y1.AsInteger());
                    years.Add(((y1, y2), ratio));
                }
            }

            var p0 = ((ModelYear.Y2050, ModelYear.Y2050), "00");
            var p25 = ((ModelYear.Y2035, ModelYear.Y2050), "25");
            var p75 = ((ModelYear.Y2020, ModelYear.Y2040), "75");
            var ratios = new ((ModelYear, ModelYear) y, string name)[] { p0, p25, p75 };

            var ers_kW = new UnitList<KiloWatts>() { 100, 300, 700 };
            var ers_cover = new UnitList<Dimensionless>() { 0.25f, 0.5f, 1.0f };

            var scenarios = new List<Scenario>();
            foreach (var depRatio in ratios)
            {
                foreach (var destRatio in ratios)
                {
                    foreach (var ersRatio in ratios.Where(r => r != p0))
                    {
                        foreach (var statRatio in ratios)
                        {
                            foreach (var kW in ers_kW)
                            {
                                foreach (var cover in ers_cover)
                                {
                                    string name = "matrix_" + depRatio.name + "_" + destRatio.name + "_" + ersRatio.name + "_" + statRatio.name + "_" + kW + "_" + cover;
                                    scenarios.Add(Experiments.Custom(sampleRate, name, simYear, (ers_max_km, kW, cover), depRatio.y, destRatio.y, ersRatio.y, statRatio.y));
                                }
                            }
                        }
                    }
                }
            }

            return scenarios;
        }

        public static List<Scenario> GetPowerDemandForecastScenarios(Dimensionless sampleRatio)
        {
            //Change station build order to random

            List<Scenario> scenarios = new List<Scenario>()
            {
                new Scenario()
                {
                    Name = "Power forecast without ERS",
                    MovementSampleRatio = sampleRatio,
                    SimStartYear = ModelYear.Y2020,
                    SimEndYear = ModelYear.Y2050,
                    DepotBuildYear = (ModelYear.Y2025, ModelYear.Y2040, new Dimensionless(.9f)),
                    DestinationBuildYear = (ModelYear.Y2030, ModelYear.Y2045, new Dimensionless(.3f)),
                    StationBuildYear = (ModelYear.Y2025, ModelYear.Y2045, new Dimensionless(0.9f)),
                    ErsBuildYear = (ModelYear.N_A, ModelYear.N_A),
                    BatteryOffers = GetDefaultBatteryOffers(),
                    InfraOffers = new InfraOffers()
                    {
                        AvailablePowerPerUser_kW = GetDefaultInfrastructurePower(),
                        ErsReferenceSpeed_kmph = new KilometersPerHour(85),
                        ErsCoverageRatio = new Dimensionless(1f),
                        FinalErsNetworkScope_km = new Kilometers(0)
                    },
                    ChargingStrategies = GetDefaultChargingStrategies(),
                    InheritInfraPower = false
                },
                new Scenario()
                {
                    Name = "Power forecast with ERS",
                    MovementSampleRatio = sampleRatio,
                    SimStartYear = ModelYear.Y2020,
                    SimEndYear = ModelYear.Y2050,
                    DepotBuildYear = (ModelYear.Y2025, ModelYear.Y2040, new Dimensionless(.9f)),
                    DestinationBuildYear = (ModelYear.Y2030, ModelYear.Y2045, new Dimensionless(.3f)),
                    StationBuildYear = (ModelYear.Y2025, ModelYear.Y2045, new Dimensionless(0.9f)),
                    ErsBuildYear = (ModelYear.Y2030, ModelYear.Y2045),
                    BatteryOffers = GetDefaultBatteryOffers(),
                    InfraOffers = new InfraOffers()
                    {
                        AvailablePowerPerUser_kW = GetDefaultInfrastructurePower(),
                        ErsReferenceSpeed_kmph = new KilometersPerHour(85),
                        ErsCoverageRatio = new Dimensionless(0.35f),
                        FinalErsNetworkScope_km = new Kilometers(4000)
                    },
                    ChargingStrategies = GetDefaultChargingStrategies(),
                    InheritInfraPower = false
                },
            };
            scenarios.Where(n => n.Name == "Power forecast with ERS").First().InfraOffers.AvailablePowerPerUser_kW[RouteSegmentType.Road] = new(500);

            return scenarios;
        }

        public static List<Scenario> SensitivityAnalysisTemplate(Dimensionless sampleRate)
        {
            ModelYear y = ModelYear.Y2030;
            var simPeriod = (y, y);
            var ersConfig = (new Kilometers(2000), new KiloWatts(300), new Dimensionless(0.5f));
            var depotPeriod = (y, y, new Dimensionless(0.5f));
            var destPeriod = (y, y, new Dimensionless(0.25f));
            var ersPeriod = (y, y);
            var stationPeriod = (y, y, new Dimensionless(0.5f));

            Scenario low = Custom(sampleRate, "diesel-price-low", simPeriod, ersConfig, depotPeriod, destPeriod, ersPeriod, stationPeriod);
            low.Before = delegate () { Parameters.World.Diesel_Price_euro_per_liter.SetToMultipleOfDefault(.5f); };
            low.After = delegate () { Parameters.World.Diesel_Price_euro_per_liter.ResetToDefault(); };

            Scenario medium = Custom(sampleRate, "diesel-price-medium", simPeriod, ersConfig, depotPeriod, destPeriod, ersPeriod, stationPeriod);
            low.Before = delegate () { Parameters.World.Diesel_Price_euro_per_liter.SetToMultipleOfDefault(1); };
            low.After = delegate () { Parameters.World.Diesel_Price_euro_per_liter.ResetToDefault(); };

            Scenario high = Custom(sampleRate, "diesel-price-high", simPeriod, ersConfig, depotPeriod, destPeriod, ersPeriod, stationPeriod);
            low.Before = delegate () { Parameters.World.Diesel_Price_euro_per_liter.SetToMultipleOfDefault(2f); };
            low.After = delegate () { Parameters.World.Diesel_Price_euro_per_liter.ResetToDefault(); };

            List<Scenario> scenarios = new List<Scenario>() { low, medium, high };

            return scenarios;
        }
    }
}
