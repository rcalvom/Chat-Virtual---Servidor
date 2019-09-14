using System;
using System.Windows.Forms;

namespace Chat_Virtual___Servidor {
    public class DataBaseConnection {

        private readonly RichTextBox Console;
        private OracleDataBase Oracle;
        private const string Ip = "localhost";
        private const string Port = "1521";
        private const string Service = "orcl";
        private const string User = "Aplicacion";
        private const string Password = "123456";
        

        public DataBaseConnection(RichTextBox Console) {
            this.Console = Console;
        }

        public void ConnectDataBase() {
            try {
                this.Oracle = new OracleDataBase(Ip, Port, Service, User, Password);
                this.Console.AppendText("Se ha conectado correctamente a la base de datos Oracle, versión: "+ this.Oracle.getConnection().ServerVersion+"\n");
            } catch(Exception ex){
                this.Console.AppendText("No se ha podido conectar a la base de datos:\n " + ex + "\n");
            }
            
        }

        private void DisconnectDataBase() {
            
        }

        public OracleDataBase GetOracleDataBase() {
            return this.Oracle;
        }

    }
}
