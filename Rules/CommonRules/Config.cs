using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules.Rules
{
    static class Config
    {
        static Config()
        {
            _VirtualSiteId = new Dictionary<int, int>();
            for (int siteId = 1; siteId <= 1024; siteId++)
            {
                int virtSiteId = Configuration.ReadIntParameter("VirtualSiteId_" + siteId, siteId);
                _VirtualSiteId[siteId] = virtSiteId;
            }            
        }
        static public readonly Dictionary<int, int> _VirtualSiteId;
    }
}
