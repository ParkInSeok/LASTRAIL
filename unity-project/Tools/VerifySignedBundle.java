import java.io.InputStream;
import java.nio.file.Path;
import java.security.MessageDigest;
import java.security.cert.Certificate;
import java.util.Enumeration;
import java.util.HexFormat;
import java.util.jar.JarEntry;
import java.util.jar.JarFile;

// Reading every non-metadata entry triggers JAR signature verification.
class VerifySignedBundle {
    public static void main(String[] args) throws Exception {
        if (args.length != 2 || !args[1].matches("[0-9a-fA-F]{64}")) throw new IllegalArgumentException("Expected bundle and SHA256.");
        int verified = 0;
        try (JarFile jar = new JarFile(Path.of(args[0]).toFile(), true)) {
            Enumeration<JarEntry> entries = jar.entries();
            byte[] buffer = new byte[8192];
            while (entries.hasMoreElements()) {
                JarEntry entry = entries.nextElement();
                if (entry.isDirectory()) continue;
                try (InputStream stream = jar.getInputStream(entry)) { while (stream.read(buffer) != -1) { } }
                String upper = entry.getName().toUpperCase(java.util.Locale.ROOT);
                if (upper.equals("META-INF/MANIFEST.MF") || upper.matches("META-INF/[^/]+\\.(SF|RSA|DSA|EC)")) continue;
                Certificate[] certificates = entry.getCertificates();
                if (certificates == null || certificates.length == 0) throw new SecurityException("Unsigned bundle entry.");
                if (entry.getCodeSigners() == null || entry.getCodeSigners().length != 1) throw new SecurityException("Expected exactly one signer.");
                String hash = HexFormat.of().formatHex(MessageDigest.getInstance("SHA-256").digest(certificates[0].getEncoded()));
                if (!hash.equalsIgnoreCase(args[1])) throw new SecurityException("Unexpected bundle signer.");
                verified++;
            }
        }
        if (verified == 0) throw new SecurityException("No signed payload entries.");
        System.out.println("Verified signed bundle payload.");
    }
}
