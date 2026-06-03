// ************************************************************
// Práctica 07 / Aplicaciones Distribuidas
// Boris Guachamin
// Fecha de realización: 02/06/2026
// Fecha de entrega: 03/06/2026
// Resultados:
//   * Se creó la clase Protocolo que centraliza la lógica
//     de comunicación (HazOperacion y ResolverPedido),
//     desacoplando cliente y servidor de Pedido/Respuesta.
// Conclusiones:
//   * Centralizar la lógica de protocolo mejora la 
//     mantenibilidad y reduce la duplicación de código.
// Recomendaciones:
//   * Usar esta clase como único punto de entrada para
//     la comunicación entre cliente y servidor.
// ************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Protocolo
{
    // Representa una solicitud del cliente al servidor
    public class Pedido
    {
        public string Comando { get; set; }
        public string[] Parametros { get; set; }

        // Convierte un string recibido por la red en un objeto Pedido
        public static Pedido Procesar(string mensaje)
        {
            var partes = mensaje.Split(' ');
            return new Pedido
            {
                Comando = partes[0].ToUpper(),
                Parametros = partes.Skip(1).ToArray()
            };
        }

        public override string ToString()
        {
            return $"{Comando} {string.Join(" ", Parametros)}";
        }
    }

    // Representa la respuesta del servidor al cliente
    public class Respuesta
    {
        public string Estado { get; set; }
        public string Mensaje { get; set; }

        public override string ToString()
        {
            return $"{Estado} {Mensaje}";
        }
    }

    // Clase principal del protocolo: centraliza el envío de pedidos
    // (lado cliente) y la resolución de pedidos (lado servidor)
    public class Protocolo
    {
        // Diccionario compartido que cuenta consultas por dirección IP de cliente
        private static Dictionary<string, int> listadoClientes
            = new Dictionary<string, int>();

        // Envía un pedido al servidor a través del flujo de red y devuelve la respuesta deserializada
        public static Respuesta HazOperacion(Pedido pedido, NetworkStream flujo)
        {
            try
            {
                // Serializar el pedido a bytes y enviarlo
                byte[] bufferTx = Encoding.UTF8.GetBytes(
                    pedido.Comando + " " + string.Join(" ", pedido.Parametros));
                flujo.Write(bufferTx, 0, bufferTx.Length);

                // Leer la respuesta del servidor
                byte[] bufferRx = new byte[1024];
                int bytesRx = flujo.Read(bufferRx, 0, bufferRx.Length);
                string mensaje = Encoding.UTF8.GetString(bufferRx, 0, bytesRx);

                // Parsear la respuesta: primer token = estado, resto = mensaje
                var partes = mensaje.Split(' ');
                return new Respuesta
                {
                    Estado = partes[0],
                    Mensaje = string.Join(" ", partes.Skip(1).ToArray())
                };
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Error al transmitir: " + ex.Message);
                return null;
            }
        }

        // --- LADO SERVIDOR ---
        // Procesa un pedido recibido y retorna la respuesta apropiada
        public static Respuesta ResolverPedido(Pedido pedido, string direccionCliente)
        {
            Respuesta respuesta = new Respuesta
            { Estado = "NOK", Mensaje = "Comando no reconocido" };

            switch (pedido.Comando)
            {
                case "INGRESO":
                    // Valida credenciales: usuario root / contraseña admin20
                    if (pedido.Parametros.Length == 2 &&
                        pedido.Parametros[0] == "root" &&
                        pedido.Parametros[1] == "admin20")
                    {
                        // Acceso siempre concedido cuando las credenciales son correctas
                        // (se corrigió el bug del servidor original que negaba al azar)
                        respuesta = new Respuesta
                        { Estado = "OK", Mensaje = "ACCESO_CONCEDIDO" };
                    }
                    else
                    {
                        respuesta.Mensaje = "ACCESO_NEGADO";
                    }
                    break;

                case "CALCULO":
                    // Requiere exactamente 3 parámetros: modelo, marca, placa
                    if (pedido.Parametros.Length == 3)
                    {
                        string placa = pedido.Parametros[2];
                        if (ValidarPlaca(placa))
                        {
                            byte indicadorDia = ObtenerIndicadorDia(placa);
                            respuesta = new Respuesta
                            { Estado = "OK", Mensaje = $"{placa} {indicadorDia}" };
                            ContadorCliente(direccionCliente);
                        }
                        else
                        {
                            respuesta.Mensaje = "Placa no válida";
                        }
                    }
                    break;

                case "CONTADOR":
                    // Devuelve cuántas consultas CALCULO ha hecho este cliente
                    if (listadoClientes.ContainsKey(direccionCliente))
                    {
                        respuesta = new Respuesta
                        { Estado = "OK", Mensaje = listadoClientes[direccionCliente].ToString() };
                    }
                    else
                    {
                        respuesta.Mensaje = "No hay solicitudes previas";
                    }
                    break;
            }

            return respuesta;
        }

        // Valida que la placa tenga el formato AAA0000 (3 letras + 4 dígitos)
        private static bool ValidarPlaca(string placa)
        {
            return Regex.IsMatch(placa, @"^[A-Z]{3}[0-9]{4}$");
        }

        // Determina el día de restricción vehicular según el último dígito de la placa
        private static byte ObtenerIndicadorDia(string placa)
        {
            int ultimoDigito = int.Parse(placa.Substring(6, 1));
            switch (ultimoDigito)
            {
                case 1: case 2: return 0b00100000; // Lunes
                case 3: case 4: return 0b00010000; // Martes
                case 5: case 6: return 0b00001000; // Miércoles
                case 7: case 8: return 0b00000100; // Jueves
                case 9: case 0: return 0b00000010; // Viernes
                default: return 0;
            }
        }

        // Incrementa (o inicializa) el contador de consultas del cliente
        private static void ContadorCliente(string direccionCliente)
        {
            if (listadoClientes.ContainsKey(direccionCliente))
                listadoClientes[direccionCliente]++;
            else
                listadoClientes[direccionCliente] = 1;
        }
    }
}