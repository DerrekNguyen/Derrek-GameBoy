using System;
using NUnit.Framework;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmuTests
{
   public class CPUTests
   {
      [SetUp]
      public void Setup()
      {

      }

      [Test]
      public void Test()
      {
         bool result = CPU.CPU_Step();
         Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(result);
      }
   }
}
