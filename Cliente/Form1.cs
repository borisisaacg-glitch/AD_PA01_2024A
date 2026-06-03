using System;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using Protocolo; // Importa Pedido, Respuesta y la nueva clase Protocolo

namespace Cliente
{
    public partial class FrmValidador : Form
    {
        private TcpClient remoto;
        private NetworkStream flujo;

        public FrmValidador()
        {
            InitializeComponent();
        }

        private void FrmValidador_Load(object sender, EventArgs e)
        {
            try
            {
                // Establece la conexión TCP con el servidor al iniciar el formulario
                remoto = new TcpClient("127.0.0.1", 8080);
                flujo = remoto.GetStream();
            }
            catch (SocketException ex)
            {
                MessageBox.Show("No se pudo establecer conexión: " + ex.Message, "ERROR");
            }
            // Nota: se eliminó el finally que cerraba flujo/remoto al arrancar,
            // ese era el bug principal — cerraba la conexión recién abierta

            // Deshabilitar panel de placa hasta que el usuario inicie sesión
            panPlaca.Enabled = false;
            chkLunes.Enabled = false;
            chkMartes.Enabled = false;
            chkMiercoles.Enabled = false;
            chkJueves.Enabled = false;
            chkViernes.Enabled = false;
            chkDomingo.Enabled = false;
            chkSabado.Enabled = false;
        }

        private void btnIniciar_Click(object sender, EventArgs e)
        {
            string usuario = txtUsuario.Text;
            string contraseña = txtPassword.Text;

            if (usuario == "" || contraseña == "")
            {
                MessageBox.Show("Se requiere el ingreso de usuario y contraseña", "ADVERTENCIA");
                return;
            }

            // Construir pedido de inicio de sesión
            Pedido pedido = new Pedido
            {
                Comando = "INGRESO",
                Parametros = new[] { usuario, contraseña }
            };

            // Delegar el envío a la clase Protocolo
            Respuesta respuesta = Protocolo.Protocolo.HazOperacion(pedido, flujo);
            if (respuesta == null)
            {
                MessageBox.Show("Hubo un error", "ERROR");
                return;
            }

            if (respuesta.Estado == "OK" && respuesta.Mensaje == "ACCESO_CONCEDIDO")
            {
                panPlaca.Enabled = true;
                panLogin.Enabled = false;
                MessageBox.Show("Acceso concedido", "INFORMACIÓN");
                txtModelo.Focus();
            }
            else if (respuesta.Estado == "NOK" && respuesta.Mensaje == "ACCESO_NEGADO")
            {
                panPlaca.Enabled = false;
                panLogin.Enabled = true;
                MessageBox.Show("No se pudo ingresar, revise credenciales", "ERROR");
                txtUsuario.Focus();
            }
        }

        private void btnConsultar_Click(object sender, EventArgs e)
        {
            string modelo = txtModelo.Text;
            string marca = txtMarca.Text;
            string placa = txtPlaca.Text;

            // Construir pedido de cálculo de restricción vehicular
            Pedido pedido = new Pedido
            {
                Comando = "CALCULO",
                Parametros = new[] { modelo, marca, placa }
            };

            Respuesta respuesta = Protocolo.Protocolo.HazOperacion(pedido, flujo);
            if (respuesta == null)
            {
                MessageBox.Show("Hubo un error", "ERROR");
                return;
            }

            if (respuesta.Estado == "NOK")
            {
                MessageBox.Show("Error en la solicitud.", "ERROR");
                // Limpiar todos los checkboxes ante un error
                chkLunes.Checked = chkMartes.Checked = chkMiercoles.Checked =
                chkJueves.Checked = chkViernes.Checked = false;
            }
            else
            {
                var partes = respuesta.Mensaje.Split(' ');
                MessageBox.Show("Se recibió: " + respuesta.Mensaje, "INFORMACIÓN");

                // El segundo token es el indicador de día en formato de byte con bits
                byte resultado = Byte.Parse(partes[1]);

                // Resetear todos antes de marcar el que corresponde
                chkLunes.Checked = chkMartes.Checked = chkMiercoles.Checked =
                chkJueves.Checked = chkViernes.Checked = false;

                switch (resultado)
                {
                    case 0b00100000: chkLunes.Checked = true; break;
                    case 0b00010000: chkMartes.Checked = true; break;
                    case 0b00001000: chkMiercoles.Checked = true; break;
                    case 0b00000100: chkJueves.Checked = true; break;
                    case 0b00000010: chkViernes.Checked = true; break;
                }
            }
        }

        private void btnNumConsultas_Click(object sender, EventArgs e)
        {
            // Solicita al servidor cuántas consultas CALCULO ha hecho este cliente
            Pedido pedido = new Pedido
            {
                Comando = "CONTADOR",
                Parametros = new[] { "hola" }
            };

            Respuesta respuesta = Protocolo.Protocolo.HazOperacion(pedido, flujo);
            if (respuesta == null)
            {
                MessageBox.Show("Hubo un error", "ERROR");
                return;
            }

            if (respuesta.Estado == "NOK")
            {
                MessageBox.Show("Error en la solicitud.", "ERROR");
            }
            else
            {
                var partes = respuesta.Mensaje.Split(' ');
                MessageBox.Show("El número de pedidos recibidos en este cliente es " + partes[0],
                    "INFORMACIÓN");
            }
        }

        private void FrmValidador_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Cerrar flujo y conexión al salir del formulario
            flujo?.Close();
            if (remoto != null && remoto.Connected)
                remoto.Close();
        }
    }
}