public class Notification {
    static Object obj = new Object();
    static class Worker extends Thread {
        public void run() {
            try {
                synchronized (obj) {
                    System.out.println("Antes del wait");
                    obj.wait();
                    System.out.println("Después del wait");
                }
            } catch (Exception e) {
                e.printStackTrace();
            }
        }
    }

    public static void main(String[] args) throws Exception {
        Worker w = new Worker();
        w.start();
        Thread.sleep(1000);
        synchronized (obj) {
            System.out.println("Antes del notify");
            obj.notify();
            System.out.println("Después del notify");
        }
    }
}
