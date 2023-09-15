using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ScoreInfrastructurePlan
{
    //Rolling my own is MADNESS! Yes, but the libraries I found had terrible performance. This is about 10 times slower than using pure floats, while other libraries were 10-100 times worse than my solution.

    public class UnitList<T> : List<T> where T : HasVal<T>
    {
        public void Add(float v) { base.Add((T)Activator.CreateInstance(typeof(T), v)); }
    }
    public static class UnitMath
    {
        public static T Min<T>(T a, T b) where T : HasVal<T> { return a.CreateNewInstance(Math.Min(a.Val, b.Val)); }
        public static T Max<T>(T a, T b) where T : HasVal<T> { return a.CreateNewInstance(Math.Max(a.Val, b.Val)); }
        public static T Ceiling<T>(T a) where T : HasVal<T> { return a.CreateNewInstance((float)Math.Ceiling(a.Val)); }
        public static T Floor<T>(T a) where T : HasVal<T> { return a.CreateNewInstance((float)Math.Floor(a.Val)); }
        public static T Abs<T>(T a) where T : HasVal<T> { return a.CreateNewInstance(Math.Abs(a.Val)); }
    }
    public abstract class HasVal<T> : IComparable<HasVal<T>>, IEquatable<HasVal<T>>
    {
        public readonly float Val;
        public HasVal(float value)
        {
            Val = value;
        }
        public abstract T CreateNewInstance(float v);
        public override string ToString()
        {
            return Val.ToString();
        }
        public override bool Equals(object obj) { return Equals(obj as HasVal<T>); }
        public bool Equals([AllowNull] HasVal<T> other) { return !(other is null) && other.Val == Val; }
        public int CompareTo([AllowNull] HasVal<T> other) { return (other is null) ? 1 : Val.CompareTo(other.Val); }
        public override int GetHashCode() { return Val.GetHashCode(); }
        public static Dimensionless operator /(HasVal<T> a, HasVal<T> b) { return new Dimensionless(a.Val / b.Val); }
        public static T operator +(HasVal<T> a, HasVal<T> b) { return a.CreateNewInstance(a.Val + b.Val); }
        public static T operator -(HasVal<T> a, HasVal<T> b) { return a.CreateNewInstance(a.Val - b.Val); }
        public static T operator /(HasVal<T> a, Dimensionless b) { return a.CreateNewInstance(a.Val / b.Val); }
        public static T operator *(HasVal<T> a, Dimensionless b) { return a.CreateNewInstance(a.Val * b.Val); }
        public static bool operator ==(HasVal<T> a, HasVal<T> b) { return a.Val == b.Val; }
        public static bool operator !=(HasVal<T> a, HasVal<T> b) { return a.Val != b.Val; }
        public static bool operator >(HasVal<T> a, HasVal<T> b) { return a.Val > b.Val; }
        public static bool operator <(HasVal<T> a, HasVal<T> b) { return a.Val < b.Val; }
        public static bool operator >=(HasVal<T> a, HasVal<T> b) { return a.Val >= b.Val; }
        public static bool operator <=(HasVal<T> a, HasVal<T> b) { return a.Val <= b.Val; }
        public static bool operator >(float a, HasVal<T> b) { return a > b.Val; }
        public static bool operator <(float a, HasVal<T> b) { return a < b.Val; }
        public static bool operator >(HasVal<T> a, float b) { return a.Val > b; }
        public static bool operator <(HasVal<T> a, float b) { return a.Val < b; }
        public static bool operator ==(HasVal<T> a, float b) { return a.Val == b; }
        public static bool operator !=(HasVal<T> a, float b) { return a.Val != b; }
        public static bool operator ==(float a, HasVal<T> b) { return a == b.Val; }
        public static bool operator !=(float a, HasVal<T> b) { return a != b.Val; }
        public static bool operator ==(HasVal<T> a, object b) { return b != null && a == b; }
        public static bool operator !=(HasVal<T> a, object b) { return b != null && a != b; }
        public static bool operator ==(object a, HasVal<T> b) { return a != null && a == b; }
        public static bool operator !=(object a, HasVal<T> b) { return a != null && a != b; }
    }
    public class Kilometers : HasVal<Kilometers>
    {
        public Kilometers(float v) : base(v) { }
        public override Kilometers CreateNewInstance(float v) { return new Kilometers(v); }
        public static KilometersPerHour operator /(Kilometers a, Hours b) { return new KilometersPerHour(a.Val / b.Val); }
        public static KilometersPerYear operator /(Kilometers a, Years b) { return new KilometersPerYear(a.Val / b.Val); }
    }
    public class Hours : HasVal<Hours>
    {
        public Hours(float v) : base(v) { }
        public override Hours CreateNewInstance(float v) { return new Hours(v); }
    }
    public class Years : HasVal<Years>
    {
        public Hours ToHours() { return new Hours(Val / (365 * 24)); }
        public Years(float v) : base(v) { }
        public override Years CreateNewInstance(float v) { return new Years(v); }
    }
    public class CRate : HasVal<CRate>
    {
        public CRate(float v) : base(v) { }
        public override CRate CreateNewInstance(float v) { return new CRate(v); }
        public static KiloWatts operator *(KiloWattHours a, CRate b) { return new KiloWatts(a.Val * b.Val); }
        public static KiloWattHours operator /(KiloWatts a, CRate b) { return new KiloWattHours(a.Val / b.Val); }
    }
    public class KiloWatts : HasVal<KiloWatts>
    {
        public KiloWatts(float v) : base(v) { }
        public override KiloWatts CreateNewInstance(float v) { return new KiloWatts(v); }
        public static KiloWattHours operator *(KiloWatts a, Hours b) { return new KiloWattHours(a.Val * b.Val); }
        public static KiloWattsPerKilometer operator /(KiloWatts a, Kilometers b) { return new KiloWattsPerKilometer(a.Val / b.Val); }
    }
    public class KiloWattsPerKilometer : HasVal<KiloWattsPerKilometer>
    {
        public KiloWattsPerKilometer(float v) : base(v) { }
        public override KiloWattsPerKilometer CreateNewInstance(float v) { return new KiloWattsPerKilometer(v); }
        public static KiloWatts operator *(KiloWattsPerKilometer a, Kilometers b) { return new KiloWatts(a.Val * b.Val); }
    }
    public class Kilogram : HasVal<Kilogram>
    {
        public Kilogram(float v) : base(v) { }
        public override Kilogram CreateNewInstance(float v) { return new Kilogram(v); }
        public Tonnes ToTonnes() { return new Tonnes(Val * 1e-3f); }
        public static KilogramPerKilometer operator /(Kilogram a, Kilometers b) { return new KilogramPerKilometer(a.Val / b.Val); }
    }
    public class Tonnes : HasVal<Tonnes>
    {
        public Tonnes(float v) : base(v) { }
        public override Tonnes CreateNewInstance(float v) { return new Tonnes(v); }
        public Tonnes ToKilogram() { return new Tonnes(Val * 1e3f); }
        public static TonKilometers operator *(Tonnes a, Kilometers b) { return new TonKilometers(a.Val * b.Val); }
        public static TonKilometersPerYear operator *(Tonnes a, KilometersPerYear b) { return new TonKilometersPerYear(a.Val * b.Val); }
    }
    public class TonnesPerYear : HasVal<TonnesPerYear>
    {
        public TonnesPerYear(float v) : base(v) { }
        public override TonnesPerYear CreateNewInstance(float v) { return new TonnesPerYear(v); }
        public static EuroPerYear operator *(TonnesPerYear a, EuroPerKilogram b) { return new EuroPerYear(a.Val * b.Val * 1000); }
    }
    public class Liter : HasVal<Liter>
    {
        public Liter(float v) : base(v) { }
        public override Liter CreateNewInstance(float v) { return new Liter(v); }
    }
    public class KilometersPerHour : HasVal<KilometersPerHour>
    {
        public KilometersPerHour(float v) : base(v) { }
        public override KilometersPerHour CreateNewInstance(float v) { return new KilometersPerHour(v); }
        public static Kilometers operator *(KilometersPerHour a, Hours b) { return new Kilometers(a.Val * b.Val); }
        public static Hours operator /(Kilometers a, KilometersPerHour b) { return new Hours(a.Val / b.Val); }
    }
    public class KilometersPerYear : HasVal<KilometersPerYear>
    {
        public KilometersPerYear(float v) : base(v) { }
        public override KilometersPerYear CreateNewInstance(float v) { return new KilometersPerYear(v); }
        public static Kilometers operator *(KilometersPerYear a, Years b) { return new Kilometers(a.Val * b.Val); }
        public static Years operator /(Kilometers a, KilometersPerYear b) { return new Years(a.Val / b.Val); }
        public static EuroPerKilometer operator /(EuroPerYear a, KilometersPerYear b) { return new EuroPerKilometer(a.Val / b.Val); }
    }
    public class TonKilometersPerYear : HasVal<TonKilometersPerYear>
    {
        public TonKilometersPerYear(float v) : base(v) { }
        public override TonKilometersPerYear CreateNewInstance(float v) { return new TonKilometersPerYear(v); }
        public static TonKilometers operator *(TonKilometersPerYear a, Years b) { return new TonKilometers(a.Val * b.Val); }
    }
    public class KiloWattHours : HasVal<KiloWattHours>
    {
        public KiloWattHours(float v) : base(v) { }
        public override KiloWattHours CreateNewInstance(float v) { return new KiloWattHours(v); }
        public static KiloWatts operator /(KiloWattHours a, Hours b) { return new KiloWatts(a.Val / b.Val); }
        public static Hours operator /(KiloWattHours a, KiloWatts b) { return new Hours(a.Val / b.Val); }
        public static CRate operator /(KiloWatts a, KiloWattHours b) { return new CRate(a.Val / b.Val); }
        public static KiloWattHoursPerKilometer operator /(KiloWattHours a, Kilometers b) { return new KiloWattHoursPerKilometer(a.Val / b.Val); }
    }
    public class KiloWattHoursPerKilometerYear : HasVal<KiloWattHoursPerKilometerYear>
    {
        public KiloWattHoursPerKilometerYear(float v) : base(v) { }
        public override KiloWattHoursPerKilometerYear CreateNewInstance(float v) { return new KiloWattHoursPerKilometerYear(v); }
    }
    public class KiloWattHoursPerKilogram : HasVal<KiloWattHoursPerKilogram>
    {
        public KiloWattHoursPerKilogram(float v) : base(v) { }
        public override KiloWattHoursPerKilogram CreateNewInstance(float v) { return new KiloWattHoursPerKilogram(v); }
        public static Kilogram operator /(KiloWattHours a, KiloWattHoursPerKilogram b) { return new Kilogram(a.Val / b.Val); }
    }
    public class TonKilometers : HasVal<TonKilometers>
    {
        public TonKilometers(float v) : base(v) { }
        public override TonKilometers CreateNewInstance(float v) { return new TonKilometers(v); }
    }
    public class Euro : HasVal<Euro>
    {
        public Euro(float v) : base(v) { }
        public override Euro CreateNewInstance(float v) { return new Euro(v); }
        public static EuroPerKilometer operator /(Euro a, Kilometers b) { return new EuroPerKilometer(a.Val / b.Val); }
        public static EuroPerHour operator /(Euro a, Hours b) { return new EuroPerHour(a.Val / b.Val); }
        public static EuroPerYear operator /(Euro a, Years b) { return new EuroPerYear(a.Val / b.Val); }
    }
    public class EuroPerKilometer : HasVal<EuroPerKilometer>
    {
        public EuroPerKilometer(float v) : base(v) { }
        public override EuroPerKilometer CreateNewInstance(float v) { return new EuroPerKilometer(v); }
        public static Euro operator *(EuroPerKilometer a, Kilometers b) { return new Euro(a.Val * b.Val); }
        public static EuroPerKilometerYear operator /(EuroPerKilometer a, Years b) { return new EuroPerKilometerYear(a.Val / b.Val); }
        public static EuroPerYear operator *(EuroPerKilometer a, KilometersPerYear b) { return new EuroPerYear(a.Val * b.Val); }
    }
    public class EuroPerHour : HasVal<EuroPerHour>
    {
        public EuroPerHour(float v) : base(v) { }
        public override EuroPerHour CreateNewInstance(float v) { return new EuroPerHour(v); }
        public static Euro operator *(EuroPerHour a, Hours b) { return new Euro(a.Val * b.Val); }
    }
    public class EuroPerYear : HasVal<EuroPerYear>
    {
        public EuroPerYear(float v) : base(v) { }
        public override EuroPerYear CreateNewInstance(float v) { return new EuroPerYear(v); }
        public static Euro operator *(EuroPerYear a, Years b) { return new Euro(a.Val * b.Val); }
    }
    public class EuroPerKilogram : HasVal<EuroPerKilogram>
    {
        public EuroPerKilogram(float v) : base(v) { }
        public override EuroPerKilogram CreateNewInstance(float v) { return new EuroPerKilogram(v); }
        public static Euro operator *(EuroPerKilogram a, Kilogram b) { return new Euro(a.Val * b.Val); }
        public static EuroPerKiloWattHour operator *(EuroPerKilogram a, KilogramPerKiloWattHour b) { return new EuroPerKiloWattHour(a.Val * b.Val); }
        public static EuroPerLiter operator *(EuroPerKilogram a, KilogramPerLiter b) { return new EuroPerLiter(a.Val * b.Val); }
    }
    public class EuroPerLiter : HasVal<EuroPerLiter>
    {
        public EuroPerLiter(float v) : base(v) { }
        public override EuroPerLiter CreateNewInstance(float v) { return new EuroPerLiter(v); }
        public static Euro operator *(EuroPerLiter a, Liter b) { return new Euro(a.Val * b.Val); }
        public static EuroPerKilometer operator *(EuroPerLiter a, LiterPerKilometer b) { return new EuroPerKilometer(a.Val * b.Val); }
        public static LiterPerYear operator /(EuroPerYear a, EuroPerLiter b) { return new LiterPerYear(a.Val / b.Val); }
        public static EuroPerYear operator *(LiterPerYear b, EuroPerLiter a) { return new EuroPerYear(a.Val * b.Val); }
    }
    public class EuroPerTonKilometer : HasVal<EuroPerTonKilometer>
    {
        public EuroPerTonKilometer(float v) : base(v) { }
        public override EuroPerTonKilometer CreateNewInstance(float v) { return new EuroPerTonKilometer(v); }
    }
    public class EuroPerKilometerYear : HasVal<EuroPerKilometerYear>
    {
        public EuroPerKilometerYear(float v) : base(v) { }
        public override EuroPerKilometerYear CreateNewInstance(float v) { return new EuroPerKilometerYear(v); }
        public static EuroPerYear operator *(EuroPerKilometerYear a, Kilometers b) { return new EuroPerYear(a.Val * b.Val); }
        public static EuroPerKiloWattHour operator /(EuroPerKilometerYear a, KiloWattHoursPerKilometerYear b) { return new EuroPerKiloWattHour(a.Val / b.Val); }
    }
    public class EuroPerCubicMeter : HasVal<EuroPerCubicMeter>
    {
        public EuroPerCubicMeter(float v) : base(v) { }
        public override EuroPerCubicMeter CreateNewInstance(float v) { return new EuroPerCubicMeter(v); }
    }
    public class EuroPerKiloWatt : HasVal<EuroPerKiloWatt>
    {
        public EuroPerKiloWatt(float v) : base(v) { }
        public override EuroPerKiloWatt CreateNewInstance(float v) { return new EuroPerKiloWatt(v); }
        public static Euro operator *(EuroPerKiloWatt a, KiloWatts b) { return new Euro(a.Val * b.Val); }
        public static EuroPerKilometer operator *(EuroPerKiloWatt a, KiloWattsPerKilometer b) { return new EuroPerKilometer(a.Val * b.Val); }
    }
    public class EuroPerKiloWattHour : HasVal<EuroPerKiloWattHour>
    {
        public EuroPerKiloWattHour(float v) : base(v) { }
        public override EuroPerKiloWattHour CreateNewInstance(float v) { return new EuroPerKiloWattHour(v); }
        public static Euro operator *(EuroPerKiloWattHour a, KiloWattHours b) { return new Euro(a.Val * b.Val); }
        public static EuroPerKilometerYear operator *(EuroPerKiloWattHour a, KiloWattHoursPerKilometerYear b) { return new EuroPerKilometerYear(a.Val * b.Val); }
        public static EuroPerKilometer operator *(EuroPerKiloWattHour a, KiloWattHoursPerKilometer b) { return new EuroPerKilometer(a.Val * b.Val); }
    }
    public class EuroPerKiloWattKilometer : HasVal<EuroPerKiloWattKilometer>
    {
        public EuroPerKiloWattKilometer(float v) : base(v) { }
        public override EuroPerKiloWattKilometer CreateNewInstance(float v) { return new EuroPerKiloWattKilometer(v); }
        public static Euro operator *(EuroPerKiloWattKilometer a, Kilometers b) { return new Euro(a.Val * b.Val); }
        public static EuroPerKilometer operator *(EuroPerKiloWattKilometer a, KiloWattsPerKilometer b) { return new EuroPerKilometer(a.Val * b.Val); }
    }
    public class LiterPerKilometer : HasVal<LiterPerKilometer>
    {
        public LiterPerKilometer(float v) : base(v) { }
        public override LiterPerKilometer CreateNewInstance(float v) { return new LiterPerKilometer(v); }
        public static Liter operator *(LiterPerKilometer a, Kilometers b) { return new Liter(a.Val * b.Val); }
    }
    public class LiterPerYear : HasVal<LiterPerYear>
    {
        public LiterPerYear(float v) : base(v) { }
        public override LiterPerYear CreateNewInstance(float v) { return new LiterPerYear(v); }
        public static TonnesPerYear operator *(LiterPerYear a, KilogramPerLiter b) { return new TonnesPerYear(a.Val * b.Val / 1000); }
    }
    public class KiloWattHoursPerKilometer : HasVal<KiloWattHoursPerKilometer>
    {
        public KiloWattHoursPerKilometer(float v) : base(v) { }
        public override KiloWattHoursPerKilometer CreateNewInstance(float v) { return new KiloWattHoursPerKilometer(v); }
        public static KiloWatts operator *(KiloWattHoursPerKilometer a, KilometersPerHour b) { return new KiloWatts(a.Val * b.Val); }
        public static KiloWattHours operator *(KiloWattHoursPerKilometer a, Kilometers b) { return new KiloWattHours(a.Val * b.Val); }
    }
    public class KiloWattHoursPerYear : HasVal<KiloWattHoursPerYear>
    {
        public KiloWattHoursPerYear(float v) : base(v) { }
        public override KiloWattHoursPerYear CreateNewInstance(float v) { return new KiloWattHoursPerYear(v); }
        public static KiloWattHours operator *(KiloWattHoursPerYear a, Years b) { return new KiloWattHours(a.Val * b.Val); }
        public static KiloWattHoursPerKilometerYear operator /(KiloWattHoursPerYear a, Kilometers b) { return new KiloWattHoursPerKilometerYear(a.Val / b.Val); }
        public static EuroPerKiloWattHour operator /(EuroPerYear a, KiloWattHoursPerYear b) { return new EuroPerKiloWattHour(a.Val / b.Val); }

    }
    public class KiloWattHoursPerLiter : HasVal<KiloWattHoursPerLiter>
    {
        public KiloWattHoursPerLiter(float v) : base(v) { }
        public override KiloWattHoursPerLiter CreateNewInstance(float v) { return new KiloWattHoursPerLiter(v); }
        public static KiloWattHours operator *(KiloWattHoursPerLiter a, Liter b) { return new KiloWattHours(a.Val * b.Val); }
    }
    public class KilogramPerKiloWattHour : HasVal<KilogramPerKiloWattHour>
    {
        public KilogramPerKiloWattHour(float v) : base(v) { }
        public override KilogramPerKiloWattHour CreateNewInstance(float v) { return new KilogramPerKiloWattHour(v); }
        public static Kilogram operator *(KilogramPerKiloWattHour a, KiloWattHours b) { return new Kilogram(a.Val * b.Val); }
        public static KilogramPerKilometer operator *(KilogramPerKiloWattHour a, KiloWattHoursPerKilometer b) { return new KilogramPerKilometer(a.Val * b.Val); }
    }
    public class KilogramPerKilometer : HasVal<KilogramPerKilometer>
    {
        public KilogramPerKilometer(float v) : base(v) { }
        public override KilogramPerKilometer CreateNewInstance(float v) { return new KilogramPerKilometer(v); }
        public static EuroPerKilometer operator *(KilogramPerKilometer a, EuroPerKilogram b) { return new EuroPerKilometer(a.Val * b.Val); }
    }
    public class KilogramPerLiter : HasVal<KilogramPerLiter>
    {
        public KilogramPerLiter(float v) : base(v) { }
        public override KilogramPerLiter CreateNewInstance(float v) { return new KilogramPerLiter(v); }
        public static Kilogram operator *(KilogramPerLiter a, Liter b) { return new Kilogram(a.Val * b.Val); }
        public static KilogramPerKilometer operator *(KilogramPerLiter a, LiterPerKilometer b) { return new KilogramPerKilometer(a.Val * b.Val); }
    }
    public class OtherUnit : HasVal<OtherUnit>
    {
        public OtherUnit(float v) : base(v) { }
        public override OtherUnit CreateNewInstance(float v) { return new OtherUnit(v); }
        public static explicit operator OtherUnit(float v) => new OtherUnit(v);
        public static explicit operator OtherUnit(double v) => new OtherUnit((float)v);
    }
    public class Dimensionless : HasVal<Dimensionless>
    {
        public Dimensionless(float v) : base(v) { }
        public override Dimensionless CreateNewInstance(float v) { return new Dimensionless(v); }
        public static Dimensionless operator +(float a, Dimensionless b) { return new Dimensionless(a + b.Val); }
        public static Dimensionless operator -(float a, Dimensionless b) { return new Dimensionless(a - b.Val); }
        public static Dimensionless operator +(Dimensionless a, float b) { return new Dimensionless(a.Val + b); }
        public static Dimensionless operator -(Dimensionless a, float b) { return new Dimensionless(a.Val - b); }
        public static Dimensionless operator *(Dimensionless a, float b) { return new Dimensionless(a.Val * b); }
        public static Dimensionless operator *(float a, Dimensionless b) { return new Dimensionless(a * b.Val); }
        public static Dimensionless operator *(Dimensionless a, Dimensionless b) { return new Dimensionless(a.Val * b.Val); }
        public static Dimensionless operator /(Dimensionless a, Dimensionless b) { return new Dimensionless(a.Val / b.Val); }
        public static Dimensionless operator /(float a, Dimensionless b) { return new Dimensionless(a / b.Val); }
    }
}
