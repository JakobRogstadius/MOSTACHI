using System.Collections.Generic;

namespace ScoreInfrastructurePlan
{
    class CostPerRouteTraversal
    {
        public Dimensionless BatteryAgeing_Cycling_RatioOfLifetime { get; set; } = new Dimensionless(0);
        public Dimensionless BatteryAgeing_Calendar_RatioOfReferenceLifetime { get; set; } = new Dimensionless(0);
        public Dimensionless BatteryAgeing_Total_RatioOfReferenceLifetime { get { return BatteryAgeing_Calendar_RatioOfReferenceLifetime + BatteryAgeing_Cycling_RatioOfLifetime; } }
        public Euro ElectricityPurchased_Euro { get; set; } = new Euro(0);
        public KiloWattHours ElectricitySpent_kWh { get; set; } = new KiloWattHours(0);
        public KiloWattHours ElectricityPurchased_kWh { get; set; } = new KiloWattHours(0);
        public KiloWattHours ElectricityPurchasePotential_kWh { get; set; } = new KiloWattHours(0);
        public Hours Traversal_h_Abroad { get; set; } = new Hours(0);
        public Hours Traversal_h_InSweden { get; set; } = new Hours(0);
        public Hours Traversal_h_Total { get { return Traversal_h_Abroad + Traversal_h_InSweden; } }
        public Hours OfWhichIsDelay_h { get; set; } = new Hours(0);
        public Liter Diesel_liter { get; set; } = new Liter(0);
        public Kilogram CO2_kg { get; set; } = new Kilogram(0);
        public Euro InfraFees_euro_approximate { get; set; } = new Euro(0);
        public HashSet<RouteSegmentType> UsedChargerTypes { get; private set; } = new HashSet<RouteSegmentType>();

        public void Add(CostPerRouteTraversal other)
        {
            this.BatteryAgeing_Calendar_RatioOfReferenceLifetime += other.BatteryAgeing_Calendar_RatioOfReferenceLifetime;
            this.BatteryAgeing_Cycling_RatioOfLifetime += other.BatteryAgeing_Cycling_RatioOfLifetime;
            this.ElectricityPurchased_Euro += other.ElectricityPurchased_Euro;
            this.Traversal_h_InSweden += other.Traversal_h_InSweden;
            this.Traversal_h_Abroad += other.Traversal_h_Abroad;
            this.OfWhichIsDelay_h += other.OfWhichIsDelay_h;
            this.ElectricitySpent_kWh += other.ElectricitySpent_kWh;
            this.ElectricityPurchased_kWh += other.ElectricityPurchased_kWh;
            this.Diesel_liter += other.Diesel_liter;
            this.CO2_kg += other.CO2_kg;
            this.InfraFees_euro_approximate += other.InfraFees_euro_approximate;
            this.UsedChargerTypes.UnionWith(other.UsedChargerTypes);
        }

        public void IncreaseBySkippedRatioAbroad(Dimensionless abroadRatioOfTotal, Hours hDepot, bool depotWasAbroad, CostPerRouteTraversal depotCost)
        {
            //Any charging that took place at the depot should not be scaled. Only the part along the route.
            //Also, the cost of energy might be different abroad.

            Dimensionless scale = abroadRatioOfTotal / (1 - abroadRatioOfTotal);

            this.BatteryAgeing_Calendar_RatioOfReferenceLifetime += (BatteryAgeing_Calendar_RatioOfReferenceLifetime - depotCost.BatteryAgeing_Cycling_RatioOfLifetime) * scale;
            this.BatteryAgeing_Cycling_RatioOfLifetime += (BatteryAgeing_Cycling_RatioOfLifetime - depotCost.BatteryAgeing_Cycling_RatioOfLifetime) * scale;
            this.ElectricityPurchased_Euro += (ElectricityPurchased_Euro - depotCost.ElectricityPurchased_Euro) * scale;
            this.Traversal_h_Abroad += depotWasAbroad ? Traversal_h_InSweden * scale : (Traversal_h_InSweden - hDepot) * scale;
            this.OfWhichIsDelay_h += (OfWhichIsDelay_h - depotCost.OfWhichIsDelay_h) * scale;
            this.ElectricitySpent_kWh += (ElectricitySpent_kWh - depotCost.ElectricitySpent_kWh) * scale;
            this.ElectricityPurchased_kWh += (ElectricityPurchased_kWh - depotCost.ElectricityPurchased_kWh) * scale;
            this.Diesel_liter += (Diesel_liter - depotCost.Diesel_liter) * scale;
            this.CO2_kg += (CO2_kg - depotCost.CO2_kg) * scale;
            this.InfraFees_euro_approximate += (InfraFees_euro_approximate - depotCost.InfraFees_euro_approximate) * scale; ;
        }

        //This normalization is important. Different charging rates result in net differences of start vs. end charge along the route. This 
        //effectively means that the energy consumption changes, which is incorrect. By normalizing to the energy consumption per traversal, the 
        //distribution of costs is retained, and the costs themselves are scaled to be correct.
        public (CostPerRouteTraversal cost, Dimensionless scaleFactor) GetNormalized(KiloWattHours total_kWh)
        {
            if (ElectricityPurchased_kWh == 0)
                return (this, new Dimensionless(1));

            Dimensionless r = total_kWh / ElectricityPurchased_kWh;

            if (this.Diesel_liter > 0)
                throw new System.NotImplementedException("Can't handle this yet");

            return (new CostPerRouteTraversal()
            {
                //Electricity spent

                BatteryAgeing_Cycling_RatioOfLifetime = this.BatteryAgeing_Cycling_RatioOfLifetime * r, //TODO: Technically, the wear would be different as the c-rate would be different
                BatteryAgeing_Calendar_RatioOfReferenceLifetime = this.BatteryAgeing_Calendar_RatioOfReferenceLifetime, //unchanged
                ElectricityPurchased_Euro = this.ElectricityPurchased_Euro * r,
                ElectricityPurchased_kWh = this.ElectricityPurchased_kWh * r,
                Traversal_h_InSweden = this.Traversal_h_InSweden, //unchanged
                Traversal_h_Abroad = this.Traversal_h_Abroad, //unchanged
                OfWhichIsDelay_h = this.OfWhichIsDelay_h, //unchanged
                Diesel_liter = this.Diesel_liter, //n/a
                CO2_kg = this.CO2_kg * r,
                InfraFees_euro_approximate = this.InfraFees_euro_approximate * r,
                UsedChargerTypes = new HashSet<RouteSegmentType>(UsedChargerTypes)
            }, r);
        }
    }
}
