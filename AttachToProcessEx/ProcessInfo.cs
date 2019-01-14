using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AttachToProcessEx
{
    /// <summary>
    /// This is the process information we will show in listview
    /// </summary>
    class ProcessInfo
    {
        private readonly string name;
        private readonly string pid;
        private readonly string commandline;

        public string Name
        {
            get
            {
                return name;
            }
        }

        public string Pid
        {
            get
            {
                return pid;
            }
        }

        public string CommandLine
        {
            get
            {
                return commandline;
            }
        }

        public ProcessInfo(string name, string pid,string commandline)
        {
            this.name = name;
            this.pid = pid;
            this.commandline = commandline;
        }
    }
}
