// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

namespace Amarok.Diagnostics.Persistence.Tracing.Reader.Internal;


[TestFixture]
public class StreamTraceReaderTests
{
    private StreamTraceReader? mReader;


    [SetUp]
    public void Setup()
    {
    }

    [TearDown]
    public void Cleanup()
    {
        mReader?.Dispose();
    }


    private void _CreateReader(params Byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        mReader = new StreamTraceReader(stream);
    }


    [Test]
    public void Stream_Too_Short_For_FileSignature()
    {
        _CreateReader(0x61, 0x64, 0x74);

        Check.ThatCode(() => mReader!.Read().ToArray()).Throws<EndOfStreamException>();
    }

    [Test]
    public void Stream_With_Unexpected_FileSignature()
    {
        _CreateReader(0x61, 0xFF, 0x74, 0x78);

        //                   ^
        //                   0x64 expected

        Check.ThatCode(() => mReader!.Read().ToArray()).Throws<FormatException>();
    }

    [Test]
    public void Stream_Too_Short_For_FileVersion()
    {
        _CreateReader(
            0x61,
            0x64,
            0x74,
            0x78,
            0x00
        );

        Check.ThatCode(() => mReader!.Read().ToArray()).Throws<EndOfStreamException>();
    }

    [Test]
    public void Stream_With_Unexpected_FileVersion()
    {
        _CreateReader(
            0x61,
            0x64,
            0x74,
            0x78,
            0xFF,
            0x01
        );

        //                                   ^
        //                                   0x00 expected

        Check.ThatCode(() => mReader!.Read().ToArray()).Throws<FormatException>();
    }

    [Test]
    public void Stream_With_Unexpected_FileVersion_2()
    {
        _CreateReader(
            0x61,
            0x64,
            0x74,
            0x78,
            0x00,
            0xFF
        );

        //                                         ^
        //                                         0x01 expected

        Check.ThatCode(() => mReader!.Read().ToArray()).Throws<FormatException>();
    }

    [Test]
    public void Stream_Too_Short_For_FileFlags()
    {
        _CreateReader(
            0x61,
            0x64,
            0x74,
            0x78,
            0x00,
            0x01,
            0x00
        );

        Check.ThatCode(() => mReader!.Read().ToArray()).Throws<EndOfStreamException>();
    }

    [Test]
    public void Stream_With_Unexpected_FileFlags()
    {
        _CreateReader(
            0x61,
            0x64,
            0x74,
            0x78,
            0x00,
            0x01,
            0xFF,
            0x0F
        );

        //                                               ^
        //                                               0x00 expected

        Check.ThatCode(() => mReader!.Read().ToArray()).Throws<FormatException>();
    }

    [Test]
    public void Stream_With_Unexpected_FileFlags_2()
    {
        _CreateReader(
            0x61,
            0x64,
            0x74,
            0x78,
            0x00,
            0x01,
            0x00,
            0xBB
        );

        //                                                     ^
        //                                                     0x0A, 0x0F, or 0xCF expected

        Check.ThatCode(() => mReader!.Read().ToArray()).Throws<FormatException>();
    }

    [Test]
    public void Stream_Too_Short_For_FileSession()
    {
        // @formatter:off
        _CreateReader(
            0x61, 0x64, 0x74, 0x78, 0x00, 0x01, 0x00, 0x0F,
            0x35, 0x22, 0x61, 0x0C, 0x63, 0x7E, 0xE7, 0x44,
            0x91, 0x5E, 0x97, 0xD4, 0x78, 0x02, 0x2F, 0x07,
            0x80, 0x62, 0x81, 0x34, 0x32, 0xBB, 0xDA, 0x08,
            0x78
        );
        // @formatter:on

        Check.ThatCode(() => mReader!.Read().ToArray()).Throws<EndOfStreamException>();
    }

    [Test]
    public void Stream_With_Unexpected_Frame_Content_Preamble()
    {
        // @formatter:off
        _CreateReader(
            0x61, 0x64, 0x74, 0x78, 0x00, 0x01, 0x00, 0x0F,
            0x35, 0x22, 0x61, 0x0C, 0x63, 0x7E, 0xE7, 0x44,
            0x91, 0x5E, 0x97, 0xD4, 0x78, 0x02, 0x2F, 0x07,
            0x80, 0x62, 0x81, 0x34, 0x32, 0xBB, 0xDA, 0x08,
            0x78, 0x00,

            // first frame
            0xFF
        // ^
        // 0xAA expected
        );
        // @formatter:on

        Check.ThatCode(() => mReader!.Read().ToArray()).Throws<FormatException>();
    }

    [Test]
    public void Stream_Too_Short_For_FirstFrame()
    {
        // @formatter:off
        _CreateReader(
            0x61, 0x64, 0x74, 0x78, 0x00, 0x01, 0x00, 0x0F,
            0x35, 0x22, 0x61, 0x0C, 0x63, 0x7E, 0xE7, 0x44,
            0x91, 0x5E, 0x97, 0xD4, 0x78, 0x02, 0x2F, 0x07,
            0x80, 0x62, 0x81, 0x34, 0x32, 0xBB, 0xDA, 0x08,
            0x78, 0x00,

            // first frame
            0xAA, 0x20, 0x00, 0x00, 0x00, 0x00
        );
        // @formatter:on

        Check.ThatCode(() => mReader!.Read().ToArray()).Throws<EndOfStreamException>();
    }
}
