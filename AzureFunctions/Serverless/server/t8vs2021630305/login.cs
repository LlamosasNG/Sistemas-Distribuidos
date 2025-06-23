// (c) Carlos Pineda Guerrero. 2025
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;

namespace ServicioWeb
{
    public class login
    {
        class ParamLogin
        {
            public string? email;
            public string? password;
        }

        class Error
        {
            public string mensaje;
            public Error(string mensaje)
            {
                this.mensaje = mensaje;
            }
        }

        [Function("login")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
            HttpRequest req)
        {
            try
            {
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                ParamLogin? data = JsonConvert.DeserializeObject<ParamLogin>(body);

                if (data == null)
                    throw new Exception("Se esperan los parámetros de login");

                string? email = data.email;
                string? password = data.password;

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
                    // Consulta para obtener id_usuario, token y nombre con email y password
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = conexion;
                    cmd.CommandText = "SELECT id_usuario, token, nombre FROM usuarios WHERE email = @email AND password = @password";

                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@password", password);

                    MySqlDataReader reader = cmd.ExecuteReader();

                    try
                    {
                        if (reader.Read())
                        {
                            // Creamos un objeto con id_usuario, token y nombre
                            var respuesta = new Dictionary<string, object>
                            {
                                { "id_usuario", reader.GetInt32(0) },
                                { "token", reader.IsDBNull(1) ? "" : reader.GetString(1) },
                                { "nombre", reader.IsDBNull(2) ? "" : reader.GetString(2) }
                            };

                            return new OkObjectResult(JsonConvert.SerializeObject(respuesta));
                        }

                        // Si no hay coincidencia, enviamos token vacío
                        var respuestaVacia = new Dictionary<string, object>
                        {
                            { "token", "" }
                        };

                        return new OkObjectResult(JsonConvert.SerializeObject(respuestaVacia));
                    }
                    finally
                    {
                        reader.Close();
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