using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQ;

namespace ReqRep
{
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
                socket.Connect(Endpoints.InProcFront);

                Console.WriteLine("Client {0} ready.", _id);
                ready.SetResult(true);

                while (_token.IsCancellationRequested == false)
                {
                    var message = string.Format("[{0}]: What's the time?", _id);
                    socket.Send(message, Encoding.ASCII);
                    
                    var result = socket.Receive(Encoding.ASCII);
                    Console.WriteLine("[{0}]: Got {1}.", _id, result);
                }
            }
        }
    }
}