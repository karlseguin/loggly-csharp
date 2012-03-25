using NUnit.Framework;
using System.Threading;

namespace Loggly.Tests.LoggerTests
{
   public class LogTests : BaseFixture
   {
      [Test]
      public void SynchronouslyLogsAMessage()
      {
         Server.Stub(new ApiExpectation {Method = "POST", Url = "/inputs/ITS-OVER-9000", Request = "Vegeta!!!", Response = "{}"});
         new Logger("ITS-OVER-9000").LogSync("Vegeta!!!");
      }

      [Test]
      public void SynchronouslyReturnsTheResponse()
      {
          Server.Stub(new ApiExpectation { Response = "{eventstamp: 123495}" });
          Assert.AreEqual(123495, new Logger("ITS-OVER-9000").LogSync("Vegeta!!!").TimeStamp);
      }

      [Test]
      [Ignore("This test is hanging the test runner randomly, my guess is that the worker thread doesn't get to execute the callback and it gets into an unconsistent state")]
      public void ASynchronouslyLogsAMessageWithNullCallback()
      {
          Server.Stub(new ApiExpectation { Method = "POST", Url = "/inpust/ATREIDES", Request = "Aynch is even cooler", Response = "{}" });
          new Logger("ATREIDES").Log("Aynch is even cooler");
      }

      [Test]
      public void ASynchronouslyCallsbackWithResponse()
      {
          Server.Stub(new ApiExpectation { Response = "{eventstamp: 747193}" });
          var signal = new AutoResetEvent(false);
          new Logger("ATREIDES").Log("Leto II", r =>
          {
              Assert.AreEqual(747193, r.TimeStamp);
              signal.Set();
          });

          bool signaled = signal.WaitOne(6000);
          Assert.IsTrue(signaled);
      }

      [Test]
      public void WriteTwoLogEntriesAsyncWithCallback()
      {
          Server.Stub(new ApiExpectation { Response = "{eventstamp: 747193}" });
          long counter = 0;
          var signal = new AutoResetEvent(false);
          new Logger("ATREIDES").Log("Leto II", r =>
          {
              Assert.AreEqual(747193, r.TimeStamp);
              if (Interlocked.Increment(ref counter) == 2)
                  signal.Set(); 
          });

          new Logger("ATREIDES").Log("Leto III", r =>
          {
              Assert.AreEqual(747193, r.TimeStamp);
              if (Interlocked.Increment(ref counter) == 2)
                  signal.Set(); 
          });

          bool signaled = signal.WaitOne(6000);
          Assert.IsTrue(signaled);
      }
   }
}