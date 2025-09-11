// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

namespace Amarok.Diagnostics.Persistence.Tracing.Reader;


[TestFixture]
public class SessionInfoTests
{
    [Test]
    [SetCulture("en")]
    public void Usage()
    {
        var uuid = new Guid("459bb329-1bfd-4e2c-9a66-4e0a7bc0613f");

        var startTime = new DateTimeOffset(
            2022,
            11,
            04,
            11,
            22,
            33,
            TimeSpan.Zero
        );

        var info = new SessionInfo(uuid, startTime);

        Check.That(info.Uuid).IsEqualTo(uuid);

        Check.That(info.StartTime).IsEqualTo(startTime);

        Check.That(info.ToString())
            .IsEqualTo("Uuid: 459bb329-1bfd-4e2c-9a66-4e0a7bc0613f, StartTime: 11/4/2022 11:22:33 AM +00:00");
    }
}
