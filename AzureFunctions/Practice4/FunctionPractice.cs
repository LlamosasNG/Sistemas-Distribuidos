using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FunctionPractice
{
    class Parametro //Pojo utilizado para deserializar el parámetro
    {
        public string? name { get; set; }
    }
    public class FunctionPractice
    {
        [Function("FunctionPractice")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            // Obtiene los parámetros que pasan en la URL
            string? name = req.Query["name"];
            // Obtiene los parámetros que pasan en el body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic? data = JsonConvert.DeserializeObject<Parametro>(requestBody);
            name = name != null ? name : data != null ? data.name : "?";
            return new OkObjectResult($"Hola, {name}");
        }
    }

}