using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatsonWebsocket;

namespace VirtualPrinter
{
    internal class Server
    {
        private WatsonWsServer server;

        public Server()
        {
            server = new WatsonWsServer(Environment.GetCommandLineArgs().ElementAtOrDefault(2) ?? "127.0.0.1", 56466, false);
            server.ClientConnected += ClientConnected;
            server.ClientDisconnected += ClientDisconnected;
            server.MessageReceived += MessageReceived;
            Console.WriteLine($"Running on 56466");
            server.Start();
            _ = Task.Run(async () =>
            {
                try
                {

                    var filename = Environment.GetCommandLineArgs().ElementAtOrDefault(3) ?? "C:\\pdf";
                    Console.WriteLine($"listening {filename}");
                    var watcher = new FileSystemWatcher(filename);
                    watcher.NotifyFilter = NotifyFilters.LastWrite;
                    watcher.EnableRaisingEvents = true;
                    watcher.Filter = "*.*";
                    watcher.Error += Watcher_Error;
                    watcher.Changed += Watcher_Changed;
                    await Task.Delay(-1);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
        }

        private async void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (File.GetAttributes(e.FullPath).HasFlag(FileAttributes.Directory))
            {
                return;
            }
            Console.WriteLine("new file");
            var file = File.ReadAllBytes(e.FullPath);
            foreach (var client in server.ListClients())
            {
                await server.SendAsync(client.Guid, file);
            }
            File.Delete(e.FullPath);
        }

        private void Watcher_Error(object sender, ErrorEventArgs e)
        {
            Console.WriteLine(e.GetException());
        }

        void ClientConnected(object sender, ConnectionEventArgs args)
        {

            Console.WriteLine("Client connected: " + args.Client.ToString());
        }

        void ClientDisconnected(object sender, DisconnectionEventArgs args)
        {
            Console.WriteLine("Client disconnected: " + args.Client.ToString());
        }

        void MessageReceived(object sender, MessageReceivedEventArgs args)
        {
            Console.WriteLine("Message received from " + args.Client.ToString() + ": " + Encoding.UTF8.GetString(args.Data));
        }
    }
}
