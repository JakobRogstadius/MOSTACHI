using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;

namespace ScoreInfrastructurePlan
{
    enum ChargingStrategy
    {
        NA_Diesel = 0,
        Depot = 1, //Electricity is cheapest at night
        Ers = 2, //Can we make it without paying for a depot charger?
        DepotAndErsPropulsion = 4,
        DepotAndErsCharging = 5,
        AllPlannedStops = 6, //Wait to charge at depot, charge while waiting at rest stops and destinations
        AllPlannedStopsAndErs = 7,
        AllPlannedAndExtraChargingStops = 8,
        PublicStaticCharging = 9
    }

    enum ChargingMode
    {
        None = 0,
        Propulsion = 1,
        Charging = 2
    }

    static class ChargingStrategyExtensions
    {
        public static ChargingMode ChargeIfPossible(this ChargingStrategy strategy, RouteSegmentType place, bool isPlannedRestStop, Dimensionless soc)
        {
            ChargingMode mode = ChargingMode.None;
            switch (strategy)
            {
                case ChargingStrategy.Depot:
                    mode = place == RouteSegmentType.Depot
                        ? ChargingMode.Charging : ChargingMode.None;
                    break;
                case ChargingStrategy.AllPlannedStops:
                    mode = place == RouteSegmentType.Depot || (isPlannedRestStop && place == RouteSegmentType.RestStop) || place == RouteSegmentType.Destination
                        ? ChargingMode.Charging : ChargingMode.None;
                    break;
                case ChargingStrategy.Ers:
                    mode = place == RouteSegmentType.Road
                        ? ChargingMode.Charging : ChargingMode.None;
                    break;
                case ChargingStrategy.DepotAndErsPropulsion:
                    mode = place == RouteSegmentType.Road ? ChargingMode.Propulsion : place == RouteSegmentType.Depot ? ChargingMode.Charging : ChargingMode.None;
                    break;
                case ChargingStrategy.DepotAndErsCharging:
                    mode = place == RouteSegmentType.Road || place == RouteSegmentType.Depot
                        ? ChargingMode.Charging : ChargingMode.None;
                    break;
                case ChargingStrategy.AllPlannedStopsAndErs:
                    mode = place != RouteSegmentType.RestStop || (isPlannedRestStop && place == RouteSegmentType.RestStop)
                        ? ChargingMode.Charging : ChargingMode.None;
                    break;
                case ChargingStrategy.PublicStaticCharging:
                    mode = (place == RouteSegmentType.Destination) || (isPlannedRestStop && place == RouteSegmentType.RestStop)
                        ? ChargingMode.Charging : ChargingMode.None;
                    break;
                case ChargingStrategy.AllPlannedAndExtraChargingStops:
                    mode = place == RouteSegmentType.Depot || ((isPlannedRestStop || soc < 0.8f) && place == RouteSegmentType.RestStop) || place == RouteSegmentType.Destination
                        ? ChargingMode.Charging : ChargingMode.None;
                    break;
                case ChargingStrategy.NA_Diesel:
                    mode = ChargingMode.None;
                    break;
                default:
                    throw new NotImplementedException();
            }
            if (mode == ChargingMode.Charging && soc == 1)
                mode = ChargingMode.Propulsion;
            return mode;
        }

        public static ChargingStrategy GetWithoutDepot(this ChargingStrategy strategy)
        {
            switch (strategy)
            {
                case ChargingStrategy.AllPlannedStops:
                    return ChargingStrategy.PublicStaticCharging;
                case ChargingStrategy.DepotAndErsCharging:
                    return ChargingStrategy.Ers;
                default:
                    return strategy;
            }
        }

        public static ChargingStrategy GetWithoutErs(this ChargingStrategy strategy)
        {
            switch (strategy)
            {
                case ChargingStrategy.Ers:
                    return ChargingStrategy.NA_Diesel;
                case ChargingStrategy.DepotAndErsCharging:
                    return ChargingStrategy.Depot;
                case ChargingStrategy.AllPlannedStopsAndErs:
                    return ChargingStrategy.AllPlannedStops;
                default:
                    return strategy;
            }
        }

        public static (Hours minTime_h, bool waitToCharge) DwellTime_h(this ChargingStrategy strategy, ModelYear year, RouteSegment segment, VehicleType vtype, Hours routeLength_h, bool isPlannedStop)
        {
            //Do I allow planned charging stops to end when the battery is full?
            RouteSegmentType rstype = segment.Type;
            bool waitToCharge = WaitToCharge(strategy, rstype);

            if (rstype == RouteSegmentType.Road)
            {
                if (isPlannedStop)
                    return (vtype.Common_Rest_stop_h[year], false);
                else
                    return (segment.LengthToTraverse_h, false);
            }
            else if (rstype == RouteSegmentType.RestStop)
                return (isPlannedStop ? vtype.Common_Rest_stop_h[year] : new Hours(0), waitToCharge);
            else if (rstype == RouteSegmentType.Depot)
                return (vtype.GetNormalizedDepotTime_h(year, routeLength_h), waitToCharge);
            else if (rstype == RouteSegmentType.Destination)
                return (vtype.Common_Destination_stop_h[year], waitToCharge);
            else
                throw new NotImplementedException();
        }

        public static bool WaitToCharge(this ChargingStrategy strategy, RouteSegmentType place)
        {
            //Currently, no strategy allows extra charging stops.
            switch (strategy)
            {
                case ChargingStrategy.AllPlannedAndExtraChargingStops:
                    return place != RouteSegmentType.Road;
                case ChargingStrategy.NA_Diesel:
                case ChargingStrategy.Depot:
                case ChargingStrategy.AllPlannedStops:
                case ChargingStrategy.Ers:
                case ChargingStrategy.DepotAndErsPropulsion:
                case ChargingStrategy.DepotAndErsCharging:
                case ChargingStrategy.AllPlannedStopsAndErs:
                case ChargingStrategy.PublicStaticCharging:
                    return false;
                default:
                    throw new NotImplementedException();
            }
        }

        public static bool HasErsPickup(this ChargingStrategy strategy)
        {
            switch (strategy)
            {
                case ChargingStrategy.Ers:
                case ChargingStrategy.DepotAndErsPropulsion:
                case ChargingStrategy.DepotAndErsCharging:
                case ChargingStrategy.AllPlannedStopsAndErs:
                    return true;
                case ChargingStrategy.Depot:
                case ChargingStrategy.AllPlannedStops:
                case ChargingStrategy.NA_Diesel:
                case ChargingStrategy.AllPlannedAndExtraChargingStops:
                case ChargingStrategy.PublicStaticCharging:
                    return false;
                default:
                    throw new NotImplementedException();
            }
        }

        public static bool RestEarlierToCharge(this ChargingStrategy strategy)
        {
            switch (strategy)
            {
                case ChargingStrategy.AllPlannedStops:
                case ChargingStrategy.AllPlannedStopsAndErs:
                case ChargingStrategy.AllPlannedAndExtraChargingStops:
                    return true;
                case ChargingStrategy.NA_Diesel:
                case ChargingStrategy.Depot:
                case ChargingStrategy.Ers:
                case ChargingStrategy.DepotAndErsPropulsion:
                case ChargingStrategy.DepotAndErsCharging:
                case ChargingStrategy.PublicStaticCharging:
                    return false;
                default:
                    throw new NotImplementedException();
            }
        }

        public static bool TakeExtraChargingStops(this ChargingStrategy strategy)
        {
            switch (strategy)
            {
                case ChargingStrategy.AllPlannedAndExtraChargingStops:
                    return true;
                case ChargingStrategy.AllPlannedStops:
                case ChargingStrategy.AllPlannedStopsAndErs:
                case ChargingStrategy.NA_Diesel:
                case ChargingStrategy.Depot:
                case ChargingStrategy.Ers:
                case ChargingStrategy.DepotAndErsPropulsion:
                case ChargingStrategy.DepotAndErsCharging:
                case ChargingStrategy.PublicStaticCharging:
                    return false;
                default:
                    throw new NotImplementedException();
            }
        }

        public static bool IsMultistopStrategy(this ChargingStrategy strategy)
        {
            switch (strategy)
            {
                case ChargingStrategy.AllPlannedAndExtraChargingStops:
                case ChargingStrategy.AllPlannedStops:
                case ChargingStrategy.AllPlannedStopsAndErs:
                case ChargingStrategy.PublicStaticCharging:
                    return true;
                case ChargingStrategy.NA_Diesel:
                case ChargingStrategy.Depot:
                case ChargingStrategy.Ers:
                case ChargingStrategy.DepotAndErsPropulsion:
                case ChargingStrategy.DepotAndErsCharging:
                    return false;
                default:
                    throw new NotImplementedException();
            }
        }

        public static bool UsesDepot(this ChargingStrategy strategy)
        {
            switch (strategy)
            {
                case ChargingStrategy.AllPlannedStops:
                case ChargingStrategy.AllPlannedStopsAndErs:
                case ChargingStrategy.DepotAndErsPropulsion:
                case ChargingStrategy.DepotAndErsCharging:
                case ChargingStrategy.Depot:
                case ChargingStrategy.AllPlannedAndExtraChargingStops:
                    return true;
                case ChargingStrategy.Ers:
                case ChargingStrategy.NA_Diesel:
                case ChargingStrategy.PublicStaticCharging:
                    return false;
                default:
                    throw new NotImplementedException();
            }
        }

        public static bool UsesErs(this ChargingStrategy strategy)
        {
            switch (strategy)
            {
                case ChargingStrategy.DepotAndErsCharging:
                case ChargingStrategy.AllPlannedStopsAndErs:
                case ChargingStrategy.DepotAndErsPropulsion:
                case ChargingStrategy.Ers:
                    return true;
                case ChargingStrategy.NA_Diesel:
                case ChargingStrategy.AllPlannedStops:
                case ChargingStrategy.Depot:
                case ChargingStrategy.PublicStaticCharging:
                case ChargingStrategy.AllPlannedAndExtraChargingStops:
                    return false;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
