using System.Diagnostics;
using System.Security.Principal;

namespace AlyxLib;
public class PrivilegeChecker
{
    public const int ERROR_PRIVILEGE_NOT_HELD = 1314; // 0x00000514
    public static bool CanCreateSymlinks()
    {
        string targetFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
        string linkFile = targetFile + "_link";

        try
        {
            File.WriteAllText(targetFile, "test");
            File.CreateSymbolicLink(linkFile, targetFile);
            return true;
        }
        catch (IOException ex) when ((ex.HResult & 0xFFFF) == ERROR_PRIVILEGE_NOT_HELD)
        {
            return false;
        }
        catch
        {
            // Ignore other errors for this check (like FS restrictions or unsupported OS)
            return false;
        }
        finally
        {
            try { File.Delete(targetFile); } catch { }
            try { File.Delete(linkFile); } catch { }
        }
    }

    public static bool IsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    public static void OpenDeveloperSettings()
    {
        // Opens the Windows Developer Mode settings page
        Process.Start(new ProcessStartInfo
        {
            FileName = "ms-settings:developers",
            UseShellExecute = true
        });
    }
}
