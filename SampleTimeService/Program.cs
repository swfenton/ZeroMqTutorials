using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ZeroMQ;

namespace ReqRep
{
    public static class Endpoints
    {
        public const string InProcFront = "inproc://front";
        public const string InProcBack = "inproc://back";
        public const string TCP = "tcp://127.0.0.1:1024";
        //public static readonly string MyEndpoint = TCP;
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
}
