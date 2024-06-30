// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

using Amarok.Diagnostics.Persistence.Protos;
using Google.Protobuf;


namespace Amarok.Diagnostics.Persistence.Tracing.Reader.Internal;


[TestFixture]
public class AnyValueDeserializerTests
{
    private AnyValueDeserializer mDeserializer = null!;


    [SetUp]
    public void Setup()
    {
        mDeserializer = new AnyValueDeserializer();
    }


    [Test]
    public void Deserialize_Null()
    {
        var any = new AnyValue { Null = false };

        Check.That(mDeserializer.Deserialize(any)).IsNull();
    }

    [Test]
    public void Deserialize_String()
    {
        var any = new AnyValue { String = "foo" };

        Check.That(mDeserializer.Deserialize(any)).IsEqualTo("foo");
    }

    [Test]
    public void Deserialize_Boolean()
    {
        var any = new AnyValue { Bool = true };

        Check.That(mDeserializer.Deserialize(any)).IsEqualTo(true);
    }

    [Test]
    public void Deserialize_Int32()
    {
        var any = new AnyValue { Int32 = 1234567890 };

        Check.That(mDeserializer.Deserialize(any)).IsEqualTo(1234567890);
    }

    [Test]
    public void Deserialize_Int64()
    {
        var any = new AnyValue { Int64 = 1234567890 };

        Check.That(mDeserializer.Deserialize(any)).IsEqualTo(1234567890);
    }

    [Test]
    public void Deserialize_Double()
    {
        var any = new AnyValue { Double = 12345.678 };

        Check.That(mDeserializer.Deserialize(any)).IsEqualTo(12345.678);
    }

    [Test]
    public void Deserialize_Guid()
    {
        var guid = Guid.NewGuid();
        var any  = new AnyValue { Guid = ByteString.CopyFrom(guid.ToByteArray()) };

        Check.That(mDeserializer.Deserialize(any)).IsEqualTo(guid);
    }

    [Test]
    public void Deserialize_Bytes()
    {
        var bytes = new Byte[] { 0x11, 0x22, 0x33, 0x44 };
        var any   = new AnyValue { Bytes = ByteString.CopyFrom(bytes) };

        Check.That((Byte[])mDeserializer.Deserialize(any)!).ContainsExactly(0x11, 0x22, 0x33, 0x44);
    }

    [Test]
    public void Deserialize_DateOnly()
    {
        var date = new DateOnly(2022, 10, 26);

        var any = new AnyValue {
            DateOnly = new DateOnlyValue {
                Year  = date.Year,
                Month = date.Month,
                Day   = date.Day,
            },
        };

        Check.That(mDeserializer.Deserialize(any)).IsEqualTo(date);
    }

    [Test]
    public void Deserialize_DateTime()
    {
        var date = DateTime.Now;

        var any = new AnyValue {
            DateTime = new DateTimeValue {
                Ticks = date.Ticks,
                Kind  = (Int32)date.Kind,
            },
        };

        Check.That(mDeserializer.Deserialize(any)).IsEqualTo(date);
    }

    [Test]
    public void Deserialize_DateTimeOffset()
    {
        var date = DateTimeOffset.Now;

        var any = new AnyValue {
            DateTimeOffset = new DateTimeOffsetValue {
                Ticks = date.Ticks,
                OffsetMinutes =
                    (Int32)date.Offset.TotalMinutes,
            },
        };

        Check.That(mDeserializer.Deserialize(any)).IsEqualTo(date);
    }

    [Test]
    public void Deserialize_TimeOnly()
    {
        var time = new TimeOnly(11, 22, 33, 456);

        var any = new AnyValue { TimeOnly = time.Ticks };

        Check.That(mDeserializer.Deserialize(any)).IsEqualTo(time);
    }

    [Test]
    public void Deserialize_TimeSpan()
    {
        var duration = TimeSpan.FromMilliseconds(12345);

        var any = new AnyValue { TimeSpan = duration.Ticks };

        Check.That(mDeserializer.Deserialize(any)).IsEqualTo(duration);
    }

    [Test]
    public void Deserialize_UInt32()
    {
        var any = new AnyValue { Uint32 = 1234567890 };

        Check.That(mDeserializer.Deserialize(any)).IsEqualTo(1234567890);
    }

    [Test]
    public void Deserialize_UInt64()
    {
        var any = new AnyValue { Uint64 = 1234567890 };

        Check.That(mDeserializer.Deserialize(any)).IsEqualTo(1234567890);
    }

    [Test]
    public void Deserialize_Decimal()
    {
        Decimal dec = 1234567;

        var bits = Decimal.GetBits(dec);

        var any = new AnyValue {
            Decimal = new DecimalValue {
                Element1 = bits[0],
                Element2 = bits[1],
                Element3 = bits[2],
                Element4 = bits[3],
            },
        };

        Check.That(mDeserializer.Deserialize(any)).IsEqualTo(dec);
    }

    [Test]
    public void Deserialize_None()
    {
        var any = new AnyValue();

        Check.ThatCode(() => mDeserializer.Deserialize(any)).Throws<FormatException>();
    }
}
