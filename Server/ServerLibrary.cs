﻿using MSDAD.Library;
using MSDAD.Server.Communication;
using MSDAD.Server.Logs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace MSDAD.Server
{
    class ServerLibrary
    {
        int server_port, tolerated_faults, min_delay, max_delay;
        string server_ip, server_identifier, server_remoting;
        ServerCommunication server_communication;

        private readonly object event_list_lock = new object();
        private List<Meeting> event_list = new List<Meeting>();
        private List<Location> known_locations = new List<Location>();

        public ServerLibrary(string server_identifier, string server_remoting, string server_ip, int server_port, int tolerated_faults, int min_delay, int max_delay)
        {
            this.server_identifier = server_identifier;
            this.server_ip = server_ip;
            this.server_port = server_port;
            this.server_remoting = server_remoting;
            this.tolerated_faults = tolerated_faults;
            this.min_delay = min_delay;
            this.max_delay = max_delay;

            this.server_communication = new ServerCommunication(this); ;

            Console.Write("Starting server remoting service... ");
            server_communication.Start();
            Console.WriteLine("Success!");
        }

        public void Create(string meeting_topic, int min_attendees, List<string> slots, List<string> invitees, string client_identifier)
        {
            Meeting m;
            List<Tuple<Location, DateTime>> parsedSlots = ListOfParsedSlots(slots);

            m = new Meeting(meeting_topic, min_attendees, parsedSlots, invitees, client_identifier);
            event_list.Add(m);
        }

        public void Join(string meeting_topic, List<string> slots, string client_identifier, int version)
        {
            Meeting meeting = null;

            try
            {
                List<Tuple<Location, DateTime>> parsedSlots = ListOfParsedSlots(slots);
                meeting = GetMeeting(meeting_topic);
                meeting.Apply(parsedSlots, client_identifier, version);
            }
            catch (ServerCoreException sce)
            {
                throw sce;
            }
        }

        public void Close(String meeting_topic, string client_identifier, int version)
        {
            GetMeeting(meeting_topic).Schedule(client_identifier, version);
            Console.Write("Please run a command to be run on the server: ");
        }

        public void Status()
        {
            string client_identifier;
            string client_url;

            List<string> going_clients;
            List<string> invitees;

            List<Room> rooms;
            List<DateTime> reserved_dates;

            Dictionary<string, string> client_dictionary;
            Dictionary<Tuple<Location, DateTime>, List<string>> slots_clients_dictionary;

            client_dictionary = this.GetClients();

            foreach (Location location in this.known_locations)
            {
                Console.Write("Location: ");
                Console.WriteLine(location.Name);

                rooms = location.Rooms;

                foreach (Room room in rooms)
                {
                    Console.WriteLine("  Room: " + room.Identifier);
                    Console.WriteLine("  Capacity: " + room.Capacity);
                    Console.WriteLine("  Dates Reserved: ");
                    reserved_dates = room.ReservedDates;

                    foreach (DateTime dateTime in reserved_dates)
                    {
                        Console.WriteLine("    " + dateTime.ToString("yyyy-MMMM-dd"));
                    }
                }
            }

            foreach (KeyValuePair<string, string> client_pair in client_dictionary)
            {
                client_identifier = client_pair.Key;
                client_url = client_pair.Value;
                Console.Write("Client: ");
                Console.WriteLine(client_identifier + " / " + client_url);
            }

            foreach (Meeting meeting in this.event_list)
            {
                Console.Write("Meeting: ");
                Console.WriteLine(meeting.Topic);
                Console.WriteLine("Minimum atteendees: " + meeting.MinAttendees);
                Console.WriteLine("State: " + meeting.State);
                Console.WriteLine("Version: " + meeting.Version);
                Console.WriteLine("Coordinator: " + meeting.Coordinator);
                invitees = meeting.Invitees;

                if (invitees != null)
                {
                    Console.WriteLine("  Invitees:");

                    foreach (string invitee in invitees)
                    {
                        Console.WriteLine("  " + invitee);
                    }
                }

                slots_clients_dictionary = meeting. GetSlotsClientsMapping();

                if (slots_clients_dictionary != null)
                {
                    Console.WriteLine("  Proposed \"Locations / Date\" per client:");

                    foreach (KeyValuePair<Tuple<Location, DateTime>, List<string>> keyValuePair in slots_clients_dictionary)
                    {
                        Console.WriteLine("    Location: " + keyValuePair.Key.Item1.Name);
                        foreach (Room room in keyValuePair.Key.Item1.Rooms)
                        {
                            Console.WriteLine("      Room: " + room.Identifier);
                            Console.WriteLine("      Capacity: " + room.Capacity);
                            foreach(DateTime dateTime in room.ReservedDates)
                            {
                                Console.WriteLine("        Date: " + dateTime.ToString());
                            }
                        }
                        Console.WriteLine("    Date: " + keyValuePair.Key.Item2.ToString());
                        Console.WriteLine("    Clients: ");
                        
                        foreach(string clients in keyValuePair.Value)
                        {
                            Console.WriteLine("        " + clients);
                        }
                    }

                }

                going_clients = meeting.GoingClients;

                if (going_clients != null)
                {
                    Console.WriteLine("  Going:");

                    foreach (string going_client in going_clients)
                    {
                        Console.WriteLine("  " + going_client);
                    }
                }

                Console.WriteLine("Final Slot: " + meeting.FinalSlot);
            }
        }

        public string ServerIdentifier
        {
            get{
                return server_identifier;
            }
        }

        public string ServerIP
        {
            get
            {
                return server_ip;
            }
        }

        public string ServerRemoting
        {
            get
            {
                return server_remoting;
            }
        }

        public int ServerPort
        {
            get
            {
                return server_port;
            }
        }

        public int ToleratedFaults
        {
            get
            {
                return tolerated_faults;
            }
        }

        public int MinDelay
        {
            get
            {
                return min_delay;
            }
        }
        public int MaxDelay
        {
            get
            {
                return max_delay;
            }
        }

        public void AddMeeting(Meeting meeting)
        {
            lock(event_list_lock)
            {
                this.event_list.Add(meeting);
            }
                
        }

        public Meeting GetMeeting(string meeting_topic)
        {
            foreach (Meeting m in this.event_list)
            {
                if (m.Topic == meeting_topic)
                {
                    return m;
                }
            }
            throw new ServerCoreException(ErrorCodes.NONEXISTENT_MEETING);         
        }

        public void AddLocation(Location location)
        {
            //agora tem um check que adiciona um quarto a localizacao se ela ja existir
            Location existingLocation = null;
            List<Room> rooms;
                          
            foreach(Location l in this.known_locations)
            {
                if(l.Name.Equals(location.Name))
                {
                    existingLocation = l;           
                }
            }

            if(existingLocation!=null)
            {
                rooms = location.Rooms;

                foreach(Room room in rooms)
                {
                    existingLocation.Add(room);
                }
            }
            else
            {
                this.known_locations.Add(location);
            }
        }

        public List<Meeting> GetEventList()
        {
            return this.event_list;
        }

        public List<Location> GetKnownLocations()
        {
            return this.known_locations;
        }

        private Dictionary<string, string> GetClients()
        {
            return this.server_communication.GetClientAddresses();
        }

        public Tuple<Location, DateTime> ParseSlot(String slot)
        {
            Location location = null;
            String[] date_string = slot.Split(',');
            DateTime date = DateTime.Parse(date_string[1]);

            foreach (Location l in known_locations)
            {
                if (l.Name == date_string[0])
                {
                    location = l;
                }
            }
            if (location == null)
            {
                throw new ServerCoreException(ErrorCodes.NOT_A_LOCATION);
            }

            return new Tuple<Location, DateTime>(location, date);
        }

        public List<Tuple<Location, DateTime>> ListOfParsedSlots(List<string> slots)
        {
            List<Tuple<Location, DateTime>> parsed_slots = new List<Tuple<Location, DateTime>>();
            foreach (string s in slots)
            {
                parsed_slots.Add(ParseSlot(s));
            }
            return parsed_slots;
        }

        // problematico
        public int GetVersion(string meeting_topic)
        {   
            Meeting read_meeting;

            int version;

            try
            {
                read_meeting = this.GetMeeting(meeting_topic);
                version = read_meeting.Version;

            }
            catch (ServerCoreException sce)
            {
                // Se nao existe trata a excepcao pondo estes campos genericos
                version = 0;
            }                

            return version;
        }

        public int WriteMeeting(string meeting_topic, List<string> logs_list, ref ConcurrentDictionary<string, List<string>> logs_dictionary, ref List<Tuple<string, string>> added_create, ref List<Tuple<string, string>> added_join, ref List<Tuple<string, string>> added_close)
        {
            // TODO2: adicionar aqui tambem ao added e aos logs
            Console.WriteLine("Trying to write most recent meetings from logs list with \"" + logs_list.Count + "\" elements...");
            int min_attendees, version = -69;
            string client_identifier, operation;
            LogsParser logsParser = new LogsParser();
            List<string> slots, invitees;
            Tuple<string, int, string, int, List<string>, List<string>, string> result_tuple;                     
            
            foreach (string json_entry in logs_list)
            {
                Console.WriteLine("Parsing the following Json entry: " + json_entry);
                Console.WriteLine("Written version of meeting topic \"" + meeting_topic + "\" is: " + this.GetVersion(meeting_topic));                
                result_tuple = logsParser.ParseEntry(json_entry);
                operation = result_tuple.Item1;
                Console.WriteLine(operation);

                switch (operation)
                {

                    // ou entao aquio gajo mudar tudo desde inicio
                    case "Create":                        
                        version = result_tuple.Item2;
                        Console.WriteLine("Most recent version: " + version);
                        if (this.GetVersion(meeting_topic) < version)
                        {                            
                            min_attendees = result_tuple.Item4;
                            slots = result_tuple.Item5;
                            invitees = result_tuple.Item6;
                            client_identifier = result_tuple.Item7;

                            Tuple<string, string> create_tuple = new Tuple<string, string>(meeting_topic, client_identifier);
                            this.Create(meeting_topic, min_attendees, slots, invitees, client_identifier);
                            added_create.Add(create_tuple);
                            this.CreateLog(meeting_topic, min_attendees, slots, invitees, client_identifier, ref logs_dictionary);
                            Console.WriteLine("Wrote: " + json_entry);
                        }                            
                        break;
                    case "Join":                        
                        version = result_tuple.Item2;
                        Console.WriteLine("Most recent version: " + version);
                        if (this.GetVersion(meeting_topic) < version)
                        {
                            slots = result_tuple.Item5;
                            client_identifier = result_tuple.Item7;

                            Tuple<string, string> join_tuple = new Tuple<string, string>(meeting_topic, client_identifier);
                            this.Join(meeting_topic, slots, client_identifier, version);
                            added_join.Add(join_tuple);
                            this.JoinLog(meeting_topic, slots, client_identifier, ref logs_dictionary);
                            Console.WriteLine("Wrote:" + json_entry);
                        }                        
                        break;
                    case "Close":                        
                        version = result_tuple.Item2;
                        Console.WriteLine("Most recent version: " + version);
                        if (this.GetVersion(meeting_topic) < version)
                        {
                            client_identifier = result_tuple.Item7;

                            Tuple<string, string> close_tuple = new Tuple<string, string>(meeting_topic, client_identifier);
                            this.Close(meeting_topic, client_identifier, version);
                            added_close.Add(close_tuple);
                            this.CloseLog(meeting_topic, client_identifier, ref logs_dictionary);
                            Console.WriteLine("Wrote:" + json_entry);
                        }                        
                        break;
                }
            }

            return version;
        }

        public void CreateLog(string meeting_topic, int min_attendees, List<string> slots, List<string> invitees, string client_identifier, ref ConcurrentDictionary<string, List<string>> logs_dictionary)
        {
            int write_version = this.GetVersion(meeting_topic);
            Console.WriteLine("Create log for version: " + write_version);
            string json_log = new LogsParser().Create_ParseJSON(meeting_topic, write_version, min_attendees, slots, invitees, client_identifier);
            Console.WriteLine("The following Json log was created: " + json_log);
            List<string> logs_list = new List<string>();
            logs_list.Add(json_log);
            logs_dictionary.AddOrUpdate(meeting_topic, logs_list, (key, oldValue) => logs_list);
            Console.WriteLine("Success!");
        }
        public void JoinLog(string meeting_topic, List<string> slots, string client_identifier, ref ConcurrentDictionary<string, List<string>> logs_dictionary)
        {
            int write_version = this.GetVersion(meeting_topic);
            Console.WriteLine("Created a Json log for the following join version: " + write_version);
            string json_log = new LogsParser().Join_ParseJSON(meeting_topic, write_version, slots, client_identifier);
            Console.WriteLine("The following Json log was created: " + json_log);
            List<string> logs_list = logs_dictionary[meeting_topic];
            logs_list.Add(json_log);
            logs_dictionary.AddOrUpdate(meeting_topic, logs_list, (key, oldValue) => logs_list);
            Console.WriteLine("Success!");
        }

        public void CloseLog(string meeting_topic, string client_identifier, ref ConcurrentDictionary<string, List<string>> logs_dictionary)
        {            
            int write_version = this.GetVersion(meeting_topic);
            Console.WriteLine("Creating log for the following Close version: " + write_version);
            string json_log = new LogsParser().Close_ParseJSON(meeting_topic, write_version, client_identifier);
            Console.WriteLine("The following Json log was created: " + json_log);
            List<string> logs_list = logs_dictionary[meeting_topic];
            logs_list.Add(json_log);
            logs_dictionary.AddOrUpdate(meeting_topic, logs_list, (key, oldValue) => logs_list);
            Console.WriteLine("Success!");
        }
    }
}
