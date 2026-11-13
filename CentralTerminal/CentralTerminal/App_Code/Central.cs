using System;
using System.IO;
using System.Net;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web.Services;
using System.Web.Services.Protocols;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[WebService(Namespace = "http://terminalsucre.org/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
public class Central : WebService
{
    // Servicios que ya tenemos funcionando
    private const string REST = "http://127.0.0.1:8000/api";
    private const string GRAPHQL = "http://127.0.0.1:4000/graphql";

    // Usuario creado con el seeder de Laravel
    private const string EMAIL = "admin@empresa.com";
    private const string PASSWORD = "12345678";

    // OPERACION 1: CONSULTAR SALIDAS
    [WebMethod]
    public Salida[] VerSalidas(string destino, string fecha)
    {
        if (!string.IsNullOrWhiteSpace(fecha))
        {
            DateTime fechaValidada;

            if (!DateTime.TryParseExact(
                fecha, "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out fechaValidada))
            {
                throw ErrorCliente("Fecha inválida. Use yyyy-MM-dd");
            }
        }

        string url = REST + "/salidas?destino=" +
            Uri.EscapeDataString(destino ?? "") +
            "&fecha=" + Uri.EscapeDataString(fecha ?? "");

        JToken respuesta = Peticion("GET", url, null, null);

        JArray lista = respuesta as JArray;

        if (lista == null)
            throw ErrorServidor("Respuesta inválida de Laravel");

        Salida[] salidas = new Salida[lista.Count];

        for (int i = 0; i < lista.Count; i++)
        {
            salidas[i] = ConvertirSalida(lista[i]);
        }

        return salidas;
    }

    // OPERACION 2: VENDER BOLETO
    [WebMethod]
    public Boleto VenderBoleto(string ci, int idSalida, int asiento)
    {
        // 1. Validar los datos recibidos
        if (string.IsNullOrEmpty(ci) ||
            !Regex.IsMatch(ci, @"^\d{5,10}$"))
        {
            throw ErrorCliente("CI inválido: debe tener entre 5 y 10 dígitos");
        }

        if (idSalida <= 0 || asiento <= 0)
        {
            throw ErrorCliente("Salida o asiento inválido");
        }

        // 2. Consultar el padrón mediante GraphQL CON VARIABLES
        JObject consulta = new JObject
        {
            ["query"] = @"query BuscarPasajero($ci: String!) {
                pasajero(ci: $ci) {
                    ci
                    nombres
                    apellidos
                    celular
                    frecuente
                }
            }",
            ["variables"] = new JObject
            {
                ["ci"] = ci
            }
        };

        JToken respuestaPadron = Peticion(
            "POST", GRAPHQL, consulta, null
        );

        // Verificar errores GraphQL
        JArray errores = respuestaPadron["errors"] as JArray;

        if (errores != null && errores.Count > 0)
        {
            string mensaje = errores[0]["message"]?.ToString()
                ?? "Error de consulta GraphQL";

            throw ErrorCliente(mensaje);
        }

        JToken pasajero = respuestaPadron["data"]?["pasajero"];

        if (pasajero == null ||
            pasajero.Type == JTokenType.Null)
        {
            throw ErrorCliente("Pasajero no registrado");
        }

        // 3. Consultar los datos de la salida en Laravel
        JToken datosSalida = Peticion(
            "GET", REST + "/salidas/" + idSalida, null, null
        );

        Salida salida = ConvertirSalida(datosSalida);

        // 4. Autenticarse en Laravel para obtener el JWT
        JObject credenciales = new JObject
        {
            ["email"] = EMAIL,
            ["password"] = PASSWORD
        };

        JToken login;

        try
        {
            login = Peticion(
                "POST", REST + "/login", credenciales, null
            );
        }
        catch (SoapException ex)
        {
            if (ex.Code == SoapException.ClientFaultCode)
                throw ErrorServidor(
                    "La Central no pudo autenticarse en Laravel: " +
                    ex.Message
                );

            throw;
        }

        string token = login["token"]?.ToString();

        if (string.IsNullOrWhiteSpace(token))
        {
            throw ErrorServidor("Laravel no devolvió el token JWT");
        }

        // 5. Reservar asiento utilizando Authorization: Bearer
        JObject reserva = new JObject
        {
            ["ci"] = ci
        };

        Peticion(
            "PUT",
            REST + "/salidas/" + idSalida + "/asientos/" + asiento,
            reserva,
            token
        );

        // 6. Generar comprobante de venta
        Boleto boleto = new Boleto
        {
            Codigo = "BOL-" + DateTime.Now.ToString("yyyyMMddHHmmss") +
                     "-" + idSalida + "-" + asiento,

            NombrePasajero = (
                pasajero["nombres"]?.ToString() + " " +
                pasajero["apellidos"]?.ToString()
            ).Trim(),

            Destino = salida.Destino,
            Fecha = salida.Fecha,
            Hora = salida.Hora,
            NumeroAsiento = asiento,
            Precio = salida.Precio
        };

        return boleto;
    }

    // CONVERTIR JSON DE LARAVEL A OBJETO SALIDA
    private Salida ConvertirSalida(JToken dato)
    {
        return new Salida
        {
            Id = (int?)dato["id"] ?? 0,
            Destino = dato["destino"]?.ToString(),
            Fecha = dato["fecha"]?.ToString(),
            Hora = dato["hora"]?.ToString(),
            Bus = dato["bus"]?.ToString(),
            Precio = (decimal?)dato["precio"] ?? 0,
            Capacidad = (int?)dato["capacidad"] ?? 0,
            AsientosLibres = (int?)dato["asientos_libres"] ?? 0
        };
    }

    // CLIENTE HTTP PARA REST Y GRAPHQL
    private JToken Peticion(
        string metodo, string url, JObject datos, string token)
    {
        try
        {
            HttpWebRequest request =
                (HttpWebRequest)WebRequest.Create(url);

            request.Method = metodo;
            request.Accept = "application/json";
            request.Timeout = 10000;
            request.ReadWriteTimeout = 10000;

            if (!string.IsNullOrEmpty(token))
            {
                request.Headers[HttpRequestHeader.Authorization] =
                    "Bearer " + token;
            }

            if (datos != null)
            {
                request.ContentType = "application/json";

                byte[] contenido = Encoding.UTF8.GetBytes(
                    datos.ToString(Formatting.None)
                );

                request.ContentLength = contenido.Length;

                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(contenido, 0, contenido.Length);
                }
            }

            using (HttpWebResponse response =
                (HttpWebResponse)request.GetResponse())
            using (StreamReader reader = new StreamReader(
                response.GetResponseStream()))
            {
                return JToken.Parse(reader.ReadToEnd());
            }
        }
        catch (WebException ex)
        {
            HttpWebResponse respuesta =
                ex.Response as HttpWebResponse;

            if (respuesta == null)
            {
                throw ErrorServidor(
                    "Servicio no disponible: " + url +
                    ". " + ex.Message
                );
            }

            int estado = (int)respuesta.StatusCode;
            string mensaje = "Error HTTP " + estado;

            using (respuesta)
            using (StreamReader reader = new StreamReader(
                respuesta.GetResponseStream()))
            {
                string contenido = reader.ReadToEnd();

                try
                {
                    JToken error = JToken.Parse(contenido);

                    mensaje = error["mensaje"]?.ToString() ??
                              error["message"]?.ToString() ??
                              mensaje;
                }
                catch (JsonException)
                {
                    // Mantener el mensaje HTTP si no es JSON.
                }
            }

            if (estado >= 400 && estado < 500)
            {
                throw ErrorCliente(mensaje);
            }

            throw ErrorServidor(mensaje);
        }
        catch (JsonException)
        {
            throw ErrorServidor("El servicio devolvió JSON inválido");
        }
    }

    // ERRORES SOAP
    private SoapException ErrorCliente(string mensaje)
    {
        return new SoapException(
            mensaje, SoapException.ClientFaultCode
        );
    }

    private SoapException ErrorServidor(string mensaje)
    {
        return new SoapException(
            mensaje, SoapException.ServerFaultCode
        );
    }
}

// OBJETO SALIDA EXPUESTO EN EL WSDL
public class Salida
{
    public int Id { get; set; }
    public string Destino { get; set; }
    public string Fecha { get; set; }
    public string Hora { get; set; }
    public string Bus { get; set; }
    public decimal Precio { get; set; }
    public int Capacidad { get; set; }
    public int AsientosLibres { get; set; }
}

// OBJETO BOLETO EXPUESTO EN EL WSDL
public class Boleto
{
    public string Codigo { get; set; }
    public string NombrePasajero { get; set; }
    public string Destino { get; set; }
    public string Fecha { get; set; }
    public string Hora { get; set; }
    public int NumeroAsiento { get; set; }
    public decimal Precio { get; set; }
}
