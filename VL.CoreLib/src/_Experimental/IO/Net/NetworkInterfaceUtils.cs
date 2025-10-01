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
            anycastAddresses = OperatingSystem.IsWindows() ? iPInterfaceProperties.AnycastAddresses.ToSpread() : Spread<IPAddressInformation>.Empty;
            dhcpServerAddresses = !OperatingSystem.IsMacOS() ? iPInterfaceProperties.DhcpServerAddresses.ToSpread() : Spread<IPAddress>.Empty;
            dnsAddresses = iPInterfaceProperties.DnsAddresses.ToSpread();
            dnsSuffix = iPInterfaceProperties.DnsSuffix;
            gatewayAddresses = iPInterfaceProperties.GatewayAddresses.Select(g => g.Address).ToSpread();
            isDnsEnabled = !OperatingSystem.IsMacOS() ? iPInterfaceProperties.IsDnsEnabled : false;
            isDynamicDnsEnabled = OperatingSystem.IsWindows() ? iPInterfaceProperties.IsDynamicDnsEnabled : false;
            multicastAddresses = iPInterfaceProperties.MulticastAddresses.ToSpread();
            unicastAddresses = iPInterfaceProperties.UnicastAddresses.ToSpread();
            winsServersAddresses = !OperatingSystem.IsMacOS() ? iPInterfaceProperties.WinsServersAddresses.ToSpread() : Spread<IPAddress>.Empty;
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
            usesWins = OperatingSystem.IsWindows() || OperatingSystem.IsLinux() ? iPv4InterfaceProperties.UsesWins : false;
            isDhcpEnabled = OperatingSystem.IsWindows() ? iPv4InterfaceProperties.IsDhcpEnabled : false;
            isAutomaticPrivateAddressingActive = OperatingSystem.IsWindows() ? iPv4InterfaceProperties.IsAutomaticPrivateAddressingActive : false;
            isAutomaticPrivateAddressingEnabled = OperatingSystem.IsWindows() ? iPv4InterfaceProperties.IsAutomaticPrivateAddressingEnabled : false;
            isForwardingEnabled = OperatingSystem.IsWindows() || OperatingSystem.IsLinux() ? iPv4InterfaceProperties.IsForwardingEnabled : false;
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
            isDnsEligible = OperatingSystem.IsWindows() ? iPAddressInformation.IsDnsEligible : false;
            isTransient = OperatingSystem.IsWindows() ? iPAddressInformation.IsTransient : false;
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
            isDnsEligible = OperatingSystem.IsWindows() ? multicastIPAddressInformation.IsDnsEligible : default;
            isTransient = OperatingSystem.IsWindows() ? multicastIPAddressInformation.IsTransient : default;
            addressPreferredLifetime = OperatingSystem.IsWindows() ? multicastIPAddressInformation.AddressPreferredLifetime : default;
            addressValidLifetime = OperatingSystem.IsWindows() ? multicastIPAddressInformation.AddressValidLifetime : default;
            dhcpLeaseLifetime = OperatingSystem.IsWindows() ? multicastIPAddressInformation.DhcpLeaseLifetime : default;
            duplicateAddressDetectionState = OperatingSystem.IsWindows() ? multicastIPAddressInformation.DuplicateAddressDetectionState : default;
            prefixOrigin = OperatingSystem.IsWindows() ? multicastIPAddressInformation.PrefixOrigin : default;
            suffixOrigin = OperatingSystem.IsWindows() ? multicastIPAddressInformation.SuffixOrigin : default;
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
            addressPreferredLifetime = OperatingSystem.IsWindows() ? unicastIPAddressInformation.AddressPreferredLifetime : default;
            addressValidLifetime = OperatingSystem.IsWindows() ? unicastIPAddressInformation.AddressValidLifetime : default;
            dhcpLeaseLifetime = OperatingSystem.IsWindows() ? unicastIPAddressInformation.DhcpLeaseLifetime : default;
            duplicateAddressDetectionState = OperatingSystem.IsWindows() ? unicastIPAddressInformation.DuplicateAddressDetectionState : default;
            prefixOrigin = OperatingSystem.IsWindows() ? unicastIPAddressInformation.PrefixOrigin : default;
            suffixOrigin = OperatingSystem.IsWindows() ? unicastIPAddressInformation.SuffixOrigin : default;
            iPv4Mask = OperatingSystem.IsWindows() ? unicastIPAddressInformation.IPv4Mask : default;
            prefixLength = OperatingSystem.IsWindows() ? unicastIPAddressInformation.PrefixLength : default;
            address = OperatingSystem.IsWindows() ? unicastIPAddressInformation.Address : default;
            isDnsEligible = OperatingSystem.IsWindows() ? unicastIPAddressInformation.IsDnsEligible : default;
            isTransient = OperatingSystem.IsWindows() ? unicastIPAddressInformation.IsTransient : default;
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
            incomingUnknownProtocolPackets = !OperatingSystem.IsLinux() ? iPInterfaceStatistics.IncomingUnknownProtocolPackets : default;
            nonUnicastPacketsReceived = iPInterfaceStatistics.NonUnicastPacketsReceived;
            nonUnicastPacketsSent = !OperatingSystem.IsLinux() ? iPInterfaceStatistics.NonUnicastPacketsSent : default;
            outgoingPacketsDiscarded = !OperatingSystem.IsMacOS() ? iPInterfaceStatistics.OutgoingPacketsDiscarded : default;
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
            outgoingPacketsDiscarded = !OperatingSystem.IsMacOS() ? iPv4InterfaceStatistics.OutgoingPacketsDiscarded : default;
            outgoingPacketsWithErrors = iPv4InterfaceStatistics.OutgoingPacketsWithErrors;
            outputQueueLength = iPv4InterfaceStatistics.OutputQueueLength;
            unicastPacketsReceived = iPv4InterfaceStatistics.UnicastPacketsReceived;
            unicastPacketsSent = iPv4InterfaceStatistics.UnicastPacketsSent;
        }
    }
}
