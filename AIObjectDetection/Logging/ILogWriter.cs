using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AICore.Logging
{
    interface ILogWriter
    {
        Task Log(string msg);
    }
}
