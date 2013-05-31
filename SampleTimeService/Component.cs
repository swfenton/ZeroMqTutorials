using System;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQ;

namespace ReqRep
{
    public class Component : IDisposable
    {
        private readonly CancellationTokenSource _tokenSource;

        public Component(ZmqContext ctx)
        {
            _tokenSource = new CancellationTokenSource();

            new Broker(_tokenSource.Token, ctx);
            Task.Delay(500).Wait();
            new Server("server1", _tokenSource.Token, ctx);

            new Client("client1", _tokenSource.Token, ctx);
            Task.Delay(500).Wait();
            new Client("client2", _tokenSource.Token, ctx);

            Task.Delay(5000).Wait();
            new Server("server2", _tokenSource.Token, ctx);
            

            Task.Delay(5000).Wait();
            new Client("client3", _tokenSource.Token, ctx);
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