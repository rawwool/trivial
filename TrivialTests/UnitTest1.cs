using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextRuler.AdvancedTextEditorControl;

namespace TrivialTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            OutlookManager om = new OutlookManager();
            var x = om.GetAppointmentsInRange(DateTime.Now.Date, DateTime.Now.Date.AddDays(1));
            Assert.IsNotNull(x);
        }
    }
}
