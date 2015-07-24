namespace Buckthorn.ServiceRole
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;

    public class SmtpServer
    {
        public int ClientTimeout => 10000;

        public SmtpServer()
        {
            this.encoder = new ASCIIEncoding();
        }

        private TcpListener server;
        private Thread serverThread;
        private readonly ASCIIEncoding encoder;

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum MessageType { EHLO, FROM, TO, DATA, CONTENT, CONTENTEND, QUIT, UNKNOWN }

        #region Server thread
        public void StartListening()
        {
            this.serverThread = new Thread(this.RunServer);
            this.serverThread.Start();
        }

        public void StopListening()
        {
            this.server?.Stop();
            this.serverThread?.Abort();
        }

        public void RestartListening()
        {
            this.StopListening();

            Thread.Sleep(1000);

            this.StartListening();
        }

        private void RunServer()
        {
            try
            {
                this.server = new TcpListener(IPAddress.Any, 25) { ExclusiveAddressUse = false };
                this.server.Start();

                while (true)
                {
                    TcpClient client = this.server.AcceptTcpClient();
                    Thread clientThread = new Thread(this.HandleClient);
                    clientThread.Start(client);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.AddError("Restarting server due to an error.", ex);
                this.RestartListening();
            }
        }
        #endregion

        #region Client methods
        private void HandleClient(object rawClient)
        {
            ClientData client = new ClientData();

            try
            {
                // Prepare the connection.
                using (TcpClient tcpClient = (TcpClient)rawClient)
                {
                    NetworkStream clientStream = tcpClient.GetStream();

                    // Prepare the state variables.
                    byte[] buffer = new byte[4096];
                    string message = "";
                    bool isNewMessage = true;
                    bool running = true;

                    // Make sure we don't have rawClient hanging too long. Miliseconds.
                    clientStream.ReadTimeout = this.ClientTimeout;
                    clientStream.WriteTimeout = this.ClientTimeout;

                    var endPoint = tcpClient.Client.RemoteEndPoint as IPEndPoint;
                    if (endPoint != null)
                    {
                        client.Ip = endPoint.Address.ToString();
                    }

                    Logger.Instance.AddInformation(client, "### Client connected");

                    // Greet the rawClient.
                    this.SendClientData("220 buckthurn.oexenhave.dk SMTP Server", client, ref clientStream);

                    while (running)
                    {
                        // Reset the string is the message has been processed.
                        if (isNewMessage) message = "";

                        // Do a read from the socket. Break if anything unexpected goes on.
                        int bytesRead;
                        try
                        {
                            bytesRead = clientStream.Read(buffer, 0, 4096);
                        }
                        catch (Exception)
                        {
                            Logger.Instance.AddInformation(client, "### Client inactive");
                            this.SendClientData("500 Inactive", client, ref clientStream);
                            break;
                        }

                        if (bytesRead == 0) { break; }

                        message += this.encoder.GetString(buffer, 0, bytesRead);

                        // Allow telnet-like connections to work as well.
                        isNewMessage = message.EndsWith("\r\n");

                        // Process the message
                        if (isNewMessage)
                        {
                            // Process one line at a time.
                            foreach (string messageLine in message.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                Logger.Instance.AddInformation(client, messageLine.Substring(0, Math.Min(255, messageLine.Length)));

                                // Intepretate the message.
                                this.ProcessMessage(this.GetMessageType(client, messageLine), messageLine, ref client, ref clientStream, ref running);
                            }
                        }
                    }

                    clientStream.Close();
                    tcpClient.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.AddError(client, "### Ending client thread due to an error", ex);
            }

            Logger.Instance.AddInformation(client, "### Client disconnected");
        }

        private void ProcessMessage(MessageType type, string message, ref ClientData client, ref NetworkStream clientStream, ref bool running)
        {
            switch (type)
            {
                case MessageType.EHLO:
                    {
                        if (client.State == ClientState.EHLO)
                        {
                            this.SendClientData("250 buckthurn.oexenhave.dk", client, ref clientStream);
                            client.State = ClientState.FROM;
                        }
                        else
                            this.SendClientData("503 Bad sequence of commands", client, ref clientStream);
                        break;
                    }
                case MessageType.FROM:
                    {
                        this.SendClientData("250 OK", client, ref clientStream);
                        client.State = ClientState.TO;
                        break;
                    }
                case MessageType.TO:
                    {
                        if (client.State == ClientState.TO || client.State == ClientState.DATA)
                        {
                            if (!message.Contains("@buckthurn.oexenhave.dk"))
                            {
                                this.SendClientData("550 No such user here", client, ref clientStream);
                                Logger.Instance.AddInformation("### Email disallowed: " + message);
                            }
                            else
                            {
                                this.SendClientData("250 OK", client, ref clientStream);
                                client.State = ClientState.DATA;
                            }
                        }
                        else
                            this.SendClientData("503 Bad sequence of commands", client, ref clientStream);
                        break;
                    }
                case MessageType.DATA:
                    {
                        if (client.State == ClientState.DATA)
                        {
                            this.SendClientData("354 Start mail input; end with <CRLF>.<CRLF>", client, ref clientStream);
                            client.State = ClientState.DATA;
                        }
                        else
                            this.SendClientData("503 Bad sequence of commands", client, ref clientStream);
                        break;
                    }
                case MessageType.CONTENT:
                    {
                        Logger.Instance.AddInformation(message);
                        //if (Message.IndexOf("Mindtrio-Quasar-Ident") > -1)
                        //{
                        //    client.Ident = Message.Replace("Mindtrio-Quasar-Ident: ", "");
                        //}

                        //if (Message.IndexOf("Mindtrio-Quasar-Type") > -1)
                        //{
                        //    client.Type = Message.Replace("Mindtrio-Quasar-Type: ", "");
                        //}

                        //if (!String.IsNullOrEmpty(client.Ident) && !String.IsNullOrEmpty(client.Type))
                        //{
                        //    this.PushClient.ReportBounce(client.Type, client.Ident, this.AppSettings["PushKey"]);
                        //    if (logger.IsInfoEnabled) logger.Info("Bounce reported. Type: {" + client.Type + "}. Ident: {" + client.Ident + "}.");
                        //    client.Type = "";
                        //    client.Ident = "";
                        //}

                        break;
                    }
                case MessageType.CONTENTEND:
                    {
                        this.SendClientData("250 OK", client, ref clientStream);
                        client.State = ClientState.QUIT;
                        break;
                    }
                case MessageType.QUIT:
                    {
                        running = false;
                        break;
                    }
                case MessageType.UNKNOWN:
                    {
                        this.SendClientData("500 Syntax error, command unrecognized", client, ref clientStream);
                        break;
                    }
            }
        }

        private MessageType GetMessageType(ClientData data, string message)
        {
            MessageType type;
            if (data.State == ClientState.DATA && message.Equals(".", StringComparison.OrdinalIgnoreCase))
                type = MessageType.CONTENTEND;
            else if (data.State == ClientState.DATA && message.StartsWith("DATA"))
                type = MessageType.DATA;
            else if (data.State == ClientState.DATA)
                type = MessageType.CONTENT;
            else if (message.StartsWith("EHLO", StringComparison.OrdinalIgnoreCase) || message.StartsWith("HELO", StringComparison.OrdinalIgnoreCase))
                type = MessageType.EHLO;
            else if (message.StartsWith("MAIL FROM:", StringComparison.OrdinalIgnoreCase))
                type = MessageType.FROM;
            else if (message.StartsWith("RCPT TO:", StringComparison.OrdinalIgnoreCase))
                type = MessageType.TO;
            else if (message.StartsWith("QUIT"))
                type = MessageType.QUIT;
            else
                type = MessageType.UNKNOWN;
            return type;
        }
        #endregion

        #region Auxiliary methods
        private void SendClientData(string message, ClientData client, ref NetworkStream clientStream)
        {
            byte[] buffer = this.encoder.GetBytes(message + "\r\n");
            clientStream.Write(buffer, 0, buffer.Length);
            clientStream.Flush();

            Logger.Instance.AddInformation(client, message.Substring(0, Math.Min(255, message.Length)));
        }
        #endregion
    }
}
