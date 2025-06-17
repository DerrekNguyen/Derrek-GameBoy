using System;

class Program
{
    static void Main()
    {
        String[] args = ["test"];
        Emulator.Run(args);
        Emulator.Delay(1000);
        Emulator.Run(args);
    }
}