namespace Buckthorn.ServiceRole
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics.CodeAnalysis;
    using System.Net.Mail;
    using System.Text;

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum ClientState { EHLO, FROM, TO, DATA, QUIT, UNKNOWN }

    public class ClientData
    {
        public ClientData()
        {
            this.State = ClientState.EHLO;
            this.Ip = "0.0.0.0";

            this.Attachments = new List<Attachment>();
            this.Bcc = new List<MailAddress>();
            this.Cc = new List<MailAddress>();
            this.From = null;
            this.Headers = new NameValueCollection();
            this.To = new List<MailAddress>();
            this.Subject = string.Empty;
            this.Body = string.Empty;
        }

        public ClientState State { get; set; }

        public string Ip { get; set; }

        public MailAddress From { get; set; }

        public IList<MailAddress> To { get; set; }

        public IList<MailAddress> Cc { get; set; }

        public IList<MailAddress> Bcc { get; set; }

        public NameValueCollection Headers { get; set; }

        public IList<Attachment> Attachments { get; set; }

        public string Subject { get; set; }

        public string Body { get; set; }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("From: " + this.From + Environment.NewLine);
            result.AppendLine("To: " + this.To + Environment.NewLine);
            result.AppendLine("Subject: " + this.Subject + Environment.NewLine);
            result.AppendLine("Body: " + this.Body + Environment.NewLine);
            return result.ToString();
        }
    }
}