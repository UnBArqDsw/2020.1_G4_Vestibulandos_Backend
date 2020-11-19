using System;
using System.Net;

public static class NetworkExtensions
{
    public static IPAddress GetNetworkAddress(this IPAddress address, IPAddress subnetMask)
    {
        byte[] ipAdressBytes = address.GetAddressBytes();
        byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

        if (ipAdressBytes.Length != subnetMaskBytes.Length)
            throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

        byte[] broadcastAddress = new byte[ipAdressBytes.Length];
        for (int i = 0; i < broadcastAddress.Length; i++)
        {
            broadcastAddress[i] = (byte)(ipAdressBytes[i] & (subnetMaskBytes[i]));
        }
        return new IPAddress(broadcastAddress);
    }

    public static long GetIpNum(string sIp)
    {
        string[] sArrIp = sIp.Split('.');

        long val1 = 16777216;
        long val2 = 65536;
        long val3 = 256;

        return (Convert.ToInt64(sArrIp[0]) * val1) + (Convert.ToInt64(sArrIp[1]) * val2) + (Convert.ToInt64(sArrIp[2]) * val3) + Convert.ToInt64(sArrIp[3]);
    }
}