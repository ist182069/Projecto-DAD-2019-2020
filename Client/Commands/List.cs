﻿using MSDAD.Client.Comunication;
using MSDAD.Client.Exceptions;
using MSDAD.Library;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MSDAD.Client.Commands

{
    class List : Command
    {
        public List(ref ClientLibrary client_library) : base(ref client_library)
        {
        }
        public override object Execute()
        {
            List<MeetingView> meeting_views = this.client_library.GetMeetingViews();
            Dictionary<string,string> meeting_query = new Dictionary<string,string>();

            foreach (MeetingView mV in meeting_views)
            {
                Console.WriteLine(mV.MeetingTopic + " " + mV.MeetingState + " " + mV.MeetingInfo + " " + mV.MeetingVersion + " "); 
                meeting_query.Add(mV.MeetingTopic, mV.MeetingState);
            }

            while (true)
            {
                try
                {
                    this.remote_server.List(meeting_query, this.client_identifier);
                    break;
                }
                catch (ServerCoreException sce)
                {
                    Console.WriteLine(sce.Message);
                }
                catch (Exception exception) when (exception is System.Net.Sockets.SocketException || exception is System.IO.IOException)
                {
                    this.remote_server = new ServerChange(ref base.client_library).Execute();
                    if (this.remote_server != null)
                    {
                        try
                        {
                            int n_replicas = this.remote_server.Hello(this.client_identifier, this.client_remoting, this.client_ip, this.client_port);
                            base.client_library.NReplicas = n_replicas;
                        }
                        catch (System.Net.Sockets.SocketException)
                        {
                            Console.WriteLine("We cannot find anymore servers to connect to! Aborting...");
                            CrashClientProcess();
                        }
                        continue;
                    }
                    else
                    {
                        Console.WriteLine("We cannot find anymore servers to connect to! Aborting...");
                        CrashClientProcess();
                    }
                }
            }
            

            foreach (MeetingView mV in meeting_views)
            {
                Console.WriteLine("Topic:"+mV.MeetingTopic + " State:" + mV.MeetingState + "\nVersion:" + mV.MeetingVersion);
                if(mV.MeetingInfo != null)
                {
                    Console.WriteLine(mV.MeetingInfo);
                }
                Console.Write("\n");
            }

            return null;
        }
    }
}
