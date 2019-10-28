using System;

namespace Chat_Virtual___Servidor.PetitionTypes {

    [Serializable]
    public class SignUp : Petition {
        public string Name { get; set; }
        public string Password { get; set; }
    }
}
