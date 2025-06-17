using System;
using System.Threading;

public class EmuContext
{
    public bool Paused { get; set; }
    public bool Running { get; set; }
    public ulong Ticks { get; set; }
}

/* 
  Emu components:

  |Cart|
  |CPU|
  |Address Bus|
  |PPU|
  |Timer|

*/

public static class Emulator
{
    private static readonly EmuContext _context = new();
    public static EmuContext GetContext() => _context;

    public static void Delay(uint ms)
    {
        System.Threading.Thread.Sleep((int)ms);
    }
    public static int Run(String[] argv)
    {
        if (argv.Length < 1)
        {
            Console.WriteLine()
        }
        Console.WriteLine(argv[0]);
        return 1;
    }
}