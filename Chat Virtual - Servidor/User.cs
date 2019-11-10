using System.IO;
using System.Net.Sockets;
using DataStructures;
using ShippingData;
using System.Threading;

namespace Chat_Virtual___Servidor {
    public class User {
        public string Name { get; set; }
        public TcpClient Client { get; set; }
        public NetworkStream Stream { get; set; }
        public BinaryReader Reader { get; set; }
        public BinaryWriter Writer { get; set; }
        protected LinkedQueue<Data> WritingQueue;
        protected LinkedQueue<Data> ReadingQueue;
        protected Semaphore CanWrite;
        protected Semaphore CanRead;

        public User() {
            this.Client = new TcpClient();
            this.ReadingQueue = new LinkedQueue<Data>();
            this.WritingQueue = new LinkedQueue<Data>();
            CanWrite = new Semaphore(1, 1);
            CanRead = new Semaphore(1, 1);
        }
        public User(string Name) {
            this.Name = Name;
            this.Client = new TcpClient();
            this.ReadingQueue = new LinkedQueue<Data>();
            this.WritingQueue = new LinkedQueue<Data>();
            CanWrite = new Semaphore(1, 1);
            CanRead = new Semaphore(1, 1);
        }

        public User(TcpClient Client) {
            this.Client = Client;
            this.Stream = Client.GetStream();
            this.Writer = new BinaryWriter(this.Stream);
            this.Reader = new BinaryReader(this.Stream);
            this.ReadingQueue = new LinkedQueue<Data>();
            this.WritingQueue = new LinkedQueue<Data>();
            CanWrite = new Semaphore(1, 1);
            CanRead = new Semaphore(1, 1);
        }

        public void WritingEnqueue(Data data) {
            CanWrite.WaitOne();
            WritingQueue.Enqueue(data);
            CanWrite.Release();
        }

        public void ReadingEnqueue(Data data) {
            CanRead.WaitOne();
            ReadingQueue.Enqueue(data);
            CanRead.Release();
        }

        public Data WritingDequeue() {
            CanWrite.WaitOne();
            Data data;
            if (WritingQueue.IsEmpty()) {
                data = default;
            } else {
                data = WritingQueue.Dequeue();
            }
            CanWrite.Release();
            return data;
        }

        public Data ReadingDequeue() {
            CanRead.WaitOne();
            Data data;
            if (ReadingQueue.IsEmpty()) {
                data = default;
            } else {
                data = ReadingQueue.Dequeue();
            }
            CanRead.Release();
            return data;
        }
    }
}
