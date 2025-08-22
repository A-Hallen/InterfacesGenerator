using System.Diagnostics;

namespace GeneradorInterfaces;

public static class NpmPublisher
{
    public static async Task PublishNpmPackage(string outputDir, bool autoLogin = false, string scope = "")
    {
        Console.WriteLine("Publicando paquete npm...");

        try
        {
            // Verificar que npm está instalado
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

            // Verificar si el usuario está autenticado en npm
            if (autoLogin)
            {
                Console.WriteLine("Verificando autenticación en npm...");
                var npmWhoamiProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "npm",
                        Arguments = "whoami",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                npmWhoamiProcess.Start();
                await npmWhoamiProcess.WaitForExitAsync();

                if (npmWhoamiProcess.ExitCode != 0)
                {
                    Console.WriteLine("No se ha detectado una sesión activa en npm. Por favor, ejecute 'npm login' antes de publicar.");
                    Console.WriteLine("Alternativamente, puede crear un archivo .npmrc en su directorio de usuario con un token de acceso.");
                    return;
                }
                else
                {
                    var username = await npmWhoamiProcess.StandardOutput.ReadToEndAsync();
                    Console.WriteLine($"Publicando como usuario npm: {username.Trim()}");
                }
            }

            // Instalar dependencias
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

            // Compilar el proyecto TypeScript
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

            // Publicar el paquete npm
            string publishArgs = "publish";
            
            // Añadir scope si se especifica
            if (!string.IsNullOrEmpty(scope))
            {
                publishArgs += $" --scope={scope}";
            }
            
            // Añadir opción para acceso público si es un paquete con scope
            if (!string.IsNullOrEmpty(scope))
            {
                publishArgs += " --access=public";
            }

            var npmPublishProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "npm",
                    Arguments = publishArgs,
                    WorkingDirectory = outputDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            npmPublishProcess.Start();
            await npmPublishProcess.WaitForExitAsync();

            if (npmPublishProcess.ExitCode != 0)
            {
                var error = await npmPublishProcess.StandardError.ReadToEndAsync();
                Console.WriteLine($"Error al publicar el paquete npm: {error}");
                return;
            }

            var output = await npmPublishProcess.StandardOutput.ReadToEndAsync();
            Console.WriteLine($"Paquete npm publicado con éxito: {output}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al publicar el paquete npm: {ex.Message}");
        }
    }
}
