public class ASynchronized extends Thread {
    static long n;
    static Object obj = new Object();

    public void run() {
        for (int i = 0; i < 100000; i++) {
            synchronized (obj) {
                n++;
            }
        }
    }

    public static void main(String[] args) throws Exception {
        ASynchronized t1 = new ASynchronized();
        ASynchronized t2 = new ASynchronized();
        t1.start();
        t2.start();
        t1.join();
        t2.join();
        System.out.println(n);
    }
}