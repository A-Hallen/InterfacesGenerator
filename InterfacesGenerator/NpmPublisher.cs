using System.Diagnostics;

namespace GeneradorInterfaces;

public static class NpmPublisher
{
    public static async Task PublishNpmPackage(string outputDir)
    {
        Console.WriteLine("Publicando paquete npm...");

        try
        {
            var npmVersionProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "npm",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            npmVersionProcess.Start();
            await npmVersionProcess.WaitForExitAsync();

            if (npmVersionProcess.ExitCode != 0)
            {
                Console.WriteLine("Error: npm no está instalado o no está disponible en el PATH.");
                return;
            }

            var npmInstallProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "npm",
                    Arguments = "install",
                    WorkingDirectory = outputDir,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            npmInstallProcess.Start();
            await npmInstallProcess.WaitForExitAsync();

            if (npmInstallProcess.ExitCode != 0)
            {
                Console.WriteLine("Error al instalar dependencias npm.");
                return;
            }

            var npmBuildProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "npm",
                    Arguments = "run build",
                    WorkingDirectory = outputDir,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            npmBuildProcess.Start();
            await npmBuildProcess.WaitForExitAsync();

            if (npmBuildProcess.ExitCode != 0)
            {
                Console.WriteLine("Error al compilar el proyecto TypeScript.");
                return;
            }

            var npmPublishProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "npm",
                    Arguments = "publish",
                    WorkingDirectory = outputDir,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            npmPublishProcess.Start();
            await npmPublishProcess.WaitForExitAsync();

            if (npmPublishProcess.ExitCode != 0)
            {
                Console.WriteLine("Error al publicar el paquete npm.");
                return;
            }

            Console.WriteLine("Paquete npm publicado con éxito.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al publicar el paquete npm: {ex.Message}");
        }
    }
}
