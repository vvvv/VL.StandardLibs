using System;

namespace VL.IO.Redis
{
    public interface IConfig
    {
        IConfig IP(string IpAdress = "127.0.0.1");
        IConfig Port(int portNumber = 6379);
    }

    public class Config : IConfig
    {
        internal string ip;
        internal int port;

        public Config()
        {
            ip = "127.0.0.1";
            port = 6379;
        }

        public IConfig IP(string IpAdress)
        {
            ip = IpAdress;
            return this;
        }

        public IConfig Port(int portNumber)
        {
            port = portNumber;
            return this;
        }
    }
}