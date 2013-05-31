using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQ;

namespace ReqRep
{
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

            using (var socket = ctx.CreateSocket(SocketType.REP))
            {
                socket.Connect(Endpoints.InProcBack);
                Console.WriteLine("Server {0} ready.", _id);
                ready.SetResult(true);

                while (_token.IsCancellationRequested == false)
                {
                    var request = socket.Receive(Encoding.ASCII);
                    Console.WriteLine("[{0}] Got {1}.", _id, request);
                    Task.Delay(5000).Wait();

                    var message = string.Format("{0} - [{1}]", DateTime.Now, _id);
                    socket.Send(message, Encoding.ASCII);                    
                }
            }
        }
    }
}