﻿using MSDAD.Client.Comunication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDAD
{
    namespace Client
    {
        namespace Commands {
            
            abstract class Command
            {
                public abstract object Execute(ClientSendComm comm, int port_int);
            }
        }        
    }    
}