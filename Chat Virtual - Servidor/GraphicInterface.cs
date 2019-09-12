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
            this.Connected = true;
            // TODO: Implementar la conexion a la base ded atos y conectar sockets.
        }

        private void ShutDown(){
            this.Connected = true;
            // TODO: Implementar desconexion a la base de datos y desconectar sockets.
        }
    }
}
