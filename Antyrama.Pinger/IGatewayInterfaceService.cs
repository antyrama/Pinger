using System.Collections.Generic;

namespace Antyrama.Pinger
{
    public interface IGatewayInterfaceService
    {
        int CheckInterface();
        IEnumerable<Interface> GetInterfaces();
    }
}