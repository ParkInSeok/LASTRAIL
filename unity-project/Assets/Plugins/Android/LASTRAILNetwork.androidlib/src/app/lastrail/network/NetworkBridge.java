package app.lastrail.network;

import android.content.Context;
import android.net.ConnectivityManager;
import android.net.Network;
import android.net.NetworkCapabilities;
import android.os.Build;

public final class NetworkBridge {
    public interface Listener { void onState(int state); }
    private final ConnectivityManager manager;
    private final Listener listener;
    private boolean started;
    private ConnectivityManager.NetworkCallback callback;
    private Network observed;

    public NetworkBridge(Context context, Listener listener) {
        this.manager = (ConnectivityManager) context.getApplicationContext().getSystemService(Context.CONNECTIVITY_SERVICE);
        this.listener = listener;
    }

    public synchronized void start() {
        if (started) return;
        started = true;
        callback = new ConnectivityManager.NetworkCallback() {
            @Override public void onAvailable(Network network) { available(this, network); }
            @Override public void onCapabilitiesChanged(Network network, NetworkCapabilities caps) { capabilities(this, network, caps); }
            @Override public void onLost(Network network) { lost(this, network); }
        };
        try {
            if (Build.VERSION.SDK_INT < 24) { listener.onState(1); return; }
            manager.registerDefaultNetworkCallback(callback);
            refresh();
        } catch (RuntimeException error) { listener.onState(1); }
    }

    private synchronized void available(ConnectivityManager.NetworkCallback source, Network network) {
        if (!started || source != callback || network.equals(observed)) return;
        observed = network;
        listener.onState(1);
    }

    private synchronized void capabilities(ConnectivityManager.NetworkCallback source, Network network, NetworkCapabilities caps) {
        if (!started || source != callback || !network.equals(observed)) return;
        publish(caps);
    }

    private synchronized void lost(ConnectivityManager.NetworkCallback source, Network network) {
        if (!started || source != callback || !network.equals(observed)) return;
        observed = null;
        listener.onState(0);
    }

    private void publish(NetworkCapabilities caps) {
        listener.onState(classify(caps));
    }

    private static int classify(NetworkCapabilities caps) {
        if (caps == null) return 1;
        boolean internet = caps.hasCapability(NetworkCapabilities.NET_CAPABILITY_INTERNET);
        boolean validated = caps.hasCapability(NetworkCapabilities.NET_CAPABILITY_VALIDATED);
        boolean wifi = caps.hasTransport(NetworkCapabilities.TRANSPORT_WIFI);
        boolean cellular = caps.hasTransport(NetworkCapabilities.TRANSPORT_CELLULAR);
        return !internet ? 0 : !validated ? 1 : wifi && !cellular ? 2 : 3;
    }

    public synchronized void refresh() {
        if (!started) return;
        listener.onState(snapshot());
    }

    public synchronized int snapshot() {
        if (!started) return 1;
        try {
            if (Build.VERSION.SDK_INT < 23) return 1;
            Network network = manager.getActiveNetwork();
            observed = network;
            if (network == null) return 0;
            NetworkCapabilities caps = manager.getNetworkCapabilities(network);
            return classify(caps);
        } catch (RuntimeException error) { return 1; }
    }

    public synchronized void stop() {
        if (!started) return;
        started = false;
        try { manager.unregisterNetworkCallback(callback); }
        catch (RuntimeException ignored) { }
        callback = null;
    }
}
