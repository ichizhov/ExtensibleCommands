package org.extensiblecommands;

/**
 * Implements a waitable synchronized object similar to C# ManualResetEvent class.
 */
public class ManualResetEvent {
    /**
     * The underlying waitable synchronization object
     */
    private final Object event = new Object();
    
    /**
     * Armed state of the synchronization object
     */
    private volatile boolean armed;

    /**
     * Constructor
     * @param initialState      Whether Manual Reset Event is initially armed or not
     */
    public ManualResetEvent(boolean initialState) {
        armed = initialState;
    }

    /**
     * Arm synchronization object: after calling it is ready to be waited on
     */
    public final void reset() {
        armed = true;
    }

    /**
     * Signal synchronization object: all waiting objects are notified
     */
    public final void set() {
        synchronized (event) {
            event.notifyAll();
        }
        armed = false;
    }

    /**
     * Wait for the synchronization object
     * @param timeoutMsec               Wait timeout (in msec)
     */
    public final void waitOne(int timeoutMsec) throws InterruptedException {
        if (timeoutMsec < 0)
            throw new RuntimeException("Timeout value cannot be negative");
        synchronized (event) {
            if (armed) {
                if (timeoutMsec == 0)
                    event.wait();
                else
                    event.wait(timeoutMsec);
            }
        }
    }
}
