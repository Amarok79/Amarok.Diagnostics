// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

using Amarok.Diagnostics.Persistence.Protos;
using Google.Protobuf;


namespace Amarok.Diagnostics.Persistence.Tracing.Writer.Internal;


internal sealed class AnyValueSerializer
{
    private readonly Int32 mMaxStringLength;
    private readonly Int32 mMaxBytesLength;


    public AnyValueSerializer(
        Int32 maxStringLength,
        Int32 maxBytesLength
    )
    {
        mMaxStringLength = maxStringLength;
        mMaxBytesLength = maxBytesLength;
    }


    // Supported types:
    // - null, DBNull
    // - Byte, UInt16, UInt32, UInt64
    // - SByte, Int16, Int32, Int64
    // - Boolean
    // - Char, String
    // - Half, Single, Double, Decimal
    // - DateOnly, TimeOnly, DateTime, DateTimeOffset,
    // - Byte[], Memory<Byte>, ReadOnlyMemory<Byte>
    // - Guid
    // - Object, Enum (converted to String)

    public void Serialize(
        AnyValue any,
        Object? value
    )
    {
        switch (value)
        {
            case null:
                any.Null = false;
                break;

            case String stringValue:
                _SerializeString(any, stringValue);
                break;

            case Boolean booleanValue:
                any.Bool = booleanValue;
                break;

            case Int32 int32Value:
                any.Int32 = int32Value;
                break;

            case Int64 int64Value:
                any.Int64 = int64Value;
                break;

            case Double doubleValue:
                any.Double = doubleValue;
                break;

            case DateTime dateTimeValue:
                _SerializeDateTime(any, dateTimeValue);
                break;

            case DateTimeOffset dateTimeOffset:
                _SerializeDateTimeOffset(any, dateTimeOffset);
                break;

            default:
                _SerializeSlow(any, value);
                break;
        }
    }


    private void _SerializeSlow(
        AnyValue any,
        Object value
    )
    {
        switch (value)
        {
            case Byte byteValue:
                any.Int32 = byteValue;
                break;

            case Guid guidValue:
                _SerializeGuid(any, guidValue);
                break;

            case UInt16 uint16Value:
                any.Uint32 = uint16Value;
                break;

            case UInt32 uint32Value:
                any.Uint32 = uint32Value;
                break;

            case UInt64 uint64Value:
                any.Uint64 = uint64Value;
                break;

            case SByte sbyteValue:
                any.Int32 = sbyteValue;
                break;

            case Int16 int16Value:
                any.Int32 = int16Value;
                break;

            case Byte[] byteArray:
            {
                var count = Math.Min(byteArray.Length, mMaxBytesLength);
                any.Bytes = ByteString.CopyFrom(byteArray, 0, count);
                break;
            }

            case Memory<Byte> byteMemory:
            {
                var span = byteMemory.Span;
                var count = Math.Min(span.Length, mMaxBytesLength);

                any.Bytes = ByteString.CopyFrom(span[..count]);
                break;
            }

            case ReadOnlyMemory<Byte> readOnlyByteMemory:
            {
                var span = readOnlyByteMemory.Span;
                var count = Math.Min(span.Length, mMaxBytesLength);

                any.Bytes = ByteString.CopyFrom(span[..count]);
                break;
            }

            case DateOnly dateOnlyValue:
                _SerializeDateOnly(any, dateOnlyValue);
                break;

            case TimeOnly timeOnlyValue:
                _SerializeTimeOnly(any, timeOnlyValue);
                break;

            case Half halfValue:
                any.Double = (Double)halfValue;
                break;

            case Single singleValue:
                any.Double = singleValue;
                break;

            case Char charValue:
                any.String = charValue.ToString();
                break;

            case Decimal decimalValue:
                _SerializeDecimal(any, decimalValue);
                break;

            case DBNull:
                any.Null = false;
                break;

            default:
                _SerializeObject(any, value);
                break;
        }
    }


    private void _SerializeString(
        AnyValue any,
        String value
    )
    {
        any.String = value.Length <= mMaxStringLength ? value : value[..mMaxStringLength];
    }

    private void _SerializeObject(
        AnyValue any,
        Object value
    )
    {
        var stringValue = value.ToString();

        if (stringValue is null)
        {
            any.Null = false;
        }
        else
        {
            _SerializeString(any, stringValue);
        }
    }

    private static void _SerializeDateOnly(
        AnyValue any,
        DateOnly value
    )
    {
        any.DateOnly = new DateOnlyValue {
            Year = value.Year,
            Month = value.Month,
            Day = value.Day,
        };
    }

    private static void _SerializeDateTime(
        AnyValue any,
        DateTime value
    )
    {
        any.DateTime = new DateTimeValue {
            Ticks = value.Ticks,
            Kind = (Int32)value.Kind,
        };
    }

    private static void _SerializeDateTimeOffset(
        AnyValue any,
        DateTimeOffset value
    )
    {
        any.DateTimeOffset = new DateTimeOffsetValue {
            Ticks = value.Ticks,
            OffsetMinutes = (Int32)value.Offset.TotalMinutes,
        };
    }

    private static void _SerializeTimeOnly(
        AnyValue any,
        TimeOnly value
    )
    {
        any.TimeOnly = value.Ticks;
    }

    private static void _SerializeDecimal(
        AnyValue any,
        Decimal value
    )
    {
        Span<Int32> bits = stackalloc Int32[4];

        Decimal.GetBits(value, bits);

        any.Decimal = new DecimalValue {
            Element1 = bits[0],
            Element2 = bits[1],
            Element3 = bits[2],
            Element4 = bits[3],
        };
    }

    private static void _SerializeGuid(
        AnyValue any,
        Guid value
    )
    {
        Span<Byte> bytes = stackalloc Byte[16];

        value.TryWriteBytes(bytes);

        any.Guid = ByteString.CopyFrom(bytes);
    }
}
