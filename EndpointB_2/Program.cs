﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EndpointB;

namespace EndpointB_2
{
    class Program
    {
        static void Main(string[] args)
        {
            Configuration.Start("2").GetAwaiter().GetResult();
        }
    }
}
