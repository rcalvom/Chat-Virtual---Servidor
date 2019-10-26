using Chat_Virtual___Servidor.PetitionTypes;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Chat_Virtual___Servidor {
    public static class Serializer {
        public static Petition Serialize(object o) {
            using (var memoryStream = new MemoryStream()) {
                (new BinaryFormatter()).Serialize(memoryStream, o);
                return new Petition { Data = memoryStream.ToArray() };
            }
        }

        public static object Deserialize(Petition petition) {
            using (var memoryStream = new MemoryStream(petition.Data))
                return (new BinaryFormatter()).Deserialize(memoryStream);
        }
    }
}
