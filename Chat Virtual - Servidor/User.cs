using System.IO;
using System.Net.Sockets;
using DataStructures;
using ShippingData;

namespace Chat_Virtual___Servidor {
    public class User {
        public string Name { get; set; }
        public TcpClient Client { get; set; }
        public NetworkStream Stream { get; set; }
        public BinaryReader Reader { get; set; }
        public BinaryWriter Writer { get; set; }
        public LinkedQueue<Data> toWrite { get; set; }
        public LinkedQueue<Data> toRead { get; set; }

        public User() {
            this.Client = new TcpClient();
            toRead = new LinkedQueue<Data>();
            toWrite = new LinkedQueue<Data>();
        }
        public User(string Name) {
            this.Name = Name;
            this.Client = new TcpClient();
            toRead = new LinkedQueue<Data>();
            toWrite = new LinkedQueue<Data>();
        }

        /// <summary>
        /// Inicializa el stream el Writer y el reader en base al cliente que ya se tenga conectado
        /// </summary>
        public void SetStreams() {
            this.Stream = Client.GetStream();
            Writer = new BinaryWriter(Stream);
            Reader = new BinaryReader(Stream);
            toRead = new LinkedQueue<Data>();
            toWrite = new LinkedQueue<Data>();
        }
    }
}
