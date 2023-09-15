using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Linq;
using System.Data;
using System.Xml;
using System.Collections.Generic;

namespace ScoreInfrastructurePlan
{
    class Program
    {
        static void Main(string[] args)
        {
            //System.Runtime.GCSettings.IsServerGC = true;

            System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.Batch;

            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            //Tests.RunTests();
            ExperimentRunner.Run();
        }
    }
}
