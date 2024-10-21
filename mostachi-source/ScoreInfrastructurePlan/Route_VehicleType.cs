namespace ScoreInfrastructurePlan
{
    class Route_VehicleType
    {
        public VehicleType VehicleType { get; set; }
        public Route Route { get; set; }
        public OtherUnit LoadedTripCountPerYear2020 { get; set; }
        public OtherUnit EmptyTripCountPerYear2020 { get; set; }
        public OtherUnit GetTotalTripCountPerYear(ModelYear year) { return new OtherUnit(LoadedTripCountPerYear2020.Val + EmptyTripCountPerYear2020.Val) * Parameters.World.Economy_Heavy_traffic_volume_vs_2020_percent[year]; }
        public OtherUnit GetLoadedTripCountPerYear(ModelYear year) { return LoadedTripCountPerYear2020 * Parameters.World.Economy_Heavy_traffic_volume_vs_2020_percent[year]; }
        public OtherUnit CargoTonnesPerYear2020 { get; set; }
        public OtherUnit GetCargoTonnesPerYear(ModelYear year) { return CargoTonnesPerYear2020 * Parameters.World.Economy_Heavy_traffic_volume_vs_2020_percent[year]; }

        public bool IsOk()
        {
            //TODO: These thresholds remove noise, which means the data may not add up to 100% of the expected

            return Route.Length_km_SE > 1
                && (LoadedTripCountPerYear2020 + EmptyTripCountPerYear2020) > 1
                && Route.SegmentSequence.Length > 2;
        }
    }
}
