using System;
using Oracle.ManagedDataAccess.Client;

namespace PruebaConexionOracle {
    public class OracleDataBase :IDisposable {

        private OracleConnection Connection;
        private OracleTransaction Transaction;
        private OracleDataReader DataReader;
        public byte Intentos = 0;

        //Getters y Setters Primitivos

        public OracleConnection getConnection() {
            return this.Connection;
        }

        public void setConnection(OracleConnection Connection) {
            this.Connection = Connection;
        }

        public OracleDataReader getDataReader() {
            return this.DataReader;
        }

        public void getDataReader(OracleDataReader DataReader) {
            this.DataReader = DataReader;
        }

        //TODO: Getters y Setters con get; y set;

        private struct EstadoConexion {
            public string ConnectionString;
            public string ErrorDescription;
            public int ErrorNumber;
        }

        private EstadoConexion ConnectionState;

        public string ErrorDescription {
            get { return this.ConnectionState.ErrorDescription; }
        }

        public string ErrorNumber {
            get { return ConnectionState.ErrorNumber.ToString(); }
        }

        public OracleDataBase(string Server, string Port, string Service, string User, string Password) {
            ConnectionState.ConnectionString = "Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(Host=" +
                    Server + ")(Port=" + Port + ")))(CONNECT_DATA=(SERVICE_NAME=" +
                    Service + "))); User Id=" + User + ";Password=" + Password + "; ";
            Connection = new OracleConnection();
            Connect();

        }

        private void AssignError(ref Exception ex) {
            if (ex is OracleException) {
                ConnectionState.ErrorNumber = ((OracleException)ex).Number;
                ConnectionState.ErrorDescription = ex.Message;
            } else {
                ConnectionState.ErrorNumber = 0;
                ConnectionState.ErrorDescription = ex.Message;
            }
            // Guardar Log de Error.
            //TODO: Guardar Logs de errores.
        }


        private bool Connect() {
            bool flag = false;
            try {
                if (Connection != null) {
                    Connection.ConnectionString = ConnectionState.ConnectionString;
                    Connection.Open();
                    flag = true;
                }
            } catch (Exception ex) {
                Disconnect();
                AssignError(ref ex);
                flag = false;
            }
            return flag;
        }

        private bool Disconnect() {
            bool flag = false;
            try {
                if (Connection != null) {
                    if (Connection.State != System.Data.ConnectionState.Closed) {
                        Connection.Close();
                    }
                }
                Connection.Dispose();
                flag = true;
            } catch (Exception ex) {
                AssignError(ref ex);
                flag = false;
            }
            return flag;
        }

        public bool ExecuteProcedure(ref OracleCommand OraCommand, string SpName) {

            bool flag = true;

            try {
                if (!IsConected()) {
                    flag = Connect();
                }

                if (flag) {
                    OraCommand.Connection = Connection;
                    OraCommand.CommandText = SpName;
                    OraCommand.CommandType = System.Data.CommandType.StoredProcedure;
                    OraCommand.ExecuteNonQuery();
                }
            } catch (Exception ex) {
                AssignError(ref ex);
                flag = false;
            }
            return flag;
        }

        public bool IsConected() {
            bool flag = false;
            try {
                if (Connection != null) {
                    switch (Connection.State) {
                        case System.Data.ConnectionState.Closed:
                        case System.Data.ConnectionState.Broken:
                        case System.Data.ConnectionState.Connecting:
                            flag = false;
                            break;
                        case System.Data.ConnectionState.Open:
                        case System.Data.ConnectionState.Fetching:
                        case System.Data.ConnectionState.Executing:
                            flag = true;
                            break;
                    }
                } else {
                    flag = false;
                }

            } catch (Exception ex) {
                AssignError(ref ex);
                flag = false;
            }

            return flag;

        }

        public bool ExecuteSQL(string SqlQuery) {
            bool flag = true;
            OracleCommand Command = new OracleCommand();
            try {
                if (!IsConected()) {
                    flag = Connect();
                }

                if (flag) {
                    if ((DataReader != null)) {
                        DataReader.Close();
                        DataReader.Dispose();
                    }

                    Command.Connection = Connection;
                    Command.CommandType = System.Data.CommandType.Text;
                    Command.CommandText = SqlQuery;
                    DataReader = Command.ExecuteReader();
                }

            } catch (Exception ex) {
                AssignError(ref ex);
                flag = false;
            } finally {
                if (Command != null) {
                    Command.Dispose();
                }
            }

            return flag;

        }

        public bool ExecuteSQL(string SqlQuery, ref int Rows) {
            bool flag = true;
            OracleCommand Command = new OracleCommand();
            try {
                if (!IsConected()) {
                    flag = Connect();
                }

                if (flag) {
                    Transaction = Connection.BeginTransaction();
                    Command = Connection.CreateCommand();
                    Command.CommandType = System.Data.CommandType.Text;
                    Command.CommandText = SqlQuery;
                    Rows = Command.ExecuteNonQuery();
                    Transaction.Commit();
                }

            } catch (Exception ex) {
                Transaction.Rollback();
                AssignError(ref ex);
                flag = false;
            } finally {
                if (Command != null) {
                    Command.Dispose();
                }
            }

            return flag;

        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
            }
            try {
                if (DataReader != null) {
                    DataReader.Close();
                    DataReader.Dispose();
                }
                if (!Disconnect()) {
                    // Guardar Log de Error.
                    //TODO: log error.
                }

            } catch (Exception ex) {
                AssignError(ref ex);
            }

        }

        ~OracleDataBase() {
            Dispose(false);
        }


    }
}
