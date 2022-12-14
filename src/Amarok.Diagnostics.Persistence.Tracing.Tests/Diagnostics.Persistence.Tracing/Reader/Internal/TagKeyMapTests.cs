// Copyright (c) 2023, Olaf Kober <olaf.kober@outlook.com>

namespace Amarok.Diagnostics.Persistence.Tracing.Reader.Internal;


[TestFixture]
public class TagKeyMapTests
{
    private String mItem1 = null!;
    private String mItem2 = null!;
    private String mItem3 = null!;
    private String mItem4 = null!;
    private TagKeyMap mMap = null!;


    [SetUp]
    public void Setup()
    {
        mItem1 = "aaa";
        mItem2 = "bbb";
        mItem3 = "ccc";
        mItem4 = "ddd";

        mMap = new TagKeyMap(3);
    }


    [Test]
    public void Lookup_when_NotDefined()
    {
        Check.ThatCode(() => mMap.Lookup(0)).Throws<FormatException>();
        Check.ThatCode(() => mMap.Lookup(2)).Throws<FormatException>();
        Check.ThatCode(() => mMap.Lookup(3)).Throws<FormatException>();
        Check.ThatCode(() => mMap.Lookup(4)).Throws<FormatException>();
        Check.ThatCode(() => mMap.Lookup(123)).Throws<FormatException>();
    }

    [Test]
    public void Lookup_when_Defined()
    {
        mMap.Define(1, mItem1);
        mMap.Define(2, mItem2);

        Check.That(mMap.Lookup(1)).IsSameReferenceAs(mItem1);
        Check.That(mMap.Lookup(2)).IsSameReferenceAs(mItem2);

        mMap.Define(3, mItem3);
        mMap.Define(4, mItem4);

        Check.That(mMap.Lookup(3)).IsSameReferenceAs(mItem3);
        Check.That(mMap.Lookup(4)).IsSameReferenceAs(mItem4);
    }

    [Test]
    public void Define_when_AlreadyDefined()
    {
        mMap.Define(1, mItem1);

        Check.ThatCode(() => mMap.Define(1, mItem2)).Throws<FormatException>();
    }

    [Test]
    public void Reset()
    {
        mMap.Define(1, mItem1);
        mMap.Define(2, mItem2);

        Check.That(mMap.Lookup(1)).IsSameReferenceAs(mItem1);
        Check.That(mMap.Lookup(2)).IsSameReferenceAs(mItem2);

        mMap.Reset();

        Check.ThatCode(() => mMap.Lookup(1)).Throws<FormatException>();
    }

    [Test]
    public void Usage()
    {
        Check.ThatCode(() => mMap.Lookup(1)).Throws<FormatException>();
        Check.ThatCode(() => mMap.Lookup(2)).Throws<FormatException>();
        Check.ThatCode(() => mMap.Lookup(3)).Throws<FormatException>();
        Check.ThatCode(() => mMap.Lookup(4)).Throws<FormatException>();

        mMap.Define(1, mItem1);
        mMap.Define(2, mItem2);

        Check.That(mMap.Lookup(1)).IsSameReferenceAs(mItem1);
        Check.That(mMap.Lookup(2)).IsSameReferenceAs(mItem2);
        Check.ThatCode(() => mMap.Lookup(3)).Throws<FormatException>();
        Check.ThatCode(() => mMap.Lookup(4)).Throws<FormatException>();

        mMap.Define(3, mItem3);

        Check.That(mMap.Lookup(1)).IsSameReferenceAs(mItem1);
        Check.That(mMap.Lookup(2)).IsSameReferenceAs(mItem2);
        Check.That(mMap.Lookup(3)).IsSameReferenceAs(mItem3);
        Check.ThatCode(() => mMap.Lookup(4)).Throws<FormatException>();

        mMap.Define(4, mItem4);

        Check.That(mMap.Lookup(1)).IsSameReferenceAs(mItem1);
        Check.That(mMap.Lookup(2)).IsSameReferenceAs(mItem2);
        Check.That(mMap.Lookup(3)).IsSameReferenceAs(mItem3);
        Check.That(mMap.Lookup(4)).IsSameReferenceAs(mItem4);

        mMap.Reset();

        Check.ThatCode(() => mMap.Lookup(1)).Throws<FormatException>();
        Check.ThatCode(() => mMap.Lookup(2)).Throws<FormatException>();
        Check.ThatCode(() => mMap.Lookup(3)).Throws<FormatException>();
        Check.ThatCode(() => mMap.Lookup(4)).Throws<FormatException>();
    }
}
