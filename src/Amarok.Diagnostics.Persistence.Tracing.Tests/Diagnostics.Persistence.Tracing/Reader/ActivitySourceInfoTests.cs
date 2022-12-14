// Copyright (c) 2023, Olaf Kober <olaf.kober@outlook.com>

namespace Amarok.Diagnostics.Persistence.Tracing.Reader;


[TestFixture]
public class ActivitySourceInfoTests
{
    [Test]
    public void Usage_with_Name()
    {
        var info = new ActivitySourceInfo("foo");

        Check.That(info.Name).IsEqualTo("foo");
        Check.That(info.Version).IsNull();

        Check.That(info.ToString()).IsEqualTo("foo");
    }

    [Test]
    public void Usage_with_Name_Version()
    {
        var info = new ActivitySourceInfo("foo", "1.0");

        Check.That(info.Name).IsEqualTo("foo");
        Check.That(info.Version).IsEqualTo("1.0");

        Check.That(info.ToString()).IsEqualTo("foo 1.0");
    }
}
