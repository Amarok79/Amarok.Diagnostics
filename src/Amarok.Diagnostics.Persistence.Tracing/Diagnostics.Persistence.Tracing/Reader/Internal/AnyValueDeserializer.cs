// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

#pragma warning disable CA1822 // Mark members as static

using Amarok.Diagnostics.Persistence.Protos;
using Google.Protobuf;


namespace Amarok.Diagnostics.Persistence.Tracing.Reader.Internal;


internal sealed class AnyValueDeserializer
{
    public Object? Deserialize(AnyValue value)
    {
        return value.ValuesCase switch {
            AnyValue.ValuesOneofCase.Null           => null,
            AnyValue.ValuesOneofCase.String         => value.String,
            AnyValue.ValuesOneofCase.Bool           => value.Bool,
            AnyValue.ValuesOneofCase.Int32          => value.Int32,
            AnyValue.ValuesOneofCase.Int64          => value.Int64,
            AnyValue.ValuesOneofCase.Double         => value.Double,
            AnyValue.ValuesOneofCase.Guid           => _DeserializeGuid(value.Guid),
            AnyValue.ValuesOneofCase.Bytes          => _DeserializeBytes(value.Bytes),
            AnyValue.ValuesOneofCase.DateOnly       => _DeserializeDateOnly(value.DateOnly),
            AnyValue.ValuesOneofCase.DateTime       => _DeserializeDateTime(value.DateTime),
            AnyValue.ValuesOneofCase.DateTimeOffset => _DeserializeDateTimeOffset(value.DateTimeOffset),
            AnyValue.ValuesOneofCase.TimeOnly       => _DeserializeTimeOnly(value.TimeOnly),
            AnyValue.ValuesOneofCase.TimeSpan       => _DeserializeTimeSpan(value.TimeSpan),
            AnyValue.ValuesOneofCase.Uint32         => value.Uint32,
            AnyValue.ValuesOneofCase.Uint64         => value.Uint64,
            AnyValue.ValuesOneofCase.Decimal        => _DeserializeDecimal(value.Decimal),
            _                                       => throw _MakeUnexpectedCaseException(value.ValuesCase),
        };
    }


    private static Exception _MakeUnexpectedCaseException(AnyValue.ValuesOneofCase caseValue)
    {
        return new FormatException($"Unexpected AnyValue case '{caseValue}'.");
    }


    private static Object _DeserializeDecimal(DecimalValue value)
    {
        Span<Int32> bits = stackalloc Int32[4] { value.Element1, value.Element2, value.Element3, value.Element4 };

        return new Decimal(bits);
    }

    private static Object _DeserializeTimeSpan(Int64 value)
    {
        return new TimeSpan(value);
    }

    private static Object _DeserializeTimeOnly(Int64 value)
    {
        return new TimeOnly(value);
    }

    private static Object _DeserializeDateTimeOffset(DateTimeOffsetValue value)
    {
        return new DateTimeOffset(value.Ticks, TimeSpan.FromMinutes(value.OffsetMinutes));
    }

    private static Object _DeserializeDateTime(DateTimeValue value)
    {
        return new DateTime(value.Ticks, (DateTimeKind)value.Kind);
    }

    private static Object _DeserializeDateOnly(DateOnlyValue value)
    {
        return new DateOnly(value.Year, value.Month, value.Day);
    }

    private static Object _DeserializeBytes(ByteString value)
    {
        return value.ToByteArray();
    }

    private static Object _DeserializeGuid(ByteString value)
    {
        return new Guid(value.Span);
    }
}
