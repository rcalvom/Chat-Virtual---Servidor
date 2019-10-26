using System.IO;
using System.Net.Sockets;

namespace Chat_Virtual___Servidor {
    public class User {
        public string Name { get; set; }
        public NetworkStream Stream { get; set; }
        public BinaryReader Reader { get; set; }
        public BinaryWriter Writer { get; set; }

        public User(string Name, NetworkStream Stream, BinaryWriter Writer, BinaryReader Reader) {
            this.Name = Name;
            this.Stream = Stream;
            this.Writer = Writer;
            this.Reader = Reader;
        }

        public User(string Name, NetworkStream Stream) {
            this.Name = Name;
            this.Stream = Stream;
            this.Writer = new BinaryWriter(Stream);
            this.Reader = new BinaryReader(Stream);
        }

        public User(NetworkStream Stream) {
            this.Stream = Stream;
            this.Writer = new BinaryWriter(Stream);
            this.Reader = new BinaryReader(Stream);
        }

    }
}
