using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NIMEngine.Plugin
{
    public interface IPlugin
    {
        string Name { get; set; }
        string Author { get; set; }
        string Description { get; set; }

        void Init(string MethodName, object[] ParamsRef);

        void HandleEvent(string MethodName, object[] ParamsRef);
    }
}
