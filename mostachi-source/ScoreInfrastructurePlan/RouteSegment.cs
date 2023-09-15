using static ScoreInfrastructurePlan.Parameters;
using System;
using Commons;

namespace ScoreInfrastructurePlan
{
    public enum RouteSegmentType { Depot, Destination, Road, RestStop };

    static class RouteSegmentTypeExtensions
    {
        public static bool CanStop(this RouteSegmentType type)
        {
            return type == RouteSegmentType.Depot || type == RouteSegmentType.RestStop || type == RouteSegmentType.Destination;
        }
    }

    class RouteSegment
    {
        //Most RouteSegment objects will represent what is elsewhere called a "cluster", i.e. a grouped sequence of
        //OSM ways. However, rest areas, depots and destinations are also represented as RouteSegments with length=0.

        public int ID { get; set; }
        public ElectricityPriceRegion Region { get; set; }
        public RouteSegmentType Type { get; set; }
        public ModelYear? ChargingOfferedFromYear { get; set; } = null;
        private KilometersPerHour _speed_kmph = new KilometersPerHour(50);
        public KilometersPerHour Speed_Kmph //This variable is too error prone and easily gets messed up
        {
            get
            {
                return _speed_kmph;
            }
            set
            {
                if (float.IsNaN(value.Val) || float.IsInfinity(value.Val) || value < 10 && Type == RouteSegmentType.Road)
                    _speed_kmph = new KilometersPerHour(50);
                else
                    _speed_kmph = value;
                LengthToTraverse_h = _speed_kmph == 0 ? new Hours(0) : LengthToTraverseOneWay_km / _speed_kmph;
            }
        }
        Kilometers _distance_km;
        public Kilometers LengthToTraverseOneWay_km
        {
            get
            {
                return _distance_km;
            }
            set
            {
                _distance_km = value;
                LengthToTraverse_h = _speed_kmph == 0 ? new Hours(0) : LengthToTraverseOneWay_km / _speed_kmph;
            }
        }
        public Kilometers LaneLengthToElectrifyOneOrBothWays_km { get { return LengthToTraverseOneWay_km * new Dimensionless(IsBidirectional ? 2 : 1); } }
        public Kilometers BidirectionalRoadLengthToElectrify_km { get { return LengthToTraverseOneWay_km * new Dimensionless(IsBidirectional ? 1 : 0.5f); } }
        public Hours LengthToTraverse_h { get; private set; }
        public CoordinateHash PlaceHash { get; set; }
        public bool IsBidirectional { get; set; }

        public override string ToString()
        {
            switch (Type)
            {
                case RouteSegmentType.Depot:
                    return "Depot (" + ChargingOfferedFromYear + ")";
                case RouteSegmentType.Destination:
                    return "Destination (" + ChargingOfferedFromYear + ")";
                case RouteSegmentType.Road:
                    return "Road (" + ChargingOfferedFromYear + ", " + LengthToTraverseOneWay_km + " km, " + LengthToTraverse_h + " h)";
                case RouteSegmentType.RestStop:
                    return "Rest (" + ChargingOfferedFromYear + ")";
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
