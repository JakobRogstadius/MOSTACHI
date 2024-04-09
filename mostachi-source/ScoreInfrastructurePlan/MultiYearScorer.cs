using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using SegmentToChargingCostDict = System.Collections.Generic.Dictionary<ScoreInfrastructurePlan.RouteSegment, ScoreInfrastructurePlan.ChargingSiteCost>;

namespace ScoreInfrastructurePlan
{
    class MultiYearScorer
    {
        public static IEnumerable<(ModelYear year, RouteVehicleTypeBehavior routeStrategies, SegmentToChargingCostDict infraAtEndOfYear)> 
            ScoreYears(IEnumerable<Route_VehicleType> movementPatterns, Scenario scenario)
        {
            Dictionary<RouteSegment, ModelYear> lastYearOfInfraInvestment = new();

            Dictionary<ModelYear, (RouteVehicleTypeBehavior routeStrategies, SegmentToChargingCostDict infraSet)> modelYearResult = new Dictionary<ModelYear, (RouteVehicleTypeBehavior, SegmentToChargingCostDict)>();
            ModelYear year = scenario.SimStartYear;
            do
            {
                Console.WriteLine();
                Console.WriteLine(scenario.Name + ": " + year);

                var inheritedInfra = GetInheritedInfrastructure(scenario, lastYearOfInfraInvestment, modelYearResult, year);

                modelYearResult[year] = SingleYearScorer.SimulateUntilConvergence(year, movementPatterns, scenario, inheritedInfra);

                yield return (year, modelYearResult[year].routeStrategies, modelYearResult[year].infraSet);

                year = year.Next();
            } while (year <= scenario.SimEndYear);
        }

        private static Dictionary<RouteSegment, (KiloWatts kW_segment, KiloWattsPerKilometer kW_perLaneKm)> GetInheritedInfrastructure(Scenario scenario, Dictionary<RouteSegment, ModelYear> lastYearOfInfraInvestment, Dictionary<ModelYear, (RouteVehicleTypeBehavior routeStrategies, SegmentToChargingCostDict infraSet)> modelYearResult, ModelYear year)
        {
            var payoffPeriods = GetPayoffPeriods(year);

            //Try to inherit charging infrastructure from prior year, if this is enabled and some infrastructure existed in a previous year
            if (modelYearResult.Count == 0 || !scenario.InheritInfraPower)
            {
                return new();
            }
            else
            {
                var prevYear = year.Previous();
                var prevInfra = modelYearResult[prevYear].infraSet;
                foreach (var justInvestedInfra in prevInfra.Where(n => n.Value.PeakPowerIsGreaterThanInherited))
                {
                    lastYearOfInfraInvestment[justInvestedInfra.Key] = prevYear;
                }
                //Don't force infra to stick around if it has lived out its expected economic lifespan
                return prevInfra.Where(n => (year.AsInteger() - lastYearOfInfraInvestment[n.Key].AsInteger()) < payoffPeriods[n.Key.Type])
                    .ToDictionary(n => n.Key, n => (prevInfra[n.Key].InstalledPower_kWPeak_Segment, prevInfra[n.Key].InstalledPower_kWPeak_PerLaneKm));
            }
        }

        private static Dictionary<RouteSegmentType, Years> GetPayoffPeriods(ModelYear year)
        {
            return new()
            { 
                { RouteSegmentType.Depot, Parameters.Infrastructure.Depot_write_off_period_years[year] },
                { RouteSegmentType.Destination, Parameters.Infrastructure.Destination_write_off_period_years[year] },
                { RouteSegmentType.RestStop, Parameters.Infrastructure.Rest_Stop_write_off_period_years[year] },
                { RouteSegmentType.Road, Parameters.Infrastructure.ERS_write_off_period_years[year] }
            };
        }
    }
}
