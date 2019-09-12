using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chat_Virtual___Servidor {
    public partial class GraphicInterface : Form {

        private bool Connected;
        private OracleDataBase Oracle;
        private const string Ip = "localhost";
        private const string Port = "1521";
        private const string Server = "orcl";
        private const string User = "Aplicacion";
        private const string Password = "123456";

        public GraphicInterface(){
            this.Connected = false;
            InitializeComponent();
            this.Console.AppendText("Hola, Bienvenido a Chat server\n"); // Nombre de la app pendiente.
        }

        private void ExitEvent(object sender, EventArgs e){
            Application.Exit();
        }

        private void InfoEvent(object sender, EventArgs e){
            MessageBox.Show("Aplicación creada por\n\n" +
                "Ricardo Andrés Calvo\n" +
                "Samuel González Nisperuza\n" +
                "Juan Diego Preciado\n");
            //TODO: Mejorar Cuadro Emergente
        }

        private void ButtonEvent(object sender, EventArgs e){
            if (Connected){
                ShutDown();
            }else{
                ShutUp();
            }
        }

        private void ShutUp(){
            try{
                this.Oracle = new OracleDataBase(Ip, Port, Server, User, Password);
                this.Console.AppendText("Se ha conectado correctamente a la base de datos Oracle, versión: "+Oracle.getConnection().ServerVersion);
                this.Connected = true;
                this.Button.Text = "Apagar servidor.";
            }
            catch (Exception ex){
                this.Console.AppendText("No se ha podido conectar a la base de datos Oracle:\n" + ex);
            }
        }

        private void ShutDown(){
            this.Connected = false;
            this.Button.Text = "Encerder servidor.";
            // TODO: Implementar desconexion a la base de datos y desconectar sockets.
        }
    }
}
