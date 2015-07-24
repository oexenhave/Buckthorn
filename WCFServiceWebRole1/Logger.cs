namespace Buckthorn.ServiceRole
{
    using System;
    using System.Diagnostics;

    public class Logger
    {
        private static Logger instance;

        public static Logger Instance => instance ?? (instance = new Logger());

        public void AddInformation(ClientData client, string message)
        {
            this.AddInformation("[" + client.Ip + "] " + message);
        }

        public void AddInformation(string message)
        {
            Trace.WriteLine(DateTime.Now.ToString("yyyyMMddHHmmss") + " INFO " + message);
        }

        public void AddError(ClientData client, string message, Exception ex)
        {
            this.AddError("[" + client.Ip + "] " + message, ex);
        }

        public void AddError(string message, Exception ex)
        {
            Trace.WriteLine(DateTime.Now.ToString("yyyyMMddHHmmss") + " ERROR " + message);
            Trace.WriteLine(DateTime.Now.ToString("yyyyMMddHHmmss") + " ERROR " + ex.Message);
        }

        public void AddWarning(ClientData client, string message)
        {
            this.AddWarning("[" + client.Ip + "] " + message);
        }

        public void AddWarning(string message)
        {
            Trace.WriteLine(DateTime.Now.ToString("yyyyMMddHHmmss") + " WARNING " + message);
        }
    }
}