using System;
using Oracle.ManagedDataAccess.Client;


namespace Chat_Virtual___Servidor{
public class OracleDataBase : IDisposable {

        public OracleConnection Connection { set; get; }
        public OracleTransaction Transaction { set; get; }
        public OracleDataReader DataReader { set; get; }

        private EstadoConexion ConnectionState;

        private struct EstadoConexion {
            public string ConnectionString;
            public string ErrorDescription;
            public int ErrorNumber;
        }

        public string ErrorDescription {
            get { return this.ConnectionState.ErrorDescription; }
        }

        public string ErrorNumber {
            get { return this.ConnectionState.ErrorNumber.ToString(); }
        }

        public OracleDataBase(string Server, string Port, string Service, string User, string Password) {
            this.ConnectionState.ConnectionString = "Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(Host=" +
                    Server + ")(Port=" + Port + ")))(CONNECT_DATA=(SERVICE_NAME=" +
                    Service + "))); User Id=" + User + ";Password=" + Password + "; ";
            this.Connection = new OracleConnection();
            this.Connect();

        }

        private void AssignError(ref Exception ex) {
            if (ex is OracleException) {
                this.ConnectionState.ErrorNumber = ((OracleException)ex).Number;
                this.ConnectionState.ErrorDescription = ex.Message;
            } else {
                this.ConnectionState.ErrorNumber = 0;
                this.ConnectionState.ErrorDescription = ex.Message;
            }
            // Guardar Log de Error.
            //TODO: Guardar Logs de errores.
        }


        private bool Connect() {
            bool flag = false;
            try {
                if (this.Connection != null) {
                    this.Connection.ConnectionString = this.ConnectionState.ConnectionString;
                    this.Connection.Open();
                    flag = true;
                }
            } catch (Exception ex) {
                this.Disconnect();
                this.AssignError(ref ex);
                flag = false;
            }
            return flag;
        }

        public bool Disconnect() {
            bool flag;
            try {
                if (this.Connection != null) {
                    if (this.Connection.State != System.Data.ConnectionState.Closed) {
                        this.Connection.Close();
                    }
                }
                this.Connection.Dispose();
                flag = true;
            } catch (Exception ex) {
                this.AssignError(ref ex);
                flag = false;
            }
            return flag;
        }

        public bool ExecuteProcedure(ref OracleCommand OraCommand, string SpName) {
            bool flag = true;
            try {
                if (!this.IsConected()) {
                    flag = this.Connect();
                }
                if (flag) {
                    OraCommand.Connection = this.Connection;
                    OraCommand.CommandText = SpName;
                    OraCommand.CommandType = System.Data.CommandType.StoredProcedure;
                    OraCommand.ExecuteNonQuery();
                }
            } catch (Exception ex) {
                this.AssignError(ref ex);
                flag = false;
            }
            return flag;
        }

        public bool IsConected() {
            bool flag = false;
            try {
                if (this.Connection != null) {
                    switch (this.Connection.State) {
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
                this.AssignError(ref ex);
                flag = false;
            }
            return flag;
        }

        public bool ExecuteSQL(string SqlQuery) {
            bool flag = true;
            OracleCommand Command = new OracleCommand();
            try {
                if (!this.IsConected()) {
                    flag = this.Connect();
                }
                if (flag) {
                    if ((this.DataReader != null)) {
                        this.DataReader.Close();
                        this.DataReader.Dispose();
                    }
                    Command.Connection = this.Connection;
                    Command.CommandType = System.Data.CommandType.Text;
                    Command.CommandText = SqlQuery;
                    this.DataReader = Command.ExecuteReader();
                }

            } catch (Exception ex) {
                this.AssignError(ref ex);
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
                if (!this.IsConected()) {
                    flag = this.Connect();
                }
                if (flag) {
                    this.Transaction = this.Connection.BeginTransaction();
                    Command = this.Connection.CreateCommand();
                    Command.CommandType = System.Data.CommandType.Text;
                    Command.CommandText = SqlQuery;
                    Rows = Command.ExecuteNonQuery();
                    this.Transaction.Commit();
                }
            } catch (Exception ex) {
                this.Transaction.Rollback();
                this.AssignError(ref ex);
                flag = false;
            } finally {
                if (Command != null) {
                    Command.Dispose();
                }
            }
            return flag;
        }

        public void Dispose() {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
            }
            try {
                if (this.DataReader != null) {
                    this.DataReader.Close();
                    this.DataReader.Dispose();
                }
                if (!this.Disconnect()) {
                    // Guardar Log de Error.
                    //TODO: log error.
                }
            } catch (Exception ex) {
                this.AssignError(ref ex);
            }

        }

        ~OracleDataBase() {
            this.Dispose(false);
        }

    }
}