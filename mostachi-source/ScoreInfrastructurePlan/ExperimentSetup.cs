using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScoreInfrastructurePlan
{
    public class LogSettings
    {
        public bool PrintInfraRasterLog { get; set; } = true;
        public bool PrintDrivingRasterLog { get; set; } = true;
        public bool PrintRoutesLog { get; set; } = true;
    }

    internal class ExperimentSetup
    {
        public static (List<Scenario> scenarios, Dimensionless sampleRatio, LogSettings settings) GetScenarios()
        {
            /*
             * THIS IS YOUR LAB! 
             * 
             * It may eventually be replaced by a pretty UI, but for now, there is a list "scenarios" that is populated
             * with the scenarios that are to be calculated when executing ScoreInfrastructurePlan. For each calculated scenario, the simulation
             * outputs .tsv files that describe:
             * - what charging infrastructure is built and how it is used;
             * - how the traffic passing each geographic grid cell is composed in terms of vehicle type, battery capacity and charging strategy;
             * - a full-system sum of various costs and charging patterns.
             * 
             * Typically, experiments are conducted by comparing scenario outputs, i.e. if we add ERS, how does the use of depot charging change, 
             * all else kept constant? This comparison has to be made using some other tool, for instance Pandas in Python.
             */

            var scenarios = new List<Scenario>();

            /* 
             * THIS PARAMETER NEEDS TO BE ADJUSTED BASED ON THE SIZE OF THE INPUT ROUTE DATASET
             * 
             * What ratio of all the route data should be used in the calculations? Less data means quicker computation. The sum of all traffic
             * remains approximately unchanged when routes are downsampled, by scaling the traffic on retained routes by the inverse of the
             * sampling ratio. If you are using a small test dataset of less than 10k routes, always set this parameter to 1.
             */
            Dimensionless sampleRatio = new Dimensionless(1f);

            /*
             * Enable and disable experiments here. Define your own experiments in Experiments.cs
             * Running one experiment (multiple scenarios) on the full Sweden dataset typically takes a few hours up to a day. A larger input dataset 
             * would take proportionally longer time, which can be compensated for by reducing the sample ratio (above), by routing between fewer 
             * locations, or by using some simplified road network other than OpenStreetMap. To route between fewer locations, ensure the density map 
             * used to sample OD-coordinates within OD-regions (in CalculateRoutes) is zero-valued for most grid cells.
             */

            //scenarios.AddRange(Experiments.Q1_AllDieselVsAllElectric(sampleRatio));

            scenarios.AddRange(Experiments.Q9_ParameterAndPolicyScenarios(sampleRatio));

            //scenarios.AddRange(Experiments.Q10_RestStopFrequency(sampleRatio));

            //Downsampling recommended, this evaluates 513 scenarios
            //scenarios.AddRange(Experiments.GetScenarioMatrix(sampleRatio, forcedErsUse: true).Shuffle());

            return (scenarios, sampleRatio, new LogSettings() { PrintRoutesLog = false, PrintDrivingRasterLog = false, PrintInfraRasterLog = false });
        }
    }
}
