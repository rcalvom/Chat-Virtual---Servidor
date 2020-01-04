using System.IO;
using System.Net.Sockets;
using DataStructures;
using ShippingData;
using System.Threading;
using System;

namespace Chat_Virtual___Servidor {
    public class User {
        public string Name { get; set; }
        public TcpClient Client { get; set; }
        public NetworkStream Stream { get; set; }
        public BinaryReader Reader { get; set; }
        public BinaryWriter Writer { get; set; }
        private readonly LinkedQueue<Data> WritingQueue;
        private readonly LinkedQueue<Data> ReadingQueue;
        private readonly Semaphore CanWrite;
        private readonly Semaphore CanRead;

        public User() {
            this.Client = new TcpClient();
            this.ReadingQueue = new LinkedQueue<Data>();
            this.WritingQueue = new LinkedQueue<Data>();
            this.CanWrite = new Semaphore(1, 1);
            this.CanRead = new Semaphore(1, 1);
        }
        public User(string Name) {
            this.Name = Name;
            this.Client = new TcpClient();
            this.ReadingQueue = new LinkedQueue<Data>();
            this.WritingQueue = new LinkedQueue<Data>();
            this.CanWrite = new Semaphore(1, 1);
            this.CanRead = new Semaphore(1, 1);
        }

        public User(TcpClient Client) {
            this.Client = Client;
            this.Stream = Client.GetStream();
            this.Writer = new BinaryWriter(this.Stream);
            this.Reader = new BinaryReader(this.Stream);
            this.ReadingQueue = new LinkedQueue<Data>();
            this.WritingQueue = new LinkedQueue<Data>();
            this.CanWrite = new Semaphore(1, 1);
            this.CanRead = new Semaphore(1, 1);
        }

        public void WritingEnqueue(Data data) {
            this.CanWrite.WaitOne();
            this.WritingQueue.Enqueue(data);
            this.CanWrite.Release();
        }

        public void ReadingEnqueue(Data data) {
            this.CanRead.WaitOne();
            this.ReadingQueue.Enqueue(data);
            this.CanRead.Release();
        }

        public Data WritingDequeue() {
            this.CanWrite.WaitOne();
            Data data = this.WritingQueue.Dequeue();
            this.CanWrite.Release();
            return data;
        }

        public Data ReadingDequeue() {
            this.CanRead.WaitOne();
            Data data = this.ReadingQueue.Dequeue();
            this.CanRead.Release();
            return data;
        }
    }
}
