// Copyright (c) 2024, Olaf Kober <olaf.kober@outlook.com>

using System.CommandLine;


namespace Amarok.Diagnostics.Persistence.Tracing;


public static class Program
{
    public static Int32 Main(String[] args)
    {
        var command = new RootCommand(".NET CLI tools for Amarok.Diagnostics.Persistence.Tracing");

        command.Subcommands.Add(new ConvertCommand());

        return command.Parse(args).Invoke();
    }
}
