using DataStructures;
using ShippingData;
using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Chat_Virtual___Servidor {

    /// <summary>
    /// Esta Clase se encarga de todas las conexiones del servidor.
    /// </summary>
    public class ServerConnection{
        public bool Connected { get; set; }                                        // Indica si el servidor esta encendido o no.
        private TcpListener Server;                                                 // Permite la escucha de conexiones entrantesde red TCP.
        private readonly GraphicInterface GraphicInterface;                         // Referencia al conexto gráfico.
        private readonly LinkedList<User> Users;                                    // Colección que contiene los usuarios conectados.
        private readonly IPEndPoint Ip;
        private readonly DataBaseConnection Oracle;

        //Métodos delegados, permiten que se pueda acceder a la interfaz desde cualquier hilo.
        private delegate void DLogConsoleAppend(string text);
        private delegate void DButtonText(string text);
        private delegate void DButtonEnable(bool flag);
        private delegate void DDataGridViewPush(string name, string ip);
        private delegate void DDataGridViewPop(string name);
        private delegate void DMenuEnable(bool flag);
        private delegate void DAlterTable(string name, string ip);

        /// <summary>
        /// Constructor. Inicializa la instancia para poder ser posteriormente utilizada como servidor.
        /// </summary>
        /// <param name="GraphicInterface">Referencia al a interfaz gráfica.</param>
        public ServerConnection(GraphicInterface GraphicInterface) {
            OracleConfigInterface.InitSettings();
            ServerConfigInterface.InitSettings();
            this.Connected = false;
            this.GraphicInterface = GraphicInterface;
            this.Oracle = new DataBaseConnection(this.GraphicInterface);
            this.Ip = new IPEndPoint(IPAddress.Any, ServerConfigInterface.Settings.port);
            this.Users = new LinkedList<User>();
        }

        /// <summary>
        /// Enciende el servidor, permite que se escuchen solicitudes de conexión entrantes.
        /// </summary>
        public void ShutUp() {
            new Thread(this.Connect) {
                IsBackground = true
            }.Start();
        }

        /// <summary>
        /// Apaga el servidor, elimina la conexión de la base de datos y finaliza la escucha de nuevas solicitudes
        /// </summary>
        public void ShutDown() {
            try {
                this.DisconnectListener();
            } catch (Exception ex) {
                this.ConsoleAppend("No se ha podido interrumpir escucha de nuevas conexiones: \n " + ex);
            }
            try {
                this.Oracle.DisconnectDataBase();
            } catch (Exception ex) {
                this.ConsoleAppend("No se ha podido desconectar la base de datos: \n " + ex);
            }
            this.Connected = false;
            this.ButtonText("Encender Servidor");
            this.MenuEnable(true);

        }

        /// <summary>
        /// Método de un hilo. Inicializa la escucha de solicitudes y la base de datos.
        /// </summary>
        private void Connect() {
            this.MenuEnable(false);
            this.ButtonEnable(false);
            this.Connected = true;
            try {
                this.ConnectListener();
            } catch (Exception ex) {
                this.ConsoleAppend("No se ha conseguido inicializar el servidor correctamente: \n" + ex + "/n");
                this.ConsoleAppend("Servidor no inicializado.");
                this.ButtonEnable(true);
                this.MenuEnable(true);
                this.Connected = false;
                return;
            }
            try {
                this.Oracle.ConnectDataBase();
            } catch (Exception ex) {
                this.ConsoleAppend("No se ha podido conectar a la base de datos: \n " + ex + "\n");
                this.ConsoleAppend("Servidor no inicializado.\n");
                this.DisconnectListener();
                this.ButtonEnable(true);
                this.MenuEnable(true);
                this.Connected = false;
                return;
            }
            this.ConsoleAppend("Servidor inicializado correctamente.\n");
            this.ButtonText("Apagar Servidor");
            this.ButtonEnable(true);
        }

        /// <summary>
        /// Detiene la escucha de solicitudes de conexión.
        /// </summary>
        private void DisconnectListener() {
            this.Server.Stop();
            this.ConsoleAppend("Se ha detenido el servidor correctamente.");
            this.ConsoleAppend("Se han dejado de escuchar solicitudes de conexión entrantes.");
        }

        /// <summary>
        /// Permite que las solicitudes de conexión sean escuchadas.
        /// </summary>
        private void ConnectListener() {
            this.ConsoleAppend("Iniciando conexión de los sockets.\n");
            this.Server = new TcpListener(this.Ip);
            this.Server.Start();
            this.ConsoleAppend("El servidor se a inicializado correctamente.");
            Thread t1 = new Thread(this.ListenConnection) {
                IsBackground = true
            };
            t1.Start();
            this.ConsoleAppend("Se han comenzado a escuchar solicitudes de conexión entrantes.\n");
            Thread t2 = new Thread(this.ExecuteRequest) {
                IsBackground = true
            };
            t2.Start();
            Thread t3 = new Thread(this.ListenUsers) {
                IsBackground = true
            };
            t3.Start();
            Thread t4 = new Thread(this.WriteUsers) {
                IsBackground = true
            };
            t4.Start();
        }

        /// <summary>
        /// Método de Hilo. Redirige los mensaje y responde a las peticiones de los usuarios
        /// </summary>
        private void ExecuteRequest() {
            ChainNode<User> Node = null;
            do {
                if (this.Users.IsEmpty()) {                     //Verifica que la lista de ususarios no está vacia
                    continue;                                   //En caso de que lo está continúa con la ejecucion
                } else if (Node == null) {                      //Verifia si el nodo que tengo es un apuntador nulo
                    Node = this.Users.GetNode(0);               //En ese caso obtiene el primer nodo de la lista
                }
                if (Node.element.ReadingQueue.IsEmpty()) {
                    continue;
                }
                
                Data data = Node.element.ReadingQueue.Dequeue();
                if (data is ChatMessage chatMessage) {
                    ChainNode<User> receiver = this.Users.GetNode(0);
                    while (receiver.next != null) {
                        if (receiver.element.Name.Equals(chatMessage.Receiver)) {
                            receiver.element.WritingQueue.Enqueue(chatMessage);
                            // ¿Se va a enviar el mismo objeto?
                            break;
                        }
                    }
                    // TODO: falta hacer el insert a la tabla
                } /*else if (data is ChatGroup chatGroup) {
                    //hay que mirar en la tabla y ver que usuarios están en el grupo para enviarles el mensaje
                }else if (data is DisconnectRequest dr) {
                    this.ConsoleAppend("El usuario [" + U.Name + " | " + U.Client.Client.RemoteEndPoint.ToString() + "] se ha desconectado del servidor.");
                    this.Users.Remove(Node);
                    this.DeleteTable(Node.element.Name);
                }*/
                Node = Node.next;
            } while (this.Connected);
        }

        /// <summary>
        /// Método de hilo. Lee cada usuario y revisa si le ha enviado datos al servidor.
        /// </summary>
        private void ListenUsers() { 
            ChainNode<User> Node = null;
            do {
                if (this.Users.IsEmpty()) {                     //Verifica que la lista de ususarios no está vacia
                    continue;                                   //En caso de que lo está continúa con la ejecucion
                } else if (Node == null) {                      //Verifia si el nodo que tengo es un apuntador nulo
                    Node = this.Users.GetNode(0);               //En ese caso obtiene el primer nodo de la lista
                }
                this.Read(Node.element);                        //Intenta leer los datos que estén pendientes para ese usuario
                Node = Node.next;                               //Avanza al siguiente nodo
            } while (this.Connected);
        }

        /// <summary>
        /// Método de hilo. Escribe los datos listos en los clientes.
        /// </summary>
        private void WriteUsers() {
            ChainNode<User> Node = null;
            do {
                if (this.Users.IsEmpty()) {                     //Verifica que la lista de ususarios no está vacia
                    continue;                                   //En caso de que lo está continúa con la ejecucion
                } else if (Node == null) {                      //Verifia si el nodo que tengo es un apuntador nulo
                    Node = this.Users.GetNode(0);               //En ese caso obtiene el primer nodo de la lista
                }
                this.Write(Node.element);                       //Intenta escribir los datos que estén pendientes para ese usuario
                Node = Node.next;                               //Avanza al siguiente nodo
            } while (this.Connected);
        }

        /// <summary>
        /// Esta a la escucha de nuevos clientes y verifica su información de inicio de sesión.
        /// </summary>
        private void ListenConnection() {          
            do {
                try {
                    if (this.Server.Pending()) {
                        this.ConsoleAppend("Prueba");
                        User U = new User (this.Server.AcceptTcpClient());
                        Thread.Sleep(1000);
                        for (int i = 0; i<1000; i++) {
                            this.Read(U);
                        }
                        
                        object obj = U.ReadingQueue.Dequeue();
                        if (obj is SignIn si) {
                            bool exist = false;
                            this.Oracle.Oracle.ExecuteSQL("SELECT USERNAME,CONTRASENA FROM USUARIO");
                            while (this.Oracle.Oracle.DataReader.Read()) {
                                if (this.Oracle.Oracle.DataReader["USERNAME"].Equals(si.user) && this.Oracle.Oracle.DataReader["CONTRASENA"].Equals(si.password)) {
                                    exist = true;
                                    break;
                                }
                            }
                            if (exist) {
                                U.Name = si.user;                                       //Inicializa el nombre de ususario
                                U.WritingQueue.Enqueue(new RequestAnswer(true));        //Agrega la respuesta a la cola de envío
                                this.Write(U);                                         //Envía la respuesta
                                this.Users.Add(U);                                  //Agrega el ususario a la de inicialización
                                this.ConsoleAppend("El usuario [" + U.Name + " | " + U.Client.Client.RemoteEndPoint.ToString() + "] se ha conectado satisfactoriamente.");
                                this.InsertTable(U.Name, U.Client.Client.RemoteEndPoint.ToString());


                                /*ChatMessage[] ms = new ChatMessage[20];
                                for (int i = 0; i<ms.Length; i++) {
                                    ms[i] = new ChatMessage("jdiegopm","jdiegopm","Prueba "+i);
                                }

                                for (int i = 0; i<ms.Length; i++) {
                                    U.WritingQueue.Enqueue(ms[i]);
                                }*/
                                


                            } else {
                                U.WritingQueue.Enqueue(new RequestAnswer(false));       //Agrega la respuesta a la cola de envío
                                U.WritingQueue.Enqueue(new RequestError(1));            //Especifica el error del fallo
                                this.Write(U);                                    //Envía la respuesta
                                this.ConsoleAppend("Se ha intentado conectar el remoto [" + U.Client.Client.RemoteEndPoint.ToString() + "] con información de inicio de sesión incorrecta.");
                                U.Client.Close();
                            }
                        } else if (obj is SignUp su) {
                            RequestAnswer answer;
                            if (this.Oracle.Oracle.ExecuteSQL("INSERT INTO USUARIOS VALUES('" + su.userName + "','" + su.name + "','" + su.password + "',SYSDATE)")) {
                                answer = new RequestAnswer(true);
                                U.WritingQueue.Enqueue(answer);
                                this.Write(U);
                                this.ConsoleAppend("Se ha registrado el usuario [" + U.Name + " | " + U.Client.Client.RemoteEndPoint.ToString() + "] correctamente.");
                                this.Users.Add(U);
                                this.ConsoleAppend("El usuario [" + U.Name + " | " + U.Client.Client.RemoteEndPoint.ToString() + "] se ha conectado satisfactoriamente.");
                                this.InsertTable(U.Name, U.Client.Client.RemoteEndPoint.ToString());
                            } else {
                                answer = new RequestAnswer(false);
                                U.WritingQueue.Enqueue(answer);
                                U.WritingQueue.Enqueue(new RequestError(0));
                                this.Write(U);
                                this.ConsoleAppend("Se ha intentado registrar el remoto [" + U.Client.Client.RemoteEndPoint.ToString() + "] con un nombre de usuario ya existente.");
                                U.Client.Close();
                            }
                        } /*else if (obj is ConnectionTest ct) {
                            // ¿Se va a devolver algo?
                        } else if (obj is ReconnectRequest rr) {
                            ChainNode<User> temp = this.Users.Get(0);
                            while (temp.next != null) {
                                if (U.Name.Equals(temp.element.Name)) {
                                    // Sustutuir nodo
                                    // Actualizar tabla de usuarios.
                                    this.ConsoleAppend("Reconectado correctamente con el usuario [" + U.Name + " | " + U.Client.Client.RemoteEndPoint.ToString() + "].");

                                    break;
                                }
                                temp = temp.next;
                            }
                        }*/ else {
                            this.ConsoleAppend("La información de la nueva conexión no pudo ser interpretada correctamente.");
                        }
                    }
                } catch (Exception) { }
            } while (this.Connected);
        }

        /// <summary>
        /// Escribe los datos que esten pendientes en la cola toWrite de un ususario
        /// </summary>
        /// <param name="user">El ususario del que se van a intentar escribir los datos</param>
        /// <returns>Verdadero si los datos fueron enviados, falso si almenos uno falló</returns>
        private bool Write(User user) {
            try {
                if (!user.WritingQueue.IsEmpty()) {                                                  // Verifica que la cola no este vacía
                    byte[] toSend = Serializer.Serialize(user.WritingQueue.GetFrontElement());       // Serializa el primer objeto de la cola
                    user.Writer.Write(toSend.Length);                                                // Envía el tamaño del objeto
                    user.Writer.Write(toSend);                                                       // Envía el objeto
                    user.WritingQueue.Dequeue();                                                     // Saca el objeto de la cola
                }
                return true;
            } catch (Exception) {
                //this.ConsoleAppend("Se ha perdido la conexión con el usuario [" + user.Name + "] Intentando reconectar.");
                return false;
            }
        }

        /// <summary>
        /// Lee los datos que estén pendientes para un usuario y los guarda en la cola toRead del mismo
        /// </summary>
        /// <param name="user">EL ususario del que se van a intentar leer datos</param>
        /// <returns>Verdadero si le leyeron todos los datos, falso si uno de ellos no pudo ser leido</returns>
        private bool Read(User user) {
            try {
                if (user.Stream.DataAvailable) {                                            // Verefica si hay datos por leer
                    int size = user.Reader.ReadInt32();                                     // lee el tamaño del objeto
                    byte[] data = new byte[size];                                           // crea el arreglo de bytes para el objeto
                    data = user.Reader.ReadBytes(size);                                     // lee el el objeto y lo guarda en el arreglo de bytes
                    object a = Serializer.Deserialize(data);                                // deserializa el objeto
                    user.ReadingQueue.Enqueue((Data)a);                                     // guarda el objeto en la cola
                }
                return true;
            } catch (Exception) {
                //this.ConsoleAppend("Se ha perdido la conexión con el usuario [" + user.Name + "] Intentando reconectar.");
                return false;
            }
        }

        /// <summary>
        /// Metodo para hilo. Agrega el texto a la interfaz gráfica.
        /// </summary>
        /// <param name="text">Texto a agregar a la consola.</param>
        private void ConsoleAppend(string text) {
            if (this.GraphicInterface.LogConsole.InvokeRequired) {
                DLogConsoleAppend d = new DLogConsoleAppend(this.ConsoleAppend);
                this.GraphicInterface.LogConsole.Invoke(d, new object[] { text });
            } else {
                this.GraphicInterface.LogConsole.AppendText("[" + DateTime.Now.ToString(new CultureInfo("en-GB")) + "] " + text + "\n");
            }
        }

        /// <summary>
        /// Metodo para hilo. Cambia el texto del botón de la interfaz.
        /// </summary>
        /// <param name="text">Texto por el cual se va a cambiar el texto.</param>
        private void ButtonText(string text) {
            if (this.GraphicInterface.Button.InvokeRequired) {
                DButtonText d = new DButtonText(this.ButtonText);
                this.GraphicInterface.Button.Invoke(d, new object[] { text });
            } else {
                this.GraphicInterface.Button.Text = text;
            }
        }

        /// <summary>
        /// Metodo para hilo. definie si el botón esta habilitado.
        /// </summary>
        /// <param name="flag">Bandera con la cual se habilitará o deshabilitará el botón.</param>
        private void ButtonEnable(bool flag) {
            if (this.GraphicInterface.Button.InvokeRequired) {
                DButtonEnable d = new DButtonEnable(this.ButtonEnable);
                this.GraphicInterface.Button.Invoke(d, new object[] { flag });
            } else {
                this.GraphicInterface.Button.Enabled = flag;
            }
        }

        /// <summary>
        /// Metodo para hilo. Agrega el un usuario conectado a la tabla de la interfaz.
        /// </summary>
        /// <param name="name">Nombre del usuario que se va a conectar.</param>
        /// <param name="ip">Ip del usuario que se va a conectar.</param>
        private void InsertTable(string name, string ip) {
            if (this.GraphicInterface.UsersTable.InvokeRequired) {
                DDataGridViewPush d = new DDataGridViewPush(this.InsertTable);
                this.GraphicInterface.Button.Invoke(d, new object[] { name, ip });
            } else {
                this.GraphicInterface.UsersTable.Rows.Add(name, ip);
            }
        }

        /// <summary>
        /// Metodo para hilo. Agrega el un usuario conectado a la tabla de la interfaz.
        /// </summary>
        /// <param name="name">Nombre del usuario que se va a conectar.</param>
        private void DeleteTable(string name) {
            if (this.GraphicInterface.UsersTable.InvokeRequired) {
                DDataGridViewPop d = new DDataGridViewPop(this.DeleteTable);
                this.GraphicInterface.Button.Invoke(d, new object[] { name});
            } else {
                int size = this.GraphicInterface.UsersTable.Rows.Count;
                for (int i = 0; i<size; i++) {
                    if (this.GraphicInterface.UsersTable.Rows[i].Cells[0].Value.Equals(name)) {
                        this.GraphicInterface.UsersTable.Rows.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Metodo para hilo. Agrega el un usuario conectado a la tabla de la interfaz.
        /// </summary>
        /// <param name="name">Nombre del usuario que se va a conectar.</param>
        private void AlterIpTable(string name, string ip) {
            if (this.GraphicInterface.UsersTable.InvokeRequired) {
                DAlterTable d = new DAlterTable(this.AlterIpTable);
                this.GraphicInterface.Button.Invoke(d, new object[] { name, ip });
            } else {
                int size = this.GraphicInterface.UsersTable.Rows.Count;
                for (int i = 0; i < size; i++) {
                    if (this.GraphicInterface.UsersTable.Rows[i].Cells[0].Value.Equals(name)) {
                        this.GraphicInterface.UsersTable.Rows[i].Cells[1].Value = ip;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Metodo para hilo. Habilita o deshabilita el menú.
        /// </summary>
        /// <param name="flag">Verdadero si se quiere habilitar.</param>
        private void MenuEnable(bool flag) {
            if (this.GraphicInterface.MenuBar.InvokeRequired) {
                DMenuEnable d = new DMenuEnable(this.MenuEnable);
                this.GraphicInterface.Button.Invoke(d, new object[] { flag });
            } else {
                this.GraphicInterface.configuraciónToolStripMenuItem.Enabled = flag;

            }
        }

    }
}
