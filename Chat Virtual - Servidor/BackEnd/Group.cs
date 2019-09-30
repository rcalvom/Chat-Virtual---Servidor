using DataStructures;

namespace Chat_Virtual___Servidor {
    public class Group {
        public string Name { get; set; }
        public Chain<User> Users { get; set; }
        
    }
}
