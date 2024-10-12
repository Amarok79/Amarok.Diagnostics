// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

using Amarok.Diagnostics.Persistence.Protos;


namespace Amarok.Diagnostics.Persistence.Tracing.Writer.Internal;


[TestFixture]
public class AnyValueSerializerTests
{
    private AnyValue mAny = null!;
    private AnyValueSerializer mSerializer = null!;


    [SetUp]
    public void Setup()
    {
        mAny        = new AnyValue();
        mSerializer = new AnyValueSerializer(128, 32);
    }


    [Test]
    public void Serialize_Null()
    {
        Object? value = null;

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Null).IsEqualTo(false);
    }

    [Test]
    public void Serialize_DbNull()
    {
        Object value = DBNull.Value;

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Null).IsEqualTo(false);
    }


    [Test]
    public void Serialize_Byte_MaxValue()
    {
        Object? value = Byte.MaxValue;

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Int32).IsEqualTo(Byte.MaxValue);
    }

    [Test]
    public void Serialize_UInt16_MaxValue()
    {
        Object? value = UInt16.MaxValue;

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Uint32).IsEqualTo(UInt16.MaxValue);
    }

    [Test]
    public void Serialize_UInt32_MaxValue()
    {
        Object? value = UInt32.MaxValue;

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Uint32).IsEqualTo(UInt32.MaxValue);
    }

    [Test]
    public void Serialize_UInt64_MaxValue()
    {
        Object? value = UInt64.MaxValue;

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Uint64).IsEqualTo(UInt64.MaxValue);
    }


    [Test]
    public void Serialize_SByte_MinValue()
    {
        Object? value = SByte.MinValue;

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Int32).IsEqualTo(SByte.MinValue);
    }

    [Test]
    public void Serialize_SByte_MaxValue()
    {
        Object? value = SByte.MaxValue;

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Int32).IsEqualTo(SByte.MaxValue);
    }

    [Test]
    public void Serialize_Int16_MinValue()
    {
        Object? value = Int16.MinValue;

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Int32).IsEqualTo(Int16.MinValue);
    }

    [Test]
    public void Serialize_Int16_MaxValue()
    {
        Object? value = Int16.MaxValue;

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Int32).IsEqualTo(Int16.MaxValue);
    }

    [Test]
    public void Serialize_Int32_MinValue()
    {
        Object? value = Int32.MinValue;

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Int32).IsEqualTo(Int32.MinValue);
    }

    [Test]
    public void Serialize_Int32_MaxValue()
    {
        Object? value = Int32.MaxValue;

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Int32).IsEqualTo(Int32.MaxValue);
    }

    [Test]
    public void Serialize_Int64_MinValue()
    {
        Object? value = Int64.MinValue;

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Int64).IsEqualTo(Int64.MinValue);
    }

    [Test]
    public void Serialize_Int64_MaxValue()
    {
        Object? value = Int64.MaxValue;

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Int64).IsEqualTo(Int64.MaxValue);
    }


    [Test]
    public void Serialize_Boolean_False()
    {
        Object value = false;

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Bool).IsEqualTo(false);
    }

    [Test]
    public void Serialize_Boolean_True()
    {
        Object value = true;

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Bool).IsEqualTo(true);
    }


    [Test]
    public void Serialize_Char()
    {
        Object value = 'a';

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.String).IsEqualTo("a");
    }

    [Test]
    public void Serialize_String()
    {
        Object value = "abc";

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.String).IsEqualTo("abc");
    }

    [Test]
    public void Serialize_String_WithWhitespace()
    {
        Object value = " a  bc   ";

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.String).IsEqualTo(" a  bc   ");
    }

    [Test]
    public void Serialize_String_Empty()
    {
        Object value = "";

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.String).IsEqualTo("");
    }

    [Test]
    public void Serialize_String_WhitespaceOnly()
    {
        Object value = "  ";

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.String).IsEqualTo("  ");
    }

    [Test]
    public void Serialize_String_Long()
    {
        Object value = new String('a', 128);

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.String).IsEqualTo(new String('a', 128));
    }

    [Test]
    public void Serialize_String_TooLong()
    {
        Object value = new String('a', 127) + "bc";

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.String).IsEqualTo(new String('a', 127) + "b");
    }


    [Test]
    public void Serialize_Half()
    {
        Object? value = (Half)12.5;

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Double).IsEqualTo(12.5);
    }

    [Test]
    public void Serialize_Single_MinValue()
    {
        Object? value = Single.MinValue;

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Double).IsEqualTo(Single.MinValue);
    }

    [Test]
    public void Serialize_Single_MaxValue()
    {
        Object? value = Single.MaxValue;

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Double).IsEqualTo(Single.MaxValue);
    }

    [Test]
    public void Serialize_Double_MinValue()
    {
        Object? value = Double.MinValue;

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Double).IsEqualTo(Double.MinValue);
    }

    [Test]
    public void Serialize_Double_MaxValue()
    {
        Object? value = Double.MaxValue;

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Double).IsEqualTo(Double.MaxValue);
    }

    [Test]
    public void Serialize_Decimal()
    {
        Object value = (Decimal)12.5;

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Decimal.Element1).IsEqualTo(125);

        Check.That(mAny.Decimal.Element2).IsEqualTo(0);

        Check.That(mAny.Decimal.Element3).IsEqualTo(0);

        Check.That(mAny.Decimal.Element4).IsEqualTo(65536);
    }


    [Test]
    public void Serialize_DateOnly()
    {
        Object value = new DateOnly(2022, 10, 04);

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.DateOnly.Year).IsEqualTo(2022);

        Check.That(mAny.DateOnly.Month).IsEqualTo(10);

        Check.That(mAny.DateOnly.Day).IsEqualTo(04);
    }

    [Test]
    public void Serialize_TimeOnly()
    {
        Object value = new TimeOnly(15, 18, 23, 123);

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.TimeOnly).IsEqualTo(551031230000);
    }

    [Test]
    public void Serialize_DateTime_Unspecified()
    {
        Object value = new DateTime(2022, 10, 04, 15, 18, 23, 123, DateTimeKind.Unspecified);

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.DateTime.Ticks).IsEqualTo(638004935031230000);

        Check.That(mAny.DateTime.Kind).IsEqualTo(0);
    }

    [Test]
    public void Serialize_DateTime_Local()
    {
        Object value = new DateTime(2022, 10, 04, 15, 18, 23, 123, DateTimeKind.Local);

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.DateTime.Ticks).IsEqualTo(638004935031230000);

        Check.That(mAny.DateTime.Kind).IsEqualTo(2);
    }

    [Test]
    public void Serialize_DateTime_Utc()
    {
        Object value = new DateTime(2022, 10, 04, 15, 18, 23, 123, DateTimeKind.Utc);

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.DateTime.Ticks).IsEqualTo(638004935031230000);

        Check.That(mAny.DateTime.Kind).IsEqualTo(1);
    }

    [Test]
    public void Serialize_DateTimeOffset()
    {
        Object value = new DateTimeOffset(2022, 10, 04, 15, 18, 23, 123, new TimeSpan(6, 0, 0));

        mSerializer.Serialize(mAny, value);

        var dto = new DateTimeOffset(
            mAny.DateTimeOffset.Ticks,
            TimeSpan.FromMinutes(mAny.DateTimeOffset.OffsetMinutes)
        );

        Check.That(dto).IsEqualTo(value);
    }


    [Test]
    public void Serialize_ArrayOfByte()
    {
        Object value = new Byte[] {
            0x11, 0x22, 0x33, 0x44,
        };

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Bytes.ToByteArray()).ContainsExactly(0x11, 0x22, 0x33, 0x44);
    }

    [Test]
    public void Serialize_ArrayOfByte_Empty()
    {
        Object value = Array.Empty<Byte>();

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Bytes.ToByteArray()).IsEmpty();
    }

    [Test]
    public void Serialize_ArrayOfByte_Long()
    {
        var bytes = new Byte[32];
        bytes[0]  = 0x11;
        bytes[31] = 0xFF;

        Object value = bytes;

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Bytes.ToByteArray()[0]).IsEqualTo(0x11);

        Check.That(mAny.Bytes.ToByteArray()[31]).IsEqualTo(0xFF);

        Check.That(mAny.Bytes).HasSize(32);
    }

    [Test]
    public void Serialize_ArrayOfByte_TooLong()
    {
        var bytes = new Byte[40];
        bytes[0]  = 0x11;
        bytes[31] = 0xFF;
        bytes[32] = 0xAA;

        Object value = bytes;

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Bytes.ToByteArray()[0]).IsEqualTo(0x11);

        Check.That(mAny.Bytes.ToByteArray()[31]).IsEqualTo(0xFF);

        Check.That(mAny.Bytes).HasSize(32);
    }


    [Test]
    public void Serialize_MemoryOfByte()
    {
        Object value = new Memory<Byte>([ 0x11, 0x22, 0x33, 0x44 ]);

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Bytes.ToByteArray()).ContainsExactly(0x11, 0x22, 0x33, 0x44);
    }

    [Test]
    public void Serialize_MemoryOfByte_Empty()
    {
        Object value = new Memory<Byte>([ ]);

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Bytes.ToByteArray()).IsEmpty();
    }

    [Test]
    public void Serialize_MemoryOfByte_Long()
    {
        var bytes = new Byte[32];
        bytes[0]  = 0x11;
        bytes[31] = 0xFF;

        Object value = new Memory<Byte>(bytes);

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Bytes.ToByteArray()[0]).IsEqualTo(0x11);

        Check.That(mAny.Bytes.ToByteArray()[31]).IsEqualTo(0xFF);

        Check.That(mAny.Bytes).HasSize(32);
    }

    [Test]
    public void Serialize_MemoryOfByte_TooLong()
    {
        var bytes = new Byte[40];
        bytes[0]  = 0x11;
        bytes[31] = 0xFF;
        bytes[32] = 0xAA;

        Object value = new Memory<Byte>(bytes);

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Bytes.ToByteArray()[0]).IsEqualTo(0x11);

        Check.That(mAny.Bytes.ToByteArray()[31]).IsEqualTo(0xFF);

        Check.That(mAny.Bytes).HasSize(32);
    }


    [Test]
    public void Serialize_ReadOnlyMemoryOfByte()
    {
        Object value = new ReadOnlyMemory<Byte>([ 0x11, 0x22, 0x33, 0x44 ]);

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Bytes.ToByteArray()).ContainsExactly(0x11, 0x22, 0x33, 0x44);
    }

    [Test]
    public void Serialize_ReadOnlyMemoryOfByte_Empty()
    {
        Object value = new ReadOnlyMemory<Byte>([ ]);

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Bytes.ToByteArray()).IsEmpty();
    }

    [Test]
    public void Serialize_ReadOnlyMemoryOfByte_Long()
    {
        var bytes = new Byte[32];
        bytes[0]  = 0x11;
        bytes[31] = 0xFF;

        Object value = new ReadOnlyMemory<Byte>(bytes);

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Bytes.ToByteArray()[0]).IsEqualTo(0x11);

        Check.That(mAny.Bytes.ToByteArray()[31]).IsEqualTo(0xFF);

        Check.That(mAny.Bytes).HasSize(32);
    }

    [Test]
    public void Serialize_ReadOnlyMemoryOfByte_TooLong()
    {
        var bytes = new Byte[40];
        bytes[0]  = 0x11;
        bytes[31] = 0xFF;
        bytes[32] = 0xAA;

        Object value = new ReadOnlyMemory<Byte>(bytes);

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Bytes.ToByteArray()[0]).IsEqualTo(0x11);

        Check.That(mAny.Bytes.ToByteArray()[31]).IsEqualTo(0xFF);

        Check.That(mAny.Bytes).HasSize(32);
    }


    [Test]
    public void Serialize_Guid()
    {
        Object value = new Guid("69A51F8D-E87F-4527-B7D4-F6314DCF04AE");

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Guid.ToByteArray())
            .ContainsExactly(new Guid("69A51F8D-E87F-4527-B7D4-F6314DCF04AE").ToByteArray());
    }


    [Test]
    public void Serialize_Object()
    {
        Object value = new Version(1, 2, 3);

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.String).IsEqualTo("1.2.3");
    }

    [Test]
    public void Serialize_Object_ToStringReturningNull()
    {
        Object value = new TypeWithNullToString();

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Null).IsEqualTo(false);
    }

    [Test]
    public void Serialize_Object_ToStringReturningLongString()
    {
        Object value = new TypeWithLongToString();

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.String).IsEqualTo(new String('a', 128));
    }

    [Test]
    public void Serialize_Object_ToStringReturningTooLongString()
    {
        Object value = new TypeWithTooLongToString();

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.String).IsEqualTo(new String('a', 127) + "b");
    }

    [Test]
    public void Serialize_Enum()
    {
        Object value = DateTimeKind.Local;

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.String).IsEqualTo("Local");
    }


    [Test]
    public void Serialize_NullableOfBoolean_Null()
    {
        Boolean? nullable = null;
        Object?  value    = nullable;

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Null).IsEqualTo(false);
    }

    [Test]
    public void Serialize_NullableOfBoolean_Value()
    {
        Object value = true;

        mSerializer.Serialize(mAny, value);

        Check.That(mAny.Bool).IsEqualTo(true);
    }



    internal class TypeWithNullToString
    {
        public override String? ToString()
        {
            return null;
        }
    }

    internal class TypeWithLongToString
    {
        public override String ToString()
        {
            return new String('a', 128);
        }
    }

    internal class TypeWithTooLongToString
    {
        public override String ToString()
        {
            return new String('a', 127) + "bc";
        }
    }
}
