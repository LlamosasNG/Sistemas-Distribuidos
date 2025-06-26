using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;

namespace FunctionApp1
{
    public class borra_usuario
    {
        class ParamBorraUsuario
        {
            public string? email;
        }

        class Error
        {
            public string mensaje;
            public Error(string mensaje)
            {
                this.mensaje = mensaje;
            }
        }

        [Function("borra_usuario")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
            HttpRequest req)
        {
            try
            {
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                ParamBorraUsuario? data = JsonConvert.DeserializeObject<ParamBorraUsuario>(body);

                if (data == null || data.email == null)
                    throw new Exception("Se debe proporcionar el email del usuario");

                string email = data.email;

                // Configuración de conexión a base de datos
                string? Server = Environment.GetEnvironmentVariable("Server");
                string? UserID = Environment.GetEnvironmentVariable("UserID");
                string? Password = Environment.GetEnvironmentVariable("Password");
                string? Database = Environment.GetEnvironmentVariable("Database");
                string cs = "Server=" + Server + ";UserID=" + UserID + ";Password=" + Password + ";" + "Database=" + Database + ";SslMode=Preferred;";

                var conexion = new MySqlConnection(cs);
                conexion.Open();

                try
                {
                    // 1. Verificar si el usuario existe
                    MySqlCommand cmd_1 = new MySqlCommand();
                    cmd_1.Connection = conexion;
                    cmd_1.CommandText = "SELECT 1 FROM usuarios WHERE email = @email";
                    cmd_1.Parameters.AddWithValue("@email", email);

                    MySqlDataReader reader = cmd_1.ExecuteReader();

                    try
                    {
                        if (!reader.Read())
                        {
                            return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error("El email no existe")));
                        }
                    }
                    finally
                    {
                        reader.Close();
                    }

                    // 2. Iniciar transacción para el borrado
                    MySqlTransaction transaccion = conexion.BeginTransaction();

                    try
                    {
                        // 3. Eliminar fotos del usuario
                        MySqlCommand cmd_2 = new MySqlCommand();
                        cmd_2.Connection = conexion;
                        cmd_2.Transaction = transaccion;
                        cmd_2.CommandText = @"DELETE FROM fotos_usuarios 
                                             WHERE id_usuario = (SELECT id_usuario FROM usuarios WHERE email = @email)";
                        cmd_2.Parameters.AddWithValue("@email", email);
                        cmd_2.ExecuteNonQuery();

                        // 4. Eliminar el usuario
                        MySqlCommand cmd_3 = new MySqlCommand();
                        cmd_3.Connection = conexion;
                        cmd_3.Transaction = transaccion;
                        cmd_3.CommandText = "DELETE FROM usuarios WHERE email = @email";
                        cmd_3.Parameters.AddWithValue("@email", email);
                        cmd_3.ExecuteNonQuery();

                        transaccion.Commit();
                        return new OkResult();
                    }
                    catch (Exception e)
                    {
                        transaccion.Rollback();
                        throw new Exception(e.Message);
                    }
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message);
                }
                finally
                {
                    conexion.Close();
                }
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error(e.Message)));
            }
        }
    }
}