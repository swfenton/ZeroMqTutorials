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

        public Client(string id, CancellationToken token, ZmqContext ctx)
        {
            _id = id;
            _token = token;
            var ready = new TaskCompletionSource<bool>();
            Task.Factory.StartNew(() => start(ctx, ready));
            ready.Task.Wait();
        }

        private void start(ZmqContext ctx, TaskCompletionSource<bool> ready)
        {
            using (var socket = ctx.CreateSocket(SocketType.REQ))
            {
                socket.Connect("inproc://foo");
                Console.WriteLine("Client {0} ready.", _id);
                ready.SetResult(true);

                while (_token.IsCancellationRequested == false)
                {
                    var message = string.Format("[{0}]: What's the time?", _id);
                    socket.Send(message, Encoding.ASCII);
                    var result = socket.Receive(Encoding.ASCII);
                    Console.WriteLine("[{0}]: Got {1}.", _id, result);

                    Task.Delay(2000).Wait(_token);
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
            using (var socket = ctx.CreateSocket(SocketType.REP))
            {
                socket.Bind("inproc://foo");
                Console.WriteLine("Server {0} ready and listening on foo.", _id);
                ready.SetResult(true);

                while (_token.IsCancellationRequested == false)
                {
                    var message = socket.Receive(Encoding.ASCII);
                    Console.WriteLine("[{0}]: Got {1}", _id, message);
                    var result = DateTime.Now.ToString(CultureInfo.InvariantCulture);
                    socket.Send(result, Encoding.ASCII);
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
            new Server("server1", _tokenSource.Token, ctx);
            Task.Delay(3000).Wait();
            new Client("client1", _tokenSource.Token, ctx);
            Task.Delay(1000).Wait();
            new Client("client2", _tokenSource.Token, ctx);
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
