using Chat_Virtual___Servidor.PetitionTypes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace Chat_Virtual___Servidor{
    public class ServerConnection{

        private readonly DataBaseConnection Oracle;
        private ConnectionSettings settings;
        public bool Connected { get; set; }
        public TcpListener Server { get; set; }
        public GraphicInterface GraphicInterface { get; set; }
        public LinkedList<User> Users { get; set; }
        public LinkedList<Group> Groups { get; set; }
        public LinkedList<string> Messages { get; set; }
        public TcpClient Client { get; set; }
        public IPEndPoint Ip { get; }

        private delegate void DLogConsoleAppend(string text);
        private delegate void DButtonText(string text);
        private delegate void DButtonEnable(bool flag);
        private delegate void DDataGridViewRow(string name, string ip);

        [Serializable]
        private struct ConnectionSettings {
            public int port;
            public int maxUsers;
        }
        public ServerConnection(GraphicInterface GraphicInterface) {
            this.Connected = false;
            this.GraphicInterface = GraphicInterface;
            this.Oracle = new DataBaseConnection(this.GraphicInterface);
            if (File.Exists("SocketSettings.config")) {
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream("SocketSettings.config", FileMode.Open, FileAccess.Read);
                this.settings = (ConnectionSettings)formatter.Deserialize(stream);
                stream.Close();
            } else {
                this.settings.port = 7777;
                this.settings.maxUsers = 30;
            }
            this.Ip = new IPEndPoint(IPAddress.Any, this.settings.port);
            this.Users = new LinkedList<User>();
            this.Groups = new LinkedList<Group>();
            this.Messages = new LinkedList<string>();
        }

        public void ShutUp() {
            new Thread(this.Connect) {
                IsBackground = true
            }.Start();
        }

        public void Connect() {
            this.ButtonEnable(false);
            try {
                this.ConnectSockets();
            } catch (Exception ex) {
                this.ConsoleAppend("No se ha conseguido inicializar el servidor correctamente: \n" + ex + "/n");
                this.ConsoleAppend("Servidor no inicializado.");
                this.ButtonEnable(true);
                return;
            }
            try {
                this.Oracle.ConnectDataBase();
            } catch (Exception ex) {
                this.ConsoleAppend("No se ha podido conectar a la base de datos: \n " + ex + "\n");
                this.ConsoleAppend("Servidor no inicializado.\n");
                this.DisconnectSockets();
                this.ButtonEnable(true);
                return;
            }
            this.Connected = true;
            this.ConsoleAppend("Servidor inicializado correctamente.\n");
            this.ButtonText("Apagar Servidor");
            this.ButtonEnable(true);
        }

        private void DisconnectSockets() {
            this.Server.Stop();
            this.ConsoleAppend("Se ha detenido el servidor correctamente.");
            this.ConsoleAppend("Se han dejado de escuchar solicitudes de conexión entrantes.");
        }

        public void ConnectSockets() {
            this.ConsoleAppend("Iniciando conexión de los sockets.\n");
            this.Server = new TcpListener(this.Ip);
            this.Server.Start();
            this.ConsoleAppend("El servidor se a inicializado correctamente.");
            Thread t1 = new Thread(this.ListenConnection) {
                IsBackground = true
            };
            t1.Start();
            this.ConsoleAppend("Se han comenzado a escuchar solicitudes de conexión entrantes.\n");
            /*Thread t2 = new Thread(this.ListenUsers) {
                IsBackground = true
            };
            t2.Start();
            Thread t3 = new Thread(this.WriteUsers) {

            };
            t3.Start();*/
        }

        private void ListenPetitions() {
            LinkedListNode<User> node = null;
            do {
                if (this.Users.Count == 0) {
                    continue;
                } else if (node == null) {
                    node = this.Users.First;
                }

                while(node.Value.Stream.DataAvailable) {
                    
                }
                if (node.Next == null) {
                    node = this.Users.First;
                } else {
                    node = node.Next;
                }
            } while (true);
        }


        // Este Hilo se va a eliminar para cambiar lo de serializacion.
        private void ListenUsers() {
            LinkedListNode<User> node = null;
            do {
                if (this.Users.Count==0) {
                    continue;
                }else if (node == null) {
                    node = this.Users.First;
                }

                if (node.Value.Stream.DataAvailable) {
                    //this.Messages.AddLast(node.Value.Name+": "+node.Value.Reader.ReadLine());
                    this.ConsoleAppend(this.Messages.Last.Value);
                    //TODO: POSIBLES SOLUCITUDES DIFERENTES A MENSAJES.
                }
                if (node.Next == null) {
                    node = this.Users.First;
                } else {
                    node = node.Next;
                }
            } while (true);
        }

        // Este Hilo se va a eliminar para cambiar lo de serializacion.
        private void WriteUsers() {
            LinkedListNode<User> node;
            string s;
            do {
                if (this.Messages.Count == 0) {
                    continue;
                } else {
                    node = this.Users.First;
                    s = this.Messages.First.Value;
                    this.Messages.RemoveFirst();
                }
                do {
                    try {
                        //node.Value.Writer.WriteLine(s);
                        node.Value.Writer.Flush();
                    } catch {
                        //TODO: Se ha desconectado.
                    }
                    node = node.Next;
                } while (node!=null);

            } while (true);
        }

        private void ListenConnection() {
            do {
                if (this.Server.Pending()) {
                    this.Client = this.Server.AcceptTcpClient();
                    User user = new User(this.Client.GetStream());
                    int size = user.Reader.ReadInt32();
                    object obj = Serializer.Deserialize(user.Reader.ReadBytes(size));
                    if (obj is SignIn si) {
                        bool exist = false;
                        this.Oracle.GetOracleDataBase().ExecuteSQL("SELECT USERNAME,CONTRASENA FROM USUARIO;");
                        while (this.Oracle.GetOracleDataBase().getDataReader().Read()) {
                            if (this.Oracle.GetOracleDataBase().getDataReader()["USERNAME"].Equals(si.Name) && this.Oracle.GetOracleDataBase().getDataReader()["CONTRASENA"].Equals(si.Password)) {
                                exist = true;
                                break;
                            }
                        }
                        if (exist) {
                            user.Name = si.Name;
                            user.Writer.Write(true);
                            user.Writer.Flush();
                            this.Users.AddLast(user); //TODO: Cambiar Implementación.
                            this.ConsoleAppend("El usuario [" + user.Name + " | " + this.Client.Client.RemoteEndPoint.ToString() + "] se ha conectado satisfactoriamente.");
                            this.InsertTable(user.Name, this.Client.Client.RemoteEndPoint.ToString());
                        } else {
                            user.Writer.Write(false);
                            user.Writer.Flush();
                            this.ConsoleAppend("Se ha intentado conectar el remoto [" + this.Client.Client.RemoteEndPoint.ToString() + "] con información de inicio de sesión incorrecta.");
                            this.Client.Client.Close();
                            this.Client.Close();
                        }
                    } else if (obj is SignUp su) {
                        if (this.Oracle.GetOracleDataBase().ExecuteSQL("INSERT INTO USUARIOS VALUES('" + su.Name + "','" + su.Password + "',DEFAULT)")) {
                            user.Writer.Write(true);
                            user.Writer.Flush();
                            this.ConsoleAppend("Se ha registrado el usuario [" + user.Name + " | " + this.Client.Client.RemoteEndPoint.ToString() + "] correctamente.");
                            this.Users.AddLast(user);
                            this.ConsoleAppend("El usuario [" + user.Name + " | " + this.Client.Client.RemoteEndPoint.ToString() + "] se ha conectado satisfactoriamente.");
                            // TODO: Actualizar Tabla.
                        } else {
                            user.Writer.Write(false);
                            user.Writer.Flush();
                            this.ConsoleAppend("Se ha intentado registrar el remoto [" + this.Client.Client.RemoteEndPoint.ToString() + "] con un nombre de usuario ya existente.");
                            this.Client.Close();
                        }
                    }
                } 
            } while (/*this.Connected && this.Users.Count()<this.settings.maxUsers*/true);
        }

        public void ShutDown() {
            try {
                this.DisconnectSockets();
            } catch(Exception ex) {
                this.ConsoleAppend("No se ha podido desconectar la conexión de los sockets: \n " + ex);
            }
            try {
                this.Oracle.DisconnectDataBase();
            } catch(Exception ex) {
                this.ConsoleAppend("No se ha podido desconectar la base de datos: \n " + ex);
            }
            this.Connected = false;
            this.ButtonText("Encender Servidor");
        }

        private void ConsoleAppend(string text) {
            if (this.GraphicInterface.LogConsole.InvokeRequired) {
                var d = new DLogConsoleAppend(this.ConsoleAppend);
                this.GraphicInterface.LogConsole.Invoke(d, new object[] { text });
            } else {
                this.GraphicInterface.LogConsole.AppendText("[" + DateTime.Now.ToString(new CultureInfo("en-GB")) + "] " + text + "\n");
            }
        }

        private void ButtonText(string text) {
            if (this.GraphicInterface.Button.InvokeRequired) {
                var d = new DButtonText(this.ButtonText);
                this.GraphicInterface.Button.Invoke(d, new object[] { text });
            } else {
                this.GraphicInterface.Button.Text = text;
            }
        }

        private void ButtonEnable(bool flag) {
            if (this.GraphicInterface.Button.InvokeRequired) {
                var d = new DButtonEnable(this.ButtonEnable);
                this.GraphicInterface.Button.Invoke(d, new object[] { flag });
            } else {
                this.GraphicInterface.Button.Enabled = flag;
            }
        }

        private void InsertTable(string name, string ip) {
            if (this.GraphicInterface.UsersTable.InvokeRequired) {
                var d = new DDataGridViewRow(this.InsertTable);
                this.GraphicInterface.Button.Invoke(d, new object[] { name, ip });
            } else {
                this.GraphicInterface.UsersTable.Rows.Add(name, ip);
            }
        }

    }
}
