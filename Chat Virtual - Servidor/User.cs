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
        public LinkedQueue<Data> WritingQueue { get; set; }
        public LinkedQueue<Data> ReadingQueue { get; set; }

        public User() {
            this.Client = new TcpClient();
            this.ReadingQueue = new LinkedQueue<Data>();
            this.WritingQueue = new LinkedQueue<Data>();
        }
        public User(string Name) {
            this.Name = Name;
            this.Client = new TcpClient();
            this.ReadingQueue = new LinkedQueue<Data>();
            this.WritingQueue = new LinkedQueue<Data>();
        }

        public User(TcpClient Client) {
            this.Client = Client;
            this.Stream = Client.GetStream();
            this.Writer = new BinaryWriter(this.Stream);
            this.Reader = new BinaryReader(this.Stream);
            this.ReadingQueue = new LinkedQueue<Data>();
            this.WritingQueue = new LinkedQueue<Data>();
        }
    }
}
