﻿using MSDAD.Library;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

namespace MSDAD.Client.Comunication
{        
    class ClientCommunication
    {
        int client_port;
        string client_ip, client_identifier;

        ClientLibrary client_library;
        RemoteClient remote_client;
        TcpChannel channel;

        public ClientCommunication(ClientLibrary client_library, string client_identifier, string client_ip, int client_port)
        {
            this.client_identifier = client_identifier;
            this.client_port = client_port;
            this.client_ip = client_ip;
            this.client_library = client_library;
        }
        public void Start()
        {
            channel = new TcpChannel(this.client_port);
            ChannelServices.RegisterChannel(channel, true);

            this.remote_client = new RemoteClient(this);
            RemotingServices.Marshal(this.remote_client, client_identifier, typeof(RemoteClient));
        }
  
        public void AddMeetingView(string topic, int version, string state)
        {
            MeetingView meetingView;

            meetingView = new MeetingView(topic, version, state);

            this.client_library.AddMeetingView(meetingView);
        }    
    }
}
