using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NIMEngine.Plugin
{
    public interface I_Plugin
    {
        string Name { get; set; }
        string Author { get; set; }
        string Description { get; set; }

        bool Enabled { get; set; }

        void Init(string MethodName,ref object[] ParamsRef);

        void HandleEvent(string MethodName,ref object[] ParamsRef);
    }
}
