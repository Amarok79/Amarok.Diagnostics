// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

namespace Amarok.Diagnostics.Persistence.Tracing.Reader.Internal;


[TestFixture]
public class ReferenceTimeMapTests
{
    private ReferenceTimeMap mMap = null!;


    [SetUp]
    public void Setup()
    {
        mMap = new ReferenceTimeMap();
    }



    [Test]
    public void GetAbsolutePointInTime_when_NotDefined()
    {
        Check.ThatCode(() => mMap.GetAbsolutePointInTime(TimeSpan.Zero)).Throws<FormatException>();
    }

    [Test]
    public void GetAbsolutePointInTime_when_Defined()
    {
        var refTime = DateTimeOffset.Now;

        mMap.Define(refTime);

        Check.That(mMap.GetAbsolutePointInTime(TimeSpan.Zero)).IsEqualTo(refTime);

        Check.That(mMap.GetAbsolutePointInTime(TimeSpan.FromSeconds(12))).IsEqualTo(refTime + TimeSpan.FromSeconds(12));
    }

    [Test]
    public void Reset()
    {
        mMap.Define(DateTimeOffset.Now);

        mMap.Reset();

        Check.ThatCode(() => mMap.GetAbsolutePointInTime(TimeSpan.Zero)).Throws<FormatException>();
    }

    [Test]
    public void Usage()
    {
        Check.ThatCode(() => mMap.GetAbsolutePointInTime(TimeSpan.Zero)).Throws<FormatException>();

        var refTime = DateTimeOffset.Now;

        mMap.Define(refTime);

        Check.That(mMap.GetAbsolutePointInTime(TimeSpan.Zero)).IsEqualTo(refTime);

        refTime = DateTimeOffset.Now + TimeSpan.FromHours(3.5);

        mMap.Define(refTime);

        Check.That(mMap.GetAbsolutePointInTime(TimeSpan.Zero)).IsEqualTo(refTime);

        mMap.Reset();

        Check.ThatCode(() => mMap.GetAbsolutePointInTime(TimeSpan.Zero)).Throws<FormatException>();
    }
}
