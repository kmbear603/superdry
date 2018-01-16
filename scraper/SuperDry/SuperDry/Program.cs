using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperDry
{
    class Program
    {
        static void Main(string[] args)
        {
            new Engine().Run(status => Console.WriteLine(status));
        }
    }
}
