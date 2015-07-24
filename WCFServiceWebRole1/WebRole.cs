namespace Buckthorn.ServiceRole
{
    using System.Diagnostics;

    using Microsoft.WindowsAzure.ServiceRuntime;

    public class WebRole : RoleEntryPoint
    {
        public SmtpServer Server;

        public override bool OnStart()
        {
            Logger.Instance.AddInformation("OnStart");
            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            this.Server = new SmtpServer();
            this.Server.StartListening();

            return base.OnStart();
        }

        public override void OnStop()
        {
            Logger.Instance.AddInformation("OnStop");
            this.Server?.StopListening();

            base.OnStop();
        }
    }
}
