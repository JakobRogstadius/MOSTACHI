using System;
using System.Collections.Generic;
using System.Text;

namespace ScoreInfrastructurePlan
{
    /// <summary>
    /// One scenario to be evaluated as part of an experiment. One experiment typically compares the results for different scenarios.
    /// </summary>
    class Scenario
    {
        public string Name { get; set; }
        public (ModelYear From, ModelYear To) ErsBuildYear { get; set; }
        public BuildPeriod StationBuildYear { get; set; }
        public BuildPeriod DepotBuildYear { get; set; }
        public BuildPeriod DestinationBuildYear { get; set; }
        public BatteryOffers BatteryOffers { get; set; }
        public InfraOffers InfraOffers { get; set; }
        public ModelYear SimStartYear { get; set; } = ModelYear.Y2020;
        public ModelYear SimEndYear { get; set; } = ModelYear.Y2050;
        public List<ChargingStrategy> ChargingStrategies { get; set; }
        public Dimensionless MovementSampleRatio { get; set; } = new Dimensionless(1);
        /// <summary>By default, charging stations are built in descending AADR-order. Setting this to true reverses the order, which creates less competition with ERS but otherwise isn't particularly good.</summary>
        public bool ReverseStationOrder { get; set; } = false;
        /// <summary>Sets whether installed charging infrastructure in one time step should carry over to the next time step, or if everything should be recalculated from scratch.</summary>
        public bool InheritInfraPower { get; set; } = true;
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("= " + Name + " =");
            sb.AppendLine("Route sample ratio: " + MovementSampleRatio);
            sb.AppendLine("Simulation from " + SimStartYear + " to " + SimEndYear);
            sb.AppendLine("ERS construction:   " + ErsBuildYear.From + " to " + ErsBuildYear.To);
            sb.AppendLine("Stat construction:  " + StationBuildYear.From + " to " + StationBuildYear.To);
            sb.AppendLine("Depot construction: " + DepotBuildYear.From + " to " + DepotBuildYear.To);
            sb.AppendLine("Dest construction:  " + DestinationBuildYear.From + " to " + DestinationBuildYear.To);
            sb.AppendLine("= Charging strategies =");
            sb.AppendLine(String.Join(", ", ChargingStrategies));
            sb.Append(BatteryOffers.ToString());
            sb.AppendLine(InfraOffers.ToString());

            return sb.ToString();
        }

        public bool AnyInfraAvailable(ModelYear year)
        {
            return DepotBuildYear.From <= year && DepotBuildYear.FinalRatio > 0
                || DestinationBuildYear.From <= year && DestinationBuildYear.FinalRatio > 0
                || StationBuildYear.From <= year && StationBuildYear.FinalRatio > 0
                || ErsBuildYear.From <= year && InfraOffers.FinalErsLength_km > 0;
        }

        /// <summary>Use this to change global settings between scenarios.</summary>
        public Action Before { get; set; } = delegate () { };

        /// <summary>Use this to reset global settings between scenarios.</summary>
        public Action After { get; set; } = delegate () { };
    }

        static class ExperimentHelpers
    {
        static Random _rand = new Random(1000);
        public static IList<T> Shuffle<T>(this IList<T> list)
        {
            var newList = new List<T>(list);
            int n = newList.Count;
            while (n > 1)
            {
                n--;
                int k = _rand.Next(n + 1);
                T value = newList[k];
                newList[k] = newList[n];
                newList[n] = value;
            }
            return newList;
        }
    }

    class BuildPeriod
    {
        public BuildPeriod(ModelYear from, ModelYear to)
        {
            From = from;
            To = to;
        }

        public BuildPeriod(ModelYear from, ModelYear to, Dimensionless finalRatio)
        {
            From = from;
            To = to;
            FinalRatio = finalRatio;
        }

        public static implicit operator BuildPeriod(ValueTuple<ModelYear, ModelYear> period) { return new BuildPeriod(period.Item1, period.Item2); }
        public static implicit operator BuildPeriod(ValueTuple<ModelYear, ModelYear, Dimensionless> period) { return new BuildPeriod(period.Item1, period.Item2, period.Item3); }
        public ModelYear From { get; set; }
        public ModelYear To { get; set; }
        public Dimensionless FinalRatio { get; set; } = new Dimensionless(1f);
    }

}
