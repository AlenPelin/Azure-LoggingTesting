namespace LoggingTesting
{
  using System;
  using System.IO;
  using System.Text;
  using System.Web.Hosting;
  using System.Web.Mvc;
  using System.Web.Routing;

  using log4net;
  using log4net.Appender;
  using log4net.Layout;
  using log4net.Repository.Hierarchy;
  using log4net.spi;

  using LoggingTesting.Controllers;

  public class MvcApplication : System.Web.HttpApplication
  {
    public static readonly string LogDir = HostingEnvironment.MapPath($"/App_Data/{Environment.MachineName}");
    public static readonly string LogPath = Path.Combine(LogDir, $"log.{DateTime.Now:yy-MM-dd.HH-mm-ss}.txt");

    protected void Application_Start()
    {
      AreaRegistration.RegisterAllAreas();

      RouteTable.Routes.MapRoute(
        name: "Default",
        url: "{controller}/{action}/{id}",
        defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional });

      InitLogger();
    }

    public static readonly StringBuilder InMemLog = new StringBuilder();

    private static void InitLogger()
    {
      Directory.Delete(HostingEnvironment.MapPath($"/App_Data"), true);
      Directory.CreateDirectory(LogDir);

      File.WriteAllText(LogPath, "");

      var pattern = new PatternLayout { ConversionPattern = "%d [%2%t] %-5p [%-10c]   %m%n%n" };
      pattern.ActivateOptions();

      var hierarchy = (Hierarchy)LogManager.GetLoggerRepository();
      hierarchy.Root.RemoveAllAppenders(); /*Remove any other appenders*/

      var mbPerFile = 100; // 10MB is optimal for Sitecore, 100MB for stress test because it takes couple seconds to produce 10MB of logs
      var fileAppender = new RollingFileAppender
      {
        AppendToFile = true,
        File = LogPath,
        Layout = pattern,
        MaxFileSize = mbPerFile * 1024 * 1024,
        RollingStyle = RollingFileAppender.RollingMode.Size,
        MaxSizeRollBackups = -1,        
        ImmediateFlush = false,
        Threshold = Level.ALL,
      };

      fileAppender.ActivateOptions();

      log4net.Config.BasicConfigurator.Configure(fileAppender);

      LogManager.GetLogger(typeof(MvcApplication)).Info("Init");

      HomeController.Result = "";
    }
  }
}
