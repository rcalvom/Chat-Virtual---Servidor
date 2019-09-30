using System.IO;
using System.Net.Sockets;

namespace Chat_Virtual___Servidor {
    public class User {
        private NetworkStream Stream;
        private StreamWriter Writer;
        private StreamReader Reader;
        private string Name;

        public void SetStream(NetworkStream Stream) {
            this.Stream = Stream;
        }

        public NetworkStream GetStream() {
            return this.Stream;
        }

        public void SetWriter(StreamWriter Writer) {
            this.Writer = Writer;
        }

        public StreamWriter GetWriter() {
            return this.Writer;
        }

        public void SetReader(StreamReader Reader) {
            this.Reader = Reader;
        }

        public StreamReader GetReader() {
            return this.Reader;
        }

        public void SetName(string Name) {
            this.Name = Name;
        }

        public string GetName() {
            return this.Name;
        }

        public User(NetworkStream Stream, StreamWriter Writer, StreamReader Reader) {
            this.Stream = Stream;
            this.Writer = Writer;
            this.Reader = Reader;
        }

        public User() {

        }

    }
}
