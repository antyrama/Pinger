using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using SnmpSharpNet;

namespace Antyrama.Pinger
{
    public class GatewayInterfaceService : IGatewayInterfaceService
    {
        private readonly Options _options;
        private readonly UdpTarget _target;
        private readonly AgentParameters _params = new AgentParameters(new OctetString("public"));
        private readonly Oid _interfaceDescriptionOid;

        public GatewayInterfaceService(Options options)
        {
            _options = options;
            _params.Version = SnmpVersion.Ver2;
            var agent = new IpAddress(options.IpAddress);
            _target = new UdpTarget((IPAddress)agent, 161, options.Interval, 1);
            _interfaceDescriptionOid = new Oid(options.DefaultOid);
        }

        public int CheckInterface()
        {
            var @interface = GetInterfaces().FirstOrDefault(i => i.Name == _options.InterfaceName);

            if (@interface == null)
            {
                return 2;
            }

            var array = @interface.Oid.ToArray();
            array[9] = 8;

            var oid = new Oid(array);

            var pdu = new Pdu(PduType.Get);
            pdu.VbList.Add(oid);

            pdu.RequestId += 1;

            try
            {
                var result = (SnmpV2Packet)_target.Request(pdu, _params);

                if (result.Pdu.ErrorStatus != 0)
                {
                    throw new InvalidOperationException(
                        $"Error in SNMP reply when calling for [{oid}]. Error [{result.Pdu.ErrorStatus}] index [{result.Pdu.ErrorIndex}]");
                }

                var status = result.Pdu.VbList.FirstOrDefault();
                if (status == null)
                {
                    throw new InvalidOperationException(
                        $"Gateway does not return readable response for interface [{_options.InterfaceName}] with OID: [{oid}].");
                }

                if (status.Value.Type == SnmpConstants.SMI_INTEGER)
                {
                    return ((Integer32)status.Value).Value;
                }

                throw new InvalidOperationException(
                    $"Gateway does not return readable response for interface [{_options.InterfaceName}] with OID: [{oid}].");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to determine whether interface [{_options.InterfaceName}] is operational, OID: [{oid}].",
                    ex);
            }
        }

        public IEnumerable<Interface> GetInterfaces()
        {
            var lastOid = (Oid)_interfaceDescriptionOid.Clone();

            var pdu = new Pdu(PduType.GetBulk);
            pdu.NonRepeaters = 0;
            pdu.MaxRepetitions = 100;

            while (lastOid != null)
            {
                if (pdu.RequestId != 0)
                {
                    pdu.RequestId += 1;
                }

                pdu.VbList.Clear();
                pdu.VbList.Add(lastOid);

                SnmpV2Packet result;
                try
                {
                    result = (SnmpV2Packet)_target.Request(pdu, _params);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Request for SNMP agent failed for [{_options.DefaultOid}].", ex);
                }

                if (result != null)
                {
                    if (result.Pdu.ErrorStatus != 0)
                    {
                        throw new InvalidOperationException(
                            $"Error in SNMP reply. Error [{result.Pdu.ErrorStatus}] index [{result.Pdu.ErrorIndex}]");
                    }

                    foreach (var v in result.Pdu.VbList)
                    {
                        if (_interfaceDescriptionOid.IsRootOf(v.Oid))
                        {
                            yield return new Interface { Oid = v.Oid, Name = v.Value.ToString() };

                            lastOid = v.Value.Type == SnmpConstants.SMI_ENDOFMIBVIEW ? null : v.Oid;
                        }
                        else
                        {
                            lastOid = null;
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException("No response received from SNMP agent.");
                }
            }
        }
    }
    
    public class Interface
    {
        public Oid Oid { get; set; }
        public string Name { get; set; }
    }
}