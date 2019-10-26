using Chat_Virtual___Servidor.PetitionTypes;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;   

namespace Chat_Virtual___Servidor {
    public static class Serializer {
        public static byte[] Serialize(object o) {
            using (var memoryStream = new MemoryStream()) {
                (new BinaryFormatter()).Serialize(memoryStream, o);
                return memoryStream.ToArray();
            }
        }

        public static object Deserialize(byte[] data) {
            using (var memoryStream = new MemoryStream(data)) {
                return (new BinaryFormatter()).Deserialize(memoryStream);
            }
        }
    }
}
