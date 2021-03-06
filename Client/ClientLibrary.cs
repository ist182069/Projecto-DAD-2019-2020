﻿using MSDAD.Client.Commands;
using MSDAD.Client.Comunication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace MSDAD.Client
{
    class ClientLibrary
    {
        int client_port, n_replicas;
        string client_ip, client_identifier, client_remoting, server_url;
        ClientCommunication client_communication;

        private List<MeetingView> meeting_views = new List<MeetingView>();

        public ClientLibrary(string client_identifier, string client_remoting, string server_url, string client_ip, int client_port)
        {
            this.client_identifier = client_identifier;
            this.server_url = server_url;
            this.client_ip = client_ip;
            this.client_port = client_port;
            this.client_remoting = client_remoting;

            this.client_communication = new ClientCommunication(this);

            Console.Write("Starting client remoting service... ");
            client_communication.Start();
        }

        public void AddMeetingView(MeetingView meeting_view)
        {
            lock(this)
            {
                foreach (MeetingView mV in meeting_views)
                {
                    if (mV.MeetingTopic.Equals(meeting_view.MeetingTopic))
                    {
                        meeting_views.Remove(mV);
                        break;
                    }
                }

                this.meeting_views.Add(meeting_view);
            }
        }

        public ClientCommunication ClientCommunication
        {
            get
            {
                return this.client_communication;
            }
        }

        public string ClientIdentifier
        {
            get
            {
                return this.client_identifier;
            }
            
        }

        public string ServerURL
        {
            get
            {
                return this.server_url;
            }
            set
            {
                this.server_url = value;
            }
            
        }
        public int ClientPort
        {
            get
            {
                return this.client_port;
            }
        }

        public string ClientIP
        {
            get
            {
                return this.client_ip;
            }
        }

        public string ClientRemoting
        {
            get
            {
                return this.client_remoting;
            }
        }

        public List<MeetingView> GetMeetingViews()
        {
            lock (this)
            {
                return this.meeting_views;
            }   
        }

        private void SetClientPort(int client_port)
        {
            this.client_port = client_port;
        }

        private void SetMeetingViews(List<MeetingView> meeting_views)
        {
            lock (this)
            {
                this.meeting_views = meeting_views;
            }
        }      
        
        public int NReplicas
        {
            get
            {
                return this.n_replicas;
            }
            set
            {
                this.n_replicas = value;
            }
        }
    }
}
