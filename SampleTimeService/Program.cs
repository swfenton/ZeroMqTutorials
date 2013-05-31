using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQ;

namespace ReqRep
{
    public static class Endpoints
    {
        public const string InProc = "inproc://foo";
        public const string TCP = "tcp://127.0.0.1:1024";
        public static readonly string MyEndpoint = TCP;
    }

    class Program
    {
        static void Main(string[] args)
        {
            new Program().run();
        }

        private void run()
        {
            var ctx = ZmqContext.Create();
            var component = new Component(ctx);
            Console.WriteLine("Component running. Hit enter to exit...");
            Console.ReadKey();
            component.Dispose();
        }
    }

    public class Client
    {
        private readonly string _id;
        private readonly CancellationToken _token;
        private readonly string _topic;

        public Client(string id, CancellationToken token, ZmqContext ctx, string topic)
        {
            _id = id;
            _token = token;
            _topic = topic;
            var ready = new TaskCompletionSource<bool>();
            Task.Factory.StartNew(() => start(ctx, ready));
            ready.Task.Wait();
        }

        private void start(ZmqContext ctx, TaskCompletionSource<bool> ready)
        {
            using (var socket = ctx.CreateSocket(SocketType.SUB))
            {
                socket.Subscribe(Encoding.ASCII.GetBytes(_topic));
                socket.Connect(Endpoints.MyEndpoint);

                Console.WriteLine("Client {0} ready.", _id);
                ready.SetResult(true);

                while (_token.IsCancellationRequested == false)
                {
                    var topic = socket.Receive(Encoding.ASCII);
                    var message = socket.Receive(Encoding.ASCII);
                    Console.WriteLine("[{0}]: Got '{1}'", _id, message);
                }
            }
        }
    }

    public class Server
    {
        private readonly string _id;
        private readonly CancellationToken _token;

        public Server(string id, CancellationToken token, ZmqContext ctx)
        {
            _id = id;
            _token = token;
            var ready = new TaskCompletionSource<bool>();
            Task.Factory.StartNew(() => start(ctx, ready));
            ready.Task.Wait();
        }

        private void start(ZmqContext ctx, TaskCompletionSource<bool> ready)
        {
            var rand = new Random(DateTime.Now.Millisecond);

            using (var socket = ctx.CreateSocket(SocketType.PUB))
            {
                socket.Bind(Endpoints.MyEndpoint);
                Console.WriteLine("Server {0} ready and listening on foo.", _id);
                ready.SetResult(true);

                while (_token.IsCancellationRequested == false)
                {
                    var topic = (rand.Next(1, 10) > 5) ? "weather" : "sports";

                    var message = string.Format("BREAKING NEWS: {0}.", topic);
                        
                    socket.SendMore(topic, Encoding.ASCII);
                    socket.Send(message, Encoding.ASCII);
                    
                    Task.Delay(2000).Wait();
                }
            }
        }
    }

    public class Component : IDisposable
    {
        private readonly CancellationTokenSource _tokenSource;

        public Component(ZmqContext ctx)
        {
            _tokenSource = new CancellationTokenSource();
            new Client("client1", _tokenSource.Token, ctx, "weather");
            Task.Delay(1000).Wait();
            new Client("client2", _tokenSource.Token, ctx, "sports");

            
            Task.Delay(3000).Wait();
            new Server("server1", _tokenSource.Token, ctx);
        }

        private bool _disposed;
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _tokenSource.Cancel();
        }
    }
}
