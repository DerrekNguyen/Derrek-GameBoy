using EmuTests;
using System;

class Program
{
   public static string GetRomPath(string filename)
   {
      string projectRoot = Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;
      return Path.Combine(projectRoot, "roms", filename);
   }
   static void Main()
   {
      String[] args = [GetRomPath("mem_timing.gb ")];
      Emulator.Run(args);
   }
}