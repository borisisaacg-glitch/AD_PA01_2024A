using System;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Protocolo; // Importa Pedido, Respuesta y la nueva clase Protocolo

namespace Servidor
{
    class Servidor
    {
        private static TcpListener escuchador;

        static void Main(string[] args)
        {
            try
            {
                // Iniciar el listener en todas las interfaces, puerto 8080
                escuchador = new TcpListener(IPAddress.Any, 8080);
                escuchador.Start();
                Console.WriteLine("Servidor inició en el puerto 8080..."); // Corregido: decía 5000

                while (true)
                {
                    // Esperar y aceptar una nueva conexión de cliente
                    TcpClient cliente = escuchador.AcceptTcpClient();
                    Console.WriteLine("Cliente conectado, puerto: {0}",
                        cliente.Client.RemoteEndPoint.ToString());

                    // Crear un hilo dedicado para atender a este cliente
                    Thread hiloCliente = new Thread(ManipuladorCliente);
                    hiloCliente.Start(cliente);
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Error de socket al iniciar el servidor: " + ex.Message);
            }
            finally
            {
                escuchador?.Stop();
            }
        }

        private static void ManipuladorCliente(object obj)
        {
            TcpClient cliente = (TcpClient)obj;
            NetworkStream flujo = null;
            try
            {
                flujo = cliente.GetStream();
                byte[] bufferRx = new byte[1024];
                int bytesRx;

                // Leer mensajes en bucle hasta que el cliente cierre la conexión
                while ((bytesRx = flujo.Read(bufferRx, 0, bufferRx.Length)) > 0)
                {
                    string mensajeRx = Encoding.UTF8.GetString(bufferRx, 0, bytesRx);

                    // Deserializar el mensaje en un objeto Pedido
                    Pedido pedido = Pedido.Procesar(mensajeRx);
                    Console.WriteLine("Se recibió: " + pedido);

                    string direccionCliente = cliente.Client.RemoteEndPoint.ToString();

                    // Delegar la resolución del pedido a la clase Protocolo
                    Respuesta respuesta = Protocolo.Protocolo.ResolverPedido(pedido, direccionCliente);
                    Console.WriteLine("Se envió: " + respuesta);

                    // Serializar y enviar la respuesta al cliente
                    byte[] bufferTx = Encoding.UTF8.GetBytes(respuesta.ToString());
                    flujo.Write(bufferTx, 0, bufferTx.Length);
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Error de socket al manejar el cliente: " + ex.Message);
            }
            finally
            {
                flujo?.Close();
                cliente?.Close();
            }
        }
    }
}