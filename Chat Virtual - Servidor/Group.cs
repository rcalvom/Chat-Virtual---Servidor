using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat_Virtual___Servidor {
    public class Group {
        public string Name { get; set; }
        public LinkedList<User> Users { get; set; }
        
    }
}
