using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;

namespace AttachToProcessEx
{
    /// <summary>
    /// This is the Model we will bind
    /// </summary>
    class ProcessInfoModel
    {
        public List<ProcessInfo> Processinfolist { get; }

        public ProcessInfoModel()
        {
            Processinfolist = new List<ProcessInfo>();
            UpdateProcessInfoList();
        }

        public void CleanProcessInfoList()
        {
            if(Processinfolist.Count() <= 0)
            {
                Processinfolist.Clear();
            }
        }

        public void UpdateProcessInfoList()
        {
            CleanProcessInfoList();
            // Need Administrator Access
            ManagementObjectSearcher search = new ManagementObjectSearcher("select * from Win32_Process");
            foreach(ManagementObject queryObj in search.Get())
            {
                PropertyDataCollection property = queryObj.Properties;
                var name = Convert.ToString(property["Name"].Value);
                var pid = Convert.ToString(property["ProcessID"].Value);
                var commandline = Convert.ToString(property["CommandLine"].Value);
                Processinfolist.Add(new ProcessInfo(name, pid, commandline));
            }
        }
    }

    /// <summary>
    /// This is the process information we will show in listview single item
    /// </summary>
    class ProcessInfo
    {
        public string Name { get; }
        public string Pid { get; }
        public string CommandLine { get; }

        public ProcessInfo(string name, string pid,string commandline)
        {
            this.Name = name;
            this.Pid = pid;
            this.CommandLine = commandline;
        }
    }
}
