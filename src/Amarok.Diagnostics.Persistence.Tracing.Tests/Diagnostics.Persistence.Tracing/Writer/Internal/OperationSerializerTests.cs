// Copyright (c) 2023, Olaf Kober <olaf.kober@outlook.com>

using Amarok.Diagnostics.Persistence.Tracing.Protos;


namespace Amarok.Diagnostics.Persistence.Tracing.Writer.Internal;


[TestFixture]
public class OperationSerializerTests
{
    private TraceRecords mRecords = null!;
    private OperationSerializer mSerializer = null!;


    [SetUp]
    public void Setup()
    {
        mRecords = new TraceRecords();
        mSerializer = new OperationSerializer(4, ObjectsPool.Create(false));
    }


    [Test]
    public void Serialize_with_Operation()
    {
        const String op = "operation-name";

        Check.That(mSerializer.Serialize(op, mRecords)).IsEqualTo(1);
        Check.That(mSerializer.Serialize(op, mRecords)).IsEqualTo(1);

        Check.That(mRecords.Items).HasSize(1);

        Check.That(mRecords.Items[0].DefineOperation).IsNotNull();
        Check.That(mRecords.Items[0].DefineOperation.Id).IsEqualTo(1);
        Check.That(mRecords.Items[0].DefineOperation.Name).IsEqualTo("operation-name");
    }

    [Test]
    public void Serialize_with_Operation_and_Different_Case()
    {
        const String op1 = "operation-name";
        const String op2 = "OPERATION-NAME";

        Check.That(mSerializer.Serialize(op1, mRecords)).IsEqualTo(1);
        Check.That(mSerializer.Serialize(op1, mRecords)).IsEqualTo(1);
        Check.That(mSerializer.Serialize(op2, mRecords)).IsEqualTo(2);
        Check.That(mSerializer.Serialize(op2, mRecords)).IsEqualTo(2);

        Check.That(mRecords.Items).HasSize(2);

        Check.That(mRecords.Items[0].DefineOperation).IsNotNull();
        Check.That(mRecords.Items[0].DefineOperation.Id).IsEqualTo(1);
        Check.That(mRecords.Items[0].DefineOperation.Name).IsEqualTo("operation-name");

        Check.That(mRecords.Items[1].DefineOperation).IsNotNull();
        Check.That(mRecords.Items[1].DefineOperation.Id).IsEqualTo(2);
        Check.That(mRecords.Items[1].DefineOperation.Name).IsEqualTo("OPERATION-NAME");
    }

    [Test]
    public void Serialize_with_Operation_and_Different_Names()
    {
        const String op1 = "operation-name-1";
        const String op2 = "operation-name-2";
        const String op3 = "operation-name-3";
        const String op4 = "operation-name-4";

        Check.That(mSerializer.Serialize(op1, mRecords)).IsEqualTo(1);
        Check.That(mSerializer.Serialize(op2, mRecords)).IsEqualTo(2);
        Check.That(mSerializer.Serialize(op3, mRecords)).IsEqualTo(3);
        Check.That(mSerializer.Serialize(op4, mRecords)).IsEqualTo(4);

        Check.That(mSerializer.Serialize(op1, mRecords)).IsEqualTo(1);
        Check.That(mSerializer.Serialize(op2, mRecords)).IsEqualTo(2);
        Check.That(mSerializer.Serialize(op3, mRecords)).IsEqualTo(3);
        Check.That(mSerializer.Serialize(op4, mRecords)).IsEqualTo(4);

        Check.That(mRecords.Items).HasSize(4);

        Check.That(mRecords.Items[0].DefineOperation).IsNotNull();
        Check.That(mRecords.Items[0].DefineOperation.Id).IsEqualTo(1);
        Check.That(mRecords.Items[0].DefineOperation.Name).IsEqualTo("operation-name-1");

        Check.That(mRecords.Items[1].DefineOperation).IsNotNull();
        Check.That(mRecords.Items[1].DefineOperation.Id).IsEqualTo(2);
        Check.That(mRecords.Items[1].DefineOperation.Name).IsEqualTo("operation-name-2");

        Check.That(mRecords.Items[2].DefineOperation).IsNotNull();
        Check.That(mRecords.Items[2].DefineOperation.Id).IsEqualTo(3);
        Check.That(mRecords.Items[2].DefineOperation.Name).IsEqualTo("operation-name-3");

        Check.That(mRecords.Items[3].DefineOperation).IsNotNull();
        Check.That(mRecords.Items[3].DefineOperation.Id).IsEqualTo(4);
        Check.That(mRecords.Items[3].DefineOperation.Name).IsEqualTo("operation-name-4");
    }

    [Test]
    public void Serialize_with_Operation_and_Different_Names_Overrun()
    {
        const String op1 = "operation-name-1";
        const String op2 = "operation-name-2";
        const String op3 = "operation-name-3";
        const String op4 = "operation-name-4";
        const String op5 = "operation-name-5";
        const String op6 = "operation-name-6";

        Check.That(mSerializer.Serialize(op1, mRecords)).IsEqualTo(1);
        Check.That(mSerializer.Serialize(op2, mRecords)).IsEqualTo(2);
        Check.That(mSerializer.Serialize(op3, mRecords)).IsEqualTo(3);
        Check.That(mSerializer.Serialize(op4, mRecords)).IsEqualTo(4);
        Check.That(mSerializer.Serialize(op5, mRecords)).IsEqualTo(1);
        Check.That(mSerializer.Serialize(op6, mRecords)).IsEqualTo(2);

        Check.That(mRecords.Items).HasSize(7);

        Check.That(mRecords.Items[0].DefineOperation).IsNotNull();
        Check.That(mRecords.Items[0].DefineOperation.Id).IsEqualTo(1);
        Check.That(mRecords.Items[0].DefineOperation.Name).IsEqualTo("operation-name-1");

        Check.That(mRecords.Items[1].DefineOperation).IsNotNull();
        Check.That(mRecords.Items[1].DefineOperation.Id).IsEqualTo(2);
        Check.That(mRecords.Items[1].DefineOperation.Name).IsEqualTo("operation-name-2");

        Check.That(mRecords.Items[2].DefineOperation).IsNotNull();
        Check.That(mRecords.Items[2].DefineOperation.Id).IsEqualTo(3);
        Check.That(mRecords.Items[2].DefineOperation.Name).IsEqualTo("operation-name-3");

        Check.That(mRecords.Items[3].DefineOperation).IsNotNull();
        Check.That(mRecords.Items[3].DefineOperation.Id).IsEqualTo(4);
        Check.That(mRecords.Items[3].DefineOperation.Name).IsEqualTo("operation-name-4");

        Check.That(mRecords.Items[4].ResetOperations).IsNotNull();

        Check.That(mRecords.Items[5].DefineOperation).IsNotNull();
        Check.That(mRecords.Items[5].DefineOperation.Id).IsEqualTo(1);
        Check.That(mRecords.Items[5].DefineOperation.Name).IsEqualTo("operation-name-5");

        Check.That(mRecords.Items[6].DefineOperation).IsNotNull();
        Check.That(mRecords.Items[6].DefineOperation.Id).IsEqualTo(2);
        Check.That(mRecords.Items[6].DefineOperation.Name).IsEqualTo("operation-name-6");
    }

    [Test]
    public void Reset()
    {
        const String op1 = "operation-name-1";
        const String op2 = "operation-name-2";

        Check.That(mSerializer.Serialize(op1, mRecords)).IsEqualTo(1);

        mSerializer.Reset();

        Check.That(mSerializer.Serialize(op2, mRecords)).IsEqualTo(1);
        Check.That(mSerializer.Serialize(op1, mRecords)).IsEqualTo(2);

        Check.That(mRecords.Items).HasSize(3);

        Check.That(mRecords.Items[0].DefineOperation).IsNotNull();
        Check.That(mRecords.Items[0].DefineOperation.Id).IsEqualTo(1);
        Check.That(mRecords.Items[0].DefineOperation.Name).IsEqualTo("operation-name-1");

        Check.That(mRecords.Items[1].DefineOperation).IsNotNull();
        Check.That(mRecords.Items[1].DefineOperation.Id).IsEqualTo(1);
        Check.That(mRecords.Items[1].DefineOperation.Name).IsEqualTo("operation-name-2");

        Check.That(mRecords.Items[2].DefineOperation).IsNotNull();
        Check.That(mRecords.Items[2].DefineOperation.Id).IsEqualTo(2);
        Check.That(mRecords.Items[2].DefineOperation.Name).IsEqualTo("operation-name-1");
    }
}
