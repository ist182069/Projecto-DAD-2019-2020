﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

namespace MSDAD
{
    namespace Server
    {
        class Server
        {
            static void Main(string[] args)
            {
                string port;

                port = Console.ReadLine();
                TcpChannel channel = new TcpChannel(Int32.Parse(port));
                ChannelServices.RegisterChannel(channel, true);
                RemotingConfiguration.RegisterWellKnownServiceType(typeof(RemoteServer), "RemoteServer", WellKnownObjectMode.Singleton);

                System.Console.ReadLine();
            }
        }
    }
    
}