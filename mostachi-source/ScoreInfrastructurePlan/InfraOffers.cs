using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ScoreInfrastructurePlan
{
    /// <summary>
    /// This class represents the maximum power each type of charging infrastructure can deliver per vehicle,
    /// the size of the road network where ERS can be built, and whether that ERS has gaps or not.
    /// </summary>
    class InfraOffers
    {
        public Dictionary<RouteSegmentType, KiloWatts> AvailablePowerPerUser_kW { get; set; }
        /// <summary>
        /// A ratio∈[0,1] defining how much of the electrified road network that is actually covered by dynamic charging infrastructure. 
        /// If one quarter is covered followed by a three-quarters gap, the ratio is 0.25.
        /// </summary>
        public Dimensionless ErsCoverageRatio { get; set; }
        public KilometersPerHour ErsReferenceSpeed_kmph { get; set; }
        public Kilometers FinalErsNetworkScope_km { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("= Infrastructure offer =");
            foreach (var item in AvailablePowerPerUser_kW)
            {
                sb.AppendLine(item.Key + ":\t" + item.Value + " kW/user");
            }
            sb.AppendLine("ErsCoverageRatio: " + ErsCoverageRatio);
            sb.AppendLine("ErsReferenceSpeed_kmph: " + ErsReferenceSpeed_kmph);
            sb.AppendLine("FinalErsLength_km: " + FinalErsNetworkScope_km);
            return sb.ToString();
        }

        /// <summary>
        /// An equation estimating the AADT on the road segment with least traffic within the electrified road network.
        /// </summary>
        /// <returns>An estimate of the AADT on the road segment with least traffic within the electrified road network.</returns>
        public OtherUnit GetExpectedErsAadt()
        {
            //This equation is valid for heavy vehicles in Sweden
            //TODO: Modify this for other countries
            return new OtherUnit(FinalErsNetworkScope_km < 1000 ? 4000 : (float)(15000 - Math.Log(Math.Log(FinalErsNetworkScope_km.Val - 500)) * 6200));
        }
    }

    /// <summary>
    /// The useable battery capacity options that are available for vehicles of different classes.
    /// </summary>
    class BatteryOffers : Dictionary<VehicleType, KiloWattHours[]>
    {
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("= Available battery capacities =");
            foreach (var item in this)
            {
                sb.AppendLine(item.Key + " [ " + string.Join(", ", item.Value.Select(n => n.Val)) + " ]" );
            }
            return sb.ToString();
        }
    }
}
