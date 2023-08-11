// Copyright (c) 2023, Olaf Kober <olaf.kober@outlook.com>

using System.CommandLine;


namespace Amarok.Diagnostics.Persistence.Tracing;


public static class Program
{
    public static Int32 Main(
        String[] args
    )
    {
        var command = new RootCommand(".NET CLI tools for Amarok.Diagnostics.Persistence.Tracing");

        command.AddCommand(new ConvertCommand());

        return command.Invoke(args);
    }
}
