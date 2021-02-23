using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArdNet.Messaging;
using Microsoft.Extensions.Configuration;
using TIPC.Core.Channels;
using TIPC.Core.Tools;
using TIPC.Core.Tools.Threading;

namespace ArdNet.Server.Tests.CLI
{
    public class RobustServerCLI : IDisposable
    {
        private volatile int _tcpConnectionCount = 0;
        private MessageHub MsgHub { get; }
        private IArdNetServer ArdServer { get; }
        private LoggingMessageHubClient ArdLogger { get; }
        private ITaskThread<object> TimerThread { get; set; }

        public RobustServerCLI(bool DEBUG = false)
        {
            var configBuilder = new ConfigurationBuilder();
            _ = configBuilder.AddJsonFile("config.json", optional: false, reloadOnChange: true);
            var config = configBuilder.Build();

            int serverPort = config.GetValue<int>("ServerPort");
            string AppID = config.GetValue<string>("AppID");

            MsgHub = new MessageHub();

            _ = IPTools.TryGetLocalIP(out var localAddress);
            var netConfig = new ArdNetServerConfig(AppID, localAddress, serverPort);
            //var providerFlags = TcpDataStreamOptions.Server | TcpDataStreamOptions.Encrypted;
            //netConfig.TCP.DataStreamProvider = TcpDataStreamProvider.GetProvider(providerFlags, myCertFactory);
            ArdServer = new ArdNetServer(netConfig, MsgHub);
            ArdServer.NetConfig.TCP.HeartbeatConfig.HeartbeatInterval = TimeSpan.FromMilliseconds(1000);
            ArdServer.NetConfig.TCP.HeartbeatConfig.ForceStrictHeartbeat = true;
            ArdServer.NetConfig.TCP.HeartbeatConfig.RespondToHeartbeats = false;
            ArdServer.NetConfig.TCP.HeartbeatConfig.HeartbeatToleranceMultiplier = 0;


            ArdLogger = new LoggingMessageHubClient(
                ArdServer.MessageHub,
                MessageCategoryTypes.ExceptionMessages | MessageCategoryTypes.LoggingMessages);
            ArdLogger.LogPushed += (sender, msg) =>
            {
                Console.WriteLine($"LOG: {msg.Message}");
            };
            ArdLogger.ExceptionPushed += (sender, msg) =>
            {
                if (msg.Severity == ExceptionSeverity.Expected)
                    return;
                Console.WriteLine($"EXCEPTION: {msg.Exception}");
            };
        }


        public async Task RunServer()
        {
            MsgHub.Start();
            ArdLogger.Start();

            ArdServer.SystemStarted += Server_ServerStarted;
            ArdServer.TcpEndpointConnected += Server_TcpClientConnected;
            ArdServer.TcpEndpointDisconnected += Server_TcpClientDisconnected;
            ArdServer.TcpMessageReceived += Server_TcpMessageReceived;
            ArdServer.TcpQueryReceived += Server_TcpQueryReceived;
            ArdServer.TcpCommandReceived += Server_TcpCommandReceived;

            ArdServer.TcpCommandTable.Register("Device.Sensors.MPU", MpuHandler);

            ArdServer.Start();


            while (true)
            {
                Console.Write("SEND: ");
                var inpt = Console.ReadLine();

                if (inpt.StartsWith("??"))
                {
                    var split = inpt.TrimStart('?').Split(' ');
                    var qry = split[0];
                    var qryArgs = split.Skip(1).ToArray();
                    var resultTask = ArdServer.SendTcpQueryAsync(qry, qryArgs);
                    foreach (var e in await resultTask)
                    {
                        Server_TcpQueryResponseReceived(null, e);
                    }
                }
                else if (inpt.StartsWith("?"))
                {
                    var split = inpt.TrimStart('?').Split(' ');
                    var qry = split[0];
                    var qryArgs = split.Skip(1).ToArray();
                    //*/
                    ArdServer.SendTcpQuery(qry, qryArgs, Server_TcpQueryResponseReceived, null);
                    /*/
                    var tokenSrc = new CancellationTokenSource();
                    var qryArg = new RequestPushedArgs(qry, qryArgs, Server_TcpQueryResponseReceived, null, tokenSrc.Token, TimeSpan.FromMinutes(1));
                    var qryMsg = new TcpQueryPushedMessage(this, qryArg);
                    //tokenSrc.Cancel();
                    MsgHub.EnqueueMessage(qryMsg);
                    //*/
                }
                else if (inpt.StartsWith("!!"))
                {
                    var split = inpt.TrimStart('!').Split(' ');
                    var cmdName = split[0];
                    var cmdArgs = split.Skip(1).ToArray();
                    var resultTask = ArdServer.SendTcpCommandAsync(cmdName, cmdArgs);
                    foreach (var e in await resultTask)
                    {
                        Server_TcpCommandResponseReceived(null, e);
                    }
                }
                else if (inpt.StartsWith("!"))
                {
                    var split = inpt.TrimStart('!').Split(' ');
                    var cmdName = split[0];
                    var cmdArgs = split.Skip(1).ToArray();

                    ArdServer.SendTcpCommand(cmdName, cmdArgs, Server_TcpCommandResponseReceived, null);
                }
                else if (inpt.StartsWith("T!"))
                {
                    var split = inpt.TrimStart('T').TrimStart('!').Split(' ');
                    var topicName = split[0];
                    var topicData = split.Skip(1).ToArray();

                    ArdServer.TopicManager.SendMessage(topicName, topicData[0]);
                }
                else if (inpt.ToLower() == "cls")
                {
                    Console.Clear();
                }
                else if (inpt.ToLower() == "exit")
                {
                    return;
                }
                else
                {
                    ArdServer.SendTcpMessage(inpt);
                }

            }
        }


        private void MpuHandler(IArdNetSystem sender, RequestResponderStateObject e)
        {
            var str = $"Accel: [{e.RequestArgs[0]}, {e.RequestArgs[1]}, {e.RequestArgs[2]}] | Gyro: [{e.RequestArgs[3]}, {e.RequestArgs[4]}, {e.RequestArgs[5]}]";
            Console.WriteLine(str);
        }

        #region Generic Message Debug

        private void Server_ServerStarted(IArdNetSystem e)
        {
            Console.WriteLine("Server started @ " + e.NetConfig.LocalAddress);
        }


        private void Server_TcpClientConnected(IArdNetSystem Sender, IConnectedSystemEndpoint e)
        {
            _ = Interlocked.Increment(ref _tcpConnectionCount);
            Console.WriteLine("New TCP Connection");
            Console.WriteLine("    IP Address: " + e.Endpoint);

            e.UserState = new
            {
                Name = "Hello",
                Time = DateTime.Now
            };

            if (!ArdServer.NetConfig.TCP.HeartbeatConfig.ExpectHeartbeatResponse)
            {
                ArdServer.SendTcpCommand(e.Endpoint, "set.htbt", new string[] { "respond", "false" });
            }
        }


        private void Server_TcpClientDisconnected(IArdNetSystem Sender, ISystemEndpoint e)
        {
            _ = Interlocked.Decrement(ref _tcpConnectionCount);
            Console.WriteLine("Closed TCP Connection");
            Console.WriteLine("    Connection Time: " + ((dynamic)e.UserState).Time);
            Console.WriteLine("    IP Address: " + e.Endpoint);
        }



        private void Server_TcpMessageReceived(IArdNetSystem Sender, MessageReceivedArgs e)
        {
            Console.WriteLine("New TCP Message");
            Console.WriteLine("    Connection Time: " + ((dynamic)e.ConnectedSystem.UserState).Time);
            Console.WriteLine("    IP Address: " + e.Endpoint);
            Console.WriteLine("    Message: " + (e.Message ?? "[NULL]"));
        }



        private void Server_TcpQueryReceived(IArdNetSystem Sender, RequestReceivedArgs e)
        {
            string answer = null;
            string QryName = e.Request;
            string[] QryArgs = e.RequestArgs;

            switch (QryName)
            {
                case "name":
                    {
                        answer = System.Environment.MachineName;
                        break;
                    }
                case "has.feat":
                    {
                        if (QryArgs.Length == 0)
                            break;
                        if (QryArgs[0] == "messaging")
                            answer = "true";
                        if (QryArgs[0] == "topics")
                            answer = "true";
                        break;
                    }
                default:
                    {
                        Console.WriteLine(e.Request);
                        ArdServer.SendTcpQueryResponse(e, e.Request, e.RequestArgs);
                        return;
                    }
            }


            ArdServer.SendTcpQueryResponse(e, answer);
        }


        private void Server_TcpQueryResponseReceived(IArdNetSystem Sender, RequestResponseReceivedArgs e)
        {
            if (e is RequestResponseTimeoutArgs)
            {
                Console.WriteLine($"Query Timeout");
            }
            else if (e is RequestResponseCanceledArgs)
            {
                Console.WriteLine($"Query Canceled");
            }
            else if (e is RequestResponseDisconnectedArgs)
            {
                Console.WriteLine($"Query Disconnected");
            }
            else if (e is RequestResponseInvalidTargetArgs)
            {
                Console.WriteLine($"Query Not Delivered");
            }
            else
            {
                Console.WriteLine($"Query Response");
            }

            Console.WriteLine("    IP Address: " + e.Endpoint);
            Console.WriteLine("    Query ID: " + e.RequestID);
            Console.WriteLine("    Response: " + (e.Response ?? "[NULL]"));
            for (int i = 0; i < e.ResponseArgs.Length; ++i)
            {
                Console.WriteLine($"    Arg [{i}]: {e.ResponseArgs[i] ?? "[NULL]"}");
            }
        }


        private void Server_TcpCommandReceived(IArdNetSystem Sender, RequestReceivedArgs e)
        {
            string status = null;
            string CmdName = e.Request;
            string[] CmdArgs = e.RequestArgs;

            switch (CmdName)
            {
                //set heartbeat
                case "set.htbt":
                    {
                        if (CmdArgs.Length != 2)
                            break;

                        var attributeName = CmdArgs[0];
                        var attributeValue = CmdArgs[1];

                        //set HeartBeat "respond to heartbeat"
                        if (attributeName == "respond")
                        {
                            if (bool.TryParse(attributeValue, out var boolVal))
                            {
                                e.ConnectedSystem.HeartbeatConfig.RespondToHeartbeats = boolVal;
                                status = "ok";
                            }
                        }
                        else if (attributeName == "interval")
                        {
                            if (int.TryParse(attributeValue, out var intVal))
                            {
                                e.ConnectedSystem.HeartbeatConfig.HeartbeatInterval = TimeSpan.FromMilliseconds(intVal);
                                status = "ok";
                            }
                        }
                        break;
                    }
                //set device parameters
                case "set.device":
                    {
                        if (CmdArgs.Length != 2)
                            break;

                        var attributeName = CmdArgs[0];
                        var attributeValue = CmdArgs[1];

                        //set power level
                        if (attributeName == "power")
                        {
                            if (attributeValue == "high")
                            {
                                //.1 sec htbt
                                e.ConnectedSystem.HeartbeatConfig.HeartbeatInterval = TimeSpan.FromMilliseconds(100);
                                status = "ok";
                            }
                            else if (attributeValue == "default")
                            {
                                //.25 sec htbt
                                e.ConnectedSystem.HeartbeatConfig.HeartbeatInterval = TimeSpan.FromMilliseconds(250);
                                status = "ok";
                            }
                            else if (attributeValue == "low")
                            {
                                //1 sec htbt
                                e.ConnectedSystem.HeartbeatConfig.HeartbeatInterval = TimeSpan.FromMilliseconds(1000);
                                status = "ok";
                            }
                            else if (attributeValue == "xlow")
                            {
                                //10 sec htbt
                                e.ConnectedSystem.HeartbeatConfig.HeartbeatInterval = TimeSpan.FromMilliseconds(10000);
                                status = "ok";
                            }
                            else if (attributeValue == "xxlow")
                            {
                                //1 min htbt
                                e.ConnectedSystem.HeartbeatConfig.HeartbeatInterval = TimeSpan.FromMinutes(1);
                                status = "ok";
                            }
                            else if (attributeValue == "xxxlow")
                            {
                                //no htbt
                                e.ConnectedSystem.HeartbeatConfig.ForceStrictHeartbeat = false;
                                status = "ok";
                            }
                        }

                        break;
                    }
                //set text color
                case "set.color":
                    {
                        if (CmdArgs.Length == 0)
                            break;

                        if (Enum.TryParse<ConsoleColor>(CmdArgs[0], out var color))
                        {
                            Console.ForegroundColor = color;
                            status = "ok";
                        }

                        break;
                    }
            }

            ArdServer.SendTcpCommandResponse(e, status);
        }


        private void Server_TcpCommandResponseReceived(IArdNetSystem Sender, RequestResponseReceivedArgs e)
        {
            if (e is RequestResponseTimeoutArgs)
            {
                Console.WriteLine($"Command Timeout");
            }
            else if (e is RequestResponseCanceledArgs)
            {
                Console.WriteLine($"Command Canceled");
            }
            else if (e is RequestResponseDisconnectedArgs)
            {
                Console.WriteLine($"Command Disconnected");
            }
            else if (e is RequestResponseInvalidTargetArgs)
            {
                Console.WriteLine($"Command Not Delivered");
            }
            else
            {
                Console.WriteLine($"Command Response");
            }

            Console.WriteLine("    IP Address: " + e.Endpoint);
            Console.WriteLine("    Command ID: " + e.RequestID);
            Console.WriteLine("    Response: " + (e.Response ?? "[NULL]"));
            for (int i = 0; i < e.ResponseArgs.Length; ++i)
            {
                Console.WriteLine($"    Arg [{i}]: {e.ResponseArgs[i] ?? "[NULL]"}");
            }
        }

        #endregion Generic Message Debug

        public void Dispose()
        {
            TimerThread?.Dispose();
            TimerThread?.Interrupt();
            ArdLogger.Dispose();
            ArdServer.Dispose();
            MsgHub.Dispose();
        }

    }
}
