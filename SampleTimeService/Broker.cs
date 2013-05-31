using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQ;

namespace ReqRep
{
    public class Broker
    {
        private readonly CancellationToken _token;

        public Broker(CancellationToken token, ZmqContext ctx)
        {
            _token = token;
            var ready = new TaskCompletionSource<bool>();
            Task.Factory.StartNew(() => start(ctx, ready));
        }

        private void start(ZmqContext ctx, TaskCompletionSource<bool> ready)
        {
            using(var router = ctx.CreateSocket(SocketType.ROUTER))
            using (var dealer = ctx.CreateSocket(SocketType.DEALER))
            {
                router.Bind(Endpoints.InProcFront);
                dealer.Bind(Endpoints.InProcBack);

                router.ReceiveReady += (s, e) => relay(router, dealer);
                dealer.ReceiveReady += (s, e) => relay(dealer, router);

                var poller = new Poller(new[] {router, dealer});
                ready.SetResult(true);

                while (_token.IsCancellationRequested == false)
                {
                    poller.Poll(TimeSpan.FromMilliseconds(100));
                }

                poller.Dispose(); ;
            }
        }

        readonly byte[] _buffer = new byte[1024];
        private void relay(ZmqSocket source, ZmqSocket destination)
        {
            bool hasMore = true;

            while (hasMore)
            {
                var number = source.Receive(_buffer);
                hasMore = source.ReceiveMore;

                if (hasMore)
                {
                    destination.SendMore(_buffer.Take(number).ToArray());
                }
                else
                {
                    destination.Send(_buffer.Take(number).ToArray());
                }
            }
        }
    }
}
