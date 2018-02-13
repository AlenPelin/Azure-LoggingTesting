using System.Web.Mvc;

namespace LoggingTesting.Controllers
{
  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.IO;
  using System.Linq;
  using System.Threading;
  using System.Web.Hosting;

  using log4net;

  public class HomeController : Controller
  {
    private static readonly ILog Log = LogManager.GetLogger(typeof(HomeController));
    private static readonly object SyncRoot = new object();
    private static readonly List<Thread> Threads = new List<Thread>();

    public ActionResult Index()
    {
      lock (SyncRoot)
      {
        return Content($"Active threads: {Threads.Count}, log size: {new DirectoryInfo(MvcApplication.LogDir).GetFiles().Sum(x => x.Length) / 1024 / 1024 }MB<br />{Result}");
      }
    }
    
    // Beware that 10M log records is 1300MB of log file; Azure S1-S3 give only 100GB of space

    public ActionResult StartWithSpin(int id = 10000000)
    {
      return StartJob(id, spin: 100);      
    }

    public ActionResult Start(int id = 10000000)
    {
      return StartJob(id, spin: 0);
    }

    private ActionResult StartJob(int id, int spin)
    {
      Thread thread = null;
      thread = new Thread(() =>
      {
        var reps = id;
        var start = Process.GetCurrentProcess().PrivateMemorySize64;

        var sw = new Stopwatch();
        var sw2 = new Stopwatch();

        Log.Info("This is example log entry that is intended to test performance");

        sw2.Start();

        if (spin > 0)
        {
          for (var i = 0; i < reps; ++i)
          {
            Thread.SpinWait(spin);

            sw.Start();
            Log.Info("This is example log entry that is intended to test performance");
            sw.Stop();
          }
        }
        else
        {
          for (var i = 0; i < reps; ++i)
          {
            sw.Start();
            Log.Info("This is example log entry that is intended to test performance");
            sw.Stop();
          }
        }

        sw2.Stop();

        var end = Process.GetCurrentProcess().PrivateMemorySize64;

        var startMb = start / 1024 / 1024;
        var endMb = end / 1024 / 1024;

        var msPerK = (double)sw.ElapsedMilliseconds * 1000 / reps;

        lock (SyncRoot)
        {
          Result += $"<hr />" +
                    $"{DateTime.UtcNow:s} INFO " +
                    $"{msPerK}ms per 1000 in batch of {reps:N1} " +
                    $"when writing directly to {MvcApplication.LogPath}. " +
                    $"Total time spent {sw2.Elapsed} including {spin}-spin waits before every log. " +
                    $"Memory at start: {startMb}MB, at end {endMb}MB";

          // ReSharper disable once AccessToModifiedClosure
          Threads.Remove(thread);
        }
      });

      lock (SyncRoot)
      {
        Threads.Add(thread);

        thread.Start();
      }

      return Content($"Started job for {id:N1} writes with {spin}-spin waits before every log");
    }

    public ActionResult Cleanup()
    {
      Directory.Delete(MvcApplication.LogDir, true);

      return Content("DONE");
    }

    public static string Result
    {
      get
      {
        return System.IO.File.ReadAllText(HostingEnvironment.MapPath("/App_Data/result.txt"));
      }
      set
      {
        System.IO.File.WriteAllText(HostingEnvironment.MapPath("/App_Data/result.txt"), value);
      }
    }
  }
}