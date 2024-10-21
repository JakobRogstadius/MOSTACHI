using System.Linq;
using System.Text;

namespace ScoreInfrastructurePlan
{
    class Route
    {
        public int ID { get; set; }
        public int VariantNo { get; set; }
        public Kilometers Length_km_total { get; private set; }
        public Kilometers Length_km_SE { get; private set; }
        public Hours Length_h_excl_breaks { get; private set; }
        private RouteSegment[] _segmentSequence;
        public RouteSegment[] SegmentSequence
        {
            get { return _segmentSequence; }
            set
            {
                _segmentSequence = value;
                Length_km_total = new Kilometers(value.Sum(n => n.LengthToTraverseOneWay_km.Val));
                Length_km_SE = new Kilometers(value.Sum(n => n.PlaceHash.IsInSweden ? n.LengthToTraverseOneWay_km.Val : 0));
                Length_h_excl_breaks = new Hours(value.Sum(n => n.LengthToTraverse_h.Val));
            }
        }

        public string ToString(ModelYear year)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var segment in _segmentSequence)
            {
                bool canCharge = segment.ChargingOfferedFromYear <= year;
                switch (segment.Type)
                {
                    case RouteSegmentType.Depot:
                        sb.Append(canCharge ? "<<" : "<");
                        break;
                    case RouteSegmentType.Destination:
                        sb.Append(canCharge ? ">>" : ">");
                        break;
                    case RouteSegmentType.Road:
                        sb.Append(canCharge ? '^' : '_');
                        break;
                    case RouteSegmentType.RestStop:
                        sb.Append(canCharge ? 'R' : 'r');
                        break;
                    default:
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
