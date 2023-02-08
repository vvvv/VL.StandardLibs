using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Linq;
using VL.Core;
using VL.Lib.Collections;

namespace VL.Lib.IO.Net
{
    public static class NetworkInterfaceUtils
    {
        public static void Supports(this NetworkInterface networkInterface, out bool supportsIPv4, out bool supportsIPv6)
        {
            supportsIPv4 = networkInterface.Supports(NetworkInterfaceComponent.IPv4);
            supportsIPv6 = networkInterface.Supports(NetworkInterfaceComponent.IPv6);
        }

        public static void Split(this NetworkInterface networkInterface,
            out string name,
            out string description,
            out string id,
            out bool isReceiveOnly,
            out NetworkInterfaceType networkInterfaceType,
            out OperationalStatus operationalStatus,
            out long speed,
            out bool supportsMulticast
            )
        {
            description = networkInterface.Description;
            id = networkInterface.Id;
            isReceiveOnly = networkInterface.IsReceiveOnly;
            name = networkInterface.Name;
            networkInterfaceType = networkInterface.NetworkInterfaceType;
            operationalStatus = networkInterface.OperationalStatus;
            speed = networkInterface.Speed;
            supportsMulticast = networkInterface.SupportsMulticast;
        }

        public static void Split(this IPInterfaceProperties iPInterfaceProperties,
            out Spread<IPAddressInformation> anycastAddresses,
            out Spread<IPAddress> dhcpServerAddresses,
            out Spread<IPAddress> dnsAddresses,
            out string dnsSuffix,
            out Spread<IPAddress> gatewayAddresses,
            out bool isDnsEnabled,
            out bool isDynamicDnsEnabled,
            out Spread<MulticastIPAddressInformation> multicastAddresses,
            out Spread<UnicastIPAddressInformation> unicastAddresses,
            out Spread<IPAddress> winsServersAddresses
            )
        {
            anycastAddresses = iPInterfaceProperties.AnycastAddresses.ToSpread();
            dhcpServerAddresses = iPInterfaceProperties.DhcpServerAddresses.ToSpread();
            dnsAddresses = iPInterfaceProperties.DnsAddresses.ToSpread();
            dnsSuffix = iPInterfaceProperties.DnsSuffix;
            gatewayAddresses = iPInterfaceProperties.GatewayAddresses.Select(g => g.Address).ToSpread();
            isDnsEnabled = iPInterfaceProperties.IsDnsEnabled;
            isDynamicDnsEnabled = iPInterfaceProperties.IsDynamicDnsEnabled;
            multicastAddresses = iPInterfaceProperties.MulticastAddresses.ToSpread();
            unicastAddresses = iPInterfaceProperties.UnicastAddresses.ToSpread();
            winsServersAddresses = iPInterfaceProperties.WinsServersAddresses.ToSpread();
        }

        public static void Split(this IPv4InterfaceProperties iPv4InterfaceProperties,
            out int index,
            out int mtu,
            out bool usesWins,
            out bool isDhcpEnabled,
            out bool isAutomaticPrivateAddressingActive,
            out bool isAutomaticPrivateAddressingEnabled,
            out bool isForwardingEnabled)
        {
            index = iPv4InterfaceProperties.Index;
            mtu = iPv4InterfaceProperties.Mtu;
            usesWins = iPv4InterfaceProperties.UsesWins;
            isDhcpEnabled = iPv4InterfaceProperties.IsDhcpEnabled;
            isAutomaticPrivateAddressingActive = iPv4InterfaceProperties.IsAutomaticPrivateAddressingActive;
            isAutomaticPrivateAddressingEnabled = iPv4InterfaceProperties.IsAutomaticPrivateAddressingEnabled;
            isForwardingEnabled = iPv4InterfaceProperties.IsForwardingEnabled;
        }

        public static void Split(this IPv6InterfaceProperties iPv6InterfaceProperties,
            out int index,
            out int mtu)
        {
            index = iPv6InterfaceProperties.Index;
            mtu = iPv6InterfaceProperties.Mtu;
        }

        public static void Split(this IPAddressInformation iPAddressInformation,
            out IPAddress iPAddress,
            out bool isDnsEligible,
            out bool isTransient)
        {
            iPAddress = iPAddressInformation.Address;
            isDnsEligible = iPAddressInformation.IsDnsEligible;
            isTransient = iPAddressInformation.IsTransient;
        }

        public static void Split(this MulticastIPAddressInformation multicastIPAddressInformation,
            out IPAddress iPAddress,
            out bool isDnsEligible,
            out bool isTransient,
            out long addressPreferredLifetime,
            out long addressValidLifetime,
            out long dhcpLeaseLifetime,
            out DuplicateAddressDetectionState duplicateAddressDetectionState,
            out PrefixOrigin prefixOrigin,
            out SuffixOrigin suffixOrigin
            )
        {
            iPAddress = multicastIPAddressInformation.Address;
            isDnsEligible = multicastIPAddressInformation.IsDnsEligible;
            isTransient = multicastIPAddressInformation.IsTransient;
            addressPreferredLifetime = multicastIPAddressInformation.AddressPreferredLifetime;
            addressValidLifetime = multicastIPAddressInformation.AddressValidLifetime;
            dhcpLeaseLifetime = multicastIPAddressInformation.DhcpLeaseLifetime;
            duplicateAddressDetectionState = multicastIPAddressInformation.DuplicateAddressDetectionState;
            prefixOrigin = multicastIPAddressInformation.PrefixOrigin;
            suffixOrigin = multicastIPAddressInformation.SuffixOrigin;
        }

        public static void Split(this UnicastIPAddressInformation unicastIPAddressInformation,
            out long addressPreferredLifetime,
            out long addressValidLifetime,
            out long dhcpLeaseLifetime,
            out DuplicateAddressDetectionState duplicateAddressDetectionState,
            out PrefixOrigin prefixOrigin,
            out SuffixOrigin suffixOrigin,
            out IPAddress iPv4Mask,
            out int prefixLength,
            out IPAddress address,
            out bool isDnsEligible,
            out bool isTransient)
        {
            addressPreferredLifetime = unicastIPAddressInformation.AddressPreferredLifetime;
            addressValidLifetime = unicastIPAddressInformation.AddressValidLifetime;
            dhcpLeaseLifetime = unicastIPAddressInformation.DhcpLeaseLifetime;
            duplicateAddressDetectionState = unicastIPAddressInformation.DuplicateAddressDetectionState;
            prefixOrigin = unicastIPAddressInformation.PrefixOrigin;
            suffixOrigin = unicastIPAddressInformation.SuffixOrigin;
            iPv4Mask = unicastIPAddressInformation.IPv4Mask;
            prefixLength = unicastIPAddressInformation.PrefixLength;
            address = unicastIPAddressInformation.Address;
            isDnsEligible = unicastIPAddressInformation.IsDnsEligible;
            isTransient = unicastIPAddressInformation.IsTransient;
        }

        public static void Split(this IPInterfaceStatistics iPInterfaceStatistics,
            out long bytesReceived,
            out long bytesSent,
            out long incomingPacketsDiscarded,
            out long incomingPacketsWithErrors,
            out long incomingUnknownProtocolPackets,
            out long nonUnicastPacketsReceived,
            out long nonUnicastPacketsSent,
            out long outgoingPacketsDiscarded,
            out long outgoingPacketsWithErrors,
            out long outputQueueLength,
            out long unicastPacketsReceived,
            out long unicastPacketsSent)
        {
            bytesReceived = iPInterfaceStatistics.BytesReceived;
            bytesSent = iPInterfaceStatistics.BytesSent;
            incomingPacketsDiscarded = iPInterfaceStatistics.IncomingPacketsDiscarded;
            incomingPacketsWithErrors = iPInterfaceStatistics.IncomingPacketsWithErrors;
            incomingUnknownProtocolPackets = iPInterfaceStatistics.IncomingUnknownProtocolPackets;
            nonUnicastPacketsReceived = iPInterfaceStatistics.NonUnicastPacketsReceived;
            nonUnicastPacketsSent = iPInterfaceStatistics.NonUnicastPacketsSent;
            outgoingPacketsDiscarded = iPInterfaceStatistics.OutgoingPacketsDiscarded;
            outgoingPacketsWithErrors = iPInterfaceStatistics.OutgoingPacketsWithErrors;
            outputQueueLength = iPInterfaceStatistics.OutputQueueLength;
            unicastPacketsReceived = iPInterfaceStatistics.UnicastPacketsReceived;
            unicastPacketsSent = iPInterfaceStatistics.UnicastPacketsSent;
        }

        public static void Split(this IPv4InterfaceStatistics iPv4InterfaceStatistics,
            out long bytesReceived,
            out long bytesSent,
            out long incomingPacketsDiscarded,
            out long incomingPacketsWithErrors,
            out long incomingUnknownProtocolPackets,
            out long nonUnicastPacketsReceived,
            out long nonUnicastPacketsSent,
            out long outgoingPacketsDiscarded,
            out long outgoingPacketsWithErrors,
            out long outputQueueLength,
            out long unicastPacketsReceived,
            out long unicastPacketsSent)
        {
            bytesReceived = iPv4InterfaceStatistics.BytesReceived;
            bytesSent = iPv4InterfaceStatistics.BytesSent;
            incomingPacketsDiscarded = iPv4InterfaceStatistics.IncomingPacketsDiscarded;
            incomingPacketsWithErrors = iPv4InterfaceStatistics.IncomingPacketsWithErrors;
            incomingUnknownProtocolPackets = iPv4InterfaceStatistics.IncomingUnknownProtocolPackets;
            nonUnicastPacketsReceived = iPv4InterfaceStatistics.NonUnicastPacketsReceived;
            nonUnicastPacketsSent = iPv4InterfaceStatistics.NonUnicastPacketsSent;
            outgoingPacketsDiscarded = iPv4InterfaceStatistics.OutgoingPacketsDiscarded;
            outgoingPacketsWithErrors = iPv4InterfaceStatistics.OutgoingPacketsWithErrors;
            outputQueueLength = iPv4InterfaceStatistics.OutputQueueLength;
            unicastPacketsReceived = iPv4InterfaceStatistics.UnicastPacketsReceived;
            unicastPacketsSent = iPv4InterfaceStatistics.UnicastPacketsSent;
        }
    }
}
