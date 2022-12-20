using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TPL
{
    public class Dz6 : IPScanner
    {
        public async Task Scan(IPAddress[] ipAddrs, int[] ports)
        {
            var tasks = ipAddrs.Select(async ipAddr => await ProcessIpAddr(ipAddr, ports));
            await Task.WhenAll(tasks);
        }

        private static async Task ProcessIpAddr(IPAddress ipAddr, int[] ports)
        {
            var ping = await PingAddrAsync(ipAddr);
            if (ping.Status != IPStatus.Success)
                return;
            await Task.WhenAll(ports.Select(port => CheckAsync(ipAddr, port)));
        }

        private static async Task<PingReply> PingAddrAsync(IPAddress ipAddr, int timeout = 3000)
        {
            Console.WriteLine($"Pinging {ipAddr}");
            var ping = new Ping();
            var sendAsync = await ping.SendPingAsync(ipAddr, timeout);
            Console.WriteLine($"Pinged {ipAddr}: {sendAsync.Status}");
            return sendAsync;
        }

        private static async Task<PortStatus> CheckAsync(IPAddress ip, int port, int timeout = 3000)
        {
            var client = new TcpClient();
            Console.WriteLine($"Checking {ip}:{port}");
            var status = await client.ConnectAsync(ip, port, timeout);
            Console.WriteLine($"Checked {ip}:{port} - {status}");
            return status;
        }
    }
}