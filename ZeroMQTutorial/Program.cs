using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroMQTutorial
{
    class Program
    {
        static void Main(string[] args)
        {
            new Program().run();
        }

        private void run()
        {
            var component = new Component();
            Console.WriteLine("Component running. Hit enter to exit...");
            Console.ReadKey();
            component.Dispose();
        }
    }

    public class Server
    {
        private readonly string _id;
        private readonly CancellationToken _token;

        public Server(string id, CancellationToken token)
        {
            _id = id;
            _token = token;
            Task.Factory.StartNew(start);
        }

        private void start()
        {
            while (_token.IsCancellationRequested == false)
            {
                Console.WriteLine("In Server {0}", _id);
                Task.Delay(2000).Wait(_token);
            }
        }
    }

    public class Component : IDisposable
    {
        private readonly CancellationTokenSource _tokenSource;

        public Component()
        {
            _tokenSource = new CancellationTokenSource();
            new Server("server1", _tokenSource.Token);
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
