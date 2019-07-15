using PcapDotNet.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leho.Functions
{
    public interface IFunction
    {
        void Execute(PacketCommunicator packetCommunicator);
    }
}
