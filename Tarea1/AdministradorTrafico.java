import java.io.InputStream;
import java.io.OutputStream;
import java.io.IOException;
import java.net.Socket;
import java.net.ServerSocket;

class AdministradorTrafico {
    static String host_remoto_1, host_remoto_2;
    static int puerto_remoto_1, puerto_remoto_2, puerto_local;

    static class Worker_1 extends Thread {
        Socket cliente_1, cliente_2, cliente_3;

        Worker_1(Socket cliente_1) {
            this.cliente_1 = cliente_1;
        }

        public void run() {
            try {
                // Conectarse a los dos servidores remotos
                cliente_2 = new Socket(host_remoto_1, puerto_remoto_1);
                cliente_3 = new Socket(host_remoto_2, puerto_remoto_2);

                // Thread para manejar la respuesta del primer servidor
                new Worker_2(cliente_1, cliente_2).start();

                InputStream entrada_1 = cliente_1.getInputStream();
                OutputStream salida_2 = cliente_2.getOutputStream();
                OutputStream salida_3 = cliente_3.getOutputStream();

                byte[] buffer = new byte[1024];
                int n;
                while ((n = entrada_1.read(buffer)) != -1) {
                    salida_2.write(buffer, 0, n);
                    salida_2.flush();
                    salida_3.write(buffer, 0, n); // Enviar al segundo servidor
                    salida_3.flush();
                }
            } catch (IOException e) {
                e.printStackTrace();
            } finally {
                try {
                    if (cliente_1 != null)
                        cliente_1.close();
                    if (cliente_2 != null)
                        cliente_2.close();
                    if (cliente_3 != null)
                        cliente_3.close();
                } catch (IOException e2) {
                    e2.printStackTrace();
                }
            }
        }
    }

    static class Worker_2 extends Thread {

        Socket cliente_1, cliente_2;

        Worker_2(Socket cliente_1, Socket cliente_2) {
            this.cliente_1 = cliente_1;
            this.cliente_2 = cliente_2;
        }

        public void run() {
            try {
                InputStream entrada_2 = cliente_2.getInputStream();
                OutputStream salida_1 = cliente_1.getOutputStream();
                byte[] buffer = new byte[4096];
                int n;
                while ((n = entrada_2.read(buffer)) != -1) {
                    salida_1.write(buffer, 0, n);
                    salida_1.flush();
                }
            } catch (IOException e) {
                e.printStackTrace();
            } finally {
                try {
                    if (cliente_1 != null)
                        cliente_1.close();
                    if (cliente_2 != null)
                        cliente_2.close();
                } catch (IOException e2) {
                    e2.printStackTrace();
                }
            }
        }
    }

    public static void main(String[] args) throws Exception {
        if (args.length != 5) {
            System.err.println(
                    "Uso:\njava Proxy <host-remoto-1> <puerto-remoto-1> <host-remoto-2> <puerto-remoto-2> <puerto-local>");
            System.exit(1);
        }
        host_remoto_1 = args[0];
        puerto_remoto_1 = Integer.parseInt(args[1]);
        host_remoto_2 = args[2];
        puerto_remoto_2 = Integer.parseInt(args[3]);
        puerto_local = Integer.parseInt(args[4]);

        System.out.println("host_remoto_1: " + host_remoto_1 + ", puerto_remoto_1: " + puerto_remoto_1);
        System.out.println("host_remoto_2: " + host_remoto_2 + ", puerto_remoto_2: " + puerto_remoto_2);
        System.out.println("puerto_local: " + puerto_local);

        ServerSocket ss = new ServerSocket(puerto_local);

        for (;;) {
            Socket cliente_1 = ss.accept();
            new Worker_1(cliente_1).start();
        }
    }
}
