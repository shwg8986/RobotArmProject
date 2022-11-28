using System.IO;

public static class DirectoryUtils {
    public static DirectoryInfo SafeCreateDirectory(string path) {
        if (Directory.Exists(path)) {
            return null;
        } else {
            return Directory.CreateDirectory(path);
        }
    }
}
