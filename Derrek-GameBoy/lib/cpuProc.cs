using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public delegate void IN_PROC(CPUContext context);
public static class CPUProc
{
   public static IN_PROC GetProc(InType type)
   {
      return type switch
      {
         InType.IN_NONE => ProcNone,
         InType.IN_LD => ProcLD,
         InType.IN_JP => ProcJP,
         _ => ProcNone
      };
   }
   public static void ProcNone(CPUContext ctx)
   {
      Console.WriteLine("Invalid Instruction");
      Environment.Exit(-7);
   }

   public static void ProcLD(CPUContext ctx)
   {

   }

   public static void ProcJP(CPUContext ctx)
   {

   }
}
