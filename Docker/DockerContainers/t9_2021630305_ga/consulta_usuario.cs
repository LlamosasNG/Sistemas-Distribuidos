using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;

namespace ServicioWeb
{
    public class consulta_usuario
    {
        class Usuario
        {
            public string? email;
            public string? password;
            public string? nombre;
            public string? apellido_paterno;
            public string? apellido_materno;
            public DateTime? fecha_nacimiento;
            public long? telefono;
            public string? genero;
            public string? foto;  // foto en base 64
        }

        class ParamConsultaUsuario
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

        [Function("consulta_usuario")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
            HttpRequest req)
        {
            try
            {
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                ParamConsultaUsuario? data = JsonConvert.DeserializeObject<ParamConsultaUsuario>(body);

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
                    // Consulta que incluye password y hace LEFT JOIN con fotos_usuarios
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = conexion;
                    cmd.CommandText = @"SELECT a.email, a.password, a.nombre, a.apellido_paterno, a.apellido_materno, 
                                              a.fecha_nacimiento, a.telefono, a.genero, b.foto 
                                       FROM usuarios a 
                                       LEFT OUTER JOIN fotos_usuarios b ON a.id_usuario = b.id_usuario 
                                       WHERE a.email = @email";

                    cmd.Parameters.AddWithValue("@email", email);

                    MySqlDataReader reader = cmd.ExecuteReader();

                    try
                    {
                        if (reader.Read())
                        {
                            Usuario usuario = new Usuario();
                            usuario.email = reader.IsDBNull(0) ? null : reader.GetString(0);
                            usuario.password = reader.IsDBNull(1) ? null : reader.GetString(1);
                            usuario.nombre = reader.IsDBNull(2) ? null : reader.GetString(2);
                            usuario.apellido_paterno = reader.IsDBNull(3) ? null : reader.GetString(3);
                            usuario.apellido_materno = reader.IsDBNull(4) ? null : reader.GetString(4);
                            usuario.fecha_nacimiento = reader.IsDBNull(5) ? null : reader.GetDateTime(5);
                            usuario.telefono = reader.IsDBNull(6) ? null : reader.GetInt64(6);
                            usuario.genero = reader.IsDBNull(7) ? null : reader.GetString(7);

                            // Convertir foto de bytes a Base64 si existe
                            if (!reader.IsDBNull(8))
                            {
                                byte[] fotoBytes = (byte[])reader.GetValue(8);
                                usuario.foto = Convert.ToBase64String(fotoBytes);
                            }

                            return new OkObjectResult(JsonConvert.SerializeObject(usuario));
                        }
                        else
                        {
                            return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error("El email no existe")));
                        }
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