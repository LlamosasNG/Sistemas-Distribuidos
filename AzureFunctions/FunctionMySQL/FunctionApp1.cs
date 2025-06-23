// Carlos Pineda G. 2024

//#define INSERT
//#define SELECT

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;


#if SELECT
using Newtonsoft.Json;
#endif

namespace Company.Function
{
    public class Function1
    {
#if SELECT
        class Usuario
        {
            public int id_usuario;
            public string? email;
            public string? nombre;
            public string? apellido_paterno;
            public string? apellido_materno;
            public DateTime fecha_nacimiento;
            public long? telefono;
            public char? genero;
        }
#endif
        [Function("Function1")]
        public IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")]
            HttpRequest req)
        {
            string? Server = Environment.GetEnvironmentVariable("Server");
            string? UserID = Environment.GetEnvironmentVariable("UserID");
            string? Password = Environment.GetEnvironmentVariable("Password");
            string? Database = Environment.GetEnvironmentVariable("Database");
            string sc = "Server=" + Server + ";UserID=" + UserID +
            ";Password=" + Password + ";Database=" + Database +
            ";SslMode=Preferred;";
            var conexion = new MySqlConnection(sc);
            conexion.Open();

#if INSERT
            MySqlTransaction transaccion = conexion.BeginTransaction();
            try
            {
                var cmd = new MySqlCommand();
                cmd.Connection = conexion;
                cmd.Transaction = transaccion;
                cmd.CommandText = "insert into usuarios(id_usuario,email,nombre,apellido_paterno,apellido_materno," +
                                  "fecha_nacimiento,telefono,genero) values(0,'a@c','nombre','apellido paterno'," +
                                  "'apellido materno','2021-01-01','1234567890','M')";
                cmd.ExecuteNonQuery();
                transaccion.Commit();
                return new OkObjectResult("OK");
            }
            catch (Exception e)
            {
                transaccion.Rollback();
                return new BadRequestObjectResult(e.Message);
            }
            finally
            {
                conexion.Close();
            }
#endif
#if SELECT
            // obtiene el email que pasa como parámtro en la URL
            string? email = req.Query["email"];

            try
            {
                var cmd = new MySqlCommand("select id_usuario,email,nombre,apellido_paterno,apellido_materno," +
                                           "fecha_nacimiento,telefono,genero from usuarios where email=@email", conexion);
                cmd.Parameters.AddWithValue("@email", email);
                MySqlDataReader r = cmd.ExecuteReader();
                List<Usuario> lista = new List<Usuario>();
                while (r.Read())
                {
                    Usuario usuario = new Usuario();
                    usuario.id_usuario = r.GetInt32(0);
                    usuario.email = r.GetString(1);
                    usuario.nombre = r.GetString(2);
                    usuario.apellido_paterno = r.GetString(3);
                    usuario.apellido_materno = r.GetString(4);
                    usuario.fecha_nacimiento = r.GetDateTime(5);
                    usuario.telefono = r.GetInt64(6);
                    usuario.genero = r.GetChar(7);
                    lista.Add(usuario);
                }

                return new OkObjectResult(JsonConvert.SerializeObject(lista));
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult(e.Message);
            }
            finally
            {
                conexion.Close();
            }
#endif
        }
    }
}